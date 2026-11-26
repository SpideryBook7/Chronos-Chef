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
            new KitchenLevel("Primer servicio", 160, 3, 100, 5, 7),
            new KitchenLevel("Raíces del tiempo", 180, 4, 95, 5, 7),
            new KitchenLevel("Hora del meteorito", 195, 5, 90, 4, 6),
            new KitchenLevel("Hambre jurásica", 180, 6, 85, 4, 6),
            new KitchenLevel("La última cena del dodo", 195, 7, 80, 3, 5),
            new KitchenLevel("Té y plasma", 165, 5, 85, 4, 6),
            new KitchenLevel("El turno de las máquinas", 180, 6, 80, 4, 6),
            new KitchenLevel("Vapor a presión", 195, 7, 75, 3, 5),
            new KitchenLevel("Paradoja industrial", 210, 8, 72, 3, 5),
            new KitchenLevel("Banquete del fin del tiempo", 225, 9, 70, 3, 5),
            new KitchenLevel("Muelle al alba", 200, 7, 80, 3, 5),
            new KitchenLevel("La lonja", 203, 7, 79, 3, 5),
            new KitchenLevel("Hora de la marea", 206, 7, 78, 3, 5),
            new KitchenLevel("Turno del faro", 209, 8, 77, 3, 5),
            new KitchenLevel("Banquete del puerto", 212, 8, 76, 3, 5),
            new KitchenLevel("El refugio", 215, 8, 75, 3, 5),
            new KitchenLevel("Fuego en la nieve", 218, 9, 74, 3, 5),
            new KitchenLevel("La gran subida", 221, 9, 73, 3, 5),
            new KitchenLevel("Cena de invierno", 224, 9, 72, 3, 5),
            new KitchenLevel("Fiesta en la cima", 227, 10, 71, 3, 5),
            new KitchenLevel("El nido temprano", 220, 9, 71, 3, 5),
            new KitchenLevel("Caldo de helechos", 224, 9, 69, 3, 5),
            new KitchenLevel("La hora del rugido", 228, 10, 67, 3, 5),
            new KitchenLevel("Fogón de lava", 232, 10, 65, 3, 5),
            new KitchenLevel("Banquete del volcán", 236, 11, 63, 3, 5),
            new KitchenLevel("Desayuno orbital", 225, 10, 71, 3, 5),
            new KitchenLevel("Cosecha de sol", 229, 10, 69, 3, 5),
            new KitchenLevel("El expreso solar", 233, 11, 67, 3, 5),
            new KitchenLevel("Servicio en gravedad cero", 237, 11, 65, 3, 5),
            new KitchenLevel("La última lanzadera", 241, 12, 63, 3, 5),
            new KitchenLevel("Mesa para tres lunas", 230, 11, 71, 3, 5),
            new KitchenLevel("Hongos de medianoche", 234, 11, 69, 3, 5),
            new KitchenLevel("Un pedido telepático", 238, 12, 67, 3, 5),
            new KitchenLevel("La lluvia de esporas", 242, 12, 65, 3, 5),
            new KitchenLevel("El gran festín galáctico", 246, 13, 63, 3, 5),
        };
        public static int Count => All.Length;
        public readonly struct Destination
        {
            public readonly string Name, Map, Kitchen;
            public Destination(string name,string map,string kitchen)
            { Name=name; Map=map; Kitchen=kitchen; }
        }
        public static readonly Destination[] Worlds = {
            new Destination("TIERRA","EarthRoute","EarthService"),
            new Destination("FERRUM","FerrumRoute","FerrumService"),
            new Destination("BRUMA","BrumaRoute","Bruma"),
            new Destination("CUMBRE","CumbreRoute","Cumbre"),
            new Destination("JURÁSICO","JurassicRoute","JurassicKitchen"),
            new Destination("FUTURO","SolariaRoute","SolariaKitchen"),
            new Destination("XENO","XenoRoute","XenoKitchen")
        };
        public static int World(int level) => UnityEngine.Mathf.Clamp((level-1)/5,0,Worlds.Length-1);
        public static KitchenLevel For(int level) => All[UnityEngine.Mathf.Clamp(level - 1, 0, All.Length - 1)];
    }
}
