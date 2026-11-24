using System.Collections.Generic;
using System.Text;
using Game.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    [DisallowMultipleComponent]
    public sealed class GatherHud : MonoBehaviour
    {
        [Header("Prompt & progress")]
        [SerializeField] private Text promptText;
        [SerializeField] private GameObject progressRoot;
        [SerializeField] private RectTransform progressFill;

        [Header("Tool durability")]
        [SerializeField] private GameObject toolRoot;
        [SerializeField] private Text toolText;
        [SerializeField] private RectTransform toolFill;
        [SerializeField] private Image toolFillImage;

        [Header("Toasts")]
        [SerializeField] private Text toastText;
        [SerializeField, Min(0.2f)] private float toastSeconds = 2.5f;
        [SerializeField, Range(1, 8)] private int maximumToasts = 4;

        private struct ToastLine
        {
            public string text;
            public float expiresAt;
        }

        private readonly List<ToastLine> toasts = new List<ToastLine>();
        private readonly StringBuilder builder = new StringBuilder(128);
        private bool toastDirty;

        private void OnEnable()
        {
            HudMessages.PromptChanged += OnPrompt;
            HudMessages.ProgressChanged += OnProgress;
            HudMessages.ToolChanged += OnTool;
            HudMessages.ToastRaised += OnToast;
            OnPrompt(string.Empty);
            OnProgress(-1f);
            OnTool(string.Empty, 0f);
            toasts.Clear();
            toastDirty = true;
            RefreshToasts();
        }

        private void OnDisable()
        {
            HudMessages.PromptChanged -= OnPrompt;
            HudMessages.ProgressChanged -= OnProgress;
            HudMessages.ToolChanged -= OnTool;
            HudMessages.ToastRaised -= OnToast;
        }

        private void OnPrompt(string text)
        {
            if (promptText != null) promptText.text = text ?? string.Empty;
        }

        private void OnProgress(float value)
        {
            if (progressRoot != null) progressRoot.SetActive(value >= 0f);
            if (value >= 0f && progressFill != null)
                progressFill.anchorMax = new Vector2(Mathf.Clamp01(value), 1f);
        }

        private void OnTool(string name, float fill)
        {
            bool visible = !string.IsNullOrEmpty(name);
            if (toolRoot != null) toolRoot.SetActive(visible);
            if (toolText != null) toolText.text = name ?? string.Empty;
            if (toolFill != null) toolFill.anchorMax = new Vector2(Mathf.Clamp01(fill), 1f);
            if (toolFillImage != null)
                toolFillImage.color = Color.Lerp(new Color(0.9f, 0.2f, 0.2f), new Color(0.3f, 0.85f, 0.3f), fill);
        }

        private void OnToast(string text)
        {
            toasts.Add(new ToastLine { text = text, expiresAt = Time.unscaledTime + toastSeconds });
            while (toasts.Count > maximumToasts) toasts.RemoveAt(0);
            toastDirty = true;
        }

        private void Update()
        {
            float now = Time.unscaledTime;
            for (int i = toasts.Count - 1; i >= 0; i--)
            {
                if (now < toasts[i].expiresAt) continue;
                toasts.RemoveAt(i);
                toastDirty = true;
            }
            if (toastDirty) RefreshToasts();
        }

        private void RefreshToasts()
        {
            if (!toastDirty && toasts.Count == 0) return;
            toastDirty = false;
            builder.Clear();
            for (int i = 0; i < toasts.Count; i++) builder.AppendLine(toasts[i].text);
            if (toastText != null) toastText.text = builder.ToString().TrimEnd();
        }
    }
}
