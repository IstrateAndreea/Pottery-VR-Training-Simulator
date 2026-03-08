using UnityEngine;
using Leap.Unity;
using Leap;

public class LeapBrushPainter : MonoBehaviour
{
    [Header("Leap Motion References")]
    public LeapProvider leapProvider; 

    public ClayManager clayManager; 

    [Header("Pottery Mesh Settings")]
    public MeshFilter[] potteryMeshFilters; 

    [Header("Brush Tip")]
    public Transform brushTip; 

    [Header("Painting Settings")]
    public float paintRadius = 0.02f; 
    public Color brushColor = Color.blue; 

    [Header("Grab Settings")]
    public float pinchDistance = 0.03f; 
    public float grabActivationDistance = 0.05f; 

    private bool isHeld = false;
    private Hand grabbedHand = null;

    void Update()
    {
        if (leapProvider == null || potteryMeshFilters == null || brushTip == null)
            return;

        Frame frame = leapProvider.CurrentFrame;

        if (!isHeld)
        {
            foreach (Hand hand in frame.Hands)
            {
                if (IsPinching(hand))
                {
                    Vector3 pinchMid = GetPinchMidpoint(hand);
                    if (Vector3.Distance(transform.position, pinchMid) < grabActivationDistance)
                    {
                        isHeld = true;
                        grabbedHand = hand;

                        if (clayManager != null)
                            clayManager.DisableAllDeformers();

                        break;
                    }
                }
            }
        }
        else
        {
            
            Vector3 pinchMid = GetPinchMidpoint(grabbedHand);
            Vector3 tipOffset = brushTip.position - transform.position;
            transform.position = pinchMid - tipOffset;

            
            transform.rotation = Quaternion.LookRotation(grabbedHand.Direction.ToVector3(), grabbedHand.PalmNormal.ToVector3());

            
            if (!IsPinching(grabbedHand))
            {
                isHeld = false;
                grabbedHand = null;
            }
        }

        if (isHeld)
        {
            PaintPottery();
        }
    }

    bool IsPinching(Hand hand)
    {
        float dist = Vector3.Distance(
            hand.Fingers[0].TipPosition.ToVector3(), 
            hand.Fingers[1].TipPosition.ToVector3()  
        );
        return dist < pinchDistance;
    }

    Vector3 GetPinchMidpoint(Hand hand)
    {
        Vector3 thumb = hand.Fingers[0].TipPosition.ToVector3();
        Vector3 index = hand.Fingers[1].TipPosition.ToVector3();
        return (thumb + index) * 0.5f;
    }

    void PaintPottery()
    {
        foreach (var meshFilter in potteryMeshFilters)
        {
            if (meshFilter == null) continue;
            Mesh mesh = meshFilter.mesh;
            Vector3[] vertices = mesh.vertices;
            Color[] colors = mesh.colors;
            if (colors == null || colors.Length != vertices.Length)
            {
                colors = new Color[vertices.Length];
                for (int i = 0; i < colors.Length; i++)
                    colors[i] = Color.white;
            }

           
            Vector3 tipPosition = brushTip.position;

            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 worldVertex = meshFilter.transform.TransformPoint(vertices[i]);
                if (Vector3.Distance(worldVertex, tipPosition) < paintRadius)
                {
                    colors[i] = brushColor;
                }
            }

            mesh.colors = colors;
        }
    }
}