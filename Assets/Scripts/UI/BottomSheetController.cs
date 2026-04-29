using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace GuidanceUI.UI
{
    public enum SnapState { Peek, Mid, Full }

    [RequireComponent(typeof(UIDocument))]
    public class BottomSheetController : MonoBehaviour
    {
        // ── Heights (mobile only) ─────────────────────────────────────────
        private const float PeekHeight  = 72f;
        private const float TopSliver   = 12f;
        private const float MidFraction = 0.45f;

        private float _parentHeight;
        private float MidHeight  => _parentHeight * MidFraction;
        private float FullHeight => _parentHeight - TopSliver;

        // ── Elements ──────────────────────────────────────────────────────
        private VisualElement _root;
        private VisualElement _sheet;
        private VisualElement _handleArea;
        private VisualElement _newJobHandleArea;
        private VisualElement _chatPanel;
        private ScrollView    _scroll;
        private ScrollView    _newJobMessages;
        private Button        _tabChat;
        private Button        _tabJobs;
        private Button        _btnNewJob;
        private VisualElement _vehicleHeaderRow;
        private VisualElement _vehicleHeaderAvatar;
        private Label         _vehicleHeaderName;
        private Button        _btnVFilterClose;

        private VisualElement ActiveHandle => NewJobPageController.IsOpen ? _newJobHandleArea : _handleArea;
        private ScrollView    ActiveScroll => NewJobPageController.IsOpen ? _newJobMessages   : _scroll;

        // ── State ─────────────────────────────────────────────────────────
        private SnapState _state = SnapState.Peek;
        private bool      _ready;
        private bool      _isDesktop;

        // ── Drag (mobile only) ────────────────────────────────────────────
        public static bool IsDragging { get; private set; }
        private bool  _isDragging;
        private float _dragStartY;
        private float _dragStartHeight;

        public static event Action<float, bool> OnHeightChanged;

        // ── Init ──────────────────────────────────────────────────────────

        void Start()
        {
            _root             = GetComponent<UIDocument>().rootVisualElement;
            _sheet            = _root.Q("bottom-sheet");
            _handleArea       = _root.Q("sheet-handle-area");
            _newJobHandleArea = _root.Q("new-job-handle-area");
            _chatPanel        = _root.Q("chat-panel");
            _scroll           = _sheet?.Q<ScrollView>("sheet-scroll");
            _newJobMessages   = _sheet?.Q<ScrollView>("new-job-messages");

            // Chat is the default active tab
            if (_chatPanel != null) _chatPanel.style.display = DisplayStyle.Flex;
            if (_scroll != null)
            {
                _scroll.style.display = DisplayStyle.None;
                _scroll.pickingMode   = PickingMode.Ignore;
            }

            if (_newJobMessages != null)
                _newJobMessages.pickingMode = PickingMode.Ignore;

            if (_sheet == null || _handleArea == null)
            {
                Debug.LogError("[BottomSheet] Could not find 'bottom-sheet' or 'sheet-handle-area' in UXML.");
                return;
            }

            _root.RegisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);
            _root.RegisterCallback<PointerMoveEvent>(OnPointerMove, TrickleDown.TrickleDown);
            _root.RegisterCallback<PointerUpEvent>(OnPointerUp,   TrickleDown.TrickleDown);
            _root.RegisterCallback<PointerCancelEvent>(_ => CancelDrag(), TrickleDown.TrickleDown);

            _tabChat = _root.Q<Button>("btn-tab-chat");
            _tabJobs = _root.Q<Button>("btn-tab-jobs");
            _tabChat?.RegisterCallback<ClickEvent>(_ => SetTab("chat"));
            _tabJobs?.RegisterCallback<ClickEvent>(_ => SetTab("jobs"));

            _btnNewJob           = _root.Q<Button>("btn-new-job");
            _vehicleHeaderRow    = _root.Q("vehicle-header-row");
            _vehicleHeaderAvatar = _root.Q("vehicle-header-avatar");
            _vehicleHeaderName   = _root.Q<Label>("vehicle-header-name");
            _btnVFilterClose     = _root.Q<Button>("btn-vfilter-close");
            _btnVFilterClose?.RegisterCallback<ClickEvent>(_ => ExitVehicleView());

            _isDesktop = ResponsiveManager.Current == Breakpoint.Desktop;
            ResponsiveManager.OnBreakpointChanged    += OnBreakpointChanged;
            NewJobPageController.OnJobCreated        += OnJobCreated;
            VehicleFleetController.OnVehicleSelected += OnVehicleSelected;

            ApplyDesktopLayout();

            _root.RegisterCallback<GeometryChangedEvent>(OnRootLayout);
        }

        void OnDestroy()
        {
            ResponsiveManager.OnBreakpointChanged    -= OnBreakpointChanged;
            NewJobPageController.OnJobCreated        -= OnJobCreated;
            VehicleFleetController.OnVehicleSelected -= OnVehicleSelected;
        }

        private void OnJobCreated() => SetTab("jobs");

        private void OnVehicleSelected(VehicleData vehicle)
        {
            if (vehicle == null)
            {
                ExitVehicleView();
                return;
            }

            // Header avatar — swap colour class
            if (_vehicleHeaderAvatar != null)
            {
                _vehicleHeaderAvatar.RemoveFromClassList("vehicle-avatar--green");
                _vehicleHeaderAvatar.RemoveFromClassList("vehicle-avatar--blue");
                _vehicleHeaderAvatar.RemoveFromClassList("vehicle-avatar--purple");
                _vehicleHeaderAvatar.AddToClassList($"vehicle-avatar--{vehicle.Color}");
            }

            if (_vehicleHeaderName != null) _vehicleHeaderName.text         = vehicle.Name;
            if (_vehicleHeaderRow  != null) _vehicleHeaderRow.style.display  = DisplayStyle.Flex;
            if (_btnNewJob         != null) _btnNewJob.style.display          = DisplayStyle.None;
            ExpandFromPeek();
        }

        private void ExitVehicleView()
        {
            if (_vehicleHeaderRow != null) _vehicleHeaderRow.style.display = DisplayStyle.None;
            if (_btnNewJob        != null) _btnNewJob.style.display         = DisplayStyle.Flex;
            if (VehicleFleetController.SelectedVehicle != null)
                VehicleFleetController.Deselect();
        }

        private void OnBreakpointChanged(Breakpoint bp)
        {
            _isDesktop = bp == Breakpoint.Desktop;
            ApplyDesktopLayout();
            if (!_isDesktop && _ready)
                SetSnap(SnapState.Mid, animate: false);
        }

        // CSS top/bottom anchors don't reliably stretch absolute elements in Unity —
        // compute the height explicitly so the panel always fills to the bottom margin.
        private const float DesktopPanelTop    = 92f;
        private const float DesktopPanelBottom = 20f;

        private void ApplyDesktopLayout()
        {
            if (_scroll == null) return;
            _scroll.pickingMode = _isDesktop ? PickingMode.Position : PickingMode.Ignore;

            if (_isDesktop)
                SetDesktopHeight(_root.resolvedStyle.height);
        }

        private void SetDesktopHeight(float parentHeight)
        {
            if (_sheet == null || parentHeight <= 0) return;
            _sheet.style.height = parentHeight - DesktopPanelTop - DesktopPanelBottom;
        }

        private void OnRootLayout(GeometryChangedEvent _)
        {
            float h = _root.resolvedStyle.height;
            if (h <= 0) return;

            if (_isDesktop)
            {
                SetDesktopHeight(h);
                return;
            }

            if (_ready && Mathf.Approximately(h, _parentHeight)) return;

            _parentHeight = h;
            _ready        = true;
            SetSnap(SnapState.Mid, animate: false);
        }

        // ── Snap (mobile only) ────────────────────────────────────────────

        public void SetSnap(SnapState state, bool animate = true)
        {
            if (_isDesktop) return;

            _state = state;
            float target = HeightFor(state);

            if (animate) _sheet.AddToClassList("sheet-animated");
            else         _sheet.RemoveFromClassList("sheet-animated");

            _sheet.style.height = target;

            if (state == SnapState.Full)
                _sheet.BringToFront();

            var activeScroll = ActiveScroll;
            if (activeScroll != null)
                activeScroll.pickingMode = state == SnapState.Full
                    ? PickingMode.Position
                    : PickingMode.Ignore;

            OnHeightChanged?.Invoke(target, animate);
        }

        private float HeightFor(SnapState s) => s switch
        {
            SnapState.Peek => PeekHeight,
            SnapState.Mid  => MidHeight,
            SnapState.Full => FullHeight,
            _              => MidHeight
        };

        private SnapState Nearest(float h)
        {
            float dPeek = Mathf.Abs(h - PeekHeight);
            float dMid  = Mathf.Abs(h - MidHeight);
            float dFull = Mathf.Abs(h - FullHeight);
            if (dPeek <= dMid && dPeek <= dFull) return SnapState.Peek;
            if (dMid  <= dFull)                  return SnapState.Mid;
            return SnapState.Full;
        }

        // ── Pointer events (mobile only) ──────────────────────────────────

        private void OnPointerDown(PointerDownEvent evt)
        {
            if (!_ready || _isDesktop) return;

            var activeHandle = ActiveHandle;
            var activeScroll = ActiveScroll;

            bool onHandle = activeHandle != null && activeHandle.worldBound.Contains(evt.position);
            bool onSheet  = _sheet.worldBound.Contains(evt.position);

            if (onHandle)
            {
                BeginDrag(evt.position.y);
                evt.StopPropagation();
                return;
            }

            if (onSheet && _state != SnapState.Full)
            {
                BeginDrag(evt.position.y);
                evt.StopPropagation();
                return;
            }

            if (onSheet && _state == SnapState.Full && (activeScroll == null || activeScroll.scrollOffset.y <= 0.5f))
            {
                BeginDrag(evt.position.y);
                evt.StopPropagation();
            }
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (!_isDragging || _isDesktop) return;
            float delta  = _dragStartY - evt.position.y;
            float height = Mathf.Clamp(_dragStartHeight + delta, PeekHeight, FullHeight);
            _sheet.style.height = height;
            OnHeightChanged?.Invoke(height, false);
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            if (!_isDragging) return;
            float current = _sheet.resolvedStyle.height;
            _isDragging = IsDragging = false;
            SetSnap(Nearest(current), animate: true);
        }

        private void CancelDrag()
        {
            if (!_isDragging) return;
            _isDragging = IsDragging = false;
            SetSnap(_state, animate: true);
        }

        private void BeginDrag(float startY)
        {
            if (_isDesktop) return;
            _isDragging = IsDragging = true;
            _dragStartY      = startY;
            _dragStartHeight = _sheet.resolvedStyle.height;
            _sheet.RemoveFromClassList("sheet-animated");
        }

        // ── Buttons ───────────────────────────────────────────────────────

        private void SetTab(string tab)
        {
            bool isChat = tab == "chat";
            _tabChat?.EnableInClassList("sheet-tab--active", isChat);
            _tabJobs?.EnableInClassList("sheet-tab--active", !isChat);

            if (_chatPanel != null)
                _chatPanel.style.display = isChat ? DisplayStyle.Flex : DisplayStyle.None;
            if (_scroll != null)
            {
                _scroll.style.display = isChat ? DisplayStyle.None : DisplayStyle.Flex;
                _scroll.pickingMode   = _isDesktop || (!isChat && _state == SnapState.Full)
                    ? PickingMode.Position : PickingMode.Ignore;
            }

            if (!_isDesktop) ExpandFromPeek();
        }

        public void ExpandFromPeek()
        {
            if (_isDesktop) return;
            if (_state == SnapState.Peek) SetSnap(SnapState.Mid);
        }
    }
}
