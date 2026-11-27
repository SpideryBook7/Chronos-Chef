using System;
using System.Collections.Generic;
using UnityEngine;
namespace ChronosChef
{
    public sealed partial class KitchenGame : MonoBehaviour
    {
        [SerializeField] KitchenView view;
        public KitchenProgress Progress { get; set; }
        public Food Carry { get; private set; }
        public Food StoveFood { get; private set; }
        public Food PrepFood { get; private set; }
        public float Remaining { get; private set; }
        public float Anomaly { get; private set; }
        public const float ParadoxSeconds = 15;
        public float BoardJam { get; private set; }
        public bool MeterDiscovered { get; private set; }
        public event Action<int,int> CustomerStormedOut;
        static readonly string[] GuestSounds = { "stomp","shortCircuit","spore","snarl" };
        public float CloneRemaining { get; private set; }
        public const float BorrowDuration = 5, BorrowRecharge = 32, BorrowAnomaly = 25;
        public float BorrowRemaining { get; private set; }
        public float BorrowCooldown { get; private set; }
        public bool BorrowTime()
        {
            if (!Running || Paused || TutorialActive || BorrowCooldown > 0) return false;
            BorrowRemaining = BorrowDuration;
            BorrowCooldown = BorrowRecharge;
            Respond("¡Cinco segundos prestados! Hasta el hambre se congela. El fogón sigue encendido.", "borrow");
            AddAnomaly(BorrowAnomaly);
            view.Refresh(this);
            return true;
        }
        public int Credits { get; private set; }
        public int Served { get; private set; }
        public int Lost { get; private set; }
        public int Reputation { get; private set; }
        public int Combo { get; private set; }
        public int Level { get; private set; } = 1;
        public KitchenLevel LevelRules => KitchenLevel.For(Level);
        public bool SoupNeedsDicing => RegionalMenu.SoupNeedsDicing(Level);
        public int Target => LevelRules.Goal;
        public float BurnWindow => LevelRules.BurnWindow;
        public int Stars => Reputation <= 0 || Served < Target ? 0 : Served < Target + 2 ? 1 : Served < Target + 4 ? 2 : 3;
        public bool Running { get; private set; }
        public bool Paused { get; private set; }
        public float CookTime { get; private set; }
        public float CookDuration { get; private set; } = 5;
        public float[] Patience { get; private set; } = new float[3];
        public float[] MaxPatience { get; private set; } = new float[3];
        public bool[] Customers { get; private set; } = new bool[3];
        public Food[] Orders { get; private set; } = new Food[3];
        public int[] Species { get; private set; } = new int[3];
        public string[] Speech { get; private set; } = new string[3];
        readonly float[] respawn = new float[3], speechTime = new float[3];
        readonly float[] banterCountdown = new float[3];
        readonly int[] banterLine = new int[3];
        static readonly string[] EarthBanter = { "Yo construí esa barra. Aguanta dos postres.", "Esa lechuga la conozco de semilla.", "La receta secreta es repetir plato.", "¡Vine en bici para pedir doble!" };
        static readonly string[][] Banter =
        {
            new[]{"Grog inventó la propina. Mañana.", "¿Sin gluten? Grog no conocer Gluten.", "¡Mi club de lectura se comió el libro!"},
            new[]{"Descargando hambre... 99%", "Mi dieta es de 8 bits.", "¿Aceptan tarjetas... gráficas?"},
            new[]{"Tiene notas de supernova. Interesante.", "Pedí sin agujeros negros, gracias.", "Mi estómago tiene tres estrellas."},
            new[]{"¡Brazos cortos, hambre larga!", "No muerdo. Casi nunca. Hoy quizá.", "¿El menú infantil incluye meteoritos?"}
        };
        readonly bool[] special = new bool[3];
        readonly List<Vector2Int> route = new List<Vector2Int>();
        Vector2Int cell = new Vector2Int(5, 1), blocked = new Vector2Int(-1, -1);
        Station? destination;
        int arrivals;
        float work, messageTime;
        public string Message { get; private set; }
        public string TaskLabel { get; private set; }
        public string DestinationLabel { get; private set; }
        public Vector2 ChefPosition { get; private set; }
        public Vector2 ClonePosition => KitchenRules.Position(blocked);
        public IReadOnlyList<Vector2Int> RoutePreview => route;
        public float WorkProgress => work > 0 ? 1 - work / 1.6f : 0;
        public event Action<string> Feedback;
        public event Action<int, Food, int> OrderServed;
        public void Configure(KitchenView target) { view = target; }
        void Start()
        {
            Application.runInBackground = true; Progress ??= new KitchenProgress();
            AudioListener.pause = false;
            if (Application.isMobilePlatform) { Application.targetFrameRate = 60; QualitySettings.vSyncCount = 0; }
            booting = true; view.Bind(this); ResetShift(); GoToMenu(); view.ShowArrival(); booting = false;
        }
        public void BeginLevel(int level, bool guided = false)
        {
            if (level < 1 || level > Progress.Unlocked || level > 5 && Progress.Era < 2) { view.MenuNotice("Este portal todavía está cerrado."); return; }
            Level = level; view.SetLevelEnvironment(level); ResetShift(); view.ShowMenu(false);
            if (level == 3) Say("¡TOSTADA MUTANTE! Una raíz > corte > recoger > pan. El ojo es comestible.");
            if (guided) { TutorialActive = true; TutorialStep = 0; view.Refresh(this); }
        }
        public void ResetShift()
        {
            BoardJam = 0; MeterDiscovered = false;
            view.ResetDiningQueue();
            ResetBetaState();
            ResetMovement();
            Carry = StoveFood = PrepFood = Food.Empty; Remaining = LevelRules.Seconds;
            Credits = Served = Lost = Combo = arrivals = 0; Reputation = 100;
            Anomaly = CloneRemaining = CookTime = work = 0; CookDuration = 5;
            BorrowRemaining = BorrowCooldown = 0;
            blocked = new Vector2Int(-1, -1); cell = new Vector2Int(5, view.InGalley ? 0 : 1);
            if (view.InGalley) Facing = Vector2.up;
            ChefPosition = KitchenRules.Position(cell);
            if (view.InGalley) ChefPosition += Vector2.right * 40;
            route.Clear(); destination = null;
            Running = true; Paused = false; TaskLabel = DestinationLabel = "";
            for (int i = 0; i < 3; i++) { SpawnCustomer(i); respawn[i] = 0; }
            Say("Desliza por el lado derecho para mover a Ares. Toca las estaciones para cocinar y a los clientes para entregar.");
            view.ShowMenu(false); view.ShowResults(false, this); view.Refresh(this);
        }
        public void GoToMenu() { if (Running) SaveCurrentSession(); Running = false; Paused = false; route.Clear(); destination = null; view.ShowResults(false, this); view.ShowMenu(true); }
        public void TogglePause() { if (!Running) return; Paused = !Paused; if (Paused) SaveCurrentSession(); Feedback?.Invoke("click"); view.Refresh(this); }
        void OnApplicationPause(bool status) { AudioListener.pause = status; if (status && Running && !Paused) TogglePause(); }
        void OnApplicationFocus(bool focus) { if (Application.isMobilePlatform) OnApplicationPause(!focus); }
        void OnApplicationQuit() { SaveCurrentSession(); }
        void OnDestroy() { SaveCurrentSession(); AudioListener.pause = false; }
        public void Say(string text) { Message = text; messageTime = 5; }
        void Respond(string text, string cue = "good") { Say(text); Feedback?.Invoke(cue); }
        void SpawnCustomer(int slot)
        {
            respawn[slot] = 0;
            Customers[slot] = true; Species[slot] = arrivals % 4;
            Orders[slot] = KitchenRecipe.OrderFor(Level, arrivals++);
            MaxPatience[slot] = LevelRules.Patience + slot * 8;
            Patience[slot] = MaxPatience[slot]; special[slot] = false; speechTime[slot] = 3;
            banterCountdown[slot] = 8 + slot * 2; banterLine[slot] = 0;
            Speech[slot] = view.InGalley ? "¡Mesa para uno!" : new[] { "¡Algo que no se haya extinguido!", "ERROR 404: comida no encontrada", "Exijo una estrella... de neutrones", "¡Rápido, tengo brazos cortos!" }[Species[slot]];
        }
        public void SelectStation(int index)
        {
            if (!Running || Paused || index < 0 || index > 8) return;
            if (TutorialActive && index != (int)TutorialStation) { Say(TutorialInstruction); Feedback?.Invoke("bad"); return; }
            var station = (Station)index;
            Vector2Int start = route.Count > 0 ? route[0] : RouteAnchor();
            bool finishStep = ChefPosition != KitchenRules.Position(start);
            var next = KitchenRules.Route(start, KitchenRules.Approach(station), blocked);
            if (start != KitchenRules.Approach(station) && next.Count == 0) { Respond("Tu otro yo bloquea esa ruta.", "bad"); return; }
            destination = station; route.Clear(); if (finishStep) route.Add(start); route.AddRange(next);
            DestinationLabel = index >= 5 && index <= 7 ? "ENTREGAR AL CLIENTE " + (index - 4) : new[] { "RECOGER DODO", "CORTAR", "FOGÓN", "MONTAR BURGER", "DESECHAR", "", "", "", "RECOGER RAÍCES" }[index];
            Say("En camino: " + DestinationLabel.ToLower() + "."); Feedback?.Invoke("click");
        }
        void Update()
        {
            if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame) view.HandleBack();
            if (!Running || Paused) return;
            float dt = Time.deltaTime;
            float pressure = TutorialActive ? 0 : Mathf.Max(0, dt - BorrowRemaining);
            BorrowRemaining = Mathf.Max(0, BorrowRemaining - dt);
            BorrowCooldown = Mathf.Max(0, BorrowCooldown - dt);
            float beforeClock = Remaining;
            Remaining = Mathf.Max(0, Remaining - pressure);
            BoardJam = Mathf.Max(0, BoardJam - dt);
            float riftTime = LevelRules.Seconds * .5f;
            if (!TutorialActive && beforeClock > riftTime + 8 && Remaining <= riftTime + 8)
            {
                AddAnomaly(15);
                Respond("Grieta temporal en 8 s. Vigila el fogón.", "warn");
            }
            if (!TutorialActive && beforeClock > riftTime && Remaining <= riftTime && CloneRemaining <= 0)
                AddAnomaly(100);
            autosave -= dt; if (autosave <= 0) { SaveCurrentSession(); autosave = 5; }
            if (messageTime > 0) messageTime -= dt; else Message = "Entrega tocando el personaje o su tarjeta. Consulta RECETAS si necesitas ayuda.";
            for (int i = 0; i < 3; i++)
            {
                speechTime[i] -= dt; if (speechTime[i] <= 0) Speech[i] = "";
                if (Customers[i])
                {
                    banterCountdown[i] -= dt;
                    if (banterCountdown[i] <= 0 && speechTime[i] <= 0 && Patience[i] >= 20)
                    {
                        Speech[i] = view.InGalley ? EarthBanter[Species[i]] : Banter[Species[i]][banterLine[i]++ % 3]; speechTime[i] = 3.5f; banterCountdown[i] = 10 + i;
                    }
                    Patience[i] -= pressure;
                    if (!special[i] && Patience[i] < 20)
                    {
                        special[i] = true; speechTime[i] = 4;
                        Speech[i] = view.InGalley ? "¡Se me acaba el descanso!" : new[] { "¡GROOAR!", "¡BIP! ¡BIP! ¡ERROR!", "Mi reseña será devastadora", "¡GRRR! ¡Esta barra es mía!" }[Species[i]];
                        if (Species[i] == 0) for (int j = Mathf.Max(0, i - 1); j <= Mathf.Min(2, i + 1); j++) if (j != i && Customers[j]) Patience[j] = Mathf.Max(1, Patience[j] - 7);
                        Feedback?.Invoke(GuestSounds[Species[i]]);
                    }
                    if (Patience[i] <= 0)
                    {
                        Customers[i] = false; respawn[i] = 4; Lost++; Combo = 0;
                        Reputation = Mathf.Max(0, Reputation - (Species[i] == 3 ? 22 : 12));
                        int kind = Species[i];
                        if (kind == 0 || kind == 2) BoardJam = kind == 0 ? 2.5f : 4;
                        if (kind == 1 && StoveFood != Food.Empty && StoveFood != Food.Burnt) CookTime += 3;
                        if (kind == 3) Remaining = Mathf.Max(0, Remaining - 3);
                        CustomerStormedOut?.Invoke(i,kind);
                        Respond(new[] {"¡Portazo! La tabla vibra durante 2,5 s.", "¡Cortocircuito! El fogón recibe una descarga.", "¡Esporas en la tabla! Ventilando durante 4 s.", "¡Estampida! Pierdes 3 s de servicio."}[kind],GuestSounds[kind]);
                        AddAnomaly(kind == 1 ? 35 : 25);
                    }
                }
                else { respawn[i] -= dt; if (respawn[i] <= 0) SpawnCustomer(i); }
            }
            if (StoveFood != Food.Empty && StoveFood != Food.Burnt)
            {
                CookTime += dt * (CloneRemaining > 0 ? 1.65f : 1);
                if (!TutorialActive && CookTime >= CookDuration + BurnWindow) { StoveFood = Food.Burnt; Combo = 0; AddAnomaly(35); Respond("¡Quemado! Retíralo y llévalo a BASURA.", "bad"); }
                else if (CookTime >= CookDuration && (StoveFood == Food.ChoppedDodo || StoveFood == Food.Roots))
                { StoveFood = StoveFood == Food.Roots ? Food.Soup : Food.CookedDodo; Respond("¡LISTO! Retira la comida antes de que se queme.", "ready"); }
            }
            if (CloneRemaining > 0)
            {
                float oldTime = CloneRemaining;
                CloneRemaining = Mathf.Max(0, CloneRemaining - dt);
                if ((oldTime > 10 && CloneRemaining <= 10) || (oldTime > 5 && CloneRemaining <= 5)) MoveEcho();
                if (CloneRemaining == 0) { blocked = new Vector2Int(-1, -1); Say("Tu otro yo se fue. Probablemente a almorzar."); } }
            if (work > 0)
            {
                if (BoardJam <= 0) work -= dt;
                if (work <= 0)
                {
                    work = 0;
                    PrepFood = PrepFood == Food.RawDodo ? Food.ChoppedDodo : PrepFood == Food.Root ? Food.ChoppedRoot : PrepFood == Food.Roots ? Food.ChoppedRoots : PrepFood;
                    Respond("¡Corte listo! La comida se queda en la mesa. Toca CORTE para recogerla.", "ready");
                }
            }
            MoveChef(dt);
            TaskLabel = work > 0 && !IsMoving && NearbyStation == Station.Cut ? "CORTANDO" : "";
            if (route.Count == 0 && destination.HasValue) { var s = destination.Value; destination = null; DestinationLabel = ""; Interact(s); }
            if (Remaining <= 0 || Reputation <= 0) EndShift();
            view.Refresh(this);
        }
        public void EndShift()
        {
            if (!Running) return;
            Running = false; Paused = false; route.Clear(); destination = null;
            Progress.ClearSession();
            Progress.Complete(Level, Credits, Stars);
            Feedback?.Invoke(Stars > 0 ? "win" : "bad"); view.ShowResults(true, this);
        }
        public void Interact(Station station)
        {
            if (!Running || Paused) return;
            if (TutorialActive && station != TutorialStation) { Say(TutorialInstruction); return; }
            FaceStation(station);
            var previousCarry = Carry; var previousStove = StoveFood; var previousPrep = PrepFood; int previousServed = Served;
            switch (station)
            {
                case Station.Supply:
                    if (Carry == Food.Empty) { Carry = Food.RawDodo; Respond("¡Dodo fresco de hace 65 millones de años! Toca CORTE."); }
                    else Respond("Ya llevas algo. Entrégalo o usa BASURA.", "bad"); break;
                case Station.Roots:
                    if (Carry == Food.Empty) { Carry = Food.Root; Respond("Una porción de "+RegionalMenu.Produce(Level)+". Toca RAÍCES de nuevo para reunir dos."); }
                    else if (Carry == Food.Root || Carry == Food.ChoppedRoot) { Carry = Food.Roots; Respond(SoupNeedsDicing ? "Dos porciones listas. Pásalas por CORTE antes del fogón." : "Dos porciones listas para el fogón."); }
                    else Respond("Necesitas las manos libres o una sola raíz.", "bad"); break;
                case Station.Cut:
                    if (BoardJam > 0) { Respond("La tabla se recupera en " + Mathf.CeilToInt(BoardJam) + " s.", "bad"); break; }
                    if (work > 0) { Respond("La cuchilla láser sigue trabajando. Puedes ir a otra estación.", "click"); break; }
                    if (PrepFood != Food.Empty)
                    {
                        if (Carry == Food.Empty) { Carry = PrepFood; PrepFood = Food.Empty; Respond("Recogido: " + RegionalMenu.Dish(Level,Carry) + "."); }
                        else Respond("La mesa ya tiene comida. Recógela con las manos libres.", "bad");
                    }
                    else if (Carry != Food.Empty)
                    {
                        PrepFood = Carry; Carry = Food.Empty;
                        if (PrepFood == Food.RawDodo || PrepFood == Food.Root || PrepFood == Food.Roots) { work = 1.6f; Respond("Ingrediente en la mesa. ¡La cuchilla trabaja sola!", "cut"); }
                        else Respond("Comida aparcada en la mesa. No se irá a ningún universo sin ti.");
                    }
                    else Respond("La mesa está vacía. Trae carne o raíces.", "bad"); break;
                case Station.Stove:
                    if (Carry==Food.Roots && SoupNeedsDicing)
                    { Respond("Esta receta lleva "+RegionalMenu.Produce(Level)+" en dados. Toca CORTE y recoge los dados.", "click"); break; }
                    if ((Carry == Food.ChoppedDodo || Carry == Food.Roots || Carry == Food.ChoppedRoots) && StoveFood == Food.Empty)
                    {
                        CookDuration = RegionalMenu.CookSeconds(Level, Carry != Food.ChoppedDodo) / (1 + (Progress?.Heat ?? 0) * .15f);
                        StoveFood = Carry == Food.ChoppedRoots ? Food.Roots : Carry; Carry = Food.Empty; CookTime = 0; Respond("Plasma encendido. Retira la comida cuando esté LISTA.", "cook");
                    }
                    else if (Carry == Food.Empty && (StoveFood == Food.CookedDodo || StoveFood == Food.Soup || StoveFood == Food.Burnt))
                    { Carry = StoveFood; StoveFood = Food.Empty; Respond(Carry == Food.Burnt ? "A BASURA. Ni Grog comería esto." : Carry == Food.Soup ? "Sopa servida en bol de energía. ¡A entregar!" : "¡Perfecto! Toca MONTAJE para añadir pan."); }
                    else Respond(Carry != Food.Empty ? "Libera las manos o espera a que el fogón esté libre." : "Todavía se cocina. ¡No mires el reloj tan fuerte!", "bad"); break;
                case Station.Assemble:
                    if (Carry == Food.CookedDodo) { Carry = Food.Burger; Respond(RegionalMenu.Dish(Level,Carry) + " listo. Toca su comanda para entregar.", "ready"); }
                    else if (Carry == Food.ChoppedRoot) { Carry = Food.Toast; Respond(RegionalMenu.Dish(Level,Carry) + " lista para entregar.", "ready"); }
                    else Respond("Trae dodo cocinado para una burger o una raíz cortada para una tostada.", "bad"); break;
                case Station.Trash:
                    if (Carry != Food.Empty) { Carry = Food.Empty; Combo = 0; AddAnomaly(10); Respond("Basura enviada a otra línea temporal. +10 anomalía.", "bad"); } break;
                default:
                    int customer = (int)station - 5;
                    if (customer < 0 || customer >= 3) return;
                    if (!Customers[customer]) { Respond("El próximo viajero está llegando.", "bad"); return; }
                    if (Carry != Orders[customer]) { Respond("Este cliente pide " + RegionalMenu.Dish(Level,Orders[customer]) + ". Llevas: " + RegionalMenu.Dish(Level,Carry) + ".", "bad"); return; }
                    Carry = Food.Empty; Customers[customer] = false; respawn[customer] = LevelRules.ReturnDelay; Combo++;
                    int reward = 100 + Mathf.CeilToInt(Patience[customer]) + Mathf.Min(Combo, 5) * 10;
                    Credits += reward; Served++; Reputation = Mathf.Min(100, Reputation + 3); Anomaly = Mathf.Max(0, Anomaly - 8);
                    Speech[customer] = "+" + reward + " créditos · ¡Delicioso!"; speechTime[customer] = 3;
                    OrderServed?.Invoke(customer, Orders[customer], reward);
                    Respond("¡ENTREGADO! +" + reward + " créditos. Combo x" + Combo + ".", "serve"); break;
            }
            if (previousCarry != Carry || previousStove != StoveFood || previousPrep != PrepFood || previousServed != Served)
            {
                GestureRemaining = .46f;
                ServingGesture = previousServed != Served;
                if (TutorialActive) AdvanceTutorial();
            }
        }
        public void AddAnomaly(float amount)
        {
            MeterDiscovered |= amount > 0;
            Anomaly = Mathf.Min(100, Anomaly + amount);
            if (Anomaly < 100 || CloneRemaining > 0) return;
            Anomaly = 0; CloneRemaining = ParadoxSeconds;
            if (!MoveEcho()) { CloneRemaining = 0; return; }
            Respond("¡PARADOJA! El eco cambia de sitio y acelera el fogón.", "paradox");
        }
        bool MoveEcho()
        {
            var candidates = new List<Vector2Int>();
            for (int x = 2; x < 10; x++) for (int y = 1; y < 3; y++)
            { var p = new Vector2Int(x, y); if (p != cell && (KitchenRules.Position(p) - ChefPosition).sqrMagnitude > 80*80 && KitchenRules.IsWalkable(p, blocked) && !KitchenRules.IsStationApproach(p) && !route.Contains(p)) candidates.Add(p); }
            if (candidates.Count == 0) return false;
            blocked = candidates[UnityEngine.Random.Range(0, candidates.Count)];
            return true;
        }
    }
}

