# PretendProgress 使用说明

[English](README.md) | [简体中文](README.zh-CN.md)

PretendProgress 是一个用于 **Unity 编辑器** 的“假进度条”工具，用来模拟耗时较长的任务过程。  
所有脚本 **仅在 Editor 中编译和运行**（见 `PretendProgress.asmdef`，`includePlatforms = Editor`）。

---

## 快速开始（Quick Start）

1. 在 Project 面板中创建资源：
   - `Create -> PretendProgress -> SimulateTaskTable`
   - `Create -> PretendProgress -> PretendProgressSo`
2. 打开 `SimulateTaskTable`，在 `textArea` 中输入任务配置文本，然后点击 `HandleTextArea`。
3. 打开 `PretendProgressSo`，进行配置并绑定 `simulateTaskTable`。
4. 在 `PretendProgressSo` 的 Inspector 面板中，点击 `StartPretend` 开始运行。

---

## SimulateTaskTable（任务表配置）

`SimulateTaskTable` 用于配置任务列表及其执行顺序：

- `runType`：任务执行顺序（Order / Random / Reverse）
- `textArea`：通过带标签的文本批量生成任务
- `simulateTasksTable`：解析后的任务列表

### textArea 格式

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

字段说明：

- `title`：任务标题
- `taskTime`：任务耗时（仅用于统计 / 配置参考）
- `detail`：多行进度描述，`{P}` 会被替换为百分比（0–100）
- 当 `detail` 为空时，默认显示 `Processing... {P}%`

点击 `HandleTextArea` 可将文本解析并生成到 `simulateTasksTable`。

---

## PretendProgressBarSo（进度条配置）

`PretendProgressBarSo` 用于控制进度条的整体行为：

- `isBlock`：是否阻塞主线程（true = 阻塞）
- `workType`：
  - `Forever`：一直运行，直到手动取消
  - `ByTime`：按 `workTime` 运行并结束
- `hideAfterRun`：运行前隐藏 Inspector 与 Project 面板中的当前 SO
- `workTime`：ByTime 模式下的持续时间（秒）
- `simulateTaskTable`：任务表资源，进度条标题与详情将从中读取
- `showCancleBtn`：是否显示取消按钮（隐藏字段，请谨慎修改）

---

## 运行方式

- **Inspector**：在 `PretendProgressSo` 中点击 `StartPretend`  
  （也可以通过右键菜单创建并运行）
- **代码调用（仅 Editor）**：

```csharp
using PretendProgressBar;

PretendProgressBarHandle.HandleSo(pretendProgressSo);
```

---

## 脚本列表

- `SimulateTaskTable.cs`：任务表与 textArea 解析逻辑
- `SimulatedProgressIncrement.cs`：随机 / 平滑的进度增长策略
- `PretendProgressBarSo.cs`：配置数据定义
- `PretendProgressBar.cs`：核心逻辑（阻塞 / 非阻塞）
- `Editor/SimulateTaskTableEditor.cs`：任务表 Inspector 按钮
- `Editor/PretendProgressEditor.cs`：StartPretend 按钮

---

## 备注（Notes）

- 在 `Forever` 且未显示取消按钮的情况下，内置 120 秒超时，防止编辑器被锁死
- `detail` 推荐使用 `{P}` 占位符（也支持 `{0}`）
- 仅限 Editor 使用，不会被打入运行时构建
