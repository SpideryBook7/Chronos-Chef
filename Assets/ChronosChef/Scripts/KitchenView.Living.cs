using TMPro;
using UnityEngine;
namespace ChronosChef
{
    public sealed partial class KitchenView
    {
        [SerializeField] Texture2D ingredientAtlas, customerEmotionAtlas;
        Sprite[] ingredientFrames;
        Sprite toastSprite;
        Sprite DishSprite(Food food) => food == Food.Toast ? toastSprite : ingredientFrames != null ? ingredientFrames[FoodTiles[(int)food]] : circle;
        Sprite[,] customerPoses;
        UnityEngine.UI.Image[,] customerPictures;
        RectTransform[] prepIcons;
        KitchenModelGraphic[] handModels, tableModels, stoveModels;
        KitchenModelGraphic breadModel;
        RectTransform prepLaser;
        GameObject prepReadout;
        TMP_Text prepCaption;
        UnityEngine.UI.Image prepFill;
        TMP_Text[] comicSounds;
        UnityEngine.UI.Button borrowButton;
        TMP_Text borrowCaption;
        RectTransform borrowDial;
        UnityEngine.UI.Image borrowFill;
        RectTransform receipt;
        TMP_Text receiptAmount;
        UnityEngine.UI.Image receiptDish;
        Vector2 receiptStart;
        float receiptAge;
        static readonly int[] FoodTiles = { -1, 0, 1, 2, 4, 10, 5, 6, 9, 3, 7, 8 };

        void InitializeFoodArt()
        {
            toastSprite = Resources.Load<Sprite>("MutantToast");
            if (!ingredientAtlas) return;
            ingredientFrames = new Sprite[12]; float w = ingredientAtlas.width / 6f, h = ingredientAtlas.height / 2f;
            for (int i = 0; i < 12; i++) ingredientFrames[i] = Sprite.Create(ingredientAtlas, new Rect(i % 6 * w, (1 - i / 6) * h, w, h), new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect);
        }
        bool TryDrawFoodArt(Transform parent, Food food)
        {
            if (ingredientFrames == null || food == Food.Empty) return false;
            var picture = Panel(parent, "FoodIllustration_" + food, 0, 0, 112, 104, Color.white).GetComponent<UnityEngine.UI.Image>();
            picture.sprite = DishSprite(food); picture.type = UnityEngine.UI.Image.Type.Simple; picture.preserveAspect = true; return true;
        }
        void ReplaceFoodArt(Transform parent, Food food)
        {
            if (ingredientFrames == null) return;
            foreach (Transform child in parent) child.gameObject.SetActive(false);
            DrawFood(parent, food);
        }
        UnityEngine.UI.Image FoodPicture(Transform parent, string name, Food food, Vector2 position, Vector2 size)
        {
            var picture = Panel(parent, name, position.x, position.y, size.x, size.y, Color.white).GetComponent<UnityEngine.UI.Image>();
            picture.sprite = DishSprite(food);
            picture.type = UnityEngine.UI.Image.Type.Simple; picture.preserveAspect = true; return picture;
        }
        void BuildLivingKitchen()
        {
            carriedIcon.localScale = Vector3.one * 1.25f; carriedIcon.anchoredPosition = new Vector2(46, 18);
            prepIcons = new RectTransform[KitchenRules.FoodNames.Length];
            for (int i = 1; i < prepIcons.Length; i++)
            {
                var holder = new GameObject("PrepFood_" + (Food)i, typeof(RectTransform)).GetComponent<RectTransform>(); holder.SetParent(stations[1].transform, false); holder.anchoredPosition = new Vector2(0, 15); holder.localScale = Vector3.one * .7f;
                DrawFood(holder, (Food)i); prepIcons[i] = holder; holder.gameObject.SetActive(false);
            }
            prepLaser = Panel(stations[1].transform, "LaserSweep", 0, 15, 122, 4, Cyan); prepLaser.gameObject.SetActive(false);
            prepReadout = Panel(stations[1].transform, "PrepReadout", 0, 87, 245, 35, C("15243C")).gameObject;
            prepCaption = Label(prepReadout.transform, "", -118, 0, 236, 32, 16, Cyan, FontStyles.Bold);
            prepFill = Bar(stations[1].transform, "PrepProgress", 0, 65, 205, 6, Cyan);
            // Bread is the assembly ingredient; a completed burger only appears after mounting.
            var tray = Dot(stations[3].transform, "AssemblyPlate", 0, 10, 150, C("382C43")); tray.sizeDelta = new Vector2(150, 96);
            Dot(tray, "PlateInset", 0, 0, 139, C("706078")).sizeDelta = new Vector2(139, 85);
            var bread = FoodPicture(stations[3].transform, "BreadSupply", Food.Bread, new Vector2(0, 18), new Vector2(138, 120));
            breadModel = ReplacePictureWithMesh(bread, Food.Bread);
            handModels = BuildFoodModels(carriedFood);
            tableModels = BuildFoodModels(prepIcons);
            stoveModels = BuildFoodModels(stoveFoods);
            var caption = stations[3].transform.Find("StationCaption"); if (caption) caption.GetComponentInChildren<TMP_Text>().text = "PAN · MONTAJE";
            if (customerEmotionAtlas)
            {
                customerPoses = new Sprite[4, 3]; customerPictures = new UnityEngine.UI.Image[3, 4];
                float w = customerEmotionAtlas.width / 4f, h = customerEmotionAtlas.height / 3f;
                for (int species = 0; species < 4; species++) for (int pose = 0; pose < 3; pose++)
                {
                    // Grog's wide punch extends into the otherwise empty gutter beside the robot.
                    float inset = species == 1 ? w * .08f : 0;
                    float width = species == 0 ? w * 1.04f : w - inset;
                    customerPoses[species, pose] = Sprite.Create(customerEmotionAtlas, new Rect(species * w + inset, (2 - pose) * h, width, h), new Vector2(.5f, 0), 100, 0, SpriteMeshType.FullRect);
                }
                for (int i = 0; i < 3; i++) for (int species = 0; species < 4; species++)
                {
                    var picture = customerArt[i][species].Find("IllustratedCharacter").GetComponent<UnityEngine.UI.Image>(); customerPictures[i, species] = picture; picture.rectTransform.sizeDelta = new Vector2(165, 190);
                }
            }
            comicSounds = new TMP_Text[3];
            for (int i = 0; i < 3; i++) comicSounds[i] = Label(stage, "", 550 + i * 270, 666, 160, 35, 23, Gold, FontStyles.Bold);
            BuildJourneyMap(); BuildRecipeTerminal();
            borrowButton = Button(stage, "BorrowTime", 190, 77, 290, 100, C("213956"));
            var dial = Dot(borrowButton.transform, "Clock", -100, 1, 56, Cyan);
            Dot(dial, "ClockFace", 0, 0, 47, Ink);
            borrowDial = Panel(dial, "MinuteHand", 0, 0, 4, 17, Gold);
            borrowDial.pivot = new Vector2(.5f, 0);
            borrowCaption = Label(borrowButton.transform, "", -65, 3, 199, 74, 17, Color.white, FontStyles.Bold);
            borrowFill = Bar(borrowButton.transform, "Charge", 3, -40, 245, 6, Cyan);
            borrowButton.onClick.AddListener(() => game.BorrowTime());
            borrowButton.transform.SetSiblingIndex(stage.Find("BottomPanel").GetSiblingIndex() + 1);
            var borrowRect = (RectTransform)borrowButton.transform;
            borrowRect.anchoredPosition = new Vector2(288,77);
            borrowRect.localScale = Vector3.one * .70f;
            BuildCastControls();
            receipt = Panel(stage, "DeliveryReceipt", 0, 0, 140, 42, C("163444"));
            receipt.SetSiblingIndex(menu.transform.GetSiblingIndex());
            receiptDish = FoodPicture(receipt, "DeliveredDish", Food.Burger, new Vector2(-47, 0), new Vector2(45, 40));
            receiptAmount = Label(receipt, "", -29, 0, 95, 38, 24, Gold, FontStyles.Bold);
            receipt.gameObject.SetActive(false);
            game.OrderServed += ShowDeliveryReceipt;
        }
        void ShowDeliveryReceipt(int slot, Food dish, int credits)
        {
            receiptStart = InGalley ? galley.ScreenPoint(new Vector3((slot-1)*3.9f,1.6f,2.92f)) : new Vector2(630 + slot * 270, 678);
            receipt.anchoredPosition = receiptStart;
            receiptDish.sprite = DishSprite(dish);
            receiptAmount.SetText("+{0}", credits);
            receiptAge = 0;
            receipt.gameObject.SetActive(true);
        }
        static KitchenModelGraphic ReplacePictureWithMesh(UnityEngine.UI.Image art, Food food, bool canInspect = false)
        {
            var model = KitchenModelGraphic.Attach(art.rectTransform, food, canInspect);
            if (model) art.enabled = false;
            return model;
        }
        static KitchenModelGraphic[] BuildFoodModels(RectTransform[] holders)
        {
            var result = new KitchenModelGraphic[holders.Length];
            for (int i = 1; i < holders.Length; i++)
            {
                var art = holders[i].Find("FoodIllustration_" + (Food)i);
                if (art) result[i] = ReplacePictureWithMesh(art.GetComponent<UnityEngine.UI.Image>(), (Food)i);
            }
            return result;
        }
        float nextMeshPose;
        void PoseFoodModels(KitchenGame g)
        {
            if (Time.unscaledTime < nextMeshPose || !g.Running || g.Paused) return;
            nextMeshPose = Time.unscaledTime + 1f / 12;
            float t = ReducedMotion ? 0 : Time.time;
            var hand = handModels[(int)g.Carry];
            if (hand) hand.Pose(-18 + Mathf.Sin(t * (g.IsMoving ? 7 : 1.2f)) * (g.IsMoving ? 15 : 5));
            var board = tableModels[(int)g.PrepFood];
            if (board) board.Pose(g.IsWorking ? -14 + Mathf.Sin(t * 14) * 16 : -14);
            var stove = stoveModels[(int)g.StoveFood];
            if (stove) stove.Pose(-20 + Mathf.Sin(t * 2) * 9);
            if (breadModel) breadModel.Pose(-12);
        }
        void RefreshLivingKitchen(KitchenGame g)
        {
            if (prepIcons == null) return;
            PoseFoodModels(g);
            RefreshCastControls(g);
            if (receipt.gameObject.activeSelf)
            {
                if (!g.Paused) receiptAge += Time.deltaTime;
                if (!g.Running || receiptAge >= 1) receipt.gameObject.SetActive(false);
                else if (!ReducedMotion)
                {
                    float flight = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.25f, 1, receiptAge));
                    receipt.anchoredPosition = Vector2.Lerp(receiptStart, wallet.rectTransform.anchoredPosition, flight) + Vector2.up * (Mathf.Sin(flight * Mathf.PI) * 38);
                }
            }
            bool borrowed = g.BorrowRemaining > 0;
            borrowButton.gameObject.SetActive(g.Running && !g.TutorialActive && g.CloneRemaining <= 0);
            borrowButton.interactable = g.BorrowCooldown <= 0;
            borrowCaption.text = borrowed ? $"TIEMPO PRESTADO\n{Mathf.CeilToInt(g.BorrowRemaining)} s" : g.BorrowCooldown > 0 ? $"RECARGANDO\n{Mathf.CeilToInt(g.BorrowCooldown)} s" : "CONGELAR 5 s\n+25 ANOMALÍA";
            borrowFill.fillAmount = 1 - g.BorrowCooldown / KitchenGame.BorrowRecharge;
            borrowDial.localEulerAngles = new Vector3(0, 0, ReducedMotion ? -30 : -360 * borrowFill.fillAmount - 30);
            if (borrowed) timer.color = Cyan;
            for (int i = 1; i < prepIcons.Length; i++) prepIcons[i].gameObject.SetActive(!InGalley && (int)g.PrepFood == i);
            prepReadout.SetActive(g.PrepFood != Food.Empty);
            prepFill.transform.parent.gameObject.SetActive(g.PrepFood != Food.Empty);
            prepFill.fillAmount = g.IsWorking ? g.WorkProgress : g.PrepFood == Food.Empty ? 0 : 1;
            prepCaption.text = g.IsWorking ? "CORTANDO · " + Mathf.RoundToInt(g.WorkProgress * 100) + "%" : "LISTO · TOCA PARA RECOGER";
            prepLaser.gameObject.SetActive(g.IsWorking);
            float t = g.Running && !g.Paused ? Time.time : 0;
            prepLaser.anchoredPosition = new Vector2(0, 15 + (ReducedMotion ? 0 : Mathf.Sin(t * 22) * 24));
            if (g.Carry != Food.Empty && !ReducedMotion && !g.Paused) carriedIcon.localEulerAngles = new Vector3(0, 0, Mathf.Sin(t * (g.IsMoving ? 10 : 2)) * 5);
            else carriedIcon.localEulerAngles = Vector3.zero;
            for (int i = 0; i < 3; i++)
            {
                int species = g.Species[i]; bool angry = g.Customers[i] && g.Patience[i] < 20;
                float phase = Mathf.Repeat(t + i * 1.7f, 6);
                bool emote = phase > 3.8f && phase < 5.2f;
                int pose = angry ? 2 : emote ? 1 : 0;
                if (customerPictures != null && !visitors[i,species])
                {
                    var picture = customerPictures[i, species]; picture.sprite = customerPoses[species, pose];
                    if (!ReducedMotion && g.Customers[i])
                    {
                        float wiggle = species == 0 ? Mathf.Sin(t * 3) * 2 : species == 1 ? Mathf.Sin(t * (angry ? 40 : 7)) * (angry ? 4 : 1) : species == 2 ? Mathf.Sin(t * 1.7f) * 3 : Mathf.Sin(t * 9) * 4;
                        picture.rectTransform.localEulerAngles = new Vector3(0, 0, wiggle);
                        picture.rectTransform.localScale = new Vector3(1 + Mathf.Sin(t * 3 + i) * .025f, 1 + Mathf.Cos(t * 3 + i) * .025f, 1);
                    }
                    else { picture.rectTransform.localEulerAngles = Vector3.zero; picture.rectTransform.localScale = Vector3.one; }
                }
                comicSounds[i].text = !g.Customers[i] || !emote && !angry ? "" : species == 0 ? angry ? "¡GROOOAR!" : "¡PUM!" : species == 1 ? angry ? "¡ERROR 404!" : "¡BIP-BUP!" : species == 2 ? angry ? "¿PERDÓN?" : "Mmm..." : angry ? "¡ÑAM-GRR!" : "¡CRUNCH!";
                if (InGalley) comicSounds[i].text = angry ? "¿Falta mucho?" : emote ? "¡Qué aroma!" : "";
                if (!InGalley && Mathf.Abs(aisleX[i] - SeatX(i)) > 8) comicSounds[i].text = "";
                comicSounds[i].color = angry ? Pink : Gold;
            }
        }
        void UpdateLivingUI() { UpdateJourneyMap(); UpdateRecipeTerminal(); }
        void DestroyLivingAssets()
        {
            if (game) game.OrderServed -= ShowDeliveryReceipt;
            if (ingredientFrames != null) foreach (var sprite in ingredientFrames) if (sprite) Destroy(sprite);
            if (customerPoses != null) foreach (var sprite in customerPoses) if (sprite) Destroy(sprite);
        }
    }
}
