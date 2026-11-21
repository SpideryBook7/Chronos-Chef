var dimensionsPath = "Tools/ViewResolution.txt";
var dimensions = System.IO.File.Exists(dimensionsPath) ? System.IO.File.ReadAllText(dimensionsPath).Trim().Split('x') : new[] { "1600", "900" };
int width = int.Parse(dimensions[0]), height = int.Parse(dimensions[1]);
if (width < 640 || height < 360) throw new System.ArgumentException("Invalid view dimensions");
var assembly = typeof(UnityEditor.Editor).Assembly;
var sizesType = assembly.GetType("UnityEditor.GameViewSizes");
var singleton = typeof(UnityEditor.ScriptableSingleton<>).MakeGenericType(sizesType);
var sizes = singleton.GetProperty("instance").GetValue(null);
var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
var groupKind = sizesType.GetProperty("currentGroupType", flags).GetValue(sizes);
var group = sizesType.GetMethod("GetGroup").Invoke(sizes, new object[] { groupKind });
var groupType = group.GetType();
var total = (int)groupType.GetMethod("GetTotalCount").Invoke(group, null);
var viewSize = assembly.GetType("UnityEditor.GameViewSize");
var sizeKind = assembly.GetType("UnityEditor.GameViewSizeType");
int chosen = -1;
for (int i = 0; i < total; i++)
{
    var existing = groupType.GetMethod("GetGameViewSize").Invoke(group, new object[] { i });
    if (System.Convert.ToInt32(viewSize.GetProperty("width")?.GetValue(existing)) == width && System.Convert.ToInt32(viewSize.GetProperty("height")?.GetValue(existing)) == height) { chosen = i; break; }
}
if (chosen < 0)
{
    var constructor = viewSize.GetConstructor(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic, null, new[] { sizeKind, typeof(int), typeof(int), typeof(string) }, null);
    var size = constructor.Invoke(new object[] { System.Enum.Parse(sizeKind, "FixedResolution"), width, height, "Chronos Chef " + width + "x" + height });
    groupType.GetMethod("AddCustomSize").Invoke(group, new[] { size }); chosen = total;
}
var gameViewType = assembly.GetType("UnityEditor.GameView");
var window = UnityEditor.EditorWindow.GetWindow(gameViewType);
gameViewType.GetProperty("selectedSizeIndex", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic).SetValue(window, chosen);
window.Repaint();
return "Game View set to " + width + "x" + height;

