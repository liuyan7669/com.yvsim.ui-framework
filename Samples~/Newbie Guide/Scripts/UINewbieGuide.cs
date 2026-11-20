using System;
using Yvsim.UIFramework;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 新手引导：使用四块遮罩围出可交互区域。
/// <para>遮罩区域拦截点击，中间镂空区域将事件透传给下层真实 UI。</para>
/// </summary>
public class UINewbieGuide : UIView
{
    [Header("遮罩")]
    [Tooltip("遮罩布局与目标坐标换算所使用的根 RectTransform；未绑定时使用当前对象的 RectTransform。")]
    public RectTransform guideRoot;
    [Tooltip("镂空区域上方的遮罩 Image，遮挡并拦截该区域的点击。")]
    public Image shadeTop;
    [Tooltip("镂空区域下方的遮罩 Image，遮挡并拦截该区域的点击。")]
    public Image shadeBottom;
    [Tooltip("镂空区域左侧的遮罩 Image，遮挡并拦截该区域的点击。")]
    public Image shadeLeft;
    [Tooltip("镂空区域右侧的遮罩 Image，遮挡并拦截该区域的点击。")]
    public Image shadeRight;

    [Header("引导目标")]
    [Tooltip("当前引导需要露出并允许交互的 UI 区域，可通过引导接口在运行时替换。")]
    public RectTransform target;
    [Tooltip("镂空区域相对目标边界向外扩展的水平、垂直边距，单位为引导根节点的本地 UI 坐标单位。")]
    public Vector2 padding = new Vector2(16f, 16f);

    [Header("前景")]
    [Tooltip("环绕镂空区域的高亮边框；位置和大小随引导目标更新，可不绑定。")]
    public RectTransform highlightFrame;
    [Tooltip("用于播放点按、框选或拖放示意动画的小手 RectTransform，可不绑定。")]
    public RectTransform finger;
    [Tooltip("点按模式下小手相对镂空右下角或指定点击目标中心的偏移，单位为本地 UI 坐标单位。")]
    public Vector2 fingerOffset = new Vector2(20f, -20f);

    [Header("聚焦动画")]
    [Tooltip("遮罩从全区域收拢到目标区域的聚焦动画时长，单位为秒；小于或等于 0 时立即完成。")]
    public float focusDuration = 0.45f;
    [Tooltip("聚焦动画的缓动方式，控制遮罩收拢时的速度变化。")]
    public Ease focusEase = Ease.OutCubic;

    [Header("小手点按动画")]
    [Tooltip("小手按下时相对初始位置的移动偏移，单位为本地 UI 坐标单位。")]
    public Vector2 fingerTapOffset = new Vector2(-8f, 8f);
    [Tooltip("小手点按动画中按下、回弹各阶段的时长，单位为秒。")]
    public float fingerTapDuration = 0.18f;
    [Tooltip("小手按下时相对于初始尺寸的缩放倍率，例如 0.92 表示缩至 92%。")]
    public float fingerTapScale = 0.92f;
    [Tooltip("一次点按完成到下一次点按之间的等待时间，单位为秒。")]
    public float fingerTapInterval = 0.7f;

    [Header("小手框选动画")]
    [Tooltip("框选或拖放模式下小手相对拖拽起点的显示偏移，单位为本地 UI 坐标单位。")]
    public Vector2 fingerDragStartOffset = new Vector2(20f, -28f);
    [Tooltip("未使用精确文本范围时，框选拖拽距离占目标区域宽度的比例；最终距离仍受最小值和最大值限制。")]
    public float fingerDragWidthRatio = 0.65f;
    [Tooltip("按目标宽度计算框选手势时允许的最小拖拽距离，单位为本地 UI 坐标单位。")]
    public float fingerDragMinDistance = 120f;
    [Tooltip("按目标宽度计算框选手势时允许的最大拖拽距离，单位为本地 UI 坐标单位。")]
    public float fingerDragMaxDistance = 360f;
    [Tooltip("框选或拖放时，小手按下和松开各阶段的缩放动画时长，单位为秒。")]
    public float fingerDragPressDuration = 0.12f;
    [Tooltip("小手从拖拽起点移动到终点的动画时长，单位为秒。")]
    public float fingerDragDuration = 0.75f;
    [Tooltip("框选或拖放过程中小手相对于初始尺寸的按下缩放倍率。")]
    public float fingerDragScale = 0.92f;
    [Tooltip("每组框选示意中重复拖拽的次数，实际至少执行 1 次；组结束后可继续演示点击目标。")]
    public int fingerDragRepeatCount = 3;
    [Tooltip("重复框选之间及小手回到起点后的等待时间，单位为秒。")]
    public float fingerDragRepeatInterval = 0.2f;
    [Tooltip("小手回到拖拽起点或移向后续点击目标的移动时长，单位为秒。")]
    public float fingerRepositionDuration = 0.3f;
    [Tooltip("框选结束后点击指定目标时，按下和松开各阶段的缩放时长，单位为秒。")]
    public float fingerOptionTapDuration = 0.18f;
    [Tooltip("一组框选或一次拖放示意结束后、返回起点前的等待时间，单位为秒。")]
    public float fingerDragInterval = 0.65f;

    /// <summary>
    /// 点击黑色不可交互区域时触发；不会拦截镂空区域后方的 UI。
    /// </summary>
    public Action OnBlockedAreaClicked;

    /// <summary>
    /// 点击镂空可交互区域时触发；后方 UI 会先正常处理本次操作。
    /// </summary>
    public Action OnHoleAreaClicked;

    private enum FingerAnimationMode
    {
        Tap,
        DragSelection,
        DragAndDrop
    }

    private enum PendingClickArea
    {
        None,
        Blocked,
        Hole
    }

    private readonly Vector3[] _worldCorners = new Vector3[4];
    private Tween _focusTween;
    private Sequence _fingerSequence;
    private Image _animationBlocker;
    private RectTransform _fingerGestureTarget;
    private RectTransform _fingerClickTarget;
    private TMP_Text _fingerGestureText;
    private string _fingerGestureContent;
    private Vector2 _fingerAnimationOffset;
    private Vector3 _fingerBaseScale = Vector3.one;
    private FingerAnimationMode _fingerAnimationMode;
    private PendingClickArea _pendingClickArea;
    private bool _hasFingerBaseScale;
    private bool _isAnimating;

    /// <summary>
    /// 将指定 UI 区域设置为唯一可交互区域。
    /// </summary>
    /// <param name="targetRect">允许点击或拖拽的目标 UI。</param>
    public void BeginTapGuide(RectTransform targetRect)
    {
        if (targetRect == null)
        {
            Debug.LogError("[UINewbieGuide] BeginTapGuide 失败：targetRect 不能为空。");
            return;
        }

        _fingerAnimationMode = FingerAnimationMode.Tap;
        _fingerGestureTarget = null;
        _fingerClickTarget = null;
        _fingerGestureText = null;
        _fingerGestureContent = null;
        SetTarget(targetRect);
        PlayFocusAnimation();
    }

    /// <summary>
    /// 将指定 UI 区域设为唯一可交互区域，并用小手在指定点击区域循环点按。
    /// </summary>
    /// <param name="targetRect">允许点击的完整镂空区域。</param>
    /// <param name="clickTarget">小手循环点按的目标区域。</param>
    public void BeginClickGuide(RectTransform targetRect, RectTransform clickTarget)
    {
        if (targetRect == null || clickTarget == null)
        {
            Debug.LogError("[UINewbieGuide] BeginClickGuide 失败：镂空区域和点击区域不能为空。");
            return;
        }

        _fingerAnimationMode = FingerAnimationMode.Tap;
        _fingerGestureTarget = null;
        _fingerClickTarget = clickTarget;
        _fingerGestureText = null;
        _fingerGestureContent = null;
        SetTarget(targetRect);
        PlayFocusAnimation();
    }

    /// <summary>
    /// 将指定 UI 区域设为唯一可交互区域，并用小手循环演示从拖拽项移动到放置槽。
    /// </summary>
    /// <param name="targetRect">允许拖拽操作的完整镂空区域。</param>
    /// <param name="dragTarget">小手按下的拖拽项。</param>
    /// <param name="dropTarget">小手移动并松开的放置槽。</param>
    public void BeginDragAndDropGuide(
        RectTransform targetRect,
        RectTransform dragTarget,
        RectTransform dropTarget)
    {
        if (targetRect == null || dragTarget == null || dropTarget == null)
        {
            Debug.LogError("[UINewbieGuide] BeginDragAndDropGuide 失败：镂空区域、拖拽项和放置槽不能为空。");
            return;
        }

        _fingerAnimationMode = FingerAnimationMode.DragAndDrop;
        _fingerGestureTarget = dragTarget;
        _fingerClickTarget = dropTarget;
        _fingerGestureText = null;
        _fingerGestureContent = null;
        SetTarget(targetRect);
        PlayFocusAnimation();
    }

    /// <summary>
    /// 将指定 UI 区域设为唯一可交互区域，并用小手演示拖拽框选。
    /// </summary>
    /// <param name="targetRect">允许框选文字和点击按钮的完整区域。</param>
    /// <param name="selectionRect">小手横向拖拽演示所对应的文字区域。</param>
    public void BeginDragSelectionGuide(RectTransform targetRect, RectTransform selectionRect)
    {
        if (targetRect == null || selectionRect == null)
        {
            Debug.LogError("[UINewbieGuide] BeginDragSelectionGuide 失败：目标区域和文字区域不能为空。");
            return;
        }

        _fingerAnimationMode = FingerAnimationMode.DragSelection;
        _fingerGestureTarget = selectionRect;
        _fingerClickTarget = null;
        _fingerGestureText = null;
        _fingerGestureContent = null;
        SetTarget(targetRect);
        PlayFocusAnimation();
    }

    /// <summary>
    /// 将指定 UI 区域设为唯一可交互区域，并精确演示框选指定文字。
    /// </summary>
    /// <param name="targetRect">允许框选文字和点击按钮的完整区域。</param>
    /// <param name="selectionText">需要演示框选的 TMP 文字。</param>
    /// <param name="selectionContent">需要框选的具体文字内容。</param>
    public void BeginDragSelectionGuide(
        RectTransform targetRect,
        TMP_Text selectionText,
        string selectionContent)
    {
        if (targetRect == null || selectionText == null || string.IsNullOrEmpty(selectionContent))
        {
            Debug.LogError("[UINewbieGuide] BeginDragSelectionGuide 失败：目标区域、文字组件和框选内容不能为空。");
            return;
        }

        _fingerAnimationMode = FingerAnimationMode.DragSelection;
        _fingerGestureTarget = selectionText.rectTransform;
        _fingerClickTarget = null;
        _fingerGestureText = selectionText;
        _fingerGestureContent = selectionContent;
        SetTarget(targetRect);
        PlayFocusAnimation();
    }

    /// <summary>
    /// 精确演示框选指定文字，重复滑动后在指定选项上模拟点击。
    /// </summary>
    /// <param name="targetRect">允许框选文字和点击按钮的完整区域。</param>
    /// <param name="selectionText">需要演示框选的 TMP 文字。</param>
    /// <param name="selectionContent">需要框选的具体文字内容。</param>
    /// <param name="clickTarget">滑动完成后需要模拟点击的选项区域。</param>
    public void BeginDragSelectionGuide(
        RectTransform targetRect,
        TMP_Text selectionText,
        string selectionContent,
        RectTransform clickTarget)
    {
        if (targetRect == null || selectionText == null ||
            string.IsNullOrEmpty(selectionContent) || clickTarget == null)
        {
            Debug.LogError(
                "[UINewbieGuide] BeginDragSelectionGuide 失败：目标区域、文字组件、框选内容和点击目标不能为空。");
            return;
        }

        _fingerAnimationMode = FingerAnimationMode.DragSelection;
        _fingerGestureTarget = selectionText.rectTransform;
        _fingerClickTarget = clickTarget;
        _fingerGestureText = selectionText;
        _fingerGestureContent = selectionContent;
        SetTarget(targetRect);
        PlayFocusAnimation();
    }

    private void Awake()
    {
        SetRaycastTarget(shadeTop, true);
        SetRaycastTarget(shadeBottom, true);
        SetRaycastTarget(shadeLeft, true);
        SetRaycastTarget(shadeRight, true);

        SetChildrenRaycastTarget(highlightFrame, false);
        SetChildrenRaycastTarget(finger, false);
        CacheFingerBaseScale();
        EnsureAnimationBlocker();
    }

    protected override void OnOpen(UIArgs args)
    {
        OnBlockedAreaClicked = () =>
        {
            Debug.Log("<color=white>点击黑色禁止区域</color>");
        };
        OnHoleAreaClicked = () =>
        {
            Debug.Log("<color=white>点击镂空可交互区域</color>");
            UIManager.Close<UINewbieGuide>();
        };

        _pendingClickArea = PendingClickArea.None;
        KillFocusAnimation();
        KillFingerAnimation(true);
        _fingerAnimationMode = FingerAnimationMode.Tap;
        _fingerGestureTarget = null;
        _fingerClickTarget = null;
        _fingerGestureText = null;
        _fingerGestureContent = null;
        SetAnimationBlockerActive(false);
        SetForegroundActive(false);
        Canvas.ForceUpdateCanvases();
        RefreshLayout();
    }

    protected override void OnClose()
    {
        OnBlockedAreaClicked = null;
        OnHoleAreaClicked = null;
        _pendingClickArea = PendingClickArea.None;
        KillFocusAnimation();
        KillFingerAnimation(true);
        SetAnimationBlockerActive(false);
        SetForegroundActive(false);
    }

    private void OnDisable()
    {
        OnBlockedAreaClicked = null;
        OnHoleAreaClicked = null;
        _pendingClickArea = PendingClickArea.None;
        KillFocusAnimation();
        KillFingerAnimation(true);
        SetAnimationBlockerActive(false);
        SetForegroundActive(false);
    }

    private void Update()
    {
        if (_isAnimating || target == null)
        {
            return;
        }

        if (Input.touchCount > 0)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);
                if (touch.phase == TouchPhase.Began)
                {
                    RecordClickArea(touch.position);
                    break;
                }
            }
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            RecordClickArea(Input.mousePosition);
        }
    }

    private void LateUpdate()
    {
        if (!_isAnimating)
        {
            RefreshLayout();
        }

        PendingClickArea clickArea = _pendingClickArea;
        _pendingClickArea = PendingClickArea.None;

        if (clickArea == PendingClickArea.Hole)
        {
            OnHoleAreaClicked?.Invoke();
        }
        else if (clickArea == PendingClickArea.Blocked)
        {
            OnBlockedAreaClicked?.Invoke();
        }
    }

    /// <summary>
    /// 设置当前允许交互的目标 UI。
    /// </summary>
    public void SetTarget(RectTransform targetRect)
    {
        target = targetRect;
        Canvas.ForceUpdateCanvases();
        RefreshLayout();
    }

    /// <summary>
    /// 播放遮罩从屏幕外向目标区域聚焦的进入动画。
    /// </summary>
    private void PlayFocusAnimation()
    {
        RectTransform root = guideRoot != null ? guideRoot : transform as RectTransform;
        if (root == null || shadeTop == null || shadeBottom == null ||
            shadeLeft == null || shadeRight == null)
        {
            return;
        }

        KillFocusAnimation();
        KillFingerAnimation(true);
        Canvas.ForceUpdateCanvases();

        Rect targetHole;
        if (!TryGetTargetHole(root, out targetHole))
        {
            return;
        }

        Rect bounds = root.rect;
        ApplyHoleLayout(root, bounds, bounds);

        SetForegroundActive(false);
        SetAnimationBlockerActive(true);

        if (focusDuration <= 0f)
        {
            ApplyHoleLayout(root, bounds, targetHole);
            OnFocusAnimationComplete();
            return;
        }

        _isAnimating = true;
        _focusTween = DOTween.To(
                () => 0f,
                progress =>
                {
                    Rect currentHole = Rect.MinMaxRect(
                        Mathf.Lerp(bounds.xMin, targetHole.xMin, progress),
                        Mathf.Lerp(bounds.yMin, targetHole.yMin, progress),
                        Mathf.Lerp(bounds.xMax, targetHole.xMax, progress),
                        Mathf.Lerp(bounds.yMax, targetHole.yMax, progress));
                    ApplyHoleLayout(root, bounds, currentHole);
                },
                1f,
                focusDuration)
            .SetEase(focusEase)
            .SetUpdate(true)
            .OnComplete(OnFocusAnimationComplete);
    }

    private void OnFocusAnimationComplete()
    {
        _focusTween = null;
        _isAnimating = false;
        SetAnimationBlockerActive(false);
        SetForegroundActive(true);
        RefreshLayout();
        PlayFingerAnimation();
    }

    private void KillFocusAnimation()
    {
        _focusTween?.Kill();
        _focusTween = null;
        _isAnimating = false;
    }

    private void PlayFingerAnimation()
    {
        if (_fingerAnimationMode == FingerAnimationMode.DragAndDrop)
        {
            PlayFingerDragAndDropAnimation();
            return;
        }

        if (_fingerAnimationMode == FingerAnimationMode.DragSelection)
        {
            PlayFingerDragAnimation();
            return;
        }

        PlayFingerTapAnimation();
    }

    private void PlayFingerTapAnimation()
    {
        if (finger == null)
        {
            return;
        }

        KillFingerAnimation(true);
        CacheFingerBaseScale();

        float duration = Mathf.Max(0.01f, fingerTapDuration);
        float scale = Mathf.Max(0.01f, fingerTapScale);

        _fingerSequence = DOTween.Sequence();
        _fingerSequence.Append(DOTween.To(
                () => _fingerAnimationOffset,
                value => _fingerAnimationOffset = value,
                fingerTapOffset,
                duration)
            .SetEase(Ease.InOutSine));
        _fingerSequence.Join(finger.DOScale(_fingerBaseScale * scale, duration)
            .SetEase(Ease.InOutSine));
        _fingerSequence.Append(DOTween.To(
                () => _fingerAnimationOffset,
                value => _fingerAnimationOffset = value,
                Vector2.zero,
                duration)
            .SetEase(Ease.OutBack));
        _fingerSequence.Join(finger.DOScale(_fingerBaseScale, duration)
            .SetEase(Ease.OutBack));
        _fingerSequence.AppendInterval(Mathf.Max(0f, fingerTapInterval));
        _fingerSequence.SetLoops(-1);
        _fingerSequence.SetUpdate(true);
    }

    private void PlayFingerDragAnimation()
    {
        RectTransform root = guideRoot != null ? guideRoot : transform as RectTransform;
        Vector2 dragStart;
        Vector2 dragEnd;
        if (finger == null || root == null || _fingerGestureTarget == null ||
            !TryGetSelectionDragPoints(root, out dragStart, out dragEnd))
        {
            PlayFingerTapAnimation();
            return;
        }

        KillFingerAnimation(true);
        CacheFingerBaseScale();

        Vector2 dragOffset = dragEnd - dragStart;
        Vector2 clickOffset = Vector2.zero;
        Rect clickRect = default;
        bool hasClickTarget = _fingerClickTarget != null &&
                              TryGetRectInRoot(_fingerClickTarget, root, out clickRect);
        if (hasClickTarget)
        {
            clickOffset = clickRect.center - dragStart;
        }

        float pressDuration = Mathf.Max(0.01f, fingerDragPressDuration);
        float dragDuration = Mathf.Max(0.01f, fingerDragDuration);
        float scale = Mathf.Max(0.01f, fingerDragScale);
        float repositionDuration = Mathf.Max(0.01f, fingerRepositionDuration);
        int repeatCount = Mathf.Max(1, fingerDragRepeatCount);

        _fingerSequence = DOTween.Sequence();
        for (int i = 0; i < repeatCount; i++)
        {
            _fingerSequence.Append(finger.DOScale(_fingerBaseScale * scale, pressDuration)
                .SetEase(Ease.InOutSine));
            _fingerSequence.Append(DOTween.To(
                    () => _fingerAnimationOffset,
                    value => _fingerAnimationOffset = value,
                    dragOffset,
                    dragDuration)
                .SetEase(Ease.InOutSine));
            _fingerSequence.Append(finger.DOScale(_fingerBaseScale, pressDuration)
                .SetEase(Ease.OutBack));

            if (i < repeatCount - 1)
            {
                _fingerSequence.Append(DOTween.To(
                        () => _fingerAnimationOffset,
                        value => _fingerAnimationOffset = value,
                        Vector2.zero,
                        repositionDuration)
                    .SetEase(Ease.InOutSine));
                _fingerSequence.AppendInterval(Mathf.Max(0f, fingerDragRepeatInterval));
            }
        }

        if (hasClickTarget)
        {
            Vector2 optionOffset = clickOffset;
            float tapDuration = Mathf.Max(0.01f, fingerOptionTapDuration);
            _fingerSequence.Append(DOTween.To(
                    () => _fingerAnimationOffset,
                    value => _fingerAnimationOffset = value,
                    optionOffset,
                    repositionDuration)
                .SetEase(Ease.InOutSine));
            _fingerSequence.AppendInterval(0.15f);
            _fingerSequence.Append(finger.DOScale(_fingerBaseScale * scale, tapDuration)
                .SetEase(Ease.InOutSine));
            _fingerSequence.Append(finger.DOScale(_fingerBaseScale, tapDuration)
                .SetEase(Ease.OutBack));
        }

        _fingerSequence.AppendInterval(Mathf.Max(0f, fingerDragInterval));
        _fingerSequence.Append(DOTween.To(
                () => _fingerAnimationOffset,
                value => _fingerAnimationOffset = value,
                Vector2.zero,
                repositionDuration)
            .SetEase(Ease.InOutSine));
        _fingerSequence.AppendInterval(Mathf.Max(0f, fingerDragRepeatInterval));
        _fingerSequence.SetLoops(-1, LoopType.Restart);
        _fingerSequence.SetUpdate(true);
    }

    private void PlayFingerDragAndDropAnimation()
    {
        RectTransform root = guideRoot != null ? guideRoot : transform as RectTransform;
        Vector2 dragStart;
        Vector2 dragEnd;
        if (finger == null || root == null ||
            !TryGetDragAndDropPoints(root, out dragStart, out dragEnd))
        {
            PlayFingerTapAnimation();
            return;
        }

        KillFingerAnimation(true);
        CacheFingerBaseScale();

        Vector2 dragOffset = dragEnd - dragStart;
        float pressDuration = Mathf.Max(0.01f, fingerDragPressDuration);
        float dragDuration = Mathf.Max(0.01f, fingerDragDuration);
        float scale = Mathf.Max(0.01f, fingerDragScale);
        float repositionDuration = Mathf.Max(0.01f, fingerRepositionDuration);

        _fingerSequence = DOTween.Sequence();
        _fingerSequence.Append(finger.DOScale(_fingerBaseScale * scale, pressDuration)
            .SetEase(Ease.InOutSine));
        _fingerSequence.Append(DOTween.To(
                () => _fingerAnimationOffset,
                value => _fingerAnimationOffset = value,
                dragOffset,
                dragDuration)
            .SetEase(Ease.InOutSine));
        _fingerSequence.Append(finger.DOScale(_fingerBaseScale, pressDuration)
            .SetEase(Ease.OutBack));
        _fingerSequence.AppendInterval(Mathf.Max(0f, fingerDragInterval));
        _fingerSequence.Append(DOTween.To(
                () => _fingerAnimationOffset,
                value => _fingerAnimationOffset = value,
                Vector2.zero,
                repositionDuration)
            .SetEase(Ease.InOutSine));
        _fingerSequence.AppendInterval(Mathf.Max(0f, fingerDragRepeatInterval));
        _fingerSequence.SetLoops(-1, LoopType.Restart);
        _fingerSequence.SetUpdate(true);
    }

    private void KillFingerAnimation(bool resetVisual)
    {
        _fingerSequence?.Kill();
        _fingerSequence = null;

        if (!resetVisual)
        {
            return;
        }

        _fingerAnimationOffset = Vector2.zero;
        if (finger != null && _hasFingerBaseScale)
        {
            finger.localScale = _fingerBaseScale;
        }

        if (finger != null)
        {
            finger.gameObject.SetActive(true);
        }
    }

    private void CacheFingerBaseScale()
    {
        if (finger == null || _hasFingerBaseScale)
        {
            return;
        }

        _fingerBaseScale = finger.localScale;
        _hasFingerBaseScale = true;
    }

    private void EnsureAnimationBlocker()
    {
        RectTransform root = guideRoot != null ? guideRoot : transform as RectTransform;
        if (root == null) return;

        Transform existing = root.Find("AnimationBlocker");
        if (existing != null)
        {
            _animationBlocker = existing.GetComponent<Image>();
        }

        if (_animationBlocker == null)
        {
            GameObject blockerObject = new GameObject(
                "AnimationBlocker",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            blockerObject.layer = root.gameObject.layer;

            RectTransform blockerRect = blockerObject.GetComponent<RectTransform>();
            blockerRect.SetParent(root, false);
            blockerRect.anchorMin = Vector2.zero;
            blockerRect.anchorMax = Vector2.one;
            blockerRect.offsetMin = Vector2.zero;
            blockerRect.offsetMax = Vector2.zero;

            _animationBlocker = blockerObject.GetComponent<Image>();
        }

        _animationBlocker.color = Color.clear;
        _animationBlocker.raycastTarget = true;
        _animationBlocker.transform.SetAsFirstSibling();
        _animationBlocker.gameObject.SetActive(false);
    }

    private void SetAnimationBlockerActive(bool active)
    {
        if (_animationBlocker == null)
        {
            EnsureAnimationBlocker();
        }

        if (_animationBlocker != null)
        {
            _animationBlocker.gameObject.SetActive(active);
        }
    }

    private void SetForegroundActive(bool active)
    {
        if (highlightFrame != null)
        {
            highlightFrame.gameObject.SetActive(active);
        }

        if (finger != null)
        {
            finger.gameObject.SetActive(active);
        }
    }

    /// <summary>
    /// 刷新四块遮罩、高亮框和手指的位置。
    /// </summary>
    public void RefreshLayout()
    {
        RectTransform root = guideRoot != null ? guideRoot : transform as RectTransform;
        if (root == null || target == null ||
            shadeTop == null || shadeBottom == null || shadeLeft == null || shadeRight == null)
        {
            return;
        }

        Rect targetHole;
        if (!TryGetTargetHole(root, out targetHole))
        {
            return;
        }

        ApplyHoleLayout(root, root.rect, targetHole);
    }

    private bool TryGetTargetHole(RectTransform root, out Rect targetHole)
    {
        targetHole = default;
        if (target == null)
        {
            return false;
        }

        Rect targetRect;
        if (!TryGetRectInRoot(target, root, out targetRect))
        {
            return false;
        }

        Rect bounds = root.rect;
        targetHole = Rect.MinMaxRect(
            Mathf.Clamp(targetRect.xMin - padding.x, bounds.xMin, bounds.xMax),
            Mathf.Clamp(targetRect.yMin - padding.y, bounds.yMin, bounds.yMax),
            Mathf.Clamp(targetRect.xMax + padding.x, bounds.xMin, bounds.xMax),
            Mathf.Clamp(targetRect.yMax + padding.y, bounds.yMin, bounds.yMax));
        return true;
    }

    private bool TryGetRectInRoot(RectTransform source, RectTransform root, out Rect localRect)
    {
        localRect = default;
        if (source == null || root == null)
        {
            return false;
        }

        Canvas targetCanvas = source.GetComponentInParent<Canvas>();
        Canvas rootCanvas = root.GetComponentInParent<Canvas>();
        Camera targetCamera = GetEventCamera(targetCanvas);
        Camera rootCamera = GetEventCamera(rootCanvas);

        source.GetWorldCorners(_worldCorners);

        float minX = float.MaxValue;
        float minY = float.MaxValue;
        float maxX = float.MinValue;
        float maxY = float.MinValue;

        for (int i = 0; i < _worldCorners.Length; i++)
        {
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(targetCamera, _worldCorners[i]);
            Vector2 localPoint;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screenPoint, rootCamera, out localPoint))
            {
                return false;
            }

            minX = Mathf.Min(minX, localPoint.x);
            minY = Mathf.Min(minY, localPoint.y);
            maxX = Mathf.Max(maxX, localPoint.x);
            maxY = Mathf.Max(maxY, localPoint.y);
        }

        localRect = Rect.MinMaxRect(minX, minY, maxX, maxY);
        return true;
    }

    private bool TryGetSelectionDragPoints(
        RectTransform root,
        out Vector2 dragStart,
        out Vector2 dragEnd)
    {
        dragStart = default;
        dragEnd = default;

        if (_fingerGestureText != null && !string.IsNullOrEmpty(_fingerGestureContent))
        {
            _fingerGestureText.ForceMeshUpdate();
            int startIndex = _fingerGestureText.text.IndexOf(
                _fingerGestureContent,
                StringComparison.Ordinal);
            int endIndex = startIndex + _fingerGestureContent.Length;

            if (startIndex >= 0 && endIndex <= _fingerGestureText.textInfo.characterCount)
            {
                float minX = float.MaxValue;
                float minY = float.MaxValue;
                float maxX = float.MinValue;
                float maxY = float.MinValue;
                bool hasVisibleCharacter = false;

                for (int i = startIndex; i < endIndex; i++)
                {
                    TMP_CharacterInfo character = _fingerGestureText.textInfo.characterInfo[i];
                    if (!character.isVisible)
                    {
                        continue;
                    }

                    minX = Mathf.Min(minX, character.bottomLeft.x);
                    minY = Mathf.Min(minY, character.bottomLeft.y);
                    maxX = Mathf.Max(maxX, character.topRight.x);
                    maxY = Mathf.Max(maxY, character.topRight.y);
                    hasVisibleCharacter = true;
                }

                if (hasVisibleCharacter)
                {
                    float centerY = (minY + maxY) * 0.5f;
                    Vector3 startWorld = _fingerGestureText.rectTransform.TransformPoint(
                        new Vector3(minX, centerY, 0f));
                    Vector3 endWorld = _fingerGestureText.rectTransform.TransformPoint(
                        new Vector3(maxX, centerY, 0f));

                    if (TryWorldPointToRootLocal(
                            _fingerGestureText.rectTransform,
                            root,
                            startWorld,
                            out dragStart) &&
                        TryWorldPointToRootLocal(
                            _fingerGestureText.rectTransform,
                            root,
                            endWorld,
                            out dragEnd))
                    {
                        return true;
                    }
                }
            }
        }

        Rect gestureRect;
        if (!TryGetRectInRoot(_fingerGestureTarget, root, out gestureRect))
        {
            return false;
        }

        float minDistance = Mathf.Max(0f, fingerDragMinDistance);
        float maxDistance = Mathf.Max(minDistance, fingerDragMaxDistance);
        float dragDistance = Mathf.Clamp(
            gestureRect.width * Mathf.Max(0f, fingerDragWidthRatio),
            minDistance,
            maxDistance);
        dragEnd = new Vector2(gestureRect.xMax, gestureRect.center.y);
        dragStart = dragEnd + new Vector2(-dragDistance, 0f);
        return true;
    }

    private bool TryGetDragAndDropPoints(
        RectTransform root,
        out Vector2 dragStart,
        out Vector2 dragEnd)
    {
        dragStart = default;
        dragEnd = default;

        Rect dragRect;
        Rect dropRect;
        if (!TryGetRectInRoot(_fingerGestureTarget, root, out dragRect) ||
            !TryGetRectInRoot(_fingerClickTarget, root, out dropRect))
        {
            return false;
        }

        dragStart = dragRect.center;
        dragEnd = dropRect.center;
        return true;
    }

    private static bool TryWorldPointToRootLocal(
        RectTransform source,
        RectTransform root,
        Vector3 worldPoint,
        out Vector2 localPoint)
    {
        Canvas sourceCanvas = source.GetComponentInParent<Canvas>();
        Canvas rootCanvas = root.GetComponentInParent<Canvas>();
        Camera sourceCamera = GetEventCamera(sourceCanvas);
        Camera rootCamera = GetEventCamera(rootCanvas);
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(sourceCamera, worldPoint);
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            root,
            screenPoint,
            rootCamera,
            out localPoint);
    }

    private void ApplyHoleLayout(RectTransform root, Rect bounds, Rect hole)
    {
        SetRect(shadeTop.rectTransform, bounds.xMin, hole.yMax, bounds.xMax, bounds.yMax);
        SetRect(shadeBottom.rectTransform, bounds.xMin, bounds.yMin, bounds.xMax, hole.yMin);
        SetRect(shadeLeft.rectTransform, bounds.xMin, hole.yMin, hole.xMin, hole.yMax);
        SetRect(shadeRight.rectTransform, hole.xMax, hole.yMin, bounds.xMax, hole.yMax);

        if (highlightFrame != null)
        {
            SetRect(highlightFrame, hole.xMin, hole.yMin, hole.xMax, hole.yMax);
        }

        if (finger != null)
        {
            finger.anchorMin = new Vector2(0.5f, 0.5f);
            finger.anchorMax = new Vector2(0.5f, 0.5f);

            Vector2 fingerPosition = new Vector2(hole.xMax, hole.yMin) + fingerOffset;
            Vector2 dragStart;
            Vector2 dragEnd;
            Rect clickRect;
            if (_fingerAnimationMode == FingerAnimationMode.Tap &&
                _fingerClickTarget != null &&
                TryGetRectInRoot(_fingerClickTarget, root, out clickRect))
            {
                fingerPosition = clickRect.center + fingerOffset;
            }
            else if (_fingerAnimationMode == FingerAnimationMode.DragSelection &&
                TryGetSelectionDragPoints(root, out dragStart, out dragEnd))
            {
                fingerPosition = dragStart + fingerDragStartOffset;
            }
            else if (_fingerAnimationMode == FingerAnimationMode.DragAndDrop &&
                     TryGetDragAndDropPoints(root, out dragStart, out dragEnd))
            {
                fingerPosition = dragStart + fingerDragStartOffset;
            }

            finger.anchoredPosition = fingerPosition + _fingerAnimationOffset;
        }
    }

    private static Camera GetEventCamera(Canvas canvas)
    {
        if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            return null;
        }

        return canvas.worldCamera;
    }

    private static void SetRect(RectTransform rectTransform, float minX, float minY, float maxX, float maxY)
    {
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = new Vector2((minX + maxX) * 0.5f, (minY + maxY) * 0.5f);
        rectTransform.sizeDelta = new Vector2(
            Mathf.Max(0f, maxX - minX),
            Mathf.Max(0f, maxY - minY));
    }

    private static void SetRaycastTarget(Graphic graphic, bool raycastTarget)
    {
        if (graphic != null)
        {
            graphic.raycastTarget = raycastTarget;
        }
    }

    private void RecordClickArea(Vector2 screenPoint)
    {
        RectTransform root = guideRoot != null ? guideRoot : transform as RectTransform;
        if (root == null)
        {
            return;
        }

        Canvas rootCanvas = root.GetComponentInParent<Canvas>();
        Camera rootCamera = GetEventCamera(rootCanvas);
        Vector2 localPoint;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                root,
                screenPoint,
                rootCamera,
                out localPoint) ||
            !root.rect.Contains(localPoint))
        {
            return;
        }

        Rect hole;
        if (!TryGetTargetHole(root, out hole))
        {
            return;
        }

        _pendingClickArea = hole.Contains(localPoint)
            ? PendingClickArea.Hole
            : PendingClickArea.Blocked;
    }

    private static void SetChildrenRaycastTarget(RectTransform root, bool raycastTarget)
    {
        if (root == null) return;

        Graphic[] graphics = root.GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
        {
            graphics[i].raycastTarget = raycastTarget;
        }
    }
}
