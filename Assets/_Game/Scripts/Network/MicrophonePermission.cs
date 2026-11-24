using System.Threading.Tasks;
using UnityEngine;

#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

namespace Game.Network
{
    public static class MicrophonePermission
    {
        public static bool HasPermission
        {
            get
            {
#if UNITY_EDITOR
                return true;
#elif UNITY_ANDROID
                return Permission.HasUserAuthorizedPermission(Permission.Microphone);
#elif UNITY_IOS
                return Application.HasUserAuthorization(UserAuthorization.Microphone);
#else
                return true;
#endif
            }
        }

        public static async Task<bool> RequestAsync()
        {
#if UNITY_EDITOR
            return true;
#elif UNITY_ANDROID
            if (Permission.HasUserAuthorizedPermission(Permission.Microphone))
                return true;

            var completion = new TaskCompletionSource<bool>();
            var callbacks = new PermissionCallbacks();
            callbacks.PermissionGranted += _ => completion.TrySetResult(true);
            callbacks.PermissionDenied += _ => completion.TrySetResult(false);
            callbacks.PermissionDeniedAndDontAskAgain += _ => completion.TrySetResult(false);
            Permission.RequestUserPermission(Permission.Microphone, callbacks);

            Task completed = await Task.WhenAny(completion.Task, Task.Delay(20000));
            return completed == completion.Task && completion.Task.Result;
#elif UNITY_IOS
            if (Application.HasUserAuthorization(UserAuthorization.Microphone))
                return true;

            var request = Application.RequestUserAuthorization(UserAuthorization.Microphone);
            while (!request.isDone)
                await Task.Yield();
            return Application.HasUserAuthorization(UserAuthorization.Microphone);
#else
            return true;
#endif
        }
    }
}
