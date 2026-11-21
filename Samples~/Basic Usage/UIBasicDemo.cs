using UnityEngine;

namespace Yvsim.UIFramework.Samples
{
    /// <summary>在 UIManager 完成场景面板注册后打开主面板。</summary>
    public sealed class UIBasicDemo : MonoBehaviour
    {
        private void Start()
        {
            UIManager.Open<UIBasicHome>();
        }
    }
}
