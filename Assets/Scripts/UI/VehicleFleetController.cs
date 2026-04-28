using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace GuidanceUI.UI
{
    public enum VehicleStatus { Intervention, Paused, Active, Idle }

    public class VehicleData
    {
        public string        Id;
        public string        Name;
        public string        Color;   // "green" | "blue" | "purple"
        public VehicleStatus Status;
    }

    [RequireComponent(typeof(UIDocument))]
    public class VehicleFleetController : MonoBehaviour
    {
        private static readonly VehicleData[] Vehicles =
        {
            new VehicleData { Id = "mark",    Name = "Mark",     Color = "purple", Status = VehicleStatus.Intervention },
            new VehicleData { Id = "steve",   Name = "Steve",    Color = "green",  Status = VehicleStatus.Active       },
            new VehicleData { Id = "bobcat3", Name = "Bobcat 3", Color = "blue",   Status = VehicleStatus.Idle         },
        };

        private readonly Dictionary<string, VehicleStatus> _overrides = new();

        private VisualElement              _root;
        private Button                     _fab;
        private VisualElement              _panel;
        private ScrollView                 _list;
        private string                     _filter = "all";
        private bool                       _panelOpen;
        private bool                       _pulseHigh;
        private IVisualElementScheduledItem _pulseHandle;

        void Start()
        {
            _root  = GetComponent<UIDocument>().rootVisualElement;
            _fab   = _root.Q<Button>("btn-vehicles-fab");
            _panel = _root.Q("vehicles-panel");
            _list  = _root.Q<ScrollView>("vehicles-list");

            if (_fab == null) return;

            _fab.RegisterCallback<ClickEvent>(_ => TogglePanel());
            _root.Q<Button>("btn-close-vehicles")?.RegisterCallback<ClickEvent>(_ => ClosePanel());

            foreach (var (name, key) in new[] { ("btn-filter-all","all"), ("btn-filter-needs-help","needs-help"), ("btn-filter-working","working"), ("btn-filter-idle","idle") })
                _root.Q<Button>(name)?.RegisterCallback<ClickEvent>(_ => SetFilter(key));

            if (_panel != null) _panel.style.display = DisplayStyle.None;

            Refresh();

            _pulseHandle = _root.schedule.Execute(PulseStep).Every(900);
        }

        void OnDestroy()
        {
            _pulseHandle?.Pause();
        }

        // ── Public stop / resume (could be called from chat commands later) ──

        public void StopVehicle(string id)
        {
            _overrides[id] = VehicleStatus.Paused;
            Refresh();
        }

        public void ResumeVehicle(string id)
        {
            _overrides.Remove(id);
            Refresh();
        }

        // ── Panel ─────────────────────────────────────────────────────────────

        private void TogglePanel() { if (_panelOpen) ClosePanel(); else OpenPanel(); }

        private void OpenPanel()
        {
            _panelOpen = true;
            if (_panel != null)
            {
                _panel.style.display = DisplayStyle.Flex;
                _panel.BringToFront();
            }
        }

        private void ClosePanel()
        {
            _panelOpen = false;
            if (_panel != null) _panel.style.display = DisplayStyle.None;
        }

        private void SetFilter(string filter)
        {
            _filter = filter;
            foreach (var (name, key) in new[] { ("btn-filter-all","all"), ("btn-filter-needs-help","needs-help"), ("btn-filter-working","working"), ("btn-filter-idle","idle") })
                _root.Q<Button>(name)?.EnableInClassList("vehicles-filter-pill--selected", key == filter);
            RefreshList();
        }

        // ── Refresh ───────────────────────────────────────────────────────────

        private void Refresh()
        {
            RefreshFab();
            RefreshList();
        }

        private void RefreshFab()
        {
            if (_fab == null) return;

            var statuses    = Vehicles.Select(GetStatus).ToList();
            int needsHelp   = statuses.Count(s => s == VehicleStatus.Intervention || s == VehicleStatus.Paused);
            int working     = statuses.Count(s => s == VehicleStatus.Active);
            int idle        = statuses.Count(s => s == VehicleStatus.Idle);
            bool hasAlert   = needsHelp > 0;

            _fab.Clear();
            var wrap = new VisualElement();
            wrap.AddToClassList("vtog-stats");

            if (hasAlert) wrap.Add(MakeStat($"{needsHelp} needs help", "vtog-dot--alert", "vtog-stat--alert"));
            wrap.Add(MakeStat($"{working} working",  "vtog-dot--active", "vtog-stat--active"));
            wrap.Add(MakeStat($"{idle} idle",        "vtog-dot--idle",   "vtog-stat--idle"));

            _fab.Add(wrap);
            _fab.EnableInClassList("btn-vehicles-fab--alert", hasAlert);
        }

        private static VisualElement MakeStat(string text, string dotClass, string statClass)
        {
            var stat = new VisualElement();
            stat.AddToClassList("vtog-stat");
            stat.AddToClassList(statClass);

            var dot = new VisualElement();
            dot.AddToClassList("vtog-dot");
            dot.AddToClassList(dotClass);

            var lbl = new Label(text);
            lbl.AddToClassList("vtog-label");

            stat.Add(dot);
            stat.Add(lbl);
            return stat;
        }

        private void RefreshList()
        {
            if (_list == null) return;
            _list.Clear();

            var groups = new (string Label, VehicleStatus[] Keys)[]
            {
                ("Needs help", new[] { VehicleStatus.Intervention, VehicleStatus.Paused }),
                ("Working",    new[] { VehicleStatus.Active }),
                ("Idle",       new[] { VehicleStatus.Idle }),
            };

            VehicleStatus[] filterKeys = _filter switch
            {
                "needs-help" => new[] { VehicleStatus.Intervention, VehicleStatus.Paused },
                "working"    => new[] { VehicleStatus.Active },
                "idle"       => new[] { VehicleStatus.Idle },
                _            => null,
            };

            foreach (var (label, keys) in groups)
            {
                if (filterKeys != null && !keys.Any(k => filterKeys.Contains(k))) continue;

                var vehicles = Vehicles.Where(v => keys.Contains(GetStatus(v))).ToArray();
                if (vehicles.Length == 0) continue;

                var group = new VisualElement();
                group.AddToClassList("vehicles-group");

                var groupLabel = new Label(label.ToUpper());
                groupLabel.AddToClassList("vehicles-group-label");
                group.Add(groupLabel);

                foreach (var v in vehicles)
                    group.Add(BuildCard(v));

                _list.Add(group);
            }
        }

        private VisualElement BuildCard(VehicleData v)
        {
            var status    = GetStatus(v);
            var statusKey = StatusKey(status);

            var card = new VisualElement();
            card.AddToClassList("vehicle-card");
            card.AddToClassList($"vehicle-card--{statusKey}");

            var avatar = new VisualElement();
            avatar.AddToClassList("vehicle-avatar");
            avatar.AddToClassList($"vehicle-avatar--{v.Color}");

            var info = new VisualElement();
            info.AddToClassList("vehicle-info");

            var name = new Label(v.Name);
            name.AddToClassList("vehicle-name");

            var statusRow = new VisualElement();
            statusRow.AddToClassList("vehicle-status-row");

            var dot = new VisualElement();
            dot.AddToClassList("vehicle-status-dot");
            dot.AddToClassList($"vehicle-status-dot--{statusKey}");

            var statusLbl = new Label(StatusLabel(status));
            statusLbl.AddToClassList("vehicle-status-text");

            statusRow.Add(dot);
            statusRow.Add(statusLbl);
            info.Add(name);
            info.Add(statusRow);

            card.Add(avatar);
            card.Add(info);

            var id = v.Id;
            if (status == VehicleStatus.Active)
            {
                var btn = new Button(() => StopVehicle(id)) { text = "Stop" };
                btn.AddToClassList("btn-stop");
                card.Add(btn);
            }
            else if (status == VehicleStatus.Paused)
            {
                var btn = new Button(() => ResumeVehicle(id)) { text = "Resume" };
                btn.AddToClassList("btn-resume");
                card.Add(btn);
            }

            return card;
        }

        // ── Alert pulse ───────────────────────────────────────────────────────

        private void PulseStep()
        {
            if (_fab == null || !_fab.ClassListContains("btn-vehicles-fab--alert"))
            {
                _fab?.RemoveFromClassList("btn-vehicles-fab--pulse-high");
                return;
            }
            _pulseHigh = !_pulseHigh;
            _fab.EnableInClassList("btn-vehicles-fab--pulse-high", _pulseHigh);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private VehicleStatus GetStatus(VehicleData v)
            => _overrides.TryGetValue(v.Id, out var s) ? s : v.Status;

        private static string StatusKey(VehicleStatus s) => s switch
        {
            VehicleStatus.Intervention => "intervention",
            VehicleStatus.Paused       => "paused",
            VehicleStatus.Active       => "active",
            _                          => "idle",
        };

        private static string StatusLabel(VehicleStatus s) => s switch
        {
            VehicleStatus.Intervention => "Needs help",
            VehicleStatus.Active       => "Working",
            VehicleStatus.Paused       => "Paused",
            _                          => "Idle",
        };
    }
}
