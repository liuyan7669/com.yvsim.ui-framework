using UnityEngine;
using UnityEngine.UI;

namespace Yvsim.UIFramework.Samples
{
    /// <summary>演示从场景面板打开弹窗 Prefab，并在关闭后接收回调。</summary>
    public sealed class UIBasicHome : UIView
    {
        [SerializeField, Tooltip("点击后通过 UIManager 打开弹窗，每次传入新的序号。")]
        private Button openButton;

        [SerializeField, Tooltip("首次打开时实例化到 Popup 层；后续打开复用同一个面板实例。")]
        private UIBasicPopup popupPrefab;

        [SerializeField, Tooltip("显示打开次数和关闭回调结果，使用 Unity 内置 UI 字体。")]
        private Text statusText;

        private int openCount;

        private void Awake()
        {
            openButton.onClick.AddListener(() =>
            {
                openCount++;
                statusText.text = $"Opened #{openCount}";
                UIManager.Open(popupPrefab, new UIBasicPopup.Args
                {
                    Message = $"Hello from UIArgs!\nOpen count: {openCount}",
                    Closed = () => statusText.text = $"Closed #{openCount}. Open again to reuse the panel."
                });
            });
        }

        /// <summary>每次由框架打开主面板时刷新说明。</summary>
        protected override void OnOpen(UIArgs args)
        {
            statusText.text = "Click Open Popup to begin.";
        }
    }
}
