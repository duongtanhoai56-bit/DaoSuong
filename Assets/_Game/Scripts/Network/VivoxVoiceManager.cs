using System;
using System.Threading;
using System.Threading.Tasks;
using Game.Core;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Vivox;
using UnityEngine;

namespace Game.Network
{
    /// <summary>
    /// Vivox group voice adapter. Microphone transmission is opt-in and uses push-to-talk.
    /// Channel lifecycle and transmit-state changes are serialized to avoid press/release races.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class VivoxVoiceManager : MonoBehaviour, IVoiceChatPort
    {
        private static VivoxVoiceManager instance;
        private readonly SemaphoreSlim channelGate = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim transmissionGate = new SemaphoreSlim(1, 1);

        private bool initialized;
        private bool loggedIn;
        private bool inChannel;
        private bool muted;
        private bool microphonePermissionGranted;
        private bool transmitting;
        private bool eventsSubscribed;
        private bool channelJoinPending;
        private bool channelLeaving;
        private bool appInBackground;
        private bool pushToTalkRequested;
        private string channelName = string.Empty;
        private string status = "Voice chưa khởi tạo";
        private TaskCompletionSource<bool> channelJoinCompletion;
        private TaskCompletionSource<bool> channelLeaveCompletion;

        public bool IsReady => loggedIn;
        public bool IsInChannel => inChannel;
        // A denied OS microphone permission is an effective mute, but does not prevent
        // joining the audio channel and listening to teammates.
        public bool IsMuted => muted || !microphonePermissionGranted;
        public bool IsTransmitting => transmitting;
        public string Status => status;
        public event Action Changed;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            OnlineServices.Voice = this;
            OnlineServices.NotifyChanged();
        }

        public async Task<bool> JoinSessionChannelAsync(string sessionId, string displayName)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                SetStatus("Không có Session ID cho voice.");
                return false;
            }

            string requestedChannel;
            try { requestedChannel = OnlineSessionRules.BuildVoiceChannelName(sessionId); }
            catch (ArgumentException ex)
            {
                SetStatus(ex.Message);
                return false;
            }

            await channelGate.WaitAsync();
            try
            {
                if (inChannel && string.Equals(channelName, requestedChannel, StringComparison.Ordinal))
                    return true;

                if (inChannel || channelJoinPending)
                    await LeaveChannelInternalAsync();

                SetStatus("Đang xin quyền microphone...");
                microphonePermissionGranted = await MicrophonePermission.RequestAsync();
                // RECORD_AUDIO is needed to transmit microphone input, not to receive
                // the group channel. Let players join in listen-only mode when denied.
                await EnsureVivoxLoginAsync(displayName);
                try
                {
                    if (!microphonePermissionGranted || muted)
                        VivoxService.Instance.MuteInputDevice();
                    else
                        VivoxService.Instance.UnmuteInputDevice();
                }
                catch (Exception deviceException)
                {
                    Debug.LogWarning("[Vivox] Input device permission state: " + deviceException.Message, this);
                }
                channelName = requestedChannel;
                channelJoinCompletion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                channelJoinPending = true;
                channelLeaving = false;
                pushToTalkRequested = false;
                await ForceSilenceAsync();
                SetStatus("Đang tham gia kênh voice nhóm...");

                await VivoxService.Instance.JoinGroupChannelAsync(channelName, ChatCapability.AudioOnly);
                Task completed = await Task.WhenAny(channelJoinCompletion.Task, Task.Delay(15000));
                if (completed != channelJoinCompletion.Task || !inChannel)
                {
                    await LeaveChannelInternalAsync();
                    SetStatus("Không nhận được xác nhận tham gia voice trong 15 giây.");
                    return false;
                }

                // Voice starts silent. Preserve the player's mute preference across room changes;
                // the player must press and hold PTT and must not be silently unmuted on rejoin.
                pushToTalkRequested = false;
                await ApplyTransmissionStateAsync();
                SetStatus(microphonePermissionGranted
                    ? "Đã vào voice nhóm. Giữ nút mic để nói."
                    : "Đã vào voice nhóm ở chế độ chỉ nghe. Cấp quyền mic để nói.");
                return true;
            }
            catch (Exception ex)
            {
                // Join APIs can fail after the service has partially accepted the request.
                // Attempt to leave that channel before clearing our local state.
                try
                {
                    if (!string.IsNullOrEmpty(channelName) && (channelJoinPending || inChannel))
                        await LeaveChannelInternalAsync();
                }
                catch (Exception cleanupException)
                {
                    Debug.LogWarning("[Vivox] Join rollback: " + cleanupException.Message, this);
                }
                channelJoinPending = false;
                channelLeaving = false;
                inChannel = false;
                pushToTalkRequested = false;
                transmitting = false;
                channelName = string.Empty;
                Debug.LogException(ex, this);
                SetStatus("Voice lỗi: " + ex.Message);
                return false;
            }
            finally
            {
                channelGate.Release();
            }
        }

        private async Task EnsureVivoxLoginAsync(string displayName)
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                var options = new InitializationOptions().SetEnvironmentName(OnlineServices.EnvironmentName);
                await UnityServices.InitializeAsync(options);
            }

            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();

            if (!initialized)
            {
                await VivoxService.Instance.InitializeAsync();
                initialized = true;
            }

            if (!eventsSubscribed)
            {
                VivoxService.Instance.ChannelJoined += OnChannelJoined;
                VivoxService.Instance.ChannelLeft += OnChannelLeft;
                eventsSubscribed = true;
            }

            if (!loggedIn)
            {
                string safeName = OnlineSessionRules.NormalizeDisplayName(displayName);
                await VivoxService.Instance.LoginAsync(new LoginOptions
                {
                    DisplayName = safeName,
                    // The SDK otherwise swaps transmission to the most recently joined channel.
                    // We explicitly select Single only while the PTT button is held.
                    DisableAutomaticChannelTransmissionSwap = true
                });
                loggedIn = true;
            }
        }

        private void OnChannelJoined(string joinedChannel)
        {
            if (!string.Equals(joinedChannel, channelName, StringComparison.Ordinal)) return;
            inChannel = true;
            channelJoinPending = false;
            channelJoinCompletion?.TrySetResult(true);
            SetStatus("Voice đã kết nối");
        }

        private void OnChannelLeft(string leftChannel)
        {
            if (!string.Equals(leftChannel, channelName, StringComparison.Ordinal)) return;
            inChannel = false;
            pushToTalkRequested = false;
            transmitting = false;
            channelLeaveCompletion?.TrySetResult(true);
            SetStatus("Đã rời voice nhóm");
        }

        public async Task LeaveSessionChannelAsync()
        {
            await channelGate.WaitAsync();
            try { await LeaveChannelInternalAsync(); }
            finally { channelGate.Release(); }
        }

        private async Task LeaveChannelInternalAsync()
        {
            // Close the transmit gate before awaiting the SDK, so a late PTT pointer event
            // cannot re-enable the microphone between silence and channel leave.
            channelLeaving = true;
            pushToTalkRequested = false;
            try
            {
                await ForceSilenceAsync();
                if ((inChannel || channelJoinPending) && !string.IsNullOrEmpty(channelName) && loggedIn)
                {
                    channelLeaveCompletion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                    await VivoxService.Instance.LeaveChannelAsync(channelName);
                    await Task.WhenAny(channelLeaveCompletion.Task, Task.Delay(5000));
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[Vivox] Leave channel: " + ex.Message, this);
            }
            finally
            {
                inChannel = false;
                channelJoinPending = false;
                channelLeaving = false;
                transmitting = false;
                channelName = string.Empty;
                SetStatus(loggedIn ? "Voice sẵn sàng; chưa vào kênh." : "Voice chưa đăng nhập");
            }
        }

        public async Task SetMutedAsync(bool value)
        {
            if (!loggedIn) return;

            if (!value && !microphonePermissionGranted)
            {
                microphonePermissionGranted = await MicrophonePermission.RequestAsync();
                if (!microphonePermissionGranted)
                {
                    pushToTalkRequested = false;
                    SetStatus("Chưa có quyền microphone; bạn vẫn nghe được voice nhóm.");
                    return;
                }
            }

            // Update intent before waiting for the SDK gate. A pending PTT press will
            // therefore observe the muted state and cannot re-enable transmission.
            muted = value;
            if (value) pushToTalkRequested = false;

            await transmissionGate.WaitAsync();
            try
            {
                bool shouldTransmit = CanTransmit();
                if (shouldTransmit)
                    await VivoxService.Instance.SetChannelTransmissionModeAsync(TransmissionMode.Single, channelName);
                else
                    await VivoxService.Instance.SetChannelTransmissionModeAsync(TransmissionMode.None);

                if (value) VivoxService.Instance.MuteInputDevice();
                else VivoxService.Instance.UnmuteInputDevice();

                transmitting = shouldTransmit;
                SetStatus(value ? "Microphone đã tắt" : "Microphone bật; giữ nút để nói");
            }
            catch (Exception ex)
            {
                transmitting = false;
                SetStatus("Không đổi được trạng thái microphone: " + ex.Message);
                Debug.LogException(ex, this);
            }
            finally
            {
                transmissionGate.Release();
            }
        }

        public async Task SetPushToTalkAsync(bool pressed)
        {
            pushToTalkRequested = pressed && CanEnterTransmitMode();
            await ApplyTransmissionStateAsync();
        }

        private async Task ApplyTransmissionStateAsync()
        {
            if (!loggedIn) return;
            await transmissionGate.WaitAsync();
            try
            {
                bool shouldTransmit = CanTransmit() && pushToTalkRequested;
                if (shouldTransmit)
                    await VivoxService.Instance.SetChannelTransmissionModeAsync(TransmissionMode.Single, channelName);
                else
                    await VivoxService.Instance.SetChannelTransmissionModeAsync(TransmissionMode.None);

                // Re-evaluate after await: a release/mute/background event can arrive while
                // Vivox is processing the earlier SDK request.
                transmitting = CanTransmit() && pushToTalkRequested;
                SetStatus(transmitting ? "Đang truyền giọng nói..." : "Voice đang chờ. Giữ nút mic để nói.");
            }
            catch (Exception ex)
            {
                transmitting = false;
                pushToTalkRequested = false;
                SetStatus("PTT lỗi: " + ex.Message);
                Debug.LogException(ex, this);
            }
            finally
            {
                transmissionGate.Release();
            }
        }

        private async Task ForceSilenceAsync()
        {
            if (!loggedIn) return;
            await transmissionGate.WaitAsync();
            try
            {
                await VivoxService.Instance.SetChannelTransmissionModeAsync(TransmissionMode.None);
                transmitting = false;
            }
            finally
            {
                transmissionGate.Release();
            }
        }

        private bool CanEnterTransmitMode() => loggedIn && inChannel && !channelLeaving && !muted &&
                                                microphonePermissionGranted && !appInBackground &&
                                                !string.IsNullOrEmpty(channelName);

        private bool CanTransmit() => VoiceTransmissionRules.CanTransmit(loggedIn, inChannel, channelLeaving,
            muted, microphonePermissionGranted, appInBackground, pushToTalkRequested) && !string.IsNullOrEmpty(channelName);

        private void OnApplicationPause(bool paused) => SetApplicationBackground(paused);
        private void OnApplicationFocus(bool focused) => SetApplicationBackground(!focused);

        private void SetApplicationBackground(bool background)
        {
            appInBackground = background;
            if (background)
            {
                pushToTalkRequested = false;
                _ = ApplyTransmissionStateAsync();
                return;
            }

            // Permission can be revoked or granted in OS settings while the app is
            // backgrounded. Reconcile the input device state without prompting again.
            microphonePermissionGranted = MicrophonePermission.HasPermission;
            if (!microphonePermissionGranted)
                pushToTalkRequested = false;
            _ = RefreshInputDeviceAfterResumeAsync();
        }

        private async Task RefreshInputDeviceAfterResumeAsync()
        {
            try
            {
                if (loggedIn)
                {
                    if (!microphonePermissionGranted || muted)
                        VivoxService.Instance.MuteInputDevice();
                    else
                        VivoxService.Instance.UnmuteInputDevice();
                }
                await ApplyTransmissionStateAsync();
            }
            catch (Exception ex)
            {
                pushToTalkRequested = false;
                transmitting = false;
                Debug.LogWarning("[Vivox] Could not restore microphone state: " + ex.Message, this);
            }
        }

        private void SetStatus(string value)
        {
            status = value ?? string.Empty;
            Changed?.Invoke();
            OnlineServices.NotifyChanged();
        }

        private void OnDestroy()
        {
            pushToTalkRequested = false;
            if (eventsSubscribed && VivoxService.Instance != null)
            {
                VivoxService.Instance.ChannelJoined -= OnChannelJoined;
                VivoxService.Instance.ChannelLeft -= OnChannelLeft;
            }
            if (ReferenceEquals(instance, this)) instance = null;
            if (ReferenceEquals(OnlineServices.Voice, this)) OnlineServices.Voice = null;
            OnlineServices.NotifyChanged();
        }
    }
}
