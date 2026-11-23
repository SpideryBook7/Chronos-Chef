using UnityEngine;
using UnityEngine.EventSystems;

namespace ChronosChef
{
    public sealed class ChefStick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler, IInitializePotentialDragHandler
    {
        KitchenGame game;
        RectTransform knob, pad;
        int? owner;
        const float Radius = 43f;

        public void Bind(KitchenGame kitchen, RectTransform thumb)
        {
            game = kitchen; knob = thumb; pad = (RectTransform)transform;
        }
        public void OnInitializePotentialDrag(PointerEventData e) => e.useDragThreshold = false;
        public void OnPointerDown(PointerEventData e)
        {
            if (owner.HasValue || e.button != PointerEventData.InputButton.Left) return;
            owner = e.pointerId;
            OnDrag(e);
        }
        public void OnDrag(PointerEventData e)
        {
            if (owner != e.pointerId || !RectTransformUtility.ScreenPointToLocalPointInRectangle(pad, e.position, e.pressEventCamera, out var local)) return;
            var axis = Vector2.ClampMagnitude(local / Radius, 1);
            knob.anchoredPosition = axis * Radius;
            float strength = Mathf.InverseLerp(.15f, 1, axis.magnitude);
            game.SetStick(axis.normalized * strength);
        }
        public void OnPointerUp(PointerEventData e) { if (owner == e.pointerId) Release(); }
        void OnDisable() => Release();
        void Release()
        {
            owner = null;
            if (knob) knob.anchoredPosition = Vector2.zero;
            if (game) game.SetStick(Vector2.zero);
        }
    }
}
