using System.Collections.Generic;
using UnityEngine;
namespace PretendProgressBar
{


    [CreateAssetMenu(fileName = "PretendProgressSo", menuName = "PretendProgress/PretendProgressSo", order = 1)]
    public class PretendProgressBarSo : ScriptableObject
    {
        //阻塞主线程
        [InspectorName("是否阻塞主线程")]
        public bool isBlock;
        [InspectorName("运行后隐藏属性面板")]
        public bool hideAfterRun;
        public enum WorkType
        {
            //永远执行(直到点击取消)
            Forever,
            ByTime,
        }
        //仅当ByTime的时候出现

        [InspectorName("运行时间(仅在ByTime下有效)")]
        public float workTime = 15;
        [InspectorName("运行类型")]
        public WorkType workType;
        //public bool Advance=false;
        [InspectorName("模拟任务表")]
        public SimulateTaskTable simulateTaskTable;
        //谨慎配置，否则你只能重启unity了
        [HideInInspector]
        public bool showCancleBtn = true;

        [Header("进度平滑参数"), InspectorName("平滑触发最小间隔(秒)")]
        public float smoothNextTriggerMin = 0f;
        [InspectorName("平滑触发最大间隔(秒)")]
        public float smoothNextTriggerMax = 3f;
        [InspectorName("平滑增量最小值")]
        public float smoothIncrementMin = 0f;
        [InspectorName("平滑增量最大值")]
        public float smoothIncrementMax = 0.1f;
        [InspectorName("平滑渐变最小时长(秒)")]
        public float smoothRampDurationMin = 0.25f;
        [InspectorName("平滑渐变最大时长(秒)")]

        public float smoothRampDurationMax = 1.2f;

        [Header("尾端滞留(99%的时候卡住)"), InspectorName("尾端滞留概率")]
        public float stallChance = 0.2f;
        [InspectorName("尾端滞留最小时长(秒)")]
        public float stallMinHold = 0.6f;
        [InspectorName("尾端滞留最大时长(秒)")]
        public float stallMaxHold = 2.5f;
        [InspectorName("尾端滞留触发进度")]
        public float stallTriggerProgress = 0.99f;
        [InspectorName("尾端滞留封顶进度")]
        public float stallCapProgress = 0.999f;
    }












}
