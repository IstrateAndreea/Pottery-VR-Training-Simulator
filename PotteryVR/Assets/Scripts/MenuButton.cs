using UnityEngine;

public class MenuButton : MonoBehaviour
{
    public Transform leftFingerTip;
    public Transform rightFingerTip;
    public float activationDistance = 0.02f; 
    public MainMenuManager menuManager;

    [Header("Button Sound")]
    public AudioClip buttonClickSound; 
    private AudioSource audioSource;

    private bool onCooldown = false;
    public float cooldownTime = 1f;

    private ButtonAnimation animManager;

    void Awake()
    {
        animManager = FindFirstObjectByType<ButtonAnimation>();
        audioSource = GetComponent<AudioSource>();
    }

    
    void Update()
    {
        if (!onCooldown && (IsTouched(leftFingerTip) || IsTouched(rightFingerTip)))
        {
            if (animManager != null)
                animManager.AnimateButtonPress(gameObject);

            PlayButtonSound();

            menuManager.ShowMainMenu();
            onCooldown = true;
            Invoke(nameof(ResetCooldown), cooldownTime);
        }
    }

    bool IsTouched(Transform fingerTip)
    {
        if (fingerTip == null) return false;
        return Vector3.Distance(fingerTip.position, transform.position) < activationDistance;
    }

    void ResetCooldown() => onCooldown = false;

    void PlayButtonSound()
    {
        if (buttonClickSound != null && audioSource != null)
            audioSource.PlayOneShot(buttonClickSound);
    }
}