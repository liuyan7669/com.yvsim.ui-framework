using System;
using System.Collections;
using System.Collections.Generic;
using Yvsim.UIFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIRBButton : UIView
{
    public class UIArg : UIArgs
    {
        public string buttonText;
        public Action action;
    }
    [Tooltip("右下角操作按钮，点击时调用打开面板参数中传入的 action。")]
    public Button okButton;
    [Tooltip("按钮上的文本组件；打开面板时使用传入文字，未提供时显示“确定”。")]
    public TMP_Text buttonText;

    protected override void OnOpen(UIArgs args)
    {
        UIArg arg = args.As<UIArg>();
        if (string.IsNullOrEmpty(arg.buttonText))
        {
            buttonText.text = "确定";
        }
        else
        {
            buttonText.text = arg.buttonText;
        }
        okButton.onClick.RemoveAllListeners();
        okButton.onClick.AddListener(() =>
        {
            arg.action?.Invoke();
        });
    }

}
