namespace Game.Network
{
    /// <summary>Pure privacy gate for Vivox microphone transmission.</summary>
    public static class VoiceTransmissionRules
    {
        public static bool CanTransmit(bool loggedIn, bool inChannel, bool channelLeaving,
            bool muted, bool microphonePermissionGranted, bool appInBackground, bool pushToTalkPressed)
        {
            return loggedIn && inChannel && !channelLeaving && !muted &&
                   microphonePermissionGranted && !appInBackground && pushToTalkPressed;
        }
    }
}
