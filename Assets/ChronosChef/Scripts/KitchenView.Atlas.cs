using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChronosChef
{
    public sealed partial class KitchenView
    {
        GameObject arrivalPage, atlasPage;
        RawImage arrivalPicture;
        TMP_Text planetTitle;
        readonly Texture2D[] routeArt=new Texture2D[KitchenLevel.Worlds.Length];
        readonly GameObject[] planetTargets=new GameObject[KitchenLevel.Worlds.Length];
        int atlasSheet;
        RawImage atlasPicture;
        GameObject previousSheet, nextSheet;
        Texture2D MapFor(int world)
        {
            if(!routeArt[world]) routeArt[world]=Resources.Load<Texture2D>(KitchenLevel.Worlds[world].Map);
            return routeArt[world];
        }
        void OpenSheet(int sheet)
        {
            atlasSheet=sheet;
            driftingWorlds.SetFloat("_Slots", Mathf.Min(4, planetTargets.Length - sheet * 4));
            atlasPicture.texture=Resources.Load<Texture2D>(sheet==0?"PlanetAtlas":"PlanetAtlas"+(sheet+1));
            for(int i=0;i<planetTargets.Length;i++) planetTargets[i].SetActive(i/4==sheet);
            previousSheet.SetActive(sheet>0);
            nextSheet.SetActive((sheet+1)*4<planetTargets.Length);
        }
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

        Material daylight, driftingWorlds;
        void AnimateArt(RawImage image, bool worlds)
        {
            var material = new Material(Resources.Load<Shader>("MenuAtmosphere"));
            material.SetFloat("_Worlds",worlds ? 1 : 0);
            image.material=material;
            if(worlds) driftingWorlds=material; else daylight=material;
        }
        void BuildAtlas()
        {
            arrivalPage = ScreenPanel("ArrivalPage");
            arrivalPicture = PageArt(arrivalPage,"WelcomeDiorama");
            Label(arrivalPage.transform,"Chronos Chef",-450,-212,900,110,78,C("374633"),FontStyles.Bold);
            var enter=Button(arrivalPage.transform,"EnterPortal",0,-325,380,82,C("435943"));
            Label(enter.transform,"Comenzar",-180,0,360,72,34,Color.white,FontStyles.Bold);
            enter.onClick.AddListener(()=>ScreenTransition(()=>{arrivalPage.SetActive(false); ShowHome();}));
            arrivalPage.SetActive(false);

            atlasPage=ScreenPanel("PlanetAtlas");
            atlasPicture=PageArt(atlasPage,"PlanetAtlas");
            AnimateArt(atlasPicture,true);
            for(int i=0;i<KitchenLevel.Worlds.Length;i++)
            {
                int planet=i;
                int slots = Mathf.Min(4, planetTargets.Length - i / 4 * 4);
                int x = Mathf.RoundToInt(1600f * (i % 4 + .5f) / slots - 800);
                var button=Button(atlasPage.transform,"Planet"+i,x,20,385,650,Color.clear);
                planetTargets[i]=button.gameObject;
                var plaque=Panel(button.transform,"Nameplate",0,-232,260,65,C("142639"));
                Label(plaque,KitchenLevel.Worlds[i].Name,-155,0,310,59,32,Color.white,FontStyles.Bold);
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
            previousSheet=SheetButton("Anterior",-640,-362);
            nextSheet=SheetButton("Siguiente",640,-362);
            OpenSheet(0);
        }
        GameObject SheetButton(string caption,int x,int y)
        {
            var button=Button(atlasPage.transform,caption,x,y,215,66,C("435943"));
            Label(button.transform,caption,-103,0,206,58,24,Color.white);
            button.onClick.AddListener(()=>OpenSheet(atlasSheet+(x<0?-1:1)));
            return button.gameObject;
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
            planetTitle.text=KitchenLevel.Worlds[selectedPlanet].Name;
            var background=menu.transform.Find("ChronoCartography");
            if(background) background.GetComponent<RawImage>().texture=MapFor(selectedPlanet);
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
        }
    }
}
