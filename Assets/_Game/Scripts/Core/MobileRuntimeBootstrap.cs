using UnityEngine;

namespace Game.Core
{
    /// <summary>Conservative mobile defaults; never alters desktop Editor/player settings.</summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-900)]
    public sealed class MobileRuntimeBootstrap : MonoBehaviour
    {
        [SerializeField, Min(30)] private int normalFrameRate = 60;
        [SerializeField, Min(20)] private int lowMemoryFrameRate = 30;
        [SerializeField, Min(512)] private int lowMemoryThresholdMb = 3500;
        [SerializeField, Min(0f)] private float normalShadowDistance = 35f;
        [SerializeField, Min(0f)] private float lowMemoryShadowDistance = 16f;

        private void Awake()
        {
            if (!Application.isMobilePlatform) return;

            Input.multiTouchEnabled = true;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            QualitySettings.vSyncCount = 0;

            bool lowMemory = SystemInfo.systemMemorySize > 0 && SystemInfo.systemMemorySize < lowMemoryThresholdMb;
            Application.targetFrameRate = lowMemory ? lowMemoryFrameRate : normalFrameRate;
            QualitySettings.shadowDistance = lowMemory ? lowMemoryShadowDistance : normalShadowDistance;
            QualitySettings.lodBias = lowMemory ? 0.65f : 1f;
            if (lowMemory)
            {
                QualitySettings.shadows = ShadowQuality.Disable;
                QualitySettings.antiAliasing = 0;
            }
        }

        private void OnValidate()
        {
            normalFrameRate = Mathf.Max(30, normalFrameRate);
            lowMemoryFrameRate = Mathf.Clamp(lowMemoryFrameRate, 20, normalFrameRate);
            lowMemoryThresholdMb = Mathf.Max(512, lowMemoryThresholdMb);
            normalShadowDistance = Mathf.Max(0f, normalShadowDistance);
            lowMemoryShadowDistance = Mathf.Clamp(lowMemoryShadowDistance, 0f, normalShadowDistance);
        }
    }
}
