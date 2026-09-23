using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// A "< VALUE >" row. Left/right cycles the options, submit or click steps forward.
public class OptionStepper : Selectable, ISubmitHandler, IPointerClickHandler
{
    [SerializeField] TMP_Text label;
    public string[] options = { };
    public UnityEvent<int> onValueChanged = new();

    public int Value { get; private set; }

    public void SetValueWithoutNotify(int value)
    {
        Value = (value % options.Length + options.Length) % options.Length;
        label.text = $"< {options[Value]} >";
    }

    public override void OnMove(AxisEventData eventData)
    {
        if (eventData.moveDir == MoveDirection.Left) Step(-1);
        else if (eventData.moveDir == MoveDirection.Right) Step(1);
        else base.OnMove(eventData);
    }

    public void OnSubmit(BaseEventData eventData) => Step(1);
    public void OnPointerClick(PointerEventData eventData) => Step(1);

    void Step(int direction)
    {
        SetValueWithoutNotify(Value + direction);
        onValueChanged.Invoke(Value);
    }
}
