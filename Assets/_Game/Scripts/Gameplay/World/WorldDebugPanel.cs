using UnityEngine;

namespace Game.Gameplay
{
    /// <summary>On-screen diagnostics are present only in the Editor and Development Builds.</summary>
    [DisallowMultipleComponent]
    public sealed class WorldDebugPanel : MonoBehaviour
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [SerializeField] private DayNightCycle day;
        [SerializeField] private WeatherSystem weather;
        [SerializeField] private IslandGenerator island;

        private bool visible = true;

        private void OnGUI()
        {
            if (day == null || weather == null || island == null)
                return;

            Matrix4x4 previousMatrix = GUI.matrix;
            float scale = Mathf.Clamp(Mathf.Min(Screen.height / 720f, Screen.width / 1080f), 0.75f, 1.25f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));

            if (!visible)
            {
                if (GUI.Button(new Rect(10f, 10f, 82f, 30f), "DEBUG"))
                    visible = true;
                GUI.matrix = previousMatrix;
                return;
            }

            GUILayout.BeginArea(new Rect(10f, 10f, 660f, 190f), GUI.skin.box);
            GUILayout.BeginHorizontal();
            int hour = Mathf.FloorToInt(day.Hour);
            int minute = Mathf.FloorToInt((day.Hour - hour) * 60f);
            GUILayout.Label($"NGÀY {day.DayCount + 1}  {hour:00}:{minute:00}  {weather.Current}", GUILayout.Width(250f));
            GUILayout.Label($"Seed {island.Seed} · Checksum {island.Checksum}");
            if (GUILayout.Button("ẨN", GUILayout.Width(54f)))
                visible = false;
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Bình minh 06:00", GUILayout.Width(120f))) day.SetTime01(0.25f);
            if (GUILayout.Button("Trưa 12:00", GUILayout.Width(105f))) day.SetTime01(0.5f);
            if (GUILayout.Button("Hoàng hôn 18:00", GUILayout.Width(120f))) day.SetTime01(0.75f);
            if (GUILayout.Button("Đêm 00:00", GUILayout.Width(105f))) day.SetTime01(0f);
            if (GUILayout.Button(day.SpeedMultiplier > 1f ? "Tua nhanh ON" : "Tua nhanh OFF", GUILayout.Width(110f)))
                day.SpeedMultiplier = day.SpeedMultiplier > 1f ? 1f : 30f;
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Trời quang", GUILayout.Width(110f))) weather.SetWeather(WeatherType.Clear);
            if (GUILayout.Button("Sương mù", GUILayout.Width(95f))) weather.SetWeather(WeatherType.Fog);
            if (GUILayout.Button("Mưa", GUILayout.Width(78f))) weather.SetWeather(WeatherType.Rain);
            if (GUILayout.Button("Bão", GUILayout.Width(78f))) weather.SetWeather(WeatherType.Storm);
            if (GUILayout.Button("Cùng seed", GUILayout.Width(112f))) island.Generate(island.Seed);
            if (GUILayout.Button("Đảo mới", GUILayout.Width(78f))) island.Generate(Random.Range(1, int.MaxValue));
            GUILayout.EndHorizontal();

            GUILayout.Label($"Chunks: {island.ChunkCount}/16   Cây: {island.TreeCount}   Đá: {island.RockCount}   Hang: {island.CaveEntrancesWorld.Count}");
            GUILayout.Label($"Generator v{IslandGenerator.GeneratorAlgorithmVersion}   GPU Instancing: {(island.UsesGpuInstancing ? "BẬT" : "Fallback/Chưa có props")}   Nhóm: {island.InstanceBatchCount}   Instances: {island.InstanceCount}   Ước tính: {island.EstimatedTriangleCount:N0} tris");
            GUILayout.EndArea();
            GUI.matrix = previousMatrix;
        }
#endif
    }
}
