foreach (var name in new[] { "Rounded", "Circle" })
{
    var path = "Assets/ChronosChef/Art/" + name + ".png";
    var images = UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Image>(UnityEngine.FindObjectsInactive.Include, UnityEngine.FindObjectsSortMode.None).Where(i => i.sprite != null && UnityEditor.AssetDatabase.GetAssetPath(i.sprite) == path).ToArray();
    var importer = (UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(path);
    importer.spriteImportMode = UnityEditor.SpriteImportMode.Single;
    importer.textureCompression = UnityEditor.TextureImporterCompression.Uncompressed;
    importer.SaveAndReimport();
    var sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Sprite>(path);
    foreach (var image in images) { image.sprite = sprite; UnityEditor.EditorUtility.SetDirty(image); }
    var view = new UnityEditor.SerializedObject(UnityEngine.Object.FindFirstObjectByType<ChronosChef.KitchenView>());
    view.FindProperty(name == "Rounded" ? "rounded" : "circle").objectReferenceValue = sprite;
    view.ApplyModifiedPropertiesWithoutUndo();
}
var captions = new[] { "GROG / HAMBRE JURÁSICA", "BIP-08 / ERROR: HAMBRE", "ZORG / CRÍTICO GALÁCTICO" };
var cards = UnityEngine.GameObject.Find("KitchenStage").transform.Cast<UnityEngine.Transform>().Where(t => t.name == "OrderCard").ToArray();
for (int i = 0; i < cards.Length; i++)
{
    foreach (var label in cards[i].GetComponentsInChildren<TMPro.TMP_Text>())
    {
        if (label.text.StartsWith("CYBER")) { label.text = "CYBER-DODO\nBURGER"; label.fontSize = 18; }
        else if (label.text.StartsWith("CORTA")) label.text = captions[i];
        UnityEditor.EditorUtility.SetDirty(label);
    }
}
foreach (var label in UnityEngine.Object.FindObjectsByType<TMPro.TMP_Text>(UnityEngine.FindObjectsSortMode.None))
{
    if (label.transform.parent.name == "KitchenStage" && (label.text.StartsWith("GROG /") || label.text.StartsWith("BIP-08 /") || label.text.StartsWith("ZORG /"))) label.gameObject.SetActive(false);
}
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
UnityEditor.AssetDatabase.SaveAssets();
return "Sprite borders and order cards corrected and saved";
