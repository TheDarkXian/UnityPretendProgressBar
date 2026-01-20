# PretendProgress 使用说明 / Usage

PretendProgress 是一个 Unity Editor 内的“假进度条”工具，用于在编辑器中模拟耗时任务进度显示。脚本只在 Editor 平台编译运行（见 `PretendProgress.asmdef` 的 includePlatforms=Editor）。
PretendProgress is a fake progress bar tool for the Unity Editor, used to simulate long-running tasks. Scripts only compile/run in the Editor (see `PretendProgress.asmdef` includePlatforms=Editor).

## 快速开始 / Quick Start

1. 在 Project 窗口中创建资源：
   - `Create -> PretendProgress -> SimulateTaskTable`
   - `Create -> PretendProgress -> PretendProgressSo`
2. 打开 `SimulateTaskTable` 资源，在 `textArea` 输入任务配置文本，点击 `HandleTextArea` 生成任务列表。
3. 打开 `PretendProgressSo` 资源，设置运行参数并指定 `simulateTaskTable`。
4. 在 `PretendProgressSo` Inspector 中点击 `StartPretend` 开始显示进度条。

1. Create assets in Project view:
   - `Create -> PretendProgress -> SimulateTaskTable`
   - `Create -> PretendProgress -> PretendProgressSo`
2. Open `SimulateTaskTable`, input task config in `textArea`, click `HandleTextArea`.
3. Open `PretendProgressSo`, configure it and assign `simulateTaskTable`.
4. In `PretendProgressSo` Inspector, click `StartPretend`.

## SimulateTaskTable 配置 / Configuration

`SimulateTaskTable` 用于配置任务列表与运行顺序：
`SimulateTaskTable` configures task list and run order:

- `runType`：任务顺序（Order/Random/Reverse）
- `textArea`：可用简易标记文本批量生成任务
- `simulateTasksTable`：解析后得到的任务列表

- `runType`: task order (Order/Random/Reverse)
- `textArea`: bulk-generate tasks from tagged text
- `simulateTasksTable`: parsed task list

### textArea 格式 / textArea Format

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

- `title`：任务标题
- `taskTime`：该任务时长（仅用于总时长统计/配置）
- `detail`：多行详情，`{P}` 替换为百分比（0-100）
- `detail` 为空时默认 `Processing... {P}%`

- `title`: task title
- `taskTime`: task duration (used for summary/config only)
- `detail`: multi-line detail, `{P}` replaced by percent (0-100)
- default detail is `Processing... {P}%` when empty

点击 `HandleTextArea` 后，会把文本解析到 `simulateTasksTable`。
Click `HandleTextArea` to parse into `simulateTasksTable`.

## PretendProgressBarSo 配置 / Configuration

`PretendProgressBarSo` 控制进度条行为：
`PretendProgressBarSo` controls progress behavior:

- `isBlock`：是否阻塞主线程（true=阻塞）
- `workType`：
  - `Forever`：一直运行直到取消
  - `ByTime`：按 `workTime` 结束
- `workTime`：ByTime 模式时长（秒）
- `simulateTaskTable`：任务表资源引用
- `showCancleBtn`：是否显示取消按钮（隐藏字段，谨慎修改）

- `isBlock`: block main thread (true=blocking)
- `workType`:
  - `Forever`: run until canceled
  - `ByTime`: end by `workTime`
- `workTime`: ByTime duration (seconds)
- `simulateTaskTable`: task table asset
- `showCancleBtn`: show cancel button (hidden field, be careful)

## 运行方式 / How to Run

- Inspector：`PretendProgressSo` 面板点击 `StartPretend`
- 代码调用（Editor 环境）：

- Inspector: click `StartPretend` in `PretendProgressSo`
- Code (Editor only):

```csharp
using PretendProgressBar;

PretendProgressBarHandle.HandleSo(pretendProgressSo);
```

## 脚本一览 / Script List

- `SimulateTaskTable.cs`：任务表与 textArea 解析
- `SimulatedProgressIncrement.cs`：随机/平滑进度增量
- `PretendProgressBarSo.cs`：进度条配置数据
- `PretendProgressBar.cs`：主逻辑（阻塞/非阻塞）
- `Editor/SimulateTaskTableEditor.cs`：任务表按钮
- `Editor/PretendProgressEditor.cs`：StartPretend 按钮入口

- `SimulateTaskTable.cs`: task table and textArea parsing
- `SimulatedProgressIncrement.cs`: random/smoothed increments
- `PretendProgressBarSo.cs`: config data
- `PretendProgressBar.cs`: main logic (blocking/non-blocking)
- `Editor/SimulateTaskTableEditor.cs`: task table button
- `Editor/PretendProgressEditor.cs`: StartPretend button

## 注意事项 / Notes

- `Forever` 且不显示取消按钮时，有 120 秒超时保护，避免卡死 Editor
- `detail` 推荐使用 `{P}` 占位符；兼容 `{0}`
- 仅用于 Editor，不会打包进运行时

- In `Forever` without cancel button, a 120s timeout prevents editor lockup
- `detail` prefers `{P}` placeholder; `{0}` also supported
- Editor-only, not included in runtime build
