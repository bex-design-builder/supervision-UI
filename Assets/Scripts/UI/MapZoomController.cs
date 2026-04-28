using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace GuidanceUI.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class MapZoomController : MonoBehaviour
    {
        private const float MinScale    = 4f;
        private const float MaxScale    = 60f;
        private const float ButtonStep  = 1.5f;

        private MapPointCloudView _view;
        private VisualElement     _root;

        // Pinch state
        private readonly Dictionary<int, Vector2> _pointers = new Dictionary<int, Vector2>();
        private bool  _isPinching;
        private float _pinchStartScale;
        private float _pinchStartDistance;

        void Start()
        {
            _root = GetComponent<UIDocument>().rootVisualElement;
            _view = GetComponent<MapViewController>().View;

            _root.Q<Button>("btn-zoom-in")?.RegisterCallback<ClickEvent>(_ => Step(ButtonStep));
            _root.Q<Button>("btn-zoom-out")?.RegisterCallback<ClickEvent>(_ => Step(1f / ButtonStep));

            _root.RegisterCallback<PointerDownEvent>(OnPointerDown,   TrickleDown.TrickleDown);
            _root.RegisterCallback<PointerMoveEvent>(OnPointerMove,   TrickleDown.TrickleDown);
            _root.RegisterCallback<PointerUpEvent>(OnPointerUp,       TrickleDown.TrickleDown);
            _root.RegisterCallback<PointerCancelEvent>(_ => EndPinch(), TrickleDown.TrickleDown);
        }

        private void Step(float multiplier) =>
            _view.ZoomScale = Mathf.Clamp(_view.ZoomScale * multiplier, MinScale, MaxScale);

        // ── Pinch ─────────────────────────────────────────────────────────

        private void OnPointerDown(PointerDownEvent evt)
        {
            _pointers[evt.pointerId] = evt.position;
            if (_pointers.Count == 2 && !BottomSheetController.IsDragging)
                BeginPinch();
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (!_pointers.ContainsKey(evt.pointerId)) return;
            _pointers[evt.pointerId] = evt.position;

            if (!_isPinching || _pointers.Count < 2) return;

            float dist = TwoPointerDistance();
            _view.ZoomScale = Mathf.Clamp(
                _pinchStartScale * (dist / _pinchStartDistance), MinScale, MaxScale);
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            _pointers.Remove(evt.pointerId);
            if (_pointers.Count < 2) EndPinch();
        }

        private void BeginPinch()
        {
            _pinchStartDistance = TwoPointerDistance();
            _pinchStartScale    = _view.ZoomScale;
            _isPinching         = true;
        }

        private void EndPinch() => _isPinching = false;

        private float TwoPointerDistance()
        {
            Vector2 a = default, b = default;
            int i = 0;
            foreach (var v in _pointers.Values)
            {
                if (i == 0) a = v; else b = v;
                if (++i == 2) break;
            }
            return Vector2.Distance(a, b);
        }
    }
}
