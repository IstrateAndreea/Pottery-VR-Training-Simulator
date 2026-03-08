using UnityEngine;
using UnityEngine.XR.Hands;
using System.Collections.Generic;

public class HandIndentableMeshHigh : MonoBehaviour
{
    public float indentRadius = 0.03f;
    public float maxIndentStrength = 0.05f;
    public float accuracyToleranceMm = 10.0f;
    public float latencyResetDelay = 2.0f;

    public XRHandJointID[] indentJoints = {
        XRHandJointID.IndexTip, XRHandJointID.MiddleTip, XRHandJointID.ThumbTip
    };

    private Mesh mesh;
    private Vector3[] originalVertices;
    private Vector3[] deformedVertices;
    private XRHandSubsystem handSubsystem;

    private Dictionary<XRHandJointID, int> trackedFrames = new();
    private Dictionary<XRHandJointID, int> notTrackedFrames = new();
    private Dictionary<XRHandJointID, int> totalFrames = new();

    private Dictionary<XRHandJointID, float> touchStartTime = new();
    private Dictionary<XRHandJointID, bool> touchingLastFrame = new();
    private Dictionary<XRHandJointID, float> lastLatency = new();

    public struct IndentEvent
    {
        public XRHandJointID jointID;
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

        var subsystems = new List<XRHandSubsystem>();
        SubsystemManager.GetSubsystems(subsystems);
        if (subsystems.Count > 0)
            handSubsystem = subsystems[0];

        foreach (XRHandJointID jointID in indentJoints)
        {
            trackedFrames[jointID] = 0;
            notTrackedFrames[jointID] = 0;
            totalFrames[jointID] = 0;
            touchStartTime[jointID] = -1f;
            touchingLastFrame[jointID] = false;
            lastLatency[jointID] = -1f;
        }
    }

    void Update()
    {
        if (handSubsystem == null) return;
        currentIndentEvents.Clear();

        foreach (XRHandJointID jointID in indentJoints)
            totalFrames[jointID]++;

        HashSet<XRHandJointID> jointsTrackedThisFrame = new();

        // --- RIGHT HAND ONLY ---
        XRHand hand = handSubsystem.rightHand;
        if (!hand.isTracked) return;

        foreach (XRHandJointID jointID in indentJoints)
        {
            var joint = hand.GetJoint(jointID);
            if (!joint.TryGetPose(out Pose jointPose)) continue;
            Vector3 jointWorldPos = jointPose.position;

            jointsTrackedThisFrame.Add(jointID);

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

            if (isTouching && !touchingLastFrame[jointID])
            {
                touchStartTime[jointID] = Time.time;
            }

            if (affected > 0 && isTouching && touchingLastFrame[jointID] && touchStartTime[jointID] > 0)
            {
                latency = (Time.time - touchStartTime[jointID]) * 1000f; // ms
                lastLatency[jointID] = latency;
                touchStartTime[jointID] = -1f;
            }
            else if (!isTouching)
            {
                touchStartTime[jointID] = -1f;
            }

            touchingLastFrame[jointID] = isTouching;

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

            trackedFrames[jointID]++;

            if (totalNearby > 0)
            {
                int notTracked = notTrackedFrames.ContainsKey(jointID) ? notTrackedFrames[jointID] : 0;
                int total = totalFrames.ContainsKey(jointID) ? totalFrames[jointID] : 1;
                float dropoutPercent = 100f * notTracked / Mathf.Max(total, 1);

                currentIndentEvents.Add(new IndentEvent
                {
                    jointID = jointID,
                    accuracyMm = centroidDist * 1000f,
                    accuracyPercent = accuracyPercent,
                    coveragePercent = coverage,
                    dropoutPercent = dropoutPercent,
                    totalNearbyVerts = totalNearby,
                    affectedVerts = affected,
                    touchToIndentLatencyMs = (latency >= 0) ? latency : lastLatency[jointID]
                });
            }
        }

        foreach (XRHandJointID jointID in indentJoints)
        {
            if (!jointsTrackedThisFrame.Contains(jointID))
                notTrackedFrames[jointID]++;
        }

        mesh.vertices = deformedVertices;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

    public Mesh Mesh => mesh;
    public Vector3[] DeformedVertices => deformedVertices;
}