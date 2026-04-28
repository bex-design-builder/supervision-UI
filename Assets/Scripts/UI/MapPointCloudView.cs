using UnityEngine.UIElements;

namespace GuidanceUI.UI
{
    // Blank map surface. Connect a real lidar / map data source here.
    // ZoomScale is kept so MapZoomController compiles and is ready to wire up.
    public class MapPointCloudView : VisualElement
    {
        private float _zoomScale = 12f;
        public float ZoomScale
        {
            get => _zoomScale;
            set { _zoomScale = value; }
        }

        public MapPointCloudView()
        {
            style.position = Position.Absolute;
            style.top = style.left = style.right = style.bottom = 0;
            pickingMode = PickingMode.Ignore;
        }
    }
}
