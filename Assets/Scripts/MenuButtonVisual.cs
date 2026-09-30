using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Ink and parchment colours shared by pointer and keyboard selection.</summary>
[RequireComponent(typeof(Button))]
public sealed class MenuButtonVisual : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
    ISelectHandler, IDeselectHandler, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private Image fill;
    [SerializeField] private TMP_Text label;
    [SerializeField] private GameObject frame;
    [SerializeField] private Color ink = new Color32(17, 34, 46, 255);
    [SerializeField] private Color parchment = new Color32(247, 230, 188, 255);
    private bool selected, pressed;
    private Button button;

    private void Awake() => button = GetComponent<Button>();
    private void OnEnable() => Refresh();
    private void OnDisable() { selected = pressed = false; }
    public void OnPointerEnter(PointerEventData e)
    {
        if (button == null || !button.IsInteractable()) return;
        // Pointer and keyboard share one selection, so they cannot highlight different buttons.
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(gameObject, e);
        Refresh();
    }
    public void OnPointerExit(PointerEventData e) { pressed = false; Refresh(); }
    public void OnSelect(BaseEventData e) { selected = true; Refresh(); }
    public void OnDeselect(BaseEventData e) { selected = pressed = false; Refresh(); }
    public void OnPointerDown(PointerEventData e) { pressed = true; Refresh(); }
    public void OnPointerUp(PointerEventData e) { pressed = false; Refresh(); }

    private void Refresh()
    {
        if (fill == null || label == null || frame == null) return;
        bool active = selected && (button == null || button.IsInteractable());
        fill.color = active ? (pressed ? Color.Lerp(ink, Color.white, 0.15f) : ink) : Color.clear;
        label.color = active ? parchment : ink;
        frame.SetActive(active);
    }
}
