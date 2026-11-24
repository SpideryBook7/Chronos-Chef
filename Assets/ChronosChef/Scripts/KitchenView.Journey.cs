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
        TMP_Text[] routeNames;
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
            Panel(menu.transform, "RouteHeader", 0, 370, 1560, 145, new Color(.025f,.035f,.10f,.78f));
            planetTitle=Label(menu.transform, "TIERRA", -340, 365, 680, 72, 46, Cyan, FontStyles.Bold);
            menuBank.rectTransform.anchoredPosition = new Vector2(610,365); menuBank.rectTransform.sizeDelta = new Vector2(280,80); menuBank.alignment = TextAlignmentOptions.Right; menuBank.fontSize = 18;
            routeLines = new UnityEngine.UI.Image[9];
            for (int i = 0; i < 9; i++)
            {
                var a = RouteNodes[i] - new Vector2(800,450); var b = RouteNodes[i+1] - new Vector2(800,450); var d = b-a;
                var line = Panel(menu.transform, "TemporalRoute"+i, (a.x+b.x)/2, (a.y+b.y)/2, d.magnitude, 4, Cyan); line.localEulerAngles = new Vector3(0,0,Mathf.Atan2(d.y,d.x)*Mathf.Rad2Deg); line.SetSiblingIndex(1); routeLines[i] = line.GetComponent<UnityEngine.UI.Image>();
            }
            portalAuras = new RectTransform[10]; routeNames = new TMP_Text[10]; routeStars = new UnityEngine.UI.Image[10,3];
            for (int i = 0; i < 10; i++)
            {
                var p = RouteNodes[i]-new Vector2(800,450); var button = levelButtons[i]; var r = button.GetComponent<RectTransform>(); r.anchoredPosition = p; r.sizeDelta = new Vector2(120,120);
                var image = button.GetComponent<UnityEngine.UI.Image>(); image.sprite = circle; image.type = UnityEngine.UI.Image.Type.Simple; image.color = C("24354E");
                var aura = Dot(menu.transform, "PortalAura"+i, p.x,p.y,134,i<5?Gold:Cyan); aura.SetSiblingIndex(button.transform.GetSiblingIndex()); portalAuras[i]=aura;
                Dot(button.transform,"PortalInset",0,0,104,C("15233D"));
                levelTexts[i].transform.SetAsLastSibling(); levelTexts[i].rectTransform.anchoredPosition = Vector2.zero; levelTexts[i].rectTransform.sizeDelta = new Vector2(100,70); levelTexts[i].fontSize=37;
                Panel(button.transform,"DestinationPlaque",0,-88,194,48,new Color(.035f,.06f,.12f,.86f));
                routeNames[i]=Label(button.transform, KitchenLevel.For(i+1).Name.ToUpperInvariant(),-90,-88,180,42,14,Color.white,FontStyles.Bold);
                for(int star=0;star<3;star++) { var s=Panel(button.transform,"EarnedStar"+star,-25+star*25,-46,12,12,Gold);s.localEulerAngles=new Vector3(0,0,45);routeStars[i,star]=s.GetComponent<UnityEngine.UI.Image>(); }
            }
            Panel(menu.transform,"UpgradeDock",0,-326,1540,165,new Color(.025f,.035f,.10f,.93f));
            PositionMapControl("BuySpeed",new Vector2(-520,-320),new Vector2(430,76));
            PositionMapControl("BuyHeat",new Vector2(520,-320),new Vector2(430,76));
            PositionMapControl("UnlockEra",new Vector2(0,-320),new Vector2(430,76));
            foreach(var caption in new[]{speedText,heatText,eraText}) { caption.rectTransform.anchoredPosition=Vector2.zero;caption.rectTransform.sizeDelta=new Vector2(410,70);caption.fontSize=18; }
            menuMessage.rectTransform.anchoredPosition=new Vector2(0,-223);menuMessage.rectTransform.sizeDelta=new Vector2(1400,50);menuMessage.fontSize=19;
            PositionMapControl("OpenSettings",new Vector2(-610,-410),new Vector2(230,48));
            PositionMapControl("Training",new Vector2(-310,-410),new Vector2(280,48));
            foreach(var control in new[]{"BuySpeed","BuyHeat","UnlockEra","OpenSettings","Training"})
            {
                var target=menu.transform.Find(control);target.SetAsLastSibling();
                var caption=target.GetComponentInChildren<TMP_Text>();caption.rectTransform.anchoredPosition=Vector2.zero;caption.rectTransform.sizeDelta=target.GetComponent<RectTransform>().sizeDelta-new Vector2(16,4);
            }
            resumeShift.GetComponent<RectTransform>().anchoredPosition=new Vector2(300,-410);resumeShift.GetComponent<RectTransform>().sizeDelta=new Vector2(300,48);resumeCaption.rectTransform.sizeDelta=new Vector2(280,44);resumeCaption.rectTransform.anchoredPosition=Vector2.zero;
            var back=menu.transform.Find("HubHome");if(back)back.SetAsLastSibling();
        }
        void PositionMapControl(string name,Vector2 position,Vector2 size)
        {var r=menu.transform.Find(name).GetComponent<RectTransform>();r.anchoredPosition=position;r.sizeDelta=size;}
        void RefreshJourneyMap()
        {
            if(portalAuras==null)return;var p=game.Progress;
            ApplyPlanet();
            menuBank.text=$"CRÉDITOS {p.Bank}\nGEMAS {p.Gems}";
            for(int i=0;i<10;i++)
            {
                bool open=i+1<=p.Unlocked&&(i<5||p.Era==2);levelTexts[i].text=(i%5+1).ToString();
                levelTexts[i].color=open?Color.white:C("7A8BA6");routeNames[i].color=open?Color.white:C("90A1BC");
                portalAuras[i].GetComponent<UnityEngine.UI.Image>().color=open?(i<5?Gold:Cyan):C("3A4662");
                for(int star=0;star<3;star++)routeStars[i,star].color=star<p.BestStars(i+1)?Gold:C("42516B");
                if(i<9)routeLines[i].color=i+2<=p.Unlocked&&(i<4||p.Era==2)?Cyan:C("35405B");
            }
            speedText.text=$"BOTAS TEMPORALES {p.Speed}/3\n"+(p.Speed==3?"¡VELOCIDAD ABSURDA!":300*(p.Speed+1)+" CRÉDITOS");
            heatText.text=$"PLASMA DOMESTICADO {p.Heat}/3\n"+(p.Heat==3?"¡CASI LEGAL!":300*(p.Heat+1)+" CRÉDITOS");
            eraText.text=p.Era==2?"PORTAL STEAMPUNK\nABIERTO":"ABRIR SEGUNDA ERA\n3 GEMAS TEMPORALES";
        }
        void UpdateJourneyMap()
        {
            if(portalAuras==null||!menu.activeSelf)return;
            for(int i=0;i<10;i++)portalAuras[i].localScale=Vector3.one*(ReducedMotion||!levelButtons[i].interactable?1:1+Mathf.Sin(Time.unscaledTime*2+i)*.025f);
        }
    }
}
