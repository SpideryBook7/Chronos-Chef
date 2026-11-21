var path = "Assets/ChronosChef/Art/ChronosChefIcon.png";
UnityEditor.AssetDatabase.ImportAsset(path);
var importer = (UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(path); importer.textureType = UnityEditor.TextureImporterType.Default; importer.mipmapEnabled = false; importer.maxTextureSize = 1024; importer.textureCompression = UnityEditor.TextureImporterCompression.Uncompressed; importer.SaveAndReimport();
var icon = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>(path);
UnityEditor.PlayerSettings.SetIcons(UnityEditor.Build.NamedBuildTarget.Unknown, new[] { icon }, UnityEditor.IconKind.Any);
var sizes = UnityEditor.PlayerSettings.GetIconSizes(UnityEditor.Build.NamedBuildTarget.Android, UnityEditor.IconKind.Any);
if (sizes.Length > 0) UnityEditor.PlayerSettings.SetIcons(UnityEditor.Build.NamedBuildTarget.Android, System.Linq.Enumerable.Repeat(icon, sizes.Length).ToArray(), UnityEditor.IconKind.Any);
UnityEditor.AssetDatabase.SaveAssets();
return "Chronos Chef icon assigned";
