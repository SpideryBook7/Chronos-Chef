using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
namespace ChronosChef
{
    public sealed partial class KitchenView
    {
        [SerializeField] Texture2D chefAnimationSheet;
        UnityEngine.UI.Button resumeShift;
        TMP_Text resumeCaption, confirmationTitle, confirmationBody, motionCaption;
        GameObject confirmation;
        UnityAction confirmed;
        UnityEngine.UI.Image animatedChef;
        Sprite[] chefFrames;
        readonly bool[] wasPresent = new bool[3];
        readonly float[] arrival = new float[3];
        public bool ReducedMotion { get; private set; }

        void BuildBetaShell()
        {
            ReducedMotion = PlayerPrefs.GetInt("ChronosChef.reducedMotion", 0) == 1;
            menuBank.rectTransform.anchoredPosition = new Vector2(-130, 185); menuBank.rectTransform.sizeDelta = new Vector2(940, 45); menuBank.fontSize = 22;
            resumeShift = Button(menu.transform, "ResumeShift", 510, 185, 230, 48, C("345B53")); resumeCaption = Label(resumeShift.transform, "RECUPERAR", -110, 0, 220, 42, 18, Green, FontStyles.Bold);
            resumeShift.onClick.AddListener(() => { if (!game.ResumeSavedShift()) MenuNotice("No hay un turno pendiente válido."); });
            var settingsControl = menu.transform.Find("OpenSettings").GetComponent<RectTransform>(); settingsControl.anchoredPosition = new Vector2(-270, -350); settingsControl.sizeDelta = new Vector2(480, 50);
            var tutorial = Button(menu.transform, "Training", 270, -350, 480, 50, C("2D4B56")); Label(tutorial.transform, "ENTRENAMIENTO", -230, 0, 460, 45, 19, Cyan, FontStyles.Bold);
            tutorial.onClick.AddListener(() => RequestLevel(1, true));
            Label(menu.transform, "BETA 0.3 · GUARDADO LOCAL · CONTROLES POR TOQUE", -600, -385, 1200, 25, 13, C("8FAEC6"));
            var motion = Button(settings.transform, "ReducedMotion", 0, -80, 700, 46, C("354056")); motionCaption = Label(motion.transform, "", -340, 0, 680, 42, 20, Cyan);
            motion.onClick.AddListener(() => { ReducedMotion = !ReducedMotion; PlayerPrefs.SetInt("ChronosChef.reducedMotion", ReducedMotion ? 1 : 0); RefreshMotionCaption(); }); RefreshMotionCaption();
            confirmation = ScreenPanel("ConfirmNewShift");
            confirmationTitle = Label(confirmation.transform, "", -600, 160, 1200, 80, 42, Gold, FontStyles.Bold);
            confirmationBody = Label(confirmation.transform, "", -570, 30, 1140, 100, 25, Color.white);
            ActionButton(confirmation.transform, "ConfirmYes", "SÍ, COMENZAR", -100, () => { confirmation.SetActive(false); var action = confirmed; confirmed = null; action?.Invoke(); });
            ActionButton(confirmation.transform, "ConfirmNo", "CANCELAR", -200, () => { confirmed = null; confirmation.SetActive(false); }); confirmation.SetActive(false);
            BuildHome();
            if (chefAnimationSheet)
            {
                chefFrames = new Sprite[8]; float w = chefAnimationSheet.width / 4f, h = chefAnimationSheet.height / 2f;
                for (int i = 0; i < 8; i++) { chefFrames[i] = Sprite.Create(chefAnimationSheet, new Rect(i % 4 * w, (1 - i / 4) * h, w, h), new Vector2(.5f, 0), 100, 0, SpriteMeshType.FullRect); castSprites.Add(chefFrames[i]); }
                animatedChef = chef.Find("IllustratedCharacter").GetComponent<UnityEngine.UI.Image>();
            }
        }
        void RefreshMotionCaption() { motionCaption.text = "MOVIMIENTO REDUCIDO: " + (ReducedMotion ? "SÍ" : "NO"); }
        public void HandleBack()
        {
            if (transitioning) return;
            if (privacy && privacy.activeSelf) { privacy.SetActive(false); return; }
            if (confirmation && confirmation.activeSelf) { confirmation.SetActive(false); confirmed = null; return; }
            if (recipes && recipes.activeSelf) { CloseRecipeBook(); return; }
            if (settings && settings.activeSelf) { settings.SetActive(false); PlayerPrefs.Save(); return; }
            if (game.Running) game.TogglePause(); else if (results.activeSelf) game.GoToMenu();
            else if (creditsPanel && creditsPanel.activeSelf) creditsPanel.SetActive(false);
            else if (menu.activeSelf) ShowHome();
        }
        void Confirm(string title, string body, UnityAction action)
        {
            confirmationTitle.text = title; confirmationBody.text = body; confirmed = action; confirmation.SetActive(true); confirmation.transform.SetAsLastSibling();
        }
        void RequestLevel(int level, bool guided)
        {
            if (game.Progress.LoadSession() != null) Confirm("¿COMENZAR OTRO TURNO?", "Hay una partida pendiente. El nuevo turno sustituirá ese guardado.\nPuedes recuperarla desde el menú principal.", () => game.BeginLevel(level, guided));
            else game.BeginLevel(level, guided);
        }
        void RefreshBetaHub()
        {
            if (!resumeShift) return;
            var session = game.Progress.LoadSession(); resumeShift.gameObject.SetActive(session != null && session.level==briefLevel);
            if (session != null) resumeCaption.text = "RECUPERAR NIVEL " + session.level;
        }
        void RefreshResultActions(KitchenGame g)
        {
            var next = results.transform.Find("NextLevel"); if (!next) return;
            var caption = next.GetComponentInChildren<TMP_Text>();
            caption.text = IsTrial ? "REPETIR PRUEBA" : g.IsTrainingResult ? "INICIAR TURNO" : g.Level == KitchenLevel.Count ? "CAMPAÑA COMPLETA" : g.Level + 1 > g.Progress.Unlocked || g.Level == 5 && g.Progress.Era < 2 ? "VOLVER AL PORTAL" : "SIGUIENTE NIVEL";
        }
        void RefreshBeta(KitchenGame g)
        {
            if (g.TutorialActive)
            {
                goal.text = "ENTRENAMIENTO " + (g.TutorialStep + 1) + "/" + g.TutorialTotal + " · SIN LÍMITE DE TIEMPO";
                goal.fontSize = 20;
                timer.text = "APRENDE"; timer.fontSize = 27;
            }
            else
            {
                timer.fontSize = 39;
                goal.fontSize = 17;
                goal.text = $"NIVEL {g.Level} · PEDIDOS {g.Served}/{g.Target}";
            }
            if (environmentArt)
                for (int i = 0; i < stations.Length; i++)
                {
                    if (g.TutorialActive) stations[i].GetComponent<UnityEngine.UI.Image>().color = i == (int)g.TutorialStation ? new Color(.3f, 1, .9f, ReducedMotion ? .18f : .12f + Mathf.Sin(Time.unscaledTime * 5) * .06f) : Color.clear;
                    else if (i < 5 || i == 8) stations[i].GetComponent<UnityEngine.UI.Image>().color = Color.clear;
                }
            float dt = g.Running && !g.Paused ? Time.deltaTime : 0;
            for (int i = 0; i < 3; i++)
            {
                if (wasPresent[i] != g.Customers[i]) { wasPresent[i] = g.Customers[i]; arrival[i] = 0; }
                arrival[i] = Mathf.Min(1, arrival[i] + dt * 3);
                var art = customerArt[i][g.Species[i]];
                if (!ReducedMotion && g.Running) art.anchoredPosition += new Vector2((g.Customers[i] ? 1 - arrival[i] : -arrival[i]) * 80, 0);
                art.GetComponent<CanvasGroup>().alpha = g.Customers[i] ? ReducedMotion ? 1 : arrival[i] : Mathf.Max(0, 1 - arrival[i] * 2);
                stations[5 + i].interactable = g.Customers[i];
                if (ReducedMotion) art.localEulerAngles = Vector3.zero;
            }
            if (!ares && animatedChef && chefFrames != null)
            {
                bool cooking = g.IsWorking && !g.IsMoving && Vector2.Distance(g.ChefPosition, KitchenRules.Position(KitchenRules.Approach(Station.Cut))) < 2 || g.StoveFood != Food.Empty && !g.IsMoving && Vector2.Distance(g.ChefPosition, KitchenRules.Position(KitchenRules.Approach(Station.Stove))) < 2;
                int pair = g.IsMoving ? 2 : cooking ? 4 : g.Anomaly >= 65 || g.Remaining < 20 ? 6 : 0;
                float rate = pair == 2 ? 7 : pair == 4 ? 4 : 1.5f;
                int frame = pair + (g.Running && !g.Paused ? (int)(Time.time * rate) % 2 : 0);
                animatedChef.sprite = chefFrames[frame];
            }
            if (ReducedMotion)
            {
                chef.localEulerAngles = Vector3.zero; chef.localScale = Vector3.one;
                foreach (var spark in workSparks) spark.gameObject.SetActive(false);
                if (g.CloneRemaining > 0) { clone.localEulerAngles = Vector3.zero; clone.GetComponent<CanvasGroup>().alpha = .55f; }
            }
        }
    }
}
