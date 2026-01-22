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

    // 平滑相关配置
    //两次“开始一次平滑增长”的最小/最大等待时间范围。值越大，触发越稀疏，整体更慢、更随机。
    float nextTriggerMin = 0f;
    float nextTriggerMax = 3f;
    //单次平滑增长的“总增量”范围。值越大，每次增长幅度越大。
    float incrementMin = 0f;
    float incrementMax = 0.1f;
    //一次平滑增长持续的最小/最大时间范围。值越大，单次增长更“拖”，更平滑。
    float rampDurationMin = 0.25f;
    float rampDurationMax = 1.2f;

    // 卡在尾段的配置 触发进度尾端卡住概率
    float stallChance = 0.9f;
    //尾端停止时间范围
    float stallMinHold = 0.6f;
    float stallMaxHold = 2.5f;
    //尾端卡住触发阈值
    float stallTriggerProgress = 0.99f;
    //尾端卡住最大进度
    float stallCapProgress = 0.999f;
    bool stallActive;
    float stallUntilTime;
    bool stallUsedThisCycle;
    float lastProgress;

    public SimulatedProgressIncrement()
    {
        ResetSchedule();
    }

    public void ConfigureEndStall(
        float chance,
        float minHoldSeconds,
        float maxHoldSeconds,
        float triggerProgress01,
        float capProgress01)
    {
        stallChance = Mathf.Clamp01(chance);
        stallMinHold = Mathf.Max(0f, minHoldSeconds);
        stallMaxHold = Mathf.Max(stallMinHold, maxHoldSeconds);
        stallTriggerProgress = Mathf.Clamp01(triggerProgress01);
        stallCapProgress = Mathf.Clamp01(capProgress01);
    }
    public void ConFigureSchedule()
    {
        ConFigureSchedule(
            nextTriggerMin,
            nextTriggerMax,
            incrementMin,
            incrementMax,
            rampDurationMin,
            rampDurationMax);
    }
    public void ConFigureSchedule(
        float nextTriggerMinSeconds,
        float nextTriggerMaxSeconds,
        float incrementMinValue,
        float incrementMaxValue,
        float rampDurationMinSeconds,
        float rampDurationMaxSeconds)
    {
        nextTriggerMin = Mathf.Max(0f, nextTriggerMinSeconds);
        nextTriggerMax = Mathf.Max(nextTriggerMin, nextTriggerMaxSeconds);
        incrementMin = Mathf.Max(0f, incrementMinValue);
        incrementMax = Mathf.Max(incrementMin, incrementMaxValue);
        rampDurationMin = Mathf.Max(0f, rampDurationMinSeconds);
        rampDurationMax = Mathf.Max(rampDurationMin, rampDurationMaxSeconds);
    }
    void ResetSchedule()
    {
        float now = (float)EditorApplication.timeSinceStartup;
        nextTriggerTime = now + Random.Range(nextTriggerMin, nextTriggerMax);
        increment = Random.Range(incrementMin, incrementMax);
        rampDuration = Random.Range(rampDurationMin, rampDurationMax);
        emitted = 0f;
        isRamping = false;
    }

    public float TryGetIncrement(float currentProgress01)
    {
        float now = (float)EditorApplication.timeSinceStartup;
        if (currentProgress01 < lastProgress)
            stallUsedThisCycle = false;
        lastProgress = currentProgress01;

        if (stallActive)
        {
            if (now < stallUntilTime)
                return 0f;
            stallActive = false;
            ResetSchedule();
        }

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

        if (stallChance > 0f && !stallUsedThisCycle)
        {
            float nextProgress = currentProgress01 + delta;
            if (nextProgress >= stallTriggerProgress && Random.value < stallChance)
            {
                stallUsedThisCycle = true;
                stallActive = true;
                stallUntilTime = now + Random.Range(stallMinHold, stallMaxHold);
                float cappedDelta = Mathf.Max(0f, stallCapProgress - currentProgress01);
                if (cappedDelta < delta)
                    return cappedDelta;
            }
        }

        return delta;
    }
}
