var game = UnityEngine.Object.FindFirstObjectByType<ChronosChef.KitchenGame>();
var original = game.Progress;
string prefix = "ChronosChef.pauseQA." + System.Guid.NewGuid() + ".";
try
{
    game.Progress = new ChronosChef.KitchenProgress(prefix); game.BeginLevel(1); game.Interact(ChronosChef.Station.Supply);
    game.gameObject.SendMessage("OnApplicationPause", true);
    if (!game.Paused || !UnityEngine.AudioListener.pause) throw new System.Exception("Background did not pause play and audio");
    var snapshot = game.Progress.LoadSession(); if (snapshot == null || snapshot.carry != ChronosChef.Food.RawDodo) throw new System.Exception("Background save missing carried food");
    game.gameObject.SendMessage("OnApplicationPause", false);
    if (!game.Paused || UnityEngine.AudioListener.pause) throw new System.Exception("Foreground did not restore audio while keeping the turn safely paused");
    var result = "PASS: application pause saves carried food and pauses play/audio; returning restores audio and requires manual resume.";
    System.IO.File.WriteAllText("Tests/mobile-pause-result.txt", result); return result;
}
finally
{
    UnityEngine.AudioListener.pause = false; game.GoToMenu(); game.Progress.ClearSession(); game.Progress = original; game.GoToMenu();
}
