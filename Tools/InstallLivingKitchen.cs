var names = new[] { "CosmicIngredients", "CustomerEmotions", "ChronoJourneyMap" };
foreach (var name in names)
{
    var path = "Assets/ChronosChef/Art/" + name + ".png";
    UnityEditor.AssetDatabase.ImportAsset(path);
    var importer = (UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(path);
    importer.textureType = UnityEditor.TextureImporterType.Default;
    importer.mipmapEnabled = false; importer.maxTextureSize = 2048;
    importer.alphaIsTransparency = name != "ChronoJourneyMap";
    importer.textureCompression = UnityEditor.TextureImporterCompression.Uncompressed;
    importer.wrapMode = UnityEngine.TextureWrapMode.Clamp;
    var android = importer.GetPlatformTextureSettings("Android");
    android.overridden = true; android.maxTextureSize = 2048;
    android.format = UnityEditor.TextureImporterFormat.ASTC_6x6;
    importer.SetPlatformTextureSettings(android); importer.SaveAndReimport();
}
var view = new UnityEditor.SerializedObject(UnityEngine.Object.FindFirstObjectByType<ChronosChef.KitchenView>());
view.FindProperty("ingredientAtlas").objectReferenceValue = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>("Assets/ChronosChef/Art/CosmicIngredients.png");
view.FindProperty("customerEmotionAtlas").objectReferenceValue = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>("Assets/ChronosChef/Art/CustomerEmotions.png");
view.FindProperty("journeyArt").objectReferenceValue = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>("Assets/ChronosChef/Art/ChronoJourneyMap.png");
view.ApplyModifiedPropertiesWithoutUndo();
UnityEditor.PlayerSettings.bundleVersion = "0.4.0-beta.1";
UnityEditor.PlayerSettings.Android.bundleVersionCode = 4;
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
UnityEditor.AssetDatabase.SaveAssets();
return "Living kitchen art assigned and scene saved";
