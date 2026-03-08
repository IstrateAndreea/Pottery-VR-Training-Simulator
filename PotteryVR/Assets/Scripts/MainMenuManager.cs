using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Leap;
using Leap.Unity;

public class MainMenuManager : MonoBehaviour
{
    [Header("Menu Panels")]
    public GameObject mainMenuPanel;
    public GameObject instructionPanel;

    [Header("Leap Motion")]
    public LeapProvider leapProvider;
    public Transform leftFingerTip;
    public Transform rightFingerTip;
    public float pinchThreshold = 0.8f;

    [Header("UI Buttons")]
    public Button startButton;
    public Button instructionsButton;
    public Button exitButton;
    public Button goBackButton;
    public Button resetButton;

    [Header("Audio")]
    public AudioClip menuButtonClickSound;
    private AudioSource audioSource;

    [Header("Interaction Settings")]
    [Tooltip("How close a finger must be to a button to interact")]
    public float buttonActivationDistance = 0.03f;

    
    private ColorBlock startColorBlock;
    private ColorBlock instructionsColorBlock;
    private ColorBlock exitColorBlock;
    private ColorBlock goBackColorBlock;
    private ColorBlock resetColorBlock;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    void Start()
    {
        ShowMainMenu();
        AssignButtonListeners();

        
        startColorBlock = startButton.colors;
        instructionsColorBlock = instructionsButton.colors;
        exitColorBlock = exitButton.colors;
        goBackColorBlock = goBackButton.colors;
        resetColorBlock = resetButton.colors;
    }

    void AssignButtonListeners()
    {
        startButton.onClick.AddListener(OnStartGame);
        instructionsButton.onClick.AddListener(OnShowInstructions);
        exitButton.onClick.AddListener(OnExitGame);
        goBackButton.onClick.AddListener(OnGoBack);
        resetButton.onClick.AddListener(ResetScene);
    }

    void PlayMenuButtonSound()
    {
        if (menuButtonClickSound != null && audioSource != null)
            audioSource.PlayOneShot(menuButtonClickSound);
    }

    void Update()
    {
        
        ResetButtonColors();

        if (mainMenuPanel.activeSelf)
        {
            HandleLeapInteraction(startButton, startColorBlock);
            HandleLeapInteraction(instructionsButton, instructionsColorBlock);
            HandleLeapInteraction(exitButton, exitColorBlock);
            HandleLeapInteraction(resetButton, resetColorBlock); 
        }
        else if (instructionPanel.activeSelf)
        {
            HandleLeapInteraction(goBackButton, goBackColorBlock);
        }
    }

    void HandleLeapInteraction(Button button, ColorBlock originalColors)
    {
        bool isHover = IsFingerNearButton(button, leftFingerTip) || IsFingerNearButton(button, rightFingerTip);
        bool isPinching = IsPinchingButton(button, leftFingerTip) || IsPinchingButton(button, rightFingerTip);

        var cb = originalColors;
        if (isPinching)
        {
            cb.normalColor = originalColors.pressedColor;
            button.colors = cb;
            button.onClick.Invoke();
            PlayMenuButtonSound();
        }
        else if (isHover)
        {
            cb.normalColor = originalColors.highlightedColor;
            button.colors = cb;
        }
        else
        {
            cb.normalColor = originalColors.normalColor;
            button.colors = cb;
        }
    }

    void ResetButtonColors()
    {
        startButton.colors = startColorBlock;
        instructionsButton.colors = instructionsColorBlock;
        exitButton.colors = exitColorBlock;
        goBackButton.colors = goBackColorBlock;
        resetButton.colors = resetColorBlock;
    }

    bool IsFingerNearButton(Button button, Transform fingerTip)
    {
        if (fingerTip == null) return false;
        Vector3 btnPos = button.transform.position;
        return Vector3.Distance(fingerTip.position, btnPos) < buttonActivationDistance;
    }

    bool IsPinchingButton(Button button, Transform fingerTip)
    {
        if (fingerTip == null || leapProvider == null) return false;

        Vector3 btnPos = button.transform.position;
        if (Vector3.Distance(fingerTip.position, btnPos) < buttonActivationDistance)
        {
            var frame = leapProvider.CurrentFrame;
            foreach (var hand in frame.Hands)
            {
                if ((fingerTip.position - hand.GetPinchPosition()).magnitude < buttonActivationDistance && hand.PinchStrength > pinchThreshold)
                    return true;
            }
        }
        return false;
    }

    
    void OnStartGame()
    {
        mainMenuPanel.SetActive(false);
    }

    void OnShowInstructions()
    {
        mainMenuPanel.SetActive(false);
        instructionPanel.SetActive(true);
    }

    void OnGoBack()
    {
        instructionPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }

    void OnExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void ResetScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void ShowMainMenu()
    {
        mainMenuPanel.SetActive(true);
        instructionPanel.SetActive(false);
    }
}