using System;
using System.Collections;
using System.Collections.Generic;
using Yvsim.UIFramework;
using UnityEngine;
using UnityEngine.UI;

public class UIHome : UIView
{
    public class Arg : UIArgs
    {
        public Action startAction;
    }

    [Tooltip("首页开始按钮，点击时调用打开面板参数中传入的 startAction。")]
    public Button startButton;

    protected override void OnOpen(UIArgs args)
    {
        Arg arg = args.As<Arg>();
        startButton.onClick.RemoveAllListeners();
        startButton.onClick.AddListener(() =>
        {
            arg.startAction?.Invoke();
        });
    }

}
