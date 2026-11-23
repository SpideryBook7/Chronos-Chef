var g = UnityEngine.Object.FindFirstObjectByType<ChronosChef.KitchenGame>();
var view = UnityEngine.Object.FindFirstObjectByType<ChronosChef.KitchenView>();
var original = g.Progress;
var isolated = new ChronosChef.KitchenProgress("ChronosChef.DriveQA." + System.Guid.NewGuid() + ".");
var events = UnityEngine.EventSystems.EventSystem.current;
var touch = new UnityEngine.EventSystems.PointerEventData(events) { pointerId = 11 };
// Route the test device to the player even when the unattended Editor is in the background.
var testSettings = UnityEngine.InputSystem.InputSystem.settings;
var savedBackground = testSettings.backgroundBehavior;
var savedEditorRouting = testSettings.editorInputBehaviorInPlayMode;
testSettings.backgroundBehavior = UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;
testSettings.editorInputBehaviorInPlayMode = UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
var previousKeyboard = UnityEngine.InputSystem.Keyboard.current;
var keyboard = UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Keyboard>();
g.Progress = isolated;
try
{
    g.BeginLevel(1);
    await System.Threading.Tasks.Task.Delay(160);
    UnityEngine.InputSystem.InputSystem.EnableDevice(keyboard);
    var pad = UnityEngine.GameObject.Find("MovementZone").GetComponent<UnityEngine.RectTransform>();
    var visual = pad.parent.Find("PilotStick");
    if (visual.gameObject.activeSelf || pad.parent.Find("ChefAction")) throw new System.Exception("Unexpected fixed control");
    var origin = new UnityEngine.Vector2(-80,-150);
    var chef = UnityEngine.GameObject.Find("ChefAres").GetComponentInChildren<ChronosChef.KitchenModelGraphic>();
    if (!chef || chef.canvasRenderer.GetMesh().vertexCount < 100) throw new System.Exception("Chef has no rendered rig");
    UnityEngine.GameObject Hit(UnityEngine.EventSystems.PointerEventData e)
    {
        var hits = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
        events.RaycastAll(e,hits);
        if (hits.Count == 0) throw new System.Exception("No UI hit at " + e.position);
        return hits[0].gameObject;
    }
    void Aim(UnityEngine.Vector2 direction)
    {
        touch.position = UnityEngine.RectTransformUtility.WorldToScreenPoint(null, pad.TransformPoint(origin + direction*43));
        UnityEngine.EventSystems.ExecuteEvents.Execute(pad.gameObject,touch,UnityEngine.EventSystems.ExecuteEvents.dragHandler);
    }
    void Press()
    {
        touch.position = UnityEngine.RectTransformUtility.WorldToScreenPoint(null,pad.TransformPoint(origin));
        var hit = Hit(touch);
        if (hit != pad.gameObject) throw new System.Exception("Thumb pad obstructed by " + hit.name);
        UnityEngine.EventSystems.ExecuteEvents.Execute(hit,touch,UnityEngine.EventSystems.ExecuteEvents.pointerDownHandler);
        if (!visual.gameObject.activeSelf || UnityEngine.Vector2.Distance(touch.position,UnityEngine.RectTransformUtility.WorldToScreenPoint(null,visual.position)) > 1)
            throw new System.Exception("Joystick did not open at touch");
    }
    void Release() => UnityEngine.EventSystems.ExecuteEvents.Execute(pad.gameObject,touch,UnityEngine.EventSystems.ExecuteEvents.pointerUpHandler);
    async System.Threading.Tasks.Task Walk(float x, float y)
    {
        Press();
        var target = new UnityEngine.Vector2(x,y);
        float deadline = UnityEngine.Time.realtimeSinceStartup + 7;
        while ((g.ChefPosition-target).magnitude > 10)
        {
            if (UnityEngine.Time.realtimeSinceStartup > deadline) throw new System.Exception("Walk blocked at " + g.ChefPosition + " en route to " + target);
            var delta = target - g.ChefPosition;
            Aim(delta.normalized*UnityEngine.Mathf.Clamp(delta.magnitude/46,.34f,1));
            await System.Threading.Tasks.Task.Delay(16);
            if (!ChronosChef.KitchenRules.IsFloorPositionClear(g.ChefPosition)) throw new System.Exception("Chef entered furniture");
        }
        Release();
    }
    async System.Threading.Tasks.Task Act(ChronosChef.Station expected)
    {
        var names = new[] { "DodoSupply","CutStation","PlasmaStove","Assembly","QuantumBin","ServeCustomer0","ServeCustomer1","ServeCustomer2","RootSupply" };
        var button = UnityEngine.GameObject.Find(names[(int)expected]).GetComponent<UnityEngine.UI.Button>();
        var action = new UnityEngine.EventSystems.PointerEventData(events) {
            pointerId = 12, position = UnityEngine.RectTransformUtility.WorldToScreenPoint(null,button.transform.position)
        };
        if (g.NearbyStation != expected || !button.interactable || Hit(action).GetComponentInParent<UnityEngine.UI.Button>() != button)
            throw new System.Exception("Action obstructed or wrong target at " + g.ChefPosition + ": " + g.NearbyStation);
        UnityEngine.EventSystems.ExecuteEvents.Execute(button.gameObject,action,UnityEngine.EventSystems.ExecuteEvents.pointerClickHandler);
        await Until(() => !g.HasDestination,"Station interaction timed out");
    }
    var start = g.ChefPosition;
    keyboard.MakeCurrent();
    UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.A));
    async System.Threading.Tasks.Task Until(System.Func<bool> predicate, string failure)
    {
        float deadline = UnityEngine.Time.realtimeSinceStartup + 4;
        while (!predicate())
        {
            if (UnityEngine.Time.realtimeSinceStartup > deadline) throw new System.Exception(failure);
            await System.Threading.Tasks.Task.Delay(16);
        }
    }
    await Until(() => g.ChefPosition.x < start.x - 2, "Keyboard did not drive the chef");
    UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState());
    await System.Threading.Tasks.Task.Delay(60);
    Press(); Aim(UnityEngine.Vector2.up);
    await System.Threading.Tasks.Task.Delay(800);
    Release();
    if (g.ChefPosition.y <= 360 || g.ChefPosition.y > 423) throw new System.Exception("Cutting table collision failed");
    g.SelectStation((int)ChronosChef.Station.Assemble);
    await Walk(750,325);
    if (g.HasDestination) throw new System.Exception("Thumb did not take over route");
    await Walk(366,325); await Walk(366,405);
    Press(); await Act(ChronosChef.Station.Supply); Release();
    if (g.Carry != ChronosChef.Food.RawDodo) throw new System.Exception("Pick-up failed");
    await Walk(678,404);
    UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.E));
    await Until(() => g.IsWorking, "Keyboard action did not start cutting");
    UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState());
    await System.Threading.Tasks.Task.Delay(1780);
    await Act(ChronosChef.Station.Cut);
    await Walk(925,403);
    await Act(ChronosChef.Station.Stove);
    await System.Threading.Tasks.Task.Delay(5250);
    await Act(ChronosChef.Station.Stove);
    await Walk(1237,405);
    await Act(ChronosChef.Station.Assemble);
    if (g.Carry != ChronosChef.Food.Burger) throw new System.Exception("Assembly failed");
    await Walk(524,405); await Walk(524,481);
    await Act(ChronosChef.Station.Customer0);
    if (g.Served != 1 || g.Carry != ChronosChef.Food.Empty) throw new System.Exception("Contextual delivery failed");
    var savedPoint = g.ChefPosition;
    g.TogglePause(); g.GoToMenu();
    if (!g.ResumeSavedShift() || !g.Paused || UnityEngine.Vector2.Distance(g.ChefPosition,savedPoint) > .01f)
        throw new System.Exception("Continuous position lost on resume");
    return "PASS: keyboard movement/action, touch hit targets, collision, takeover, manual burger from cooler to customer, and exact pause/restore position.";
}
finally
{
    UnityEngine.InputSystem.InputSystem.RemoveDevice(keyboard);
    testSettings.backgroundBehavior = savedBackground;
    testSettings.editorInputBehaviorInPlayMode = savedEditorRouting;
    if (previousKeyboard != null && previousKeyboard.added) previousKeyboard.MakeCurrent();
    g.GoToMenu(); isolated.ClearSession(); g.Progress = original; view.ShowHome();
}
