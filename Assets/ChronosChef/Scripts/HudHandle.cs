using UnityEngine;
using UnityEngine.EventSystems;

namespace ChronosChef
{
    public sealed class HudHandle : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        RectTransform rect, field;
        Vector2 offset, start;
        internal System.Func<bool> PlacementValid;
        void Awake() { rect = (RectTransform)transform; field = (RectTransform)rect.parent; }
        public void OnBeginDrag(PointerEventData e)
        {
            start = rect.anchoredPosition;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(field,e.position,e.pressEventCamera,out var pointer);
            offset = rect.localPosition - (Vector3)pointer;
        }
        public void OnDrag(PointerEventData e)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(field,e.position,e.pressEventCamera,out var pointer)) return;
            var half = rect.rect.size * .5f + Vector2.one * 8;
            var p = pointer + offset - field.rect.min;
            rect.anchoredPosition = new Vector2(Mathf.Clamp(p.x,half.x,field.rect.width-half.x),Mathf.Clamp(p.y,half.y,field.rect.height-half.y));
        }
        public void OnEndDrag(PointerEventData e) { if (PlacementValid != null && !PlacementValid()) rect.anchoredPosition = start; }
    }
}
