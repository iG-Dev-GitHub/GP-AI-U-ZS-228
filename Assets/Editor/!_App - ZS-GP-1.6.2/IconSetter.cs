using System.IO;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEngine;

public class AdaptiveIconSetter : Editor
{
    // [MenuItem("Assets/Change Icons", true)]
    // private static bool ValidateSetIcons()
    // {
    // Проверяем, выбрана ли папка
    //return Selection.activeObject && AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(Selection.activeObject));
    //  }

    //[MenuItem("Assets/Change Icons")]
    public static void SetAllIcons()
    {
        SetAllIcons(null);
    }
    public static void DeleteAllIcons()
    {
        DeleteMainAppIcon();
        RemoveDefaultIcons();
        DeleteAllPlayerIcons();
        AssetDatabase.Refresh();
    }
    public static void SetAllIcons( string? path = null)
    {
        string selectedFolderPath = path;
        if (path == null)
            selectedFolderPath = AssetDatabase.GetAssetPath(Selection.activeObject);

        // Проверяем наличие нужных файлов в выбранной папке
        bool isAndroidFolder = false;
        bool isIOSFolder = false;   
        try
        {
            isAndroidFolder = Directory.GetFiles(selectedFolderPath, "play_store_512.png", SearchOption.AllDirectories).Length > 0;
            isIOSFolder = Directory.GetFiles(selectedFolderPath, "1024.png", SearchOption.AllDirectories).Length > 0;
        }
        catch { 
            Debug.LogWarning("Пожалуйста, выберите папку!");
        }
        if (isAndroidFolder)
        {
            SetMainIcon(Path.Combine(selectedFolderPath, "play_store_512.png"));
            SetIcons(selectedFolderPath);
            SetRoundIcons(selectedFolderPath);
            SetAdaptiveIcons(selectedFolderPath);
        }
        else
        {
            Debug.LogWarning("Пожалуйста, выберите папку, содержащую иконки для Android ");
        }
        if (isIOSFolder)
        {
            SetMainIcon(Path.Combine(selectedFolderPath, "1024.png"));
            SetIOSIcons(selectedFolderPath);
        }
        else
        {
            Debug.LogWarning("Пожалуйста, выберите папку, содержащую иконки для iOS.");
        }
    }
    public static void DeleteMainAppIcon()
    {
        // Получаем иконки для платформы Standalone (Desktop)
        Texture2D[] icons = PlayerSettings.GetIconsForTargetGroup(BuildTargetGroup.Unknown);

        if (icons == null || icons.Length == 0 || icons[0] == null)
        {
            Debug.LogWarning("Главная иконка приложения не установлена или уже удалена.");
            return;
        }

        // Удаление каждой иконки, если она существует
        foreach (Texture2D icon in icons)
        {
            if (icon == null) continue;

            string path = AssetDatabase.GetAssetPath(icon);
            if (!string.IsNullOrEmpty(path))
            {
                bool success = AssetDatabase.DeleteAsset(path);
                if (success)
                {
                    Debug.Log($"Иконка удалена: {path}");
                }
                else
                {
                    Debug.LogError($"Не удалось удалить иконку: {path}");
                }
            }
        }

        // Сброс иконок для всех целевых платформ
        PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Standalone, new Texture2D[0]);
        PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Android, new Texture2D[0]);
        PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.iOS, new Texture2D[0]);

        // Обновляем базу данных ассетов
        AssetDatabase.Refresh();
        Debug.Log("Главная иконка приложения полностью удалена из проекта.");
    }
    private static void RemoveDefaultIcons()
    {
        // Получаем иконки для всех платформ
        var buildTargetGroups = new[]
        {
        BuildTargetGroup.iOS,
        BuildTargetGroup.Android,
        BuildTargetGroup.Standalone,
        BuildTargetGroup.WebGL
        // Добавьте другие платформы по мере необходимости
    };

        foreach (var group in buildTargetGroups)
        {
            // Получаем текущие иконки для данной платформы
            Texture2D[] icons = PlayerSettings.GetIconsForTargetGroup(group);

            // Удаляем иконки
            foreach (var icon in icons)
            {
                if (icon != null)
                {
                    string assetPath = AssetDatabase.GetAssetPath(icon);
                    if (!string.IsNullOrEmpty(assetPath))
                    {
                        AssetDatabase.DeleteAsset(assetPath);
                        Debug.Log($"Removed icon for {group}: {assetPath}");
                    }
                }
            }

            // Устанавливаем пустые иконки для платформы
            PlayerSettings.SetIconsForTargetGroup(group, new Texture2D[0]);
        }

        AssetDatabase.Refresh();
    }
    private static void DeleteAllPlayerIcons()
    {
        // Получаем все целевые группы платформ
        BuildTargetGroup[] targetGroups = (BuildTargetGroup[])System.Enum.GetValues(typeof(BuildTargetGroup));

        foreach (BuildTargetGroup targetGroup in targetGroups)
        {
            // Пропускаем устаревшие или не поддерживаемые платформы
            if (targetGroup == BuildTargetGroup.Unknown || !IsValidBuildTargetGroup(targetGroup))
                continue;

            // Удаляем иконки для текущей целевой группы
            DeleteIconsForTargetGroup(targetGroup);

            // Дополнительно удаляем адаптивные иконки для Android
            if (targetGroup == BuildTargetGroup.Android)
            {
                DeleteAndroidAdaptiveIcons();
            }
        }

        // Обновляем проект после удаления
        AssetDatabase.Refresh();
    }

    private static void DeleteIconsForTargetGroup(BuildTargetGroup targetGroup)
    {
        // Получаем все иконки для текущей целевой группы
        Texture2D[] icons = PlayerSettings.GetIconsForTargetGroup(targetGroup);

        foreach (Texture2D icon in icons)
        {
            if (icon == null) continue;

            // Находим путь к иконке в проекте
            string path = AssetDatabase.GetAssetPath(icon);

            // Удаляем файл иконки
            if (!string.IsNullOrEmpty(path))
            {
                AssetDatabase.DeleteAsset(path);
                Debug.Log($"Deleted icon: {path}");
            }
        }
    }

    private static void DeleteAndroidAdaptiveIcons()
    {
        // Получаем адаптивные иконки для Android как PlatformIcon[]
        PlatformIcon[] adaptiveIcons = PlayerSettings.GetPlatformIcons(BuildTargetGroup.Android, AndroidPlatformIconKind.Adaptive);

        foreach (var platformIcon in adaptiveIcons)
        {
            Texture2D[] icon = platformIcon.GetTextures();
            for (int i = 0; i < icon.Length; i++)
            {
                if (icon[i] == null) continue;

                string path = AssetDatabase.GetAssetPath(icon[i]);
                if (!string.IsNullOrEmpty(path))
                {
                    AssetDatabase.DeleteAsset(path);
                    Debug.Log($"Deleted Android adaptive icon: {path}");
                }
            }
        }
    }
    private static bool IsValidBuildTargetGroup(BuildTargetGroup group)
    {
        // Проверяем, поддерживается ли группа в текущей версии Unity
        try
        {
            NamedBuildTarget.FromBuildTargetGroup(group);
            return true;
        }
        catch
        {
            return false;
        }
    }
    private static void SetRoundIcons(string folderPath)
    {
       

        // Устанавливаем иконки для Android по уровням разрешения
        string resPath = Path.Combine(folderPath, "res");
        if (!Directory.Exists(resPath))
        {
            Debug.LogError($"Папка res не найдена по пути: {resPath}. Убедитесь, что структура папок правильная.");
            return;
        }

        // Массивы для иконок разных разрешений (в порядке от xxxhdpi к mdpi)
        var mipmapFolders = new string[]
        {
            "mipmap-xxxhdpi", "mipmap-xxhdpi", "mipmap-xhdpi", "mipmap-hdpi", "mipmap-mdpi", "mipmap-mdpi"
        };

        Texture2D[] resolutionIcons = new Texture2D[mipmapFolders.Length];

        for (int i = 0; i < mipmapFolders.Length; i++)
        {
            string mipmapPath = Path.Combine(resPath, mipmapFolders[i]);
            if (Directory.Exists(mipmapPath))
            {
                // Загружаем файл ic_launcher.png, если он существует в папке
                string iconPath = Path.Combine(mipmapPath, "ic_launcher.png");
                resolutionIcons[i] = AssetDatabase.LoadAssetAtPath<Texture2D>(iconPath);

                if (resolutionIcons[i] == null)
                {
                    Debug.LogWarning($"Файл ic_launcher.png не найден в {mipmapPath}");
                }
            }
            else
            {
                Debug.LogWarning($"Папка {mipmapFolders[i]} не найдена в {resPath}.");
            }
        }
        NamedBuildTarget platform = NamedBuildTarget.Android;
        PlatformIcon[] icons = PlayerSettings.GetPlatformIcons(platform, AndroidPlatformIconKind.Round);

        for (int j = 0; j < 6; j++)
        {
            if (resolutionIcons[j] != null && resolutionIcons[j] != null)
            {
                icons[j].SetTextures(resolutionIcons[j]);
            }
            else
            {
                Debug.LogWarning($"Пропускаем установку для {mipmapFolders[j]} из-за отсутствия текстур.");
            }
        }
        PlayerSettings.SetPlatformIcons(platform, AndroidPlatformIconKind.Round, icons);
    }
    private static void SetAdaptiveIcons(string folderPath)
    {
        string resPath = Path.Combine(folderPath, "res");

        if (!Directory.Exists(resPath))
        {
            Debug.LogError($"Папка res не найдена по пути: {resPath}. Убедитесь, что структура папок правильная.");
            return;
        }

        // Папки с mipmap разрешениями
        string[] mipmapFolders = { "mipmap-xxxhdpi", "mipmap-xxhdpi", "mipmap-xhdpi", "mipmap-hdpi", "mipmap-mdpi", "mipmap-mdpi" };
        Texture2D[][] textures = new Texture2D[mipmapFolders.Length][];

        for (int i = 0; i < mipmapFolders.Length; i++)
        {
            string mipmapPath = Path.Combine(resPath, mipmapFolders[i]);

            if (Directory.Exists(mipmapPath))
            {
                // Загружаем ic_launcher_background.png и ic_launcher_foreground.png
                string backgroundIconPath = Path.Combine(mipmapPath, "ic_launcher_background.png");
                string foregroundIconPath = Path.Combine(mipmapPath, "ic_launcher_foreground.png");

                Texture2D backgroundIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(backgroundIconPath);
                Texture2D foregroundIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(foregroundIconPath);

                if (backgroundIcon == null || foregroundIcon == null)
                {
                    Debug.LogWarning($"Не удалось найти иконки в папке {mipmapPath}. Пропускаем.");
                    textures[i] = new Texture2D[] { null, null };
                    continue;
                }

                textures[i] = new Texture2D[] { backgroundIcon, foregroundIcon };
            }
            else
            {
                Debug.LogWarning($"Папка {mipmapFolders[i]} не найдена. Пропускаем.");
                textures[i] = new Texture2D[] { null, null };
            }
        }

        // Установка иконок для Android
        NamedBuildTarget platform = NamedBuildTarget.Android;
        PlatformIcon[] icons = PlayerSettings.GetPlatformIcons(platform, AndroidPlatformIconKind.Adaptive);

        for (int j = 0; j < 6; j++)
        {
            if (textures[j][0] != null && textures[j][1] != null)
            {
                icons[j].SetTextures(textures[j]);
            }
            else
            {
                Debug.LogWarning($"Пропускаем установку для {mipmapFolders[j]} из-за отсутствия текстур.");
            }
        }

        PlayerSettings.SetPlatformIcons(platform, AndroidPlatformIconKind.Adaptive, icons);
        Debug.Log("Адаптивные иконки успешно установлены.");
    }
    private static void SetMainIcon(string defaultIconPath)
    {
        Texture2D defaultIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(defaultIconPath);

        if (defaultIcon == null)
        {
            Debug.LogError($"Иконка по умолчанию не найдена по пути: {defaultIconPath}. Убедитесь, что файл существует и указан верный путь.");
            return;
        }

        // Применяем Default Icon с изменённым порядком
        PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Unknown, new[] { defaultIcon });
        Debug.Log("Иконка применена как Default Icon.");
    }
    private static void SetIcons(string folderPath)
    {
        // Устанавливаем иконку по умолчанию (расположение в обратном порядке)
      

        // Устанавливаем иконки для Android по уровням разрешения
        string resPath = Path.Combine(folderPath, "res");
        if (!Directory.Exists(resPath))
        {
            Debug.LogError($"Папка res не найдена по пути: {resPath}. Убедитесь, что структура папок правильная.");
            return;
        }

        // Массивы для иконок разных разрешений (в порядке от xxxhdpi к mdpi)
        var mipmapFolders = new string[]
        {
            "mipmap-xxxhdpi", "mipmap-xxhdpi", "mipmap-xhdpi", "mipmap-hdpi", "mipmap-mdpi", "mipmap-mdpi"
        };

        Texture2D[] resolutionIcons = new Texture2D[mipmapFolders.Length];

        for (int i = 0; i < mipmapFolders.Length; i++)
        {
            string mipmapPath = Path.Combine(resPath, mipmapFolders[i]);
            if (Directory.Exists(mipmapPath))
            {
                // Загружаем файл ic_launcher.png, если он существует в папке
                string iconPath = Path.Combine(mipmapPath, "ic_launcher.png");
                resolutionIcons[i] = AssetDatabase.LoadAssetAtPath<Texture2D>(iconPath);

                if (resolutionIcons[i] == null)
                {
                    Debug.LogWarning($"Файл ic_launcher.png не найден в {mipmapPath}");
                }
            }
            else
            {
                Debug.LogWarning($"Папка {mipmapFolders[i]} не найдена в {resPath}.");
            }
        }

        // Устанавливаем иконки для всех разрешений на Android
        PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Android, resolutionIcons);

        Debug.Log("Стандартные иконки для Android установлены для разных разрешений.");
    }


    private static void SetIOSIcons(string folderPath)
    {
        // Путь к папке с иконками
        string iconsFolderPath = folderPath;

        // Сопоставление размеров для iPhone иконок
        var iconSizes = new (int size, int scale, int resolution)[]
        {
            (60, 3, 180),(60, 2, 120),(0,0,0),(0,0,0),(0,0,0),
            (40, 3, 120), (40, 2, 80),(0,0,0),(0,0,0),
            (29, 1, 29), (29, 2, 58), (29, 3, 87),(0,0,0),(0,0,0),
            (20, 1, 60), (20, 2, 40),(0,0,0),(0,0,0),
            (1024, 1, 1024)
        };

        // Создаем массив для иконок
        Texture2D[] icons = new Texture2D[iconSizes.Length];

        for (int i = 0; i < iconSizes.Length; i++)
        {

            var (size, scale, resolution) = iconSizes[i];
            if (size == 0)
            {
                icons[i] = null;
                continue;
            }
            // Путь к иконке
            string iconPath = Path.Combine(iconsFolderPath, $"{resolution}.png");
            Texture2D iconTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(iconPath);

            if (iconTexture != null)
            {
                icons[i] = iconTexture;
                Debug.Log($"Loaded iOS icon for iPhone {size}pt@{scale}x from {iconPath}");
            }
            else
            {
                Debug.LogWarning($"Icon file not found at {iconPath}");
            }
        }

        // Устанавливаем иконки для iOS
        PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.iOS, icons);

        AssetDatabase.Refresh();
    }
}
