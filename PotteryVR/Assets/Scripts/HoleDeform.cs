using UnityEngine;
using System.Collections.Generic;
using Leap;
using Leap.Unity;

public enum FingerToUse
{
    Thumb = 0,
    Index = 1,
    Middle = 2,
    Ring = 3,
    Pinky = 4
}

[RequireComponent(typeof(MeshFilter))]
public class HoleDeform : MonoBehaviour
{
    [Header("Leap Motion")]
    public LeapProvider provider;

    [Header("Finger Selection")]
    public List<FingerToUse> fingersToUse = new List<FingerToUse> { FingerToUse.Index, FingerToUse.Middle };

    [Header("Clay Deformation Parameters")]
    public float holeStrength = 0.15f;
    public float neckStrength = 0.24f;
    public float neckThinAmount = 0.06f;
    public float ringRadiusTolerance = 0.15f;
    public float verticalTolerance = 0.13f;
    public float neckRadiusThreshold = 0.07f;
    public float centerRadiusThreshold = 0.025f;
    public float minAllowedRadius = 0.01f;

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
            foreach (var fingerEnum in fingersToUse)
            {
                var finger = hand.Fingers[(int)fingerEnum];
                contactPoints.Add(transform.InverseTransformPoint(finger.TipPosition.ToVector3()));
            }

            foreach (var point in contactPoints)
            {
                float targetRadius = Mathf.Sqrt(point.x * point.x + point.z * point.z);
                float targetY = point.y;
                List<int> bandIndices = new List<int>();


                for (int i = 0; i < displacedVertices.Length; i++)
                {
                    Vector3 v = displacedVertices[i];
                    float vRadius = Mathf.Sqrt(v.x * v.x + v.z * v.z);


                    float bandVertical = (targetRadius < neckRadiusThreshold) ? verticalTolerance * 0.5f : verticalTolerance;
                    float bandRadius = (targetRadius < neckRadiusThreshold) ? ringRadiusTolerance * 0.5f : ringRadiusTolerance;

                    if (Mathf.Abs(v.y - targetY) < bandVertical && Mathf.Abs(vRadius - targetRadius) < bandRadius)
                        bandIndices.Add(i);
                }

                if (bandIndices.Count == 0)
                    continue;


                foreach (int idx in bandIndices)
                {
                    Vector3 v = displacedVertices[idx];
                    float vRadius = Mathf.Sqrt(v.x * v.x + v.z * v.z);
                    Vector2 flat = new Vector2(v.x, v.z);
                    float flatLen = flat.magnitude;

                    float pullAmount, thinAmount, newLen;
                    if (targetRadius < neckRadiusThreshold)
                    {
                        pullAmount = neckStrength;
                        thinAmount = neckThinAmount;
                    }
                    else
                    {
                        pullAmount = holeStrength;
                        thinAmount = 0.01f;
                    }

                    pullAmount = Mathf.Max(0f, pullAmount);

                    newLen = Mathf.Max(flatLen - pullAmount, minAllowedRadius);
                    newLen *= (1.0f - thinAmount);

                    if (flatLen > 1e-5f) flat = flat.normalized * newLen;
                    displacedVertices[idx].x = flat.x;
                    displacedVertices[idx].z = flat.y;
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