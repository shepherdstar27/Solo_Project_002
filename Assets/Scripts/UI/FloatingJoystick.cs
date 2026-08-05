using UnityEngine;
using UnityEngine.EventSystems;

public class FloatingJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [SerializeField] private RectTransform RectTransform_Background;
    [SerializeField] private RectTransform RectTransform_Handle;

    [SerializeField] private float _handleRange = 100f;   // 핸들 최대 이동 반경(px)

    public Vector2 Direction { get; private set; }
    public bool IsActive { get; private set; }

    private void Start()
    {
        RectTransform_Background.gameObject.SetActive(false);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        RectTransform_Background.gameObject.SetActive(true);
        RectTransform_Background.position = eventData.position;
        RectTransform_Handle.anchoredPosition = Vector2.zero;
        IsActive = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 offset = eventData.position - (Vector2)RectTransform_Background.position;
        Vector2 clamped = Vector2.ClampMagnitude(offset, _handleRange);

        RectTransform_Handle.anchoredPosition = clamped;

        if (clamped.sqrMagnitude > 0.01f)
        {
            Direction = clamped.normalized;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        RectTransform_Background.gameObject.SetActive(false);
        RectTransform_Handle.anchoredPosition = Vector2.zero;
        Direction = Vector2.zero;
        IsActive = false;
    }
}