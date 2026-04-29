using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace GuidanceUI.UI
{
    public class MockJobData
    {
        public string   Name;
        public string   Status;        // "active" | "paused" | "pending"
        public float    Progress;      // 0–1
        public int      EstimatedMins;
        public (string color, string name)[] Vehicles;
    }

    [RequireComponent(typeof(UIDocument))]
    public class JobsListController : MonoBehaviour
    {
        private static readonly string[] JobNames =
        {
            "Navigate to Zone B",
            "Load materials",
            "Stake out boundary",
            "Grade site area",
            "Transport materials",
            "Clear debris",
        };

        private static readonly (string color, string name)[][] VehiclePresets =
        {
            new[] { ("green",  "Steve")    },
            new[] { ("purple", "Mark")     },
            new[] { ("blue",   "Bobcat 3") },
            new[] { ("green",  "Steve"),    ("blue", "Bobcat 3") },
            new[] { ("purple", "Mark"),     ("green", "Steve")   },
        };

        private VisualElement _root;
        private ScrollView    _scroll;
        private VisualElement _emptyState;
        private int           _jobIndex;
        private int           _presetIndex;

        void Start()
        {
            _root       = GetComponent<UIDocument>().rootVisualElement;
            _scroll     = _root.Q<ScrollView>("sheet-scroll");
            _emptyState = _root.Q(className: "jobs-empty-state");

            NewJobPageController.OnJobCreated += HandleJobCreated;

            var blockedJob = new MockJobData
            {
                Name          = "Clear debris field",
                Status        = "blocked",
                Progress      = 0.3f,
                EstimatedMins = 0,
                Vehicles      = new[] { ("purple", "Mark") },
            };

            if (_emptyState != null)
                _emptyState.style.display = DisplayStyle.None;

            _scroll?.Add(BuildCard(blockedJob));
        }

        void OnDestroy()
        {
            NewJobPageController.OnJobCreated -= HandleJobCreated;
        }

        private void HandleJobCreated()
        {
            var job = new MockJobData
            {
                Name          = JobNames[_jobIndex % JobNames.Length],
                Status        = "active",
                Progress      = UnityEngine.Random.Range(0.05f, 0.75f),
                EstimatedMins = UnityEngine.Random.Range(8, 55),
                Vehicles      = VehiclePresets[_presetIndex % VehiclePresets.Length],
            };
            _jobIndex++;
            _presetIndex++;

            if (_emptyState != null)
                _emptyState.style.display = DisplayStyle.None;

            _scroll?.Add(BuildCard(job));
        }

        private static VisualElement BuildCard(MockJobData job)
        {
            var card = new VisualElement();
            card.AddToClassList("job-card");
            card.AddToClassList($"job-card--{job.Status}");

            // Status pill
            var pill = new Label(StatusLabel(job.Status).ToUpper());
            pill.AddToClassList("job-status-pill");
            pill.AddToClassList($"job-status-pill--{job.Status}");
            card.Add(pill);

            // Job name
            var name = new Label(job.Name);
            name.AddToClassList("job-name");
            card.Add(name);

            // Progress row
            var progressRow = new VisualElement();
            progressRow.AddToClassList("job-progress-row");

            var track = new VisualElement();
            track.AddToClassList("job-bar-track");

            var fill = new VisualElement();
            fill.AddToClassList("job-bar-fill");
            fill.AddToClassList($"job-bar-fill--{job.Status}");
            fill.style.width = new StyleLength(new Length(job.Progress * 100f, LengthUnit.Percent));

            track.Add(fill);

            var timeText = job.Status == "blocked" ? "On hold" : $"{job.EstimatedMins}m left";
            var time = new Label(timeText);
            time.AddToClassList("job-time");

            progressRow.Add(track);
            progressRow.Add(time);
            card.Add(progressRow);

            // Vehicle pills
            var avatars = new VisualElement();
            avatars.AddToClassList("job-avatars");
            foreach (var (color, vehicleName) in job.Vehicles)
            {
                var vehiclePill = new VisualElement();
                vehiclePill.AddToClassList("job-vehicle-pill");

                var dot = new VisualElement();
                dot.AddToClassList("job-vehicle-dot");
                dot.AddToClassList($"job-avatar--{color}");

                var label = new Label(vehicleName);
                label.AddToClassList("job-vehicle-name");

                vehiclePill.Add(dot);
                vehiclePill.Add(label);
                avatars.Add(vehiclePill);
            }
            card.Add(avatars);

            return card;
        }

        private static string StatusLabel(string s) => s switch
        {
            "active"  => "In progress",
            "paused"  => "Paused",
            "pending" => "Pending",
            "blocked" => "Blocked",
            _         => s,
        };
    }
}
