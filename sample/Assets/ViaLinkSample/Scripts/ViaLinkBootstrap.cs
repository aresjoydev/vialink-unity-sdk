using UnityEngine;
using ViaLink;

namespace ViaLinkSample
{
    /// 앱 시작 시점에 SDK 콜백을 먼저 등록하고, 그 뒤에 SDK를 초기화한다.
    /// 순서가 중요한 이유: Initialize() 가 cold-start 딥링크(Application.absoluteURL)를
    /// 동기적으로 콜백으로 발사할 가능성이 있어, 구독을 먼저 해두어야 누락되지 않음.
    /// (iOS SDK v3.0.13 의 flushPendingDeepLinks 패턴과 동일한 의도)
    public static class ViaLinkBootstrap
    {
        // 샘플 전용 API 키 — slug: /unity
        private const string ApiKey = "0aaa5216b634887f2d524eef2c5853912f89d7f8189b030128e2c767b31a20df";

        // Managed Stripping 으로부터 UnityWebRequest.Result 와 관련 nested types 를
        // 강제로 보존한다. SDK 는 이 type 들을 사용하지만 link.xml 만으로는 일부 경로에서
        // strip 되는 경우가 있어, 사용자 코드 reachability 로 확실히 보존.
        private static readonly object[] _il2cppPreserve = new object[]
        {
            UnityEngine.Networking.UnityWebRequest.Result.InProgress,
            UnityEngine.Networking.UnityWebRequest.Result.Success,
            UnityEngine.Networking.UnityWebRequest.Result.ConnectionError,
            UnityEngine.Networking.UnityWebRequest.Result.ProtocolError,
            UnityEngine.Networking.UnityWebRequest.Result.DataProcessingError,
        };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            // _il2cppPreserve 가 strip 되지 않도록 한 번 읽기
            if (_il2cppPreserve.Length == 0) return;

            // ViaLinkSDK 는 MonoBehaviour singleton (static Instance auto-property, lazy-create 안 함).
            // 씬에 컴포넌트가 없으면 직접 GameObject 만들어 부착 → Awake() 에서 Instance 가 this 로 세팅됨.
            if (ViaLinkSDK.Instance == null)
            {
                var go = new GameObject("ViaLinkSDK");
                go.AddComponent<ViaLinkSDK>();
                // Awake 가 즉시 호출되어 Instance 와 DontDestroyOnLoad 가 세팅된다.
            }

            Debug.Log("[ViaLink] 콜백 등록 (Initialize 이전)");

            // 딥링크 콜백 (App Link / Universal Link 진입)
            ViaLinkSDK.Instance.OnDeepLink += data =>
            {
                if (data == null)
                {
                    Debug.Log("[ViaLink] 일반 진입 (data == null)");
                    DeepLinkBus.Post("딥링크 콜백", "일반 진입 (data == null)");
                    return;
                }

                string paramsStr = FormatParams(data.Params);
                Debug.Log($"[ViaLink] 딥링크 수신 path={data.Path} params={paramsStr}");
                DeepLinkBus.Post(
                    "딥링크 수신 (App Link)",
                    $"경로: {data.Path}\n파라미터: {paramsStr}\nshortCode: {data.ShortCode ?? "없음"}"
                );
            };

            // 디퍼드 딥링크 콜백 (첫 실행 시 매칭 결과)
            ViaLinkSDK.Instance.OnDeferredDeepLink += (data, error) =>
            {
                if (error != null)
                {
                    Debug.LogWarning($"[ViaLink] 디퍼드 매칭 실패: code={error.Code} message={error.Message} retryable={error.Retryable}");
                    DeepLinkBus.Post(
                        "디퍼드 매칭 실패",
                        $"코드: {error.Code}\n메시지: {error.Message}\n재시도 가능: {error.Retryable}"
                    );
                    return;
                }

                if (data == null)
                {
                    Debug.Log("[ViaLink] 디퍼드 결과 없음 (organic install)");
                    DeepLinkBus.Post(
                        "디퍼드 딥링크 콜백",
                        "매칭 결과 없음 (organic install)"
                    );
                    return;
                }

                string paramsStr = FormatParams(data.Params);
                Debug.Log($"[ViaLink] 디퍼드 딥링크 수신 path={data.Path} params={paramsStr}");
                DeepLinkBus.Post(
                    "디퍼드 딥링크 수신",
                    $"경로: {data.Path}\n파라미터: {paramsStr}\nshortCode: {data.ShortCode ?? "없음"}"
                );
            };

            Debug.Log("[ViaLink] SDK 초기화 호출");
            ViaLinkSDK.Instance.Initialize(ApiKey);
            // SDK 가 Initialize() 끝에서 "[ViaLink] SDK 초기화 완료" 를 자체 로깅하므로 여기서 중복 로그 없음.
        }

        private static string FormatParams(System.Collections.Generic.Dictionary<string, string> p)
        {
            if (p == null || p.Count == 0) return "(없음)";
            var sb = new System.Text.StringBuilder();
            foreach (var kv in p)
            {
                if (sb.Length > 0) sb.Append(", ");
                sb.Append(kv.Key).Append("=").Append(kv.Value);
            }
            return sb.ToString();
        }
    }
}
