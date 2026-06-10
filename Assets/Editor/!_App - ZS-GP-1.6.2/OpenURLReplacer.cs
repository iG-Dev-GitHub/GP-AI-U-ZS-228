using System.IO;
using UnityEditor;
using UnityEngine;

public class OpenURLReplacer : EditorWindow
{
    private string newURL = "https://example.com"; // Новая ссылка по умолчанию

    [MenuItem("Tools/OpenURL Replacer")]
    public static void ShowWindow()
    {
        GetWindow<OpenURLReplacer>("OpenURL Replacer");
    }

    private void OnGUI()
    {
        GUILayout.Label("Replace all Application.OpenURL() calls", EditorStyles.boldLabel);

        // Поле для ввода новой ссылки
        newURL = EditorGUILayout.TextField("New URL:", newURL);

        if (GUILayout.Button("Replace URLs"))
        {
            ReplaceOpenURLCalls();
        }
    }

    private void ReplaceOpenURLCalls()
    {
        string[] allFiles = Directory.GetFiles(Application.dataPath, "*.cs", SearchOption.AllDirectories);

        foreach (string filePath in allFiles)
        {
            // Исключаем файл OpenURLReplacer.cs из обработки
            if (Path.GetFileName(filePath) == "OpenURLReplacer.cs")
            {
                continue;
            }

            string fileContent = File.ReadAllText(filePath);

            // Регулярное выражение для поиска вызовов Application.OpenURL()
            string pattern = @"Application\.OpenURL\s*\(([^)]*)\)";
            string updatedContent = System.Text.RegularExpressions.Regex.Replace(
                fileContent,
                pattern,
                $"Application.OpenURL(\"{newURL}\")"
            );

            // Если содержимое файла изменилось, сохраняем его
            if (fileContent != updatedContent)
            {
                File.WriteAllText(filePath, updatedContent);
                Debug.Log($"Updated: {filePath}");
            }
        }

        AssetDatabase.Refresh(); // Обновляем базу данных Unity
        Debug.Log("All Application.OpenURL() calls have been updated.");
    }
}