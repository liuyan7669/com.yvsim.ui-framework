# UI Framework Examples

基础界面脚本、Prefab 和配套字体，不依赖其他 Yvsim 业务包。题目面板与新手引导分别从独立的 **Question Panel**、**Newbie Guide** Sample 导入。

## 导入与运行

1. 安装 UI Framework 和 Inspector Groups，从 Package Manager 导入 **UI Framework Examples**。默认路径是 `Assets/Samples/UI Framework/1.0.0/UI Framework Examples/`。
2. 通过 `Window > TextMeshPro > Import TMP Essential Resources` 导入 TMP 资源，供示例字体使用。导入整个示例还需 Unity Audio 模块（`com.unity.modules.audio`）；即使不填写 `audioPath`，也需保留该模块以编译示例脚本。
3. 在 Hierarchy 右键选择 `Yvsim > 框架 > 创建 UI 系统`。将需要的 Prefab 放在 UIRoot 对应层级下，UIView 会按其 Layer 自动校正父节点。
4. 在业务启动代码中打开面板并传入对应参数。例如主页使用：

```csharp
UIManager.Open<UIHome>(new UIHome.Arg { startAction = () => Debug.Log("开始") });
UIManager.Open<UIWaiting>(new UIWaiting.UIArg { text = "处理中", duration = 0 });
// 工作完成后关闭等待界面；配置过的 closeAction 会触发一次。
UIManager.Close<UIWaiting>();
```

代码需引用 `Yvsim.UIFramework` 与 `UnityEngine`。示例没有自动运行的主场景，需要由业务入口打开面板。

## 面板与参数

| 面板 | 用途 | 打开参数 |
| --- | --- | --- |
| UIHome | 主页开始回调 | `UIHome.Arg` |
| UIMenu | 菜单点击与按钮状态 | `UIMenu.Arg` |
| UIHelp | 帮助页与关闭回调 | `UIHelp.UIArg` |
| UITitlebar | 标题栏按钮状态与帮助入口 | `UITitlebar.UIArg`，允许省略 |
| UIPrompt | 确认、取消、关闭、提示文本及 Resources 音频 | `UIPrompt.UIArg`，允许省略 |
| UILoadScene | 异步加载、文案与进度百分比 | `UILoadScene.UIArg`；场景须加入 Build Settings |
| UIWaiting | 等待文本、定时或手动关闭 | `UIWaiting.UIArg`，允许省略 |
| UIRBButton | 文本按钮与点击回调 | `UIRBButton.UIArg` |
| UITopTip | 顶部文本及 Resources 音频播放 | `UITopTip.Arg` |

`UITitlebar` 的全屏/窗口按钮只演示状态切换，不会自行修改应用分辨率；Home 按钮的业务跳转由项目自行绑定。`UILoadScene` 会在百分比前显示 `loadText`，为空时只显示百分比。`UIPrompt` 与 `UITopTip` 的 `audioPath` 是 Resources 相对路径（不含扩展名），例如 `Audio/notice`；非空时用本面板独占的 2D AudioSource 播放，重开替换上一段，关闭或停用时停止。路径为空时只显示文字；找不到资源时输出警告并保持面板可用。音频播放不依赖 Audio Player 或其他 Yvsim 业务包。需要参数的面板必须传入表中对应的 UIArgs 子类。

创建的 EventSystem 使用 StandaloneInputModule；Player Settings 的 Active Input Handling 应为 **Input Manager (Old)** 或 **Both**。若使用新输入系统，请自行替换 EventSystem 输入模块。
