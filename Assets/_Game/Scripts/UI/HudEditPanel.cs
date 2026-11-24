using Game.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    [DisallowMultipleComponent]
    public sealed class HudEditPanel : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Toggle leftHandedToggle;
        [SerializeField] private Slider scaleSlider;
        [SerializeField] private Slider sensitivitySlider;

        private void OnEnable()
        {
            HudEditMode.Changed += RefreshScaleSlider;
        }

        private void OnDisable()
        {
            HudEditMode.Changed -= RefreshScaleSlider;
        }

        public void OpenEdit()
        {
            ControlsSettings settings = ControlsSettingsStore.Current;
            if (leftHandedToggle != null)
                leftHandedToggle.SetIsOnWithoutNotify(settings.leftHanded);
            if (sensitivitySlider != null)
                sensitivitySlider.SetValueWithoutNotify(settings.lookSensitivity);
            if (panel != null)
                panel.SetActive(true);

            HudEditMode.SetActive(true);
            RefreshScaleSlider();
        }

        public void Done()
        {
            ControlsSettingsStore.Save();
            HudEditMode.SetActive(false);
            if (panel != null)
                panel.SetActive(false);
        }

        public void ResetAll()
        {
            ControlsSettingsStore.ResetAll();
            ControlsSettings settings = ControlsSettingsStore.Current;
            if (leftHandedToggle != null)
                leftHandedToggle.SetIsOnWithoutNotify(settings.leftHanded);
            if (sensitivitySlider != null)
                sensitivitySlider.SetValueWithoutNotify(settings.lookSensitivity);
            RefreshScaleSlider();
        }

        public void SetLeftHanded(bool value)
        {
            ControlsSettingsStore.Current.leftHanded = value;
            ControlsSettingsStore.NotifyChanged();
        }

        public void SetScale(float value)
        {
            HudElement selected = HudEditMode.Selected;
            if (selected == null || !selected.Customizable)
                return;

            ControlsSettingsStore.Current.Get(selected.Id).scale = Mathf.Clamp(value, 0.5f, 2f);
            ControlsSettingsStore.NotifyChanged();
        }

        public void SetSensitivity(float value)
        {
            ControlsSettingsStore.Current.lookSensitivity = Mathf.Clamp(value, 0.05f, 0.4f);
        }

        private void RefreshScaleSlider()
        {
            if (scaleSlider == null)
                return;

            HudElement selected = HudEditMode.Selected;
            bool canScale = selected != null && selected.Customizable;
            scaleSlider.interactable = canScale;
            if (canScale)
                scaleSlider.SetValueWithoutNotify(ControlsSettingsStore.Current.Get(selected.Id).scale);
        }
    }
}
