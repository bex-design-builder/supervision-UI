using UnityEngine;
using System.Collections.Generic;

namespace GuidanceUI.MockData
{
    public static class MockMapData
    {
        public static readonly Vector3 MachinePosition = new Vector3(2f, 0f, -1f);
        public static readonly float MachineHeading = 38f;

        public static Vector3[] GenerateSitePoints()
        {
            var points = new List<Vector3>();

            // Ground plane with terrain noise
            for (int x = -22; x <= 22; x++)
            {
                for (int z = -22; z <= 22; z++)
                {
                    float noise = Mathf.PerlinNoise(x * 0.12f + 5f, z * 0.12f + 5f) * 0.4f;
                    points.Add(new Vector3(
                        x + Random.Range(-0.4f, 0.4f),
                        noise,
                        z + Random.Range(-0.4f, 0.4f)
                    ));
                }
            }

            // Dirt mound — top-right area
            for (int i = 0; i < 300; i++)
            {
                float angle = Random.Range(0f, Mathf.PI * 2f);
                float radius = Random.Range(0f, 5f);
                float x = 14f + Mathf.Cos(angle) * radius;
                float z = 10f + Mathf.Sin(angle) * radius;
                float height = Mathf.Max(0f, (5f - radius) * 0.7f) + Random.Range(0f, 0.2f);
                points.Add(new Vector3(x, height, z));
            }

            // Linear barrier — left side
            for (int i = 0; i < 80; i++)
            {
                float z = Mathf.Lerp(-8f, 8f, i / 80f);
                float height = Random.Range(0.8f, 1.4f);
                points.Add(new Vector3(-16f + Random.Range(-0.2f, 0.2f), height, z));
            }

            // Scattered debris — bottom area
            for (int i = 0; i < 120; i++)
            {
                float x = Random.Range(-10f, 10f);
                float z = Random.Range(-18f, -12f);
                float height = Random.Range(0.1f, 0.6f);
                points.Add(new Vector3(x, height, z));
            }

            return points.ToArray();
        }
    }
}
