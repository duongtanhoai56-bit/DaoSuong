using System;
using Game.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>Small runtime-created panel for room codes, voice mute and push-to-talk.</summary>
    [DisallowMultipleComponent]
    public sealed class OnlineSessionHUD : MonoBehaviour
    {
        private Canvas canvas;
        private GameObject panel;
        private Text statusText;
        private Text codeText;
        private InputField nameInput;
        private InputField joinCodeInput;
        private Button createButton;
        private Button joinButton;
        private Button leaveButton;
        private Button copyButton;
        private Button muteButton;
        private Button openButton;
        private VoicePushToTalkButton pttButton;
        private IOnlineSessionPort observedSessions;
        private IVoiceChatPort observedVoice;
        private bool panelOpen;

        private void Start()
        {
            BuildIfNeeded();
            TryBindServices();
            Refresh();
        }

        private void OnEnable()
        {
            if (panel != null)
            {
                TryBindServices();
                Refresh();
            }
        }

        private void Update()
        {
            // Services are created in Awake on persistent scene objects. Rebind only when
            // their registry instances change (for example after a scene reload).
            if (!ReferenceEquals(observedSessions, OnlineServices.Sessions) ||
                !ReferenceEquals(observedVoice, OnlineServices.Voice))
            {
                TryBindServices();
                Refresh();
            }
        }

        private void BuildIfNeeded()
        {
            if (panel != null) return;
            canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                var canvasObject = new GameObject("OnlineCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                var scaler = canvasObject.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.matchWidthOrHeight = 0.5f;
            }

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Transform safeRoot = SafeAreaFitter.GetOrCreate(canvas.transform);
            if (safeRoot == null) safeRoot = canvas.transform;
            openButton = CreateButton("OnlineToggle", safeRoot, "ONLINE", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-90f, -48f), new Vector2(150f, 64f), font, OnTogglePanel);
            panel = CreatePanel("OnlinePanel", safeRoot, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-16f, -80f), new Vector2(370f, 485f));
            panel.SetActive(false);

            CreateLabel("Title", panel.transform, "ĐẢO SƯƠNG · CHƠI CÙNG BẠN", 22, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, -14f), new Vector2(338f, 34f), font, TextAnchor.MiddleLeft);
            statusText = CreateLabel("Status", panel.transform, "Chưa kết nối", 16, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, -52f), new Vector2(338f, 44f), font, TextAnchor.MiddleLeft);
            nameInput = CreateInput("DisplayName", panel.transform, "Tên hiển thị", 14, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, -104f), new Vector2(338f, 38f), font);
            nameInput.text = PlayerPrefs.GetString("dao_suong_display_name", "NguoiChoi");
            nameInput.characterLimit = 24;
            nameInput.onEndEdit.AddListener(value => PlayerPrefs.SetString("dao_suong_display_name", OnlineSessionRules.NormalizeDisplayName(value)));

            joinCodeInput = CreateInput("JoinCode", panel.transform, "Mã phòng bạn bè", 16, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, -150f), new Vector2(338f, 42f), font);
            joinCodeInput.characterLimit = 24;
            createButton = CreateButton("CreateRoom", panel.transform, "TẠO PHÒNG RIÊNG", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, -202f), new Vector2(160f, 42f), font, OnCreateRoom);
            joinButton = CreateButton("JoinRoom", panel.transform, "VÀO PHÒNG", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(194f, -202f), new Vector2(160f, 42f), font, OnJoinRoom);

            codeText = CreateLabel("RoomCode", panel.transform, "Mã mời: —", 18, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, -252f), new Vector2(220f, 38f), font, TextAnchor.MiddleLeft);
            copyButton = CreateButton("CopyCode", panel.transform, "SAO CHÉP", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-92f, -252f), new Vector2(92f, 38f), font, OnCopyCode);
            // PTT lives outside the collapsible settings panel so it remains reachable
            // during normal gameplay after the player closes the ONLINE panel.
            pttButton = CreateButton("PushToTalk", safeRoot, "GIỮ ĐỂ NÓI",
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-92f, -122f),
                new Vector2(170f, 54f), font, null).gameObject.AddComponent<VoicePushToTalkButton>();
            pttButton.SetLabel("GIỮ ĐỂ NÓI");
            pttButton.gameObject.SetActive(false);
            muteButton = CreateButton("MuteMic", panel.transform, "TẮT MICRO", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, -364f), new Vector2(160f, 40f), font, OnToggleMute);
            leaveButton = CreateButton("LeaveRoom", panel.transform, "RỜI PHÒNG", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(194f, -364f), new Vector2(160f, 40f), font, OnLeaveRoom);

            CreateLabel("Privacy", panel.transform, "Micro chỉ truyền khi giữ nút nói. Tắt tiếng bất cứ lúc nào.", 12, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, -420f), new Vector2(338f, 36f), font, TextAnchor.MiddleLeft);

            // Keep the toggle above the panel so the user can always close it.
            openButton.transform.SetAsLastSibling();
        }

        private void TryBindServices()
        {
            if (!ReferenceEquals(observedSessions, OnlineServices.Sessions))
            {
                if (observedSessions != null) observedSessions.Changed -= Refresh;
                observedSessions = OnlineServices.Sessions;
                if (observedSessions != null) observedSessions.Changed += Refresh;
            }
            if (!ReferenceEquals(observedVoice, OnlineServices.Voice))
            {
                if (observedVoice != null) observedVoice.Changed -= Refresh;
                observedVoice = OnlineServices.Voice;
                if (observedVoice != null) observedVoice.Changed += Refresh;
            }
        }

        private void Refresh()
        {
            if (statusText == null) return;
            string sessionStatus = OnlineServices.Sessions != null ? OnlineServices.Sessions.Status : "Session service chưa sẵn sàng";
            string voiceStatus = OnlineServices.Voice != null ? OnlineServices.Voice.Status : "Vivox chưa được gắn";
            statusText.text = sessionStatus + "\n" + voiceStatus;
            string code = OnlineServices.Sessions != null ? OnlineServices.Sessions.JoinCode : string.Empty;
            codeText.text = string.IsNullOrEmpty(code) ? "Mã mời: —" : "Mã mời: " + code;
            bool busy = OnlineServices.Sessions != null && OnlineServices.Sessions.IsBusy;
            bool hasSession = OnlineServices.Sessions != null && OnlineServices.Sessions.HasSession;
            createButton.interactable = !busy && !hasSession;
            joinButton.interactable = !busy && !hasSession;
            leaveButton.interactable = hasSession && !busy;
            copyButton.interactable = !string.IsNullOrEmpty(code);
            if (muteButton != null)
            {
                muteButton.interactable = OnlineServices.Voice != null && OnlineServices.Voice.IsInChannel;
                SetButtonLabel(muteButton, OnlineServices.Voice != null && OnlineServices.Voice.IsMuted ? "BẬT MICRO" : "TẮT MICRO");
            }
            if (pttButton != null)
            {
                bool inVoice = OnlineServices.Voice != null && OnlineServices.Voice.IsInChannel;
                bool voiceAvailable = !panelOpen && inVoice;
                if (pttButton.gameObject.activeSelf != voiceAvailable)
                    pttButton.gameObject.SetActive(voiceAvailable);
                Button pttControl = pttButton.GetComponent<Button>();
                bool muted = OnlineServices.Voice != null && OnlineServices.Voice.IsMuted;
                if (pttControl != null) pttControl.interactable = !muted;
                pttButton.SetLabel(muted ? "MIC ĐÃ TẮT" :
                    (OnlineServices.Voice != null && OnlineServices.Voice.IsTransmitting ? "ĐANG NÓI…" : "GIỮ ĐỂ NÓI"));
            }
        }

        private string DisplayName => OnlineSessionRules.NormalizeDisplayName(nameInput != null ? nameInput.text : "NguoiChoi");

        private async void OnCreateRoom()
        {
            if (OnlineServices.Sessions == null) { SetStatus("Thiếu NetworkSessionManager."); return; }
            PlayerPrefs.SetString("dao_suong_display_name", DisplayName);
            try { await OnlineServices.Sessions.CreatePrivateRoomAsync(4, DisplayName); }
            catch (Exception ex) { SetStatus(ex.Message); }
        }

        private async void OnJoinRoom()
        {
            if (OnlineServices.Sessions == null) { SetStatus("Thiếu NetworkSessionManager."); return; }
            PlayerPrefs.SetString("dao_suong_display_name", DisplayName);
            try { await OnlineServices.Sessions.JoinRoomByCodeAsync(joinCodeInput.text, DisplayName); }
            catch (Exception ex) { SetStatus(ex.Message); }
        }

        private async void OnLeaveRoom()
        {
            if (OnlineServices.Sessions == null) return;
            await OnlineServices.Sessions.LeaveRoomAsync();
        }

        private async void OnToggleMute()
        {
            if (OnlineServices.Voice == null) return;
            await OnlineServices.Voice.SetMutedAsync(!OnlineServices.Voice.IsMuted);
            Refresh();
        }

        private void OnCopyCode()
        {
            string code = OnlineServices.Sessions != null ? OnlineServices.Sessions.JoinCode : string.Empty;
            if (!string.IsNullOrEmpty(code)) GUIUtility.systemCopyBuffer = code;
            SetStatus(string.IsNullOrEmpty(code) ? "Chưa có mã phòng." : "Đã sao chép mã phòng.");
        }

        private void OnTogglePanel()
        {
            SetPanelOpen(panel != null && !panel.activeSelf);
        }

        private void SetPanelOpen(bool open)
        {
            if (panel == null) return;
            panelOpen = open;
            panel.SetActive(open);
            if (open)
            {
                GameplayInputGate.Acquire(this);
                if (pttButton != null) pttButton.ReleaseForOwnerDisable();
            }
            else GameplayInputGate.Release(this);
            Refresh();
        }

        private void OnDisable()
        {
            if (panelOpen)
            {
                panelOpen = false;
                if (panel != null) panel.SetActive(false);
                GameplayInputGate.Release(this);
            }
            if (pttButton != null)
            {
                pttButton.ReleaseForOwnerDisable();
                pttButton.gameObject.SetActive(false);
            }
        }

        private void SetStatus(string message)
        {
            if (statusText != null) statusText.text = message;
        }

        private void OnDestroy()
        {
            GameplayInputGate.Release(this);
            if (observedSessions != null) observedSessions.Changed -= Refresh;
            if (observedVoice != null) observedVoice.Changed -= Refresh;
        }

        private static GameObject CreatePanel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin; rect.anchorMax = anchorMax; rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = position; rect.sizeDelta = size;
            go.GetComponent<Image>().color = new Color(0.055f, 0.075f, 0.09f, 0.94f);
            return go;
        }

        private static Text CreateLabel(string name, Transform parent, string value, int size, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 dimensions, Font font, TextAnchor alignment)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin; rect.anchorMax = anchorMax; rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position; rect.sizeDelta = dimensions;
            var text = go.GetComponent<Text>();
            text.font = font; text.text = value; text.fontSize = size; text.color = Color.white; text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        private static Button CreateButton(string name, Transform parent, string label, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 dimensions, Font font, UnityEngine.Events.UnityAction callback)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin; rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(anchorMin.x == 1f ? 1f : 0f, 1f);
            rect.anchoredPosition = position; rect.sizeDelta = dimensions;
            Image image = go.GetComponent<Image>(); image.color = new Color(0.12f, 0.34f, 0.38f, 1f);
            Button button = go.GetComponent<Button>(); button.targetGraphic = image;
            if (callback != null) button.onClick.AddListener(callback);
            var textGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(go.transform, false);
            RectTransform textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one; textRect.offsetMin = Vector2.zero; textRect.offsetMax = Vector2.zero;
            Text text = textGo.GetComponent<Text>(); text.font = font; text.text = label; text.fontSize = 15; text.color = Color.white; text.alignment = TextAnchor.MiddleCenter; text.raycastTarget = false;
            return button;
        }

        private static InputField CreateInput(string name, Transform parent, string placeholder, int fontSize, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 dimensions, Font font)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(InputField));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>(); rect.anchorMin = anchorMin; rect.anchorMax = anchorMax; rect.pivot = new Vector2(0f, 1f); rect.anchoredPosition = position; rect.sizeDelta = dimensions;
            Image image = go.GetComponent<Image>(); image.color = new Color(0.15f, 0.18f, 0.20f, 1f);
            var textGo = new GameObject("Text", typeof(RectTransform), typeof(Text)); textGo.transform.SetParent(go.transform, false);
            RectTransform textRect = textGo.GetComponent<RectTransform>(); textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one; textRect.offsetMin = new Vector2(10f, 4f); textRect.offsetMax = new Vector2(-10f, -4f);
            Text text = textGo.GetComponent<Text>(); text.font = font; text.fontSize = fontSize; text.color = Color.white; text.alignment = TextAnchor.MiddleLeft;
            var placeGo = new GameObject("Placeholder", typeof(RectTransform), typeof(Text)); placeGo.transform.SetParent(go.transform, false);
            RectTransform placeRect = placeGo.GetComponent<RectTransform>(); placeRect.anchorMin = Vector2.zero; placeRect.anchorMax = Vector2.one; placeRect.offsetMin = new Vector2(10f, 4f); placeRect.offsetMax = new Vector2(-10f, -4f);
            Text placeText = placeGo.GetComponent<Text>(); placeText.font = font; placeText.fontSize = fontSize; placeText.color = new Color(0.68f, 0.72f, 0.74f); placeText.text = placeholder; placeText.alignment = TextAnchor.MiddleLeft;
            var field = go.GetComponent<InputField>(); field.textComponent = text; field.placeholder = placeText; field.targetGraphic = image;
            return field;
        }

        private static void SetButtonLabel(Button button, string label)
        {
            Text text = button.GetComponentInChildren<Text>();
            if (text != null) text.text = label;
        }
    }

    public sealed class VoicePushToTalkButton : MonoBehaviour, UnityEngine.EventSystems.IPointerDownHandler, UnityEngine.EventSystems.IPointerUpHandler, UnityEngine.EventSystems.IPointerExitHandler
    {
        private Text label;
        private bool pressed;

        public void SetLabel(string value)
        {
            if (label == null) label = GetComponentInChildren<Text>();
            if (label != null && !pressed) label.text = value;
        }

        public async void OnPointerDown(UnityEngine.EventSystems.PointerEventData eventData)
        {
            IVoiceChatPort voice = OnlineServices.Voice;
            if (pressed || voice == null || !voice.IsInChannel || voice.IsMuted) return;
            pressed = true;
            if (label == null) label = GetComponentInChildren<Text>();
            if (label != null) label.text = "ĐANG NÓI…";
            try
            {
                await voice.SetPushToTalkAsync(true);
            }
            catch (Exception ex)
            {
                pressed = false;
                Debug.LogException(ex, this);
                SetLabel("MIC LỖI");
            }
        }

        public async void OnPointerUp(UnityEngine.EventSystems.PointerEventData eventData) => await ReleaseAsync();
        public async void OnPointerExit(UnityEngine.EventSystems.PointerEventData eventData) => await ReleaseAsync();

        private async System.Threading.Tasks.Task ReleaseAsync()
        {
            if (!pressed) return;
            pressed = false;
            try
            {
                if (OnlineServices.Voice != null) await OnlineServices.Voice.SetPushToTalkAsync(false);
            }
            catch (Exception ex) { Debug.LogException(ex, this); }
        }

        public void ReleaseForOwnerDisable() => _ = ReleaseAsync();

        private void OnApplicationPause(bool paused)
        {
            if (paused) _ = ReleaseAsync();
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused) _ = ReleaseAsync();
        }

        private void OnDisable() => _ = ReleaseAsync();
    }
}
