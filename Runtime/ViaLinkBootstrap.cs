using UnityEngine;

namespace ViaLink
{
    internal static class ViaLinkBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EnsureSingleton()
        {
            if (ViaLinkSDK.Instance != null)
                return;

            var go = new GameObject("ViaLinkSDK");
            Object.DontDestroyOnLoad(go);
            go.AddComponent<ViaLinkSDK>();
        }
    }
}
