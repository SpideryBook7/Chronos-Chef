using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace ChronosChef
{
    public sealed partial class KitchenView
    {
        [SerializeField] Texture2D castAtlas;
        readonly System.Collections.Generic.List<Sprite> castSprites = new System.Collections.Generic.List<Sprite>();
        KitchenGame game;
        KitchenAudio audioFeedback;
        GameObject menu, pauseOverlay, recipes, settings, privacy;
        TMP_Text menuBank, menuMessage, goal, comboLabel, feedbackLabel, speedText, heatText, eraText;
        TMP_Text[] levelTexts, speechLabels, speciesLabels;
        UnityEngine.UI.Button[] levelButtons;
        RectTransform[][] customerArt;
        RectTransform[] burgers, bowls;
        readonly UnityEngine.UI.Image[] toastOrders = new UnityEngine.UI.Image[3];
        RectTransform[] carriedFood;
        RectTransform[] workSparks;
        RectTransform[] routeDots, steam, stoveFoods;
        UnityEngine.UI.Image workFill, burnFill;
        int shownCarry = -1, shownEra = -1;
        float pop, pulse;
        Vector2 lastChef;
        readonly string[] speciesNames = { "GROG / CAVERNÍCOLA", "BIP-08 / ROBOT", "ZORG / GOURMET", "REX / RAPTOR" };

        void SetupExpansion(KitchenGame owner)
        {
            InitializeFoodArt();
            game = owner; audioFeedback = gameObject.AddComponent<KitchenAudio>(); audioFeedback.Initialize(game);
            game.Feedback += Cue;
            System.Array.Resize(ref stations, 9);
            // Fix the reported delivery bug: body, head AND order card share one hit target.
            for (int i = 0; i < 3; i++)
            { var r = (RectTransform)stations[5 + i].transform; r.anchoredPosition = new Vector2(630 + i * 270, 730); r.sizeDelta = new Vector2(250, 222); }
            var supply = (RectTransform)stations[0].transform; supply.anchoredPosition = new Vector2(175, 366); supply.sizeDelta = new Vector2(205, 110);
            stage.Find("DodoSupplyGlow").GetComponent<RectTransform>().anchoredPosition = new Vector2(175, 361);
            stage.Find("DodoSupplyGlow").GetComponent<RectTransform>().sizeDelta = new Vector2(212, 119);
            foreach (var text in supply.GetComponentsInChildren<TMP_Text>()) { var r = (RectTransform)text.transform; r.anchoredPosition = new Vector2(r.anchoredPosition.x, text.text.Contains("DODO") ? 36 : -38); }
            supply.Find("Dodo").localScale = Vector3.one * .7f;
            stations[8] = StationButton("RootSupply", 175, 490, 205, 110, Green, "RAÍCES ×2", "SOPA CUÁNTICA"); RootIcon(stations[8].transform, 0, 0, .7f);

            goal = stage.GetComponentsInChildren<TMP_Text>(true).First(t => t.text.StartsWith("PROTOTIPO"));
            goal.fontSize = 19; goal.color = Gold;
            comboLabel = Label(stage, "", 1345, 325, 220, 44, 20, Gold, FontStyles.Bold);
            workFill = Bar(chef, "CuttingProgress", 0, 108, 100, 7, Gold);
            burnFill = Bar(stage, "BurnWindow", 920, 484, 225, 5, Pink);
            var help = Button(stage, "RecipeHelp", 1450, 253, 180, 54, C("2C4055")); Label(help.transform, "RECETAS", -85, 0, 170, 45, 20, Cyan, FontStyles.Bold);
            help.onClick.AddListener(OpenRecipeBook);
            feedbackLabel = Label(stage, "", 480, 390, 770, 70, 34, Gold, FontStyles.Bold);
            workSparks = new RectTransform[10];
            for (int i = 0; i < 10; i++) { workSparks[i] = Dot(stage, "TemporalSpark", 0, 0, 8, i % 2 == 0 ? Cyan : Gold); workSparks[i].gameObject.SetActive(false); }
            routeDots = new RectTransform[20];
            for (int i = 0; i < routeDots.Length; i++) { routeDots[i] = Dot(stage, "RouteDot", 0, 0, 10, Cyan); routeDots[i].gameObject.SetActive(false); }
            steam = new RectTransform[6];
            for (int i = 0; i < steam.Length; i++) { steam[i] = Dot(stage, "PlasmaSteam", 900, 570, 12, Cyan); steam[i].gameObject.SetActive(false); }
            stoveFoods = new RectTransform[KitchenRules.FoodNames.Length];
            for (int i = 1; i < stoveFoods.Length; i++)
            {
                var holder = new GameObject("Stove_" + (Food)i, typeof(RectTransform)).GetComponent<RectTransform>(); holder.SetParent(stations[2].transform, false); holder.localScale = Vector3.one * .45f;
                DrawFood(holder, (Food)i); holder.gameObject.SetActive(false); stoveFoods[i] = holder;
            }

            carriedIcon.Find("BurgerIcon").gameObject.SetActive(false);
            carriedFood = new RectTransform[KitchenRules.FoodNames.Length];
            for (int i = 1; i < carriedFood.Length; i++)
            {
                var holder = new GameObject("Carry_" + (Food)i, typeof(RectTransform)).GetComponent<RectTransform>(); holder.SetParent(carriedIcon, false); holder.localScale = Vector3.one * .55f;
                DrawFood(holder, (Food)i); holder.gameObject.SetActive(false); carriedFood[i] = holder;
            }

            customerArt = new RectTransform[3][]; burgers = new RectTransform[3]; bowls = new RectTransform[3];
            speechLabels = new TMP_Text[3]; speciesLabels = new TMP_Text[3];
            var cards = stage.Cast<Transform>().Where(t => t.name == "OrderCard").ToArray();
            for (int i = 0; i < 3; i++)
            {
                var card = cards[i]; burgers[i] = card.Find("BurgerIcon").GetComponent<RectTransform>();
                ReplaceFoodArt(burgers[i], Food.Burger);
                bowls[i] = new GameObject("SoupIcon", typeof(RectTransform)).GetComponent<RectTransform>(); bowls[i].SetParent(card, false); bowls[i].anchoredPosition = new Vector2(-75, 5); bowls[i].localScale = Vector3.one * .8f; DrawFood(bowls[i], Food.Soup);
                toastOrders[i] = FoodPicture(card, "ToastOrder", Food.Toast, new Vector2(-75, 5), new Vector2(90, 82));
                speciesLabels[i] = card.GetComponentsInChildren<TMP_Text>().First(t => t != customerLabels[i]);
                // Keep the original editable art as variant 0; add the other temporal visitors.
                var old = stage.Find("Customer" + (i + 1)); old.gameObject.SetActive(false);
                customerArt[i] = new RectTransform[4];
                for (int species = 0; species < 4; species++)
                {
                    var art = Avatar(stage, 630 + i * 270, 652, species == 3 ? 3 : species + 1, false); art.name = "Visitor_" + i + "_" + species;
                    if (species == 0) { Panel(art, "CyberShoulder", -36, 17, 28, 22, Gold); Panel(art, "LaserBone", 0, 91, 62, 7, Cyan).localEulerAngles = new Vector3(0, 0, 18); }
                    if (species == 3) RaptorDetails(art);
                    art.gameObject.AddComponent<CanvasGroup>();
                    customerArt[i][species] = art;
                }
                speechLabels[i] = Label(stage, "", 505 + i * 270, 690, 250, 30, 13, Gold, FontStyles.Bold);
            }
            ApplyIllustrations();
            InstallEnvironment();
            BuildShell();
            BuildBetaShell();
            BuildLivingKitchen();
            results.transform.SetAsLastSibling();
            // Results now include navigation to the next unlocked level and the kitchen hub.
            var next = Button(results.transform, "NextLevel", 235, -280, 300, 52, Cyan); Label(next.transform, "SIGUIENTE NIVEL", -145, 0, 290, 45, 19, Ink, FontStyles.Bold);
            next.onClick.AddListener(() => { if (game.IsTrainingResult) { game.BeginLevel(1); return; } if (game.Level == 10) { game.GoToMenu(); return; } int level = game.Level + 1; if (level > game.Progress.Unlocked || level > 5 && game.Progress.Era < 2) game.GoToMenu(); else game.BeginLevel(level); });
            var hub = Button(results.transform, "ResultsHub", -235, -280, 300, 52, C("34465D")); Label(hub.transform, "VOLVER AL PORTAL", -145, 0, 290, 45, 19, Color.white, FontStyles.Bold); hub.onClick.AddListener(game.GoToMenu);
            resultText.fontSize = 27; resultText.rectTransform.anchoredPosition = new Vector2(0, 60); resultText.rectTransform.sizeDelta = new Vector2(810, 365);
            restart.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -175);
        }
        void BuildShell()
        {
            menu = ScreenPanel("TemporalHub");
            Label(menu.transform, "CHRONOS CHEF", -600, 300, 1200, 80, 65, Cyan, FontStyles.Bold);
            Label(menu.transform, "EL TIEMPO SE ACABA. LA COCINA NO PERDONA.", -600, 235, 1200, 40, 22, Pink, FontStyles.Bold);
            menuBank = Label(menu.transform, "", -600, 185, 1200, 45, 25, Gold, FontStyles.Bold);
            Label(menu.transform, "ELIGE UN TURNO / PREHISTORIA → STEAMPUNK", -600, 130, 1200, 35, 20, Cyan);
            levelButtons = new UnityEngine.UI.Button[10]; levelTexts = new TMP_Text[10];
            for (int i = 0; i < 10; i++)
            {
                int level = i + 1; float x = -490 + i % 5 * 245, y = i < 5 ? 60 : -35;
                var b = Button(menu.transform, "Level" + level, x, y, 215, 78, C("283C54"));
                levelButtons[i] = b; levelTexts[i] = Label(b.transform, "", -100, 0, 200, 72, 21, Color.white, FontStyles.Bold);
                b.onClick.AddListener(() => { audioFeedback.Play("click"); RequestLevel(level, level == 1 && !game.Progress.TutorialDone); });
            }
            var speed = Button(menu.transform, "BuySpeed", -320, -145, 590, 64, C("2C4254")); speedText = Label(speed.transform, "", -285, 0, 570, 58, 21, Cyan, FontStyles.Bold);
            speed.onClick.AddListener(() => { MenuNotice(game.Progress.Buy(true)); RefreshHub(); });
            var heat = Button(menu.transform, "BuyHeat", 320, -145, 590, 64, C("48364D")); heatText = Label(heat.transform, "", -285, 0, 570, 58, 21, Pink, FontStyles.Bold);
            heat.onClick.AddListener(() => { MenuNotice(game.Progress.Buy(false)); RefreshHub(); });
            var era = Button(menu.transform, "UnlockEra", 0, -225, 760, 58, C("594833")); eraText = Label(era.transform, "", -370, 0, 740, 50, 22, Gold, FontStyles.Bold);
            era.onClick.AddListener(() => { MenuNotice(game.Progress.UnlockEra()); RefreshHub(); });
            menuMessage = Label(menu.transform, "Supera objetivos para abrir niveles. 3 estrellas otorgan una gema por nivel.", -610, -290, 1220, 45, 20, C("BBD1E4"));
            var settingsButton = Button(menu.transform, "OpenSettings", 0, -350, 350, 50, C("2C4055")); Label(settingsButton.transform, "SONIDO Y AYUDA", -170, 0, 340, 45, 19, Color.white); settingsButton.onClick.AddListener(() => { settings.SetActive(true); settings.transform.SetAsLastSibling(); });

            pauseOverlay = ScreenPanel("PauseScreen");
            Label(pauseOverlay.transform, "TIEMPO CONGELADO", -600, 210, 1200, 80, 54, Cyan, FontStyles.Bold);
            ActionButton(pauseOverlay.transform, "Resume", "VOLVER A LA COCINA", 80, () => game.TogglePause());
            ActionButton(pauseOverlay.transform, "PauseRestart", "REINICIAR TURNO", -15, () => Confirm("¿REINICIAR EL TURNO?", "Los pedidos y créditos de este turno se reiniciarán.", game.ResetShift));
            ActionButton(pauseOverlay.transform, "PauseHub", "PORTAL Y MEJORAS", -110, game.GoToMenu);
            ActionButton(pauseOverlay.transform, "PauseSettings", "SONIDO Y AYUDA", -205, () => { settings.SetActive(true); settings.transform.SetAsLastSibling(); });

            recipes = ScreenPanel("RecipeBook");
            Label(recipes.transform, "RECETARIO TEMPORAL", -600, 260, 1200, 75, 47, Cyan, FontStyles.Bold);
            Label(recipes.transform, "CYBER-DODO BURGER", -570, 165, 1140, 50, 32, Gold, FontStyles.Bold);
            Label(recipes.transform, "DODO > CORTE > FOGÓN (5 s) > RETIRAR > MONTAJE > CLIENTE", -610, 100, 1220, 55, 23, Color.white);
            Label(recipes.transform, "SOPA CUÁNTICA DE RAÍCES", -570, 5, 1140, 50, 32, Green, FontStyles.Bold);
            Label(recipes.transform, "RAÍCES (toca dos veces) > FOGÓN (7 s) > RETIRAR BOL > CLIENTE", -610, -60, 1220, 55, 23, Color.white);
            Label(recipes.transform, "Tienes 6 s para retirar la comida lista. Un plato en las manos.\nCada cliente acepta únicamente su pedido. Entrega tocando su cuerpo, cabeza o tarjeta.\nErrores = anomalías. A 100% aparece tu clon por 15 s.\nGrog ruge, el robot chispea, el raptor araña la barra y Zorg juzga tu existencia.", -600, -170, 1200, 130, 21, C("BDD3E6"));
            ActionButton(recipes.transform, "CloseRecipes", "ENTENDIDO", -300, CloseRecipeBook);

            settings = ScreenPanel("Settings");
            Label(settings.transform, "SONIDO Y AYUDA", -600, 250, 1200, 75, 50, Cyan, FontStyles.Bold);
            MakeSlider(settings.transform, "MÚSICA", 100, audioFeedback.MusicGain, audioFeedback.SetMusic);
            MakeSlider(settings.transform, "EFECTOS", -10, audioFeedback.EffectsGain, audioFeedback.SetEffects);
            ActionButton(settings.transform, "SettingsRecipes", "VER RECETAS", -140, OpenRecipeBook);
            ActionButton(settings.transform, "SettingsClose", "GUARDAR Y VOLVER", -245, () => { PlayerPrefs.Save(); settings.SetActive(false); });
            privacy = ScreenPanel("Privacy");
            Label(privacy.transform, "PRIVACIDAD Y DATOS", -600, 245, 1200, 75, 47, Cyan, FontStyles.Bold);
            Label(privacy.transform, "Chronos Chef guarda en este dispositivo tu turno, las estrellas, los créditos y los ajustes de sonido y movimiento. Usa estos datos para continuar donde lo dejaste.\n\nPuedes eliminarlos desde Ajustes de Android → Aplicaciones → Chronos Chef → Almacenamiento → Borrar datos. Esto reinicia todo el progreso local.\n\nPara consultas de privacidad, usa el contacto del desarrollador en la ficha de Chronos Chef de Google Play.", -500, 5, 1000, 340, 25, Color.white);
            ActionButton(privacy.transform, "PrivacyBack", "VOLVER A AJUSTES", -260, () => privacy.SetActive(false));
            privacy.SetActive(false);
            ActionButton(settings.transform, "OpenPrivacy", "PRIVACIDAD Y DATOS", -340, () => { privacy.SetActive(true); privacy.transform.SetAsLastSibling(); });
            pauseOverlay.SetActive(false); recipes.SetActive(false); settings.SetActive(false); menu.SetActive(false);
        }
        GameObject ScreenPanel(string name)
        {
            var r = Panel(stage, name, 800, 450, 1600, 900, new Color(.035f, .055f, .10f, .995f)); r.GetComponent<UnityEngine.UI.Image>().raycastTarget = true;
            Panel(r, "TopAccent", 0, 397, 1450, 4, Cyan); Panel(r, "BottomAccent", 0, -397, 1450, 4, Pink); return r.gameObject;
        }
        void ActionButton(Transform p, string name, string caption, float y, UnityEngine.Events.UnityAction action)
        { var b = Button(p, name, 0, y, 620, 68, C("2A4058")); Label(b.transform, caption, -300, 0, 600, 60, 25, Cyan, FontStyles.Bold); b.onClick.AddListener(() => { audioFeedback.Play("click"); action(); }); }
        void MakeSlider(Transform p, string name, float y, float value, UnityEngine.Events.UnityAction<float> changed)
        {
            Label(p, name, -500, y, 260, 50, 25, Gold, FontStyles.Bold);
            var track = Panel(p, name + "Slider", 130, y, 650, 42, C("2A4058")); track.GetComponent<UnityEngine.UI.Image>().raycastTarget = true;
            var fillArea = Panel(track, "FillArea", 0, 0, 620, 15, C("192638"));
            var fill = Panel(fillArea, "Fill", 0, 0, 620, 15, Cyan);
            var handleArea = new GameObject("HandleArea", typeof(RectTransform)).GetComponent<RectTransform>(); handleArea.SetParent(track, false); handleArea.anchorMin = Vector2.zero; handleArea.anchorMax = Vector2.one; handleArea.offsetMin = new Vector2(15, 0); handleArea.offsetMax = new Vector2(-15, 0);
            var handle = Dot(handleArea, "Handle", 0, 0, 38, Gold); handle.GetComponent<UnityEngine.UI.Image>().raycastTarget = true;
            var slider = track.gameObject.AddComponent<UnityEngine.UI.Slider>(); slider.fillRect = fill; slider.handleRect = handle; slider.targetGraphic = handle.GetComponent<UnityEngine.UI.Image>(); slider.minValue = 0; slider.maxValue = 1; slider.value = value; slider.onValueChanged.AddListener(changed);
        }
        public void ShowMenu(bool show) { if (!menu) return; if (home) home.SetActive(false); menu.SetActive(show); if (show) { SetParadoxVisible(false); menu.transform.SetAsLastSibling(); RefreshHub(); } if (pauseOverlay) pauseOverlay.SetActive(false); if (!show) { recipes.SetActive(false); settings.SetActive(false); } }
        public void MenuNotice(string text) { if (menuMessage) menuMessage.text = text; audioFeedback?.Play("click"); }
        void RefreshHub()
        {
            var p = game.Progress; if (p == null) return;
            menuBank.text = $"{p.Bank} CRÉDITOS    /    {p.Gems} GEMAS    /    PROGRESO GUARDADO";
            for (int i = 0; i < 10; i++) { bool open = i + 1 <= p.Unlocked && (i < 5 || p.Era == 2); levelButtons[i].interactable = open; levelTexts[i].text = $"{(i < 5 ? "JURÁSICO" : "STEAMPUNK")} {i % 5 + 1}\n" + (open ? p.BestStars(i + 1) + "/3 ESTRELLAS" : "BLOQUEADO"); }
            speedText.text = $"BOTAS {p.Speed}/3  /  " + (p.Speed >= 3 ? "MÁXIMO" : 300 * (p.Speed + 1) + " CRÉDITOS");
            heatText.text = $"PLASMA {p.Heat}/3  /  " + (p.Heat >= 3 ? "MÁXIMO" : 300 * (p.Heat + 1) + " CRÉDITOS");
            eraText.text = p.Era == 2 ? "PORTAL STEAMPUNK ABIERTO" : "ABRIR STEAMPUNK / 3 GEMAS";
            RefreshBetaHub();
            RefreshJourneyMap();
        }
        void Cue(string cue)
        {
            pop = .3f; if (cue == "click") return;
            pulse = .8f; feedbackLabel.text = cue == "serve" ? "¡PEDIDO ENTREGADO!" : cue == "paradox" ? "¡PARADOX ALERT!" : cue == "ready" ? "¡COMIDA LISTA!" : cue == "bad" ? "¡CUIDADO!" : "";
            feedbackLabel.color = cue == "bad" || cue == "paradox" ? Pink : Gold;
        }
        void RefreshExpansion(KitchenGame g)
        {
            if (!game) return;
            RefreshEnvironment(g);
            pauseOverlay.SetActive(g.Running && g.Paused);
            if (recipes.activeSelf || settings.activeSelf) pauseOverlay.transform.SetSiblingIndex(Mathf.Min(pauseOverlay.transform.GetSiblingIndex(), recipes.transform.GetSiblingIndex() - 1));
            wallet.text = $"GEMAS  {g.Progress.Gems}\nCRÉDITOS  {g.Credits}";
            goal.text = $"NIVEL {g.Level}  /  PEDIDOS {g.Served}/{g.Target}  /  REPUTACIÓN {g.Reputation}%";
            comboLabel.text = g.Combo > 1 ? "COMBO ×" + g.Combo : "";
            workFill.fillAmount = g.WorkProgress; workFill.transform.parent.gameObject.SetActive(g.TaskLabel == "CORTANDO");
            burnFill.fillAmount = g.StoveFood == Food.CookedDodo || g.StoveFood == Food.Soup ? 1 - (g.CookTime - g.CookDuration) / g.BurnWindow : 0;
            taskLabel.text = g.TaskLabel == "" ? g.DestinationLabel : g.TaskLabel; taskLabel.fontSize = 16; taskLabel.rectTransform.sizeDelta = new Vector2(270, 35); taskLabel.rectTransform.anchoredPosition = new Vector2(0, 130);
            if (shownCarry != (int)g.Carry) { shownCarry = (int)g.Carry; for (int i = 1; i < carriedFood.Length; i++) carriedFood[i].gameObject.SetActive(i == shownCarry); }
            pop = Mathf.Max(0, pop - Time.unscaledDeltaTime); pulse = Mathf.Max(0, pulse - Time.unscaledDeltaTime);
            float lean = Mathf.Clamp(g.ChefPosition.x - lastChef.x, -1, 1); lastChef = g.ChefPosition;
            if (!ares && !g.Paused) { chef.localEulerAngles = new Vector3(0, 0, -lean * 6); chef.localScale *= 1 + pop * .25f; }
            feedbackLabel.gameObject.SetActive(pulse > 0); feedbackLabel.rectTransform.anchoredPosition = new Vector2(865, 410 + (1 - pulse) * 35); feedbackLabel.alpha = Mathf.Min(1, pulse * 3);
            for (int i = 0; i < workSparks.Length; i++) { workSparks[i].gameObject.SetActive(pulse > .25f); if (pulse > .25f) workSparks[i].anchoredPosition = g.ChefPosition + new Vector2(Mathf.Cos(i * 2.4f), Mathf.Sin(i * 2.4f)) * ((.85f - pulse) * 170 + 15); }
            for (int i = 0; i < routeDots.Length; i++) { bool visible = i < g.RoutePreview.Count; routeDots[i].gameObject.SetActive(visible); if (visible) routeDots[i].anchoredPosition = KitchenRules.Position(g.RoutePreview[i]); }
            for (int i = 1; i < stoveFoods.Length; i++) stoveFoods[i].gameObject.SetActive((int)g.StoveFood == i);
            float clock = g.Running && !g.Paused ? Time.time : 0;
            for (int i = 0; i < steam.Length; i++)
            {
                bool visible = g.StoveFood != Food.Empty; steam[i].gameObject.SetActive(visible);
                float f = Mathf.Repeat(clock * .65f + i / 6f, 1);
                steam[i].anchoredPosition = new Vector2(945 + i * 7 + Mathf.Sin(clock * 3 + i) * 9, 538 + f * 85);
                steam[i].localScale = Vector3.one * (1 + f * 1.4f);
                steam[i].GetComponent<UnityEngine.UI.Image>().color = new Color(g.StoveFood == Food.Burnt ? .7f : .5f, .8f, 1, (1 - f) * .5f);
            }
            stations[1].transform.Find("KnifeBlade").localEulerAngles = new Vector3(0, 0, -23 + (g.TaskLabel == "CORTANDO" ? Mathf.Sin(clock * 28) * 20 : 0));
            if (g.CloneRemaining > 0) { clone.localEulerAngles = new Vector3(0, 0, Mathf.Sin(clock * 35) * 4); clone.GetComponent<CanvasGroup>().alpha = .35f + Mathf.Abs(Mathf.Sin(clock * 17)) * .35f; }
            for (int i = 0; i < 3; i++)
            {
                burgers[i].gameObject.SetActive(g.Orders[i] == Food.Burger);
                bowls[i].gameObject.SetActive(g.Orders[i] == Food.Soup);
                toastOrders[i].gameObject.SetActive(g.Orders[i] == Food.Toast);
                speciesLabels[i].text = string.IsNullOrEmpty(g.Speech[i]) ? speciesNames[g.Species[i]] : g.Speech[i];
                speciesLabels[i].fontSize = environmentArt ? 11 : string.IsNullOrEmpty(g.Speech[i]) ? 14 : 12;
                speechLabels[i].gameObject.SetActive(false);
                for (int j = 0; j < 4; j++)
                {
                    var art = customerArt[i][j]; art.gameObject.SetActive(j == g.Species[i]);
                    if (j != g.Species[i]) continue;
                    float t = g.Running && !g.Paused ? Time.time : 0;
                    art.anchoredPosition = VisitorPosition(i, j, t);
                    art.localScale = Vector3.one * (g.Customers[i] ? 1 : .75f);
                    var cg = art.GetComponent<CanvasGroup>(); if (!cg) cg = art.gameObject.AddComponent<CanvasGroup>(); cg.alpha = g.Customers[i] ? 1 : .25f;
                    if (j == 1 && g.Patience[i] < 20 && g.Customers[i]) art.localEulerAngles = new Vector3(0, 0, Mathf.Sin(t * 32) * 5);
                    else art.localEulerAngles = Vector3.zero;
                }
                bool matching = g.Carry == g.Orders[i] && g.Customers[i]; stations[5 + i].GetComponent<UnityEngine.UI.Image>().color = matching ? new Color(.3f, 1, .75f, .12f) : Color.clear;
            }
            if (shownEra != (g.Level > 5 ? 2 : 1))
            {
                shownEra = g.Level > 5 ? 2 : 1;
                stage.Find("KitchenBorder").GetComponent<UnityEngine.UI.Image>().color = shownEra == 2 ? Gold : Cyan;
                stage.Find("KitchenFloor").GetComponent<UnityEngine.UI.Image>().color = shownEra == 2 ? C("493747") : C("202C42");
                var badge = stage.GetComponentsInChildren<TMP_Text>(true).First(t => t.text.Contains("PREHISTORIA") || t.text.Contains("VICTORIANA")); badge.text = shownEra == 2 ? "02 / VICTORIANA STEAMPUNK" : "01 / PREHISTORIA NEÓN";
            }
            RefreshBeta(g);
            RefreshLivingKitchen(g);
        }
        void DrawFood(Transform p, Food food)
        {
            if (TryDrawFoodArt(p, food)) return;
            if (food == Food.Bread) { Panel(p, "BreadTop", 0, 14, 83, 30, Gold); Panel(p, "BreadBottom", 0, -22, 82, 15, C("E59D5A")); return; }
            if (food == Food.Burger) { Burger(p, 0, 0, 1); return; }
            if (food == Food.Soup) { SoupIcon(p); return; }
            if (food == Food.Root || food == Food.Roots) { RootIcon(p, food == Food.Roots ? -14 : 0, 0, 1); if (food == Food.Roots) RootIcon(p, 18, 4, .85f); return; }
            Meat(p, 0, 0);
            if (food == Food.ChoppedDodo) { Panel(p, "Cut1", -10, 0, 4, 50, Cyan); Panel(p, "Cut2", 9, 0, 4, 50, Cyan); }
            if (food == Food.CookedDodo || food == Food.Burnt) foreach (var image in p.GetComponentsInChildren<UnityEngine.UI.Image>()) image.color = food == Food.Burnt ? C("3A2F42") : C("C68B59");
        }
        void SoupIcon(Transform p) { Panel(p, "Bowl", 0, -7, 80, 43, Cyan); var soup = Dot(p, "Soup", 0, 14, 76, Green); soup.sizeDelta = new Vector2(76, 25); Panel(p, "Spoon", 30, 34, 7, 45, Gold).localEulerAngles = new Vector3(0, 0, -25); for (int i = 0; i < 3; i++) Dot(p, "RootChunk", -23 + i * 21, 15 + i % 2 * 3, 9, Pink); }
        void RootIcon(Transform p, float x, float y, float scale) { var r = Panel(p, "NeonRoot", x, y, 27 * scale, 48 * scale, Pink); r.localEulerAngles = new Vector3(0, 0, -18); Panel(r, "LeafL", -9, 28 * scale, 11 * scale, 29 * scale, Green).localEulerAngles = new Vector3(0, 0, 30); Panel(r, "LeafR", 9, 28 * scale, 11 * scale, 29 * scale, Cyan).localEulerAngles = new Vector3(0, 0, -30); }
        void RaptorDetails(RectTransform art)
        {
            foreach (var image in art.GetComponentsInChildren<UnityEngine.UI.Image>()) if (image.name.Contains("Head") || image.name.Contains("Hand")) image.color = Green;
            Panel(art, "Snout", 20, 47, 62, 33, Green); Panel(art, "Visor", 0, 67, 62, 14, Cyan); Panel(art, "Tail", -48, -2, 53, 18, Green).localEulerAngles = new Vector3(0, 0, -25);
            for (int i = 0; i < 3; i++) Panel(art, "Tooth", 6 + i * 13, 31, 6, 8, Gold);
        }
        void ApplyIllustrations()
        {
            if (!castAtlas) return;
            float[] edges = { 0, .183f, .415f, .612f, .807f, 1 };
            for (int i = 0; i < 5; i++)
            {
                var rect = new Rect(Mathf.Round(edges[i] * castAtlas.width), 0, Mathf.Round((edges[i + 1] - edges[i]) * castAtlas.width), castAtlas.height);
                rect.width = Mathf.Min(rect.width, castAtlas.width - rect.x);
                castSprites.Add(Sprite.Create(castAtlas, rect, new Vector2(.5f, 0), 100, 0, SpriteMeshType.FullRect));
            }
            ReplaceCharacter(chef, castSprites[0], 155, 38);
            ReplaceCharacter(clone, castSprites[0], 155, 38);
            for (int i = 0; i < 3; i++) for (int j = 0; j < 4; j++) ReplaceCharacter(customerArt[i][j], castSprites[j + 1], 126, 22);
        }
        void ReplaceCharacter(RectTransform parent, Sprite sprite, float height, float center)
        {
            foreach (Transform child in parent)
                if (child.name != "Shadow" && child.name != "CarriedFood" && child.name != "CuttingProgress" && !child.GetComponent<TMP_Text>()) child.gameObject.SetActive(false);
            var art = Panel(parent, "IllustratedCharacter", 0, center, 145, height, Color.white).GetComponent<UnityEngine.UI.Image>();
            art.sprite = sprite; art.type = UnityEngine.UI.Image.Type.Simple; art.preserveAspect = true; art.raycastTarget = false;
            if (parent == clone) art.color = new Color(1, .5f, 1, .85f);
            if (parent == chef) { carriedIcon.SetAsLastSibling(); taskLabel.transform.SetAsLastSibling(); }
        }
        void OnDestroy() { foreach (var sprite in castSprites) if (sprite) Destroy(sprite); DestroyLivingAssets(); }
    }
}
