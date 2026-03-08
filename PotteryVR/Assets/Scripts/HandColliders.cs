using UnityEngine;

public class HandColliders : MonoBehaviour
{
    public float palmRadius = 0.03f;
    public float wristRadius = 0.025f;
    public float fingerRadius = 0.012f;
    public float boneLength = 0.028f;

    void Start()
    {
        GameObject ghostHands = GameObject.Find("Ghost Hands (URP) Variant");
        if (ghostHands == null)
        {
            Debug.LogWarning("GhostHands not found!");
            return;
        }

        AddCollidersRecursive(ghostHands.transform);
    }

    void AddCollidersRecursive(Transform current)
    {
        
        if (current.name.EndsWith("_Palm"))
        {
            if (current.GetComponent<SphereCollider>() == null)
            {
                var palmCol = current.gameObject.AddComponent<SphereCollider>();
                palmCol.radius = palmRadius;
                Debug.Log("Added SphereCollider to PALM: " + current.name);
            }
        }
       
        else if (current.name.EndsWith("_Wrist"))
        {
            if (current.GetComponent<SphereCollider>() == null)
            {
                var wristCol = current.gameObject.AddComponent<SphereCollider>();
                wristCol.radius = wristRadius;
                Debug.Log("Added SphereCollider to WRIST: " + current.name);
            }
        }
       
        else if (IsFingerSegment(current.name))
        {
            if (current.GetComponent<CapsuleCollider>() == null)
            {
                var fingerCol = current.gameObject.AddComponent<CapsuleCollider>();
                fingerCol.radius = fingerRadius;
                fingerCol.height = boneLength;
                fingerCol.direction = 2; // Z axis
                Debug.Log("Added CapsuleCollider to FINGER: " + current.name);
            }
        }

        
        foreach (Transform child in current)
        {
            AddCollidersRecursive(child);
        }
    }

    bool IsFingerSegment(string name)
    {
       
        return (
            (name.Contains("_meta") || name.Contains("_a") || name.Contains("_b") || name.Contains("_c") || name.Contains("_end"))
            && !name.EndsWith("_Palm")
            && !name.EndsWith("_Wrist")
        );
    }
}