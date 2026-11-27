using UnityEngine;

namespace ChronosChef
{
    // Rows follow the destination catalogue. Columns share the grill, pot and cold-prep stations.
    internal static class RegionalMenu
    {
        static readonly string[,] dishes = {
            {"Brioche de huerta","Crema de calabaza","Pan con tomate"},
            {"Bollo de caldera","Caldo de cobre","Tosta ahumada"},
            {"Mollete del muelle","Sopa de coral","Tosta de salicornia"},
            {"Bocata alpino","Crema de refugio","Tosta de arándanos"},
            {"Bocado volcánico","Caldo de helecho","Tosta de bayas"},
            {"Órbita solar","Consomé de plasma","Cristal de raíz"},
            {"Luna rellena","Bisque de esporas","Tosta de nébula"}
        };
        static readonly string[,] guests = {
            {"BRUNO","MOLINÍN","FLORA","PIPO"}, {"REMACHE","BIP-08","ÁMBAR","PISTÓN"},
            {"CAPITÁN SAL","SONAR","NÁCARA","BOYA"}, {"ALDO","TERMOS","NIEVES","YETI"},
            {"GROG","FÓSIL-9","LÍA","REX"}, {"AXEL","ION-4","NOVA","QUARK"},
            {"ORBU","PULSO","ZORG","KIRI"}
        };
        internal static string Dish(int level, Food food)
        {
            int index = KitchenRecipe.Index(food);
            return index < 0 ? KitchenRules.FoodNames[(int)food] : dishes[KitchenLevel.World(level),index];
        }
        internal static string Guest(int level,int kind) => guests[KitchenLevel.World(level),kind];
        public static bool SoupNeedsDicing(int level)
        {
            int world=KitchenLevel.World(level);
            return world==1 || world==3 || world==6;
        }
        static readonly string[] produce = {"calabaza","tubérculo cobrizo","salicornia","raíz alpina","helecho","raíz solar","espora"};
        internal static string Produce(int level) => produce[KitchenLevel.World(level)];
        internal static float CookSeconds(int level, bool broth)
        {
            int world = KitchenLevel.World(level);
            return broth ? 7 - (world % 3) * .45f : 5 + (world % 4) * .35f;
        }
    }
}
