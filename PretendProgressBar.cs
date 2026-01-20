using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace PretendProgressBar
{
    public static class PretendProgressBarHandle
    {
        static PretendProgressBarSo hanle;
        static SimulateTaskHandle sthandle;
        static SimulatedProgressIncrement simulProgressStep;

        const double k_ForeverHardTimeoutSeconds = 120.0;

        public static void HandleSo(PretendProgressBarSo pretend)
        {
            if (hanle != null) return;
            if (pretend == null) return;

            hanle = pretend;

            try
            {
                sthandle = new SimulateTaskHandle(pretend.simulateTaskTable, pretend.isBlock);

                if (pretend.isBlock)
                {
                    RunProgressBarBlocking(pretend);
                }
                else
                {
                    RunProgressBar(pretend);
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                ForceStop();
            }
    
    
    
        }

 // ====== Non-blocking state ======
static bool s_runningNoBlock;
static double s_startTime;
static double s_lastUpdateTime;
static float s_progress;              
static float s_duration;              
static bool s_isForever;
static bool s_showCancelBtn;

// 防止重复注册 update
static void EnsureUpdateHooked()
{
    EditorApplication.update -= ProgressNoBlockUpdate;
    EditorApplication.update += ProgressNoBlockUpdate;
}

static void UnhookUpdate()
{
    EditorApplication.update -= ProgressNoBlockUpdate;
}

static void RunProgressBar(PretendProgressBarSo pr)
{
    if (pr == null) return;

    // 初始化非阻塞状态
    s_runningNoBlock = true;
    s_startTime = EditorApplication.timeSinceStartup;
    s_lastUpdateTime = s_startTime;

    s_duration = Mathf.Max(0.1f, pr.workTime);
    s_isForever = pr.workType == PretendProgressBarSo.WorkType.Forever;
    s_showCancelBtn = pr.showCancleBtn;

    s_progress = 0f;
    simulProgressStep = new SimulatedProgressIncrement();

    if (sthandle == null)
        sthandle = new SimulateTaskHandle(pr.simulateTaskTable, pr.isBlock);

    EnsureUpdateHooked();
}

static void ProgressNoBlockUpdate()
{
    if (!s_runningNoBlock || hanle == null || sthandle == null)
    {
        StopNoBlock();
        return;
    }

    try
    {
        double now = EditorApplication.timeSinceStartup;
        float dt = (float)(now - s_lastUpdateTime);
        if (dt < 0f) dt = 0f;
        s_lastUpdateTime = now;

        float elapsed = (float)(now - s_startTime);

        float prevProgress = s_progress;
        s_progress += simulProgressStep.TryGetIncrement();
        s_progress = Mathf.Repeat(s_progress, 1f);
        bool wrapped = s_progress < prevProgress;

        bool timeUp = (!s_isForever) && (elapsed >= s_duration);

        if (wrapped)
            sthandle.Handle(1f);
        sthandle.Handle(s_progress);

        string title = sthandle.title;
        string detail = SafeDetail(sthandle.detail, s_progress);

        bool cancelled = ShowProgress(s_showCancelBtn, title, detail, s_progress);
        if (s_isForever && !s_showCancelBtn)
        {
            if ((now - s_startTime) >= k_ForeverHardTimeoutSeconds)
            {
                Debug.LogWarning($"PretendProgress: Forever mode without cancel button hit hard-timeout ({k_ForeverHardTimeoutSeconds}s). Stopping to avoid locking the Editor.");
                StopNoBlock();
                return;
            }
        }

        if (cancelled || timeUp)
        {
            StopNoBlock();
            return;
        }
        EditorApplication.QueuePlayerLoopUpdate();
        InternalEditorUtility.RepaintAllViews();
    }
    catch (Exception e)
    {
        Debug.LogException(e);
        StopNoBlock();
    }
}

static void StopNoBlock()
{
    s_runningNoBlock = false;
    UnhookUpdate();
    ForceStop(); 
}






        static void RunProgressBarBlocking(PretendProgressBarSo pretend)
        {
            switch (pretend.workType)
            {
                case PretendProgressBarSo.WorkType.Forever:
                    Forever(pretend);
                    break;
                case PretendProgressBarSo.WorkType.ByTime:
                    ByTime(pretend);
                    break;
                default:
                    Forever(pretend);
                    break;
            }
        }

        static void ByTime(PretendProgressBarSo pretend)
        {
            double start = EditorApplication.timeSinceStartup;
            float duration = Mathf.Max(0.1f, pretend.workTime);
            bool showCancelBtn = pretend.showCancleBtn;
            float progress = 0f;
            simulProgressStep = new SimulatedProgressIncrement();

            try
            {
                while (true)
                {
                    double now = EditorApplication.timeSinceStartup;
                    float elapsed = (float)(now - start);
                    float prevProgress = progress;
                    progress += simulProgressStep.TryGetIncrement();
                    float displayProgress = Mathf.Repeat(progress, 1f);
                    if (displayProgress < prevProgress)
                        sthandle.Handle(1f);
                    sthandle.Handle(displayProgress);
                    progress = displayProgress;

                    string title = sthandle.title;
                    string detail = SafeDetail(sthandle.detail, displayProgress);

                    bool cancelled = ShowProgress(showCancelBtn, title, detail,progress);
                    PumpEditorUI();

                    if (cancelled || elapsed >= duration)
                        break;

                    Thread.Sleep(50);
                }
            }
            finally
            {
                ForceStop();
            }
        }

        static void Forever(PretendProgressBarSo pretend)
        {
            double start = EditorApplication.timeSinceStartup;
            bool showCancelBtn = pretend.showCancleBtn;

            simulProgressStep = new SimulatedProgressIncrement();
            float progress = 0f;

            try
            {
                while (true)
                {
                    if (!showCancelBtn)
                    {
                        double now = EditorApplication.timeSinceStartup;
                        if (now - start >= k_ForeverHardTimeoutSeconds)
                        {
                            Debug.LogWarning($"PretendProgress: Forever mode without cancel button hit hard-timeout ({k_ForeverHardTimeoutSeconds}s). Stopping to avoid locking the Editor.");
                            break;
                        }
                    }
                    float prevProgress = progress;
                    progress += simulProgressStep.TryGetIncrement();
                    float displayProgress = Mathf.Repeat(progress, 1f);
                    if (displayProgress < prevProgress)
                        sthandle.Handle(1f);
                    sthandle.Handle(displayProgress);

                    string title = sthandle.title;
                    string detail = SafeDetail(sthandle.detail, displayProgress);

                    progress = displayProgress;
                    bool cancelled = ShowProgress(showCancelBtn, title, detail, displayProgress);
                    PumpEditorUI();
                    if (cancelled)
                        break;

                    Thread.Sleep(50);
                }
            }
            finally
            {
                ForceStop();
            }
        }

        static string SafeDetail(string template, float progress01)
        {
            string percent = (progress01 * 100f).ToString("F0");

            if (string.IsNullOrEmpty(template))
                return percent + "%";
            if (template.Contains("{P}"))
                return template.Replace("{P}", percent);

            if (template.Contains("{0}"))
                return template.Replace("{0}", percent);

            return template;
        }

        static bool ShowProgress(bool showCancelBtn, string title, string detail, float progress)
        {
            if (showCancelBtn)
                return EditorUtility.DisplayCancelableProgressBar(title, detail, progress);

            EditorUtility.DisplayProgressBar(title, detail, progress);
            return false;
        }

        static void PumpEditorUI()
        {
            EditorApplication.QueuePlayerLoopUpdate();
            InternalEditorUtility.RepaintAllViews();
        }

        static void ForceStop()
        {
            hanle = null;
            sthandle = null;
            simulProgressStep = null;

            EditorUtility.ClearProgressBar();
        }
    
    
    
    
    
    
    }

    public class SimulateTaskHandle
    {
        public SimulateTaskTable simulateTaskTable;

        public Queue<SimulateTask> simualateQueue;
        public SimulateTask curentTask;
        public float simualteTableTaskTime;
        public bool cycle;

        public string title;
        public string detail;
        public Queue<string> detaillist;

        static readonly SimulateTask k_FallbackTask = new SimulateTask
        {
            title = "Working",
            detail = new List<string>{"Processing... {P}%" },
            taskTime = 1f
        };

        public SimulateTaskHandle(SimulateTaskTable st, bool cycle)
        {
            simulateTaskTable = st;
            this.cycle = cycle;

            IniTable(st);

            curentTask = GetSimulateTast();
            if (curentTask == null) curentTask = k_FallbackTask;

            EnsureDetailQueue(curentTask);
            title = curentTask.title ?? "Working";
            detail = SafePeekDetail();
        }

        public void Handle(float progress01)
        {
            if (progress01 >= 1f)
            {
                if (detaillist != null && detaillist.Count > 1)
                {
                    detaillist.Dequeue();
                }
                else
                {
                    curentTask = GetSimulateTast();
                    if (curentTask == null) curentTask = k_FallbackTask;

                    EnsureDetailQueue(curentTask);
                }
            }

            title = curentTask?.title ?? "Working";
            detail = SafePeekDetail();
        }

        void EnsureDetailQueue(SimulateTask task)
        {
            if (task == null)
            {
                detaillist = new Queue<string>(k_FallbackTask.detail);
                return;
            }

            IEnumerable<string> details = task.detail;

            if (details == null)
            {
                detaillist = new Queue<string>(k_FallbackTask.detail);
                return;
            }

            var arr = details.Where(s => !string.IsNullOrEmpty(s)).ToList();
            if (arr.Count== 0)
                arr = k_FallbackTask.detail;

            detaillist = new Queue<string>(arr);
        }

        string SafePeekDetail()
        {
            if (detaillist == null || detaillist.Count == 0)
                return "Processing... {P}%";

            return detaillist.Peek();
        }

        void IniTable(SimulateTaskTable table)
        {
            simualateQueue = new Queue<SimulateTask>();

            if (table == null || table.simulateTasksTable == null || table.simulateTasksTable.Count == 0)
            {
                simualateQueue.Enqueue(k_FallbackTask);
                simualteTableTaskTime = 1f;
                return;
            }

            var runType = table.runType;
            switch (runType)
            {
                case SimulateTaskTable.SimulateRunType.Order:
                {
                    foreach (var i in table.simulateTasksTable)
                        simualateQueue.Enqueue(i);
                    break;
                }
                case SimulateTaskTable.SimulateRunType.Random:
                {
                    var list = new List<SimulateTask>(table.simulateTasksTable);
                    while (list.Count > 0)
                    {
                        int randomdex = UnityEngine.Random.Range(0, list.Count);
                        simualateQueue.Enqueue(list[randomdex]);
                        list.RemoveAt(randomdex);
                    }
                    break;
                }
                case SimulateTaskTable.SimulateRunType.Reverse:
                {
                    for (int i = table.simulateTasksTable.Count - 1; i >= 0; i--)
                        simualateQueue.Enqueue(table.simulateTasksTable[i]);
                    break;
                }
                default:
                {
                    foreach (var i in table.simulateTasksTable)
                        simualateQueue.Enqueue(i);
                    break;
                }
            }

            simualteTableTaskTime = table.simulateTasksTable.Sum(v => v != null ? v.taskTime : 0f);
            if (simualteTableTaskTime <= 0f) simualteTableTaskTime = 1f;
        }

        public SimulateTask GetSimulateTast()
        {
            if (simualateQueue != null && simualateQueue.TryDequeue(out var re) && re != null)
            {
                return re;
            }

            if (cycle && simulateTaskTable != null)
            {
                IniTable(simulateTaskTable);
                return GetSimulateTast();
            }

            return k_FallbackTask;
        }
    }
}
