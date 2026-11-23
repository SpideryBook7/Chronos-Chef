var stage = UnityEngine.GameObject.Find("KitchenStage").transform;
var view = UnityEngine.Object.FindFirstObjectByType<ChronosChef.KitchenView>();
view.ShowHome();
UnityEngine.GameObject.Find("HomeOptions").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
UnityEngine.GameObject.Find("SettingsRecipes").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
// Graphic depth and hit ordering are assigned during the first rendered frame.
await System.Threading.Tasks.Task.Delay(150);
var hero = UnityEngine.GameObject.Find("RecipeFinishedDish").GetComponentInChildren<ChronosChef.KitchenModelGraphic>();
if (!hero) throw new System.Exception("Recipe hero has no 3D model");
var results = new System.Collections.Generic.List<string>();
try
{
    foreach (ChronosChef.Food food in System.Enum.GetValues(typeof(ChronosChef.Food)))
    {
        if (food == ChronosChef.Food.Empty) continue;
        hero.Show(food); hero.Pose(-18);
        UnityEngine.Canvas.ForceUpdateCanvases();
        var mesh = hero.canvasRenderer.GetMesh();
        if (!mesh || mesh.vertexCount < 30 || mesh.bounds.size.x <= 0 || mesh.bounds.size.y <= 0) throw new System.Exception("Empty model: " + food);
        results.Add(food + ":" + mesh.GetIndexCount(0)/3);
    }
    var oldVertices = hero.canvasRenderer.GetMesh().vertices;
    var events = UnityEngine.EventSystems.EventSystem.current;
    var pointer = new UnityEngine.EventSystems.PointerEventData(events) {
        position = UnityEngine.RectTransformUtility.WorldToScreenPoint(null, hero.rectTransform.TransformPoint(hero.rectTransform.rect.center)),
        delta = new UnityEngine.Vector2(90,0)
    };
    var hits = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
    events.RaycastAll(pointer, hits);
    var drag = hits.Count > 0 ? UnityEngine.EventSystems.ExecuteEvents.GetEventHandler<UnityEngine.EventSystems.IDragHandler>(hits[0].gameObject) : null;
    if (drag != hero.gameObject) throw new System.Exception("Recipe plate drag missed at " + pointer.position + ", first hit: " + (hits.Count == 0 ? "none" : hits[0].gameObject.name) + ", depth: " + hero.depth);
    UnityEngine.EventSystems.ExecuteEvents.Execute(drag, pointer, UnityEngine.EventSystems.ExecuteEvents.dragHandler);
    UnityEngine.Canvas.ForceUpdateCanvases();
    var turned = hero.canvasRenderer.GetMesh().vertices;
    if (oldVertices.Length == turned.Length && UnityEngine.Vector3.Distance(oldVertices[0],turned[0]) < .01f) throw new System.Exception("Drag did not rotate geometry");
    if (stage.Find("CustomerWindow1/Visitor_1_1/IllustratedCharacter/Model3D") == null) throw new System.Exception("Missing articulated robot");
    return "PASS: drag changes mesh projection, robot present; visible triangles " + string.Join(", ",results);
}
finally { view.ShowHome(); }
