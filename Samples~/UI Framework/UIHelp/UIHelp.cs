using System;
using System.Collections;
using System.Collections.Generic;
using Yvsim.UIFramework;
using UnityEngine;
using UnityEngine.UI;

public class UIHelp : UIView
{
    public class UIArg : UIArgs
    {
        public bool backgroundActive;
        public Action action;
    }
    [Tooltip("帮助界面的确认按钮，点击时调用打开面板参数中传入的 action。")]
    public Button okButton;

    protected override void OnOpen(UIArgs args)
    {
        UIArg arg = args.As<UIArg>();
        SetBackgroundPanelActive(arg.backgroundActive);
        okButton.onClick.RemoveAllListeners();
        okButton.onClick.AddListener(() =>
        {
            arg.action?.Invoke();
        });
    }
}
