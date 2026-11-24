using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChronosChef
{
    public sealed partial class KitchenView
    {
        GameObject arrivalPage, atlasPage;
        RawImage arrivalPicture;
        readonly RectTransform[] portalMotes = new RectTransform[14];
        TMP_Text planetTitle;
        Texture2D earthLevels;
        int selectedPlanet;

        RawImage PageArt(GameObject page, string texture)
        {
            var image = new GameObject(texture, typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
            image.rectTransform.SetParent(page.transform,false);
            image.rectTransform.sizeDelta = new Vector2(1600,900);
            image.raycastTarget = false;
            image.texture = Resources.Load<Texture2D>(texture);
            return image;
        }

        void BuildAtlas()
        {
            arrivalPage = ScreenPanel("ArrivalPage");
            arrivalPicture = PageArt(arrivalPage,"Arrival");
            for(int i=0;i<portalMotes.Length;i++)
                portalMotes[i]=Dot(arrivalPicture.transform,"Portal mote",0,0,2+i%4,new Color(.5f,1,1,.78f));
            var enter=Button(arrivalPage.transform,"EnterPortal",0,-290,450,135,Color.clear);
            enter.onClick.AddListener(()=>ScreenTransition(()=>{arrivalPage.SetActive(false); ShowHome();}));
            arrivalPage.SetActive(false);

            atlasPage=ScreenPanel("PlanetAtlas");
            PageArt(atlasPage,"PlanetAtlas");
            for(int i=0;i<KitchenLevel.Worlds.Length;i++)
            {
                int planet=i;
                int x=-600+i*400;
                var button=Button(atlasPage.transform,"Planet"+i,x,20,385,650,Color.clear);
                var plaque=Panel(button.transform,"Nameplate",0,-232,260,65,C("142639"));
                Label(plaque,KitchenLevel.Worlds[i],-155,0,310,59,32,Color.white,FontStyles.Bold);
                button.onClick.AddListener(()=>ScreenTransition(()=>{
                    atlasPage.SetActive(false);
                    game.GoToMenu();
                    selectedPlanet=planet;
                    RefreshHub();
                }));
            }
            var back=Button(atlasPage.transform,"AtlasHome",-660,368,180,60,C("162B40"));
            Label(back.transform,"< INICIO",-84,0,168,54,23,Cyan);
            back.onClick.AddListener(()=>ScreenTransition(()=>{atlasPage.SetActive(false);ShowHome();}));
            atlasPage.SetActive(false);
            earthLevels=Resources.Load<Texture2D>("EarthMap");
        }

        public void ShowArrival()
        {
            menu.SetActive(false);
            home.SetActive(false);
            arrivalPage.SetActive(true);
            arrivalPage.transform.SetAsLastSibling();
        }

        void ShowPlanets()
        {
            home.SetActive(false);
            menu.SetActive(false);
            atlasPage.SetActive(true);
            atlasPage.transform.SetAsLastSibling();
        }

        void ApplyPlanet()
        {
            if(!planetTitle) return;
            planetTitle.text=KitchenLevel.Worlds[selectedPlanet];
            var background=menu.transform.Find("ChronoCartography");
            if(background) background.GetComponent<RawImage>().texture=selectedPlanet==0?earthLevels:selectedPlanet==1?journeyArt:WorldKitchen(selectedPlanet);
            for(int i=0;i<KitchenLevel.Count;i++)
            {
                bool visible=i/5==selectedPlanet;
                levelButtons[i].gameObject.SetActive(visible);
                portalAuras[i].gameObject.SetActive(visible);
                if(i<KitchenLevel.Count-1) routeLines[i].gameObject.SetActive(visible && i%5!=4);
            }
        }

        void AnimateArrival()
        {
            if(!arrivalPage || !arrivalPage.activeSelf) return;
            float t=ReducedMotion?0:Time.unscaledTime;
            arrivalPicture.rectTransform.localScale=Vector3.one*(1+.006f*(1-Mathf.Cos(t*.68f)));
            for(int i=0;i<portalMotes.Length;i++)
            {
                float a=i*2.39996f+t*(.24f+i%3*.055f);
                float radius=195+10*Mathf.Sin(t+i);
                portalMotes[i].anchoredPosition=new Vector2(Mathf.Cos(a)*radius,4+Mathf.Sin(a)*radius);
                portalMotes[i].localScale=Vector3.one*(.75f+.25f*Mathf.Sin(a*3+i));
            }
        }
    }
}
