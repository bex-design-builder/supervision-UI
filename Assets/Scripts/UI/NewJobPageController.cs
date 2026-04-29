using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace GuidanceUI.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class NewJobPageController : MonoBehaviour
    {
        public static event Action OnJobCreated;
        public static bool IsOpen { get; private set; }

        // ── Panel elements ────────────────────────────────────────────────
        private VisualElement         _sheetMain;
        private VisualElement         _newJobPanel;
        private BottomSheetController _sheet;

        // ── Chat elements ─────────────────────────────────────────────────
        private ScrollView    _messages;
        private TextField     _input;
        private Button        _sendBtn;
        private VisualElement _lastMessage;
        private Label         _placeholder;
        private bool          _inputFocused;

        void Start()
        {
            var root     = GetComponent<UIDocument>().rootVisualElement;
            _sheetMain   = root.Q("sheet-main");
            _newJobPanel = root.Q("new-job-panel");
            _sheet       = GetComponent<BottomSheetController>();

            if (_newJobPanel == null)
            {
                Debug.LogError("[NewJobPage] 'new-job-panel' not found in UXML.");
                return;
            }

            // ── Chat wiring ───────────────────────────────────────────────
            _messages = _newJobPanel.Q<ScrollView>("new-job-messages");
            _input    = _newJobPanel.Q<TextField>("new-job-input");
            _sendBtn  = _newJobPanel.Q<Button>("btn-new-job-send");

            var welcome = BuildSystem("Hi there, what would you like to get done?");
            _messages?.Add(welcome);

            var stuck = BuildVehicle("Mark", "‼️ Vehicle stuck", "purple");
            _messages?.Add(stuck);

            var confirmRow = BuildConfirmRow();
            _messages?.Add(confirmRow);
            _lastMessage = confirmRow;

            if (_input != null)
            {
                _input.multiline = true;
                _input.RegisterValueChangedCallback(OnInputChanged);
                _input.RegisterCallback<FocusInEvent>(_ => { _inputFocused = true;  UpdatePlaceholder(); });
                _input.RegisterCallback<FocusOutEvent>(_ => { _inputFocused = false; UpdatePlaceholder(); });
                _input.RegisterCallback<KeyDownEvent>(evt =>
                {
                    if (evt.keyCode != KeyCode.Return && evt.keyCode != KeyCode.KeypadEnter) return;
                    if (evt.shiftKey) return;
                    Send();
                    evt.StopPropagation();
                }, TrickleDown.TrickleDown);

                var inputWrap = _newJobPanel.Q(className: "chat-input-wrap");
                if (inputWrap != null)
                {
                    _placeholder = new Label("What do you want to build?");
                    _placeholder.AddToClassList("chat-placeholder");
                    _placeholder.pickingMode = PickingMode.Ignore;
                    inputWrap.Add(_placeholder);
                }
            }

            _sendBtn?.RegisterCallback<ClickEvent>(_ => Send());

            UpdateSendButton();

            // ── Panel buttons ─────────────────────────────────────────────
            root.Q<Button>("btn-new-job")?.RegisterCallback<ClickEvent>(_ => Open());
            root.Q<Button>("btn-new-job-empty")?.RegisterCallback<ClickEvent>(_ => Open());
            root.Q<Button>("btn-close-new-job")?.RegisterCallback<ClickEvent>(_ => Close());
        }

        // ── Open / Close ──────────────────────────────────────────────────

        public void Open()
        {
            if (_sheetMain   != null) _sheetMain.style.display  = DisplayStyle.None;
            if (_newJobPanel != null) _newJobPanel.style.display = DisplayStyle.Flex;
            IsOpen = true;

            if (ResponsiveManager.Current != Breakpoint.Desktop)
                _sheet?.SetSnap(SnapState.Mid, animate: true);

            _messages?.schedule.Execute(FixBubbleWidths).StartingIn(150);
        }

        public void Close()
        {
            if (_newJobPanel != null) _newJobPanel.style.display = DisplayStyle.None;
            if (_sheetMain   != null) _sheetMain.style.display   = DisplayStyle.Flex;
            IsOpen = false;

            if (ResponsiveManager.Current != Breakpoint.Desktop)
                _sheet?.SetSnap(SnapState.Mid, animate: true);
        }

        // ── Chat ──────────────────────────────────────────────────────────

        private void Send()
        {
            if (_input == null || string.IsNullOrWhiteSpace(_input.value)) return;

            var row = BuildCommand(_input.value);
            _messages?.Add(row);
            _lastMessage = row;
            _input.value = "";
            UpdateSendButton();
            UpdateInputHeight();
            UpdatePlaceholder();
            _input.Focus();

            _messages?.schedule.Execute(FixBubbleWidths).StartingIn(150);
            _messages?.schedule.Execute(ScrollToBottom).StartingIn(200);
        }

        private void OnInputChanged(ChangeEvent<string> _)
        {
            UpdateSendButton();
            UpdateInputHeight();
            UpdatePlaceholder();
        }

        private void UpdateSendButton()
        {
            _sendBtn?.SetEnabled(!string.IsNullOrWhiteSpace(_input?.value));
        }

        private void UpdateInputHeight()
        {
            var inputEl = _input?.Q(className: "unity-base-text-field__input");
            if (inputEl == null) return;
            int lines = (_input.value ?? "").Split('\n').Length;
            float height = Mathf.Clamp(44f + (lines - 1) * 21f, 44f, 120f);
            inputEl.style.height = height;
        }

        private void UpdatePlaceholder()
        {
            if (_placeholder == null) return;
            bool show = string.IsNullOrEmpty(_input?.value) && !_inputFocused;
            _placeholder.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void ScrollToBottom()
        {
            if (_lastMessage != null)
                _messages?.ScrollTo(_lastMessage);
        }

        private void FixBubbleWidths()
        {
            if (_messages == null) return;
            float w = _messages.resolvedStyle.width;
            if (w <= 0) return;
            float maxPx = w * 0.72f;
            foreach (var b in _messages.Query(className: "message-bubble").ToList())
                b.style.maxWidth = maxPx;
        }

        private void ConfirmAndStartJob()
        {
            OnJobCreated?.Invoke();
            Close();
        }

        // ── Message builders ──────────────────────────────────────────────

        private VisualElement BuildConfirmRow()
        {
            var row = new VisualElement();
            row.AddToClassList("new-job-action-row");

            var btn = new Button();
            btn.text = "Confirm and start job";
            btn.AddToClassList("btn-new-job-primary");
            btn.RegisterCallback<ClickEvent>(_ => ConfirmAndStartJob());
            row.Add(btn);
            return row;
        }

        private static VisualElement BuildVehicle(string sender, string body, string color)
        {
            var row = new VisualElement();
            row.AddToClassList("message-row");
            row.AddToClassList("message-row-vehicle");

            var content = new VisualElement();
            content.AddToClassList("vehicle-message-content");

            var name = new Label(sender);
            name.AddToClassList("vehicle-sender-name");
            name.AddToClassList($"sender--{color}");

            var bubble = new Label(body);
            bubble.AddToClassList("message-bubble");
            bubble.AddToClassList("vehicle-bubble");

            content.Add(name);
            content.Add(bubble);
            row.Add(content);
            return row;
        }

        private static VisualElement BuildSystem(string body)
        {
            var row = new VisualElement();
            row.AddToClassList("message-row");
            row.AddToClassList("message-row-vehicle");

            var bubble = new Label(body);
            bubble.AddToClassList("message-bubble");
            bubble.AddToClassList("vehicle-bubble");

            row.Add(bubble);
            return row;
        }

        private static VisualElement BuildCommand(string body)
        {
            var row = new VisualElement();
            row.AddToClassList("message-row");
            row.AddToClassList("message-row-command");

            var bubble = new Label(body);
            bubble.AddToClassList("message-bubble");
            bubble.AddToClassList("command-bubble");

            row.Add(bubble);
            return row;
        }
    }
}
