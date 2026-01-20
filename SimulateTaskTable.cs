using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Text.RegularExpressions;
using UnityEditor;
using System.Text;

[CreateAssetMenu(fileName = "SimulateTaskTable", menuName = "PretendProgress/SimulateTaskTable", order = 1)]
public class SimulateTaskTable : ScriptableObject
{
    public List<SimulateTask> simulateTasksTable = new List<SimulateTask>();
    public enum SimulateRunType
    {
        Order,
        Random,
        Reverse
    }

    public SimulateRunType runType;

    [TextArea(10, 30)]
    public string textArea;

    // ===== Regex =====
    static readonly Regex TaskBlockRegex =
        new Regex(@"<Task>([\s\S]*?)</Task>", RegexOptions.Compiled);

    static readonly Regex TitleRegex =
        new Regex(@"<title\s+value\s*=\s*""([^""]*)""\s*/?>", RegexOptions.Compiled);

    static readonly Regex TaskTimeRegex =
        new Regex(@"<taskTime\s+value\s*=\s*""([^""]*)""\s*/?>", RegexOptions.Compiled);

    static readonly Regex DetailBlockRegex =
        new Regex(@"<detail>([\s\S]*?)</detail>", RegexOptions.Compiled);

    static readonly Regex DetailLineRegex =
        new Regex(@"<l>([\s\S]*?)</l>", RegexOptions.Compiled);

    // ===== Entry =====
    public void HandleTextArea()
    {
        if (string.IsNullOrWhiteSpace(textArea))
        {
            Debug.LogWarning("TextArea is empty.");
            return;
        }

        if (simulateTasksTable == null)
            simulateTasksTable = new List<SimulateTask>();

        var matches = TaskBlockRegex.Matches(textArea);

        foreach (Match taskMatch in matches)
        {
            string taskContent = taskMatch.Groups[1].Value;

            var task = new SimulateTask();
            task.detail = new List<string>();
            task.taskTime = 1f; // default

            // ---- title ----
            var titleMatch = TitleRegex.Match(taskContent);
            task.title = titleMatch.Success ? titleMatch.Groups[1].Value : "Default Title";

            // ---- taskTime ----
            var timeMatch = TaskTimeRegex.Match(taskContent);
            if (timeMatch.Success && float.TryParse(timeMatch.Groups[1].Value, out float t))
            {
                task.taskTime = Mathf.Max(0.01f, t);
            }

            // ---- detail ----
            var detailBlockMatch = DetailBlockRegex.Match(taskContent);
            if (detailBlockMatch.Success)
            {
                string detailBlock = detailBlockMatch.Groups[1].Value;
                var lineMatches = DetailLineRegex.Matches(detailBlock);

                foreach (Match line in lineMatches)
                {
                    string text = line.Groups[1].Value.Trim();
                    if (!string.IsNullOrEmpty(text))
                        task.detail.Add(text);
                }
            }

            if (task.detail.Count == 0)
                task.detail.Add("Processing... {P}%");

            simulateTasksTable.Add(task);
        }

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }



    /// <summary>
    /// 从 simulateTasksTable 反向生成 textArea（覆盖）。
    /// </summary>
    public void BuildTextAreaFromTasks(bool includeTaskTime = true)
    {
        if (simulateTasksTable == null || simulateTasksTable.Count == 0)
        {
            textArea = string.Empty;
#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif
            return;
        }

        var sb = new StringBuilder(1024);

        for (int i = 0; i < simulateTasksTable.Count; i++)
        {
            var task = simulateTasksTable[i] ?? new SimulateTask();

            string title = Escape(task.title);
            float taskTime = task.taskTime <= 0f ? 1f : task.taskTime;

            sb.AppendLine("<Task>");
            sb.Append("    <title value=\"").Append(title).AppendLine("\"/>");

            if (includeTaskTime)
            {
                sb.Append("    <taskTime value=\"").Append(taskTime.ToString("0.###")).AppendLine("\"/>");
            }

            sb.AppendLine("    <detail>");

            var details = task.detail;
            if (details != null && details.Count > 0)
            {
                foreach (var line in details)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    sb.Append("        <l>").Append(Escape(line.Trim())).AppendLine("</l>");
                }
            }
            else
            {
                sb.AppendLine("        <l>Processing... {P}%</l>");
            }

            sb.AppendLine("    </detail>");
            sb.AppendLine("</Task>");

            if (i != simulateTasksTable.Count - 1)
                sb.AppendLine();
        }

        textArea = sb.ToString();

#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
#endif
    }

    static string Escape(string s)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;

        // 轻量 XML 转义（足够应对你这个格式）
        return s.Replace("&", "&amp;")
                .Replace("\"", "&quot;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;");
    }
}

[System.Serializable]
public class SimulateTask
{   
    public string title;
    //{0} 是当前进度百分比
    public List<string> detail;
    //任务时长
    public float taskTime;

}