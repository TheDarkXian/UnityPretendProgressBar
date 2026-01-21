# PretendProgress Usage

[English](README.md) | [简体中文](README.zh-CN.md)

PretendProgress is a fake progress bar tool for the Unity Editor, used to simulate long-running tasks. Scripts only compile/run in the Editor (see `PretendProgress.asmdef` includePlatforms=Editor).

##  Quick Start

1. Create assets in Project view:
   - `Create -> PretendProgress -> SimulateTaskTable`
   - `Create -> PretendProgress -> PretendProgressSo`
2. Open `SimulateTaskTable`, input task config in `textArea`, click `HandleTextArea`.
3. Open `PretendProgressSo`, configure it and assign `simulateTaskTable`.
4. In `PretendProgressSo` Inspector, click `StartPretend`.

## SimulateTaskTable  / Configuration


`SimulateTaskTable` configures task list and run order:

- `runType`: task order (Order/Random/Reverse)
- `textArea`: bulk-generate tasks from tagged text
- `simulateTasksTable`: parsed task list

### textArea Format

```xml
<Task>
  <title value="Import Assets"/>
  <taskTime value="2.5"/>
  <detail>
    <l>Loading... {P}%</l>
    <l>Processing shaders... {P}%</l>
  </detail>
</Task>
<Task>
  <title value="Build Cache"/>
  <taskTime value="1.0"/>
  <detail>
    <l>Hashing files... {P}%</l>
  </detail>
</Task>
```



- `title`: task title
- `taskTime`: task duration (used for summary/config only)
- `detail`: multi-line detail, `{P}` replaced by percent (0-100)
- default detail is `Processing... {P}%` when empty


Click `HandleTextArea` to parse into `simulateTasksTable`.

## PretendProgressBarSo  Configuration


`PretendProgressBarSo` controls progress behavior:

- `isBlock`: block main thread (true=blocking)
- `workType`:
  - `Forever`: run until canceled
  - `ByTime`: end by `workTime`
  - `hideAfterRun`: hide Inspect panel and project panel `hideAfterRun`
- `workTime`: ByTime duration (seconds)
- `simulateTaskTable`: task table asset , progressbar will show title and detail form this table.
- `showCancleBtn`: show cancel button (hidden field, be careful)

##  How to Run

- Inspector: click `StartPretend` in `PretendProgressSo`,you can create file on mouse context
- Code (Editor only):

```csharp
using PretendProgressBar;

PretendProgressBarHandle.HandleSo(pretendProgressSo);
```

##  Script List

- `SimulateTaskTable.cs`: task table and textArea parsing
- `SimulatedProgressIncrement.cs`: random/smoothed increments
- `PretendProgressBarSo.cs`: config data
- `PretendProgressBar.cs`: main logic (blocking/non-blocking)
- `Editor/SimulateTaskTableEditor.cs`: task table button
- `Editor/PretendProgressEditor.cs`: StartPretend button

## Notes

- In `Forever` without cancel button, a 120s timeout prevents editor lockup
- `detail` prefers `{P}` placeholder; `{0}` also supported
- Editor-only, not included in runtime build
