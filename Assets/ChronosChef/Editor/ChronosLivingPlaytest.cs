using System;
using System.Collections.Generic;
using System.IO;
using ChronosChef;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class ChronosLivingPlaytest
{
    static KitchenGame game;
    static KitchenView view;
    static KitchenProgress original;
    static string prefix;
    static int step;
    static double deadline, heldSince;
    static float pausedRemaining;
    static Vector3[] idlePose;
    public static string Status = "Not run";
    public static void Start()
    {
        if (!EditorApplication.isPlaying) throw new Exception("Enter Play Mode first.");
        game = UnityEngine.Object.FindFirstObjectByType<KitchenGame>(); view = UnityEngine.Object.FindFirstObjectByType<KitchenView>();
        original = game.Progress; prefix = "ChronosChef.livingQA." + Guid.NewGuid() + ".";
        game.Progress = new KitchenProgress(prefix); game.BeginLevel(1); Time.timeScale = 4;
        step = 0; deadline = EditorApplication.timeSinceStartup + 80; Status = "Running";
        EditorApplication.update -= Tick; EditorApplication.update += Tick;
        Click("DodoSupply");
    }
    static void Assert(bool condition, string message) { if (!condition) throw new Exception(message); }
    static void Click(string name) => GameObject.Find(name).GetComponent<Button>().onClick.Invoke();
    static Transform Stage => view.transform.Find("SafeArea/LandscapeFrame/KitchenStage");
    static GameObject Panel(string name) => Stage.Find(name).gameObject;
    static KitchenModelGraphic Portrait => Stage.Find("CustomerWindow0/Visitor_0_0/IllustratedCharacter/Model3D").GetComponent<KitchenModelGraphic>();
    static void Tick()
    {
        try
        {
            Assert(EditorApplication.isPlaying && game, "Play Mode interrupted");
            Assert(EditorApplication.timeSinceStartup < deadline, "Timeout at step " + step);
            Assert(KitchenRules.IsFloorPositionClear(game.ChefPosition), "Chef crossed furniture");
            switch (step)
            {
                case 0: if (game.Carry != Food.RawDodo) return; Click("CutStation"); step++; break;
                case 1:
                    if (!game.IsWorking) return;
                    Assert(game.PrepFood == Food.RawDodo && game.Carry == Food.Empty, "Cut did not deposit food/free hands");
                    game.SaveCurrentSession(); var saved = game.Progress.LoadSession();
                    Assert(saved != null && saved.prep == Food.RawDodo && saved.work > 0, "Cutting station was absent from save");
                    game.GoToMenu(); Assert(game.ResumeSavedShift() && game.Paused && game.PrepFood == Food.RawDodo && game.IsWorking, "Mid-cut restore failed");
                    game.TogglePause(); Click("RootSupply");
                    Assert(game.HasDestination && game.IsWorking, "Cannot leave automatic cutting job"); step++; break;
                case 2:
                    if (game.Carry != Food.Root || game.PrepFood != Food.ChoppedDodo || game.IsWorking) return;
                    Click("CutStation"); step++; break;
                case 3:
                    if (game.HasDestination) return;
                    Assert(game.Carry == Food.Root && game.PrepFood == Food.ChoppedDodo, "Occupied cutting board overwrote food");
                    heldSince = EditorApplication.timeSinceStartup; idlePose = Portrait.canvasRenderer.GetMesh().vertices; step++; break;
                case 4:
                    if (EditorApplication.timeSinceStartup - heldSince < 2) return;
                    Assert(game.PrepFood == Food.ChoppedDodo && game.Carry == Food.Root, "Prepared food vanished while left on board");
                    game.Patience[0] = 10; step++; break;
                case 5:
                    var current = Portrait.canvasRenderer.GetMesh().vertices;
                    Assert(current.Length != idlePose.Length || !System.Linq.Enumerable.SequenceEqual(current,idlePose), "Customer mesh stayed in a single pose");
                    Assert(Portrait.canvasRenderer.hasRectClipping, "Animated customer is not clipped behind bar");
                    Click("QuantumBin"); step++; break;
                case 6:
                    if (game.Carry != Food.Empty) return; Click("CutStation"); step++; break;
                case 7:
                    if (game.Carry != Food.ChoppedDodo) return;
                    Assert(game.PrepFood == Food.Empty, "Collection duplicated prepared food");
                    Click("RecipeHelp"); pausedRemaining = game.Remaining; heldSince = EditorApplication.timeSinceStartup;
                    Assert(game.Paused && Panel("RecipeBook").activeSelf, "Recipe terminal did not pause game"); step++; break;
                case 8:
                    if (EditorApplication.timeSinceStartup - heldSince < .5) return;
                    Assert(game.Remaining == pausedRemaining, "Recipe terminal did not freeze timer");
                    Click("SoupTab"); Click("RecipeStep1"); Click("RecipeAuto"); Click("RecipeNext"); Click("CloseRecipes");
                    Assert(game.Running && !game.Paused && !Panel("PauseScreen").activeSelf && !Panel("RecipeBook").activeSelf, "Entendido did not resume directly");
                    game.ResetShift(); game.Interact(Station.Roots); game.Interact(Station.Cut); step++; break;
                case 9:
                    if (game.IsWorking) return;
                    Assert(game.PrepFood == Food.ChoppedRoot && game.Carry == Food.Empty, "Single vegetable did not remain on cutting board");
                    game.Interact(Station.Cut); game.Interact(Station.Roots); game.Interact(Station.Cut); step++; break;
                case 10:
                    if (game.IsWorking) return;
                    Assert(game.PrepFood == Food.ChoppedRoots, "Two vegetables were not chopped");
                    game.SaveCurrentSession(); game.GoToMenu(); Assert(game.ResumeSavedShift() && game.PrepFood == Food.ChoppedRoots, "Prepared vegetables lost on restore"); game.TogglePause();
                    game.Interact(Station.Cut); game.Interact(Station.Stove); step++; break;
                case 11:
                    if (game.StoveFood != Food.Soup) return;
                    game.Interact(Station.Stove); Assert(game.Carry == Food.Soup, "Chopped vegetables did not make soup");
                    game.GoToMenu(); step++; break;
                case 12:
                    AuditMap();
                    Assert(Stage.Find("Assembly/BreadSupply").GetComponent<Image>().sprite != Stage.Find("Hotbar0/FoodIcon/FoodIllustration_RawDodo").GetComponent<Image>().sprite, "Bread illustration missing");
                    Complete("PASS: automatic cutting frees hands; leave station mid-job; meat persists; occupied board rejects overwrite; manual pickup; mid-cut and prepared-vegetable saves; cut one/two vegetables and cook soup; recipe pauses timer and Entendido resumes directly; customer expression change with bar clipping; ten map portal hit targets and bread art."); break;
            }
        }
        catch (Exception error) { Complete("FAIL step " + step + ": " + error); }
    }
    static void AuditMap()
    {
        Canvas.ForceUpdateCanvases();
        var map = Stage.Find("TemporalHub");
        for (int i = 1; i <= 10; i++)
        {
            var portal = map.Find("Level" + i); var hits = new List<RaycastResult>();
            var pointer = new PointerEventData(EventSystem.current) { position = RectTransformUtility.WorldToScreenPoint(null, portal.position) };
            EventSystem.current.RaycastAll(pointer, hits);
            Assert(hits.Count > 0 && hits[0].gameObject.GetComponentInParent<Button>()?.name == portal.name, $"Blocked portal {i}, pointer {pointer.position}, screen {Screen.width}x{Screen.height}, first hit {(hits.Count > 0 ? hits[0].gameObject.name : "none")}");
        }
    }
    static void Complete(string result)
    {
        EditorApplication.update -= Tick; Time.timeScale = 1; Status = result;
        if (game) { game.GoToMenu(); game.Progress.ClearSession(); game.Progress = original; game.GoToMenu(); view.ShowHome(); }
        foreach (var key in new[]{"credits","gems","unlocked","tutorial","session"}) PlayerPrefs.DeleteKey(prefix + key);
        PlayerPrefs.Save(); Directory.CreateDirectory("Tests"); File.WriteAllText("Tests/living-kitchen-result.txt", result);
        if (result.StartsWith("PASS")) Debug.Log(result); else Debug.LogError(result);
    }
}
