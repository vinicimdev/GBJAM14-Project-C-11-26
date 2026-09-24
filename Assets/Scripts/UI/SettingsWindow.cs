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
    [SerializeField] Material paletteMaterial;
    [SerializeField, Tooltip("The first one is the default every launch starts on.")] Palette[] palettes;
    [SerializeField, Range(0, 1)] float ditherWidth = 0.5f;

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

    public GameObject FirstRow => musicSlider.gameObject;

    // Enter Play Mode Options skip the domain reload, so statics would survive from the last play.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetSession()
    {
        sessionPalette = 0;
        TouchControlsChanged = null;
    }

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

#if UNITY_EDITOR
        RememberMaterial();
#endif
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
        paletteMaterial.SetColor("_Color0", p.darkest);
        paletteMaterial.SetColor("_Color1", p.dark);
        paletteMaterial.SetColor("_Color2", p.light);
        paletteMaterial.SetColor("_Color3", p.lightest);
        paletteMaterial.SetFloat("_TransitionWidth", ditherToggle.isOn ? ditherWidth : 0f);

        TouchControlsChanged?.Invoke(touchToggle.isOn);
    }

    // Sliders run 0..10. The mixer works in decibels, so step 0 has to be silence.
    static float ToDecibels(float step) => step > 0 ? Mathf.Log10(step / 10f) * 20f : -80f;

#if UNITY_EDITOR
    static readonly string[] MaterialColors = { "_Color0", "_Color1", "_Color2", "_Color3" };
    static Material edited;
    static Color[] savedColors;
    static float savedWidth;

    // In the editor, play mode writes into the material asset itself.
    // Put its colors and dither back when play stops, so the next play and git both see the default.
    void RememberMaterial()
    {
        if (edited != null) return;

        edited = paletteMaterial;
        var saved = new UnityEditor.SerializedObject(edited).FindProperty("m_SavedProperties");
        savedColors = Array.ConvertAll(MaterialColors, c => SavedValue(saved, "m_Colors", c).colorValue);
        savedWidth = SavedValue(saved, "m_Floats", "_TransitionWidth").floatValue;
        UnityEditor.EditorApplication.playModeStateChanged += RestoreMaterial;
    }

    static void RestoreMaterial(UnityEditor.PlayModeStateChange state)
    {
        if (state != UnityEditor.PlayModeStateChange.ExitingPlayMode) return;

        UnityEditor.EditorApplication.playModeStateChanged -= RestoreMaterial;
        var so = new UnityEditor.SerializedObject(edited);
        var saved = so.FindProperty("m_SavedProperties");
        for (int i = 0; i < MaterialColors.Length; i++) SavedValue(saved, "m_Colors", MaterialColors[i]).colorValue = savedColors[i];
        SavedValue(saved, "m_Floats", "_TransitionWidth").floatValue = savedWidth;
        so.ApplyModifiedPropertiesWithoutUndo();
        edited = null;
    }

    // The serialized numbers, not GetColor/SetColor: those round-trip through linear space and drift.
    static UnityEditor.SerializedProperty SavedValue(UnityEditor.SerializedProperty saved, string list, string name)
    {
        UnityEditor.SerializedProperty entries = saved.FindPropertyRelative(list);
        for (int i = 0; i < entries.arraySize; i++)
        {
            UnityEditor.SerializedProperty entry = entries.GetArrayElementAtIndex(i);
            if (entry.FindPropertyRelative("first").stringValue == name) return entry.FindPropertyRelative("second");
        }
        throw new ArgumentException($"{name} isn't saved on {edited.name}");
    }
#endif
}
