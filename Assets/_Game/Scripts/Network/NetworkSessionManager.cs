using System;
using System.Threading.Tasks;
using Game.Core;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;

namespace Game.Network
{
    [DisallowMultipleComponent]
    public sealed class NetworkSessionManager : MonoBehaviour, IOnlineSessionPort
    {
        private static NetworkSessionManager instance;
        [SerializeField] private string environmentName = "development";
        private ISession session;
        private bool busy;
        private string joinCode = string.Empty;
        private string status = "Chưa kết nối";
        private NetworkManager subscribedNetworkManager;

        public bool IsBusy => busy;
        public bool HasSession => session != null;
        public bool IsConnected => session != null && NetworkManager.Singleton != null &&
                                   NetworkManager.Singleton.IsListening &&
                                   (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsConnectedClient);
        public string JoinCode => joinCode;
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
            OnlineServices.EnvironmentName = OnlineSessionRules.NormalizeEnvironment(environmentName);
            DontDestroyOnLoad(gameObject);
            OnlineServices.Sessions = this;
            OnlineServices.NotifyChanged();
        }

        private void Start() => SubscribeNetworkEvents();

        public async Task CreatePrivateRoomAsync(int maxPlayers, string displayName)
        {
            displayName = OnlineSessionRules.NormalizeDisplayName(displayName);
            if (busy) return;
            if (session != null)
                throw new InvalidOperationException("Bạn đang ở trong một phòng. Hãy rời phòng trước.");

            busy = true;
            status = "Đang khởi tạo dịch vụ online...";
            RaiseChanged();
            try
            {
                await EnsureServicesAndAuthenticationAsync();
                EnsureNetworkManagerReady();

                var options = new SessionOptions
                {
                    Name = "Dao Suong",
                    MaxPlayers = OnlineSessionRules.ClampPlayerCount(maxPlayers),
                    IsPrivate = true
                }.WithRelayNetwork();

                status = "Đang tạo phòng và kết nối Relay...";
                RaiseChanged();
                session = await MultiplayerService.Instance.CreateSessionAsync(options);
                joinCode = session.Code ?? string.Empty;
                await WaitForNetworkAsync();
                status = "Đã tạo phòng. Gửi mã cho bạn bè.";
                RaiseChanged();

                // A voice failure must not destroy a healthy game session.
                await TryJoinVoiceAsync(displayName);
            }
            catch (Exception ex)
            {
                status = "Không tạo được phòng: " + ex.Message;
                Debug.LogException(ex, this);
                await CleanupFailedSessionAsync();
                RaiseChanged();
                throw;
            }
            finally
            {
                busy = false;
                RaiseChanged();
            }
        }

        public async Task JoinRoomByCodeAsync(string rawCode, string displayName)
        {
            displayName = OnlineSessionRules.NormalizeDisplayName(displayName);
            if (busy) return;
            if (session != null)
                throw new InvalidOperationException("Bạn đang ở trong một phòng. Hãy rời phòng trước.");
            if (!OnlineSessionRules.TryNormalizeJoinCode(rawCode, out string code))
                throw new ArgumentException("Mã phòng phải gồm 4–12 chữ cái hoặc chữ số.", nameof(rawCode));

            busy = true;
            status = "Đang tìm phòng...";
            RaiseChanged();
            try
            {
                await EnsureServicesAndAuthenticationAsync();
                EnsureNetworkManagerReady();

                session = await MultiplayerService.Instance.JoinSessionByCodeAsync(code);
                joinCode = session.Code ?? code;
                status = "Đã vào phòng. Đang đồng bộ kết nối...";
                RaiseChanged();
                await WaitForNetworkAsync();
                status = "Đã kết nối với phòng bạn bè.";
                RaiseChanged();

                await TryJoinVoiceAsync(displayName);
            }
            catch (Exception ex)
            {
                status = "Không vào được phòng: " + ex.Message;
                Debug.LogException(ex, this);
                await CleanupFailedSessionAsync();
                RaiseChanged();
                throw;
            }
            finally
            {
                busy = false;
                RaiseChanged();
            }
        }

        public async Task LeaveRoomAsync()
        {
            if (busy) return;
            if (session == null)
            {
                status = "Chưa kết nối";
                joinCode = string.Empty;
                RaiseChanged();
                return;
            }

            busy = true;
            status = "Đang rời phòng...";
            RaiseChanged();
            try
            {
                if (OnlineServices.Voice != null)
                    await OnlineServices.Voice.LeaveSessionChannelAsync();

                ISession oldSession = session;
                session = null;
                joinCode = string.Empty;
                await oldSession.LeaveAsync();
                status = "Đã rời phòng";
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[Online] Leave session: " + ex.Message, this);
                if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
                    NetworkManager.Singleton.Shutdown();
                session = null;
                joinCode = string.Empty;
                status = "Đã ngắt kết nối cục bộ; dịch vụ có thể cần khôi phục.";
            }
            finally
            {
                busy = false;
                RaiseChanged();
            }
        }

        private async Task EnsureServicesAndAuthenticationAsync()
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                var options = new InitializationOptions().SetEnvironmentName(OnlineServices.EnvironmentName);
                await UnityServices.InitializeAsync(options);
            }

            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        private void EnsureNetworkManagerReady()
        {
            if (NetworkManager.Singleton == null)
                throw new InvalidOperationException("Scene chưa có NetworkManager. Chạy Game → Giai đoạn 7 → Nâng cấp Scene.");
            if (NetworkManager.Singleton.IsListening)
                throw new InvalidOperationException("NetworkManager đã chạy một phiên khác.");

            NetworkManager manager = NetworkManager.Singleton;
            if (manager.transform.parent != null)
                manager.transform.SetParent(null);
            DontDestroyOnLoad(manager.gameObject);

            if (manager.NetworkConfig == null || !(manager.NetworkConfig.NetworkTransport is UnityTransport))
                throw new InvalidOperationException("NetworkManager chưa cấu hình UnityTransport. Chạy công cụ nâng cấp Scene Giai đoạn 7.");

            GameObject playerPrefab = manager.NetworkConfig.PlayerPrefab;
            if (playerPrefab == null || playerPrefab.GetComponent<NetworkObject>() == null ||
                playerPrefab.GetComponent<NetworkTransform>() == null || playerPrefab.GetComponent<NetworkPlayerAvatar>() == null)
                throw new InvalidOperationException("PlayerPrefab phải có NetworkObject, NetworkTransform và NetworkPlayerAvatar. Hãy tạo lại Network Player Prefab.");

            SubscribeNetworkEvents();
        }

        private async Task WaitForNetworkAsync()
        {
            const int timeoutMilliseconds = 20000;
            const int pollMilliseconds = 50;
            int elapsed = 0;
            while (elapsed < timeoutMilliseconds)
            {
                NetworkManager current = NetworkManager.Singleton;
                if (current != null && current.IsListening && (current.IsHost || current.IsConnectedClient))
                    return;
                await Task.Delay(pollMilliseconds);
                elapsed += pollMilliseconds;
            }

            throw new TimeoutException("Relay/NGO không khởi động trong 20 giây. Kiểm tra NetworkManager, Relay và cấu hình Unity Dashboard.");
        }

        private async Task TryJoinVoiceAsync(string displayName)
        {
            if (OnlineServices.Voice == null || session == null)
            {
                status = "Đã kết nối game; voice chưa được cấu hình.";
                RaiseChanged();
                return;
            }

            bool voiceJoined = await OnlineServices.Voice.JoinSessionChannelAsync(session.Id, displayName);
            status = voiceJoined
                ? "Đã kết nối game và voice nhóm."
                : "Đã kết nối game; voice chưa sẵn sàng: " + OnlineServices.Voice.Status;
            RaiseChanged();
        }

        private async Task CleanupFailedSessionAsync()
        {
            if (OnlineServices.Voice != null)
            {
                try { await OnlineServices.Voice.LeaveSessionChannelAsync(); }
                catch (Exception voiceException) { Debug.LogWarning(voiceException.Message); }
            }

            if (session != null)
            {
                try { await session.LeaveAsync(); }
                catch (Exception leaveException) { Debug.LogWarning(leaveException.Message); }
            }
            session = null;
            joinCode = string.Empty;
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
                NetworkManager.Singleton.Shutdown();
        }

        private void SubscribeNetworkEvents()
        {
            NetworkManager current = NetworkManager.Singleton;
            if (ReferenceEquals(current, subscribedNetworkManager)) return;

            if (subscribedNetworkManager != null)
                subscribedNetworkManager.OnClientDisconnectCallback -= OnClientDisconnected;

            subscribedNetworkManager = current;
            if (subscribedNetworkManager != null)
                subscribedNetworkManager.OnClientDisconnectCallback += OnClientDisconnected;
        }

        private void OnClientDisconnected(ulong clientId)
        {
            if (NetworkManager.Singleton == null || clientId != NetworkManager.Singleton.LocalClientId)
                return;
            if (session == null) return;
            status = "Mất kết nối mạng. Voice đã được ngắt; hãy rời phòng và vào lại.";
            _ = LeaveVoiceAfterDisconnectAsync();
            RaiseChanged();
        }

        private async Task LeaveVoiceAfterDisconnectAsync()
        {
            if (OnlineServices.Voice == null) return;
            try { await OnlineServices.Voice.LeaveSessionChannelAsync(); }
            catch (Exception ex) { Debug.LogWarning("[Online] Voice cleanup after disconnect: " + ex.Message, this); }
        }

        private void RaiseChanged()
        {
            Changed?.Invoke();
            OnlineServices.NotifyChanged();
        }

        private void OnDestroy()
        {
            if (subscribedNetworkManager != null)
                subscribedNetworkManager.OnClientDisconnectCallback -= OnClientDisconnected;
            subscribedNetworkManager = null;
            if (ReferenceEquals(instance, this)) instance = null;
            if (ReferenceEquals(OnlineServices.Sessions, this)) OnlineServices.Sessions = null;
            OnlineServices.NotifyChanged();
        }
    }
}
