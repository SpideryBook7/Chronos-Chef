var names = new[] { "DodoSupply", "CutStation", "PlasmaStove", "Assembly", "QuantumBin", "ServeCustomer0", "ServeCustomer1", "ServeCustomer2", "Pause" };
var verified = new System.Collections.Generic.List<string>();
UnityEngine.Canvas.ForceUpdateCanvases();
foreach (var name in names)
{
    var button = UnityEngine.GameObject.Find(name).GetComponent<UnityEngine.UI.Button>();
    var rect = button.GetComponent<UnityEngine.RectTransform>();
    var point = UnityEngine.RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
    var pointer = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current) { position = point };
    var hits = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
    UnityEngine.EventSystems.EventSystem.current.RaycastAll(pointer, hits);
    if (hits.Count == 0 || hits[0].gameObject.GetComponentInParent<UnityEngine.UI.Button>() != button)
        throw new System.Exception("Touch target occluded: " + name);
    verified.Add(name);
}
System.IO.File.WriteAllText("Tests/touch-targets-result.txt", "PASS: EventSystem raycasts reach all nine controls at 1600x900.\n" + string.Join(", ", verified));
return verified;
