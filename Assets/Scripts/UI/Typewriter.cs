using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class Typewriter : MonoBehaviour
{
    [SerializeField] TMP_Text tmp;
    [SerializeField] InputActionReference submit;
    [SerializeField] AudioSource blip;
    [SerializeField] float charsPerSecond = 24f;
    [SerializeField] float punctuationPause = 0.25f;
    [SerializeField, Min(1), Tooltip("How many times faster text appears with FAST TEXT on in Settings.")] float fastTextSpeed = 3f;
    [SerializeField, Min(1)] int blipEvery = 3;
    [SerializeField] Vector2 blipPitch = new Vector2(0.92f, 1.08f);

    [Header("Page end")]
    [SerializeField, Tooltip("Blinks while a full page waits for a press.")] GameObject pageEndIcon;
    [SerializeField] float blinkSeconds = 0.4f;

    [Header("Standalone playback")]
    [SerializeField, Tooltip("Plays displayText on its own at Start. Turn this off once a CutsceneDirector drives this typewriter.")]
    bool playOnStart = true;
    [SerializeField, TextArea(3, 10)] string displayText = "";

    // Raised once the final page has been dismissed.
    public event Action Finished;
    public bool IsPlaying { get; private set; }

    bool Pressed => submit.action.WasPressedThisFrame();

    void Awake() => tmp.overflowMode = TextOverflowModes.Page;
    void OnEnable() => submit.action.Enable();

    void Start()
    {
        if (playOnStart) Play(displayText);
    }

    public Coroutine Play(string text)
    {
        StopAllCoroutines();
        return StartCoroutine(Run(text));
    }

    public void Clear()
    {
        StopAllCoroutines();
        IsPlaying = false;
        tmp.text = "";
        tmp.maxVisibleCharacters = 0;
        ShowIcon(false);
    }

    IEnumerator Run(string text)
    {
        IsPlaying = true;
        ShowIcon(false);

        tmp.text = text;              
        tmp.pageToDisplay = 1;
        tmp.ForceMeshUpdate();
        tmp.maxVisibleCharacters = 0;

        for (int page = 0; page < tmp.textInfo.pageCount; page++)
        {
            tmp.pageToDisplay = page + 1; // TMP counts pages from 1
            TMP_PageInfo info = tmp.textInfo.pageInfo[page];
            yield return Reveal(info.firstCharacterIndex, info.lastCharacterIndex + 1);

            yield return null;          // one press can't do two things
            yield return WaitForPress();
        }

        IsPlaying = false;
        Finished?.Invoke();
    }

    IEnumerator Reveal(int from, int to)
    {
        int shown = from;
        float wait = 0f;
        float speed = SettingsWindow.FastText ? fastTextSpeed : 1f;
        tmp.maxVisibleCharacters = shown;

        while (shown < to)
        {
            yield return null;

            if (Pressed)
            {
                tmp.maxVisibleCharacters = to;
                yield break;
            }

            wait -= Time.unscaledDeltaTime;
            while (wait <= 0f && shown < to)
            {
                char c = tmp.textInfo.characterInfo[shown].character;
                shown++;

                if (c != ' ' && shown % blipEvery == 0) Blip();

                wait += (".,!?".IndexOf(c) >= 0 ? punctuationPause : 1f / charsPerSecond) / speed;
            }

            tmp.maxVisibleCharacters = shown;
        }
    }

    IEnumerator WaitForPress()
    {
        float t = 0f;

        while (!Pressed)
        {
            t += Time.unscaledDeltaTime;
            ShowIcon(t % (blinkSeconds * 2f) < blinkSeconds);
            yield return null;
        }

        ShowIcon(false);
    }

    void Blip()
    {
        if (blip == null) return;

        blip.pitch = UnityEngine.Random.Range(blipPitch.x, blipPitch.y);
        blip.Play();
    }

    void ShowIcon(bool on)
    {
        if (pageEndIcon != null) pageEndIcon.SetActive(on);
    }
}
