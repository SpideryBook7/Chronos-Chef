var g = UnityEngine.Object.FindFirstObjectByType<ChronosChef.KitchenGame>();
var view = UnityEngine.Object.FindFirstObjectByType<ChronosChef.KitchenView>();
var original = g.Progress;
var isolated = new ChronosChef.KitchenProgress("ChronosChef.ClockQA." + System.Guid.NewGuid() + ".");
g.Progress = isolated;
try
{
    g.BeginLevel(1);
    g.Interact(ChronosChef.Station.Roots); g.Interact(ChronosChef.Station.Roots); g.Interact(ChronosChef.Station.Stove);
    if (!g.BorrowTime() || g.BorrowTime() || g.Anomaly != 25) throw new System.Exception("Ability cost or cooldown failed");
    var before = g.Remaining;
    var patience = g.Patience[0];
    var cook = g.CookTime;
    await System.Threading.Tasks.Task.Delay(600);
    if (g.Remaining != before || g.Patience[0] != patience || g.CookTime <= cook) throw new System.Exception("Expected frozen shift/patience and a live stove");
    g.TogglePause(); var saved = isolated.LoadSession(); g.GoToMenu();
    if (!g.ResumeSavedShift() || !g.Paused || g.BorrowRemaining != saved.borrowRemaining || g.BorrowCooldown != saved.borrowCooldown) throw new System.Exception("Ability changed on recovery");
    return "PASS: borrowed time costs anomaly, freezes shift and patience, keeps cooking, prevents double use and survives recovery.";
}
finally { g.GoToMenu(); isolated.ClearSession(); g.Progress = original; g.GoToMenu(); view.ShowHome(); }
