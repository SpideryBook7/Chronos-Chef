namespace ChronosChef
{
    public readonly struct RecipeStep
    {
        public readonly Food Picture;
        public readonly string Name, Instruction, Joke;
        public RecipeStep(Food picture, string name, string instruction, string joke)
        { Picture = picture; Name = name; Instruction = instruction; Joke = joke; }
    }
    public sealed class KitchenRecipe
    {
        public readonly Food Dish;
        public readonly string Tab, Card;
        public readonly Food[] Ingredients;
        public readonly RecipeStep[] Steps;
        KitchenRecipe(Food dish, string tab, string card, Food[] ingredients, params RecipeStep[] steps)
        { Dish = dish; Tab = tab; Card = card; Ingredients = ingredients; Steps = steps; }
        public static readonly KitchenRecipe[] All =
        {
            new KitchenRecipe(Food.Burger, "CYBER-DODO BURGER", "CYBER-DODO\nBURGER", new[] { Food.RawDodo, Food.ChoppedDodo, Food.CookedDodo, Food.Bread },
                new RecipeStep(Food.RawDodo, "RECOGER DODO", "Toca el almacén de carne. Llevarás una porción de dodo crudo.", "El dodo está fresco. Geológicamente hablando."),
                new RecipeStep(Food.RawDodo, "DEJAR EN CORTE", "Toca CORTE: dejas la carne y la cuchilla trabaja sola. Puedes ir a otra estación.", "La cuchilla ya cortó el lunes. Dos veces."),
                new RecipeStep(Food.ChoppedDodo, "RECOGER DE LA MESA", "Cuando termine, toca CORTE con las manos libres. El ingrediente permanece allí hasta que lo recoges.", "La mesa no es un portal. Tu comida se queda."),
                new RecipeStep(Food.CookedDodo, "COCINAR Y RETIRAR", "Pon la carne cortada en PLASMA. Espera LISTO y toca el fogón otra vez antes de que se queme.", "Cinco segundos de plasma. No cinco eras."),
                new RecipeStep(Food.Bread, "AÑADIR PAN", "Lleva la carne cocinada al PAN de MONTAJE. Allí se monta la hamburguesa; no recoges una ya hecha.", "Es pan. No una hamburguesa prefabricada del futuro."),
                new RecipeStep(Food.Burger, "ENTREGAR", "Busca el pedido de hamburguesa y toca el cliente, su cabeza o su tarjeta.", "Propinas en monedas. Los meteoritos no caben.")),
            new KitchenRecipe(Food.Soup, "SOPA CUÁNTICA", "SOPA\nCUÁNTICA", new[] { Food.Root, Food.Root, Food.Roots, Food.Soup },
                new RecipeStep(Food.Roots, "RECOGER DOS RAÍCES", "Toca RAÍCES dos veces para reunir dos unidades.", "Dos raíces. La tercera invoca al brócoli."),
                new RecipeStep(Food.ChoppedRoots, "CORTE OPCIONAL", "Si quieres, deja las raíces en CORTE. Las raíces cortadas también sirven para la sopa; recógelas al terminar.", "¡Mise en place espacial! Suena carísimo."),
                new RecipeStep(Food.Roots, "COCINAR EN PLASMA", "Pon las dos raíces, enteras o cortadas, en PLASMA. No mezcles otro plato en un fogón ocupado.", "Si la sopa te mira, no la mires de vuelta."),
                new RecipeStep(Food.Soup, "RETIRAR EL BOL", "Espera LISTO y toca PLASMA con las manos libres. La sopa ya sale en su bol de energía.", "El bol incluye garantía cuántica. Quizá."),
                new RecipeStep(Food.Soup, "ENTREGAR", "Entrega al cliente que muestre sopa. Una hamburguesa no arregla una crisis existencial de sopa.", "Servicio completado. El universo puede seguir girando.")),
            new KitchenRecipe(Food.Toast, "TOSTADA MUTANTE", "TOSTADA\nMUTANTE", new[] { Food.Root, Food.ChoppedRoot, Food.Bread, Food.Toast },
                new RecipeStep(Food.Root, "RECOGER UNA RAÍZ", "Recoge una sola raíz neón.", "Una verdura con ambiciones."),
                new RecipeStep(Food.Root, "DEJAR EN CORTE", "Deja la raíz en CORTE. La cuchilla trabaja sola.", "Los hipercubos cuestan extra."),
                new RecipeStep(Food.ChoppedRoot, "RECOGER LOS CUBOS", "Con las manos libres, recoge la raíz cortada.", "El cubo te ha guiñado el ojo."),
                new RecipeStep(Food.Bread, "MONTAR EN PAN", "Lleva los cubos al PAN, directamente desde la tabla.", "Pan abajo, mutante arriba."),
                new RecipeStep(Food.Toast, "ENTREGAR", "Toca al cliente que pida tostada. Aparece desde el nivel 3.", "La aceituna ha pedido un aumento."))
        };
        public static int Index(Food dish)
        {
            for (int i = 0; i < All.Length; i++) if (All[i].Dish == dish) return i;
            return -1;
        }
        static readonly Food[] EarlyOrders = { Food.Burger, Food.Soup, Food.Burger };
        static readonly Food[] FullOrders = { Food.Burger, Food.Soup, Food.Toast, Food.Burger, Food.Toast, Food.Soup };
        public static Food OrderFor(int level, int arrival)
        {
            var menu = level < 3 ? EarlyOrders : FullOrders;
            return menu[arrival % menu.Length];
        }
    }
}
