foreach (var path in new[] { "Assets/ChronosChef/Art/AresAnimationSheet.png", "Assets/ChronosChef/Art/SteampunkKitchen.png", "Assets/ChronosChef/Art/ChronosKitchenClean.png", "Assets/ChronosChef/Art/StartMenuHero.png" })
{
    var importer = (UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(path);
    importer.textureType = UnityEditor.TextureImporterType.Default; importer.mipmapEnabled = false; importer.maxTextureSize = 2048; importer.alphaIsTransparency = path.Contains("Ares");
    importer.textureCompression = UnityEditor.TextureImporterCompression.Uncompressed; importer.wrapMode = UnityEngine.TextureWrapMode.Clamp;
    var android = importer.GetPlatformTextureSettings("Android"); android.overridden = true; android.maxTextureSize = 2048; android.format = UnityEditor.TextureImporterFormat.ASTC_6x6; importer.SetPlatformTextureSettings(android); importer.SaveAndReimport();
}
var view = new UnityEditor.SerializedObject(UnityEngine.Object.FindFirstObjectByType<ChronosChef.KitchenView>());
view.FindProperty("chefAnimationSheet").objectReferenceValue = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>("Assets/ChronosChef/Art/AresAnimationSheet.png");
view.FindProperty("steampunkArt").objectReferenceValue = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>("Assets/ChronosChef/Art/SteampunkKitchen.png");
view.FindProperty("environmentArt").objectReferenceValue = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>("Assets/ChronosChef/Art/ChronosKitchenClean.png");
view.FindProperty("homeArt").objectReferenceValue = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>("Assets/ChronosChef/Art/StartMenuHero.png");
view.ApplyModifiedPropertiesWithoutUndo();
UnityEditor.PlayerSettings.bundleVersion = "0.3.0-beta.1";
UnityEditor.PlayerSettings.Android.bundleVersionCode = 3;
UnityEditor.PlayerSettings.defaultInterfaceOrientation = UnityEditor.UIOrientation.AutoRotation;
UnityEditor.PlayerSettings.allowedAutorotateToLandscapeLeft = UnityEditor.PlayerSettings.allowedAutorotateToLandscapeRight = true;
UnityEditor.PlayerSettings.allowedAutorotateToPortrait = UnityEditor.PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
UnityEditor.PlayerSettings.Android.renderOutsideSafeArea = true;
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene()); UnityEditor.AssetDatabase.SaveAssets();
return "Beta assets assigned; landscape safe-area and texture settings saved";
