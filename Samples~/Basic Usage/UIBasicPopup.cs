using System;
using UnityEngine;
using UnityEngine.UI;

namespace Yvsim.UIFramework.Samples
{
    /// <summary>演示 UIArgs、OnOpen 和 OnClose；关闭时保留实例供下次复用。</summary>
    public sealed class UIBasicPopup : UIView
    {
        /// <summary>由调用者传入的弹窗文字和关闭回调。</summary>
        public sealed class Args : UIArgs
        {
            /// <summary>本次打开显示的文字。</summary>
            public string Message;

            /// <summary>本次弹窗通过框架关闭时调用。</summary>
            public Action Closed;
        }

        [SerializeField, Tooltip("显示本次 UIArgs 传入的文字；未传参数时显示默认说明。")]
        private Text messageText;

        [SerializeField, Tooltip("通过 UIManager.Close 关闭弹窗，触发 OnClose 并保留实例。")]
        private Button closeButton;

        private Action onClosed;

        private void Awake()
        {
            closeButton.onClick.AddListener(() => UIManager.Close<UIBasicPopup>());
        }

        /// <summary>每次打开都更新文字和回调，包括复用已有实例时。</summary>
        protected override void OnOpen(UIArgs args)
        {
            var data = args as Args;
            messageText.text = data?.Message ?? "Opened without arguments.";
            onClosed = data?.Closed;
        }

        /// <summary>释放本次回调后通知调用者，避免重入时重复通知。</summary>
        protected override void OnClose()
        {
            var callback = onClosed;
            onClosed = null;
            callback?.Invoke();
        }
    }
}
