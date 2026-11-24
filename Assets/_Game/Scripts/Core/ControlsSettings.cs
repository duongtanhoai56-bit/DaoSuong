using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Core
{
    [Serializable]
    public sealed class ElementLayout
    {
        public string id;
        public Vector2 offset;
        [Range(0.5f, 2f)] public float scale = 1f;
    }

    [Serializable]
    public sealed class ControlsSettings
    {
        public bool leftHanded;
        [Range(0.05f, 0.4f)] public float lookSensitivity = 0.15f;
        public List<ElementLayout> elements = new List<ElementLayout>();

        public ElementLayout Get(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("HUD element ID cannot be empty.", nameof(id));

            if (elements == null)
                elements = new List<ElementLayout>();

            for (int i = 0; i < elements.Count; i++)
            {
                ElementLayout element = elements[i];
                if (element != null && string.Equals(element.id, id, StringComparison.Ordinal))
                {
                    element.scale = Mathf.Clamp(element.scale, 0.5f, 2f);
                    return element;
                }
            }

            var created = new ElementLayout { id = id };
            elements.Add(created);
            return created;
        }

        public void Sanitize()
        {
            lookSensitivity = Mathf.Clamp(lookSensitivity, 0.05f, 0.4f);
            if (elements == null)
            {
                elements = new List<ElementLayout>();
                return;
            }

            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (int i = elements.Count - 1; i >= 0; i--)
            {
                ElementLayout element = elements[i];
                if (element == null || string.IsNullOrWhiteSpace(element.id) || !seen.Add(element.id))
                {
                    elements.RemoveAt(i);
                    continue;
                }

                element.scale = Mathf.Clamp(element.scale, 0.5f, 2f);
            }
        }
    }

    /// <summary>
    /// Small persistence boundary for local HUD preferences. Changes are previewed
    /// in memory and only persisted when the player presses Done.
    /// </summary>
    public static class ControlsSettingsStore
    {
        private const string Key = "dao_suong.controls.v1";
        private static ControlsSettings current;

        public static event Action Changed;
        public static ControlsSettings Current => current ?? (current = Load());

        private static ControlsSettings Load()
        {
            string json = PlayerPrefs.GetString(Key, string.Empty);
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    ControlsSettings loaded = JsonUtility.FromJson<ControlsSettings>(json);
                    if (loaded != null)
                    {
                        loaded.Sanitize();
                        return loaded;
                    }
                }
                catch (Exception)
                {
                    // Corrupted or older preferences fall back to safe defaults.
                }
            }

            return new ControlsSettings();
        }

        public static void Save()
        {
            Current.Sanitize();
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(Current));
            PlayerPrefs.Save();
        }

        public static void NotifyChanged()
        {
            Changed?.Invoke();
        }

        /// <summary>Reset the in-memory preview; Done is still required to save it.</summary>
        public static void ResetAll()
        {
            current = new ControlsSettings();
            NotifyChanged();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            current = null;
            Changed = null;
        }
    }
}
