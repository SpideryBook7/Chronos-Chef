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
        public float CloneRemaining { get; private set; }
        public int Credits { get; private set; }
        public int Served { get; private set; }
        public int Lost { get; private set; }
        public int Reputation { get; private set; }
        public int Combo { get; private set; }
        public int Level { get; private set; } = 1;
        public KitchenLevel LevelRules => KitchenLevel.For(Level);
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
        public void Configure(KitchenView target) { view = target; }
        void Start()
        {
            Application.runInBackground = true; Progress ??= new KitchenProgress();
            AudioListener.pause = false;
            if (Application.isMobilePlatform) { Application.targetFrameRate = 60; QualitySettings.vSyncCount = 0; }
            booting = true; view.Bind(this); ResetShift(); GoToMenu(); view.ShowHome(); booting = false;
        }
        public void BeginLevel(int level, bool guided = false)
        {
            if (level < 1 || level > Progress.Unlocked || level > 5 && Progress.Era < 2) { view.MenuNotice("Este portal todavía está cerrado."); return; }
            Level = level; ResetShift(); view.ShowMenu(false);
            if (guided) { TutorialActive = true; TutorialStep = 0; view.Refresh(this); }
        }
        public void ResetShift()
        {
            ResetBetaState();
            Carry = StoveFood = PrepFood = Food.Empty; Remaining = LevelRules.Seconds;
            Credits = Served = Lost = Combo = arrivals = 0; Reputation = 100;
            Anomaly = CloneRemaining = CookTime = work = 0; CookDuration = 5;
            blocked = new Vector2Int(-1, -1); cell = new Vector2Int(5, 1);
            ChefPosition = KitchenRules.Position(cell); route.Clear(); destination = null;
            Running = true; Paused = false; TaskLabel = DestinationLabel = "";
            for (int i = 0; i < 3; i++) { SpawnCustomer(i); respawn[i] = 0; }
            Say("¡Turno abierto! Burger: dodo > corte > fogón > montaje. Sopa: 2 raíces > fogón.");
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
            Orders[slot] = arrivals % 3 == 1 ? Food.Soup : Food.Burger; arrivals++;
            MaxPatience[slot] = LevelRules.Patience + slot * 8;
            Patience[slot] = MaxPatience[slot]; special[slot] = false; speechTime[slot] = 3;
            banterCountdown[slot] = 8 + slot * 2; banterLine[slot] = 0;
            Speech[slot] = new[] { "¡Algo que no se haya extinguido!", "ERROR 404: comida no encontrada", "Exijo una estrella... de neutrones", "¡Rápido, tengo brazos cortos!" }[Species[slot]];
        }
        public void SelectStation(int index)
        {
            if (!Running || Paused || index < 0 || index > 8) return;
            if (TutorialActive && index != (int)TutorialStation) { Say(TutorialInstruction); Feedback?.Invoke("bad"); return; }
            var station = (Station)index;
            Vector2Int start = route.Count > 0 ? route[0] : cell;
            bool finishStep = route.Count > 0;
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
            float dt = Time.deltaTime, pressure = TutorialActive ? 0 : dt; Remaining = Mathf.Max(0, Remaining - pressure);
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
                        Speech[i] = Banter[Species[i]][banterLine[i]++ % 3]; speechTime[i] = 3.5f; banterCountdown[i] = 10 + i;
                    }
                    Patience[i] -= pressure;
                    if (!special[i] && Patience[i] < 20)
                    {
                        special[i] = true; speechTime[i] = 4;
                        Speech[i] = new[] { "¡GROOAR!", "¡BIP! ¡BIP! ¡ERROR!", "Mi reseña será devastadora", "¡GRRR! ¡Esta barra es mía!" }[Species[i]];
                        if (Species[i] == 0) for (int j = Mathf.Max(0, i - 1); j <= Mathf.Min(2, i + 1); j++) if (j != i && Customers[j]) Patience[j] = Mathf.Max(1, Patience[j] - 7);
                        Feedback?.Invoke("warn");
                    }
                    if (Patience[i] <= 0)
                    {
                        Customers[i] = false; respawn[i] = 4; Lost++; Combo = 0;
                        Reputation = Mathf.Max(0, Reputation - (Species[i] == 3 ? 22 : 12));
                        AddAnomaly(25); Respond("Cliente perdido. " + (Species[i] == 3 ? "¡El raptor arañó el mostrador!" : "El tiempo también se come."), "bad");
                    }
                }
                else { respawn[i] -= dt; if (respawn[i] <= 0) SpawnCustomer(i); }
            }
            if (StoveFood != Food.Empty && StoveFood != Food.Burnt)
            {
                CookTime += dt;
                if (!TutorialActive && CookTime >= CookDuration + BurnWindow) { StoveFood = Food.Burnt; Combo = 0; AddAnomaly(35); Respond("¡Quemado! Retíralo y llévalo a BASURA.", "bad"); }
                else if (CookTime >= CookDuration && (StoveFood == Food.ChoppedDodo || StoveFood == Food.Roots))
                { StoveFood = StoveFood == Food.Roots ? Food.Soup : Food.CookedDodo; Respond("¡LISTO! Retira la comida antes de que se queme.", "ready"); }
            }
            if (CloneRemaining > 0)
            { CloneRemaining = Mathf.Max(0, CloneRemaining - dt); if (CloneRemaining == 0) { blocked = new Vector2Int(-1, -1); Say("Tu otro yo se fue. Probablemente a almorzar."); } }
            if (work > 0)
            {
                work -= dt;
                if (work <= 0)
                {
                    work = 0;
                    PrepFood = PrepFood == Food.RawDodo ? Food.ChoppedDodo : PrepFood == Food.Root ? Food.ChoppedRoot : PrepFood == Food.Roots ? Food.ChoppedRoots : PrepFood;
                    Respond("¡Corte listo! La comida se queda en la mesa. Toca CORTE para recogerla.", "ready");
                }
            }
            TaskLabel = work > 0 && route.Count == 0 && Vector2.Distance(ChefPosition, KitchenRules.Position(KitchenRules.Approach(Station.Cut))) < 2 ? "CORTANDO" : "";
            if (route.Count > 0)
            {
                float distanceBudget = (340 + (Progress?.Speed ?? 0) * 60) * dt;
                while (route.Count > 0 && distanceBudget > 0)
                {
                    var waypoint = KitchenRules.Position(route[0]); float distance = Vector2.Distance(ChefPosition, waypoint);
                    if (distance <= distanceBudget) { ChefPosition = waypoint; distanceBudget -= distance; cell = route[0]; route.RemoveAt(0); }
                    else { ChefPosition = Vector2.MoveTowards(ChefPosition, waypoint, distanceBudget); distanceBudget = 0; }
                }
            }
            else if (destination.HasValue) { var s = destination.Value; destination = null; DestinationLabel = ""; Interact(s); }
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
            var previousCarry = Carry; var previousStove = StoveFood; var previousPrep = PrepFood; int previousServed = Served;
            switch (station)
            {
                case Station.Supply:
                    if (Carry == Food.Empty) { Carry = Food.RawDodo; Respond("¡Dodo fresco de hace 65 millones de años! Toca CORTE."); }
                    else Respond("Ya llevas algo. Entrégalo o usa BASURA.", "bad"); break;
                case Station.Roots:
                    if (Carry == Food.Empty) { Carry = Food.Root; Respond("Una raíz. Toca RAÍCES otra vez para recoger la segunda."); }
                    else if (Carry == Food.Root || Carry == Food.ChoppedRoot) { Carry = Food.Roots; Respond("Dos raíces listas. Puedes cocinarlas directamente o cortarlas juntas."); }
                    else Respond("Necesitas las manos libres o una sola raíz.", "bad"); break;
                case Station.Cut:
                    if (work > 0) { Respond("La cuchilla láser sigue trabajando. Puedes ir a otra estación.", "click"); break; }
                    if (PrepFood != Food.Empty)
                    {
                        if (Carry == Food.Empty) { Carry = PrepFood; PrepFood = Food.Empty; Respond("Recogido: " + KitchenRules.FoodNames[(int)Carry] + "."); }
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
                    if ((Carry == Food.ChoppedDodo || Carry == Food.Roots || Carry == Food.ChoppedRoots) && StoveFood == Food.Empty)
                    {
                        CookDuration = (Carry == Food.ChoppedDodo ? 5 : 7) / (1 + (Progress?.Heat ?? 0) * .15f);
                        StoveFood = Carry == Food.ChoppedRoots ? Food.Roots : Carry; Carry = Food.Empty; CookTime = 0; Respond("Plasma encendido. Retira la comida cuando esté LISTA.", "cook");
                    }
                    else if (Carry == Food.Empty && (StoveFood == Food.CookedDodo || StoveFood == Food.Soup || StoveFood == Food.Burnt))
                    { Carry = StoveFood; StoveFood = Food.Empty; Respond(Carry == Food.Burnt ? "A BASURA. Ni Grog comería esto." : Carry == Food.Soup ? "Sopa servida en bol de energía. ¡A entregar!" : "¡Perfecto! Toca MONTAJE para añadir pan."); }
                    else Respond(Carry != Food.Empty ? "Libera las manos o espera a que el fogón esté libre." : "Todavía se cocina. ¡No mires el reloj tan fuerte!", "bad"); break;
                case Station.Assemble:
                    if (Carry == Food.CookedDodo) { Carry = Food.Burger; Respond("¡Cyber-Dodo Burger! Toca un cliente que pida hamburguesa.", "ready"); }
                    else Respond("Trae dodo cocinado. La sopa ya sale del fogón en su bol.", "bad"); break;
                case Station.Trash:
                    if (Carry != Food.Empty) { Carry = Food.Empty; Combo = 0; AddAnomaly(10); Respond("Basura enviada a otra línea temporal. +10 anomalía.", "bad"); } break;
                default:
                    int customer = (int)station - 5;
                    if (customer < 0 || customer >= 3) return;
                    if (!Customers[customer]) { Respond("El próximo viajero está llegando.", "bad"); return; }
                    if (Carry != Orders[customer]) { Respond("Este cliente pide " + KitchenRules.FoodNames[(int)Orders[customer]] + ". Llevas: " + KitchenRules.FoodNames[(int)Carry] + ".", "bad"); return; }
                    Carry = Food.Empty; Customers[customer] = false; respawn[customer] = LevelRules.ReturnDelay; Combo++;
                    int reward = 100 + Mathf.CeilToInt(Patience[customer]) + Mathf.Min(Combo, 5) * 10;
                    Credits += reward; Served++; Reputation = Mathf.Min(100, Reputation + 3); Anomaly = Mathf.Max(0, Anomaly - 8);
                    Speech[customer] = "+" + reward + " créditos · ¡Delicioso!"; speechTime[customer] = 3;
                    Respond("¡ENTREGADO! +" + reward + " créditos. Combo x" + Combo + ".", "serve"); break;
            }
            if (TutorialActive && (previousCarry != Carry || previousStove != StoveFood || previousPrep != PrepFood || previousServed != Served)) AdvanceTutorial();
        }
        public void AddAnomaly(float amount)
        {
            Anomaly = Mathf.Min(100, Anomaly + amount);
            if (Anomaly < 100 || CloneRemaining > 0) return;
            Anomaly = 0; CloneRemaining = 15;
            var candidates = new List<Vector2Int>();
            for (int x = 2; x < 10; x++) for (int y = 1; y < 3; y++)
            { var p = new Vector2Int(x, y); if (p != cell && KitchenRules.IsWalkable(p, blocked) && !KitchenRules.IsStationApproach(p) && !route.Contains(p)) candidates.Add(p); }
            if (candidates.Count == 0) { CloneRemaining = 0; return; }
            blocked = candidates[UnityEngine.Random.Range(0, candidates.Count)];
            Respond("¡PARADOJA! Tu yo del futuro bloquea una casilla durante 15 s.", "paradox");
        }
    }
}

