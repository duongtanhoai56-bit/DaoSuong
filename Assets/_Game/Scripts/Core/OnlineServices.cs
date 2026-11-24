using System;
using System.Threading.Tasks;

namespace Game.Core
{
    /// <summary>Transport-neutral contract for creating and joining a co-op room.</summary>
    public interface IOnlineSessionPort
    {
        bool IsBusy { get; }
        bool HasSession { get; }
        bool IsConnected { get; }
        string JoinCode { get; }
        string Status { get; }
        event Action Changed;
        Task CreatePrivateRoomAsync(int maxPlayers, string displayName);
        Task JoinRoomByCodeAsync(string joinCode, string displayName);
        Task LeaveRoomAsync();
    }

    /// <summary>Transport-neutral voice controls. Voice is independent from game networking.</summary>
    public interface IVoiceChatPort
    {
        bool IsReady { get; }
        bool IsInChannel { get; }
        bool IsMuted { get; }
        bool IsTransmitting { get; }
        string Status { get; }
        event Action Changed;
        Task<bool> JoinSessionChannelAsync(string sessionId, string displayName);
        Task LeaveSessionChannelAsync();
        Task SetMutedAsync(bool muted);
        Task SetPushToTalkAsync(bool pressed);
    }

    public static class OnlineServices
    {
        public static IOnlineSessionPort Sessions { get; set; }
        public static IVoiceChatPort Voice { get; set; }
        public static string EnvironmentName { get; set; } = "development";

        public static event Action Changed;

        public static void NotifyChanged() => Changed?.Invoke();

        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Sessions = null;
            Voice = null;
            EnvironmentName = "development";
            Changed = null;
        }
    }

    public static class OnlineSessionRules
    {
        public const int MinPlayers = 2;
        public const int MaxPlayers = 4;

        public static int ClampPlayerCount(int value)
        {
            if (value < MinPlayers) return MinPlayers;
            if (value > MaxPlayers) return MaxPlayers;
            return value;
        }

        public static bool TryNormalizeJoinCode(string raw, out string normalized)
        {
            normalized = string.Empty;
            if (string.IsNullOrWhiteSpace(raw)) return false;

            var builder = new System.Text.StringBuilder(raw.Length);
            foreach (char c in raw)
            {
                if (char.IsWhiteSpace(c) || c == '-') continue;
                bool valid = (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9');
                if (!valid) return false;
                builder.Append(c);
                if (builder.Length > 12) return false;
            }

            if (builder.Length < 4 || builder.Length > 12) return false;
            normalized = builder.ToString().ToUpperInvariant();
            return true;
        }

        public static string NormalizeEnvironment(string environmentName)
        {
            if (string.IsNullOrWhiteSpace(environmentName)) return "development";
            var builder = new System.Text.StringBuilder(24);
            foreach (char c in environmentName.Trim())
            {
                bool valid = (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') ||
                             (c >= '0' && c <= '9') || c == '-' || c == '_';
                if (!valid) return "development";
                builder.Append(char.ToLowerInvariant(c));
                if (builder.Length >= 24) break;
            }
            return builder.Length == 0 ? "development" : builder.ToString();
        }

        public static string NormalizeDisplayName(string displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName)) return "NguoiChoi";

            var builder = new System.Text.StringBuilder(24);
            foreach (char c in displayName.Trim())
            {
                // Drop control and surrogate code units. Vietnamese names remain intact;
                // excluding half-surrogates avoids malformed UTF-16 reaching Vivox metadata.
                if (char.IsControl(c) || char.IsSurrogate(c)) continue;
                builder.Append(c);
                if (builder.Length >= 24) break;
            }

            string result = builder.ToString().Trim();
            return string.IsNullOrEmpty(result) ? "NguoiChoi" : result;
        }

        public static string BuildVoiceChannelName(string sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
                throw new ArgumentException("Session ID không được để trống.", nameof(sessionId));

            var chars = new System.Text.StringBuilder(sessionId.Length);
            foreach (char c in sessionId)
            {
                if (c >= 'a' && c <= 'z') chars.Append(c);
                else if (c >= 'A' && c <= 'Z') chars.Append(char.ToLowerInvariant(c));
                else if (c >= '0' && c <= '9') chars.Append(c);
            }

            if (chars.Length < 4 || chars.Length > 48)
                throw new ArgumentException("Session ID không hợp lệ.", nameof(sessionId));

            return "island-" + chars;
        }
    }
}
