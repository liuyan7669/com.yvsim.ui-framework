# Basic Usage

只演示 UI Framework 自身用法，不包含字体文件、TMP 资源或其他业务包集成。

## 运行

1. 安装 UI Framework 及其声明的 Unity UGUI、Inspector Groups 依赖。
2. 在 Package Manager 的 Samples 中导入 Basic Usage。
3. 打开导入目录内的 BasicUsage.unity，进入 Play Mode。
4. 点击 Open Popup，弹窗显示 UIArgs 传入的文字和打开次数。
5. 点击 Close，主面板显示关闭回调结果；再次打开会显示新次数，并复用同一个弹窗实例。

场景和 Prefab 已绑定引用，无需导入 TMP Essential Resources。界面使用英文和 Unity 内置 UGUI Text 字体，中文说明在脚本和 Inspector Tooltip 中。

## 阅读顺序

- UIBasicDemo.cs：在 Start 中打开场景内已注册的主面板。
- UIBasicHome.cs：按钮调用 UIManager.Open(prefab, args)，展示首次创建和后续复用。
- UIBasicPopup.cs：继承 UIView，在 OnOpen 读取参数，在 OnClose 通知调用者。
- BasicPopup.prefab：Layer 为 Popup；在首次打开时由 UIManager 实例化到对应层。

UIManager 会在 Awake 扫描 UICanvas 下包括未激活对象在内的 UIView。主面板初始未激活，由入口脚本打开；弹窗只保存在 Prefab 中，不预放进场景。关闭不会销毁实例；如需彻底销毁，可使用 UIManager.Delete<UIBasicPopup>()，下次再用 Prefab 重载创建。

按钮监听只在 Awake 注册一次，反复开关不会重复添加。弹窗还支持不传 UIArgs 打开，此时显示默认说明。

## 验证要点

- 启动后只显示主面板。
- 弹窗遮罩阻止点击下面的主面板。
- Open Popup / Close 连续操作至少三次，次数每次只加一。
- 关闭回调更新主面板；Hierarchy 中始终只有一个弹窗实例。
- Console 无错误；退出播放后不保存运行时生成的弹窗。

本示例未加入资源加载、题库、配置、音频、动画或业务流程，便于直接照着框架 API 编写自己的界面。
