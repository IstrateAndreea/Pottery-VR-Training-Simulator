using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;
using TMPro;

public class HandMeshesDebugger : MonoBehaviour
{
    // Assign these in Inspector
    public HandIndentableMesh meshStandard;
    public HandIndentableMeshMetal meshMetal;
    public HandIndentableMeshRubber meshRubber;
    public HandIndentableMeshHigh meshHigh;
    public HandIndentableMeshDepth meshDepth;
    public HandIndentableMeshDepthSmall meshDepthSmall;

    public TMP_Text debugText;
    public TMP_Text overallTextStandard;
    public TMP_Text overallTextMetal;
    public TMP_Text overallTextRubber;
    public TMP_Text overallTextHigh;
    public TMP_Text overallTextDepth;
    public TMP_Text overallTextDepthSmall;

    public float indentRadius = 0.03f;
    public float indentStopDelay = 5.0f;

    private XRHandSubsystem handSubsystem;
    private List<string> logBuffer = new();

    // Per mesh, per joint
    private class StatBuffer
    {
        public List<float> accuracyPercent = new();
        public List<float> coveragePercent = new();
        public List<float> dropoutPercent = new();
        public List<float> touchToIndentLatencies = new();
        public float lastIndentTime = -1000f;
        public bool indentActive = false;
    }

    private Dictionary<XRHandJointID, StatBuffer> statsStandard = new();
    private Dictionary<XRHandJointID, StatBuffer> statsMetal = new();
    private Dictionary<XRHandJointID, StatBuffer> statsRubber = new();
    private Dictionary<XRHandJointID, StatBuffer> statsHigh = new();
    private Dictionary<XRHandJointID, StatBuffer> statsDepth = new();
    private Dictionary<XRHandJointID, StatBuffer> statsDepthSmall = new();

    void Start()
    {
        var subsystems = new List<XRHandSubsystem>();
        SubsystemManager.GetSubsystems(subsystems);
        if (subsystems.Count > 0)
            handSubsystem = subsystems[0];

        if (debugText != null)
            debugText.text = "Debug Log:\n";

        InitMeshStats(meshStandard, statsStandard);
        InitMeshStats(meshMetal, statsMetal);
        InitMeshStats(meshRubber, statsRubber);
        InitMeshStats(meshHigh, statsHigh);
        InitMeshStats(meshDepth, statsDepth);
        InitMeshStats(meshDepthSmall, statsDepthSmall);

        TMP_Text[] allOverallTexts = {
            overallTextStandard, overallTextMetal, overallTextRubber,
            overallTextHigh, overallTextDepth, overallTextDepthSmall
        };
        foreach (var t in allOverallTexts)
            if (t != null) t.text = "";
    }

    void InitMeshStats(MonoBehaviour mesh, Dictionary<XRHandJointID, StatBuffer> statsDict)
    {
        if (mesh == null) return;
        var indentJoints = (XRHandJointID[])mesh.GetType().GetField("indentJoints").GetValue(mesh);
        foreach (XRHandJointID joint in indentJoints)
            statsDict[joint] = new StatBuffer();
    }

    void Update()
    {
        if (handSubsystem == null)
            return;

        if (ProcessMesh(meshStandard, statsStandard, overallTextStandard))
            return;
        if (ProcessMesh(meshMetal, statsMetal, overallTextMetal))
            return;
        if (ProcessMesh(meshRubber, statsRubber, overallTextRubber))
            return;
        if (ProcessMesh(meshHigh, statsHigh, overallTextHigh))
            return;
        if (ProcessMesh(meshDepth, statsDepth, overallTextDepth))
            return;
        if (ProcessMesh(meshDepthSmall, statsDepthSmall, overallTextDepthSmall))
            return;
    }

    bool ProcessMesh(HandIndentableMesh mesh, Dictionary<XRHandJointID, StatBuffer> statsDict, TMP_Text overallText)
    {
        if (mesh == null || mesh.Mesh == null) return false;

        XRHand[] hands = { handSubsystem.leftHand, handSubsystem.rightHand };
        bool indentDetected = false;

        foreach (var indent in mesh.CurrentIndentEvents)
        {
            var s = statsDict[indent.jointID];
            float now = Time.time;

            if (indent.affectedVerts > 0)
            {
                s.accuracyPercent.Add(indent.accuracyPercent);
                s.coveragePercent.Add(indent.coveragePercent);
                s.dropoutPercent.Add(indent.dropoutPercent);
                s.lastIndentTime = now;
                s.indentActive = true;
                if (indent.touchToIndentLatencyMs >= 0)
                    s.touchToIndentLatencies.Add(indent.touchToIndentLatencyMs);

                string log =
                    $"{indent.jointID}\n" +
                    $"Latency: {(indent.touchToIndentLatencyMs >= 0 ? indent.touchToIndentLatencyMs.ToString("F1") : "--")} ms\n" +
                    $"Coverage: {indent.coveragePercent:F1}% ({indent.affectedVerts}/{indent.totalNearbyVerts})\n" +
                    $"Dropouts: {indent.dropoutPercent:F1}%";
                AddLog(log);

                indentDetected = true;
            }
            else
            {
                if (s.indentActive && (now - s.lastIndentTime) > indentStopDelay)
                {
                    ShowOverallStats(statsDict, indent.jointID, overallText);
                    statsDict[indent.jointID] = new StatBuffer();
                }
            }
        }

        float currentTime = Time.time;
        foreach (XRHandJointID joint in mesh.indentJoints)
        {
            var s = statsDict[joint];
            if (s.indentActive && (currentTime - s.lastIndentTime) > indentStopDelay)
            {
                ShowOverallStats(statsDict, joint, overallText);
                statsDict[joint] = new StatBuffer();
            }
        }
        return indentDetected;
    }

    bool ProcessMesh(HandIndentableMeshMetal mesh, Dictionary<XRHandJointID, StatBuffer> statsDict, TMP_Text overallText)
    {
        if (mesh == null || mesh.Mesh == null) return false;
        XRHand[] hands = { handSubsystem.leftHand, handSubsystem.rightHand };
        bool indentDetected = false;
        foreach (var indent in mesh.CurrentIndentEvents)
        {
            var s = statsDict[indent.jointID];
            float now = Time.time;
            if (indent.affectedVerts > 0)
            {
                s.accuracyPercent.Add(indent.accuracyPercent);
                s.coveragePercent.Add(indent.coveragePercent);
                s.dropoutPercent.Add(indent.dropoutPercent);
                s.lastIndentTime = now;
                s.indentActive = true;
                if (indent.touchToIndentLatencyMs >= 0)
                    s.touchToIndentLatencies.Add(indent.touchToIndentLatencyMs);
                string log =
                    $"{indent.jointID}\n" +
                    $"Latency: {(indent.touchToIndentLatencyMs >= 0 ? indent.touchToIndentLatencyMs.ToString("F1") : "--")} ms\n" +
                    $"Coverage: {indent.coveragePercent:F1}% ({indent.affectedVerts}/{indent.totalNearbyVerts})\n" +
                    $"Dropouts: {indent.dropoutPercent:F1}%";
                AddLog(log);
                indentDetected = true;
            }
            else
            {
                if (s.indentActive && (now - s.lastIndentTime) > indentStopDelay)
                {
                    ShowOverallStats(statsDict, indent.jointID, overallText);
                    statsDict[indent.jointID] = new StatBuffer();
                }
            }
        }
        float currentTime = Time.time;
        foreach (XRHandJointID joint in mesh.indentJoints)
        {
            var s = statsDict[joint];
            if (s.indentActive && (currentTime - s.lastIndentTime) > indentStopDelay)
            {
                ShowOverallStats(statsDict, joint, overallText);
                statsDict[joint] = new StatBuffer();
            }
        }
        return indentDetected;
    }

    bool ProcessMesh(HandIndentableMeshRubber mesh, Dictionary<XRHandJointID, StatBuffer> statsDict, TMP_Text overallText)
    {
        if (mesh == null || mesh.Mesh == null) return false;
        XRHand[] hands = { handSubsystem.leftHand, handSubsystem.rightHand };
        bool indentDetected = false;
        foreach (var indent in mesh.CurrentIndentEvents)
        {
            var s = statsDict[indent.jointID];
            float now = Time.time;
            if (indent.affectedVerts > 0)
            {
                s.accuracyPercent.Add(indent.accuracyPercent);
                s.coveragePercent.Add(indent.coveragePercent);
                s.dropoutPercent.Add(indent.dropoutPercent);
                s.lastIndentTime = now;
                s.indentActive = true;
                if (indent.touchToIndentLatencyMs >= 0)
                    s.touchToIndentLatencies.Add(indent.touchToIndentLatencyMs);
                string log =
                    $"{indent.jointID}\n" +
                    $"Latency: {(indent.touchToIndentLatencyMs >= 0 ? indent.touchToIndentLatencyMs.ToString("F1") : "--")} ms\n" +
                    $"Coverage: {indent.coveragePercent:F1}% ({indent.affectedVerts}/{indent.totalNearbyVerts})\n" +
                    $"Dropouts: {indent.dropoutPercent:F1}%";
                AddLog(log);
                indentDetected = true;
            }
            else
            {
                if (s.indentActive && (now - s.lastIndentTime) > indentStopDelay)
                {
                    ShowOverallStats(statsDict, indent.jointID, overallText);
                    statsDict[indent.jointID] = new StatBuffer();
                }
            }
        }
        float currentTime = Time.time;
        foreach (XRHandJointID joint in mesh.indentJoints)
        {
            var s = statsDict[joint];
            if (s.indentActive && (currentTime - s.lastIndentTime) > indentStopDelay)
            {
                ShowOverallStats(statsDict, joint, overallText);
                statsDict[joint] = new StatBuffer();
            }
        }
        return indentDetected;
    }

    bool ProcessMesh(HandIndentableMeshHigh mesh, Dictionary<XRHandJointID, StatBuffer> statsDict, TMP_Text overallText)
    {
        if (mesh == null || mesh.Mesh == null) return false;
        XRHand[] hands = { handSubsystem.leftHand, handSubsystem.rightHand };
        bool indentDetected = false;
        foreach (var indent in mesh.CurrentIndentEvents)
        {
            var s = statsDict[indent.jointID];
            float now = Time.time;
            if (indent.affectedVerts > 0)
            {
                s.accuracyPercent.Add(indent.accuracyPercent);
                s.coveragePercent.Add(indent.coveragePercent);
                s.dropoutPercent.Add(indent.dropoutPercent);
                s.lastIndentTime = now;
                s.indentActive = true;
                if (indent.touchToIndentLatencyMs >= 0)
                    s.touchToIndentLatencies.Add(indent.touchToIndentLatencyMs);
                string log =
                    $"{indent.jointID}\n" +
                    $"Latency: {(indent.touchToIndentLatencyMs >= 0 ? indent.touchToIndentLatencyMs.ToString("F1") : "--")} ms\n" +
                    $"Coverage: {indent.coveragePercent:F1}% ({indent.affectedVerts}/{indent.totalNearbyVerts})\n" +
                    $"Dropouts: {indent.dropoutPercent:F1}%";
                AddLog(log);
                indentDetected = true;
            }
            else
            {
                if (s.indentActive && (now - s.lastIndentTime) > indentStopDelay)
                {
                    ShowOverallStats(statsDict, indent.jointID, overallText);
                    statsDict[indent.jointID] = new StatBuffer();
                }
            }
        }
        float currentTime = Time.time;
        foreach (XRHandJointID joint in mesh.indentJoints)
        {
            var s = statsDict[joint];
            if (s.indentActive && (currentTime - s.lastIndentTime) > indentStopDelay)
            {
                ShowOverallStats(statsDict, joint, overallText);
                statsDict[joint] = new StatBuffer();
            }
        }
        return indentDetected;
    }

    bool ProcessMesh(HandIndentableMeshDepth mesh, Dictionary<XRHandJointID, StatBuffer> statsDict, TMP_Text overallText)
    {
        if (mesh == null || mesh.Mesh == null) return false;
        XRHand[] hands = { handSubsystem.leftHand, handSubsystem.rightHand };
        bool indentDetected = false;
        foreach (var indent in mesh.CurrentIndentEvents)
        {
            var s = statsDict[indent.jointID];
            float now = Time.time;
            if (indent.affectedVerts > 0)
            {
                s.accuracyPercent.Add(indent.accuracyPercent);
                s.coveragePercent.Add(indent.coveragePercent);
                s.dropoutPercent.Add(indent.dropoutPercent);
                s.lastIndentTime = now;
                s.indentActive = true;
                if (indent.touchToIndentLatencyMs >= 0)
                    s.touchToIndentLatencies.Add(indent.touchToIndentLatencyMs);
                string log =
                    $"{indent.jointID}\n" +
                    $"Latency: {(indent.touchToIndentLatencyMs >= 0 ? indent.touchToIndentLatencyMs.ToString("F1") : "--")} ms\n" +
                    $"Coverage: {indent.coveragePercent:F1}% ({indent.affectedVerts}/{indent.totalNearbyVerts})\n" +
                    $"Dropouts: {indent.dropoutPercent:F1}%";
                AddLog(log);
                indentDetected = true;
            }
            else
            {
                if (s.indentActive && (now - s.lastIndentTime) > indentStopDelay)
                {
                    ShowOverallStats(statsDict, indent.jointID, overallText);
                    statsDict[indent.jointID] = new StatBuffer();
                }
            }
        }
        float currentTime = Time.time;
        foreach (XRHandJointID joint in mesh.indentJoints)
        {
            var s = statsDict[joint];
            if (s.indentActive && (currentTime - s.lastIndentTime) > indentStopDelay)
            {
                ShowOverallStats(statsDict, joint, overallText);
                statsDict[joint] = new StatBuffer();
            }
        }
        return indentDetected;
    }

    bool ProcessMesh(HandIndentableMeshDepthSmall mesh, Dictionary<XRHandJointID, StatBuffer> statsDict, TMP_Text overallText)
    {
        if (mesh == null || mesh.Mesh == null) return false;
        XRHand[] hands = { handSubsystem.leftHand, handSubsystem.rightHand };
        bool indentDetected = false;
        foreach (var indent in mesh.CurrentIndentEvents)
        {
            var s = statsDict[indent.jointID];
            float now = Time.time;
            if (indent.affectedVerts > 0)
            {
                s.accuracyPercent.Add(indent.accuracyPercent);
                s.coveragePercent.Add(indent.coveragePercent);
                s.dropoutPercent.Add(indent.dropoutPercent);
                s.lastIndentTime = now;
                s.indentActive = true;
                if (indent.touchToIndentLatencyMs >= 0)
                    s.touchToIndentLatencies.Add(indent.touchToIndentLatencyMs);
                string log =
                    $"{indent.jointID}\n" +
                    $"Latency: {(indent.touchToIndentLatencyMs >= 0 ? indent.touchToIndentLatencyMs.ToString("F1") : "--")} ms\n" +
                    $"Coverage: {indent.coveragePercent:F1}% ({indent.affectedVerts}/{indent.totalNearbyVerts})\n" +
                    $"Dropouts: {indent.dropoutPercent:F1}%";
                AddLog(log);
                indentDetected = true;
            }
            else
            {
                if (s.indentActive && (now - s.lastIndentTime) > indentStopDelay)
                {
                    ShowOverallStats(statsDict, indent.jointID, overallText);
                    statsDict[indent.jointID] = new StatBuffer();
                }
            }
        }
        float currentTime = Time.time;
        foreach (XRHandJointID joint in mesh.indentJoints)
        {
            var s = statsDict[joint];
            if (s.indentActive && (currentTime - s.lastIndentTime) > indentStopDelay)
            {
                ShowOverallStats(statsDict, joint, overallText);
                statsDict[joint] = new StatBuffer();
            }
        }
        return indentDetected;
    }

    void ShowOverallStats(Dictionary<XRHandJointID, StatBuffer> statsDict, XRHandJointID joint, TMP_Text overallText)
    {
        if (overallText == null)
            return;
        var stat = statsDict[joint];

        float Avg(List<float> list)
        {
            if (list == null || list.Count == 0) return 0f;
            float sum = 0f;
            foreach (var v in list) sum += v;
            return sum / list.Count;
        }

        float latencySum = 0f;
        int latencyCount = 0;
        foreach (float v in stat.touchToIndentLatencies)
        {
            if (v >= 0) { latencySum += v; latencyCount++; }
        }
        float latencyAvg = (latencyCount > 0) ? (latencySum / latencyCount) : -1f;

        string text =
            $"Overall Stats [{joint}]:\n" +
            $"Accuracy: {Avg(stat.accuracyPercent):F1}%\n" +
            $"Coverage: {Avg(stat.coveragePercent):F1}%\n" +
            $"Dropouts: {Avg(stat.dropoutPercent):F1}%\n" +
            $"Latency: {(latencyAvg >= 0 ? latencyAvg.ToString("F1") : "--")} ms";
        overallText.text = text;
    }

    void AddLog(string line)
    {
        logBuffer.Add(line);
        if (logBuffer.Count > 10)
            logBuffer.RemoveAt(0);

        if (debugText != null)
            debugText.text = "Debug Log:\n" + string.Join("\n\n", logBuffer);
    }
}