using UnityEngine;
using UnityEngine.UIElements;

namespace GuidanceUI.UI
{
    [DefaultExecutionOrder(-1)]  // must run before MapZoomController
    [RequireComponent(typeof(UIDocument))]
    public class MapViewController : MonoBehaviour
    {
        public MapPointCloudView View { get; private set; }

        void Start()
        {
            var root     = GetComponent<UIDocument>().rootVisualElement;
            var viewport = root.Q("map-viewport");
            viewport.pickingMode = PickingMode.Ignore;
            View = new MapPointCloudView();
            viewport.Add(View);
        }
    }
}
