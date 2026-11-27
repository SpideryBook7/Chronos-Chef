using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ChronosChef;
using UnityEditor;
using UnityEngine;

// Real, timed turns using the same station inputs as the player. No forced score or time.
public static class ChronosBetaPlaytest
{
    static KitchenGame game;
    static KitchenView view;
    static KitchenProgress original;
    static string prefix;
    static int phase, level;
    static Food plannedDish;
    static double deadline;
    static float originalSpeed;
    static readonly List<string> results = new List<string>();
    public static string Status = "Not run";
    public static void Start()
    {
        if (!EditorApplication.isPlaying) throw new Exception("Enter Play Mode first.");
        game = UnityEngine.Object.FindFirstObjectByType<KitchenGame>(); view = UnityEngine.Object.FindFirstObjectByType<KitchenView>();
        original = game.Progress; prefix = "ChronosChef.betaQA." + Guid.NewGuid() + ".";
        game.Progress = new KitchenProgress(prefix); game.GoToMenu(); view.ShowHome();
        phase = level = 0; results.Clear(); originalSpeed = Time.timeScale; Time.timeScale = 20;
        deadline = EditorApplication.timeSinceStartup + 600; Status = "Testing home and tutorial";
        EditorApplication.update -= Tick; EditorApplication.update += Tick; Click("HomePlay");
    }
    static void Click(string name)
    {
        var target = GameObject.Find(name); Assert(target, "Missing control " + name);
        target.GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
    }
    static void Assert(bool condition, string message) { if (!condition) throw new Exception(message); }
    static GameObject FindStage(string name) => view.transform.Find("SafeArea/LandscapeFrame/KitchenStage/" + name).gameObject;
    static void Tick()
    {
        try
        {
            Assert(EditorApplication.isPlaying && game, "Play Mode interrupted"); Assert(EditorApplication.timeSinceStartup < deadline, "QA timed out at phase " + phase);
            Assert(KitchenRules.IsFloorPositionClear(game.ChefPosition), "Movement crossed furniture");
            switch (phase)
            {
                case 0:
                    if (!FindStage("TemporalHub").activeSelf || FindStage("PageTransition").activeSelf) return;
                    Click("Level1"); Assert(game.TutorialActive, "First-time play did not offer guided tutorial"); phase++; break;
                case 1:
                    if (game.IsTrainingResult)
                    {
                        Assert(game.Progress.TutorialDone && game.Progress.Bank == 0 && game.Progress.Unlocked == 1, "Training changed campaign economy");
                        Assert(game.Remaining == KitchenLevel.For(1).Seconds, "Training timer was not protected");
                        results.Add("Tutorial: both recipes, all twelve steps, no pressure or economy rewards"); Click("NextLevel"); phase++; break;
                    }
                    Assert(game.TutorialActive, "Tutorial stopped early");
                    if (game.IsMoving || game.IsWorking || game.HasDestination) return;
                    if (game.TutorialStation == Station.Stove && (game.StoveFood == Food.ChoppedDodo || game.StoveFood == Food.Roots)) return;
                    game.SelectStation((int)game.TutorialStation); break;
                case 2:
                    Assert(!game.TutorialActive && game.Running, "Training did not lead into a fresh turn"); Click("DodoSupply"); phase++; break;
                case 3: if (game.Carry != Food.RawDodo) return; Click("CutStation"); phase++; break;
                case 4:
                    if (game.IsWorking || game.HasDestination) return;
                    if (game.Carry == Food.Empty && game.PrepFood == Food.ChoppedDodo) { Click("CutStation"); return; }
                    if (game.Carry != Food.ChoppedDodo) return; Click("PlasmaStove"); phase++; break;
                case 5:
                    if (game.StoveFood != Food.ChoppedDodo || game.CookTime < 1) return;
                    game.TogglePause(); var saved = game.Progress.LoadSession(); Assert(saved != null && saved.stove == Food.ChoppedDodo, "Mid-cook autosave missing");
                    game.GoToMenu(); game.Progress = new KitchenProgress(prefix); Assert(game.ResumeSavedShift(), "Could not recover mid-cook session");
                    Assert(game.Paused && game.CookTime == saved.cookTime && game.Remaining == saved.remaining, "Recovered time differs from save"); game.TogglePause(); phase++; break;
                case 6:
                    if (game.StoveFood == Food.CookedDodo && !game.HasDestination) { Click("PlasmaStove"); return; }
                    if (game.Carry != Food.CookedDodo) return;
                    game.TogglePause(); game.GoToMenu(); Assert(game.ResumeSavedShift() && game.Carry == Food.CookedDodo, "Carried food was lost on resume");
                    results.Add("Save/reload: cooking progress, carried food, remaining time and paused recovery; explicit collection from prep station");
                    game.BeginLevel(1); level = 1; phase++; Status = "Campaign: level 1/10"; break;
                case 7:
                    if (!game.Running)
                    {
                        results.Add($"Level {level}: {game.Served} delivered, {game.Stars} stars, {game.Lost} lost, reputation {game.Reputation}");
                        File.WriteAllLines("Tests/beta-campaign-progress.txt", results);
                        Assert(game.Stars > 0, "Campaign goal unreachable by timed bot at level " + level);
                        int bank = game.Progress.Bank; game.EndShift(); Assert(bank == game.Progress.Bank, "Results paid out twice");
                        if (level == 10) { phase++; game.BeginLevel(1); game.AddAnomaly(100); Status = "Checking anomaly and settings"; return; }
                        if (level == 5) { Assert(game.Progress.Gems >= 3, "Not enough attainable gems for era gate"); game.Progress.UnlockEra(); Assert(game.Progress.Era == 2, "Era gate did not open"); }
                        level++; game.BeginLevel(level); Status = "Campaign: level " + level + "/10"; return;
                    }
                    DriveCook(); break;
                case 8:
                    if (game.CloneRemaining > 0) { if (game.CloneRemaining < 10) Assert(!FindStage("ParadoxBadge").activeSelf, "Onset alert lingered"); return; }
                    Assert(!FindStage("ParadoxBadge").activeSelf, "Expired anomaly alert still visible");
                    game.GoToMenu(); game.Progress.ClearSession(); view.ShowHome(); Click("HomeOptions");
                    Assert(FindStage("Settings").activeSelf, "Home settings did not open");
                    bool motion = view.ReducedMotion; Click("ReducedMotion"); Assert(view.ReducedMotion != motion, "Reduced motion toggle failed"); Click("ReducedMotion"); Assert(view.ReducedMotion == motion, "Motion setting did not restore");
                    var audio = view.GetComponent<KitchenAudio>(); float gain = audio.MusicGain; audio.SetMusic(0); foreach (var source in view.GetComponents<AudioSource>()) if (source.clip) Assert(source.volume == 0, "Music mute failed"); audio.SetMusic(gain);
                    Click("SettingsClose"); Click("HomeCredits"); Assert(FindStage("Credits").activeSelf, "Credits did not open"); Click("CloseCredits");
                    PlayerPrefs.SetString(prefix + "session", "{malformed json"); Assert(game.Progress.LoadSession() == null, "Corrupt session did not recover safely");
                    results.Add("Anomaly lifecycle, all ten timed campaign turns, home navigation, audio gain, reduced motion and corrupted-save recovery");
                    Complete("PASS"); break;
            }
        }
        catch (Exception e) { Complete("FAIL phase " + phase + ": " + e); }
    }
    static void DriveCook()
    {
        if (game.IsMoving || game.IsWorking || game.HasDestination || game.Paused) return;
        switch (game.Carry)
        {
            case Food.Empty:
                if (game.PrepFood != Food.Empty) { game.SelectStation(1); return; }
                if (game.StoveFood == Food.CookedDodo || game.StoveFood == Food.Soup || game.StoveFood == Food.Burnt) { game.SelectStation(2); return; }
                if (game.StoveFood != Food.Empty) return;
                int slot = Customer(Food.Empty); if (slot < 0) return;
                plannedDish = game.Orders[slot];
                game.SelectStation(plannedDish == Food.Burger ? 0 : 8); break;
            case Food.RawDodo: game.SelectStation(1); break;
            case Food.Root: game.SelectStation(plannedDish == Food.Toast ? 1 : 8); break;
            case Food.ChoppedRoot: game.SelectStation(3); break;
            case Food.Roots: game.SelectStation(game.SoupNeedsDicing ? 1 : 2); break;
            case Food.ChoppedDodo: case Food.ChoppedRoots: game.SelectStation(2); break;
            case Food.CookedDodo: game.SelectStation(3); break;
            case Food.Toast: case Food.Burger: case Food.Soup: int target = Customer(game.Carry); if (target >= 0) game.SelectStation(5 + target); break;
            case Food.Burnt: game.SelectStation(4); break;
        }
    }
    static int Customer(Food food)
    {
        int slot = -1; float patience = float.MaxValue;
        for (int i = 0; i < 3; i++) if (game.Customers[i] && (food == Food.Empty || game.Orders[i] == food) && game.Patience[i] < patience) { slot = i; patience = game.Patience[i]; }
        return slot;
    }
    static void Complete(string verdict)
    {
        EditorApplication.update -= Tick; Time.timeScale = originalSpeed; Status = verdict;
        if (game) { game.GoToMenu(); game.Progress.ClearSession(); game.Progress = original; game.GoToMenu(); view.ShowHome(); }
        foreach (var key in new[] { "credits", "gems", "unlocked", "era", "speed", "heat", "tutorial" }) PlayerPrefs.DeleteKey(prefix + key);
        for (int i = 1; i <= 10; i++) PlayerPrefs.DeleteKey(prefix + "stars." + i); PlayerPrefs.Save();
        Directory.CreateDirectory("Tests"); File.WriteAllText("Tests/beta-result.txt", verdict + "\n" + string.Join("\n", results));
        if (verdict == "PASS") Debug.Log("Chronos beta QA passed"); else Debug.LogError(verdict);
    }
}
