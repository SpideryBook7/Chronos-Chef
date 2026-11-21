var empty = new UnityEngine.Vector2Int(-1, -1);
var nodes = new System.Collections.Generic.List<UnityEngine.Vector2Int>();
for (int x = 0; x < 12; x++) for (int y = 0; y < 4; y++)
{
    var node = new UnityEngine.Vector2Int(x, y);
    if (ChronosChef.KitchenRules.IsWalkable(node, empty)) nodes.Add(node);
}
// Directly cross both tables: the old grid returned the straight upper-row path.
var detour = ChronosChef.KitchenRules.Route(new UnityEngine.Vector2Int(2, 3), new UnityEngine.Vector2Int(9, 3), empty);
if (detour.Count <= 7) throw new System.Exception("Route failed to detour around cutting/plasma stations.");
int checkedRoutes = 0;
foreach (var from in nodes) foreach (var to in nodes)
{
    var path = ChronosChef.KitchenRules.Route(from, to, empty);
    if (from != to && path.Count == 0) throw new System.Exception("Unreachable free floor cell.");
    var previous = ChronosChef.KitchenRules.Position(from);
    foreach (var cell in path)
    {
        var next = ChronosChef.KitchenRules.Position(cell);
        for (int sample = 0; sample <= 20; sample++)
        {
            var p = UnityEngine.Vector2.Lerp(previous, next, sample / 20f);
            if (new UnityEngine.Rect(558, 423, 224, 159).Contains(p) || new UnityEngine.Rect(833, 420, 224, 164).Contains(p))
                throw new System.Exception("A movement segment intersects a fixture with chef clearance.");
        }
        previous = next;
    }
    checkedRoutes++;
}
for (int index = 0; index < 9; index++)
{
    var target = ChronosChef.KitchenRules.Approach((ChronosChef.Station)index);
    if (!ChronosChef.KitchenRules.IsWalkable(target, empty)) throw new System.Exception("Station approach is inside furniture.");
    foreach (var clone in nodes)
    {
        if (clone.x < 2 || clone.x > 9 || clone.y < 1 || clone.y > 2 || clone == target) continue;
        var route = ChronosChef.KitchenRules.Route(new UnityEngine.Vector2Int(0, 0), target, clone);
        if (route.Count == 0 && target != UnityEngine.Vector2Int.zero || route.Contains(clone)) throw new System.Exception("Clone blocked a station or route entered clone.");
    }
}
var result = "PASS: " + checkedRoutes + " routes; sampled movement segments avoid both fixtures; all nine station approaches reachable, including clone obstacles.";
System.IO.File.WriteAllText("Tests/navigation-result.txt", result);
return result;
