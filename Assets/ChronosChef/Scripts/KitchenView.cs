using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace ChronosChef
{
    public sealed partial class KitchenView : MonoBehaviour
    {
        [SerializeField] RectTransform stage, chef, clone, carriedIcon;
        [SerializeField] TMP_Text timer, wallet, message, inventory, stoveLabel, anomalyLabel, cloneLabel, taskLabel, pauseLabel, resultText;
        [SerializeField] UnityEngine.UI.Image anomalyFill, cookFill;
        [SerializeField] UnityEngine.UI.Image[] patienceFills;
        [SerializeField] TMP_Text[] customerLabels;
        [SerializeField] UnityEngine.UI.Button[] stations;
        [SerializeField] UnityEngine.UI.Button pause, restart;
        [SerializeField] GameObject results;
        [SerializeField] Sprite rounded, circle;
        [SerializeField] TMP_FontAsset font;
        [SerializeField] RectTransform safeRoot;
        Rect lastSafe; int lastWidth, lastHeight;
        static readonly Color Cyan = C("5CE7EA"), Pink = C("F17BC6"), Gold = C("FFC568"), Ink = C("141B30"), Green = C("98EF9A");
        public static Color C(string hex) { ColorUtility.TryParseHtmlString("#" + hex, out var c); return c; }
        public void Bind(KitchenGame game)
        {
            SetupExpansion(game);
            for (int i = 0; i < stations.Length; i++) { int index = i; stations[i].onClick.AddListener(() => game.SelectStation(index)); }
            pause.onClick.AddListener(game.TogglePause); restart.onClick.AddListener(game.ResetShift);
            ApplySafeArea();
        }
        void Update() { if (Screen.safeArea != lastSafe || Screen.width != lastWidth || Screen.height != lastHeight) ApplySafeArea(); UpdateHome(); UpdateLivingUI(); }
        void ApplySafeArea()
        {
            if (!safeRoot || Screen.width <= 0 || Screen.height <= 0) return;
            lastSafe = Screen.safeArea; lastWidth = Screen.width; lastHeight = Screen.height;
            safeRoot.anchorMin = lastSafe.position / new Vector2(Screen.width, Screen.height);
            safeRoot.anchorMax = (lastSafe.position + lastSafe.size) / new Vector2(Screen.width, Screen.height);
            safeRoot.offsetMin = safeRoot.offsetMax = Vector2.zero;
        }
        public void Refresh(KitchenGame g)
        {
            chef.anchoredPosition = g.ChefPosition;
            chef.localScale = new Vector3(1, 1 + (g.Running && !g.Paused ? Mathf.Sin(Time.time * 7) * .025f : 0), 1);
            clone.gameObject.SetActive(g.CloneRemaining > 0);
            clone.anchoredPosition = g.ClonePosition;
            cloneLabel.text = "ECO TEMPORAL  /  " + Mathf.CeilToInt(g.CloneRemaining) + " s";
            cloneLabel.gameObject.SetActive(g.CloneRemaining > 0);
            timer.text = $"{Mathf.FloorToInt(g.Remaining / 60):00}:{Mathf.FloorToInt(g.Remaining % 60):00}";
            timer.color = g.Remaining < 20 ? Pink : Color.white;
            message.text = g.TutorialActive ? g.TutorialInstruction : g.Paused ? "EN PAUSA · Toca CONTINUAR para volver a la cocina." : g.Message;
            inventory.text = KitchenRules.FoodNames[(int)g.Carry];
            carriedIcon.gameObject.SetActive(g.Carry != Food.Empty);
            anomalyFill.fillAmount = g.Anomaly / 100;
            anomalyLabel.text = $"ANOMALÍA  {g.Anomaly:0}%";
            cookFill.fillAmount = g.StoveFood == Food.Empty ? 0 : Mathf.Clamp01(g.CookTime / g.CookDuration);
            bool ready = g.StoveFood == Food.CookedDodo || g.StoveFood == Food.Soup;
            cookFill.color = g.StoveFood == Food.Burnt ? Pink : ready ? Green : Gold;
            stoveLabel.text = g.StoveFood == Food.Empty ? "FOGÓN · LIBRE" : g.StoveFood == Food.Burnt ? "¡QUEMADO!" : ready ? "¡LISTO! RETIRAR" : $"COCINANDO  {Mathf.CeilToInt(g.CookDuration - g.CookTime)} s";
            taskLabel.text = g.TaskLabel;
            pauseLabel.text = g.Paused ? "CONTINUAR" : "PAUSA";
            for (int i = 0; i < 3; i++)
            {
                patienceFills[i].fillAmount = g.Customers[i] ? g.Patience[i] / Mathf.Max(1, g.MaxPatience[i]) : 0;
                patienceFills[i].color = g.Patience[i] < 20 ? Pink : Green;
                customerLabels[i].text = g.Customers[i] ? g.Orders[i] == Food.Soup ? "SOPA\nCUÁNTICA" : "CYBER-DODO\nBURGER" : "VIAJERO\nEN CAMINO...";
            }
            RefreshExpansion(g);
        }
        public void ShowResults(bool show, KitchenGame g)
        {
            results.SetActive(show);
            if (!show) return;
            int stars = g.Stars;
            resultText.text = g.IsTrainingResult ? "¡ENTRENAMIENTO COMPLETADO!\n\nYa sabes preparar burgers y sopa,\ncocinar y entregar al cliente correcto.\n\nEn un turno real el reloj y los clientes\nno esperarán: ¡la paradoja tampoco!\n\nPulsa INICIAR TURNO para comenzar." : (stars > 0 ? "¡TURNO SUPERADO!" : "TURNO FALLIDO") + $"\n\n{stars} / 3 ESTRELLAS\n\n{g.Served}/{g.Target} pedidos · {g.Lost} clientes perdidos\n{g.Credits} créditos guardados · {g.Progress.Gems} gemas\n\n" + (stars == 3 ? "¡La prehistoria pide otra ronda!" : "El tiempo vuela. El dodo, ya no.");
            RefreshResultActions(g);
        }

        // Editor scene builder. Every generated visual remains editable in the hierarchy.
        public void Build(TMP_FontAsset typeface, Sprite roundedSprite, Sprite circleSprite)
        {
            font = typeface; rounded = roundedSprite; circle = circleSprite;
            var canvas = gameObject.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = gameObject.AddComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1600, 900); scaler.matchWidthOrHeight = .5f;
            gameObject.AddComponent<GraphicRaycaster>();
            safeRoot = new GameObject("SafeArea", typeof(RectTransform)).GetComponent<RectTransform>(); safeRoot.SetParent(transform, false); safeRoot.anchorMin = Vector2.zero; safeRoot.anchorMax = Vector2.one; safeRoot.sizeDelta = Vector2.zero;
            var fit = new GameObject("LandscapeFrame", typeof(RectTransform), typeof(AspectRatioFitter)).GetComponent<RectTransform>(); fit.SetParent(safeRoot, false);
            var fitter = fit.GetComponent<AspectRatioFitter>(); fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent; fitter.aspectRatio = 16f / 9f;
            // Scaling a fixed design inside a fitted frame keeps touch targets and routes aligned.
            stage = new GameObject("KitchenStage", typeof(RectTransform)).GetComponent<RectTransform>(); stage.SetParent(fit, false); stage.anchorMin = stage.anchorMax = stage.pivot = Vector2.zero; stage.anchoredPosition = Vector2.zero; stage.sizeDelta = new Vector2(1600, 900);
            var scaling = fit.gameObject.AddComponent<KitchenStageScale>(); scaling.Configure(stage);
            Panel(stage, "Backdrop", 800, 450, 1600, 900, C("101728"));
            for (int i = 0; i < 18; i++) Dot(stage, "DistantStar", 40 + (i * 337 % 1500), 180 + (i * 173 % 700), 3 + i % 3, C("384664"));
            Label(stage, "CHRONOS", 55, 829, 335, 58, 51, Cyan, FontStyles.Bold, TextAlignmentOptions.Left);
            Label(stage, "CHEF /", 55, 775, 300, 58, 51, Color.white, FontStyles.Bold, TextAlignmentOptions.Left);
            Label(stage, "COCINA FUERA DE TIEMPO", 58, 724, 330, 32, 18, Pink, FontStyles.Bold, TextAlignmentOptions.Left);
            Panel(stage, "EraBadge", 185, 675, 255, 36, C("28334D")); Label(stage, "01 / PREHISTORIA NEÓN", 60, 675, 250, 30, 19, Gold);
            wallet = Label(stage, "CRÉDITOS  0000    /    SERVIDOS  0", 430, 863, 795, 40, 21, Gold, FontStyles.Bold, TextAlignmentOptions.Left);
            pause = Button(stage, "Pause", 1450, 856, 180, 48, C("2B3852")); pauseLabel = Label(pause.transform, "PAUSA", -80, 0, 160, 40, 21, Color.white);

            Panel(stage, "KitchenBorder", 850, 395, 1110, 405, Cyan);
            Panel(stage, "KitchenFloor", 850, 395, 1100, 395, C("202C42"));
            for (int x = 0; x < 12; x++) for (int y = 0; y < 4; y++)
            {
                var p = KitchenRules.Position(new Vector2Int(x, y));
                Panel(stage, "Floor_" + x + "_" + y, p.x, p.y, 77, 77, (x + y) % 2 == 0 ? C("30394E") : C("354056"));
                Panel(stage, "TileLight", p.x - 26, p.y - 31, 12, 2, C("495972"));
            }
            Panel(stage, "CounterShadow", 898, 625, 1025, 55, C("080F20"));
            Panel(stage, "Counter", 898, 638, 1035, 53, C("56677B"));
            Panel(stage, "CounterEdge", 898, 618, 1025, 5, Pink);
            patienceFills = new UnityEngine.UI.Image[3]; customerLabels = new TMP_Text[3]; stations = new UnityEngine.UI.Button[8];
            var names = new[] { "GROG / HAMBRE JURÁSICA", "BIP-08 / ERROR: HAMBRE", "ZORG / CRÍTICO GALÁCTICO" };
            for (int i = 0; i < 3; i++)
            {
                float x = 630 + i * 270;
                Panel(stage, "OrderRim", x, 783, 252, 115, i == 1 ? Pink : Cyan);
                var card = Panel(stage, "OrderCard", x, 783, 247, 110, C("1D2B43"));
                Burger(card, -75, 5, .8f);
                customerLabels[i] = Label(card, "CYBER-DODO\nBURGER", -20, 16, 139, 45, 18, Color.white, FontStyles.Bold);
                Label(card, names[i], -112, -28, 224, 25, 14, Cyan);
                patienceFills[i] = Bar(stage, "Patience", x, 719, 240, 7, Green);
                Avatar(stage, x, 655, i + 1, false);
                stations[5 + i] = Button(stage, "ServeCustomer" + i, x, 730, 250, 222, new Color(1, 1, 1, 0));
            }
            stations[0] = StationButton("DodoSupply", 175, 438, 205, 150, Cyan, "01 / DODO", "ALMACÉN CRUDO");
            Meat(stations[0].transform, 0, 6);
            stations[1] = StationButton("CutStation", 600, 560, 205, 115, Cyan, "02 / CORTE", "CUCHILLA LÁSER");
            var knife = Panel(stations[1].transform, "KnifeBlade", -8, 3, 85, 21, C("D0EAF1")); knife.localEulerAngles = new Vector3(0, 0, -23); Panel(knife, "Handle", 55, 0, 34, 16, C("B27864"));
            stations[2] = StationButton("PlasmaStove", 920, 560, 235, 115, Pink, "03 / FOGÓN", "");
            Dot(stations[2].transform, "PlasmaRing", 0, 5, 68, Pink); Dot(stations[2].transform, "Plasma", 0, 5, 52, C("384E65")); Dot(stations[2].transform, "Energy", 0, 5, 29, Green);
            stoveLabel = Label(stage, "FOGÓN · LIBRE", 800, 478, 240, 28, 18, Green, FontStyles.Bold);
            cookFill = Bar(stage, "Cooking", 920, 498, 225, 9, Gold);
            stations[3] = StationButton("Assembly", 1450, 425, 205, 150, Gold, "04 / MONTAJE", "PAN ESPACIAL"); Burger(stations[3].transform, 0, 5, 1);
            stations[4] = StationButton("QuantumBin", 175, 240, 205, 105, C("73849D"), "BASURA", "OTRA DIMENSIÓN");
            Panel(stations[4].transform, "Bin", 0, 2, 39, 39, C("6B7D91")); Panel(stations[4].transform, "Lid", 0, 25, 50, 7, Cyan);

            Panel(stage, "AnomalyPanel", 185, 588, 255, 92, C("28233E"));
            anomalyLabel = Label(stage, "ANOMALÍA  0%", 65, 607, 240, 30, 22, Pink, FontStyles.Bold);
            anomalyFill = Bar(stage, "Anomaly", 185, 576, 225, 14, Pink);
            Label(stage, "ERRORES = CAOS TEMPORAL", 60, 549, 250, 26, 14, C("ACBBD1"));
            clone = Avatar(stage, 920, 325, 0, true); clone.gameObject.SetActive(false);
            chef = Avatar(stage, 760, 325, 0, false);
            carriedIcon = new GameObject("CarriedFood", typeof(RectTransform)).GetComponent<RectTransform>(); carriedIcon.SetParent(chef, false); carriedIcon.anchoredPosition = new Vector2(43, 12); Burger(carriedIcon, 0, 0, .55f);
            taskLabel = Label(chef, "", -100, 100, 200, 35, 20, Gold, FontStyles.Bold);
            cloneLabel = Label(stage, "", 520, 200, 650, 35, 21, Pink, FontStyles.Bold);

            Panel(stage, "BottomPanel", 800, 90, 1488, 125, C("1D293E"));
            Label(stage, "EN MANOS", 80, 122, 230, 28, 16, Cyan, FontStyles.Bold, TextAlignmentOptions.Left);
            inventory = Label(stage, "Manos libres", 80, 85, 340, 40, 27, Color.white, FontStyles.Bold, TextAlignmentOptions.Left);
            message = Label(stage, "¡Turno abierto!", 445, 105, 825, 80, 24, C("D1DEEF"), FontStyles.Normal, TextAlignmentOptions.Left);
            timer = Label(stage, "02:00", 1320, 100, 205, 65, 53, Gold, FontStyles.Bold);
            Label(stage, "TIEMPO DEL TURNO", 1310, 53, 220, 30, 15, Cyan);
            Label(stage, "PROTOTIPO 0.1  /  TOCA UNA ESTACIÓN PARA CAMINAR E INTERACTUAR", 400, 164, 990, 24, 15, C("8498B5"));

            results = Panel(stage, "ShiftResults", 800, 450, 1600, 900, new Color(.025f, .04f, .09f, .95f)).gameObject;
            results.GetComponent<UnityEngine.UI.Image>().raycastTarget = true;
            Panel(results.transform, "ResultBorder", 0, 0, 880, 590, Cyan); Panel(results.transform, "ResultCard", 0, 0, 874, 584, Ink);
            resultText = Label(results.transform, "TURNO COMPLETADO", -405, 55, 810, 400, 32, Color.white, FontStyles.Bold);
            restart = Button(results.transform, "Restart", 0, -210, 420, 70, Cyan); Label(restart.transform, "OTRO SALTO TEMPORAL", -200, 0, 400, 60, 25, Ink, FontStyles.Bold);
            results.SetActive(false);
        }
        UnityEngine.UI.Button StationButton(string name, float x, float y, float w, float h, Color accent, string title, string subtitle)
        {
            Panel(stage, name + "Glow", x, y - 5, w + 7, h + 9, accent * .55f);
            var b = Button(stage, name, x, y, w, h, C("25344B"));
            Label(b.transform, title, -w / 2 + 5, h / 2 - 20, w - 10, 30, 20, accent, FontStyles.Bold);
            Label(b.transform, subtitle, -w / 2 + 5, -h / 2 + 18, w - 10, 25, 14, C("B0C6D8"));
            return b;
        }
        RectTransform Panel(Transform parent, string name, float x, float y, float w, float h, Color color, bool round = true)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image)); var r = go.GetComponent<RectTransform>(); r.SetParent(parent, false); if (parent == stage) r.anchorMin = r.anchorMax = Vector2.zero; r.anchoredPosition = new Vector2(x, y); r.sizeDelta = new Vector2(w, h);
            var image = go.GetComponent<UnityEngine.UI.Image>(); image.color = color; image.raycastTarget = false; image.sprite = round ? rounded : null; image.type = UnityEngine.UI.Image.Type.Sliced;
            return r;
        }
        RectTransform Dot(Transform p, string name, float x, float y, float size, Color color)
        { var r = Panel(p, name, x, y, size, size, color); r.GetComponent<UnityEngine.UI.Image>().sprite = circle; return r; }
        TMP_Text Label(Transform p, string text, float left, float y, float w, float h, float size, Color color, FontStyles style = FontStyles.Normal, TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            var go = new GameObject("Text_" + text, typeof(RectTransform), typeof(TextMeshProUGUI)); var r = go.GetComponent<RectTransform>(); r.SetParent(p, false); if (p == stage) r.anchorMin = r.anchorMax = Vector2.zero; r.anchoredPosition = new Vector2(left + w / 2, y); r.sizeDelta = new Vector2(w, h);
            var t = go.GetComponent<TextMeshProUGUI>(); t.font = font; t.text = text; t.fontSize = size; t.color = color; t.fontStyle = style; t.alignment = align; t.raycastTarget = false;
            return t;
        }
        UnityEngine.UI.Button Button(Transform p, string name, float x, float y, float w, float h, Color color)
        {
            var r = Panel(p, name, x, y, w, h, color); r.GetComponent<UnityEngine.UI.Image>().raycastTarget = true;
            var b = r.gameObject.AddComponent<UnityEngine.UI.Button>(); b.targetGraphic = r.GetComponent<UnityEngine.UI.Image>(); var colors = b.colors; colors.highlightedColor = new Color(1.2f, 1.2f, 1.2f); colors.pressedColor = new Color(.7f, .9f, 1); b.colors = colors; b.navigation = new Navigation { mode = Navigation.Mode.None }; return b;
        }
        UnityEngine.UI.Image Bar(Transform p, string name, float x, float y, float w, float h, Color color)
        {
            var background = Panel(p, name, x, y, w, h, C("101B2C")); var fill = Panel(background, "Fill", 0, 0, w, h, color).GetComponent<UnityEngine.UI.Image>(); fill.type = UnityEngine.UI.Image.Type.Filled; fill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal; fill.fillOrigin = 0; return fill;
        }
        void Burger(Transform p, float x, float y, float scale)
        {
            var r = new GameObject("BurgerIcon", typeof(RectTransform)).GetComponent<RectTransform>(); r.SetParent(p, false); r.anchoredPosition = new Vector2(x, y); r.localScale = Vector3.one * scale;
            Panel(r, "BunTop", 0, 17, 83, 30, Gold); Panel(r, "Lettuce", 0, -1, 91, 10, Green); Panel(r, "DodoPatty", 0, -11, 82, 14, C("9B5C62")); Panel(r, "BunBottom", 0, -25, 82, 15, C("E59D5A"));
            for (int i = 0; i < 5; i++) Panel(r, "Sesame", -26 + i * 13, 18 + i % 2 * 7, 5, 3, C("FFF0C9"));
        }
        void Meat(Transform p, float x, float y)
        { var r = Dot(p, "Dodo", x, y, 65, C("EC8792")); Dot(r, "Steak", 0, 0, 43, C("BA516C")); Dot(r, "Bone", 9, 0, 13, C("FFE7CD")); }
        RectTransform Avatar(Transform p, float x, float y, int type, bool ghost)
        {
            var root = new GameObject(ghost ? "ParadoxClone" : type == 0 ? "ChefAres" : "Customer" + type, typeof(RectTransform)).GetComponent<RectTransform>(); root.SetParent(p, false); if (p == stage) root.anchorMin = root.anchorMax = Vector2.zero; root.anchoredPosition = new Vector2(x, y);
            var shadow = Dot(root, "Shadow", 0, -28, 75, new Color(0, 0, 0, .28f)); shadow.sizeDelta = new Vector2(83, 22);
            Color skin = type == 1 ? C("D89A74") : type == 2 ? C("7DBCC9") : type == 3 ? Green : C("F0B194");
            Panel(root, "BootLeft", -17, -20, 24, 25, Ink); Panel(root, "BootRight", 17, -20, 24, 25, Ink);
            Panel(root, "BodyOutline", 0, 6, 75, 69, Ink); Panel(root, "Body", 0, 9, 65, 62, type == 3 ? Pink : C("657998"));
            Panel(root, "Apron", 0, 4, 39, 43, type == 0 ? C("B3E9DC") : C("8CA6BF")); Panel(root, "Belt", 0, -9, 65, 8, Pink);
            Dot(root, "HandLeft", -38, 2, 20, skin); Dot(root, "HandRight", 38, 2, 20, skin);
            Dot(root, "HeadOutline", 0, 54, 67, Ink); Dot(root, "Head", 0, 55, 58, skin);
            if (type == 2) { Panel(root, "RobotFace", 0, 57, 54, 41, Ink); Panel(root, "Antenna", 0, 93, 5, 20, Cyan); Dot(root, "AntennaTip", 0, 104, 10, Pink); }
            else if (type != 3) { Panel(root, "Hair", 0, 80, 58, 20, C("634C58")); if (type == 0) { Panel(root, "Goggles", 0, 76, 59, 15, Ink); Dot(root, "LensL", -15, 77, 14, Cyan); Dot(root, "LensR", 15, 77, 14, Cyan); } }
            Dot(root, "EyeL", -13, 57, 8, type == 2 ? Cyan : Ink); Dot(root, "EyeR", 13, 57, 8, type == 2 ? Cyan : Ink);
            if (type == 3) { Dot(root, "ThirdEye", 0, 76, 15, Color.white); Dot(root, "ThirdPupil", 0, 76, 7, Ink); }
            Panel(root, "Mouth", 0, 42, 16, 4, Ink);
            if (ghost) { var group = root.gameObject.AddComponent<CanvasGroup>(); group.alpha = .55f; group.blocksRaycasts = false; foreach (var graphic in root.GetComponentsInChildren<UnityEngine.UI.Image>()) graphic.color = Pink; }
            return root;
        }
    }
}
