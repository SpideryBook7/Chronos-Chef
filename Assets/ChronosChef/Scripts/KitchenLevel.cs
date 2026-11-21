namespace ChronosChef
{
    public readonly struct KitchenLevel
    {
        public readonly string Name;
        public readonly int Seconds, Goal;
        public readonly float Patience, ReturnDelay, BurnWindow;
        public KitchenLevel(string name, int seconds, int goal, float patience, float delay, float burn)
        { Name = name; Seconds = seconds; Goal = goal; Patience = patience; ReturnDelay = delay; BurnWindow = burn; }
        public static readonly KitchenLevel[] All =
        {
            new KitchenLevel("Primer servicio", 150, 3, 100, 5, 7),
            new KitchenLevel("Raíces del tiempo", 150, 4, 95, 5, 7),
            new KitchenLevel("Hora del meteorito", 165, 5, 90, 4, 6),
            new KitchenLevel("Hambre jurásica", 180, 6, 85, 4, 6),
            new KitchenLevel("La última cena del dodo", 195, 7, 80, 3, 5),
            new KitchenLevel("Té y plasma", 165, 5, 85, 4, 6),
            new KitchenLevel("El turno de las máquinas", 180, 6, 80, 4, 6),
            new KitchenLevel("Vapor a presión", 195, 7, 75, 3, 5),
            new KitchenLevel("Paradoja industrial", 210, 8, 72, 3, 5),
            new KitchenLevel("Banquete del fin del tiempo", 225, 9, 70, 3, 5)
        };
        public static KitchenLevel For(int level) => All[UnityEngine.Mathf.Clamp(level - 1, 0, All.Length - 1)];
    }
}
