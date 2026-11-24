using System;
using UnityEngine;

namespace Game.Core
{
    /// <summary>UI-neutral message bus used by gameplay to update the gather HUD.</summary>
    public static class HudMessages
    {
        public static event Action<string> PromptChanged;
        public static event Action<float> ProgressChanged;
        public static event Action<string, float> ToolChanged;
        public static event Action<string> ToastRaised;

        private static string prompt = string.Empty;
        private static string toolName = string.Empty;
        private static float progress = -1f;
        private static float toolFill = 0f;

        public static void SetPrompt(string text)
        {
            text = text ?? string.Empty;
            if (string.Equals(text, prompt, StringComparison.Ordinal)) return;
            prompt = text;
            PromptChanged?.Invoke(prompt);
        }

        public static void SetProgress(float value)
        {
            value = value < 0f ? -1f : Mathf.Clamp01(value);
            if (Mathf.Approximately(value, progress)) return;
            progress = value;
            ProgressChanged?.Invoke(progress);
        }

        public static void SetTool(string name, float fill01)
        {
            name = name ?? string.Empty;
            fill01 = Mathf.Clamp01(fill01);
            if (string.Equals(name, toolName, StringComparison.Ordinal) &&
                Mathf.Approximately(fill01, toolFill)) return;
            toolName = name;
            toolFill = fill01;
            ToolChanged?.Invoke(toolName, toolFill);
        }

        public static void Toast(string text)
        {
            if (!string.IsNullOrWhiteSpace(text))
                ToastRaised?.Invoke(text);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            PromptChanged = null;
            ProgressChanged = null;
            ToolChanged = null;
            ToastRaised = null;
            prompt = string.Empty;
            toolName = string.Empty;
            progress = -1f;
            toolFill = 0f;
        }
    }
}
