using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace GuidanceUI.UI
{
    public enum Breakpoint { Mobile, Tablet, Desktop }

    [RequireComponent(typeof(UIDocument))]
    public class ResponsiveManager : MonoBehaviour
    {
        // Logical-pixel thresholds (Panel coordinate space, not physical pixels).
        // At 390×844 reference with height-match=1 these map to:
        //   Mobile  < 500  →  phones in portrait  (~390 logical)
        //   Tablet  500–700 → tablets in portrait (~580 logical)
        //   Desktop ≥ 700  →  desktop / landscape (~1500 logical at 1920×1080)
        private const float TabletMinWidth  = 500f;
        private const float DesktopMinWidth = 700f;

        public static event Action<Breakpoint> OnBreakpointChanged;
        public static Breakpoint Current { get; private set; } = Breakpoint.Desktop;

        private VisualElement _root;
        private Breakpoint _last = (Breakpoint)(-1);

        void Start()
        {
            _root = GetComponent<UIDocument>().rootVisualElement;
            _root.RegisterCallback<GeometryChangedEvent>(_ => Evaluate());
            Evaluate();
        }

        void Update() => Evaluate();

        private void Evaluate()
        {
            if (_root == null) return;
            float w = _root.resolvedStyle.width;
            if (w <= 0) return;

            var bp = Classify(w);
            if (bp == _last) return;

            _last = bp;
            Current = bp;

            _root.RemoveFromClassList("layout-mobile");
            _root.RemoveFromClassList("layout-tablet");
            _root.RemoveFromClassList("layout-desktop");
            _root.AddToClassList("layout-" + bp.ToString().ToLower());

            OnBreakpointChanged?.Invoke(bp);
            Debug.Log($"[Responsive] {bp} — logical width: {w:F0}px");
        }

        private static Breakpoint Classify(float width)
        {
            if (width < TabletMinWidth)  return Breakpoint.Mobile;
            if (width < DesktopMinWidth) return Breakpoint.Tablet;
            return Breakpoint.Desktop;
        }
    }
}
