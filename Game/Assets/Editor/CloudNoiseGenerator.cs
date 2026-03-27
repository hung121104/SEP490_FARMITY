using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor utility that generates a tileable cellular (Worley / cnoise) texture
/// for the cloud shadow system.  Access via  Tools ▸ Generate Cloud Noise Texture.
///
/// Cellular noise places random feature points in a grid and colours each pixel
/// by its distance to the nearest point.  Inverting the result gives smooth
/// bright blobs on a dark background — ideal for cloud-shaped shadows.
/// </summary>
public class CloudNoiseGenerator : EditorWindow
{
    private int resolution = 256;
    private int gridSize   = 6;    // number of cells per axis
    private int octaves    = 3;
    private string savePath = "Assets/Shaders/cnoise.png";

    [MenuItem("Tools/Generate Cloud Noise Texture")]
    static void ShowWindow()
    {
        GetWindow<CloudNoiseGenerator>("Cloud Noise Generator");
    }

    void OnGUI()
    {
        GUILayout.Label("Tileable Cellular (Worley) Noise", EditorStyles.boldLabel);
        resolution = EditorGUILayout.IntField("Resolution", resolution);
        gridSize   = EditorGUILayout.IntSlider("Grid Size (cells)", gridSize, 2, 16);
        octaves    = EditorGUILayout.IntSlider("Octaves", octaves, 1, 5);
        savePath   = EditorGUILayout.TextField("Save Path", savePath);

        if (GUILayout.Button("Generate"))
            GenerateAndSave();
    }

    private void GenerateAndSave()
    {
        var tex = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);

        for (int y = 0; y < resolution; y++)
        for (int x = 0; x < resolution; x++)
        {
            float value = CloudShadowNoise.TileableCellular(x, y, resolution, gridSize, octaves);
            tex.SetPixel(x, y, new Color(value, value, value, 1f));
        }

        tex.Apply();
        byte[] png = tex.EncodeToPNG();
        DestroyImmediate(tex);

        System.IO.File.WriteAllBytes(savePath, png);
        AssetDatabase.Refresh();

        var importer = AssetImporter.GetAtPath(savePath) as TextureImporter;
        if (importer != null)
        {
            importer.wrapMode    = TextureWrapMode.Repeat;
            importer.filterMode  = FilterMode.Bilinear;
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = false;
            importer.SaveAndReimport();
        }

        Debug.Log($"[cnoise] Saved {resolution}×{resolution} tileable cellular noise to {savePath}");
    }
}
