using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Goes on a window's close button. Up/down scroll the text one line at a time.
public class ScrollText : MonoBehaviour, IMoveHandler
{
    [SerializeField] TMP_Text text;
    [SerializeField] RectTransform scrollBar;
    [SerializeField, Tooltip("Child of scrollBar, anchored to its top edge.")] RectTransform thumb;
    [SerializeField] float minThumbHeight = 6f;

    readonly List<int> lineStarts = new();
    int visibleLines;
    bool refresh;

    int TopLine => lineStarts.Count - 1;

    void Awake()
    {
        // Up/down belong to the text now, so the button must not navigate away.
        GetComponent<Selectable>().navigation = new Navigation { mode = Navigation.Mode.None };
    }

    void OnEnable()
    {
        lineStarts.Clear();
        lineStarts.Add(0);
        text.firstVisibleCharacter = 0;
        refresh = true;
    }

    public void OnMove(AxisEventData eventData)
    {
        TMP_TextInfo info = text.textInfo;

        // Overflowing means there are still lines below the box.
        if (eventData.moveDir == MoveDirection.Down && text.isTextOverflowing && info.lineCount > 1)
            lineStarts.Add(info.lineInfo[1].firstCharacterIndex);
        else if (eventData.moveDir == MoveDirection.Up && TopLine > 0)
            lineStarts.RemoveAt(TopLine);
        else
            return;

        text.firstVisibleCharacter = lineStarts[TopLine];
        refresh = true;
    }

    void LateUpdate()
    {
        TMP_TextInfo info = text.textInfo;

        // Wait until TMP has laid the text out from the current top line.
        if (!refresh || info.lineCount == 0 || info.lineInfo[0].firstCharacterIndex != lineStarts[TopLine]) return;
        refresh = false;

        if (text.isTextOverflowing)
        {
            visibleLines = info.characterInfo[text.firstOverflowCharacterIndex].lineNumber;
            text.maxVisibleLines = visibleLines;
        }

        int totalLines = TopLine + info.lineCount;
        bool scrolls = visibleLines > 0 && totalLines > visibleLines;
        scrollBar.gameObject.SetActive(scrolls);
        if (!scrolls) return;

        // Whole pixels only, or the thumb shimmers at 160x144.
        float track = scrollBar.rect.height;
        float height = Mathf.Max(minThumbHeight, Mathf.Round(track * visibleLines / totalLines));
        float offset = Mathf.Round((track - height) * Mathf.Clamp01((float)TopLine / (totalLines - visibleLines)));
        thumb.anchoredPosition = new Vector2(thumb.anchoredPosition.x, -offset);
        thumb.sizeDelta = new Vector2(thumb.sizeDelta.x, height);
    }
}
