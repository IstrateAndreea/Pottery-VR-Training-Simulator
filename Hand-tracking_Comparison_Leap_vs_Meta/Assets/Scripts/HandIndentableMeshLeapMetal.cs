using System.Collections.Generic;
using UnityEngine;

public class HandIndentableMeshLeapMetal : MonoBehaviour
{
    [Tooltip("Assign fingertip transforms from your hand prefab (e.g., index, middle, thumb tip).")]
    public Transform[] tipTransforms;

    public float indentRadius = 0.018f;
    public float maxIndentStrength = 0.07f;
    public float accuracyToleranceMm = 10.0f;
    public float latencyResetDelay = 2.0f;

    private Mesh mesh;
    private Vector3[] originalVertices;
    private Vector3[] deformedVertices;

    private Dictionary<Transform, int> trackedFrames = new();
    private Dictionary<Transform, int> notTrackedFrames = new();
    private Dictionary<Transform, int> totalFrames = new();

    private Dictionary<Transform, float> touchStartTime = new();
    private Dictionary<Transform, bool> touchingLastFrame = new();
    private Dictionary<Transform, float> lastLatency = new();

    public struct IndentEvent
    {
        public Transform jointTransform;
        public float accuracyMm;
        public float accuracyPercent;
        public float coveragePercent;
        public float dropoutPercent;
        public int totalNearbyVerts;
        public int affectedVerts;
        public float touchToIndentLatencyMs;
    }

    private List<IndentEvent> currentIndentEvents = new();
    public List<IndentEvent> CurrentIndentEvents => currentIndentEvents;

    void Start()
    {
        mesh = GetComponent<MeshFilter>().mesh;
        originalVertices = mesh.vertices;
        deformedVertices = (Vector3[])mesh.vertices.Clone();

        foreach (Transform tip in tipTransforms)
        {
            if (tip == null) continue;
            trackedFrames[tip] = 0;
            notTrackedFrames[tip] = 0;
            totalFrames[tip] = 0;
            touchStartTime[tip] = -1f;
            touchingLastFrame[tip] = false;
            lastLatency[tip] = -1f;
        }
    }

    void Update()
    {
        if (tipTransforms == null || tipTransforms.Length == 0) return;
        currentIndentEvents.Clear();

        foreach (Transform tip in tipTransforms)
            totalFrames[tip]++;

        HashSet<Transform> jointsTrackedThisFrame = new();

        foreach (Transform tip in tipTransforms)
        {
            if (tip == null) continue;
            Vector3 jointWorldPos = tip.position;
            jointsTrackedThisFrame.Add(tip);

            int totalNearby = 0;
            int affected = 0;
            List<int> affectedIndices = new();

            bool isTouching = false;
            float latency = -1f;

            for (int i = 0; i < deformedVertices.Length; ++i)
            {
                Vector3 worldVert = transform.TransformPoint(deformedVertices[i]);
                float dist = Vector3.Distance(jointWorldPos, worldVert);
                if (dist < indentRadius)
                {
                    isTouching = true;
                    totalNearby++;

                    float pressure = 1f - Mathf.Clamp01(dist / indentRadius);
                    pressure = Mathf.Pow(pressure, 2.0f); // metal: sharper falloff
                    float expectedIndent = maxIndentStrength * pressure;

                    Vector3 dir = (worldVert - jointWorldPos).normalized;
                    Vector3 localDir = transform.InverseTransformDirection(dir);
                    Vector3 newVertex = deformedVertices[i] + localDir * expectedIndent;

                    float originalDepth = Vector3.Distance(originalVertices[i], deformedVertices[i]);
                    float newDepth = Vector3.Distance(originalVertices[i], newVertex);

                    if (newDepth > originalDepth)
                    {
                        deformedVertices[i] = newVertex;
                        affected++;
                        affectedIndices.Add(i);
                    }
                }
            }

            if (isTouching && !touchingLastFrame[tip])
            {
                touchStartTime[tip] = Time.time;
            }

            if (affected > 0 && isTouching && touchingLastFrame[tip] && touchStartTime[tip] > 0)
            {
                latency = (Time.time - touchStartTime[tip]) * 1000f;
                lastLatency[tip] = latency;
                touchStartTime[tip] = -1f;
            }
            else if (!isTouching)
            {
                touchStartTime[tip] = -1f;
            }

            touchingLastFrame[tip] = isTouching;

            float coverage = (float)affected / Mathf.Max(totalNearby, 1) * 100f;
            float centroidDist = 0f;
            float accuracyPercent = 0f;
            Vector3 centroid = Vector3.zero;

            if (affected > 0)
            {
                foreach (int idx in affectedIndices)
                    centroid += transform.TransformPoint(deformedVertices[idx]);
                centroid /= affected;

                centroidDist = Vector3.Distance(centroid, jointWorldPos);
                float centroidDistMm = centroidDist * 1000f;
                float indentRadiusMm = indentRadius * 1000f;
                float tolerance = Mathf.Clamp(accuracyToleranceMm, 0f, indentRadiusMm);

                if (centroidDistMm <= tolerance)
                    accuracyPercent = 100f;
                else
                    accuracyPercent = Mathf.Clamp01(1f - ((centroidDistMm - tolerance) / (indentRadiusMm - tolerance))) * 100f;
            }

            trackedFrames[tip]++;

            if (totalNearby > 0)
            {
                int notTracked = notTrackedFrames.ContainsKey(tip) ? notTrackedFrames[tip] : 0;
                int total = totalFrames.ContainsKey(tip) ? totalFrames[tip] : 1;
                float dropoutPercent = 100f * notTracked / Mathf.Max(total, 1);

                currentIndentEvents.Add(new IndentEvent
                {
                    jointTransform = tip,
                    accuracyMm = centroidDist * 1000f,
                    accuracyPercent = accuracyPercent,
                    coveragePercent = coverage,
                    dropoutPercent = dropoutPercent,
                    totalNearbyVerts = totalNearby,
                    affectedVerts = affected,
                    touchToIndentLatencyMs = (latency >= 0) ? latency : lastLatency[tip]
                });
            }
        }

        foreach (Transform tip in tipTransforms)
        {
            if (tip == null) continue;
            if (!jointsTrackedThisFrame.Contains(tip))
                notTrackedFrames[tip]++;
        }

        mesh.vertices = deformedVertices;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

    public Mesh Mesh => mesh;
    public Vector3[] DeformedVertices => deformedVertices;
}