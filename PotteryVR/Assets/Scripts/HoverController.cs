using UnityEngine;
using System.Collections.Generic;
using Leap;
using Leap.Unity;

public class HoverController : MonoBehaviour
{
    [Header("Button Setup")]
    public List<GameObject> interactiveButtons;  
    public List<GameObject> infoTexts;           

    [Header("Leap Motion")]
    public LeapProvider leapProvider;
    public float hoverDistance = 0.04f; 

    private int hoveredIndex = -1;

    void Start()
    {
        
        foreach (var info in infoTexts)
            if (info != null) info.SetActive(false);
    }

    void Update()
    {
        int hover = CheckHoverButtonWithPalms();
        UpdateHover(hover);
    }

   
    int CheckHoverButtonWithPalms()
    {
        if (leapProvider == null) return -1;
        Frame frame = leapProvider.CurrentFrame;

        for (int i = 0; i < interactiveButtons.Count; i++)
        {
            GameObject button = interactiveButtons[i];
            if (button == null) continue;

            foreach (var hand in frame.Hands)
            {
                Vector3 palmPos = hand.PalmPosition.ToVector3();
                if (Vector3.Distance(palmPos, button.transform.position) < hoverDistance)
                    return i;
            }
        }
        return -1;
    }

  
    void UpdateHover(int index)
    {
        if (hoveredIndex != index)
        {
           
            for (int i = 0; i < infoTexts.Count; i++)
                if (infoTexts[i] != null) infoTexts[i].SetActive(false);

           
            if (index != -1 && index < infoTexts.Count && infoTexts[index] != null)
                infoTexts[index].SetActive(true);

            hoveredIndex = index;
        }
    }
}