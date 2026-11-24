using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
namespace ChronosChef
{
    public sealed partial class KitchenView
    {
        [SerializeField] Texture2D homeArt;
        GameObject home, creditsPanel;
        UnityEngine.UI.Button homeContinue, homeTraining, homeOptions, homeCredits;
        UnityEngine.UI.Image transition;
        RectTransform[] homeStars;
        bool transitioning;
        void BuildHome()
        {
            home = ScreenPanel("StartHome");
            if (homeArt)
            {
                var picture = new GameObject("HomeIllustration", typeof(RectTransform), typeof(UnityEngine.UI.RawImage));
                var rect = picture.GetComponent<RectTransform>(); rect.SetParent(home.transform, false); rect.sizeDelta = new Vector2(1600, 900);
                var raw = picture.GetComponent<UnityEngine.UI.RawImage>(); raw.texture = homeArt; raw.raycastTarget = false;
            }
            // Interactive typography and buttons remain independent from the illustration.
            Panel(home.transform, "TitleScrim", -470, 0, 660, 900, new Color(.035f, .035f, .10f, .55f));
            Label(home.transform, "CHRONOS", -730, 286, 660, 120, 79, Cyan, FontStyles.Bold, TextAlignmentOptions.Left);
            Label(home.transform, "CHEF", -730, 190, 660, 120, 103, Color.white, FontStyles.Bold, TextAlignmentOptions.Left);
            Label(home.transform, "COCINA FUERA DE TIEMPO", -723, 100, 610, 50, 22, Pink, FontStyles.Bold, TextAlignmentOptions.Left);
            var play = HomeButton("HomePlay", "JUGAR", -22, Cyan, Ink, () => ScreenTransition(() => game.GoToMenu()));
            homeContinue = HomeButton("HomeContinue", "CONTINUAR TURNO", -112, C("394C61"), Gold, () => ScreenTransition(() => { if (!game.ResumeSavedShift()) ShowHome(); }));
            homeTraining = HomeButton("HomeTraining", "ENTRENAMIENTO", -202, C("273D54"), Cyan, () => { home.SetActive(false); game.GoToMenu(); RequestLevel(1, true); });
            homeOptions = HomeButton("HomeOptions", "AJUSTES", -292, C("273D54"), Color.white, () => { settings.SetActive(true); settings.transform.SetAsLastSibling(); });
            homeCredits = HomeButton("HomeCredits", "CRÉDITOS", -382, C("273D54"), Color.white, () => { creditsPanel.SetActive(true); creditsPanel.transform.SetAsLastSibling(); });
            var trial = Button(home.transform,"TryOrbitalGalley",475,-329,435,72,C("214751"));
            Label(trial.transform,"PRACTICAR EN LA TIERRA",-208,0,416,60,23,Cyan,FontStyles.Bold);
            trial.onClick.AddListener(() => ScreenTransition(StartGalleyTrial));
            Label(home.transform, "PRIMERA BETA · 0.4.0", 300, -403, 430, 30, 15, C("B6D1E6"), FontStyles.Bold, TextAlignmentOptions.Right);
            homeStars = new RectTransform[9];
            for (int i = 0; i < homeStars.Length; i++) homeStars[i] = Dot(home.transform, "ChronoSpark", -740 + i * 69, 350 - i * 67, 3 + i % 3, new Color(.3f, .9f, 1, .45f));
            var back = Button(menu.transform, "HubHome", -640, 350, 155, 45, C("2D4357")); Label(back.transform, "< INICIO", -73, 0, 146, 40, 18, Cyan); back.onClick.AddListener(() => ScreenTransition(ShowHome));
            creditsPanel = ScreenPanel("Credits");
            Label(creditsPanel.transform, "CHRONOS CHEF", -600, 235, 1200, 80, 58, Cyan, FontStyles.Bold);
            Label(creditsPanel.transform, "Cocina fuera de tiempo\n\nUn universo de dodos, plasma y comensales imposibles.\n\nPrimera beta: juego, arte y personajes originales\ncreados para el proyecto Chronos Chef.\n\nGracias por jugar y ayudar a mejorar el próximo servicio.", -600, 0, 1200, 360, 26, C("CBDDEA"));
            ActionButton(creditsPanel.transform, "CloseCredits", "VOLVER", -285, () => creditsPanel.SetActive(false)); creditsPanel.SetActive(false);
            transition = Panel(stage, "PageTransition", 800, 450, 1600, 900, new Color(.025f, .035f, .08f, 0)).GetComponent<UnityEngine.UI.Image>(); transition.raycastTarget = true; transition.gameObject.SetActive(false);
            home.SetActive(false);
        }
        UnityEngine.UI.Button HomeButton(string name, string caption, float y, Color color, Color textColor, UnityAction action)
        {
            var b = Button(home.transform, name, -475, y, 510, 73, color);
            Label(b.transform, caption, -240, 0, 480, 65, 27, textColor, FontStyles.Bold);
            b.onClick.AddListener(() => { audioFeedback.Play("click"); action(); }); return b;
        }
        public void ShowHome()
        {
            if (!home) return;
            menu.SetActive(false); pauseOverlay.SetActive(false); recipes.SetActive(false); settings.SetActive(false); creditsPanel.SetActive(false); SetParadoxVisible(false);
            bool pending = game.Progress.LoadSession() != null; homeContinue.gameObject.SetActive(pending);
            homeTraining.GetComponent<RectTransform>().anchoredPosition = new Vector2(-475, pending ? -202 : -112);
            homeOptions.GetComponent<RectTransform>().anchoredPosition = new Vector2(-475, pending ? -292 : -202);
            homeCredits.GetComponent<RectTransform>().anchoredPosition = new Vector2(-475, pending ? -382 : -292);
            home.SetActive(true); home.transform.SetAsLastSibling();
        }
        void UpdateHome()
        {
            if (!home || !home.activeSelf || homeStars == null) return;
            for (int i = 0; i < homeStars.Length; i++)
                homeStars[i].anchoredPosition = new Vector2(-740 + i * 69 + (ReducedMotion ? 0 : Mathf.Sin(Time.unscaledTime * .4f + i) * 8), 350 - i * 67 + (ReducedMotion ? 0 : Mathf.Sin(Time.unscaledTime * .6f + i) * 8));
        }
        void ScreenTransition(Action next)
        {
            if (transitioning) return;
            StartCoroutine(FadePage(next));
        }
        IEnumerator FadePage(Action next)
        {
            transitioning = true; transition.gameObject.SetActive(true); transition.transform.SetAsLastSibling();
            for (float t = 0; t < .15f; t += Time.unscaledDeltaTime) { transition.color = new Color(.025f, .035f, .08f, t / .15f); yield return null; }
            next(); transition.transform.SetAsLastSibling();
            for (float t = .15f; t > 0; t -= Time.unscaledDeltaTime) { transition.color = new Color(.025f, .035f, .08f, t / .15f); yield return null; }
            transition.gameObject.SetActive(false); transitioning = false;
        }
    }
}
