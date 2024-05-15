using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.Events;

public class Menu : MonoBehaviour
{
    #region Fields

    // nicely formated fields to serialize
    [Header("Audio")]
    [SerializeField] AudioMixer audioMixer;
    [Header("Labels")]
    [SerializeField] Sprite victoryLabel;
    [SerializeField] Sprite gameOverLabel;
    [Header("Dropdowns")]
    [SerializeField] TMP_Dropdown resolutionDropdown;
    [SerializeField] TMP_Dropdown qualityDropdown;
    [Header("Screen Effects")]
    [SerializeField] Image sceneTransitor;

    // input
    private IA input;
    private InputAction pause;
    private InputAction back;

    // audio
    private AudioSource audioSource;

    // menus
    private GameObject mainMenu;
    private GameObject pauseMenu;
    private GameObject resultMenu;
    private GameObject optionsMenu;
    private GameObject libraryMenu;

    // post-process
    private Vignette ppVignette;

    // diverse
    private Resolution[] resolutions;
    Utils utils;
    private bool isPaused;
    private bool mute;
    private bool characterSelected;
    private bool levelSelected;
    private TotalMuteEvent totalMuteEvent;

    #endregion

    #region Private Methods

    private void Awake()
    {
        // set references
        input = new IA();
        utils = FindObjectOfType<Utils>();
        totalMuteEvent = new TotalMuteEvent();
        utils.SetTotalMuteEventInvoker(this);
        ppVignette = FindObjectOfType<PostProcessVolume>().profile.GetSetting<Vignette>();
        audioSource = GetComponent<AudioSource>();

        // initialise menus
        MenuCanvas[] menus = GetComponentsInChildren<MenuCanvas>(true);
        foreach (MenuCanvas menu in menus)
            switch (menu.GetMenuType())
            {
                case MenuCanvas.MenuType.Main: mainMenu = menu.gameObject; break;
                case MenuCanvas.MenuType.Pause: pauseMenu = menu.gameObject; break;
                case MenuCanvas.MenuType.Result: resultMenu = menu.gameObject; break;
                case MenuCanvas.MenuType.Options: optionsMenu = menu.gameObject; break;
                case MenuCanvas.MenuType.Library: libraryMenu = menu.gameObject; break;
            }
    }

    private void Start()
    {
        // if pause menu is not null that means that we are in an actual level
        if (pauseMenu != null)
        {
            // hide mouse coursor
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;

            // add self as a listener of multiple events
            utils.AddVictoryEventListner(Victory);
            utils.AddGameOverEventListner(GameOver);

            // remove vignette effect
            ppVignette.intensity.value = 0;

            // set and activate pause input
            pause = input.Menu.Pause;
            pause.started += Pause;
            pause.Enable();
        }
        // on the other hand if main menu is not null that means that we are in main menu
        else if (mainMenu != null) { ppVignette.intensity.value = 0.2f; }

        // set "back" input for options menu but don't activate it yet
        back = input.Menu.Back;
        back.started += CloseOptions;

        // fill resolution dropdown with avalible resolutions
        resolutions = Screen.resolutions;
        resolutionDropdown.ClearOptions();
        List<string> stringResolutions = new List<string>();
        int currentResolutionIndex = 0;
        for (int i = 0; i < resolutions.Length; i++)
        {
            stringResolutions.Add(resolutions[i].width + "x" + resolutions[i].height);
            if (resolutions[i].width == Screen.currentResolution.width &&
                resolutions[i].height == Screen.currentResolution.height) { currentResolutionIndex = i; }
        }
        resolutionDropdown.AddOptions(stringResolutions);

        // update resolution dropdown accordingly to current resolution
        resolutionDropdown.value = currentResolutionIndex;
        resolutionDropdown.RefreshShownValue();

        // set graphics dropdown quality
        qualityDropdown.value = QualitySettings.GetQualityLevel();

        // set volume slider value
        float volumeValue = 0;
        if (audioMixer.GetFloat("volume", out volumeValue)) { GetComponentInChildren<Slider>(true).value = volumeValue; }

        // set fullscreen tougle value
        GetComponentInChildren<Toggle>(true).isOn = Screen.fullScreen;

        // select first button in menu
        if (mainMenu != null) { mainMenu.GetComponentInChildren<Button>().Select(); }

        // perform scene opening
        StartCoroutine(OpenScene());
    }

    // Disable input
    private void OnDisable()
    {
        if (pause != null && pause.enabled) { pause.Disable(); }
        if (back.enabled) { back.Disable(); }
    }

    // Smoothly load scene
    private IEnumerator LoadLevel(string sceneName)
    {
        // obscure screen
        Color color = sceneTransitor.color;
        sceneTransitor.enabled = true;
        while (color.a < 1)
        {
            color.a += Time.unscaledDeltaTime;
            audioSource.volume -= Time.unscaledDeltaTime;
            sceneTransitor.color = color;
            yield return new WaitForEndOfFrame();
        }
        if (audioSource.volume > 0) { audioSource.volume = 0; }
        if (color.a != 1) { color.a = 1; }
        sceneTransitor.color = color;

        // load scene
        SceneManager.LoadScene(sceneName);
    }

    // Smoothly open scene
    private IEnumerator OpenScene()
    {
        // mute any actions while opening scenes 
        mute = true;

        // prepare for scene opening
        Time.timeScale = 0;
        sceneTransitor.enabled = true;
        Color color = sceneTransitor.color;
        color.a = 1;
        sceneTransitor.color = color;
        if (audioSource.volume > 0) { audioSource.volume = 0; }

        // smoothly illuminate scene
        while (color.a > 0)
        {
            color.a -= Time.unscaledDeltaTime;
            audioSource.volume += Time.unscaledDeltaTime;
            sceneTransitor.color = color;
            yield return new WaitForEndOfFrame();
        }

        // clamp values
        if (audioSource.volume != 1) { audioSource.volume = 1; }
        if (color.a != 0) { color.a = 0; }
        sceneTransitor.color = color;
        sceneTransitor.enabled = false;
        Time.timeScale = 1.0f;

        // allow any actions
        mute = false;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Sets if character or level is selected or not, when both are selected loads level automaticaly
    /// </summary>
    /// <param name="category"> Character or Level</param>
    /// <param name="isSelected"> Is selected or not </param>
    public void MakeChoice(Library.Category category, bool isSelected)
    {
        // set value
        if (category == Library.Category.Character) { characterSelected = isSelected; }
        else { levelSelected = isSelected; }

        // if both are selected load level
        if (characterSelected && levelSelected)
        {
            // prevent player from interrupting process
            totalMuteEvent.Invoke();
            back.Disable();
            Button[] buttons = GetComponentsInChildren<Button>();
            foreach (Button button in buttons) { button.interactable = false; }

            // load level
            StartCoroutine(LoadLevel("PilotLevel"));
        }
    }

    public void AddTotalMuteEventListener(UnityAction listener) { totalMuteEvent.AddListener(listener); }

    // Reload current scene
    public void Restart() { StartCoroutine(LoadLevel(SceneManager.GetActiveScene().name)); }

    // Load main menu scene
    public void MainMenu() { StartCoroutine(LoadLevel("MainMenu")); }

    // Set audio mixer volume
    public void SetVolume(float value) { audioMixer.SetFloat("volume", value); }

    // Set quality level
    public void SetQuality(int qualityIndex) { QualitySettings.SetQualityLevel(qualityIndex); }

    // Set fullscreen
    public void SetFullscreen(bool isFullscreen) { Screen.fullScreen = isFullscreen; }

    // Set screen resolution
    public void SetResolution(int resolutionIndex) 
    { 
        Screen.SetResolution(resolutions[resolutionIndex].width, resolutions[resolutionIndex].height, Screen.fullScreen); 
        if (mainMenu != null) { FindObjectOfType<Background>().ResetBackground(); }
    }

    // Quit application
    public void Quit() { Application.Quit(); }

    #endregion

    #region Pause

    // Pause game if it's not already paused, otherwise unpause it
    private void Pause(InputAction.CallbackContext callbackContext) 
    { 
        if (!isPaused && !mute) { StartCoroutine(SmoothPause());  } 
        else if (isPaused && !mute) { { StartCoroutine(SmoothResume()); } }
    }

    public void Resume() { if (isPaused && !mute) { StartCoroutine(SmoothResume()); } }

    // Smoothly pause game
    private IEnumerator SmoothPause() 
    {
        // prohibit any actions while pausing
        mute = true;
        isPaused = true;

        // prepare canvas, vignette and audio for pausing
        pauseMenu.SetActive(true);
        CanvasScaler scaler = pauseMenu.GetComponent<CanvasScaler>();
        pauseMenu.GetComponentInChildren<Button>().Select();
        if (audioSource.volume != 1) { audioSource.volume = 1; }
        if (scaler.scaleFactor != 0) { scaler.scaleFactor = 0; }
        if (ppVignette.intensity.value != 0) { ppVignette.intensity.value = 0; }

        // smoothly pause game
        while (scaler.scaleFactor < 1)
        {
            scaler.scaleFactor += Time.unscaledDeltaTime * 6;
            if (Time.timeScale - Time.unscaledDeltaTime * 6 > 0) { Time.timeScale -= Time.unscaledDeltaTime * 6; }
            if (audioSource.volume > 0.1f) { audioSource.volume -= Time.unscaledDeltaTime * 6; }
            if (ppVignette.intensity.value < 0.35f) ppVignette.intensity.value += Time.unscaledDeltaTime * 2.5f;
            yield return new WaitForEndOfFrame();
        }

        // clamp values
        if (audioSource.volume != 0.1f) { audioSource.volume = 0.1f; }
        if (scaler.scaleFactor != 1) { scaler.scaleFactor = 1; }
        if (ppVignette.intensity.value != 0.35f) { ppVignette.intensity.value = 0.35f; }
        if (Time.timeScale != 0.0f) { Time.timeScale = 0.0f; }

        // show mouse cursor
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // allow any actions
        mute = false;
    }

    // Smoothly resume game
    private IEnumerator SmoothResume()
    {
        // prohibit any actions while resuming
        mute = true;
        isPaused = false;

        // hide mouse coursor
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        // prepare canvas and vignette
        CanvasScaler scaler = pauseMenu.GetComponent<CanvasScaler>();
        if (ppVignette.intensity.value != 0.35f) { ppVignette.intensity.value = 0.35f; }

        // smoothly unpause
        while (scaler.scaleFactor > 0.01f)
        {
            scaler.scaleFactor -= Time.unscaledDeltaTime * 6;
            if (Time.timeScale < 1) Time.timeScale += Time.unscaledDeltaTime * 6;
            if (audioSource.volume < 1) { audioSource.volume += Time.unscaledDeltaTime * 6; }
            ppVignette.intensity.value -= Time.unscaledDeltaTime * 2.5f;
            yield return new WaitForEndOfFrame();
        }

        // clamp values
        if (audioSource.volume != 1) { audioSource.volume = 1; }
        if (scaler.scaleFactor != 0) { scaler.scaleFactor = 0; }
        if (ppVignette.intensity.value != 0) { ppVignette.intensity.value = 0; }
        if (Time.timeScale != 1.0f) { Time.timeScale = 1.0f; }
        pauseMenu.SetActive(false);

        // allow any actions
        mute = false;
    }

    #endregion

    #region Options

    // Open options
    public void Options() 
    {
        // switch controls
        if (pause != null && pause.enabled) { pause.Disable(); }
        back.started += CloseOptions;
        back.Enable();

        // swith menus
        if (mainMenu != null) { mainMenu.SetActive(false); }
        else if (pauseMenu != null) { pauseMenu.SetActive(false); }
        optionsMenu.SetActive(true);
        optionsMenu.GetComponentInChildren<TMP_Dropdown>().Select();
    }

    // Close options (input overload)
    private void CloseOptions(InputAction.CallbackContext callbackContext)
    {
        // switch controls
        if (pause != null && !pause.enabled) { pause.Enable(); }
        back.started -= CloseOptions;
        back.Disable();

        // switch menus
        optionsMenu.SetActive(false);
        if (mainMenu != null) { mainMenu.SetActive(true); mainMenu.GetComponentInChildren<Button>().Select(); }
        else if (pauseMenu != null) { pauseMenu.SetActive(true); pauseMenu.GetComponentInChildren<Button>().Select(); }

    }

    // Close options (button overload)
    public void CloseOptions()
    {
        // switch controls
        if (pause != null && !pause.enabled) { pause.Enable(); }
        back.started -= CloseOptions;
        back.Disable();

        // switch menus
        optionsMenu.SetActive(false);
        if (mainMenu != null) { mainMenu.SetActive(true); mainMenu.GetComponentInChildren<Button>().Select(); }
        else if (pauseMenu != null) { pauseMenu.SetActive(true); pauseMenu.GetComponentInChildren<Button>().Select(); }
    }

    #endregion

    #region Result

    // Anounce victory
    private void Victory() { StartCoroutine(SmoothResult(true)); }

    // Anounce game over 
    private void GameOver() { StartCoroutine(SmoothResult(false)); }

    // Smoothly anounce result
    private IEnumerator SmoothResult(bool isVictorious)
    {
        // mute any actions while anouncing
        mute = true;

        // prepare canvas, label ect. for pausing
        CanvasScaler scaler = resultMenu.GetComponent<CanvasScaler>();
        resultMenu.GetComponentInChildren<Image>().sprite = isVictorious ? victoryLabel : gameOverLabel;
        resultMenu.SetActive(true);
        resultMenu.GetComponentInChildren<Button>().Select();
        if (audioSource.volume != 1) { audioSource.volume = 1; }
        if (scaler.scaleFactor != 0) { scaler.scaleFactor = 0; }
        if (ppVignette.intensity.value != 0) { ppVignette.intensity.value = 0; }

        // smoothly pause
        while (scaler.scaleFactor < 1)
        {
            scaler.scaleFactor += Time.deltaTime * 6;
            if (audioSource.volume > 0.1f) { audioSource.volume -= Time.deltaTime * 6; }
            if (ppVignette.intensity.value < 0.35f) ppVignette.intensity.value += Time.deltaTime * 2.5f;
            yield return new WaitForEndOfFrame();
        }

        // clamp values
        if (audioSource.volume != 0.1f) { audioSource.volume = 0.1f; }
        if (scaler.scaleFactor != 1) { scaler.scaleFactor = 1; }
        if (ppVignette.intensity.value != 0.35f) { ppVignette.intensity.value = 0.35f; }
        Time.timeScale = 0.0f;

        // show mouse cursor
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    #endregion

    #region Library
    
    // Open library
    public void GoToLibrary()
    {
        // switch menus
        mainMenu.SetActive(false);
        libraryMenu.SetActive(true);

        // activate back input
        back.started += CloseLibrary;
        back.Enable();
    }

    // Close library (input overload)
    private void CloseLibrary(InputAction.CallbackContext callbackContext)
    {
        // swith menus
        mainMenu.SetActive(true);
        libraryMenu.SetActive(false);
        mainMenu.GetComponentInChildren<Button>().Select();

        // deactivate back input
        back.started -= CloseLibrary;
        back.Disable();
    }

    // Close library (button overload)
    public void CloseLibrary()
    {
        // swith menus
        mainMenu.SetActive(true);
        libraryMenu.SetActive(false);
        mainMenu.GetComponentInChildren<Button>().Select();

        // deactivate back input
        back.started -= CloseLibrary;
        back.Disable();
    }

    #endregion

}
