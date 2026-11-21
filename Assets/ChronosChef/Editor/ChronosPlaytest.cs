using System;
using System.IO;
using ChronosChef;
using UnityEditor;
using UnityEngine;

// Exercises the same Button callbacks as pointer input, including movement and timed work.
public static class ChronosPlaytest
{
    static KitchenGame game;
    static KitchenProgress savedProgress;
    static string testPrefix;
    static int step;
    static double deadline, pauseStarted;
    static float pausedTime;
    public static string Status = "Not run";
    public static void Start()
    {
        if (!EditorApplication.isPlaying) throw new InvalidOperationException("Enter Play Mode first.");
        game = UnityEngine.Object.FindFirstObjectByType<KitchenGame>();
        savedProgress = game.Progress; testPrefix = "ChronosChef.smoke." + System.Guid.NewGuid() + ".";
        game.Progress = new KitchenProgress(testPrefix);
        game.ResetShift(); Time.timeScale = 4; step = 0; Status = "Running";
        if (AlertVisible()) throw new Exception("Paradox alert visible before an anomaly.");
        deadline = EditorApplication.timeSinceStartup + 45;
        EditorApplication.update -= Tick; EditorApplication.update += Tick;
        Click("DodoSupply");
    }
    static void Click(string name) => GameObject.Find(name).GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
    static void Tick()
    {
        try
        {
            if (!EditorApplication.isPlaying || !game) throw new Exception("Play Mode interrupted.");
            if (!KitchenRules.IsFloorPositionClear(game.ChefPosition)) throw new Exception("Chef entered a kitchen fixture during movement.");
            if (EditorApplication.timeSinceStartup > deadline) throw new Exception("Timeout at step " + step);
            switch (step)
            {
                case 0: if (game.Carry != Food.RawDodo) return; Click("CutStation"); step++; break;
                case 1: if (CollectPrep()) return; if (game.Carry != Food.ChoppedDodo) return; Click("PlasmaStove"); step++; break;
                case 2: if (game.StoveFood != Food.CookedDodo) return; Click("PlasmaStove"); step++; break;
                case 3: if (game.Carry != Food.CookedDodo) return; Click("Assembly"); step++; break;
                case 4: if (game.Carry != Food.Burger) return; Click("ServeCustomer0"); step++; break;
                case 5:
                    if (game.Served != 1) return;
                    if (game.Credits < 100 || game.Carry != Food.Empty) throw new Exception("Delivery did not award credits/clear hands.");
                    Click("DodoSupply"); step++; break;
                case 6: if (game.Carry != Food.RawDodo) return; Click("CutStation"); step++; break;
                case 7: if (CollectPrep()) return; if (game.Carry != Food.ChoppedDodo) return; Click("PlasmaStove"); step++; break;
                case 8: if (game.StoveFood != Food.Burnt) return; Click("PlasmaStove"); step++; break;
                case 9: if (game.Carry != Food.Burnt) return; Click("QuantumBin"); step++; break;
                case 10:
                    if (game.Carry != Food.Empty) return;
                    if (game.Anomaly < 10) throw new Exception("Waste did not add anomaly.");
                    game.AddAnomaly(100);
                    if (game.CloneRemaining <= 0) throw new Exception("Clone did not spawn.");
                    var blocked = new Vector2Int(Mathf.RoundToInt((game.ClonePosition.x - 360) / 80), Mathf.RoundToInt((game.ClonePosition.y - 245) / 80));
                    for (int i = 0; i < 8; i++)
                    {
                        var route = KitchenRules.Route(new Vector2Int(0, 0), KitchenRules.Approach((Station)i), blocked);
                        if (i != 4 && route.Count == 0 || route.Contains(blocked)) throw new Exception("Unreachable station or route through clone.");
                    }
                    game.TogglePause(); pausedTime = game.Remaining; pauseStarted = EditorApplication.timeSinceStartup; step++; break;
                case 11:
                    if (EditorApplication.timeSinceStartup - pauseStarted < .4) return;
                    if (!AlertVisible()) throw new Exception("Active paradox did not show its alert.");
                    if (Mathf.Abs(game.Remaining - pausedTime) > .01f) throw new Exception("Pause did not stop timer.");
                    game.TogglePause(); Time.timeScale = 15; step++; break;
                case 12:
                    if (game.Running) return;
                    if (AlertVisible()) throw new Exception("Paradox alert remained after shift end.");
                    var panel = UnityEngine.Object.FindFirstObjectByType<KitchenView>().transform.Find("SafeArea/LandscapeFrame/KitchenStage/ShiftResults");
                    if (!panel.gameObject.activeSelf) throw new Exception("Missing results screen.");
                    GameObject.Find("Restart").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
                    if (!game.Running || game.Served != 0 || game.Carry != Food.Empty || game.CloneRemaining != 0) throw new Exception("Reset failed.");
                    Complete("PASS: button callbacks, grid movement, cutting, cooking, assembly, delivery/credits, burning, trash, clone routing, pause, shift end, results and restart."); break;
            }
        }
        catch (Exception error) { Complete("FAIL: " + error); }
    }
    static bool AlertVisible() => UnityEngine.Object.FindFirstObjectByType<KitchenView>().transform.Find("SafeArea/LandscapeFrame/KitchenStage/ParadoxBadge").gameObject.activeSelf;
    static bool CollectPrep()
    {
        if (game.Carry != Food.Empty || game.PrepFood != Food.ChoppedDodo || game.IsWorking || game.HasDestination) return false;
        Click("CutStation"); return true;
    }
    static void Complete(string result)
    {
        EditorApplication.update -= Tick; Time.timeScale = 1; Status = result;
        if (game) { game.GoToMenu(); game.Progress.ClearSession(); game.Progress = savedProgress; game.GoToMenu(); }
        foreach (var key in new[] { "credits", "gems", "unlocked", "stars.1" }) PlayerPrefs.DeleteKey(testPrefix + key);
        PlayerPrefs.Save();
        Directory.CreateDirectory("Tests"); File.WriteAllText("Tests/playtest-result.txt", result);
        if (result.StartsWith("PASS")) Debug.Log(result); else Debug.LogError(result);
    }
}
