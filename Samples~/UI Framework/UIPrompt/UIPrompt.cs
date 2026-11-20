using System;
using System.Collections;
using System.Collections.Generic;
using Yvsim.UIFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIPrompt : UIView
{
    public class UIArg : UIArgs
    {
        public string text;

        /// <summary>
        /// 要播放的 Resources 音频路径，不包含文件扩展名。
        /// 为空时不播放；重新打开时替换上一段音频，关闭或停用面板时停止播放。
        /// </summary>
        public string audioPath;
        public Action closeAction;
        public Action okAction;
        public Action noAction;
        public bool okActive;
        public bool noActive;
        public bool closeActive;
    }

    [Tooltip("显示打开面板时传入的提示内容的文本组件。")]
    public TMP_Text text;

    [Tooltip("确认按钮，按打开参数决定是否显示；点击后执行确认回调并关闭提示面板。")]
    public Button okButton;
    [Tooltip("否定或取消按钮，按打开参数决定是否显示；点击后执行对应回调并关闭提示面板。")]
    public Button noButton;
    [Tooltip("关闭按钮，按打开参数决定是否显示；点击后执行关闭回调并关闭提示面板。")]
    public Button closeButton;

    private AudioSource resourceAudioSource;

    protected override void OnOpen(UIArgs args)
    {
        UIArg arg = args == null ? null : args.As<UIArg>();
        UISampleAudio.Play(gameObject, ref resourceAudioSource, arg?.audioPath);

        bool hasArgs = arg != null;
        okButton.gameObject.SetActive(hasArgs && arg.okActive);
        noButton.gameObject.SetActive(hasArgs && arg.noActive);
        closeButton.gameObject.SetActive(hasArgs && arg.closeActive);

        okButton.onClick.RemoveAllListeners();
        noButton.onClick.RemoveAllListeners();
        closeButton.onClick.RemoveAllListeners();

        if (!hasArgs)
        {
            text.text = string.Empty;
            return;
        }

        text.text = arg.text;


        okButton.onClick.AddListener(() =>
        {
            arg.okAction?.Invoke();
            UIManager.Close<UIPrompt>();
        });

        noButton.onClick.AddListener(() =>
        {
            arg.noAction?.Invoke();
            UIManager.Close<UIPrompt>();
        });

        closeButton.onClick.AddListener(() =>
        {
            arg.closeAction?.Invoke();
            UIManager.Close<UIPrompt>();
        });
    }

    protected override void OnClose()
    {
        UISampleAudio.Stop(resourceAudioSource);
    }

    private void OnDisable()
    {
        UISampleAudio.Stop(resourceAudioSource);
    }

}
