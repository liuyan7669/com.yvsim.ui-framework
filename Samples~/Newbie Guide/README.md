# Newbie Guide

基于 UINewbieGuide 的 UI 聚焦、点按、精确文字框选与拖放动画示例。

## 前置条件

安装并初始化 DOTween（包括 UGUI 扩展），导入 UI Framework 的基础示例与 TMP Essential Resources，再导入 **Newbie Guide**。默认路径是 `Assets/Samples/UI Framework/1.0.0/Newbie Guide/`。

将示例 Prefab 放入 UI 系统 Guide 层，目标 RectTransform 必须属于同一 UI 场景。打开 `UIManager.Open<UINewbieGuide>()` 后，调用 `BeginTapGuide(target)`、`BeginClickGuide(target, clickTarget)`、`BeginDragSelectionGuide(...)` 或 `BeginDragAndDropGuide(...)` 开始对应引导。遮罩阻挡外围点击，镂空区域透传下层 UI，OnBlockedAreaClicked 与 OnHoleAreaClicked 分别通知两种点击。

脚本使用 UnityEngine.Input 读取鼠标和触摸，Player Settings 的 Active Input Handling 必须为 **Input Manager (Old)** 或 **Both**。这些可选依赖只属于本 Sample，不属于 UI Framework 核心。
