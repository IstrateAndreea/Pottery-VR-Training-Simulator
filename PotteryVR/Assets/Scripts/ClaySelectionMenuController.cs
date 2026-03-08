using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Leap;
using Leap.Unity;

public class ClaySelectionMenuController : MonoBehaviour
{
    [Header("Menu UI")]
    public GameObject menuCanvas;
    public List<Button> clayButtons;
    public List<GameObject> clayInfoTexts;

    [Header("Clay Manager")]
    public ClayManager clayManager;

    [Header("Leap Motion")]
    public LeapProvider leapProvider;
    public Transform leftFingerTip;
    public Transform rightFingerTip;
    public float activationDistance = 0.025f;
    public float hoverDistance = 0.04f;
    public float pinchThreshold = 0.85f;

    [Header("Audio")]
    public AudioClip buttonPressSound;
    private AudioSource audioSource;

    [Header("Clay Button Sounds")]
    public AudioClip clayButtonClickSound;

    private bool menuActive = false;
    private int hoveredIndex = -1;

    private ButtonAnimation animManager;

    
    private List<ColorBlock> originalColorBlocks;

    void Awake()
    {
        animManager = FindFirstObjectByType<ButtonAnimation>();
        audioSource = GetComponent<AudioSource>();
    }

    void Start()
    {
        if (menuCanvas != null)
            menuCanvas.SetActive(false);

        foreach (var info in clayInfoTexts)
            info.SetActive(false);

       
        originalColorBlocks = new List<ColorBlock>();
        foreach (var btn in clayButtons)
            originalColorBlocks.Add(btn.colors);
    }

    void Update()
    {
       
        ResetButtonColors();

        
        if (!menuActive && (CheckFingerNearButton(leftFingerTip) || CheckFingerNearButton(rightFingerTip)))
        {
            OpenMenu();
            animManager?.AnimateButtonPress(gameObject);
            if (buttonPressSound != null) audioSource?.PlayOneShot(buttonPressSound);
        }

        if (menuActive)
        {
            int hover = CheckHoverButtonWithHands();
            UpdateHover(hover);

            for (int i = 0; i < clayButtons.Count; i++)
            {
                HandleLeapInteraction(clayButtons[i], originalColorBlocks[i], i == hoveredIndex);
            }

            
            if (hoveredIndex != -1 && (IsPinching(leftFingerTip) || IsPinching(rightFingerTip)))
            {
                SelectClay(hoveredIndex);
            }
        }
    }

    void HandleLeapInteraction(Button button, ColorBlock originalColors, bool isHovered)
    {
        bool isPinching = IsPinchingButton(button, leftFingerTip) || IsPinchingButton(button, rightFingerTip);

        var cb = originalColors;
        if (isPinching && isHovered)
        {
            cb.normalColor = originalColors.pressedColor; 
            button.colors = cb;
           
        }
        else if (isHovered)
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
        for (int i = 0; i < clayButtons.Count; i++)
        {
            clayButtons[i].colors = originalColorBlocks[i];
        }
    }

    bool CheckFingerNearButton(Transform tip)
    {
        if (!tip) return false;
        return Vector3.Distance(tip.position, transform.position) < activationDistance;
    }

    int CheckHoverButtonWithHands()
    {
        if (leapProvider == null) return -1;
        var frame = leapProvider.CurrentFrame;
        for (int i = 0; i < clayButtons.Count; i++)
        {
            foreach (var hand in frame.Hands)
            {
                Vector3 palmPos = hand.PalmPosition.ToVector3();
                if (Vector3.Distance(palmPos, clayButtons[i].transform.position) < hoverDistance)
                    return i;
            }
        }
        return -1;
    }

    void UpdateHover(int index)
    {
        if (hoveredIndex != index)
        {
            foreach (var info in clayInfoTexts)
                info.SetActive(false);

            if (index != -1 && index < clayInfoTexts.Count)
                clayInfoTexts[index].SetActive(true);

            hoveredIndex = index;
        }
    }

    bool IsPinching(Transform tip)
    {
        if (!tip || leapProvider == null) return false;
        Frame frame = leapProvider.CurrentFrame;
        foreach (var hand in frame.Hands)
        {
            if ((tip.position - hand.GetPinchPosition()).magnitude < 0.03f && hand.PinchStrength > pinchThreshold)
                return true;
        }
        return false;
    }

    bool IsPinchingButton(Button button, Transform fingerTip)
    {
        if (fingerTip == null || leapProvider == null) return false;
        Vector3 btnPos = button.transform.position;
        if (Vector3.Distance(fingerTip.position, btnPos) < 0.03f)
        {
            var frame = leapProvider.CurrentFrame;
            foreach (var hand in frame.Hands)
            {
                if ((fingerTip.position - hand.GetPinchPosition()).magnitude < 0.03f && hand.PinchStrength > pinchThreshold)
                    return true;
            }
        }
        return false;
    }

    void SelectClay(int index)
    {
        if (clayButtonClickSound != null && audioSource != null)
            audioSource.PlayOneShot(clayButtonClickSound);
        if (clayManager) clayManager.SelectClay(index);
        CloseMenu();
    }

    void OpenMenu()
    {
        if (menuCanvas != null)
            menuCanvas.SetActive(true);

        menuActive = true;
        hoveredIndex = -1;
    }

    void CloseMenu()
    {
        if (menuCanvas != null)
            menuCanvas.SetActive(false);

        foreach (var info in clayInfoTexts)
            info.SetActive(false);

        menuActive = false;
        hoveredIndex = -1;
    }
}