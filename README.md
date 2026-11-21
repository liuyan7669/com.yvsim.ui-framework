# UI Framework

基于 Unity UGUI 的轻量界面框架，提供标准 UI Hierarchy、面板分层、场景面板扫描、面板复用和统一静态访问接口。

## 功能

- 一键创建 UIRoot、UICamera、EventSystem、UICanvas 和六个 UI Layer
- 启动时扫描并注册场景中已存在的 UIView
- 根据 UIView.Layer 在编辑器和运行时自动校正面板层级
- 通过 UIManager 静态接口打开、查找、关闭和删除面板
- 支持 Predicate 条件查询、关闭和删除
- 支持批量关闭以及排除指定类型
- 使用 UIArgs 子类传递面板专用参数
- 支持多选 UI 锚点匹配当前矩形四角，并可通过 Undo 撤销

## 安装

当前源码版本为 `1.0.0`，最低 Unity 2021.3。

在 **Window > Package Manager > + > Add package from disk...** 中选择本包根目录的 `package.json`。安装本包前，先用相同方式安装本地 `com.yvsim.inspector-groups` `1.0.0`，或将两者一起作为 embedded package 放入项目 `Packages/` 目录。

也可把完整包目录（含 `.meta`）放入项目 `Packages/com.yvsim.ui-framework`，由 Unity 作为 embedded package 加载。

本包提供本地源码、Git 和 OpenUPM 安装方式；固定版本使用 `1.0.0`，具体步骤见下文。

## 创建 UI 根节点

在 Unity 菜单中选择：

```text
GameObject > Yvsim > 框架 > 创建 UI 系统
```

生成的 Hierarchy：

```text
UIRoot
├── UICamera
├── EventSystem
└── UICanvas
    ├── BackgroundLayer
    ├── NormalLayer
    ├── FixedLayer
    ├── PopupLayer
    ├── GuideLayer
    └── TopLayer
```

选择 `UIRoot` 后，Inspector 会把基础引用和界面层级分区显示，并在每个层级下直接给出中文用途说明，方便判断不同界面应该放在哪一层。

## 锚点匹配四角

在 Hierarchy 中选择一个或多个具有 `RectTransform` 父节点的 UI，然后执行：

```text
Yvsim > UI > 锚点贴合当前矩形
```

也可以通过物体右键菜单执行：

```text
GameObject > Yvsim > UI > 锚点贴合当前矩形
```

两个入口共用同一套批量逻辑。工具会把每个 UI 的 `anchorMin`、`anchorMax` 移动到其当前矩形四角，并将四边偏移归零。处理前后的视觉位置和尺寸保持不变，全部选中物体统一归入一次 Undo；处理中发生异常时会整体回滚。

## 创建面板

场景中已有 UI Framework 层级后，在任意 UGUI 物体上右键选择：

```text
GameObject > Yvsim > UI > 创建面板
```

新面板会铺满父节点，`Image` 颜色为全透明，并且不会自动挂载 `UIView` 脚本。请创建自己的面板脚本并继承 `UIView`，再将它添加到面板物体：

面板根节点脚本继承 `UIView`：

```csharp
using Yvsim.UIFramework;

public sealed class UIHome : UIView
{
    protected override void OnOpen(UIArgs args)
    {
        UIHomeArgs homeArgs = args.As<UIHomeArgs>();
    }

    protected override void OnClose()
    {
    }
}

public sealed class UIHomeArgs : UIArgs
{
    public string UserName;
}
```

## 使用 UIManager

```csharp
using Yvsim.UIFramework;

UIHome home = UIManager.Open<UIHome>(new UIHomeArgs());
UIHome found = UIManager.Find<UIHome>();
UIManager.Close<UIHome>();
UIManager.Delete<UIHome>();
```

条件与批量操作：

```csharp
UIHome[] activeHomes = UIManager.FindAll<UIHome>(view => view.gameObject.activeInHierarchy);
UIManager.Close<UIView>(view => view.gameObject.activeInHierarchy);
UIManager.CloseAll(typeof(UIHome));
```

`UIManager` 以面板的具体运行时类型作为唯一缓存键，同一个具体面板类型只保留一个受管理实例。类型匹配规则如下：

- `Open<T>(args)`、`Find<T>()`、`Close<T>()` 和 `Delete<T>()` 按 `T` 精确匹配已注册面板。
- `Open<T>(prefab, args)` 按 `prefab.GetType()` 查找和复用实例；即使 Prefab 变量声明为 `UIView`，也不会重复创建相同的具体面板。
- 带 `Predicate<T>` 的 `Find`、`FindAll`、`Close` 和 `Delete` 会遍历已注册面板，并包含可转换为 `T` 的派生类型。

因此，需要处理全部面板时应使用条件重载，例如 `UIManager.Close<UIView>(view => true)`；无条件的 `Close<UIView>()` 只会查找精确的 `UIView` 类型，而 `UIView` 本身是抽象类，正常情况下不会注册该键。

## 示例

Package Manager 只提供一个 `Basic Usage` 示例。导入后打开 `BasicUsage.unity` 并进入 Play Mode，无需创建额外物体或绑定引用。

1. `UIBasicDemo.Start` 调用 `UIManager.Open<UIBasicHome>()`，打开已在场景中注册的主面板。
2. 点击 **Open Popup**，通过 `UIManager.Open(popupPrefab, args)` 创建弹窗并传入打开次数。
3. 弹窗位于 `Popup` 层，`OnOpen` 读取 `UIArgs` 并刷新文字。
4. 点击 **Close**，通过 `UIManager.Close<UIBasicPopup>()` 关闭面板，`OnClose` 回调更新主面板状态。
5. 再次打开时复用同一实例，文字显示新的次数；可据此理解关闭与删除的区别。

示例只含场景、弹窗 Prefab、三个脚本、程序集定义和说明。使用 Unity 内置的 UGUI Text 字体，界面为英文以避免额外字库，代码说明和悬停提示为中文。不包含字体文件、TMP 资源、题目、新手引导、音频或其他包集成。详细步骤见 `Samples~/Basic Usage/README.md`。

## 测试

包内 `Tests/Editor` 提供 EditMode 测试，覆盖 `UIManager` 的面板缓存与类型匹配、`UIView` 生命周期、`UIRoot` 层级配置。示例行为可按 Basic Usage README 的步骤验证。

通过 Package Manager 安装后，如需在 Test Runner 中显示包测试，请在项目的 `Packages/manifest.json` 根对象中添加 `testables` 字段（以下仅示意）：

```json
{
  "testables": [
    "com.yvsim.ui-framework"
  ]
}
```

然后打开 `Window > General > Test Runner`，在 EditMode 页签运行 `Yvsim.UIFramework.Editor.Tests`。

## 环境要求

- Unity 2021.3 或更高版本
- Unity UI（`com.unity.ugui`）
- Inspector Groups（`com.yvsim.inspector-groups`）1.0.0

## 许可证

本包使用 MIT License，详情参见 `LICENSE.md`。

## Inspector 统一依赖

本包依赖 `com.yvsim.inspector-groups` `1.0.0`，只声明字段、状态与业务操作。Inspector 的布局、样式和控件绘制由 Inspector Groups 提供；包内分组使用框架样式，用户新增分组默认使用普通样式。除 Inspector Groups 外，核心包与基础示例均不依赖其他 Yvsim 业务包。

## 从 GitHub 安装

仓库：[com.yvsim.ui-framework](https://github.com/liuyan7669/com.yvsim.ui-framework)。仓库名与 UPM 包名一致。

先通过 Package Manager 的 Add package from git URL 安装以下 Yvsim 依赖，再安装本包：

- `https://github.com/liuyan7669/com.yvsim.inspector-groups.git#main`（1.0.0）

在 Unity Package Manager 选择 Add package from git URL，输入：

```text
https://github.com/liuyan7669/com.yvsim.ui-framework.git#main
```

此地址跟随 main 分支。需要固定 1.0.0 源码时，将 `#main` 改为 `#v1.0.0`；Unity 官方依赖仍按 package.json 解析。

## 从 OpenUPM 安装

在 Unity 的 Project Settings > Package Manager 中添加 Scoped Registry：

- Name：OpenUPM
- URL：`https://package.openupm.com`
- Scope：`com.yvsim`

然后在 Package Manager 中通过 Add package by name 输入 `com.yvsim.ui-framework`，版本填 `1.0.0`。也可从 My Registries 中选择本包。声明的 Yvsim 依赖由 OpenUPM 解析，Unity 官方依赖仍由 Unity 注册表提供。

如果已安装 OpenUPM CLI，可在工程根目录执行：

```sh
openupm add com.yvsim.ui-framework@1.0.0
```

[OpenUPM 包页面](https://openupm.com/packages/com.yvsim.ui-framework/)
