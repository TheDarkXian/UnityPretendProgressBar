using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace PretendProgressBar
{
    public static partial class PretendProgressBarHandle
    {
        static PretendProgressBarSo hanle;
        static SimulateTaskHandle sthandle;
        static SimulatedProgressIncrement simulProgressStep;
        static bool s_focusAssetsQueued;

        public static void FocusAssetsRootInstant()
        {
            if (s_focusAssetsQueued)
                return;
            s_focusAssetsQueued = true;

            EditorApplication.delayCall += FocusAssetsRoot_Do;
        }
        static void FocusAssetsRoot_Do()
        {
            EditorApplication.delayCall -= FocusAssetsRoot_Do;
            s_focusAssetsQueued = false;

            var assetsObj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets");
            if (assetsObj == null)
                return;
            var projectWindowType = Type.GetType("UnityEditor.ProjectBrowser,UnityEditor");
            if (projectWindowType != null)
            {
                EditorWindow.GetWindow(projectWindowType, false, null, true);
            }
            Selection.activeObject = assetsObj;
            ProjectWindowUtil.ShowCreatedAsset(assetsObj);
        }

        static float ticktime = 0;
        static float tickStartTime = 0;
        static Action invokeDelay;
        static void Tick()
        {
            tickStartTime = (float)EditorApplication.timeSinceStartup;
            if (tickStartTime >= ticktime)
            {
                Debug.Log(tickStartTime + ":" + ticktime);
                invokeDelay.Invoke();
                EditorApplication.update -= Tick;
                invokeDelay = null;
            }
        }
        static void StartTick(float waittime, Action invokeOnEnd)
        {
            ticktime = (float)EditorApplication.timeSinceStartup + waittime;
            EditorApplication.update += Tick;
            invokeDelay = invokeOnEnd;
        }
        static void RunProgress(PretendProgressBarSo pretend)
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
        public static void HandleSo(PretendProgressBarSo pretend)
        {
            if (hanle != null)
                return;
            if (pretend == null)
                return;

            hanle = pretend;
            float waittime = 0;

            if (pretend.hideAfterRun)
            {
                FocusAssetsRootInstant();
                waittime = 0.5f;
            }
            try
            {
                StartTick(waittime, () => { RunProgress(pretend); });
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                ForceStop();
            }

        }



        static void RunProgressBar(PretendProgressBarSo pr)
        {
            if (pr == null)
                return;

            // 初始化非阻塞状态
            s_runningNoBlock = true;
            s_startTime = EditorApplication.timeSinceStartup;
            s_lastUpdateTime = s_startTime;

            s_duration = Mathf.Max(0.1f, pr.workTime);
            s_isForever = pr.workType == PretendProgressBarSo.WorkType.Forever;
            s_showCancelBtn = pr.showCancleBtn;

            s_progress = 0f;
            simulProgressStep = new SimulatedProgressIncrement();
            simulProgressStep.ConFigureSchedule(
                pr.smoothNextTriggerMin,
                pr.smoothNextTriggerMax,
                pr.smoothIncrementMin,
                pr.smoothIncrementMax,
                pr.smoothRampDurationMin,
                pr.smoothRampDurationMax);
            simulProgressStep.ConfigureEndStall(
                pr.stallChance,
                pr.stallMinHold,
                pr.stallMaxHold,
                pr.stallTriggerProgress,
                pr.stallCapProgress);

            if (sthandle == null)
                sthandle = new SimulateTaskHandle(pr.simulateTaskTable, pr.isBlock);

            EnsureUpdateHooked();
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
            simulProgressStep.ConFigureSchedule(
                pretend.smoothNextTriggerMin,
                pretend.smoothNextTriggerMax,
                pretend.smoothIncrementMin,
                pretend.smoothIncrementMax,
                pretend.smoothRampDurationMin,
                pretend.smoothRampDurationMax);
            simulProgressStep.ConfigureEndStall(
                pretend.stallChance,
                pretend.stallMinHold,
                pretend.stallMaxHold,
                pretend.stallTriggerProgress,
                pretend.stallCapProgress);

            try
            {
                while (true)
                {
                    double now = EditorApplication.timeSinceStartup;
                    float elapsed = (float)(now - start);
                    float prevProgress = progress;
                    progress += simulProgressStep.TryGetIncrement(progress);
                    float displayProgress = Mathf.Repeat(progress, 1f);
                    if (displayProgress < prevProgress)
                        sthandle.Handle(1f);
                    sthandle.Handle(displayProgress);
                    progress = displayProgress;

                    string title = sthandle.title;
                    string detail = SafeDetail(sthandle.detail, displayProgress);

                    bool cancelled = ShowProgress(showCancelBtn, title, detail, progress);
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
            simulProgressStep.ConFigureSchedule(
                pretend.smoothNextTriggerMin,
                pretend.smoothNextTriggerMax,
                pretend.smoothIncrementMin,
                pretend.smoothIncrementMax,
                pretend.smoothRampDurationMin,
                pretend.smoothRampDurationMax);
            simulProgressStep.ConfigureEndStall(
                pretend.stallChance,
                pretend.stallMinHold,
                pretend.stallMaxHold,
                pretend.stallTriggerProgress,
                pretend.stallCapProgress);
            float progress = 0f;

            try
            {
                while (true)
                {
                    float prevProgress = progress;
                    progress += simulProgressStep.TryGetIncrement(progress);
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









}
