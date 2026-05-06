using UnityEngine;
using UnityEngine.UIElements;

namespace GuidanceUI.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class ChatController : MonoBehaviour
    {
        private ScrollView    _messages;
        private TextField     _input;
        private Button        _sendBtn;
        private VisualElement _lastMessage;
        private VisualElement _chatPanel;
        private bool          _chatWasVisible;
        private Label         _placeholder;
        private bool          _inputFocused;

        void Start()
        {
            var root   = GetComponent<UIDocument>().rootVisualElement;
            _messages  = root.Q<ScrollView>("chat-messages");
            _input     = root.Q<TextField>("chat-input");
            _sendBtn   = root.Q<Button>("btn-send");
            _chatPanel = root.Q("chat-panel");

            if (_messages == null) return;

            var welcome = BuildSystem("Hi there, what would you like to get done?");
            _messages.Add(welcome);

            var stuck = BuildVehicle("Mark", "!! Vehicle stuck", "purple");
            _messages.Add(stuck);
            _lastMessage = stuck;

            _input.multiline = true;
            _input.RegisterValueChangedCallback(OnInputChanged);

            // Unity's placeholder-string doesn't render on multiline TextFields —
            // overlay a label instead and toggle it based on focus + content.
            var inputWrap = root.Q(className: "chat-input-wrap");
            if (inputWrap != null)
            {
                _placeholder = new Label("Direct a vehicle or ask anything...");
                _placeholder.AddToClassList("chat-placeholder");
                _placeholder.pickingMode = PickingMode.Ignore;
                inputWrap.Add(_placeholder);
            }

            _input.RegisterCallback<FocusInEvent>(_ => { _inputFocused = true;  UpdatePlaceholder(); });
            _input.RegisterCallback<FocusOutEvent>(_ => { _inputFocused = false; UpdatePlaceholder(); });

            _sendBtn?.RegisterCallback<ClickEvent>(_ => Send());

            // TrickleDown intercepts plain Enter to send the message.
            // Shift+Enter falls through to Unity's native multiline handler which inserts \n.
            _input?.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode != KeyCode.Return && evt.keyCode != KeyCode.KeypadEnter) return;
                if (evt.shiftKey) return;
                Send();
                evt.StopPropagation();
            }, TrickleDown.TrickleDown);

            // Scroll to bottom whenever the chat panel becomes visible (tab switch).
            _chatPanel?.RegisterCallback<GeometryChangedEvent>(OnChatPanelGeometry);

            UpdateSendButton();
            _messages.schedule.Execute(FixBubbleWidths).StartingIn(150);
        }

        private void OnInputChanged(ChangeEvent<string> _)
        {
            UpdateSendButton();
            UpdateInputHeight();
            UpdatePlaceholder();
        }

        private void UpdatePlaceholder()
        {
            if (_placeholder == null) return;
            bool show = string.IsNullOrEmpty(_input?.value) && !_inputFocused;
            _placeholder.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
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

        private void OnChatPanelGeometry(GeometryChangedEvent evt)
        {
            bool visible = evt.newRect.height > 0;
            if (visible && !_chatWasVisible && _lastMessage != null)
                _messages.schedule.Execute(ScrollToBottom).StartingIn(50);
            _chatWasVisible = visible;
        }

        private void Send()
        {
            if (_input == null || string.IsNullOrWhiteSpace(_input.value)) return;

            var row = BuildCommand(_input.value);
            _messages.Add(row);
            _lastMessage = row;
            _input.value = "";
            UpdateSendButton();
            UpdateInputHeight();
            UpdatePlaceholder();
            _input.Focus();

            _messages.schedule.Execute(FixBubbleWidths).StartingIn(150);
            _messages.schedule.Execute(ScrollToBottom).StartingIn(200);
        }

        private void ScrollToBottom()
        {
            if (_lastMessage != null)
                _messages.ScrollTo(_lastMessage);
        }

        private void FixBubbleWidths()
        {
            float w = _messages.resolvedStyle.width;
            if (w <= 0) return;
            float maxPx = w * 0.72f;
            foreach (var b in _messages.Query(className: "message-bubble").ToList())
                b.style.maxWidth = maxPx;
        }

        // ── Message builders ──────────────────────────────────────────────

        private VisualElement BuildSystem(string body)
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

        private VisualElement BuildCommand(string body)
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

        public VisualElement BuildVehicle(string sender, string body, string color)
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
    }
}
