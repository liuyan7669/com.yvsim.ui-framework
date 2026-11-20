using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace Yvsim.UIFramework.Tests
{
    /// <summary>导入基础 UI Sample 后验证可选参数和统一关闭入口的实际行为。</summary>
    public sealed class ImportedSampleLifecycleTests
    {
        private static readonly MethodInfo OpenMethod = typeof(UIView).GetMethod("Open", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo CloseMethod = typeof(UIView).GetMethod("Close", BindingFlags.Instance | BindingFlags.NonPublic);
        private GameObject root;

        [TearDown]
        public void TearDown()
        {
            if (root != null) UnityEngine.Object.DestroyImmediate(root);
        }

        [TestCase("UILoadScene")]
        [TestCase("UIWaiting")]
        [TestCase("UITitlebar")]
        public void Open_WithOptionalNullArguments_UsesSampleDefaultBehavior(string className)
        {
            UIView view = CreateSample(className);
            if (className == "UITitlebar")
            {
                foreach (string field in new[] { "homeButton", "helpButton", "fullButton", "windButton" })
                {
                    var buttonObject = new GameObject(field, typeof(RectTransform), typeof(Button));
                    buttonObject.transform.SetParent(root.transform, false);
                    view.GetType().GetField(field).SetValue(view, buttonObject.GetComponent<Button>());
                }
            }

            Assert.DoesNotThrow(() => OpenMethod.Invoke(view, new object[] { null }));
            Assert.IsTrue(view.gameObject.activeSelf);
            if (className == "UITitlebar")
            {
                Assert.IsTrue(((Button)view.GetType().GetField("homeButton").GetValue(view)).gameObject.activeSelf);
                Assert.IsTrue(((Button)view.GetType().GetField("helpButton").GetValue(view)).gameObject.activeSelf);
            }
        }

        [Test]
        public void Waiting_ClosingThroughFrameworkLifecycle_InvokesCallbackOncePerOpen()
        {
            UIView view = CreateSample("UIWaiting");
            Type argsType = view.GetType().GetNestedType("UIArg");
            UIArgs args = (UIArgs)Activator.CreateInstance(argsType);
            int closeCount = 0;
            argsType.GetField("closeAction").SetValue(args, (Action)(() => closeCount++));

            OpenMethod.Invoke(view, new object[] { args });
            CloseMethod.Invoke(view, null);
            CloseMethod.Invoke(view, null);

            Assert.AreEqual(1, closeCount, "UIManager uses this same UIView.Close lifecycle entry.");
            Assert.IsFalse(view.gameObject.activeSelf);

            OpenMethod.Invoke(view, new object[] { args });
            CloseMethod.Invoke(view, null);
            Assert.AreEqual(2, closeCount);
        }

        private UIView CreateSample(string className)
        {
            Type sampleType = null;
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                sampleType = assembly.GetType(className, false);
                if (sampleType != null) break;
            }
            if (sampleType == null) Assert.Ignore("Import UI Framework Examples before running sample lifecycle tests.");
            Assert.IsTrue(typeof(UIView).IsAssignableFrom(sampleType));
            root = new GameObject("Imported Sample Lifecycle Test", typeof(RectTransform));
            return (UIView)root.AddComponent(sampleType);
        }
    }
}
