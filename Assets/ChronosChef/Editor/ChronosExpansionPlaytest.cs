using System;
using System.Collections.Generic;
using System.IO;
using ChronosChef;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

public static class ChronosExpansionPlaytest
{
    static KitchenGame game;
    static KitchenProgress original;
    static int step;
    static string prefix;
    static double deadline;
    public static string Status = "Not run";
    public static void Start()
    {
        if (!EditorApplication.isPlaying) throw new Exception("Start Play Mode first.");
        game = UnityEngine.Object.FindFirstObjectByType<KitchenGame>(); original = game.Progress;
        prefix = "ChronosChef.test." + Guid.NewGuid() + ".";
        game.Progress = new KitchenProgress(prefix); game.ResetShift(); Time.timeScale = 4;
        step = 0; deadline = EditorApplication.timeSinceStartup + 80; Status = "Running";
        AuditTargets(); EditorApplication.update -= Tick; EditorApplication.update += Tick;
        Click("DodoSupply");
    }
    static void Assert(bool condition, string message) { if (!condition) throw new Exception(message); }
    static void Click(string name) => GameObject.Find(name).GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
    static PointerEventData PointerAt(float x, float y)
    {
        var stage = GameObject.Find("KitchenStage").transform;
        return new PointerEventData(EventSystem.current) { position = RectTransformUtility.WorldToScreenPoint(null, stage.TransformPoint(new Vector3(x, y))) };
    }
    static GameObject Hit(float x, float y)
    {
        Canvas.ForceUpdateCanvases();
        var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(PointerAt(x, y), hits);
        Assert(hits.Count > 0, "No raycast at " + x + ", " + y); return hits[0].gameObject;
    }
    static void AuditTargets()
    {
        Canvas.ForceUpdateCanvases();
        for (int i = 0; i < 3; i++) foreach (float y in new[] { 665f, 725f, 790f, 850f })
            Assert(Hit(630 + i * 270, y).GetComponentInParent<UnityEngine.UI.Button>().name == "ServeCustomer" + i, "Customer body/head/card click was missed.");
    }
    static void Deliver(int i, float y)
    {
        var target = Hit(630 + i * 270, y);
        ExecuteEvents.Execute(target, PointerAt(630 + i * 270, y), ExecuteEvents.pointerClickHandler);
    }
    static void Tick()
    {
        try
        {
            Assert(EditorApplication.isPlaying && game, "Play mode interrupted");
            Assert(KitchenRules.IsFloorPositionClear(game.ChefPosition), "Chef crossed a kitchen fixture.");
            Assert(EditorApplication.timeSinceStartup < deadline, "Timed out at step " + step);
            switch (step)
            {
                case 0: if (game.Carry != Food.RawDodo) return; Click("CutStation"); step++; break;
                case 1: if (CollectPrep()) return; if (game.Carry != Food.ChoppedDodo) return; Click("PlasmaStove"); step++; break;
                case 2: if (game.StoveFood != Food.CookedDodo) return; Click("PlasmaStove"); step++; break;
                case 3: if (game.Carry != Food.CookedDodo) return; Click("Assembly"); step++; break;
                case 4:
                    if (game.Carry != Food.Burger) return;
                    game.Interact(Station.Customer1); Assert(game.Carry == Food.Burger && game.Served == 0, "Wrong-order delivery consumed a burger.");
                    Deliver(0, 725); step++; break;
                case 5: if (game.Served != 1) return; Assert(game.Carry == Food.Empty && game.Credits > 100, "Delivery did not clear hands/pay."); Click("RootSupply"); step++; break;
                case 6: if (game.Carry != Food.Root) return; Click("RootSupply"); step++; break;
                case 7: if (game.Carry != Food.Roots) return; Click("PlasmaStove"); step++; break;
                case 8: if (game.StoveFood != Food.Soup) return; Click("PlasmaStove"); step++; break;
                case 9: if (game.Carry != Food.Soup) return; Deliver(1, 790); step++; break;
                case 10: if (game.Served != 2) return; Click("DodoSupply"); step++; break;
                case 11: if (game.Carry != Food.RawDodo) return; Click("CutStation"); step++; break;
                case 12: if (CollectPrep()) return; if (game.Carry != Food.ChoppedDodo) return; Click("PlasmaStove"); step++; break;
                case 13: if (game.StoveFood != Food.CookedDodo) return; Click("PlasmaStove"); step++; break;
                case 14: if (game.Carry != Food.CookedDodo) return; Click("Assembly"); step++; break;
                case 15: if (game.Carry != Food.Burger) return; Deliver(2, 665); step++; break;
                case 16:
                    if (game.Served != 3) return;
                    Assert(game.Combo == 3 && game.Stars == 1, "Combo or level objective failed.");
                    Assert(Array.Exists(game.Species, s => s == 3), "Raptor never arrived.");
                    game.EndShift(); Assert(!game.Running && game.Progress.Bank == game.Credits && game.Progress.Unlocked == 2, "Results not persisted/unlocked.");
                    var p = game.Progress; p.Complete(1, 2000, 3); int gems = p.Gems; p.Complete(1, 0, 3); Assert(p.Gems == gems, "Repeated stars farmed gems.");
                    p.Complete(2, 0, 3); p.Complete(3, 0, 3); Assert(p.Gems == 3, "Three-star gems failed.");
                    p.UnlockEra(); Assert(p.Era == 2 && p.Gems == 0, "Era unlock did not spend gems.");
                    int bank = p.Bank; p.Buy(true); p.Buy(false); Assert(p.Speed == 1 && p.Heat == 1 && p.Bank == bank - 600, "Upgrades failed.");
                    var reload = new KitchenProgress(prefix); Assert(reload.Speed == 1 && reload.Era == 2 && reload.Bank == p.Bank, "Save reload failed.");
                    game.BeginLevel(2); Assert(game.Running && game.Level == 2 && game.Served == 0, "Next-level reset failed.");
                    game.TogglePause(); Assert(game.Paused, "Pause failed."); game.TogglePause();
                    Complete("PASS: real EventSystem delivery from head, order card and body; both full recipes; wrong-order rejection; combos; raptor arrivals; objectives; results; local save reload; one-time gems; era unlock; upgrades; next level and pause."); break;
            }
        }
        catch (Exception e) { Complete("FAIL step " + step + ": " + e); }
    }
    static bool CollectPrep()
    {
        if (game.Carry != Food.Empty || game.PrepFood != Food.ChoppedDodo || game.IsWorking || game.HasDestination) return false;
        Click("CutStation"); return true;
    }
    static void Complete(string message)
    {
        EditorApplication.update -= Tick; Time.timeScale = 1;
        if (game) { game.GoToMenu(); game.Progress.ClearSession(); game.Progress = original; game.GoToMenu(); }
        foreach (var key in new[] { "credits", "gems", "unlocked", "era", "speed", "heat", "stars.1", "stars.2", "stars.3" }) PlayerPrefs.DeleteKey(prefix + key);
        PlayerPrefs.Save(); Status = message;
        Directory.CreateDirectory("Tests"); File.WriteAllText("Tests/expansion-playtest-result.txt", message);
        if (message.StartsWith("PASS")) Debug.Log(message); else Debug.LogError(message);
    }
}
