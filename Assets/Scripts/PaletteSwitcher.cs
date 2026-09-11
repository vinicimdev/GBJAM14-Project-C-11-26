using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Ten shells for the same console, on the top row of the keyboard.
///
/// The palette shader only ever looks at four colours, so a "palette" here is four
/// entries and nothing else -- no ramps, no gradients, no lookup textures. 1 through 9
/// and then 0 select them, in that order, which is the only order a person's fingers
/// expect.
///
/// Entries are written darkest to lightest because <c>Palette4_Fullscreen</c> maps
/// scene luminance onto the index directly: index 0 is where the shadows and the edge
/// pass land, index 3 is the sky. Shuffling the order does not recolour the picture,
/// it inverts it.
///
/// Colours are authored here as ordinary sRGB hex, the way a person reads them off a
/// palette site, and converted to linear on the way into the material because the
/// project renders in linear space. The shader outputs its palette entry as the final
/// colour, so an unconverted value would come out visibly washed.
///
/// One caveat worth knowing in the editor: the material this writes to is an asset,
/// not an instance, so switching palettes in play mode leaves the asset on whatever
/// was picked last. That is harmless because <see cref="Start"/> puts it back on
/// palette 1 every run, but it does mean hand-tuning the four colours in the
/// inspector will not survive pressing play.
/// </summary>
public sealed class PaletteSwitcher : MonoBehaviour
{
    [Tooltip("The material on the Full Screen Pass Renderer Feature. Pallete4Full, unless you have made another.")]
    [SerializeField] private Material paletteMaterial = null;   // wired to Pallete4Full in the scene

    [Tooltip("Name the palette in the console when it changes. The HUD has no room to say it, and four colours is not enough to spell it.")]
    [SerializeField] private bool logSwitches = true;

    /// <summary>Four sRGB hex entries, darkest first, and a name for the log line.</summary>
    private readonly struct Palette
    {
        public readonly string Name;
        public readonly string Darkest, Dark, Light, Lightest;

        public Palette(string name, string darkest, string dark, string light, string lightest)
        {
            Name = name;
            Darkest = darkest;
            Dark = dark;
            Light = light;
            Lightest = lightest;
        }
    }

    // Slot 1 is the palette the material shipped with. Everything after it is a four
    // colour ramp that holds up under a shader that will happily throw away the
    // difference between two colours that only differ in hue.
    private static readonly Palette[] palettes =
    {
        new Palette("Amber",      "#000000", "#8A3700", "#FFD09D", "#FFFFFF"),
        new Palette("DMG Green",  "#0F380F", "#306230", "#8BAC0F", "#9BBC0F"),
        new Palette("Grayscale",  "#000000", "#555555", "#AAAAAA", "#FFFFFF"),
        new Palette("Pocket",     "#2A3325", "#5B6B4A", "#96A97B", "#C4CFA1"),
        new Palette("Bubblegum",  "#2D0B2E", "#8B2E67", "#F26A9B", "#FFD9E8"),
        new Palette("Ice",        "#08131E", "#1E4D6B", "#4FA3C7", "#C7F0FF"),
        new Palette("Sunset",     "#2B0F3B", "#7B3161", "#E0655B", "#FFC98B"),
        new Palette("Crimson",    "#170000", "#7A0F14", "#D9333D", "#FFB3B8"),
        new Palette("Phosphor",   "#001B0E", "#0A5C2E", "#2BC46B", "#B6FFD1"),
        new Palette("Blueprint",  "#0B1026", "#2E3A87", "#7C8FE0", "#EAF0FF"),
    };

    // Cached because SetColor by string hashes the name on every call, and this runs
    // on a keypress rather than a frame, but the habit costs nothing.
    private static readonly int Color0Id = Shader.PropertyToID("_Color0");
    private static readonly int Color1Id = Shader.PropertyToID("_Color1");
    private static readonly int Color2Id = Shader.PropertyToID("_Color2");
    private static readonly int Color3Id = Shader.PropertyToID("_Color3");

    private int current = -1;

    private void Start()
    {
        Apply(0);
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        // The top row, not the numpad, and 0 sits at the end of the row rather than
        // the start of the array. This is the whole input surface of the feature.
        if (keyboard.digit1Key.wasPressedThisFrame) Apply(0);
        else if (keyboard.digit2Key.wasPressedThisFrame) Apply(1);
        else if (keyboard.digit3Key.wasPressedThisFrame) Apply(2);
        else if (keyboard.digit4Key.wasPressedThisFrame) Apply(3);
        else if (keyboard.digit5Key.wasPressedThisFrame) Apply(4);
        else if (keyboard.digit6Key.wasPressedThisFrame) Apply(5);
        else if (keyboard.digit7Key.wasPressedThisFrame) Apply(6);
        else if (keyboard.digit8Key.wasPressedThisFrame) Apply(7);
        else if (keyboard.digit9Key.wasPressedThisFrame) Apply(8);
        else if (keyboard.digit0Key.wasPressedThisFrame) Apply(9);
    }

    /// <summary>Writes one palette into the shader. Re-selecting the current one is free.</summary>
    public void Apply(int index)
    {
        if (paletteMaterial == null)
        {
            Debug.LogWarning("PaletteSwitcher has no material. Assign the Full Screen Pass Renderer Feature's material.", this);
            enabled = false;
            return;
        }

        if (index < 0 || index >= palettes.Length || index == current)
            return;

        Palette palette = palettes[index];

        paletteMaterial.SetColor(Color0Id, ToRenderSpace(palette.Darkest));
        paletteMaterial.SetColor(Color1Id, ToRenderSpace(palette.Dark));
        paletteMaterial.SetColor(Color2Id, ToRenderSpace(palette.Light));
        paletteMaterial.SetColor(Color3Id, ToRenderSpace(palette.Lightest));

        current = index;

        if (logSwitches)
            Debug.Log($"Palette {(index + 1) % 10}: {palette.Name}");
    }

    /// <summary>
    /// sRGB hex to whatever space the renderer is actually working in. In gamma space
    /// the parsed value is already correct; in linear space it is not, and skipping
    /// the conversion is what makes a hand-picked palette come out chalky.
    /// </summary>
    private static Color ToRenderSpace(string hex)
    {
        if (!ColorUtility.TryParseHtmlString(hex, out Color color))
            return Color.magenta;   // loud on purpose: a typo in a hex string should not be subtle

        return QualitySettings.activeColorSpace == ColorSpace.Linear ? color.linear : color;
    }
}
