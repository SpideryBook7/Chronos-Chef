var stage = UnityEngine.Object.FindFirstObjectByType<ChronosChef.KitchenView>().transform.Find("SafeArea/LandscapeFrame/KitchenStage");
UnityEngine.Canvas.ForceUpdateCanvases();
for (int i = 0; i < 3; i++)
{
    var window = stage.Find("CustomerWindow" + i).GetComponent<UnityEngine.RectTransform>();
    if (!window.GetComponent<UnityEngine.UI.RectMask2D>()) throw new System.Exception("Missing stationary counter mask");
    if (window.anchoredPosition.y - window.rect.height / 2 != 650) throw new System.Exception("Mask no longer aligns with bar edge");
    foreach (UnityEngine.Transform visitor in window)
    {
        if (visitor.GetComponent<UnityEngine.UI.RectMask2D>()) throw new System.Exception("Mask incorrectly follows visitor movement");
        if (!visitor.gameObject.activeInHierarchy) continue;
        var picture = visitor.Find("IllustratedCharacter").GetComponent<UnityEngine.UI.Image>();
        if (!picture.canvasRenderer.hasRectClipping) throw new System.Exception("Visitor is not clipped by the bar mask");
    }
}
var result = "PASS: three fixed mask windows at the bar edge; active visitor images are clipped; arrival/levitation cannot move the mask.";
System.IO.File.WriteAllText("Tests/counter-mask-result.txt", result);
return result;
