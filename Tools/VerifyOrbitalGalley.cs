var game = UnityEngine.Object.FindFirstObjectByType<ChronosChef.KitchenGame>();
var view = UnityEngine.Object.FindFirstObjectByType<ChronosChef.KitchenView>();
var campaign = game.Progress;
var isolated = new ChronosChef.KitchenProgress("ChronosChef.GalleyQA.");
string saved;
async System.Threading.Tasks.Task Wait(System.Func<bool> done,string task)
{
    float end=UnityEngine.Time.realtimeSinceStartup + 12;
    while (!done())
    {
        if (UnityEngine.Time.realtimeSinceStartup > end) throw new System.Exception(task);
        await System.Threading.Tasks.Task.Delay(20);
    }
}
void Tap(string name)
{
    var target = UnityEngine.GameObject.Find(name);
    var events = UnityEngine.EventSystems.EventSystem.current;
    var touch = new UnityEngine.EventSystems.PointerEventData(events) {
        position = UnityEngine.RectTransformUtility.WorldToScreenPoint(null,target.transform.position)
    };
    var hits = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
    events.RaycastAll(touch,hits);
    if (hits.Count==0 || hits[0].gameObject.GetComponentInParent<UnityEngine.UI.Button>()?.gameObject!=target)
        throw new System.Exception("Tap intercepted: " + name + " by " + (hits.Count==0 ? "nothing" : hits[0].gameObject.name));
    UnityEngine.EventSystems.ExecuteEvents.Execute(target,touch,UnityEngine.EventSystems.ExecuteEvents.pointerClickHandler);
}
try
{
    game.Progress = isolated;
    game.BeginLevel(1); game.Interact(ChronosChef.Station.Roots); game.GoToMenu(); view.ShowHome();
    saved=UnityEngine.JsonUtility.ToJson(isolated.LoadSession());
    await System.Threading.Tasks.Task.Delay(180);
    Tap("TryOrbitalGalley");
    await Wait(()=>game.Running && game.Progress!=isolated,"Trial did not open");
    await System.Threading.Tasks.Task.Delay(350);
    var room=UnityEngine.Object.FindFirstObjectByType<ChronosChef.OrbitalGalley>();
    if (!room || !room.Image.IsCreated() || room.GetComponentInChildren<UnityEngine.Camera>().targetTexture!=room.Image)
        throw new System.Exception("3D camera not rendering");
    var camera = room.GetComponentInChildren<UnityEngine.Camera>();
    var cameraPosition = camera.transform.position;
    var cameraRotation = camera.transform.rotation;
    if (camera.orthographic || game.Facing != UnityEngine.Vector2.up || UnityEngine.Mathf.Abs(camera.transform.localPosition.x) > .001f)
        throw new System.Exception("Trial did not open with a centered rear perspective");
    var actor=room.transform.Find("Ares");
    var start=actor.localPosition;
    game.SetStick(UnityEngine.Vector2.left);
    await Wait(()=>UnityEngine.Vector3.Distance(actor.localPosition,start)>.3f,"3D chef stationary");
    game.SetStick(UnityEngine.Vector2.zero);
    Tap("DodoSupply"); await Wait(()=>game.Carry==ChronosChef.Food.RawDodo,"Supply");
    Tap("CutStation"); await Wait(()=>game.PrepFood==ChronosChef.Food.ChoppedDodo,"Prep");
    Tap("CutStation"); await Wait(()=>game.Carry==ChronosChef.Food.ChoppedDodo,"Collect");
    Tap("PlasmaStove"); await Wait(()=>game.StoveFood==ChronosChef.Food.CookedDodo,"Cook");
    Tap("PlasmaStove"); await Wait(()=>game.Carry==ChronosChef.Food.CookedDodo,"Plate");
    Tap("Assembly"); await Wait(()=>game.Carry==ChronosChef.Food.Burger,"Assemble");
    Tap("ServeCustomer0"); await Wait(()=>game.Served==1,"Serve");
    if (camera.transform.position != cameraPosition || camera.transform.rotation != cameraRotation)
        throw new System.Exception("Camera moved with chef");
    Tap("Pause"); await Wait(()=>game.Paused,"Pause");
    await System.Threading.Tasks.Task.Delay(180);
    Tap("PauseHub"); await Wait(()=>!room.gameObject.activeSelf,"Exit");
    if(game.Progress!=isolated || UnityEngine.JsonUtility.ToJson(isolated.LoadSession())!=saved)
        throw new System.Exception("Campaign save changed during trial");
    view.ShowHome();
    await System.Threading.Tasks.Task.Delay(220);
    Tap("TryOrbitalGalley"); await Wait(()=>room.gameObject.activeSelf && game.Running,"Second entry");
    return "PASS: home button, real camera and actor movement, six station taps, burger delivered, pause/exit, campaign save preserved, repeat entry.";
}
finally
{
    game.GoToMenu(); isolated.ClearSession(); game.Progress=campaign; view.ShowHome();
}
