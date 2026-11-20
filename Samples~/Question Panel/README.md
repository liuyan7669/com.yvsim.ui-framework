# Question Panel

可独立导入的 UIQuestion 题目面板，支持单选、多选、确认、解析、逐题切换和完成回调。

## 前置条件

先安装 Config Loader、Object Utilities，再导入 **UI Framework Examples**（提供本 Prefab 使用的字体）和 TMP Essential Resources。然后在 UI Framework 的 Samples 中导入 **Question Panel**。默认路径为 `Assets/Samples/UI Framework/1.0.0/Question Panel/`。

将 `UIQuestion.prefab` 放入已创建 UI 系统的对应层。使用 `UIManager.Open<UIQuestion>(new UIQuestion.UIArg { questions = yourQuestions })` 打开，其中 `yourQuestions` 是 Config Loader 的 `List<QuestionData>`。面板通过 Object Utilities 的 `PrefabSpawner` 生成题目项。

每次打开会清空本次题目的得分与已选答案。`allowEmptySelection` 和 `skipConfirmation` 可覆盖 Inspector 默认配置；StartAction、SelectionChangedAction、ConfirmAction、NextAction、EndAction 和 AllFinishAction 提供过程回调，具体时机见 UIQuestion.UIArg 的 XML 注释。示例不负责读取题库文件，题目列表由调用方准备。
