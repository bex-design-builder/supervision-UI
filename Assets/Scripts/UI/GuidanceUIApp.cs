using UnityEngine;
using UnityEngine.UIElements;

namespace GuidanceUI.UI
{
    // Add this one component to the UI Document GameObject.
    // Unity automatically attaches every other required component.
    [RequireComponent(typeof(UIDocument))]
    [RequireComponent(typeof(MapViewController))]
    [RequireComponent(typeof(MapZoomController))]
    [RequireComponent(typeof(BottomSheetController))]
    [RequireComponent(typeof(NewJobPageController))]
    [RequireComponent(typeof(ChatController))]
    [RequireComponent(typeof(VehicleFleetController))]
    [RequireComponent(typeof(JobsListController))]
    [RequireComponent(typeof(ResponsiveManager))]
    public class GuidanceUIApp : MonoBehaviour { }
}
