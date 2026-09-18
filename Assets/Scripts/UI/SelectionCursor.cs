using UnityEngine;
using UnityEngine.EventSystems;

public class SelectionCursor : MonoBehaviour
{
    [SerializeField] RectTransform cursor;
    [SerializeField] Vector2 offset = new Vector2(-10f, 0f);

    void LateUpdate()
    {
        GameObject selected = EventSystem.current.currentSelectedGameObject;
        cursor.gameObject.SetActive(selected != null);
        if (selected == null) return;

        RectTransform target = (RectTransform)selected.transform;
        cursor.position = target.TransformPoint(target.rect.xMin, target.rect.center.y, 0f);

        // Whole pixels only, or the arrow shimmers at 160x144.
        Vector2 p = cursor.anchoredPosition + offset;
        cursor.anchoredPosition = new Vector2(Mathf.Round(p.x), Mathf.Round(p.y));
    }
}
