using System.Linq;
using TMPro;
using UnityEngine;
namespace ChronosChef
{
    public sealed partial class KitchenView
    {
        [SerializeField] Texture2D journeyArt;
        RectTransform[] portalAuras;
        UnityEngine.UI.Image[] routeLines;

        UnityEngine.UI.Image[,] routeStars;
        static readonly Vector2[] RouteNodes =
        {
            new Vector2(205,385), new Vector2(495,520), new Vector2(795,390), new Vector2(1090,540), new Vector2(1385,405),
            new Vector2(205,385), new Vector2(495,520), new Vector2(795,390), new Vector2(1090,540), new Vector2(1385,405)
        };
        void BuildJourneyMap()
        {
            foreach (var text in menu.GetComponentsInChildren<TMP_Text>(true)) if (text.transform.parent == menu.transform && text != menuBank && text != menuMessage) text.gameObject.SetActive(false);
            if (journeyArt)
            {
                var background = new GameObject("ChronoCartography", typeof(RectTransform), typeof(UnityEngine.UI.RawImage)); var r = background.GetComponent<RectTransform>(); r.SetParent(menu.transform, false); r.sizeDelta = new Vector2(1600,900); r.SetAsFirstSibling();
                var image = background.GetComponent<UnityEngine.UI.RawImage>(); image.texture = journeyArt; image.raycastTarget = false;
            }
            Panel(menu.transform,"WorldNamePlate",0,365,365,77,C("EFDFC2"));
            planetTitle=Label(menu.transform, "TIERRA", -340, 365, 680, 72, 42, C("384837"), FontStyles.Bold);
            menuBank.rectTransform.anchoredPosition = new Vector2(610,365); menuBank.rectTransform.sizeDelta = new Vector2(280,80); menuBank.alignment = TextAlignmentOptions.Right; menuBank.fontSize = 18;
            routeLines = new UnityEngine.UI.Image[KitchenLevel.Count-1];
            for (int i = 0; i < KitchenLevel.Count-1; i++)
            {
                var a = RouteNodes[i%5] - new Vector2(800,450); var b = RouteNodes[(i+1)%5] - new Vector2(800,450); var d = b-a;
                var line = Panel(menu.transform, "TemporalRoute"+i, (a.x+b.x)/2, (a.y+b.y)/2, d.magnitude, 4, Cyan); line.localEulerAngles = new Vector3(0,0,Mathf.Atan2(d.y,d.x)*Mathf.Rad2Deg); line.SetSiblingIndex(1); routeLines[i] = line.GetComponent<UnityEngine.UI.Image>();
            }
            portalAuras = new RectTransform[KitchenLevel.Count];  routeStars = new UnityEngine.UI.Image[KitchenLevel.Count,3];
            for (int i = 0; i < KitchenLevel.Count; i++)
            {
                var p = RouteNodes[i%5]-new Vector2(800,450); var button = levelButtons[i]; var r = button.GetComponent<RectTransform>(); r.anchoredPosition = p; r.sizeDelta = new Vector2(120,120);
                var image = button.GetComponent<UnityEngine.UI.Image>(); image.sprite = circle; image.type = UnityEngine.UI.Image.Type.Simple; image.color = C("766B56");
                var aura = Dot(menu.transform, "PortalAura"+i, p.x,p.y,134,i<5?Gold:Cyan); aura.SetSiblingIndex(button.transform.GetSiblingIndex()); portalAuras[i]=aura;
                Dot(button.transform,"PortalInset",0,0,104,C("4E5B48"));
                levelTexts[i].transform.SetAsLastSibling(); levelTexts[i].rectTransform.anchoredPosition = Vector2.zero; levelTexts[i].rectTransform.sizeDelta = new Vector2(100,70); levelTexts[i].fontSize=37;
                for(int star=0;star<3;star++) { var s=Panel(button.transform,"EarnedStar"+star,-25+star*25,-46,12,12,Gold);s.localEulerAngles=new Vector3(0,0,45);routeStars[i,star]=s.GetComponent<UnityEngine.UI.Image>(); }
            }
            BuildBrief();
            foreach(string name in new[]{"OpenSettings","Training","TopAccent","BottomAccent"})
                menu.transform.Find(name)?.gameObject.SetActive(false);
            var back=menu.transform.Find("HubHome");
            back.GetComponent<UnityEngine.UI.Image>().color=C("465841");
            back.GetComponentInChildren<TMP_Text>().text="‹  Regresar";
            back.GetComponentInChildren<TMP_Text>().color=Color.white;
            back.SetAsLastSibling();
        }
        GameObject levelBrief;
        TMP_Text briefTitle,briefStats;
        UnityEngine.UI.RawImage preview;
        int briefLevel;
        void BuildBrief()
        {
            levelBrief=ScreenPanel("LevelBrief");
            PageArt(levelBrief,"HomeStillLife");
            Panel(levelBrief.transform,"BriefPaper",-330,35,785,660,C("F2E6CF"));
            preview=new GameObject("KitchenPreview",typeof(RectTransform),typeof(UnityEngine.UI.RawImage)).GetComponent<UnityEngine.UI.RawImage>();
            preview.rectTransform.SetParent(levelBrief.transform,false);
            preview.rectTransform.anchoredPosition=new Vector2(420,85);
            preview.rectTransform.sizeDelta=new Vector2(670,377);
            preview.raycastTarget=false;
            briefTitle=Label(levelBrief.transform,"",-690,210,720,150,44,C("384837"),FontStyles.Bold);
            briefStats=Label(levelBrief.transform,"",-690,22,720,205,31,C("4D5748"));
            var play=Button(levelBrief.transform,"PlaySelectedLevel",-330,-175,390,84,C("495F47"));
            Label(play.transform,"Jugar",-185,0,370,72,35,Color.white,FontStyles.Bold);
            play.onClick.AddListener(()=>RequestLevel(briefLevel,briefLevel==1 && !game.Progress.TutorialDone));
            var back=Button(levelBrief.transform,"BriefBack",-655,383,210,64,C("465841"));
            Label(back.transform,"‹  Regresar",-99,0,198,58,23,Color.white);
            back.onClick.AddListener(()=>{levelBrief.SetActive(false);menu.SetActive(true);RefreshHub();});
            // Workshop actions live in the briefing, leaving the route clear.
            int x=-520;
            foreach(string name in new[]{"BuySpeed","UnlockEra","BuyHeat"})
            {
                var r=(RectTransform)menu.transform.Find(name);
                r.SetParent(levelBrief.transform,false);r.anchoredPosition=new Vector2(x,-380);
                r.sizeDelta=new Vector2(445,67);x+=520;
                r.GetComponent<UnityEngine.UI.Image>().color=C("55634F");
                var text=r.GetComponentInChildren<TMP_Text>();
                text.rectTransform.anchoredPosition=Vector2.zero;text.rectTransform.sizeDelta=new Vector2(430,63);text.fontSize=18;text.color=Color.white;
            }
            menuMessage.rectTransform.SetParent(levelBrief.transform,false);
            menuMessage.rectTransform.anchoredPosition=new Vector2(0,-308);
            menuMessage.color=C("344131");
            menuBank.rectTransform.SetParent(levelBrief.transform,false);
            menuBank.rectTransform.anchoredPosition=new Vector2(595,365);
            menuBank.color=C("344131");
            resumeShift.transform.SetParent(levelBrief.transform,false);
            ((RectTransform)resumeShift.transform).anchoredPosition=new Vector2(420,-180);
            ((RectTransform)resumeShift.transform).sizeDelta=new Vector2(340,70);
            resumeCaption.rectTransform.anchoredPosition=Vector2.zero;
            resumeCaption.rectTransform.sizeDelta=new Vector2(325,62);
            resumeShift.gameObject.SetActive(false);
            levelBrief.SetActive(false);
        }
        void OpenBrief(int level)
        {
            briefLevel=level;
            RefreshBetaHub();
            var rules=KitchenLevel.For(level);
            briefTitle.text=KitchenLevel.Worlds[selectedPlanet].Name+"  ·  "+((level-1)%5+1)+"\n"+rules.Name;
            briefStats.text=$"Objetivo   {rules.Goal} pedidos\nTiempo   {rules.Seconds/60}:{rules.Seconds%60:00}\nMejor turno   {game.Progress.BestStars(level)} / 3 estrellas";
            preview.texture=WorldKitchen(selectedPlanet);
            menuMessage.text="";
            menu.SetActive(false);levelBrief.SetActive(true);levelBrief.transform.SetAsLastSibling();
        }
        void RefreshJourneyMap()
        {
            if(portalAuras==null)return;var p=game.Progress;
            ApplyPlanet();
            menuBank.text=$"CRÉDITOS {p.Bank}\nGEMAS {p.Gems}";
            for(int i=0;i<KitchenLevel.Count;i++)
            {
                bool open=i+1<=p.Unlocked&&(i<5||p.Era==2);levelTexts[i].text=(i%5+1).ToString();
                levelTexts[i].color=open?Color.white:C("7A8BA6");
                portalAuras[i].GetComponent<UnityEngine.UI.Image>().color=open?C("DDBD82"):C("807C70");
                for(int star=0;star<3;star++)routeStars[i,star].color=star<p.BestStars(i+1)?Gold:C("42516B");
                if(i<KitchenLevel.Count-1)routeLines[i].color=i+2<=p.Unlocked&&(i<4||p.Era==2)?Cyan:C("35405B");
            }
            speedText.text=$"BOTAS TEMPORALES {p.Speed}/3\n"+(p.Speed==3?"¡VELOCIDAD ABSURDA!":300*(p.Speed+1)+" CRÉDITOS");
            heatText.text=$"PLASMA DOMESTICADO {p.Heat}/3\n"+(p.Heat==3?"¡CASI LEGAL!":300*(p.Heat+1)+" CRÉDITOS");
            eraText.text=p.Era==2?"PORTAL STEAMPUNK\nABIERTO":"ABRIR SEGUNDA ERA\n3 GEMAS TEMPORALES";
        }
        void UpdateJourneyMap()
        {
            if(portalAuras==null||!menu.activeSelf)return;
            for(int i=0;i<KitchenLevel.Count;i++)portalAuras[i].localScale=Vector3.one*(ReducedMotion||!levelButtons[i].interactable?1:1+Mathf.Sin(Time.unscaledTime*2+i)*.025f);
        }
    }
}
