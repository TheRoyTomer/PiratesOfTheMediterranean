using System;
using System.IO;
using UnityEditor;
using UnityEngine;

internal static class MinimapCaptureExporter
{
    private const string OutputPath = "Assets/Minimap/MinimapBackground.png";

    [MenuItem("Tools/Minimap/Save Capture as PNG")]
    private static void SaveCaptureAsPng()
    {
        RenderTexture previousActive = RenderTexture.active;
        Texture2D pixels = null;

        try
        {
            RenderTexture capture = null;
            foreach (string guid in AssetDatabase.FindAssets("MinimapCapture t:RenderTexture", new[] { "Assets" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                RenderTexture candidate = AssetDatabase.LoadAssetAtPath<RenderTexture>(path);
                if (candidate == null || candidate.name != "MinimapCapture")
                    continue;

                if (capture != null)
                    throw new InvalidOperationException("Multiple Render Texture assets named MinimapCapture were found. Use a unique asset name.");

                capture = candidate;
            }

            if (capture == null)
                throw new InvalidOperationException("Render Texture asset MinimapCapture was not found under Assets.");

            if (!capture.IsCreated())
                throw new InvalidOperationException("MinimapCapture has no active GPU texture. Enter Play mode and let the minimap camera render before saving.");

            try
            {
                RenderTexture.active = capture;
                pixels = new Texture2D(capture.width, capture.height, TextureFormat.RGBA32, false)
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
                pixels.ReadPixels(new Rect(0, 0, capture.width, capture.height), 0, 0, false);

                // PNG stores display-ready values; encode linear RGB as sRGB once.
                // An sRGB RenderTexture already contains encoded values. Preserve alpha.
                if (QualitySettings.activeColorSpace == ColorSpace.Linear && !capture.sRGB)
                {
                    Color[] colors = pixels.GetPixels();
                    for (int i = 0; i < colors.Length; i++)
                    {
                        colors[i].r = Mathf.LinearToGammaSpace(colors[i].r);
                        colors[i].g = Mathf.LinearToGammaSpace(colors[i].g);
                        colors[i].b = Mathf.LinearToGammaSpace(colors[i].b);
                    }
                    pixels.SetPixels(colors);
                }

                pixels.Apply(false, false);

                byte[] png = pixels.EncodeToPNG();
                if (png == null || png.Length == 0)
                    throw new InvalidOperationException("PNG encoding returned no image data.");

                string folder = Path.Combine(Application.dataPath, "Minimap");
                Directory.CreateDirectory(folder);
                File.WriteAllBytes(Path.Combine(folder, "MinimapBackground.png"), png);
            }
            finally
            {
                RenderTexture.active = previousActive;
                if (pixels != null)
                    UnityEngine.Object.DestroyImmediate(pixels);
            }

            AssetDatabase.Refresh();
            Debug.Log("Minimap capture saved to " + OutputPath);
            EditorUtility.DisplayDialog("Minimap Capture", "PNG saved successfully:\n" + OutputPath, "OK");
        }
        catch (Exception exception)
        {
            Debug.LogError("Failed to save minimap capture: " + exception);
            EditorUtility.DisplayDialog("Minimap Capture - Error", "Failed to save PNG:\n" + exception.Message, "OK");
        }
    }
}
