# PretendProgress（Unity 假进度条）

[English](README.md) | [简体中文](README.zh-CN.md)

PretendProgress 是一个 **Unity 编辑器内的假进度条工具**，用于模拟耗时任务的进度显示。  
脚本仅在 Editor 平台编译和运行（见 `PretendProgress.asmdef` 中的 `includePlatforms=Editor`）。

---

## 快速开始

1. 在 Project 窗口中创建资源：
   - `Create -> PretendProgress -> SimulateTaskTable`
   - `Create -> PretendProgress -> PretendProgressSo`
2. 打开 `SimulateTaskTable`，在 `textArea` 中输入任务配置文本，点击 `HandleTextArea` 生成任务列表。
3. 打开 `PretendProgressSo`，设置运行参数并指定 `simulateTaskTable`。
4. 在 `PretendProgressSo` Inspector 中点击 `StartPretend` 显示进度条。

---

## SimulateTaskTable 配置说明

`SimulateTaskTable` 用于配置任务列表和运行顺序：

- `runType`：任务顺序（`Order` / `Random` / `Reverse`）
- `textArea`：使用标记文本批量生成任务
- `simulateTasksTable`：解析后的任务列表

---

## 注意事项

- `detail` 推荐使用 `{P}` 占位符
- 仅用于 Editor，不会打包进运行时
