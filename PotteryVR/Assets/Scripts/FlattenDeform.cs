using UnityEngine;
using System.Collections.Generic;
using Leap;
using Leap.Unity;

[RequireComponent(typeof(MeshFilter))]
public class FlattenDeform : MonoBehaviour
{
    [Header("Leap Motion")]
    public LeapProvider provider;

    [Header("Fingertip (assign your index finger tip transform here)")]
    public Transform indexFingertipTransform;

    [Header("Clay Deformation Parameters")]
    public float proximityThreshold = 0.015f;
    public float maxIndentDepth = 0.07f;
    public float indentRadius = 0.025f;
    public float indentStrength = 1.2f;

    [Header("Mesh Smoothing")]
    public int smoothingIterations = 2;
    public float smoothingFactor = 0.5f;

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

    void Update()
    {
        SyncWithCurrentMesh(); 

        if (indexFingertipTransform == null) return;

        Vector3 fingerLocalPos = transform.InverseTransformPoint(indexFingertipTransform.position);

        
        float minDist = float.MaxValue;
        int closestVertIdx = -1;
        for (int i = 0; i < displacedVertices.Length; i++)
        {
            float d = Vector3.Distance(displacedVertices[i], fingerLocalPos);
            if (d < minDist)
            {
                minDist = d;
                closestVertIdx = i;
            }
        }

        if (minDist < proximityThreshold)
        {
            float penetration = proximityThreshold - minDist;
            float indentAmount = Mathf.Clamp(penetration * indentStrength, 0, maxIndentDepth);

            for (int i = 0; i < displacedVertices.Length; i++)
            {
                float distToTip = Vector3.Distance(displacedVertices[i], displacedVertices[closestVertIdx]);
                if (distToTip < indentRadius)
                {
                    float falloff = Mathf.Cos(Mathf.PI * distToTip / indentRadius) * 0.5f + 0.5f;
                    
                    Vector3 meshCenter = Vector3.zero; 

                    
                    Vector3 inwardDir = (meshCenter - displacedVertices[i]).normalized;

                    
                    Vector3 toFinger = fingerLocalPos - displacedVertices[i];

                    
                    if (Vector3.Dot(toFinger, inwardDir) > 0)
                    {
                        
                        float pushDist = Vector3.Dot(toFinger, inwardDir);
                        float moveAmount = Mathf.Min(pushDist, indentAmount * falloff);

                        displacedVertices[i] += inwardDir * moveAmount;
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
                    if (dist < indentRadius * indentRadius * 0.7f)
                    {
                        average += displacedVertices[j];
                        count++;
                    }
                }
                smoothed[i] = (count > 0)
                    ? Vector3.Lerp(displacedVertices[i], average / count, smoothingFactor)
                    : displacedVertices[i];
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