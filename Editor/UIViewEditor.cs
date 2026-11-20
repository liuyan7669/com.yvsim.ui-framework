using UnityEditor;
using Yvsim.InspectorGroups.Editor;

namespace Yvsim.UIFramework.Editor
{
    [CustomEditor(typeof(UIView), true)]
    [CanEditMultipleObjects]
    public sealed class UIViewEditor : InspectorGroupsEditor<UIView>
    {
    }
}
