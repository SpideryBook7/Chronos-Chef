using System.IO;
using ChronosChef;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

public static class ChronosSceneBuilder
{
    [MenuItem("Chronos Chef/Create Prototype Scene")]
    public static void CreateScene()
    {
        if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Exit Play Mode first.");
        const string path = "Assets/ChronosChef/Scenes/ChronosKitchen.unity";
        if (File.Exists(path)) throw new System.InvalidOperationException("Scene already exists. Open it instead of overwriting your edits.");
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        if (!font) throw new System.InvalidOperationException("Import TMP essentials before creating the scene.");
        Directory.CreateDirectory("Assets/ChronosChef/Art"); Directory.CreateDirectory("Assets/ChronosChef/Scenes");
        var rounded = MakeShape("Rounded", false); var circle = MakeShape("Circle", true);
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var camera = new GameObject("Main Camera", typeof(Camera)); camera.tag = "MainCamera";
        camera.GetComponent<Camera>().backgroundColor = new Color(.025f, .04f, .08f); camera.GetComponent<Camera>().clearFlags = CameraClearFlags.SolidColor; camera.GetComponent<Camera>().orthographic = true; camera.transform.position = new Vector3(0, 0, -10);
        var game = new GameObject("ChronosChefGame").AddComponent<KitchenGame>();
        var view = new GameObject("KitchenCanvas", typeof(RectTransform)).AddComponent<KitchenView>(); view.Build(font, rounded, circle); game.Configure(view);
        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
        PlayerSettings.allowedAutorotateToLandscapeLeft = PlayerSettings.allowedAutorotateToLandscapeRight = true;
        PlayerSettings.allowedAutorotateToPortrait = PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.defaultScreenWidth = 1600; PlayerSettings.defaultScreenHeight = 900;
        EditorSceneManager.SaveScene(scene, path);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(path, true) };
        AssetDatabase.SaveAssets(); Selection.activeGameObject = game.gameObject;
        Debug.Log("Chronos Chef: prototype scene created and saved.");
    }
    static Sprite MakeShape(string name, bool circle)
    {
        string path = "Assets/ChronosChef/Art/" + name + ".png";
        const int size = 64;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
        {
            float dx = Mathf.Abs(x - 31.5f), dy = Mathf.Abs(y - 31.5f);
            float distance = circle ? Mathf.Sqrt(dx * dx + dy * dy) - 30.5f : new Vector2(Mathf.Max(0, dx - 21), Mathf.Max(0, dy - 21)).magnitude - 10;
            texture.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(.5f - distance)));
        }
        texture.Apply(); File.WriteAllBytes(path, texture.EncodeToPNG()); Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path); importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single; importer.spritePixelsPerUnit = 100; importer.spriteBorder = circle ? Vector4.zero : new Vector4(13, 13, 13, 13); importer.textureCompression = TextureImporterCompression.Uncompressed; importer.mipmapEnabled = false; importer.alphaIsTransparency = true; importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
