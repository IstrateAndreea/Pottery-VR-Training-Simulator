using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ButtonAnimation : MonoBehaviour
{
    [Header("Assign your button GameObjects (the cubes) here, in the Inspector.")]
    public List<GameObject> allButtons;

    [Header("Animation Settings")]
    public float pressedScale = 0.85f;
    public float animationDuration = 0.1f;

    public void AnimateButtonPress(GameObject buttonObj)
    {
        if (buttonObj == null) return;
        StartCoroutine(AnimatePressRoutine(buttonObj.transform));
    }

    IEnumerator AnimatePressRoutine(Transform button)
    {
        Vector3 originalScale = button.localScale;
        button.localScale = new Vector3(
        originalScale.x,
        originalScale.y * pressedScale,
        originalScale.z
);
        yield return new WaitForSeconds(animationDuration);
        button.localScale = originalScale;
    }
}