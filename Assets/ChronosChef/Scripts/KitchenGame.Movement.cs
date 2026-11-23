using UnityEngine;
using UnityEngine.InputSystem;

namespace ChronosChef
{
    public sealed partial class KitchenGame
    {
        const float Reach = 69f;
        Vector2 stick, velocity;
        public Vector2 Facing { get; private set; } = Vector2.down;
        public float Travelled { get; private set; }
        public float GestureRemaining { get; private set; }
        public bool ServingGesture { get; private set; }
        public Station? NearbyStation { get; private set; }

        public void SetStick(Vector2 value)
        {
            stick = Running && !Paused ? Vector2.ClampMagnitude(value, 1) : Vector2.zero;
        }

        void ResetMovement()
        {
            stick = velocity = Vector2.zero;
            Facing = Vector2.down;
            NearbyStation = null;
            Travelled = GestureRemaining = 0;
        }

        void FaceStation(Station station)
        {
            Facing = station == Station.Roots || station == Station.Trash || station == Station.Supply
                ? Vector2.left : station == Station.Assemble ? Vector2.right : Vector2.up;
        }

        public void UseNearbyStation()
        {
            if (!Running || Paused) return;
            FindNearbyStation();
            if (!NearbyStation.HasValue) return;
            route.Clear(); destination = null; DestinationLabel = "";
            velocity = Vector2.zero;
            Interact(NearbyStation.Value);
        }

        void FindNearbyStation()
        {
            NearbyStation = null;
            float best = float.MaxValue;
            for (int i = 0; i <= (int)Station.Roots; i++)
            {
                var station = (Station)i;
                if (TutorialActive && TutorialStation != station) continue;
                if (i >= 5 && i <= 7 && !Customers[i - 5]) continue;
                var approach = KitchenRules.Position(KitchenRules.Approach(station));
                var offset = approach - ChefPosition;
                float distance = offset.magnitude;
                if (distance > Reach || !KitchenRules.ClearSegment(ChefPosition, approach, blocked)) continue;
                float score = distance - Vector2.Dot(offset.normalized, Facing) * 8;
                if (score >= best) continue;
                best = score;
                NearbyStation = station;
            }
        }

        Vector2Int RouteAnchor()
        {
            float best = float.MaxValue;
            var nearest = cell;
            for (int x = 0; x < KitchenRules.Columns; x++)
            for (int y = 0; y < KitchenRules.Rows; y++)
            {
                var candidate = new Vector2Int(x, y);
                var point = KitchenRules.Position(candidate);
                float distance = (point - ChefPosition).sqrMagnitude;
                if (distance >= best || !KitchenRules.IsWalkable(candidate, blocked) ||
                    !KitchenRules.ClearSegment(ChefPosition, point, blocked)) continue;
                nearest = candidate;
                best = distance;
            }
            return nearest;
        }

        void MoveChef(float dt)
        {
            var keyboard = Keyboard.current;
            Vector2 input = stick;
            if (keyboard != null)
            {
                var keys = new Vector2(
                    (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1 : 0) - (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1 : 0),
                    (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed ? 1 : 0) - (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed ? 1 : 0));
                if (keys != Vector2.zero) input = keys.normalized;
            }
            var before = ChefPosition;
            float speed = 340 + (Progress?.Speed ?? 0) * 60;
            if (input.sqrMagnitude > .001f)
            {
                route.Clear(); destination = null; DestinationLabel = "";
                Facing = input.normalized;
                velocity = Vector2.MoveTowards(velocity, input * speed, 9 * speed * dt);
                var step = velocity * dt;
                var limit = new Vector2(Mathf.Clamp(before.x + step.x, 360, 1240), Mathf.Clamp(before.y + step.y, 245, 485));
                var next = before;
                if (KitchenRules.ClearSegment(before, limit, blocked)) next = limit;
                else
                {
                    // Keep tangential travel when a thumb is held diagonally against a table.
                    var horizontal = new Vector2(limit.x, before.y);
                    if (KitchenRules.ClearSegment(before, horizontal, blocked)) next = horizontal;
                    var vertical = new Vector2(next.x, limit.y);
                    if (KitchenRules.ClearSegment(next, vertical, blocked)) next = vertical;
                }
                ChefPosition = next;
                if (next == before) velocity = Vector2.zero;
                else
                {
                    var rounded = new Vector2Int(Mathf.RoundToInt((next.x-360)/80), Mathf.RoundToInt((next.y-245)/80));
                    if (KitchenRules.IsWalkable(rounded, blocked)) cell = rounded;
                }
            }
            else
            {
                velocity = Vector2.zero;
                float budget = speed * dt;
                while (route.Count > 0 && budget > 0)
                {
                    var point = KitchenRules.Position(route[0]);
                    float distance = Vector2.Distance(ChefPosition, point);
                    if (distance <= budget) { ChefPosition = point; budget -= distance; cell = route[0]; route.RemoveAt(0); }
                    else { ChefPosition = Vector2.MoveTowards(ChefPosition, point, budget); break; }
                }
                if ((ChefPosition - before).sqrMagnitude > .001f) Facing = (ChefPosition - before).normalized;
            }
            Travelled += Vector2.Distance(before, ChefPosition);
            GestureRemaining = Mathf.Max(0, GestureRemaining - dt);
            FindNearbyStation();
            if (keyboard != null && keyboard.eKey.wasPressedThisFrame) UseNearbyStation();
        }
    }
}
