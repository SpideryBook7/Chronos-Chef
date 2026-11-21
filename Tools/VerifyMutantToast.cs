var game = UnityEngine.Object.FindFirstObjectByType<ChronosChef.KitchenGame>();
var view = UnityEngine.Object.FindFirstObjectByType<ChronosChef.KitchenView>();
var original = game.Progress;
var key = "ChronosChef.ToastQA." + System.Guid.NewGuid() + ".";
var isolated = new ChronosChef.KitchenProgress(key);
game.Progress = isolated;
try
{
    isolated.Complete(1, 0, 1); isolated.Complete(2, 0, 1); game.BeginLevel(3);
    var dish = ChronosChef.Food.Toast;
    if (game.Orders[2] != dish) throw new System.Exception("New level lacks a toast order");
    game.Interact(ChronosChef.Station.Roots); game.Interact(ChronosChef.Station.Cut);
    for (int i = 0; i < 60 && game.IsWorking; i++) await System.Threading.Tasks.Task.Delay(100);
    game.Interact(ChronosChef.Station.Cut); game.Interact(ChronosChef.Station.Assemble);
    if (game.Carry != dish) throw new System.Exception("Toast assembly failed");
    game.SaveCurrentSession(); game.GoToMenu(); if (!game.ResumeSavedShift() || game.Carry != dish) throw new System.Exception("Saved toast lost");
    game.TogglePause(); UnityEngine.GameObject.Find("RecipeHelp").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
    var art = UnityEngine.GameObject.Find("RecipeFinishedDish").GetComponent<UnityEngine.UI.Image>().sprite;
    if (!art || art.name != "MutantToast") throw new System.Exception("Missing illustrated toast recipe");
    UnityEngine.GameObject.Find("CloseRecipes").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
    game.Interact(ChronosChef.Station.Customer2);
    if (game.Served != 1 || game.Credits == 0 || game.Carry != ChronosChef.Food.Empty || !UnityEngine.GameObject.Find("DeliveryReceipt"))
        throw new System.Exception("Delivery did not award the toast or show receipt");
    return "PASS: level 3 toast order, root cutting, assembly, saved dish, recipe illustration and paid delivery.";
}
finally
{
    game.GoToMenu(); isolated.ClearSession(); game.Progress = original; game.GoToMenu(); view.ShowHome();
    foreach (var suffix in new[] { "stars.1", "stars.2", "credits", "unlocked" }) UnityEngine.PlayerPrefs.DeleteKey(key + suffix);
    UnityEngine.PlayerPrefs.Save();
}
