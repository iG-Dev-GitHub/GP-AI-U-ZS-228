using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class ScreenshotToolEditor : EditorWindow
{
    private string _savePath = "Screenshots/";
    private bool _isHorizontal = false;

    private ResolutionData[] _resolutions = new ResolutionData[]
    {
        new ResolutionData("Size 1284x2778", 1284, 2778),
        new ResolutionData("Size 1242x2208", 1242, 2208),
        new ResolutionData("Size 1290x2796", 1290, 2796)
    };

    private List<Texture2D> _screenshotTextures = new List<Texture2D>();
    private List<string> _screenshotFileNames = new List<string>();
    private Vector2 _scrollPosition;
    private bool _loadedScreenshots = false;

    [MenuItem("Tools/Screenshots Tool")]
    public static void ShowWindow()
    {
        GetWindow<ScreenshotToolEditor>("Screenshots Tool");
    }

    private void OnGUI()
    {
        GUILayout.Label("SCREENSHOT SETTINGS", EditorStyles.boldLabel);
        GUILayout.Label("Save Folder:");
        _savePath = EditorGUILayout.TextField(_savePath);

        _isHorizontal = EditorGUILayout.Toggle("Horizontal Mode", _isHorizontal);
        GUILayout.Label("Resolutions:");

        for (int i = 0; i < _resolutions.Length; i++)
        {
            GUILayout.BeginHorizontal();
            _resolutions[i].Name = EditorGUILayout.TextField(_resolutions[i].Name);
            _resolutions[i].Width = EditorGUILayout.IntField(_resolutions[i].Width);
            _resolutions[i].Height = EditorGUILayout.IntField(_resolutions[i].Height);
            GUILayout.EndHorizontal();
        }

        if (GUILayout.Button("Take Screenshot"))
        {
            PrepareAndTakeScreenshots();
            _loadedScreenshots = false;
        }

        GUILayout.Space(20);
        GUILayout.Label("SAVED SCREENSHOTS", EditorStyles.boldLabel);

        if (!_loadedScreenshots)
        {
            LoadAllScreenshots();
            _loadedScreenshots = true;
        }

        ShowAllScreenshots();
    }

    private void PrepareAndTakeScreenshots()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("Main Camera not found!");
            return;
        }

        Canvas[] canvases = GameObject.FindObjectsOfType<Canvas>();
        foreach (var canvas in canvases)
        {
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = mainCamera;
        }

        TakeScreenshots();
    }

    private void TakeScreenshots()
    {
        foreach (var res in _resolutions)
        {
            int width = _isHorizontal ? res.Height : res.Width;
            int height = _isHorizontal ? res.Width : res.Height;

            string resolutionFolder = Path.Combine(_savePath, $"{res.Height}x{res.Width}");

            if (!Directory.Exists(resolutionFolder))
                Directory.CreateDirectory(resolutionFolder);

            int fileCount = Directory.GetFiles(resolutionFolder, "*.jpg").Length;
            int newFileNumber = fileCount + 1;

            string filePath = Path.Combine(resolutionFolder, $"{newFileNumber} - {res.Width}x{res.Height} .jpg");
            CaptureScreenshot(width, height, filePath);
        }

        Debug.Log("Screenshots saved in " + _savePath);
    }


    private void CaptureScreenshot(int width, int height, string filePath)
    {
        Camera camera = Camera.main;
        if (camera == null)
        {
            Debug.LogError("Main Camera not found!");
            return;
        }

        RenderTexture rt = new RenderTexture(width, height, 24);
        camera.targetTexture = rt;
        Texture2D screenShot = new Texture2D(width, height, TextureFormat.RGB24, false);

        camera.Render();
        RenderTexture.active = rt;
        screenShot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        screenShot.Apply();

        byte[] bytes = screenShot.EncodeToJPG(100);
        File.WriteAllBytes(filePath, bytes);

        camera.targetTexture = null;
        RenderTexture.active = null;
        DestroyImmediate(rt);
        DestroyImmediate(screenShot);

        Debug.Log($"Screenshot saved: {filePath}");
    }

    private void LoadAllScreenshots()
    {
        _screenshotTextures.Clear();
        _screenshotFileNames.Clear();

        if (!Directory.Exists(_savePath)) return;

        string[] jpgFiles = Directory.GetFiles(_savePath, "*.jpg", SearchOption.AllDirectories);
        string[] pngFiles = Directory.GetFiles(_savePath, "*.png", SearchOption.AllDirectories);
        List<string> allFiles = new List<string>();
        allFiles.AddRange(jpgFiles);
        allFiles.AddRange(pngFiles);

        foreach (string path in allFiles)
        {
            byte[] fileData = File.ReadAllBytes(path);
            Texture2D texture = new Texture2D(2, 2);
            texture.LoadImage(fileData);
            texture.Apply();

            _screenshotTextures.Add(texture);
            _screenshotFileNames.Add(path);
        }
    }

    private void ShowAllScreenshots()
    {
        _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);
        GUILayout.BeginVertical();

        for (int i = 0; i < _screenshotTextures.Count; i++)
        {
            Texture2D screen = _screenshotTextures[i];
            if (screen != null)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(screen, GUILayout.Width(100), GUILayout.Height(100 * screen.height / screen.width));
                GUILayout.Label(Path.GetFileName(_screenshotFileNames[i]));

                if (GUILayout.Button("Open"))
                {
                    EditorUtility.OpenWithDefaultApp(_screenshotFileNames[i]);
                }

                if (GUILayout.Button("Delete"))
                {
                    DeleteScreenshot(i);
                    GUILayout.EndHorizontal();
                    GUILayout.EndVertical();
                    GUILayout.EndScrollView();
                    return;
                }

                if (GUILayout.Button("Rename"))
                {
                    RenameScreenshot(i);
                    GUILayout.EndHorizontal();
                    GUILayout.EndVertical();
                    GUILayout.EndScrollView();
                    return;
                }

                GUILayout.EndHorizontal();
            }
        }

        GUILayout.EndVertical();
        GUILayout.EndScrollView();
    }

    private void DeleteScreenshot(int index)
    {
        string path = _screenshotFileNames[index];
        if (File.Exists(path))
            File.Delete(path);

        _screenshotTextures.RemoveAt(index);
        _screenshotFileNames.RemoveAt(index);
        Repaint();
    }

    private void RenameScreenshot(int index)
    {
        string oldPath = _screenshotFileNames[index];
        string directory = Path.GetDirectoryName(oldPath);
        string newName = EditorUtility.SaveFilePanel("Rename Screenshot", directory, Path.GetFileNameWithoutExtension(oldPath), Path.GetExtension(oldPath).TrimStart('.'));

        if (!string.IsNullOrEmpty(newName))
        {
            File.Move(oldPath, newName);
            _screenshotFileNames[index] = newName;
            Repaint();
        }
    }

    private class ResolutionData
    {
        public string Name;
        public int Width;
        public int Height;

        public ResolutionData(string name, int width, int height)
        {
            Name = name;
            Width = width;
            Height = height;
        }
    }
}