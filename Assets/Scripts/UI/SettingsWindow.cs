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
    [SerializeField] Toggle touchToggle;

    [Header("Applies to")]
    [SerializeField] AudioMixer mixer;
    [SerializeField, Tooltip("The first one is the default every launch starts on. Keep it matching Pallete4Full.mat.")] Palette[] palettes;

    [Header("Feedback")]
    [SerializeField] AudioSource sfx;
    [SerializeField] AudioClip tickClip;

    public static bool FastText => PlayerPrefs.GetInt("FastText", 0) == 1;

    // Phones and tablets start with the touch buttons showing.
    public static bool TouchControls => PlayerPrefs.GetInt("Touch", Application.isMobilePlatform ? 1 : 0) == 1;

    // Raised with the TOUCH row's value every time settings are applied. The touch buttons listen here.
    public static event Action<bool> TouchControlsChanged;

    // The palette isn't saved: it lasts until the game is closed or the page reloads.
    static int sessionPalette;

    // Globals the palette shader reads instead of its material, so the material asset never changes.
    static readonly int PaletteOn = Shader.PropertyToID("_SettingsPaletteOn");
    static readonly int DitherOff = Shader.PropertyToID("_SettingsDitherOff");
    static readonly int[] PaletteColors =
    {
        Shader.PropertyToID("_SettingsColor0"), Shader.PropertyToID("_SettingsColor1"),
        Shader.PropertyToID("_SettingsColor2"), Shader.PropertyToID("_SettingsColor3"),
    };

    public GameObject FirstRow => musicSlider.gameObject;

    // Enter Play Mode Options skip the domain reload, so statics would survive from the last play.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetSession()
    {
        sessionPalette = 0;
        TouchControlsChanged = null;
        UseMaterialPalette();
    }

    // Hands the palette and dither back to the material.
    static void UseMaterialPalette()
    {
        Shader.SetGlobalFloat(PaletteOn, 0f);
        Shader.SetGlobalFloat(DitherOff, 0f);
    }

#if UNITY_EDITOR
    // Globals outlive play mode in the editor, so the scene view would keep the last palette otherwise.
    [UnityEditor.InitializeOnLoadMethod]
    static void UseMaterialPaletteWhenPlayStops() =>
        UnityEditor.EditorApplication.playModeStateChanged += state =>
        {
            if (state == UnityEditor.PlayModeStateChange.EnteredEditMode) UseMaterialPalette();
        };
#endif

    void Awake() => window.SetActive(false);

    // Start, not Awake: the mixer ignores SetFloat until it has initialised.
    void Start()
    {
        paletteStepper.options = Array.ConvertAll(palettes, p => p.name);

        musicSlider.SetValueWithoutNotify(PlayerPrefs.GetInt("Music", 8));
        sfxSlider.SetValueWithoutNotify(PlayerPrefs.GetInt("Sfx", 8));
        paletteStepper.SetValueWithoutNotify(sessionPalette);
        ditherToggle.SetIsOnWithoutNotify(PlayerPrefs.GetInt("Dither", 1) == 1);
        fastTextToggle.SetIsOnWithoutNotify(FastText);
        touchToggle.SetIsOnWithoutNotify(TouchControls);

        musicSlider.onValueChanged.AddListener(v => Save("Music", (int)v));
        sfxSlider.onValueChanged.AddListener(v => Save("Sfx", (int)v));
        paletteStepper.onValueChanged.AddListener(i => { sessionPalette = i; Changed(); });
        ditherToggle.onValueChanged.AddListener(on => Save("Dither", on ? 1 : 0));
        fastTextToggle.onValueChanged.AddListener(on => Save("FastText", on ? 1 : 0));
        touchToggle.onValueChanged.AddListener(on => Save("Touch", on ? 1 : 0));

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
        Changed();
    }

    void Changed()
    {
        Apply();
        if (sfx != null && tickClip != null) sfx.PlayOneShot(tickClip);
    }

    void Apply()
    {
        mixer.SetFloat("MusicVolume", ToDecibels(musicSlider.value));
        mixer.SetFloat("SfxVolume", ToDecibels(sfxSlider.value));

        Palette p = palettes[paletteStepper.Value];
        SetPaletteColor(0, p.darkest);
        SetPaletteColor(1, p.dark);
        SetPaletteColor(2, p.light);
        SetPaletteColor(3, p.lightest);
        Shader.SetGlobalFloat(PaletteOn, 1f);
        // Dither on uses the material's Transition Width.
        Shader.SetGlobalFloat(DitherOff, ditherToggle.isOn ? 0f : 1f);

        TouchControlsChanged?.Invoke(touchToggle.isOn);
    }

    // Unlike material colors, globals aren't converted from sRGB, so do it here to match the material's look.
    static void SetPaletteColor(int index, Color color) =>
        Shader.SetGlobalColor(PaletteColors[index], QualitySettings.activeColorSpace == ColorSpace.Linear ? color.linear : color);

    // Sliders run 0..10. The mixer works in decibels, so step 0 has to be silence.
    static float ToDecibels(float step) => step > 0 ? Mathf.Log10(step / 10f) * 20f : -80f;
}
