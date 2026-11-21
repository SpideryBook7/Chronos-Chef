using TMPro;
using UnityEngine;
namespace ChronosChef
{
    public sealed partial class KitchenView
    {
        int bookRecipe, bookStep;
        bool bookAuto = true;
        float bookClock;
        UnityEngine.UI.Button[] recipeCards;
        UnityEngine.UI.Image[] recipeCardIcons;
        TMP_Text[] recipeCardLabels;
        TMP_Text recipeName, recipeDetail, recipeJoke, recipeAutoLabel;
        UnityEngine.UI.Image recipeHero;
        RectTransform registerHalo;
        KitchenRecipe CurrentRecipe => KitchenRecipe.All[bookRecipe];
        void BuildRecipeTerminal()
        {
            var close=recipes.transform.Find("CloseRecipes").GetComponent<UnityEngine.UI.Button>();
            foreach(Transform child in recipes.transform)if(child!=close.transform)child.gameObject.SetActive(false);
            var bezel=Panel(recipes.transform,"TerminalBezel",0,0,1490,795,Cyan);Panel(bezel,"Metal",0,0,1482,787,C("26394D"));Panel(bezel,"Screen",0,0,1450,755,C("101D34"));
            for(int i=0;i<4;i++)Dot(bezel,"Bolt",i%2==0?-722:722,i<2?367:-367,12,Gold);
            for(int i=0;i<15;i++)Panel(bezel,"ScanLine",0,-340+i*48,1415,1,new Color(.3f,.9f,1,.045f));
            Label(recipes.transform,"ARCHIVO GASTRO-TEMPORAL",-650,325,1300,65,43,Cyan,FontStyles.Bold);
            for (int i = 0; i < KitchenRecipe.All.Length; i++)
            {
                int index = i;
                var entry = KitchenRecipe.All[i];
                float width = 1320f / KitchenRecipe.All.Length;
                var tab = Button(recipes.transform, entry.Dish + "Tab", -660 + (i + .5f) * width, 252, width - 18, 58, C("30475F"));
                Label(tab.transform, entry.Tab, -(width - 30) / 2, 0, width - 30, 48, 22, Cyan, FontStyles.Bold);
                tab.onClick.AddListener(() => SelectRecipe(index));
            }
            Panel(recipes.transform,"RecipeHeroDock",-525,-5,360,470,C("1D3049"));
            recipeName=Label(recipes.transform,"",-695,168,340,60,28,Gold,FontStyles.Bold);
            recipeHero=FoodPicture(recipes.transform,"RecipeFinishedDish",Food.Burger,new Vector2(-525,20),new Vector2(240,210));
            var joke=Panel(recipes.transform,"ChefComment",-525,-175,335,126,C("2D2947"));recipeJoke=Label(joke,"",-157,0,314,115,19,C("E3D3EA"));
            recipeCards=new UnityEngine.UI.Button[6];recipeCardIcons=new UnityEngine.UI.Image[6];recipeCardLabels=new TMP_Text[6];
            for(int i=0;i<6;i++)
            {
                int step=i;float x=-150+i%3*330,y=105-i/3*195;
                var card=Button(recipes.transform,"RecipeStep"+i,x,y,300,172,C("253A53"));recipeCards[i]=card;
                recipeCardIcons[i]=FoodPicture(card.transform,"StepIngredient",Food.RawDodo,new Vector2(0,20),new Vector2(93,88));
                recipeCardLabels[i]=Label(card.transform,"",-140,-53,280,54,17,C("CEE4EF"),FontStyles.Bold);
                Label(card.transform,(i+1).ToString("00"),-136,58,45,30,19,Cyan,FontStyles.Bold,TextAlignmentOptions.Left);
                card.onClick.AddListener(()=>{bookStep=step;bookClock=0;RenderRecipe();audioFeedback.Play("click");});
            }
            var detail=Panel(recipes.transform,"StepExplanation",180,-263,960,111,C("20364F"));recipeDetail=Label(detail,"",-460,0,920,102,22,Color.white);
            var auto=Button(recipes.transform,"RecipeAuto",-435,-347,400,52,C("30475F"));recipeAutoLabel=Label(auto.transform,"",-190,0,380,48,19,Cyan,FontStyles.Bold);
            auto.onClick.AddListener(()=>{bookAuto=!bookAuto;bookClock=0;RenderRecipe();});
            var previous=Button(recipes.transform,"RecipePrevious",20,-347,80,52,C("30475F"));Label(previous.transform,"<",-35,0,70,48,30,Cyan,FontStyles.Bold);previous.onClick.AddListener(()=>{bookStep=(bookStep+CurrentRecipe.Steps.Length-1)%CurrentRecipe.Steps.Length;bookClock=0;RenderRecipe();});
            var next=Button(recipes.transform,"RecipeNext",125,-347,80,52,C("30475F"));Label(next.transform,">",-35,0,70,48,30,Cyan,FontStyles.Bold);next.onClick.AddListener(()=>{bookStep=(bookStep+1)%CurrentRecipe.Steps.Length;bookClock=0;RenderRecipe();});
            close.transform.SetAsLastSibling();close.GetComponent<RectTransform>().anchoredPosition=new Vector2(485,-347);close.GetComponent<RectTransform>().sizeDelta=new Vector2(390,58);var label=close.GetComponentInChildren<TMP_Text>();label.rectTransform.sizeDelta=new Vector2(370,52);label.rectTransform.anchoredPosition=Vector2.zero;label.fontSize=24;
            close.onClick.RemoveAllListeners();close.onClick.AddListener(CloseRecipeBook);
            var register=stage.Find("RecipeHelp").GetComponent<UnityEngine.UI.Button>();register.GetComponent<RectTransform>().anchoredPosition=new Vector2(1450,260);register.GetComponent<RectTransform>().sizeDelta=new Vector2(190,100);
            foreach(Transform child in register.transform)child.gameObject.SetActive(false);
            register.GetComponent<UnityEngine.UI.Image>().color=new Color(.04f,.13f,.23f,.94f);
            registerHalo=Panel(register.transform,"HologramRim",0,0,196,106,Cyan);registerHalo.SetAsFirstSibling();Panel(register.transform,"TerminalGlass",0,0,187,97,C("173049"));
            FoodPicture(register.transform,"BurgerShortcut",Food.Burger,new Vector2(-40,12),new Vector2(68,62));FoodPicture(register.transform,"SoupShortcut",Food.Soup,new Vector2(40,12),new Vector2(68,62));
            Label(register.transform,"RECETARIO",-90,-31,180,28,17,Cyan,FontStyles.Bold);SelectRecipe(0);
        }
        void OpenRecipeBook()
        {
            if(!recipes)return;
            if(game.Running&&!game.Paused)game.TogglePause();
            bool soup=game.Carry==Food.Root||game.Carry==Food.Roots||game.Carry==Food.ChoppedRoot||game.Carry==Food.ChoppedRoots||game.Carry==Food.Soup||game.StoveFood==Food.Roots||game.StoveFood==Food.Soup;
            SelectRecipe(game.Carry == Food.Toast || game.Carry == Food.ChoppedRoot ? 2 : soup ? 1 : 0);recipes.SetActive(true);recipes.transform.SetAsLastSibling();
        }
        void CloseRecipeBook()
        {
            if(recipes)recipes.SetActive(false);
            if(game&&game.Running){settings.SetActive(false);if(game.Paused)game.TogglePause();}
        }
        void SelectRecipe(int recipe){bookRecipe=recipe;bookStep=0;bookClock=0;RenderRecipe();}
        void RenderRecipe()
        {
            if(recipeCards==null)return;bookStep=Mathf.Clamp(bookStep,0,CurrentRecipe.Steps.Length-1);
            recipeName.text=CurrentRecipe.Card;
            recipeHero.sprite=DishSprite(CurrentRecipe.Dish);
            recipeDetail.text="PASO "+(bookStep+1)+" · "+CurrentRecipe.Steps[bookStep].Instruction;recipeJoke.text=CurrentRecipe.Steps[bookStep].Joke;recipeAutoLabel.text=bookAuto?"ANIMACIÓN: ACTIVADA":"ANIMACIÓN: PAUSADA";
            for(int i=0;i<recipeCards.Length;i++)
            {
                bool active=i<CurrentRecipe.Steps.Length;recipeCards[i].gameObject.SetActive(active);if(!active)continue;
                recipeCardLabels[i].text=CurrentRecipe.Steps[i].Name;recipeCardIcons[i].sprite=DishSprite(CurrentRecipe.Steps[i].Picture);
                recipeCards[i].GetComponent<UnityEngine.UI.Image>().color=i==bookStep?C("386378"):C("253A53");
            }
        }
        void UpdateRecipeTerminal()
        {
            if(registerHalo)registerHalo.localScale=Vector3.one*(ReducedMotion?1:1+Mathf.Sin(Time.unscaledTime*2)*.02f);
            if(recipeCards==null||!recipes.activeSelf)return;
            bookClock+=Time.unscaledDeltaTime;if(bookAuto&&bookClock>4){bookClock=0;bookStep=(bookStep+1)%CurrentRecipe.Steps.Length;RenderRecipe();}
            for(int i=0;i<recipeCardIcons.Length;i++)recipeCardIcons[i].rectTransform.localScale=Vector3.one*(ReducedMotion||i!=bookStep?1:1+Mathf.Sin(Time.unscaledTime*4)*.07f);
            recipeHero.rectTransform.localEulerAngles=new Vector3(0,0,ReducedMotion?0:Mathf.Sin(Time.unscaledTime*1.4f)*3);
        }
    }
}
