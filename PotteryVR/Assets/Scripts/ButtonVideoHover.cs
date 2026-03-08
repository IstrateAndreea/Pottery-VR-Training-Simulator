using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;
using Leap;
using Leap.Unity;
using System.Collections.Generic;

public class ButtonVideoHoverManager : MonoBehaviour
{
    [Header("Assign your three buttons here")]
    public List<GameObject> videoButtons;          

    [Header("Assign RawImages that display videos")]
    public List<RawImage> videoDisplays;           

    [Header("Assign VideoPlayers for each video")]
    public List<VideoPlayer> videoPlayers;         

    [Header("Leap Motion")]
    public LeapProvider leapProvider;
    public float hoverDistance = 0.04f;            

    [Header("Audio")]
    public AudioClip buttonPressSound;
    private AudioSource audioSource;

    private int hoveredIndex = -1;
    private ButtonAnimation animManager;

    void Awake()
    {
        animManager = FindFirstObjectByType<ButtonAnimation>();
        audioSource = GetComponent<AudioSource>();
    }

    void Start()
    {
        
        for (int i = 0; i < videoDisplays.Count; i++)
        {
            if (videoDisplays[i] != null) videoDisplays[i].gameObject.SetActive(false);
            if (videoPlayers[i] != null) videoPlayers[i].Stop();
        }
    }

    void Update()
    {
        int hover = CheckHoverWithHand();
        UpdateVideoDisplay(hover);
    }

    int CheckHoverWithHand()
    {
        if (leapProvider == null) return -1;
        var frame = leapProvider.CurrentFrame;
        for (int i = 0; i < videoButtons.Count; i++)
        {
            GameObject btn = videoButtons[i];
            if (btn == null) continue;

            foreach (var hand in frame.Hands)
            {
                Vector3 palmPos = hand.PalmPosition.ToVector3();
                if (Vector3.Distance(palmPos, btn.transform.position) < hoverDistance)
                    return i;
            }
        }
        return -1;
    }

    void UpdateVideoDisplay(int index)
    {
        if (hoveredIndex != index)
        {
            
            for (int i = 0; i < videoDisplays.Count; i++)
            {
                if (videoDisplays[i] != null) videoDisplays[i].gameObject.SetActive(false);
                if (videoPlayers[i] != null) videoPlayers[i].Stop();
            }

            
            if (index != -1 && index < videoDisplays.Count)
            {
                if (videoDisplays[index] != null) videoDisplays[index].gameObject.SetActive(true);
                if (videoPlayers[index] != null)
                {
                    videoPlayers[index].Play();
                }

                
                if (animManager != null && index >= 0 && index < videoButtons.Count)
                    animManager.AnimateButtonPress(videoButtons[index]);
                if (buttonPressSound != null) audioSource?.PlayOneShot(buttonPressSound);
            }

            hoveredIndex = index;
        }
    }
}