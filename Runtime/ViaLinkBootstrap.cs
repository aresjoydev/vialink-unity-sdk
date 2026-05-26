using UnityEngine;
using UnityEngine.Scripting;

namespace ViaLink
{
    // [Preserve] + link.xml(ViaLink 어셈블리 전체 보호) 이중 안전망.
    // 외부 참조가 0 인 internal 클래스라 IL2CPP 가 제거할 수 있어 명시적으로 보호한다.
    [Preserve]
    internal static class ViaLinkBootstrap
    {
        [Preserve]
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
