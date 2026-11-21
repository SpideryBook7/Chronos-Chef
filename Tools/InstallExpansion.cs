var path = "Assets/ChronosChef/Art/TemporalCast.png";
var importer = (UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(path);
importer.alphaIsTransparency = true; importer.mipmapEnabled = false; importer.maxTextureSize = 4096;
importer.textureCompression = UnityEditor.TextureImporterCompression.Uncompressed; importer.wrapMode = UnityEngine.TextureWrapMode.Clamp;
importer.SaveAndReimport();
var view = UnityEngine.Object.FindFirstObjectByType<ChronosChef.KitchenView>();
var serialized = new UnityEditor.SerializedObject(view);
serialized.FindProperty("castAtlas").objectReferenceValue = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>(path);
serialized.ApplyModifiedPropertiesWithoutUndo();
for (int i = 0; i < 3; i++)
{
    var rect = UnityEngine.GameObject.Find("ServeCustomer" + i).GetComponent<UnityEngine.RectTransform>();
    rect.anchoredPosition = new UnityEngine.Vector2(630 + i * 270, 730); rect.sizeDelta = new UnityEngine.Vector2(250, 222);
}
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
UnityEditor.AssetDatabase.SaveAssets();
return "Illustrations assigned; complete customer hit targets saved";
