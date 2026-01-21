using System.Collections.Generic;
using System.Linq;

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
        bool hasValidTasks;

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
                }
            }

            title = curentTask?.title ?? "Working";
            detail = SafePeekDetail();
        }

        void EnsureDetailQueue(SimulateTask task)
        {
            if (task == null)
            {
                detaillist = new Queue<string>(new[] { "Processing... {P}%" });
                return;
            }
            IEnumerable<string> details = task.detail;
            if (details == null)
            {
                detaillist = new Queue<string>(new[] { "Processing... {P}%" });
                return;
            }
            var arr = details.Where(s => !string.IsNullOrEmpty(s)).ToList();
            if (arr.Count== 0)
                arr = new List<string> { "Processing... {P}%" };
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
                hasValidTasks = false;
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
                        if (i != null)
                            simualateQueue.Enqueue(i);
                    break;
                }
                case SimulateTaskTable.SimulateRunType.Random:
                {
                    var list = table.simulateTasksTable.Where(t => t != null).ToList();
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
                    {
                        var task = table.simulateTasksTable[i];
                        if (task != null)
                            simualateQueue.Enqueue(task);
                    }
                    break;
                }
                default:
                {
                    foreach (var i in table.simulateTasksTable)
                        if (i != null)
                            simualateQueue.Enqueue(i);
                    break;
                }
            }

            hasValidTasks = simualateQueue.Count > 0;
            simualteTableTaskTime = table.simulateTasksTable.Sum(v => v != null ? v.taskTime : 0f);
            if (simualteTableTaskTime <= 0f) simualteTableTaskTime = 1f;

            if (!hasValidTasks)
            {
                simualateQueue.Enqueue(k_FallbackTask);
            }
        }

        public SimulateTask GetSimulateTast()
        {
            if (simualateQueue != null && simualateQueue.TryDequeue(out var re) && re != null)
            {
                return re;
            }

            if (hasValidTasks && simulateTaskTable != null)
            {
                IniTable(simulateTaskTable);
                return GetSimulateTast();
            }

            return k_FallbackTask;
        }
    }


