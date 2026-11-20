using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Yvsim.UIFramework.Tests
{
    /// <summary>确保序列化验证只排队，不在 Unity 校验阶段修改 Canvas 或 Transform。</summary>
    public sealed class UIValidationTimingTests
    {
        private GameObject host;
        private UIRoot root;
        private Canvas canvas;
        private RectTransform normal;
        private RectTransform popup;
        private readonly List<string> validationWarnings = new List<string>();

        [SetUp]
        public void SetUp()
        {
            validationWarnings.Clear();
            Application.logMessageReceived += CaptureValidationWarning;
            host = new GameObject("UI Validation Timing Root", typeof(RectTransform));
            root = host.AddComponent<UIRoot>();
            var canvasObject = new GameObject("Validation Canvas", typeof(RectTransform), typeof(Canvas));
            canvasObject.transform.SetParent(host.transform, false);
            canvas = canvasObject.GetComponent<Canvas>();
            normal = CreateLayer("Normal");
            popup = CreateLayer("Popup");
            root.Configure(null, canvas, null, normal, null, popup, null, null);
        }

        [TearDown]
        public void TearDown()
        {
            if (host != null) UnityEngine.Object.DestroyImmediate(host);
            Application.logMessageReceived -= CaptureValidationWarning;
            Assert.That(validationWarnings, Is.Empty, string.Join("\n", validationWarnings));
        }

        [UnityTest]
        public IEnumerator RootValidation_DefersCanvasScaleAndRecursiveLayerChanges()
        {
            Vector3 originalScale = new Vector3(2f, 3f, 4f);
            canvas.transform.localScale = originalScale;
            canvas.gameObject.layer = normal.gameObject.layer = popup.gameObject.layer = 0;
            var serialized = new SerializedObject(root);
            SerializedProperty persist = serialized.FindProperty("dontDestroyOnLoad");
            persist.boolValue = !persist.boolValue;
            serialized.ApplyModifiedProperties();

            Assert.That(CountQueued(root), Is.EqualTo(1), "SerializedObject must dispatch the real UIRoot.OnValidate.");
            Assert.That(canvas.transform.localScale, Is.EqualTo(originalScale));
            Assert.That(canvas.gameObject.layer, Is.Zero, "OnValidate must not write GameObject.layer synchronously.");
            Assert.That(normal.gameObject.layer, Is.Zero);
            yield return WaitForEditor(() => canvas.gameObject.layer == LayerMask.NameToLayer("UI")
                && canvas.transform.localScale == Vector3.one);

            int uiLayer = LayerMask.NameToLayer("UI");
            Assert.That(canvas.transform.localScale, Is.EqualTo(Vector3.one));
            Assert.That(canvas.gameObject.layer, Is.EqualTo(uiLayer));
            Assert.That(normal.gameObject.layer, Is.EqualTo(uiLayer));
            Assert.That(popup.gameObject.layer, Is.EqualTo(uiLayer));
        }

        [UnityTest]
        public IEnumerator ViewValidation_DefersReparentingAndPreservesLocalLayout()
        {
            var viewObject = new GameObject("Validation View", typeof(RectTransform));
            viewObject.transform.SetParent(normal, false);
            var view = viewObject.AddComponent<UIValidationTimingProbe>();
            var rect = (RectTransform)view.transform;
            rect.anchoredPosition3D = new Vector3(11f, 17f, 0f);
            rect.sizeDelta = new Vector2(120f, 60f);
            var serialized = new SerializedObject(view);
            serialized.FindProperty("layer").intValue = (int)UILayer.Popup;
            serialized.ApplyModifiedProperties();

            Assert.That(CountQueued(view), Is.EqualTo(1), "SerializedObject must dispatch the real UIView.OnValidate.");
            Assert.That(view.transform.parent, Is.SameAs(normal));
            yield return WaitForEditor(() => view.transform.parent == popup);

            Assert.That(view.transform.parent, Is.SameAs(popup));
            Assert.That(rect.anchoredPosition3D, Is.EqualTo(new Vector3(11f, 17f, 0f)));
            Assert.That(rect.sizeDelta, Is.EqualTo(new Vector2(120f, 60f)));
        }

        [UnityTest]
        public IEnumerator RepeatedValidation_QueuesOnceAndDestructionRemovesPendingCallbacks()
        {
            var viewObject = new GameObject("Pending Validation View", typeof(RectTransform));
            viewObject.transform.SetParent(normal, false);
            var view = viewObject.AddComponent<UIValidationTimingProbe>();
            MethodInfo validateRoot = typeof(UIRoot).GetMethod("OnValidate", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo validateView = typeof(UIView).GetMethod("OnValidate", BindingFlags.Instance | BindingFlags.NonPublic);
            for (int i = 0; i < 3; i++)
            {
                validateRoot.Invoke(root, null);
                validateView.Invoke(view, null);
            }
            Assert.That(CountQueued(root), Is.EqualTo(1));
            Assert.That(CountQueued(view), Is.EqualTo(1));

            UnityEngine.Object.DestroyImmediate(host);
            host = null;
            // 非 ExecuteAlways 组件在 EditMode 中可能未执行 Awake，因此不能要求立即收到 OnDestroy。
            // 延迟入口仍必须识别已销毁目标并移除自己，不能留下重试任务或操作失效引用。
            yield return WaitForEditor(() => CountQueued(root) == 0 && CountQueued(view) == 0);
            Assert.That(CountQueued(root), Is.Zero);
            Assert.That(CountQueued(view), Is.Zero);
        }

        private static IEnumerator WaitForEditor(Func<bool> completed)
        {
            double deadline = EditorApplication.timeSinceStartup + 5d;
            while (!completed() && EditorApplication.timeSinceStartup < deadline)
            {
                EditorApplication.QueuePlayerLoopUpdate();
                yield return null;
            }
        }

        private RectTransform CreateLayer(string name)
        {
            var layer = new GameObject(name, typeof(RectTransform));
            layer.transform.SetParent(canvas.transform, false);
            return layer.GetComponent<RectTransform>();
        }

        private static int CountQueued(UnityEngine.Object target)
        {
            Delegate[] callbacks = EditorApplication.delayCall?.GetInvocationList();
            int count = 0;
            if (callbacks != null)
                foreach (Delegate callback in callbacks)
                    if (ReferenceEquals(callback.Target, target)) count++;
            return count;
        }

        private void CaptureValidationWarning(string message, string stackTrace, LogType type)
        {
            if (message.Contains("SendMessage cannot be called during")) validationWarnings.Add(message);
        }
    }

}
