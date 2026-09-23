using System;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class SettingsWindow : MonoBehaviour
{
    [Serializable]
    public struct Palette
    {
        public string name;
        public Color darkest, dark, light, lightest;
    }

    [SerializeField] GameObject window;
    [SerializeField] Slider musicSlider;
    [SerializeField] Slider sfxSlider;
    [SerializeField] OptionStepper paletteStepper;
    [SerializeField] Toggle ditherToggle;
    [SerializeField] Toggle fastTextToggle;

    [Header("Applies to")]
    [SerializeField] AudioMixer mixer;
    [SerializeField] Material paletteMaterial;
    [SerializeField] Palette[] palettes;
    [SerializeField, Range(0, 1)] float ditherWidth = 0.5f;

    [Header("Feedback")]
    [SerializeField] AudioSource sfx;
    [SerializeField] AudioClip tickClip;

    public static bool FastText => PlayerPrefs.GetInt("FastText", 0) == 1;

    public GameObject FirstRow => musicSlider.gameObject;

    void Awake() => window.SetActive(false);

    // Start, not Awake: the mixer ignores SetFloat until it has initialised.
    void Start()
    {
        paletteStepper.options = Array.ConvertAll(palettes, p => p.name);

        musicSlider.SetValueWithoutNotify(PlayerPrefs.GetInt("Music", 8));
        sfxSlider.SetValueWithoutNotify(PlayerPrefs.GetInt("Sfx", 8));
        paletteStepper.SetValueWithoutNotify(PlayerPrefs.GetInt("Palette", 0));
        ditherToggle.SetIsOnWithoutNotify(PlayerPrefs.GetInt("Dither", 1) == 1);
        fastTextToggle.SetIsOnWithoutNotify(FastText);

        musicSlider.onValueChanged.AddListener(v => Save("Music", (int)v));
        sfxSlider.onValueChanged.AddListener(v => Save("Sfx", (int)v));
        paletteStepper.onValueChanged.AddListener(i => Save("Palette", i));
        ditherToggle.onValueChanged.AddListener(on => Save("Dither", on ? 1 : 0));
        fastTextToggle.onValueChanged.AddListener(on => Save("FastText", on ? 1 : 0));

        Apply();
    }

    public void Show() => window.SetActive(true);

    public void Hide()
    {
        window.SetActive(false);
        PlayerPrefs.Save();
    }

    void Save(string key, int value)
    {
        PlayerPrefs.SetInt(key, value);
        Apply();
        if (sfx != null && tickClip != null) sfx.PlayOneShot(tickClip);
    }

    void Apply()
    {
        mixer.SetFloat("MusicVolume", ToDecibels(musicSlider.value));
        mixer.SetFloat("SfxVolume", ToDecibels(sfxSlider.value));

        Palette p = palettes[paletteStepper.Value];
        paletteMaterial.SetColor("_Color0", p.darkest);
        paletteMaterial.SetColor("_Color1", p.dark);
        paletteMaterial.SetColor("_Color2", p.light);
        paletteMaterial.SetColor("_Color3", p.lightest);
        paletteMaterial.SetFloat("_TransitionWidth", ditherToggle.isOn ? ditherWidth : 0f);
    }

    // Sliders run 0..10. The mixer works in decibels, so step 0 has to be silence.
    static float ToDecibels(float step) => step > 0 ? Mathf.Log10(step / 10f) * 20f : -80f;
}
