using System.Collections;
using System.Collections.Generic;
using Yvsim.UIFramework;
using TMPro;
using UnityEngine;

public class UITopTip : UIView
{
    public class Arg : UIArgs
    {
        public string text;

        /// <summary>
        /// 要播放的 Resources 音频路径，不包含文件扩展名。
        /// 为空时不播放；重新打开时替换上一段音频，关闭或停用面板时停止播放。
        /// </summary>
        public string audioPath;
    }

    [Tooltip("显示打开面板参数中提示内容的顶部文本组件。")]
    public TMP_Text text;

    private AudioSource resourceAudioSource;

    protected override void OnOpen(UIArgs args)
    {
        Arg arg = args.As<Arg>();

        text.text = arg.text;
        UISampleAudio.Play(gameObject, ref resourceAudioSource, arg.audioPath);
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
