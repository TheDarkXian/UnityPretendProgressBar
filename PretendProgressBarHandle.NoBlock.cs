
using System;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace PretendProgressBar
{
    public static partial class PretendProgressBarHandle
    {
        static bool s_runningNoBlock;
        static double s_startTime;
        static double s_lastUpdateTime;
        static float s_progress;
        static float s_duration;
        static bool s_isForever;
        static bool s_showCancelBtn;

        static void EnsureUpdateHooked()
        {
            EditorApplication.update -= ProgressNoBlockUpdate;
            EditorApplication.update += ProgressNoBlockUpdate;
        }

        static void UnhookUpdate()
        {
            EditorApplication.update -= ProgressNoBlockUpdate;
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
                if (dt < 0f)
                    dt = 0f;
                s_lastUpdateTime = now;

                float elapsed = (float)(now - s_startTime);

                float prevProgress = s_progress;
                s_progress += simulProgressStep.TryGetIncrement(s_progress);
                s_progress = Mathf.Repeat(s_progress, 1f);
                bool wrapped = s_progress < prevProgress;

                bool timeUp = (!s_isForever) && (elapsed >= s_duration);

                if (wrapped)
                    sthandle.Handle(1f);
                sthandle.Handle(s_progress);

                string title = sthandle.title;
                string detail = SafeDetail(sthandle.detail, s_progress);

                bool cancelled = ShowProgress(s_showCancelBtn, title, detail, s_progress);
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














    }















}
