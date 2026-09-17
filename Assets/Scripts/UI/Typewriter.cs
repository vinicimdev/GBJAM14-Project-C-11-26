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
    [SerializeField] string displayText = "";

    bool Pressed => submit.action.WasPressedThisFrame();

    void Awake() => tmp.overflowMode = TextOverflowModes.Page;
    void OnEnable() => submit.action.Enable();
    void Start() => Play(tmp.text);

    public Coroutine Play(string text)
    {
        StopAllCoroutines();
        return StartCoroutine(Run(text));
    }

    IEnumerator Run(string text)
    {
        tmp.text = displayText;
        tmp.pageToDisplay = 1;
        tmp.ForceMeshUpdate();

        for (int page = 0; page < tmp.textInfo.pageCount; page++)
        {
            tmp.pageToDisplay = page + 1; // TMP counts pages from 1
            TMP_PageInfo info = tmp.textInfo.pageInfo[page];
            yield return Reveal(info.firstCharacterIndex, info.lastCharacterIndex + 1);

            yield return null; // wait a frame so one press can't do two things
            while (!Pressed) yield return null;
        }
    }

    IEnumerator Reveal(int from, int to)
    {
        int shown = from;
        float wait = 0f;
        tmp.maxVisibleCharacters = shown;

        while (shown < to)
        {
            yield return null; // same reason as above

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