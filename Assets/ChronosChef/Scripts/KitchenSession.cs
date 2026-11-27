using System;
using UnityEngine;
namespace ChronosChef
{
    [Serializable]
    public sealed class KitchenSession
    {
        public int version = 1, level, credits, served, lost, reputation, combo, arrivals;
        public Food carry, stove, prep;
        public float borrowRemaining, borrowCooldown, boardJam;
        public bool meterDiscovered;
        public float remaining, anomaly, cloneTime, cookTime, cookDuration, work;
        public Vector2Int cell, blocked;
        public bool hasExactPosition;
        public Vector2 position, facing;
        public bool[] customers, special;
        public Food[] orders;
        public int[] species;
        public float[] patience, maxPatience, respawn;
        public bool IsValid()
        {
            if (version != 1 || level < 1 || level > KitchenLevel.Count || remaining <= 0 || remaining > KitchenLevel.For(level).Seconds || !Finite(remaining)) return false;
            if (!Enum.IsDefined(typeof(Food), carry) || !Enum.IsDefined(typeof(Food), stove) || !Enum.IsDefined(typeof(Food), prep)) return false;
            if (!Finite(boardJam) || boardJam < 0 || boardJam > 4) return false;
            if (!Finite(borrowRemaining) || !Finite(borrowCooldown) || borrowRemaining < 0 || borrowRemaining > KitchenGame.BorrowDuration || borrowCooldown < borrowRemaining || borrowCooldown > KitchenGame.BorrowRecharge) return false;
            if (customers?.Length != 3 || orders?.Length != 3 || species?.Length != 3 || patience?.Length != 3 || maxPatience?.Length != 3 || respawn?.Length != 3 || special?.Length != 3) return false;
            if (!KitchenRules.IsWalkable(cell, new Vector2Int(-1, -1)) || !Finite(cookDuration) || cookDuration < 1 || cookDuration > 7 || !Finite(cookTime) || cookTime < 0 || !Finite(work) || work < 0 || work > 1.6f) return false;
            if (!Finite(anomaly) || anomaly < 0 || anomaly > 100 || !Finite(cloneTime) || cloneTime < 0 || cloneTime > 15 || reputation < 1 || reputation > 100 || credits < 0 || served < 0 || lost < 0 || combo < 0 || arrivals < 3) return false;
            if (cloneTime > 0 && (!KitchenRules.IsWalkable(blocked, cell) || KitchenRules.IsStationApproach(blocked))) return false;
            if (hasExactPosition && (!Finite(position.x) || !Finite(position.y) || !Finite(facing.x) || !Finite(facing.y)
                || position.x < 360 || position.x > 1240 || position.y < 245 || position.y > 485
                || !KitchenRules.ClearSegment(position, position, cloneTime > 0 ? blocked : new Vector2Int(-1,-1)))) return false;
            for (int i = 0; i < 3; i++) if (KitchenRecipe.Index(orders[i]) < 0 || species[i] < 0 || species[i] > 3 || !Finite(patience[i]) || !Finite(maxPatience[i]) || !Finite(respawn[i]) || maxPatience[i] <= 0 || patience[i] > maxPatience[i] || respawn[i] < 0 || respawn[i] > 10) return false;
            return true;
        }
        static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
