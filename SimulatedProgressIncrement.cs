using UnityEditor;
using UnityEngine;

class SimulatedProgressIncrement
{
    float nextTriggerTime;
    float increment;

    // 平滑输出参数
    float rampDuration;   
    float emitted; 
    bool isRamping;       

    public SimulatedProgressIncrement()
    {
        ResetSchedule();
    }

    void ResetSchedule()
    {
        float now = (float)EditorApplication.timeSinceStartup;
        nextTriggerTime = now + Random.Range(0f, 3f);
        increment = Random.Range(0f, 0.08f);
        rampDuration = Random.Range(0.25f, 1.2f);
        emitted = 0f;
        isRamping = false;
    }

    public float TryGetIncrement()
    {
        float now = (float)EditorApplication.timeSinceStartup;
        if (!isRamping)
        {
            if (now >= nextTriggerTime)
            {
                isRamping = true;
                emitted = 0f;
            }
            return 0f;
        }
        float t = Mathf.Clamp01((now - nextTriggerTime) / Mathf.Max(0.001f, rampDuration));
        float target = Mathf.SmoothStep(0f, increment, t);
        float delta = target - emitted;
        emitted = target;
        if (t >= 1f)
        {
            ResetSchedule();
        }

        return delta;
    }
}
