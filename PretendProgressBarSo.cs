using System.Collections.Generic;
using UnityEngine;
namespace PretendProgressBar
{
    

[CreateAssetMenu(fileName = "PretendProgressSo", menuName = "PretendProgress/PretendProgressSo", order = 1)]
public class PretendProgressBarSo : ScriptableObject
{
    //阻塞主线程
    public bool isBlock;
    public enum WorkType
    {
    //永远执行(直到点击取消)
        Forever,
        ByTime,
    }
    //仅当ByTime的时候出现
    public float workTime=15;
    public WorkType workType;
    //public bool Advance=false;
    public SimulateTaskTable simulateTaskTable;
    //谨慎配置，否则你只能重启unity了
    [HideInInspector]
    public bool showCancleBtn=true;

}












}