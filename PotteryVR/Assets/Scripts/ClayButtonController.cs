using UnityEngine;
using Leap;
using Leap.Unity;

public class ClayButtonController : MonoBehaviour
{
    [System.Serializable]
    public class ClayModeButton
    {
        public string modeName; 
        public GameObject buttonObject;
        [HideInInspector] public bool isInCooldown = false;
    }

    [Header("Button Setup")]
    public ClayModeButton[] buttons;
    public ClayManager clayManager;

    [Header("Leap Setup")]
    public LeapProvider provider;
    public Transform leftFingerTip;
    public Transform rightFingerTip;

    [Header("Settings")]
    public float activationDistance = 0.02f;
    public float cooldownTime = 1f;

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
        if (provider == null || clayManager == null) return;

        TryFinger(leftFingerTip);
        TryFinger(rightFingerTip);
    }

    void TryFinger(Transform finger)
    {
        if (finger == null) return;

        Vector3 pos = finger.position;

        foreach (var btn in buttons)
        {
            if (btn.isInCooldown || btn.buttonObject == null) continue;

            float distance = Vector3.Distance(pos, btn.buttonObject.transform.position);
            if (distance < activationDistance)
            {
                clayManager.SetMode(btn.modeName);
                btn.isInCooldown = true;
                StartCoroutine(ResetCooldown(btn));

                animManager?.AnimateButtonPress(btn.buttonObject);
                if (buttonPressSound != null) audioSource?.PlayOneShot(buttonPressSound);

                break; 
            }
        }
    }

    System.Collections.IEnumerator ResetCooldown(ClayModeButton btn)
    {
        yield return new WaitForSeconds(cooldownTime);
        btn.isInCooldown = false;
    }
}