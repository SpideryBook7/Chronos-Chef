var names = new[] { "HomePlay", "HomeContinue", "HomeTraining", "HomeOptions", "HomeCredits" };
UnityEngine.Canvas.ForceUpdateCanvases();
int tested = 0;
foreach (var name in names)
{
    var control = UnityEngine.GameObject.Find(name); if (!control) continue;
    var button = control.GetComponent<UnityEngine.UI.Button>(); var rect = control.GetComponent<UnityEngine.RectTransform>();
    var r = rect.rect;
    foreach (var local in new[] { r.center, new UnityEngine.Vector2(r.xMin + 12, r.center.y), new UnityEngine.Vector2(r.xMax - 12, r.center.y) })
    {
        var p = UnityEngine.RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(local));
        if (p.x < 0 || p.y < 0 || p.x > UnityEngine.Screen.width || p.y > UnityEngine.Screen.height) throw new System.Exception("Off-screen menu control " + name);
        var pointer = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current) { position = p };
        var hits = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>(); UnityEngine.EventSystems.EventSystem.current.RaycastAll(pointer, hits);
        if (hits.Count == 0 || hits[0].gameObject.GetComponentInParent<UnityEngine.UI.Button>() != button) throw new System.Exception("Menu control blocked: " + name);
        tested++;
    }
}
return "PASS: " + tested + " menu touch points at " + UnityEngine.Screen.width + "x" + UnityEngine.Screen.height;
