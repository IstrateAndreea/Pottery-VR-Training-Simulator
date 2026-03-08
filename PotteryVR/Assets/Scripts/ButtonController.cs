using UnityEngine;
using Leap;
using Leap.Unity;

public class ButtonController : MonoBehaviour
{
    
    [Header("Button References")]
    public GameObject startButton;
    public GameObject stopButton;
    public WheelSpin wheelSpin;
    public LeapProvider provider;

    [Header("Finger Tip Transforms")]
    public Transform leftFingerTip;
    public Transform rightFingerTip;

    [Header("Settings")]
    public float activationDistance = 0.02f;
    public float cooldownTime = 1f;

    private bool startCooldown;
    private bool stopCooldown;

    [Header("Audio")]
    public AudioClip buttonPressSound;
    private AudioSource audioSource;

    private ButtonAnimation animManager;

    void Awake()
    {
        animManager = FindObjectOfType<ButtonAnimation>();
        audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        if (provider == null || wheelSpin == null) return;

        
        TryPressButton(leftFingerTip);
        TryPressButton(rightFingerTip);
    }

    void TryPressButton(Transform fingerTip)
    {
        if (fingerTip == null) return;

        Vector3 pos = fingerTip.position;
        
        if (!startCooldown && Vector3.Distance(pos, startButton.transform.position) < activationDistance)
        {
            wheelSpin.StartSpinning();
            startCooldown = true;
            Invoke(nameof(ResetStartCooldown), cooldownTime);

           
            animManager?.AnimateButtonPress(startButton);
            audioSource?.PlayOneShot(buttonPressSound);
        }

        
        if (!stopCooldown && Vector3.Distance(pos, stopButton.transform.position) < activationDistance)
        {
            wheelSpin.StopSpinning();
            stopCooldown = true;
            Invoke(nameof(ResetStopCooldown), cooldownTime);

            animManager?.AnimateButtonPress(stopButton);
            audioSource?.PlayOneShot(buttonPressSound);
        }
    }

    void ResetStartCooldown() => startCooldown = false;
    void ResetStopCooldown() => stopCooldown = false;
}
