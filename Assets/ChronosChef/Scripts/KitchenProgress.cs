using UnityEngine;
namespace ChronosChef
{
    public sealed class KitchenProgress
    {
        readonly string prefix;
        public KitchenProgress(string keyPrefix = "ChronosChef.v2.") { prefix = keyPrefix; }
        int Get(string key, int fallback = 0) => PlayerPrefs.GetInt(prefix + key, fallback);
        void Set(string key, int value) => PlayerPrefs.SetInt(prefix + key, value);
        public int Bank => Mathf.Clamp(Get("credits"), 0, 100000000);
        public int Gems => Mathf.Clamp(Get("gems"), 0, 30);
        public int Unlocked => Mathf.Clamp(Get("unlocked", 1), 1, 10);
        public int Era => Mathf.Clamp(Get("era", 1), 1, 2);
        public int Speed => Mathf.Clamp(Get("speed"), 0, 3);
        public int Heat => Mathf.Clamp(Get("heat"), 0, 3);
        public int BestStars(int level) => Mathf.Clamp(Get("stars." + level), 0, 3);
        public bool TutorialDone => Get("tutorial") == 1;
        public void CompleteTutorial() { Set("tutorial", 1); PlayerPrefs.Save(); }
        public void SaveSession(KitchenSession session) { PlayerPrefs.SetString(prefix + "session", JsonUtility.ToJson(session)); PlayerPrefs.Save(); }
        public void ClearSession() { PlayerPrefs.DeleteKey(prefix + "session"); PlayerPrefs.Save(); }
        public KitchenSession LoadSession()
        {
            try { var json = PlayerPrefs.GetString(prefix + "session", ""); if (string.IsNullOrEmpty(json)) return null; var session = JsonUtility.FromJson<KitchenSession>(json); if (session != null && session.IsValid() && session.level <= Unlocked && (session.level <= 5 || Era == 2)) return session; }
            catch (System.ArgumentException) { }
            ClearSession(); return null;
        }
        public void Complete(int level, int credits, int stars)
        {
            if (level < 1 || level > 10) throw new System.ArgumentOutOfRangeException(nameof(level));
            stars = Mathf.Clamp(stars, 0, 3);
            Set("credits", (int)System.Math.Min(100000000L, (long)Bank + Mathf.Max(0, credits)));
            if (stars == 3 && BestStars(level) < 3) Set("gems", Gems + 1);
            Set("stars." + level, Mathf.Max(BestStars(level), stars));
            if (stars > 0) Set("unlocked", Mathf.Max(Unlocked, Mathf.Min(10, level + 1)));
            PlayerPrefs.Save();
        }
        public string Buy(bool speed)
        {
            string key = speed ? "speed" : "heat"; int tier = Get(key), price = 300 * (tier + 1);
            if (tier >= 3) return "Mejora al máximo.";
            if (Bank < price) return "Necesitas " + price + " créditos.";
            Set("credits", Bank - price); Set(key, tier + 1); PlayerPrefs.Save();
            return speed ? "¡Botas temporales mejoradas!" : "¡Fogón de plasma mejorado!";
        }
        public string UnlockEra()
        {
            if (Era == 2) return "La era Steampunk ya está abierta.";
            if (Gems < 3) return "Consigue 3 gemas: una por nivel con 3 estrellas.";
            Set("gems", Gems - 3); Set("era", 2); PlayerPrefs.Save(); return "¡Portal Steampunk desbloqueado!";
        }
    }
}
