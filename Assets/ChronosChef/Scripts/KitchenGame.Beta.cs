using UnityEngine;
namespace ChronosChef
{
    public sealed partial class KitchenGame
    {
        bool booting;
        float autosave = 5;
        public bool TutorialActive { get; private set; }
        public bool IsTrainingResult { get; private set; }
        public int TutorialStep { get; private set; }
        public bool IsMoving => route.Count > 0 || velocity.sqrMagnitude > 1;
        public bool IsWorking => work > 0;
        public bool HasDestination => destination.HasValue;
        static readonly Station[] Training = { Station.Supply, Station.Cut, Station.Cut, Station.Stove, Station.Stove, Station.Assemble, Station.Customer0, Station.Roots, Station.Roots, Station.Stove, Station.Stove, Station.Customer1 };
        public int TutorialTotal => Training.Length;
        static readonly string[] Instructions =
        {
            "1/12 · Toca la nevera DODO para recoger carne.", "2/12 · Toca CORTE: la carne queda en la mesa mientras trabaja la cuchilla.",
            "3/12 · Espera el corte y toca CORTE otra vez para recoger la carne.",
            "4/12 · Toca PLASMA para colocar el dodo cortado.", "5/12 · Espera LISTO y vuelve a tocar PLASMA para retirar la carne.",
            "6/12 · Toca el PAN de MONTAJE para preparar la hamburguesa.", "7/12 · Toca al primer cliente o su tarjeta: pide hamburguesa.",
            "8/12 · Ahora la sopa: toca RAÍCES para recoger la primera.", "9/12 · Toca RAÍCES otra vez: necesitas dos.",
            "10/12 · Toca PLASMA: las raíces se cocinan sin cortar, o puedes prepararlas en la mesa.", "11/12 · Espera LISTO y toca PLASMA para retirar el bol.",
            "12/12 · Entrega la sopa al robot: toca su cuerpo o su pedido."
        };
        public Station TutorialStation => Training[Mathf.Clamp(TutorialStep, 0, Training.Length - 1)];
        public string TutorialInstruction => Instructions[Mathf.Clamp(TutorialStep, 0, Instructions.Length - 1)];
        void ResetBetaState()
        {
            TutorialActive = IsTrainingResult = false; TutorialStep = 0; autosave = 5;
            if (!booting) Progress?.ClearSession();
        }
        void AdvanceTutorial()
        {
            if (!TutorialActive) return;
            TutorialStep++;
            if (TutorialStep < Training.Length) return;
            TutorialActive = false; IsTrainingResult = true; Running = false; Paused = false;
            route.Clear(); destination = null; Progress.CompleteTutorial(); Progress.ClearSession();
            Feedback?.Invoke("win"); view.ShowResults(true, this);
        }
        public void SaveCurrentSession()
        {
            if (booting || !Running || TutorialActive || Progress == null || Remaining <= 0 || Reputation <= 0) return;
            var session = new KitchenSession
            {
                level = Level, carry = Carry, stove = StoveFood, prep = PrepFood, remaining = Remaining, anomaly = Anomaly, cloneTime = CloneRemaining,
                borrowRemaining = BorrowRemaining, borrowCooldown = BorrowCooldown, boardJam = BoardJam, meterDiscovered = MeterDiscovered,
                credits = Credits, served = Served, lost = Lost, reputation = Reputation, combo = Combo, arrivals = arrivals,
                cell = cell, blocked = blocked, cookTime = CookTime, cookDuration = CookDuration, work = work,
                hasExactPosition = true, position = ChefPosition, facing = Facing,
                customers = (bool[])Customers.Clone(), special = (bool[])special.Clone(), orders = (Food[])Orders.Clone(), species = (int[])Species.Clone(),
                patience = (float[])Patience.Clone(), maxPatience = (float[])MaxPatience.Clone(), respawn = (float[])respawn.Clone()
            };
            if (session.IsValid()) Progress.SaveSession(session);
        }
        public bool ResumeSavedShift()
        {
            var s = Progress.LoadSession(); if (s == null) return false;
            ResetMovement();
            TutorialActive = IsTrainingResult = false; Level = s.level; view.SetLevelEnvironment(Level);
            Carry = s.carry; StoveFood = s.stove; PrepFood = s.prep; Remaining = s.remaining; Anomaly = s.anomaly; CloneRemaining = s.cloneTime;
            BoardJam = Mathf.Clamp(s.boardJam,0,4); MeterDiscovered = s.meterDiscovered || Anomaly>0 || CloneRemaining>0;
            BorrowRemaining = s.borrowRemaining; BorrowCooldown = s.borrowCooldown;
            Credits = s.credits; Served = s.served; Lost = s.lost; Reputation = s.reputation; Combo = s.combo; arrivals = s.arrivals;
            cell = s.cell; blocked = s.cloneTime > 0 ? s.blocked : new Vector2Int(-1, -1); ChefPosition = KitchenRules.Position(cell);
            if (s.hasExactPosition) { ChefPosition = s.position; Facing = s.facing.sqrMagnitude > .01f ? s.facing.normalized : Vector2.down; }
            CookTime = s.cookTime; CookDuration = s.cookDuration; work = s.work; TaskLabel = work > 0 ? "CORTANDO" : ""; DestinationLabel = "";
            if (work > 0 && PrepFood == Food.Empty && Carry == Food.RawDodo) { PrepFood = Carry; Carry = Food.Empty; }
            view.ResetDiningQueue(s.customers);
            Customers = s.customers; Orders = s.orders; Species = s.species; Patience = s.patience; MaxPatience = s.maxPatience;
            for (int i = 0; i < 3; i++) { special[i] = s.special[i]; respawn[i] = s.respawn[i]; speechTime[i] = 0; Speech[i] = ""; banterCountdown[i] = 8 + i * 2; banterLine[i] = 0; }
            route.Clear(); destination = null; Running = true; Paused = true; autosave = 5;
            Say("Partida recuperada. Continúa cuando estés listo."); view.ShowMenu(false); view.ShowResults(false, this); view.Refresh(this); return true;
        }
    }
}
