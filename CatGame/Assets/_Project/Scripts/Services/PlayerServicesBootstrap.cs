using System;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

namespace CatGame
{
    public static class PlayerServicesBootstrap
    {
        public static bool IsReady { get; private set; }
        public static event Action Ready;

        static int sessionId;

        // A sign-in from an earlier play session can resume after play mode exits or restarts.
        static bool IsStale(int session) => session != sessionId || !Application.isPlaying;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static async void Initialize()
        {
            // Domain reload is disabled in this project, so statics survive between play sessions.
            IsReady = false;
            Ready = null;
            int session = ++sessionId;
            try
            {
                await UnityServices.InitializeAsync();
                if (IsStale(session))
                    return;
                if (!AuthenticationService.Instance.IsSignedIn)
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                if (IsStale(session))
                    return;

                IsReady = true;
                Debug.Log($"[Services] Signed in anonymously as {AuthenticationService.Instance.PlayerId}");
                Ready?.Invoke();
            }
            catch (Exception e)
            {
                // Offline or services unavailable: the game keeps running with local-only state.
                Debug.LogWarning($"[Services] Sign-in skipped: {e.Message}");
            }
        }
    }
}
