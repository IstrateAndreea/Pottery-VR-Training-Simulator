using UnityEngine;
using Leap;
using Leap.Unity;

public class ClayResetButtonController : MonoBehaviour
{
    [Header("Reset Button Settings")]
    public LeapProvider leapProvider; // Assign your LeapProvider in the inspector
    public GameObject resetButton;          // Assign the cube/button here in inspector
    public ClayManager clayManager;         // Assign your ClayManager here
    public Transform leftFingerTip;         // Assign in inspector
    public Transform rightFingerTip;        // Assign in inspector
    public float activationDistance = 0.02f;
    public float cooldownTime = 1f;

    private bool resetCooldown = false;

    void Update()
    {
        if (resetCooldown || clayManager == null || resetButton == null) return;

        // Check both finger tips for proximity
        if ((leftFingerTip && Vector3.Distance(leftFingerTip.position, resetButton.transform.position) < activationDistance) ||
            (rightFingerTip && Vector3.Distance(rightFingerTip.position, resetButton.transform.position) < activationDistance))
        {
          
            resetCooldown = true;
            Invoke(nameof(ResetResetCooldown), cooldownTime);
        }
    }

    void ResetResetCooldown()
    {
        resetCooldown = false;
    }
}