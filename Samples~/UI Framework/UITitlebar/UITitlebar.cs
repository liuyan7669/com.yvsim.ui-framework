using System.Collections;
using System.Collections.Generic;
using Yvsim.UIFramework;
using UnityEngine;
using UnityEngine.UI;

public class UITitlebar : UIView
{
    public class UIArg : UIArgs
    {
        public bool homeActive;
        public bool helpActive;
    }

    [Tooltip("标题栏首页按钮；当前示例只控制其显示，点击行为需要按项目补充。")]
    public Button homeButton;
    [Tooltip("打开帮助面板的按钮，可由打开参数控制是否显示。")]
    public Button helpButton;
    [Tooltip("将 IsFullScreen 设为 true 并切换按钮显示的按钮；实际窗口全屏逻辑需由项目接入。")]
    public Button fullButton;
    [Tooltip("将 IsFullScreen 设为 false 并切换按钮显示的按钮；实际窗口模式切换需由项目接入。")]
    public Button windButton;
    [Tooltip("决定显示全屏按钮还是窗口按钮的界面状态；该示例不会据此修改屏幕分辨率或窗口模式。")]
    public bool IsFullScreen;

    protected override void OnOpen(UIArgs args)
    {
        UIArg arg = args?.As<UIArg>();
        if (arg != null)
        {
            homeButton.gameObject.SetActive(arg.homeActive);
            helpButton.gameObject.SetActive(arg.helpActive);
        }
        else
        {
            homeButton.gameObject.SetActive(true);
            helpButton.gameObject.SetActive(true);
        }

        homeButton.onClick.RemoveAllListeners();
        helpButton.onClick.RemoveAllListeners();
        fullButton.onClick.RemoveAllListeners();
        windButton.onClick.RemoveAllListeners();

        RefreshFullStyle();

        homeButton.onClick.AddListener(() =>
        {

        });
        helpButton.onClick.AddListener(() =>
        {
            UIManager.Open<UIHelp>(new UIHelp.UIArg()
            {
                action = () =>
                {
                    UIManager.Close<UIHelp>();
                }
            });
        });
        fullButton.onClick.AddListener(() =>
        {
            IsFullScreen = true;
            RefreshFullStyle();
        });
        windButton.onClick.AddListener(() =>
        {
            IsFullScreen = false;
            RefreshFullStyle();
        });
    }

    private void RefreshFullStyle()
    {
        fullButton.gameObject.SetActive(!IsFullScreen);
        windButton.gameObject.SetActive(IsFullScreen);
    }

}
