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
        bool transitioning;
        void BuildHome()
        {
            home = ScreenPanel("StartHome");
            AnimateArt(PageArt(home,"HomeStillLife"),false);
            Label(home.transform,"Chronos Chef",-695,200,740,130,88,C("373F35"),FontStyles.Bold,TextAlignmentOptions.Left);
            var play = HomeButton("HomePlay", "JUGAR", -22, C("475847"), Color.white, () => ScreenTransition(ShowPlanets));
            homeContinue = HomeButton("HomeContinue", "CONTINUAR TURNO", -112, C("EBDFCB"), C("343D34"), () => ScreenTransition(() => { if (!game.ResumeSavedShift()) ShowHome(); }));
            homeTraining = HomeButton("HomeTraining", "ENTRENAMIENTO", -202, C("EBDFCB"), C("343D34"), () => { home.SetActive(false); game.GoToMenu(); RequestLevel(1, true); });
            homeOptions = HomeButton("HomeOptions", "AJUSTES", -292, C("EBDFCB"), C("343D34"), () => { settings.SetActive(true); settings.transform.SetAsLastSibling(); });
            homeCredits = HomeButton("HomeCredits", "CRÉDITOS", -382, C("EBDFCB"), C("343D34"), () => { creditsPanel.SetActive(true); creditsPanel.transform.SetAsLastSibling(); });
            var trial = Button(home.transform,"TryOrbitalGalley",475,-329,435,72,C("465343"));
            Label(trial.transform,"PRACTICAR EN LA TIERRA",-208,0,416,60,23,Color.white,FontStyles.Bold);
            trial.onClick.AddListener(() => ScreenTransition(StartGalleyTrial));
            var back = Button(menu.transform, "HubHome", -640, 350, 155, 45, C("2D4357")); Label(back.transform, "< PLANETAS", -73, 0, 146, 40, 18, Cyan); back.onClick.AddListener(() => ScreenTransition(ShowPlanets));
            creditsPanel = ScreenPanel("Credits");
            Label(creditsPanel.transform, "CHRONOS CHEF", -600, 235, 1200, 80, 58, Cyan, FontStyles.Bold);
            Label(creditsPanel.transform, "Cocina fuera de tiempo\n\nUn universo de dodos, plasma y comensales imposibles.\n\nPrimera beta: juego, arte y personajes originales\ncreados para el proyecto Chronos Chef.\n\nGracias por jugar y ayudar a mejorar el próximo servicio.", -600, 0, 1200, 360, 26, C("CBDDEA"));
            ActionButton(creditsPanel.transform, "CloseCredits", "VOLVER", -285, () => creditsPanel.SetActive(false)); creditsPanel.SetActive(false);
            transition = Panel(stage, "PageTransition", 800, 450, 1600, 900, new Color(.025f, .035f, .08f, 0)).GetComponent<UnityEngine.UI.Image>(); transition.raycastTarget = true; transition.gameObject.SetActive(false);
            BuildAtlas();
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
            AnimateArrival();
            float phase=ReducedMotion ? 0 : Time.unscaledTime;
            if(daylight && home.activeInHierarchy) daylight.SetFloat("_Phase",phase);
            if(driftingWorlds && atlasPage.activeInHierarchy) driftingWorlds.SetFloat("_Phase",phase);

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
