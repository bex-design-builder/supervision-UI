using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace GuidanceUI.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class NewJobPageController : MonoBehaviour
    {
        public static event Action OnJobCreated;

        private VisualElement _page;

        void Start()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            _page = root.Q("new-job-page");

            if (_page == null)
            {
                Debug.LogError("[NewJobPage] 'new-job-page' element not found in UXML.");
                return;
            }

            root.Q<Button>("btn-close-new-job")?.RegisterCallback<ClickEvent>(_ => Close());
            root.Q<Button>("btn-create-job")?.RegisterCallback<ClickEvent>(_ =>
            {
                OnJobCreated?.Invoke();
                Close();
            });

            // Start off-screen below
            _page.style.translate = new StyleTranslate(
                new Translate(new Length(0), new Length(100, LengthUnit.Percent)));
            _page.pickingMode = PickingMode.Ignore;
        }

        public void Open()
        {
            if (_page == null) return;
            _page.BringToFront();
            _page.pickingMode = PickingMode.Position;
            _page.style.translate = new StyleTranslate(new Translate(0, 0));
            _page.AddToClassList("page-visible");
        }

        public void Close()
        {
            if (_page == null) return;
            _page.pickingMode = PickingMode.Ignore;
            _page.style.translate = new StyleTranslate(
                new Translate(new Length(0), new Length(100, LengthUnit.Percent)));
            _page.RemoveFromClassList("page-visible");
        }
    }
}
