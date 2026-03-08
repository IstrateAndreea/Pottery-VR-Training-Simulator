using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class HandDebuggerLeap : MonoBehaviour
{
    public HandIndentableMeshLeapMetal meshLeapMetal;
    public HandIndentableMeshLeapRubber meshLeapRubber;
    public HandIndentableMeshHighLeap meshHighLeap;
    public HandIndentableMeshLeapDepth meshLeapDepth;
    public HandIndentableMeshLeapDepthSmall meshLeapDepthSmall;
    public HandIndentableMesh_ManualTips meshManualTips;

    public TMP_Text debugText;
    public TMP_Text overallTextManualTips;
    public TMP_Text overallTextLeapMetal;
    public TMP_Text overallTextLeapRubber;
    public TMP_Text overallTextHighLeap;
    public TMP_Text overallTextLeapDepth;
    public TMP_Text overallTextLeapDepthSmall;

    public float indentStopDelay = 5.0f;

    private List<string> logBuffer = new();

    private class StatBuffer
    {
        public List<float> accuracyPercent = new();
        public List<float> coveragePercent = new();
        public List<float> dropoutPercent = new();
        public List<float> touchToIndentLatencies = new();
        public float lastIndentTime = -1000f;
        public bool indentActive = false;
    }

    private Dictionary<Transform, StatBuffer> statsManualTips = new();
    private Dictionary<Transform, StatBuffer> statsLeapMetal = new();
    private Dictionary<Transform, StatBuffer> statsLeapRubber = new();
    private Dictionary<Transform, StatBuffer> statsHighLeap = new();
    private Dictionary<Transform, StatBuffer> statsLeapDepth = new();
    private Dictionary<Transform, StatBuffer> statsLeapDepthSmall = new();

    void Start()
    {
        InitMeshStats(meshManualTips, statsManualTips);
        InitMeshStats(meshLeapMetal, statsLeapMetal);
        InitMeshStats(meshLeapRubber, statsLeapRubber);
        InitMeshStats(meshHighLeap, statsHighLeap);
        InitMeshStats(meshLeapDepth, statsLeapDepth);
        InitMeshStats(meshLeapDepthSmall, statsLeapDepthSmall);

        if (debugText != null)
            debugText.text = "Debug Log:\n";

        TMP_Text[] allOverallTexts = {
            overallTextManualTips, overallTextLeapMetal, overallTextLeapRubber,
            overallTextHighLeap, overallTextLeapDepth, overallTextLeapDepthSmall
        };
        foreach (var t in allOverallTexts)
            if (t != null) t.text = "";
    }

    void InitMeshStats(MonoBehaviour mesh, Dictionary<Transform, StatBuffer> statsDict)
    {
        if (mesh == null) return;
        var tips = (Transform[])mesh.GetType().GetField("tipTransforms").GetValue(mesh);
        foreach (Transform tip in tips)
        {
            if (tip == null) continue;
            statsDict[tip] = new StatBuffer();
        }
    }

    void Update()
    {
        if (ProcessMesh(meshManualTips, statsManualTips, overallTextManualTips))
            return;
        if (ProcessMesh(meshLeapMetal, statsLeapMetal, overallTextLeapMetal))
            return;
        if (ProcessMesh(meshLeapRubber, statsLeapRubber, overallTextLeapRubber))
            return;
        if (ProcessMesh(meshHighLeap, statsHighLeap, overallTextHighLeap))
            return;
        if (ProcessMesh(meshLeapDepth, statsLeapDepth, overallTextLeapDepth))
            return;
        if (ProcessMesh(meshLeapDepthSmall, statsLeapDepthSmall, overallTextLeapDepthSmall))
            return;
    }

    // ONE function for each mesh type. This is Unity best practice for type safety.
    bool ProcessMesh(HandIndentableMesh_ManualTips mesh, Dictionary<Transform, StatBuffer> statsDict, TMP_Text overallText)
    {
        if (mesh == null || mesh.Mesh == null) return false;

        bool indentDetected = false;
        foreach (var indent in mesh.CurrentIndentEvents)
        {
            var s = statsDict[indent.jointTransform];
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
                    $"{indent.jointTransform.name}\n" +
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
                    ShowOverallStats(statsDict, indent.jointTransform, overallText);
                    statsDict[indent.jointTransform] = new StatBuffer();
                }
            }
        }

        float currentTime = Time.time;
        foreach (Transform tip in mesh.tipTransforms)
        {
            if (tip == null) continue;
            var s = statsDict[tip];
            if (s.indentActive && (currentTime - s.lastIndentTime) > indentStopDelay)
            {
                ShowOverallStats(statsDict, tip, overallText);
                statsDict[tip] = new StatBuffer();
            }
        }
        return indentDetected;
    }

    bool ProcessMesh(HandIndentableMeshLeapMetal mesh, Dictionary<Transform, StatBuffer> statsDict, TMP_Text overallText)
    {
        if (mesh == null || mesh.Mesh == null) return false;

        bool indentDetected = false;
        foreach (var indent in mesh.CurrentIndentEvents)
        {
            var s = statsDict[indent.jointTransform];
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
                    $"{indent.jointTransform.name}\n" +
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
                    ShowOverallStats(statsDict, indent.jointTransform, overallText);
                    statsDict[indent.jointTransform] = new StatBuffer();
                }
            }
        }

        float currentTime = Time.time;
        foreach (Transform tip in mesh.tipTransforms)
        {
            if (tip == null) continue;
            var s = statsDict[tip];
            if (s.indentActive && (currentTime - s.lastIndentTime) > indentStopDelay)
            {
                ShowOverallStats(statsDict, tip, overallText);
                statsDict[tip] = new StatBuffer();
            }
        }
        return indentDetected;
    }

    bool ProcessMesh(HandIndentableMeshLeapRubber mesh, Dictionary<Transform, StatBuffer> statsDict, TMP_Text overallText)
    {
        if (mesh == null || mesh.Mesh == null) return false;

        bool indentDetected = false;
        foreach (var indent in mesh.CurrentIndentEvents)
        {
            var s = statsDict[indent.jointTransform];
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
                    $"{indent.jointTransform.name}\n" +
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
                    ShowOverallStats(statsDict, indent.jointTransform, overallText);
                    statsDict[indent.jointTransform] = new StatBuffer();
                }
            }
        }

        float currentTime = Time.time;
        foreach (Transform tip in mesh.tipTransforms)
        {
            if (tip == null) continue;
            var s = statsDict[tip];
            if (s.indentActive && (currentTime - s.lastIndentTime) > indentStopDelay)
            {
                ShowOverallStats(statsDict, tip, overallText);
                statsDict[tip] = new StatBuffer();
            }
        }
        return indentDetected;
    }

    bool ProcessMesh(HandIndentableMeshHighLeap mesh, Dictionary<Transform, StatBuffer> statsDict, TMP_Text overallText)
    {
        if (mesh == null || mesh.Mesh == null) return false;

        bool indentDetected = false;
        foreach (var indent in mesh.CurrentIndentEvents)
        {
            var s = statsDict[indent.jointTransform];
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
                    $"{indent.jointTransform.name}\n" +
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
                    ShowOverallStats(statsDict, indent.jointTransform, overallText);
                    statsDict[indent.jointTransform] = new StatBuffer();
                }
            }
        }

        float currentTime = Time.time;
        foreach (Transform tip in mesh.tipTransforms)
        {
            if (tip == null) continue;
            var s = statsDict[tip];
            if (s.indentActive && (currentTime - s.lastIndentTime) > indentStopDelay)
            {
                ShowOverallStats(statsDict, tip, overallText);
                statsDict[tip] = new StatBuffer();
            }
        }
        return indentDetected;
    }

    bool ProcessMesh(HandIndentableMeshLeapDepth mesh, Dictionary<Transform, StatBuffer> statsDict, TMP_Text overallText)
    {
        if (mesh == null || mesh.Mesh == null) return false;

        bool indentDetected = false;
        foreach (var indent in mesh.CurrentIndentEvents)
        {
            var s = statsDict[indent.jointTransform];
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
                    $"{indent.jointTransform.name}\n" +
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
                    ShowOverallStats(statsDict, indent.jointTransform, overallText);
                    statsDict[indent.jointTransform] = new StatBuffer();
                }
            }
        }

        float currentTime = Time.time;
        foreach (Transform tip in mesh.tipTransforms)
        {
            if (tip == null) continue;
            var s = statsDict[tip];
            if (s.indentActive && (currentTime - s.lastIndentTime) > indentStopDelay)
            {
                ShowOverallStats(statsDict, tip, overallText);
                statsDict[tip] = new StatBuffer();
            }
        }
        return indentDetected;
    }

    bool ProcessMesh(HandIndentableMeshLeapDepthSmall mesh, Dictionary<Transform, StatBuffer> statsDict, TMP_Text overallText)
    {
        if (mesh == null || mesh.Mesh == null) return false;

        bool indentDetected = false;
        foreach (var indent in mesh.CurrentIndentEvents)
        {
            var s = statsDict[indent.jointTransform];
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
                    $"{indent.jointTransform.name}\n" +
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
                    ShowOverallStats(statsDict, indent.jointTransform, overallText);
                    statsDict[indent.jointTransform] = new StatBuffer();
                }
            }
        }

        float currentTime = Time.time;
        foreach (Transform tip in mesh.tipTransforms)
        {
            if (tip == null) continue;
            var s = statsDict[tip];
            if (s.indentActive && (currentTime - s.lastIndentTime) > indentStopDelay)
            {
                ShowOverallStats(statsDict, tip, overallText);
                statsDict[tip] = new StatBuffer();
            }
        }
        return indentDetected;
    }

    void ShowOverallStats(Dictionary<Transform, StatBuffer> statsDict, Transform tip, TMP_Text overallText)
    {
        if (overallText == null)
            return;
        var stat = statsDict[tip];

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
            $"Overall Stats [{tip.name}]:\n" +
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