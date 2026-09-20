using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SplashController : MonoBehaviour
{
    [Header("Next Scene")]
    [SerializeField] private string nextSceneName = "Level1Graybox";

    [Header("Pre-Roll")]
    [SerializeField] private float preRollSeconds = 0.5f;

    [Header("Phase 1 - Logo")]
    [SerializeField] private Image logoImage;
    [SerializeField] private AudioSource logoSound;
    [SerializeField] private float phase1FadeInSeconds = 1.0f;
    [SerializeField] private float phase1HoldSeconds = 3.5f;
    [SerializeField] private float phase1FadeOutSeconds = 1.0f;
    [SerializeField] private float logoStartScale = 1.0f;
    [SerializeField] private float logoEndScale = 1.08f;

    [SerializeField] private float gapSeconds = 0.5f;

    [Header("Phase 2 - Trademark Card")]
    [SerializeField] private CanvasGroup trademarkTextRoot;
    [SerializeField] private CanvasGroup trademarkLogosRoot;
    [SerializeField] private AudioSource trademarkSound;
    [SerializeField] private float phase2FadeInSeconds = 1.0f;
    [SerializeField] private float phase2HoldSeconds = 4.0f;
    [SerializeField] private float phase2FadeOutSeconds = 1.0f;

    private void Awake()
    {

        if (logoImage != null)
        {
            SetImageAlpha(logoImage, 0f);
            logoImage.rectTransform.localScale = Vector3.one * logoStartScale;
        }
        ResetCanvasGroup(trademarkTextRoot);
        ResetCanvasGroup(trademarkLogosRoot);
    }

    private static void ResetCanvasGroup(CanvasGroup group)
    {
        if (group == null) return;
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;
    }

    private void Start()
    {
        StartCoroutine(RunSplash());
    }

    private IEnumerator RunSplash()
    {
        if (preRollSeconds > 0f)
        {
            yield return new WaitForSecondsRealtime(preRollSeconds);
        }

        if (logoSound != null && logoSound.clip != null)
        {
            logoSound.Play();
        }

        if (logoImage != null)
        {
            float phase1Total = phase1FadeInSeconds + phase1HoldSeconds + phase1FadeOutSeconds;
            StartCoroutine(ScaleRect(logoImage.rectTransform, logoStartScale, logoEndScale, phase1Total));
            yield return FadeImage(logoImage, 0f, 1f, phase1FadeInSeconds);
        }

        yield return new WaitForSecondsRealtime(phase1HoldSeconds);

        if (logoImage != null)
        {
            yield return FadeImage(logoImage, 1f, 0f, phase1FadeOutSeconds);
        }

        yield return new WaitForSecondsRealtime(gapSeconds);

        bool hasTrademark = trademarkTextRoot != null || trademarkLogosRoot != null;
        if (hasTrademark)
        {
            yield return FadeCanvasGroups(trademarkTextRoot, trademarkLogosRoot, 0f, 1f, phase2FadeInSeconds);

            if (trademarkSound != null && trademarkSound.clip != null)
            {
                trademarkSound.Play();
            }

            yield return new WaitForSecondsRealtime(phase2HoldSeconds);
            yield return FadeCanvasGroups(trademarkTextRoot, trademarkLogosRoot, 1f, 0f, phase2FadeOutSeconds);
        }

        AsyncOperation op = SceneManager.LoadSceneAsync(nextSceneName);
        while (op != null && !op.isDone)
        {
            yield return null;
        }
    }

    private IEnumerator FadeCanvasGroups(CanvasGroup a, CanvasGroup b, float from, float to, float seconds)
    {
        if (seconds <= 0f)
        {
            if (a != null) a.alpha = to;
            if (b != null) b.alpha = to;
            yield break;
        }

        float t = 0f;
        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;
            float v = Mathf.Lerp(from, to, t / seconds);
            if (a != null) a.alpha = v;
            if (b != null) b.alpha = v;
            yield return null;
        }
        if (a != null) a.alpha = to;
        if (b != null) b.alpha = to;
    }

    private static IEnumerator ScaleRect(RectTransform rect, float from, float to, float seconds)
    {
        if (seconds <= 0f)
        {
            rect.localScale = Vector3.one * to;
            yield break;
        }

        float t = 0f;
        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;
            rect.localScale = Vector3.one * Mathf.Lerp(from, to, t / seconds);
            yield return null;
        }
        rect.localScale = Vector3.one * to;
    }

    private static IEnumerator FadeImage(Image image, float from, float to, float seconds)
    {
        if (seconds <= 0f)
        {
            SetImageAlpha(image, to);
            yield break;
        }

        float t = 0f;
        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;
            SetImageAlpha(image, Mathf.Lerp(from, to, t / seconds));
            yield return null;
        }
        SetImageAlpha(image, to);
    }

    private static void SetImageAlpha(Image image, float alpha)
    {
        Color c = image.color;
        c.a = alpha;
        image.color = c;
    }
}
