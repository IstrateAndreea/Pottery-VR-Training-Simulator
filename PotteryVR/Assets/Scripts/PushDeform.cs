using UnityEngine;
using System.Collections.Generic;
using Leap;
using Leap.Unity;

[RequireComponent(typeof(MeshFilter))]
public class PushDeform : MonoBehaviour
{
    [Header("Leap Motion")]
    public LeapProvider provider;

    [Header("Clay Deformation Parameters")]
    public float pushStrength = 0.15f;
    public float ringRadiusTolerance = 0.07f;
    public float verticalTolerance = 0.13f;
    public float centerRadiusThreshold = 0.025f;
    public float minAllowedRadius = 0.01f;

    [Header("Mesh Smoothing")]
    public int smoothingIterations = 2;
    public float smoothingFactor = 0.5f;

    [Header("Limits")]
    public float maxPushHeight = 1.5f;

    [Header("Boundaries")]
    public float tableHeight = 0f; 

    
    public float maxAllowedDrop = 0.05f; 

    private Mesh deformMesh;
    private Vector3[] originalVertices;
    private Vector3[] displacedVertices;
    private float originalMeshVolume;

    void Start()
    {
        deformMesh = Instantiate(GetComponent<MeshFilter>().mesh);
        GetComponent<MeshFilter>().mesh = deformMesh;
        originalVertices = deformMesh.vertices;
        displacedVertices = new Vector3[originalVertices.Length];
        originalVertices.CopyTo(displacedVertices, 0);

        originalMeshVolume = ComputeMeshVolume(deformMesh, originalVertices);
    }

    public void SyncWithCurrentMesh()
    {
        Mesh mesh = GetComponent<MeshFilter>().mesh;
        Vector3[] current = mesh.vertices;
        displacedVertices = new Vector3[current.Length];
        current.CopyTo(displacedVertices, 0);
        deformMesh = mesh;
        originalMeshVolume = ComputeMeshVolume(mesh, displacedVertices);
    }

    float GetBaseY()
    {
        float minY = float.MaxValue;
        foreach (var v in displacedVertices)
            if (v.y < minY) minY = v.y;
        return minY;
    }

    void Update()
    {
        if (provider == null) return;

        Frame frame = provider.CurrentFrame;

        foreach (Hand hand in frame.Hands)
        {
            List<Vector3> contactPoints = new List<Vector3>();

            contactPoints.Add(transform.InverseTransformPoint(hand.PalmPosition.ToVector3()));
            foreach (Finger finger in hand.Fingers)
                contactPoints.Add(transform.InverseTransformPoint(finger.TipPosition.ToVector3()));

            foreach (var point in contactPoints)
            {
                float targetRadius = Mathf.Sqrt(point.x * point.x + point.z * point.z);
                float targetY = point.y;

                float baseY = GetBaseY();
                float safeMargin = 0.1f;

                List<int> bandIndices = new List<int>();
                float minR = float.MaxValue, maxR = float.MinValue;
                float minY = float.MaxValue, maxY = float.MinValue;

                for (int i = 0; i < displacedVertices.Length; i++)
                {
                    Vector3 v = displacedVertices[i];
                    float vRadius = Mathf.Sqrt(v.x * v.x + v.z * v.z);

                    if (v.y <= baseY + safeMargin)
                        continue;

                    float effectiveVerticalTolerance = verticalTolerance * 2f;

                    if (targetRadius < centerRadiusThreshold)
                    {
                        if (vRadius < ringRadiusTolerance && Mathf.Abs(v.y - targetY) < effectiveVerticalTolerance)
                        {
                            bandIndices.Add(i);
                            if (vRadius < minR) minR = vRadius;
                            if (vRadius > maxR) maxR = vRadius;
                            if (v.y < minY) minY = v.y;
                            if (v.y > maxY) maxY = v.y;
                        }
                    }
                    else
                    {
                        if (Mathf.Abs(vRadius - targetRadius) < ringRadiusTolerance && Mathf.Abs(v.y - targetY) < effectiveVerticalTolerance)
                        {
                            bandIndices.Add(i);
                            if (vRadius < minR) minR = vRadius;
                            if (vRadius > maxR) maxR = vRadius;
                            if (v.y < minY) minY = v.y;
                            if (v.y > maxY) maxY = v.y;
                        }
                    }
                }

                if (bandIndices.Count == 0)
                    continue;

                foreach (int idx in bandIndices)
                {
                    Vector3 v = displacedVertices[idx];
                    float vRadius = Mathf.Sqrt(v.x * v.x + v.z * v.z);
                    float distance = Mathf.Sqrt((vRadius - targetRadius) * (vRadius - targetRadius) + (v.y - targetY) * (v.y - targetY));
                    float pushAmount = pushStrength * (1.0f - distance / (ringRadiusTolerance + verticalTolerance));

                    float minAllowedHeight = baseY + safeMargin;

                    
                    float desiredNewY = displacedVertices[idx].y - Mathf.Abs(pushAmount);

                    
                    desiredNewY = Mathf.Max(desiredNewY, minAllowedHeight);

                    
                    float maxDrop = Mathf.Min(maxAllowedDrop, displacedVertices[idx].y - minAllowedHeight);
                    float newY = Mathf.Max(desiredNewY, displacedVertices[idx].y - maxDrop);

                   
                    newY = Mathf.Max(newY, tableHeight);

                    
                    if (newY > baseY + safeMargin)
                    {
                        float bulgeAmount = 0.01f; 
                        displacedVertices[idx].x *= (1.0f + bulgeAmount);
                        displacedVertices[idx].z *= (1.0f + bulgeAmount);
                    }

                    
                    displacedVertices[idx].y = newY;
                }

               
                foreach (int idx in bandIndices)
                {
                    Vector3 v = displacedVertices[idx];
                    float flatLen = new Vector2(v.x, v.z).magnitude;
                    if (flatLen < minAllowedRadius)
                    {
                        float factor = minAllowedRadius / (flatLen + 1e-8f);
                        displacedVertices[idx].x *= factor;
                        displacedVertices[idx].z *= factor;
                    }
                }
            }
        }

        
        SmoothMesh(smoothingIterations, smoothingFactor);

        
        float currentVolume = ComputeMeshVolume(deformMesh, displacedVertices);
        float scale = Mathf.Pow(originalMeshVolume / (currentVolume + 1e-8f), 1f / 3f);

        for (int i = 0; i < displacedVertices.Length; i++)
        {
            displacedVertices[i] *= scale;
        }

        deformMesh.vertices = displacedVertices;
        deformMesh.RecalculateNormals();
    }

    void SmoothMesh(int iterations = 1, float smoothingFactor = 0.5f)
    {
        Vector3[] smoothed = new Vector3[displacedVertices.Length];

        for (int it = 0; it < iterations; it++)
        {
            for (int i = 0; i < displacedVertices.Length; i++)
            {
                Vector3 average = Vector3.zero;
                int count = 0;
                for (int j = 0; j < displacedVertices.Length; j++)
                {
                    if (i == j) continue;
                    float dist = (displacedVertices[i] - displacedVertices[j]).sqrMagnitude;
                    if (dist < ringRadiusTolerance * ringRadiusTolerance * 0.5f)
                    {
                        average += displacedVertices[j];
                        count++;
                    }
                }
                if (count > 0)
                    smoothed[i] = Vector3.Lerp(displacedVertices[i], average / count, smoothingFactor);
                else
                    smoothed[i] = displacedVertices[i];
            }
            smoothed.CopyTo(displacedVertices, 0);
        }
    }

    

    float ComputeMeshVolume(Mesh mesh, Vector3[] verts = null)
    {
        if (verts == null)
            verts = mesh.vertices;

        float volume = 0f;
        int[] tris = mesh.triangles;
        for (int i = 0; i < tris.Length; i += 3)
        {
            Vector3 v0 = verts[tris[i]];
            Vector3 v1 = verts[tris[i + 1]];
            Vector3 v2 = verts[tris[i + 2]];
            volume += SignedVolumeOfTriangle(v0, v1, v2);
        }
        return Mathf.Abs(volume);
    }

    float SignedVolumeOfTriangle(Vector3 p1, Vector3 p2, Vector3 p3)
    {
        return Vector3.Dot(p1, Vector3.Cross(p2, p3)) / 6f;
    }
}