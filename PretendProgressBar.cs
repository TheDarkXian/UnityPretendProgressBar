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

        // 误配置保护：Forever 且不显示取消按钮时，避免永久锁死 Editor
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
                    // 你原来这里还没实现；保持不做事
                    // 若以后你要非阻塞版本，建议用 EditorApplication.update。
                    RunProgressBar(pretend);
                }
            }
            catch (Exception e)
            {
                // 这里不要让异常把“hanle”锁死
                Debug.LogException(e);
                ForceStop();
            }
    
    
    
        }

 // ====== Non-blocking state ======
static bool s_runningNoBlock;
static double s_startTime;
static double s_lastUpdateTime;
static float s_progress;              // 用于显示的 progress（可随机推进）
static float s_duration;              // ByTime 时长
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

    // 确保有 task
    if (sthandle == null)
        sthandle = new SimulateTaskHandle(pr.simulateTaskTable, pr.isBlock);

    EnsureUpdateHooked();
}

static void ProgressNoBlockUpdate()
{
    // 任何原因导致 handle 丢了：都停
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

        // 1) 计算“显示用”的 progress（你原本想要 pretend progress，所以这里默认用随机增量推进）
        //    - Forever：一直循环 [0,1)
        //    - ByTime：也用随机增量推进，但结束条件用时间（避免永远不到 1）
        s_progress += simulProgressStep.TryGetIncrement();
        s_progress = Mathf.Repeat(s_progress, 1f);

        // 2) 计算结束条件与“用于结束判定的 progress”
        //    - Forever：只有取消/硬超时才结束
        //    - ByTime：elapsed >= duration 结束
        bool timeUp = (!s_isForever) && (elapsed >= s_duration);

        // 3) 驱动任务文案
        //    这里用 s_progress 驱动，保持“假进度”的节奏
        sthandle.Handle(s_progress);

        string title = sthandle.title;
        string detail = SafeDetail(sthandle.detail, s_progress);

        // 4) 展示与取消
        bool cancelled = ShowProgress(s_showCancelBtn, title, detail, s_progress);

        // 5) 结束条件
        //    Forever 的硬超时保护（沿用你阻塞版本的策略）
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

        // 非阻塞模式下，update 自己会驱动重绘；
        // 但为了进度条显示更顺滑，可以保留这两句（开销可接受）
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
    ForceStop(); // 复用你现有的清理：hanle/sthandle/simulProgressStep + ClearProgressBar
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
            // 你原脚本没实现：这里给一个安全实现（仍然阻塞）。
            // 逻辑：按时间推进 progress（0->1），任务切换仍以 progress 驱动（>=1 时换）
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
                    progress += simulProgressStep.TryGetIncrement();
                    progress = Mathf.Repeat(progress, 1f);

                    // 用“最终用于展示的 progress”驱动任务
                    sthandle.Handle(progress);

                    string title = sthandle.title;
                    string detail = SafeDetail(sthandle.detail, progress);

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
                    // 误配置保护：Forever 且没有取消按钮 -> 超时退出，避免永久锁死
                    if (!showCancelBtn)
                    {
                        double now = EditorApplication.timeSinceStartup;
                        if (now - start >= k_ForeverHardTimeoutSeconds)
                        {
                            Debug.LogWarning($"PretendProgress: Forever mode without cancel button hit hard-timeout ({k_ForeverHardTimeoutSeconds}s). Stopping to avoid locking the Editor.");
                            break;
                        }
                    }

                    // Forever 就只走“假进度增量”，并循环到 [0,1)
                    progress += simulProgressStep.TryGetIncrement();
                    sthandle.Handle(progress);
                    progress = Mathf.Repeat(progress, 1f);


                    string title = sthandle.title;
                    string detail = SafeDetail(sthandle.detail, progress);

                    bool cancelled = ShowProgress(showCancelBtn, title, detail, progress);
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
            // 推荐你在配置里用 {P} 作为百分比占位符，例如： "Loading... {P}%"
            // 这样不会因为 { } 导致 string.Format 崩溃。
            string percent = (progress01 * 100f).ToString("F0");

            if (string.IsNullOrEmpty(template))
                return percent + "%";

            // 安全替换：优先 {P}，兼容你旧逻辑的 "{0}"（但不会抛 FormatException）
            // - 如果包含 {P}：Replace
            // - 如果包含 "{0}"：手动替换为 percent（不用 string.Format）
            // 其它情况：原样返回
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
            // 阻塞主线程时，强制 Editor 刷新视图和 player loop，
            // 可以显著改善进度条不刷新/取消按钮不响应的问题。
            EditorApplication.QueuePlayerLoopUpdate();
            InternalEditorUtility.RepaintAllViews();
        }

        static void ForceStop()
        {
            hanle = null;
            sthandle = null;
            simulProgressStep = null;

            // ClearProgressBar 不要放在 catch 里单点调用；统一在 stop/finally 里保证执行
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
            // 假设你的 SimulateTask 有这些字段；如果没有，请按你实际类型改一下
            title = "Working",
            detail = new List<string>{"Processing... {P}%" },
            taskTime = 1f
        };

        public SimulateTaskHandle(SimulateTaskTable st, bool cycle)
        {
            simulateTaskTable = st;
            this.cycle = cycle;

            IniTable(st);

            // 关键：构造时就保证 currentTask 和 detail 队列可用
            curentTask = GetSimulateTast();
            if (curentTask == null) curentTask = k_FallbackTask;

            EnsureDetailQueue(curentTask);
            title = curentTask.title ?? "Working";
            detail = SafePeekDetail();
        }

        public void Handle(float progress01)
        {
            // 这里根据你的设计：progress >= 1 换任务或弹 detail
            // 但你外层 forever/bytime 大多不会到 1，因此这里保持你的原意：
            // 当 progress01 >= 1 时切换；否则保持当前 detail。
            if (progress01 >= 1f)
            {
                if (detaillist != null && detaillist.Count > 1)
                {
                    // 保留至少 1 条，避免 Peek 空
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
                // 空表兜底：至少塞一个任务，避免后续 null
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

            // cycle==false 且队列空：不要返回 null（避免上层 NRE）
            return k_FallbackTask;
        }
    }
}
