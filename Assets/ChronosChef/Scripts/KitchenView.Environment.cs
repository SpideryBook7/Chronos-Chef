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
        CanvasGroup alertCanvas, borderCanvas;
        bool announcing;
        internal const float NoticeSeconds = 3.2f;
        UnityEngine.UI.Image paradoxRim;
        GameObject paradoxBorder;
        UnityEngine.UI.Image clockSweep;
        OrbitalGalley galley;
        KitchenProgress campaignProgress;
        internal bool InGalley => campaignProgress != null;

        void StartGalleyTrial()
        {
            campaignProgress = game.Progress;
            game.Progress = new KitchenProgress("ChronosChef.OrbitalTrial.");
            if (!galley)
            {
                galley = new GameObject("Orbital Galley Trial").AddComponent<OrbitalGalley>();
                galley.Build(ares);
            }
            galley.gameObject.SetActive(true);
            galley.BindChef(ares);
            counterLayer.gameObject.SetActive(false);
            stations[2].transform.Find("LiveHotplate").gameObject.SetActive(false);
            stations[3].transform.Find("AssemblyPlate").gameObject.SetActive(false);
            stations[3].transform.Find("BreadSupply").gameObject.SetActive(false);
            chef.Find("Shadow").gameObject.SetActive(false);
            for(int i=0;i<3;i++) stage.Find("CustomerWindow"+i).gameObject.SetActive(false);
            ApplyRearLayout();
            angle = 180;
            game.BeginLevel(1);
        }

        void LeaveGalleyTrial()
        {
            if (!InGalley) return;
            RestoreRearLayout();
            for(int i=0;i<3;i++) stage.Find("CustomerWindow"+i).gameObject.SetActive(true);
            ares.SetWorldSkin(null);
            galley.gameObject.SetActive(false);
            counterLayer.gameObject.SetActive(true);
            stations[2].transform.Find("LiveHotplate").gameObject.SetActive(true);
            stations[3].transform.Find("AssemblyPlate").gameObject.SetActive(true);
            stations[3].transform.Find("BreadSupply").gameObject.SetActive(true);
            chef.Find("Shadow").gameObject.SetActive(true);
            environmentLayer.texture = game.Level > 5 && steampunkArt ? steampunkArt : environmentArt;
            counterLayer.texture = environmentLayer.texture;
            game.Progress.ClearSession();
            game.Progress = campaignProgress;
            campaignProgress = null;
        }
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
            for (int i = 0; i < 3; i++)
            {
                var card = customerLabels[i].transform.parent;
                var cardRect = card.GetComponent<RectTransform>(); cardRect.anchoredPosition = new Vector2(630 + i * 270, 810); cardRect.sizeDelta = new Vector2(247, 100);
                var rim = stage.Cast<Transform>().Where(t => t.name == "OrderRim").ElementAt(i).GetComponent<RectTransform>(); rim.anchoredPosition = cardRect.anchoredPosition; rim.sizeDelta = new Vector2(252, 105);
                patienceFills[i].transform.parent.GetComponent<RectTransform>().anchoredPosition = new Vector2(630 + i * 270, 751);
                var hit = stations[5 + i].GetComponent<RectTransform>(); hit.anchoredPosition = new Vector2(630 + i * 270, 746); hit.sizeDelta = new Vector2(250, 232);
                customerLabels[i].rectTransform.anchoredPosition = new Vector2(48, 6);
                speciesLabels[i].rectTransform.anchoredPosition = new Vector2(0, 39); speciesLabels[i].rectTransform.sizeDelta = new Vector2(230, 18);
                cardRect.sizeDelta = new Vector2(246,83);
                rim.sizeDelta = new Vector2(250,87);
                rim.GetComponent<UnityEngine.UI.Image>().color = C("67ADBD");
                speciesLabels[i].rectTransform.anchoredPosition = new Vector2(44,25);
                customerLabels[i].rectTransform.anchoredPosition = new Vector2(44,-7);
                var track = (RectTransform)patienceFills[i].transform.parent;
                track.SetParent(cardRect,false);
                track.anchorMin = track.anchorMax = new Vector2(.5f,.5f);
                track.anchoredPosition = new Vector2(44,-32);
                track.sizeDelta = new Vector2(132,8);
                patienceFills[i].rectTransform.sizeDelta = track.sizeDelta;
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
            bottom.GetComponent<UnityEngine.UI.Image>().enabled = false;
            inventory.gameObject.SetActive(false);
            stage.Find("AnomalyPanel").gameObject.SetActive(false);
            anomalyLabel.gameObject.SetActive(false);
            anomalyFill.transform.parent.gameObject.SetActive(false);
            stage.Find("Text_ERRORES = CAOS TEMPORAL").gameObject.SetActive(false);
            paradoxBorder = Panel(stage,"ParadoxBadgeRim",68,583,4,73,Pink).gameObject;
            var face = Panel(stage,"ParadoxBadge",239,583,336,73,new Color(.09f,.055f,.16f,.72f));
            paradoxRim = face.GetComponent<UnityEngine.UI.Image>();
            var watch = Dot(face,"FracturedClock",-132,0,41,Pink);
            Dot(watch,"Dial",0,0,35,C("29243D"));
            clockHand = Panel(watch,"Hand",0,6,3,13,Color.white);
            clockHand.pivot = new Vector2(.5f,0);
            clockHand.anchoredPosition = Vector2.zero;
            Label(face,"¡ANOMALÍA!",-101,13,247,33,22,Color.white,FontStyles.Bold,TextAlignmentOptions.Left);
            Label(face,"Tu eco bloquea el paso",-101,-15,247,27,17,C("F9CDE7"),FontStyles.Normal,TextAlignmentOptions.Left);
            alertCanvas = face.gameObject.AddComponent<CanvasGroup>();
            borderCanvas = paradoxBorder.AddComponent<CanvasGroup>();
            SetParadoxVisible(false);
            InstallShiftClock();
            cloneLabel.rectTransform.anchoredPosition = new Vector2(830, 206);
            // Reference characters should remain above the environment and below the interaction overlays.
            chef.SetAsLastSibling(); clone.SetAsLastSibling(); feedbackLabel.transform.SetAsLastSibling();
        }
        void RefreshEnvironment(KitchenGame g)
        {
            if (!clockHand) return;
            var texture = g.Level > 5 && steampunkArt ? steampunkArt : environmentArt;
            if (InGalley)
            {
                environmentLayer.texture = galley.Image;
                if(galley.CameraMoved()) ApplyRearLayout();
                galley.UpdateService(g,ares.WorldScale);
                chef.anchoredPosition = galley.FloorScreen(g.ChefPosition);
                clone.anchoredPosition = galley.FloorScreen(g.ClonePosition);
            }
            else if (environmentLayer.texture != texture) { environmentLayer.texture = texture; counterLayer.texture = texture; }
            float age = KitchenGame.ParadoxSeconds - g.CloneRemaining;
            bool visible = announcing && g.Running && g.CloneRemaining > 0 && age < NoticeSeconds;
            if (!visible) announcing = false;
            SetParadoxVisible(visible && !g.Paused);
            if (visible)
            {
                alertCanvas.alpha = borderCanvas.alpha = ReducedMotion ? 1 : Mathf.Clamp01((NoticeSeconds-age)/.45f);
                clockHand.localEulerAngles = new Vector3(0,0,ReducedMotion ? -30 : -age*155);
            }
            clockSweep.fillAmount = Mathf.Clamp01(g.Remaining / g.LevelRules.Seconds);
            clockSweep.color = g.Remaining < 20 ? Pink : Cyan;
        }
        void InstallShiftClock()
        {
            foreach (var title in stage.GetComponentsInChildren<TMP_Text>(true))
                if (title.transform.parent == stage && (title.text == "CHRONOS" || title.text == "CHEF /" ||
                    title.text == "COCINA FUERA DE TIEMPO" || title.text.Contains("PREHISTORIA NEÓN")))
                    title.gameObject.SetActive(false);
            stage.Find("EraBadge").gameObject.SetActive(false);
            var dial = Dot(stage,"ShiftClock",161,795,171,C("365666"));
            dial.SetSiblingIndex(timer.transform.GetSiblingIndex());
            clockSweep = Dot(dial,"TimeRemaining",0,0,165,Cyan).GetComponent<UnityEngine.UI.Image>();
            clockSweep.type = UnityEngine.UI.Image.Type.Filled;
            clockSweep.fillMethod = UnityEngine.UI.Image.FillMethod.Radial360;
            clockSweep.fillOrigin = (int)UnityEngine.UI.Image.Origin360.Top;
            Dot(dial,"DialFace",0,0,156,C("142936"));
            var watch = Dot(dial,"Chronometer",0,48,27,Cyan);
            Dot(watch,"Inset",0,0,22,C("142936"));
            Panel(watch,"Hand",1,4,3,11,Cyan);
            Panel(watch,"Crown",0,16,10,4,Cyan);
            timer.rectTransform.anchoredPosition = new Vector2(161,793);
            timer.rectTransform.sizeDelta = new Vector2(151,57);
            var caption = stage.Find("Text_TIEMPO DEL TURNO").GetComponent<TMP_Text>();
            caption.rectTransform.anchoredPosition = new Vector2(161,757);
            caption.fontSize = 12;
            Label(stage,"GEMAS\nCRÉDITOS",66,667,120,56,18,Cyan,FontStyles.Bold,TextAlignmentOptions.Left);
            wallet.rectTransform.anchoredPosition = new Vector2(218,667);
            wallet.rectTransform.sizeDelta = new Vector2(96,56);
            wallet.fontSize = 18; wallet.alignment = TextAlignmentOptions.Right;
            var menuRect = (RectTransform)pause.transform;
            menuRect.anchoredPosition = new Vector2(1526,835); menuRect.sizeDelta = new Vector2(86,86);
            pause.image.sprite = circle; pause.image.type = UnityEngine.UI.Image.Type.Simple;
            pauseLabel.gameObject.SetActive(false);
            Dot(menuRect,"Outline",0,0,78,C("76CAD6"));
            Dot(menuRect,"Face",0,0,72,C("1B3043"));
            for (int i=-1;i<=1;i++)
            {
                Dot(menuRect,"Bullet",-18,i*12,5,Cyan);
                Panel(menuRect,"Line",5,i*12,29,4,Cyan);
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
