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
        static readonly Food[][] StepFoods =
        {
            new[]{Food.RawDodo,Food.RawDodo,Food.ChoppedDodo,Food.CookedDodo,Food.Bread,Food.Burger},
            new[]{Food.Roots,Food.ChoppedRoots,Food.Roots,Food.Soup,Food.Soup}
        };
        static readonly string[][] StepNames =
        {
            new[]{"RECOGER DODO","DEJAR EN CORTE","RECOGER DE LA MESA","COCINAR Y RETIRAR","AÑADIR PAN","ENTREGAR"},
            new[]{"RECOGER DOS RAÍCES","CORTE OPCIONAL","COCINAR EN PLASMA","RETIRAR EL BOL","ENTREGAR"}
        };
        static readonly string[][] StepDetails =
        {
            new[]{"Toca el almacén de carne. Llevarás una porción de dodo crudo.","Toca CORTE: dejas la carne y la cuchilla trabaja sola. Puedes ir a otra estación.","Cuando termine, toca CORTE con las manos libres. El ingrediente permanece allí hasta que lo recoges.","Pon la carne cortada en PLASMA. Espera LISTO y toca el fogón otra vez antes de que se queme.","Lleva la carne cocinada al PAN de MONTAJE. Allí se monta la hamburguesa; no recoges una ya hecha.","Busca el pedido de hamburguesa y toca el cliente, su cabeza o su tarjeta."},
            new[]{"Toca RAÍCES dos veces para reunir dos unidades.","Si quieres, deja las raíces en CORTE. Las raíces cortadas también sirven para la sopa; recógelas al terminar.","Pon las dos raíces, enteras o cortadas, en PLASMA. No mezcles otro plato en un fogón ocupado.","Espera LISTO y toca PLASMA con las manos libres. La sopa ya sale en su bol de energía.","Entrega al cliente que muestre sopa. Una hamburguesa no arregla una crisis existencial de sopa."}
        };
        static readonly string[][] StepJokes =
        {
            new[]{"El dodo está fresco. Geológicamente hablando.","La cuchilla ya cortó el lunes. Dos veces.","La mesa no es un portal. Tu comida se queda.","Cinco segundos de plasma. No cinco eras.","Es pan. No una hamburguesa prefabricada del futuro.","Propinas en monedas. Los meteoritos no caben."},
            new[]{"Dos raíces. La tercera invoca al brócoli.","¡Mise en place espacial! Suena carísimo.","Si la sopa te mira, no la mires de vuelta.","El bol incluye garantía cuántica. Quizá.","Servicio completado. El universo puede seguir girando."}
        };
        void BuildRecipeTerminal()
        {
            var close=recipes.transform.Find("CloseRecipes").GetComponent<UnityEngine.UI.Button>();
            foreach(Transform child in recipes.transform)if(child!=close.transform)child.gameObject.SetActive(false);
            var bezel=Panel(recipes.transform,"TerminalBezel",0,0,1490,795,Cyan);Panel(bezel,"Metal",0,0,1482,787,C("26394D"));Panel(bezel,"Screen",0,0,1450,755,C("101D34"));
            for(int i=0;i<4;i++)Dot(bezel,"Bolt",i%2==0?-722:722,i<2?367:-367,12,Gold);
            for(int i=0;i<15;i++)Panel(bezel,"ScanLine",0,-340+i*48,1415,1,new Color(.3f,.9f,1,.045f));
            Label(recipes.transform,"ARCHIVO GASTRO-TEMPORAL",-650,325,1300,65,43,Cyan,FontStyles.Bold);
            var burger=Button(recipes.transform,"BurgerTab",-320,252,540,58,C("30475F"));Label(burger.transform,"CYBER-DODO BURGER",-260,0,520,50,24,Gold,FontStyles.Bold);burger.onClick.AddListener(()=>SelectRecipe(0));
            var soup=Button(recipes.transform,"SoupTab",320,252,540,58,C("30475F"));Label(soup.transform,"SOPA CUÁNTICA",-260,0,520,50,24,Green,FontStyles.Bold);soup.onClick.AddListener(()=>SelectRecipe(1));
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
            var previous=Button(recipes.transform,"RecipePrevious",20,-347,80,52,C("30475F"));Label(previous.transform,"<",-35,0,70,48,30,Cyan,FontStyles.Bold);previous.onClick.AddListener(()=>{bookStep=(bookStep+StepFoods[bookRecipe].Length-1)%StepFoods[bookRecipe].Length;bookClock=0;RenderRecipe();});
            var next=Button(recipes.transform,"RecipeNext",125,-347,80,52,C("30475F"));Label(next.transform,">",-35,0,70,48,30,Cyan,FontStyles.Bold);next.onClick.AddListener(()=>{bookStep=(bookStep+1)%StepFoods[bookRecipe].Length;bookClock=0;RenderRecipe();});
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
            SelectRecipe(soup?1:0);recipes.SetActive(true);recipes.transform.SetAsLastSibling();
        }
        void CloseRecipeBook()
        {
            if(recipes)recipes.SetActive(false);
            if(game&&game.Running){settings.SetActive(false);if(game.Paused)game.TogglePause();}
        }
        void SelectRecipe(int recipe){bookRecipe=recipe;bookStep=0;bookClock=0;RenderRecipe();}
        void RenderRecipe()
        {
            if(recipeCards==null)return;bookStep=Mathf.Clamp(bookStep,0,StepFoods[bookRecipe].Length-1);
            recipeName.text=bookRecipe==0?"CYBER-DODO\nBURGER":"SOPA CUÁNTICA\nDE RAÍCES";
            recipeHero.sprite=ingredientFrames!=null?ingredientFrames[bookRecipe==0?4:9]:circle;
            recipeDetail.text="PASO "+(bookStep+1)+" · "+StepDetails[bookRecipe][bookStep];recipeJoke.text=StepJokes[bookRecipe][bookStep];recipeAutoLabel.text=bookAuto?"ANIMACIÓN: ACTIVADA":"ANIMACIÓN: PAUSADA";
            for(int i=0;i<recipeCards.Length;i++)
            {
                bool active=i<StepFoods[bookRecipe].Length;recipeCards[i].gameObject.SetActive(active);if(!active)continue;
                recipeCardLabels[i].text=StepNames[bookRecipe][i];recipeCardIcons[i].sprite=ingredientFrames!=null?ingredientFrames[FoodTiles[(int)StepFoods[bookRecipe][i]]]:circle;
                recipeCards[i].GetComponent<UnityEngine.UI.Image>().color=i==bookStep?C("386378"):C("253A53");
            }
        }
        void UpdateRecipeTerminal()
        {
            if(registerHalo)registerHalo.localScale=Vector3.one*(ReducedMotion?1:1+Mathf.Sin(Time.unscaledTime*2)*.02f);
            if(recipeCards==null||!recipes.activeSelf)return;
            bookClock+=Time.unscaledDeltaTime;if(bookAuto&&bookClock>4){bookClock=0;bookStep=(bookStep+1)%StepFoods[bookRecipe].Length;RenderRecipe();}
            for(int i=0;i<recipeCardIcons.Length;i++)recipeCardIcons[i].rectTransform.localScale=Vector3.one*(ReducedMotion||i!=bookStep?1:1+Mathf.Sin(Time.unscaledTime*4)*.07f);
            recipeHero.rectTransform.localEulerAngles=new Vector3(0,0,ReducedMotion?0:Mathf.Sin(Time.unscaledTime*1.4f)*3);
        }
    }
}
