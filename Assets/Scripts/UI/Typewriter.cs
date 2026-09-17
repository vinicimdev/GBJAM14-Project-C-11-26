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
    [SerializeField, Min(1)] int blipEvery = 3;

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
    }

    IEnumerator Run(string text)
    {
        IsPlaying = true;

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
            while (!Pressed) yield return null;
        }

        IsPlaying = false;
        Finished?.Invoke();
    }

    IEnumerator Reveal(int from, int to)
    {
        int shown = from;
        float wait = 0f;
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

                if (blip != null && c != ' ' && shown % blipEvery == 0)
                    blip.Play();

                wait += ".,!?".IndexOf(c) >= 0 ? punctuationPause : 1f / charsPerSecond;
            }

            tmp.maxVisibleCharacters = shown;
        }
    }
}