using System.Linq;
using TMPro;
using UnityEngine;
namespace ChronosChef
{
    public sealed partial class KitchenView
    {
        [SerializeField] Texture2D environmentArt;
        [SerializeField] Texture2D steampunkArt;
        UnityEngine.UI.RawImage environmentLayer, counterLayer;
        bool fixedCustomerWindows;
        RectTransform clockHand;
        TMP_Text paradoxCaption;
        UnityEngine.UI.Image paradoxRim;
        GameObject paradoxBorder;
        GameObject[][] ingredientRows;
        void InstallEnvironment()
        {
            if (!environmentArt) return;
            var art = new GameObject("IllustratedKitchen", typeof(RectTransform), typeof(UnityEngine.UI.RawImage));
            var rect = art.GetComponent<RectTransform>(); rect.SetParent(stage, false); rect.anchorMin = rect.anchorMax = Vector2.zero; rect.anchoredPosition = new Vector2(800, 450); rect.sizeDelta = new Vector2(1600, 900); rect.SetSiblingIndex(1);
            var raw = art.GetComponent<UnityEngine.UI.RawImage>(); raw.texture = environmentArt; raw.raycastTarget = false; environmentLayer = raw;
            foreach (Transform child in stage)
                if (child.name.StartsWith("Floor_") || child.name == "TileLight" || child.name.StartsWith("Counter") || child.name.StartsWith("KitchenBorder") || child.name == "KitchenFloor" || child.name.EndsWith("Glow")) child.gameObject.SetActive(false);

            Vector2[] positions = { new Vector2(185, 350), new Vector2(670, 510), new Vector2(945, 510), new Vector2(1430, 455), new Vector2(147, 220), Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(210, 478) };
            string[] captions = { "DODO", "CORTE", "PLASMA", "MONTAJE", "BASURA", "", "", "", "RAÍCES ×2" };
            for (int i = 0; i < stations.Length; i++)
            {
                if (i >= 5 && i <= 7) continue;
                var station = stations[i]; var r = (RectTransform)station.transform;
                r.anchoredPosition = positions[i]; r.sizeDelta = new Vector2(i == 4 ? 180 : 205, i == 4 ? 150 : 140);
                station.GetComponent<UnityEngine.UI.Image>().color = Color.clear;
                foreach (Transform child in r) if (!child.name.StartsWith("Stove_")) child.gameObject.SetActive(false);
                var badge = Panel(r, "StationCaption", 0, -53, 150, 25, new Color(.035f, .065f, .12f, .92f));
                Label(badge, captions[i], -73, 0, 146, 23, 16, i == 8 ? Green : Cyan, FontStyles.Bold);
            }
            // A live hotplate covers the decorative food baked into the environment illustration.
            var hotplate = Dot(stations[2].transform, "LiveHotplate", 0, 23, 66, C("243049")); hotplate.sizeDelta = new Vector2(71, 34); hotplate.SetAsFirstSibling();
            Dot(hotplate, "PlasmaCore", 0, 0, 32, Green).sizeDelta = new Vector2(40, 19);
            foreach (var food in stoveFoods) if (food) { food.anchoredPosition = new Vector2(0, 27); food.SetAsLastSibling(); }
            // A stationary window clips each visitor ABOVE the back edge of the bar.
            // It must not travel with the visitor: that exposed bodies during arrival/bobbing.
            for (int i = 0; i < 3; i++)
            {
                var window = new GameObject("CustomerWindow" + i, typeof(RectTransform), typeof(UnityEngine.UI.RectMask2D)).GetComponent<RectTransform>();
                window.SetParent(stage, false); window.anchorMin = window.anchorMax = Vector2.zero; window.anchoredPosition = new Vector2(630 + i * 270, 706); window.sizeDelta = new Vector2(250, 112);
                for (int j = 0; j < 4; j++)
                {
                    var visitor = customerArt[i][j]; visitor.SetParent(window, false); visitor.anchorMin = visitor.anchorMax = new Vector2(.5f, .5f); visitor.anchoredPosition = new Vector2(0, -26); visitor.sizeDelta = new Vector2(190, 145);
                    var illustration = visitor.Find("IllustratedCharacter") as RectTransform;
                    if (illustration) { illustration.sizeDelta = new Vector2(150, 175); illustration.anchoredPosition = new Vector2(0, -15); }
                }
            }
            fixedCustomerWindows = true;
            var foreground = new GameObject("CounterForeground", typeof(RectTransform), typeof(UnityEngine.UI.RawImage));
            var foregroundRect = foreground.GetComponent<RectTransform>(); foregroundRect.SetParent(stage, false); foregroundRect.anchorMin = foregroundRect.anchorMax = Vector2.zero; foregroundRect.anchoredPosition = new Vector2(835, 612.5f); foregroundRect.sizeDelta = new Vector2(1070, 75);
            var foregroundImage = foreground.GetComponent<UnityEngine.UI.RawImage>(); foregroundImage.texture = environmentArt; foregroundImage.uvRect = new Rect(300f / 1600, 575f / 900, 1070f / 1600, 75f / 900); foregroundImage.raycastTarget = false; counterLayer = foregroundImage;
            ingredientRows = new GameObject[3][];
            for (int i = 0; i < 3; i++)
            {
                var card = customerLabels[i].transform.parent;
                var cardRect = card.GetComponent<RectTransform>(); cardRect.anchoredPosition = new Vector2(630 + i * 270, 810); cardRect.sizeDelta = new Vector2(247, 100);
                var rim = stage.Cast<Transform>().Where(t => t.name == "OrderRim").ElementAt(i).GetComponent<RectTransform>(); rim.anchoredPosition = cardRect.anchoredPosition; rim.sizeDelta = new Vector2(252, 105);
                patienceFills[i].transform.parent.GetComponent<RectTransform>().anchoredPosition = new Vector2(630 + i * 270, 751);
                var hit = stations[5 + i].GetComponent<RectTransform>(); hit.anchoredPosition = new Vector2(630 + i * 270, 746); hit.sizeDelta = new Vector2(250, 232);
                customerLabels[i].rectTransform.anchoredPosition = new Vector2(48, 6);
                speciesLabels[i].rectTransform.anchoredPosition = new Vector2(0, 39); speciesLabels[i].rectTransform.sizeDelta = new Vector2(230, 18);
                ingredientRows[i] = new GameObject[KitchenRecipe.All.Length];
                for (int recipe = 0; recipe < ingredientRows[i].Length; recipe++)
                {
                    var row = new GameObject("RecipeIngredients" + recipe, typeof(RectTransform)); row.transform.SetParent(card, false); row.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -35); ingredientRows[i][recipe] = row;
                    Food[] foods = KitchenRecipe.All[recipe].Ingredients;
                    for (int item = 0; item < 4; item++)
                    {
                        var icon = new GameObject("Step" + item, typeof(RectTransform)).GetComponent<RectTransform>(); icon.SetParent(row.transform, false); icon.anchoredPosition = new Vector2(-87 + item * 58, 0); icon.localScale = Vector3.one * .3f; DrawFood(icon, foods[item]);
                        if (item < 3) Label(row.transform, item == 0 && recipe == 1 ? "+" : ">", -66 + item * 58, 0, 20, 22, 15, Cyan, FontStyles.Bold);
                    }
                }
            }
            stoveLabel.rectTransform.anchoredPosition = new Vector2(945, 428); stoveLabel.fontSize = 16;
            cookFill.transform.parent.GetComponent<RectTransform>().anchoredPosition = new Vector2(945, 446);
            burnFill.transform.parent.GetComponent<RectTransform>().anchoredPosition = new Vector2(945, 436);
            wallet.rectTransform.anchoredPosition = new Vector2(1450, 790); wallet.rectTransform.sizeDelta = new Vector2(260, 80); wallet.fontSize = 21; wallet.alignment = TextAlignmentOptions.Right;
            goal.rectTransform.anchoredPosition = new Vector2(855, 882); goal.rectTransform.sizeDelta = new Vector2(860, 28); goal.alignment = TextAlignmentOptions.Left; goal.fontSize = 17;
            // Compact lower hotbar, matching the reference composition.
            var bottom = stage.Find("BottomPanel").GetComponent<RectTransform>(); bottom.anchoredPosition = new Vector2(800, 74); bottom.sizeDelta = new Vector2(1488, 140); bottom.GetComponent<UnityEngine.UI.Image>().color = new Color(.03f, .06f, .11f, .88f);
            foreach (var label in stage.GetComponentsInChildren<TMP_Text>()) if (label.text == "EN MANOS") label.gameObject.SetActive(false);
            inventory.rectTransform.anchoredPosition = new Vector2(820, 24); inventory.rectTransform.sizeDelta = new Vector2(740, 30); inventory.fontSize = 18; inventory.alignment = TextAlignmentOptions.Center;
            message.rectTransform.anchoredPosition = new Vector2(820, 159); message.rectTransform.sizeDelta = new Vector2(950, 44); message.fontSize = 19; message.alignment = TextAlignmentOptions.Center;
            Panel(stage, "MessageStrip", 820, 159, 990, 45, new Color(.025f, .04f, .10f, .85f)).SetSiblingIndex(message.transform.GetSiblingIndex());
            var hotbarFoods = new[] { Food.RawDodo, Food.Roots, Food.ChoppedDodo, Food.CookedDodo, Food.Bread, Food.Soup };
            int[] actions = { 0, 8, 1, 2, 3, 2 };
            for (int i = 0; i < 6; i++)
            {
                int action = actions[i];
                var b = Button(stage, "Hotbar" + i, 460 + i * 138, 85, 120, 94, C("25364C"));
                Panel(b.transform, "IconRim", 0, 0, 114, 88, C("48637B")); Panel(b.transform, "IconFace", 0, 0, 109, 83, C("1A2B41"));
                var icon = new GameObject("FoodIcon", typeof(RectTransform)).GetComponent<RectTransform>(); icon.SetParent(b.transform, false); icon.localScale = Vector3.one * .7f; DrawFood(icon, hotbarFoods[i]);
                b.onClick.AddListener(() => game.SelectStation(action));
            }
            paradoxBorder = Panel(stage, "ParadoxBadgeRim", 192, 77, 295, 113, Pink).gameObject;
            var face = Panel(stage, "ParadoxBadge", 192, 77, 289, 107, C("331B3D")); paradoxRim = face.GetComponent<UnityEngine.UI.Image>();
            var clock = Dot(face, "ClockFace", -93, 0, 86, Cyan); Dot(clock, "ClockInner", 0, 0, 75, C("DBE8EC"));
            for (int i = 0; i < 12; i++) { float a = i * Mathf.PI / 6; Dot(clock, "Tick", Mathf.Sin(a) * 31, Mathf.Cos(a) * 31, 4, Ink); }
            clockHand = Panel(clock, "ClockHand", 0, 10, 5, 25, Ink); clockHand.pivot = new Vector2(.5f, 0); clockHand.anchoredPosition = Vector2.zero;
            var shortHand = Panel(clock, "HourHand", -8, 2, 22, 5, Ink); shortHand.localEulerAngles = new Vector3(0, 0, -45);
            paradoxCaption = Label(face, "PARADOX\nALERT", -34, 0, 170, 85, 27, Color.white, FontStyles.Bold);
            SetParadoxVisible(false);
            timer.rectTransform.anchoredPosition = new Vector2(1430, 88); timer.rectTransform.sizeDelta = new Vector2(210, 65); timer.fontSize = 51;
            cloneLabel.rectTransform.anchoredPosition = new Vector2(830, 206);
            // Reference characters should remain above the environment and below the interaction overlays.
            chef.SetAsLastSibling(); clone.SetAsLastSibling(); feedbackLabel.transform.SetAsLastSibling();
        }
        void RefreshEnvironment(KitchenGame g)
        {
            if (!clockHand) return;
            var texture = g.Level > 5 && steampunkArt ? steampunkArt : environmentArt;
            if (environmentLayer.texture != texture) { environmentLayer.texture = texture; counterLayer.texture = texture; }
            SetParadoxVisible(g.Running && g.CloneRemaining > 0);
            float t = g.Running && !g.Paused ? Time.time : 0;
            clockHand.localEulerAngles = new Vector3(0, 0, -t * (g.CloneRemaining > 0 ? 600 : 40));
            paradoxCaption.text = g.CloneRemaining > 0 ? "PARADOX\n" + Mathf.CeilToInt(g.CloneRemaining) + " s" : "PARADOX\nALERT";
            paradoxRim.color = g.CloneRemaining > 0 ? ReducedMotion ? C("7A2853") : Color.Lerp(C("331B3D"), C("9C275E"), Mathf.Abs(Mathf.Sin(t * 10))) : C("331B3D");
            for (int i = 0; i < 3; i++)
            {
                speciesLabels[i].fontSize = 11;
                for (int recipe = 0; recipe < ingredientRows[i].Length; recipe++) ingredientRows[i][recipe].SetActive(g.Orders[i] == KitchenRecipe.All[recipe].Dish);
            }
        }
        void SetParadoxVisible(bool visible)
        {
            if (paradoxBorder) paradoxBorder.SetActive(visible);
            if (paradoxRim) paradoxRim.gameObject.SetActive(visible);
        }
        Vector2 VisitorPosition(int slot, int species, float clock)
        {
            float x = species == 3 ? Mathf.Sin(clock * 5) * 7 : 0;
            float y = species == 2 ? Mathf.Sin(clock * 2) * 7 : 0;
            return fixedCustomerWindows ? new Vector2(x, -26 + y) : new Vector2(630 + slot * 270 + x, 652 + y);
        }
    }
}
