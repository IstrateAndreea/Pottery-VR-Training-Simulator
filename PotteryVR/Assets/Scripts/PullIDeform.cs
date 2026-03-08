using UnityEngine;
using System.Collections.Generic;
using Leap;
using Leap.Unity;

[RequireComponent(typeof(MeshFilter))]
public class PullDeform : MonoBehaviour
{
    [Header("Leap Motion")]
    public LeapProvider provider;

    [Header("Clay Deformation Parameters")]
    public float pushStrength = 0.15f;
    public float ringRadiusTolerance = 0.15f;
    public float verticalTolerance = 0.13f;
    public float centerRadiusThreshold = 0.025f;
    public float minAllowedRadius = 0.01f;
    public float pullDepth = 0.35f;

    [Header("Mesh Smoothing")]
    public int smoothingIterations = 2;
    public float smoothingFactor = 0.5f;

    [Header("Limits")]
    public float maxPushHeight = 1.5f;

    
    int bandsCount = 16;
    List<List<int>> bandVertexIndices = new List<List<int>>();
    float[] bandYs;

    
    public float fingerBandProximity = 0.11f; 
    public float fingerRadiusProximity = 0.18f; 

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

       
        bandVertexIndices = new List<List<int>>(bandsCount);
        for (int i = 0; i < bandsCount; i++) bandVertexIndices.Add(new List<int>());

        float minY = float.MaxValue, maxY = float.MinValue;
        foreach (var v in displacedVertices)
        {
            if (v.y < minY) minY = v.y;
            if (v.y > maxY) maxY = v.y;
        }
        float height = maxY - minY + 1e-8f;

        for (int i = 0; i < displacedVertices.Length; i++)
        {
            Vector3 v = displacedVertices[i];
            float t = Mathf.InverseLerp(minY, maxY, v.y);
            int band = Mathf.Clamp(Mathf.RoundToInt((1.0f - t) * (bandsCount - 1)), 0, bandsCount - 1);
            bandVertexIndices[band].Add(i);
        }
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

        
        bandYs = new float[bandsCount];
        for (int b = 0; b < bandsCount; b++)
        {
            float sum = 0f;
            foreach (int idx in bandVertexIndices[b])
                sum += displacedVertices[idx].y;
            bandYs[b] = (bandVertexIndices[b].Count > 0) ? sum / bandVertexIndices[b].Count : 0f;
        }

        foreach (Hand hand in frame.Hands)
        {
            List<Vector3> contactPoints = new List<Vector3>();
            contactPoints.Add(transform.InverseTransformPoint(hand.PalmPosition.ToVector3()));
            foreach (Finger finger in hand.Fingers)
                contactPoints.Add(transform.InverseTransformPoint(finger.TipPosition.ToVector3()));

            foreach (var point in contactPoints)
            {
                float fingerRadius = Mathf.Sqrt(point.x * point.x + point.z * point.z);
                float targetY = point.y;

               
                int closestBand = 0;
                float minDist = float.MaxValue;
                for (int b = 0; b < bandsCount; b++)
                {
                    float dist = Mathf.Abs(bandYs[b] - targetY);
                    if (dist < minDist)
                    {
                        minDist = dist;
                        closestBand = b;
                    }
                }

                
                float bandAvgRadius = 0f;
                foreach (int idx in bandVertexIndices[closestBand])
                {
                    Vector3 v = displacedVertices[idx];
                    bandAvgRadius += new Vector2(v.x, v.z).magnitude;
                }
                bandAvgRadius /= Mathf.Max(1, bandVertexIndices[closestBand].Count);

                float radialDist = Mathf.Abs(bandAvgRadius - fingerRadius);

              
                if (minDist > fingerBandProximity || radialDist > fingerRadiusProximity)
                    continue;

               
                if (closestBand >= bandsCount - 1)
                    continue;

                
                if (targetY > bandYs[closestBand])
                {
                    float moveAmount = pushStrength * Time.deltaTime * 60f;

                    float[] bandShifts = new float[bandsCount];

                    for (int b = 0; b < bandsCount; b++)
                    {
                       
                        if (b >= bandsCount - 1)
                        {
                            bandShifts[b] = 0f;
                        }
                        else if (b < closestBand)
                        {
                            bandShifts[b] = moveAmount;
                        }
                        else if (b == closestBand)
                        {
                            bandShifts[b] = moveAmount;
                        }
                        else
                        {
                            float t = (float)(b - closestBand) / (bandsCount - closestBand - 1 + 1e-5f);
                            bandShifts[b] = moveAmount * Mathf.Pow(1f - t, 1.5f);
                        }
                    }

                    for (int b = 0; b < bandsCount; b++)
                    {
                        foreach (int idx in bandVertexIndices[b])
                        {
                            float newY = displacedVertices[idx].y + bandShifts[b];
                            newY = Mathf.Min(newY, maxPushHeight);
                            displacedVertices[idx].y = newY;

                            if (bandShifts[b] > 0f)
                            {
                                float thinAmount = 0.01f * (bandShifts[b] / pushStrength);
                                displacedVertices[idx].x *= (1.0f - thinAmount);
                                displacedVertices[idx].z *= (1.0f - thinAmount);
                            }
                        }
                    }
                }

               
                foreach (int idx in bandVertexIndices[closestBand])
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
            displacedVertices[i] *= scale;

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

    public void ResetMesh()
    {
        originalVertices.CopyTo(displacedVertices, 0);
        deformMesh.vertices = displacedVertices;
        deformMesh.RecalculateNormals();
        originalMeshVolume = ComputeMeshVolume(deformMesh, originalVertices);
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