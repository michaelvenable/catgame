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

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static async void Initialize()
        {
            IsReady = false;
            try
            {
                await UnityServices.InitializeAsync();
                if (!AuthenticationService.Instance.IsSignedIn)
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();

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
