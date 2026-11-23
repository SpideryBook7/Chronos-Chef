var game = UnityEngine.Object.FindFirstObjectByType<ChronosChef.KitchenGame>();
var view = UnityEngine.Object.FindFirstObjectByType<ChronosChef.KitchenView>();
var stage = view.transform.Find("SafeArea/LandscapeFrame/KitchenStage");
var alert = stage.Find("ParadoxBadge").gameObject;
var border = stage.Find("ParadoxBadgeRim").gameObject;
var original = game.Progress;
var isolated = new ChronosChef.KitchenProgress("ChronosChef.AlertQA.");
game.Progress = isolated;
float speed = UnityEngine.Time.timeScale;
try
{
    game.ResetShift();
    if (alert.activeSelf || border.activeSelf) throw new System.Exception("Alert shown during normal play.");
    game.AddAnomaly(100);
    await System.Threading.Tasks.Task.Delay(150);
    if (!alert.activeSelf || !border.activeSelf || game.CloneRemaining <= 0) throw new System.Exception("Alert missing during paradox.");
    UnityEngine.Time.timeScale = 4;
    await System.Threading.Tasks.Task.Delay(1050);
    if (game.CloneRemaining <= 0 || alert.activeSelf || border.activeSelf) throw new System.Exception("Notice lingered after onset.");
    UnityEngine.Time.timeScale = 20;
    await System.Threading.Tasks.Task.Delay(1400);
    if (game.CloneRemaining > 0 || alert.activeSelf || border.activeSelf) throw new System.Exception("Alert did not hide when paradox expired.");
    var result = "PASS: alert and border hidden on fresh shift, visible during active paradox, hidden after expiry while shift continues.";
    System.IO.File.WriteAllText("Tests/alert-lifecycle-result.txt", result);
    return result;
}
finally
{
    UnityEngine.Time.timeScale = speed;
    game.GoToMenu(); isolated.ClearSession(); game.Progress = original; view.ShowHome();
}
