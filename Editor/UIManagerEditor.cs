using UnityEditor;
using Yvsim.InspectorGroups.Editor;

namespace Yvsim.UIFramework.Editor
{
    /// <summary>使用 Inspector Groups 绘制字段与条件分组。</summary>
    [CustomEditor(typeof(UIManager))]
    [CanEditMultipleObjects]
    public sealed class UIManagerEditor : InspectorGroupsEditor<UIManager>
    {
    }
}
