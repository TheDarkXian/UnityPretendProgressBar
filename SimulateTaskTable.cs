using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "SimulateTaskTable", menuName = "PretendProgress/SimulateTaskTable", order = 1)]
public class SimulateTaskTable : ScriptableObject
{
    public List<SimulateTask> simulateTasksTable;
    public enum SimulateRunType
    {
        Order,
        Random,
        Reverse
    }
    public SimulateRunType runType;
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