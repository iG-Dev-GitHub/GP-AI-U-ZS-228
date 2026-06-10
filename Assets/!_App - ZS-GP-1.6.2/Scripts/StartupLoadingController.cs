using System;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StartupLoadingController : MonoBehaviour
{
#if UNITY_EDITOR
    public string Version = "ZS-GP-1.6.2";
#endif

    [SerializeField] private bool _rotateToLanscapeMode = false;

    private static StartupLoadingController _instance;

    // ! - DO NOT CHANGE - !
    private async void Awake()
    {
        Application.targetFrameRate = 60;

        if (_instance != null)
        {
            Destroy(this.gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(this.gameObject);

#if UNITY_EDITOR
        await LoadEditorApp();
#endif

        //await GetComponent<NavigationManager>().Initialize();
        Startup();
    }


    // ! - CAN BE CHANGED - !
    private void Startup()
    {
        this.gameObject.SetActive(false);

        SetupAppOrientation();

        // ! - ANY ADDITIONAL LOGIC ADD HERE...

        // TODO 1. Enable background music here
    }

    private void SetupAppOrientation()
    {
        Screen.orientation = ScreenOrientation.AutoRotation;
        if (!_rotateToLanscapeMode)
        {
            SetupPortraitOrientation();
        }
        else
        {
            SetupLandscapeOrientation();
        }
    }

    private void SetupPortraitOrientation()
    {
#if UNITY_ANDROID
        Screen.orientation = ScreenOrientation.Portrait;
        return;
#endif

        Screen.autorotateToPortrait = true;

        Screen.autorotateToLandscapeLeft
            = Screen.autorotateToLandscapeRight
                = Screen.autorotateToPortraitUpsideDown = false;
    }

    private void SetupLandscapeOrientation()
    {
#if UNITY_ANDROID
        Screen.orientation = ScreenOrientation.LandscapeLeft;
        return;
#endif

        Screen.autorotateToLandscapeLeft = true;

        Screen.autorotateToPortrait
            = Screen.autorotateToPortraitUpsideDown
                = Screen.autorotateToLandscapeRight = false;
    }


#if UNITY_EDITOR
    private async Task LoadEditorApp()
    {
        var loadingCanvas = Resources.Load("LoadingCanvas" + " - " + Version) as GameObject;
        if (loadingCanvas != null)
        {
            Debug.LogError("Resources - Loading Canvas prefab must be Removed from project after placed in Scene");
        }

        var appManager = Resources.Load("AppManager" + " - " + Version) as GameObject;
        if (appManager != null)
        {
            Debug.LogError("Resources - App Manager prefab must be Removed from project after placed in Scene");
        }

        var transforms = this.gameObject.GetComponentsInChildren<Transform>(true); ;
        CheckGraphicsSetup(transforms);

        try
        {
            var loadingView = transforms.First(tr => tr.gameObject.name == "LoadingView").gameObject;

            var loadingText = transforms.First(tr => tr.gameObject.name == "LoadingText").GetComponent<TextMeshProUGUI>();

            await ShowLoadingView(loadingView, loadingText);
        }
        catch (Exception e)
        {
            Debug.LogError("Loading Canvas - invalid structure, please update with the last App plugin version for this project Type.");
            throw new UnityException();
        }
    }


    private void CheckGraphicsSetup(Transform[] transforms)
    {
        // ! - Nav Background
        try
        {
            var navBackground = transforms.First(tr => tr.gameObject.name == "NavBackground").GetComponentInChildren<Image>();
            if (navBackground.sprite != null)
            {
                Debug.LogError("Loading Canvas - Nav Background - must be Black!");
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Loading Canvas - Nav Background - invalid structure!");
        }
    }


    private async Task ShowLoadingView(GameObject loadingView, TextMeshProUGUI loadingText)
    {
        loadingView.SetActive(true);

        var loadingPercent = 0;

        while (loadingPercent < 100)
        {
            loadingPercent += 5;
            loadingText.text = $"{loadingPercent.ToString()}%";

            await Task.Delay(150);
        }

        loadingView.SetActive(false);
    }

#endif
}


