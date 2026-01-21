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
    public float workTime=15;
    [InspectorName("运行类型")]
    public WorkType workType;
    //public bool Advance=false;
    [InspectorName("模拟任务表")]
    public SimulateTaskTable simulateTaskTable;
    //谨慎配置，否则你只能重启unity了
    [HideInInspector]
    public bool showCancleBtn=true;

}












}