# ViaLink Unity SDK

ViaLink 딥링크 인프라 서비스를 위한 Unity SDK입니다.
사전 컴파일된 DLL(`Runtime/Plugins/ViaLinkSDK.dll`, netstandard2.1)로 배포됩니다.

## 설치

### Unity Package Manager

Window > Package Manager > + > Add package from git URL:
```
https://github.com/aresjoydev/vialink-unity-sdk.git
```

## 요구사항

- Unity 2021.3 이상 (Unity 6 권장 — DLL 은 2026-05 시점 6000.4.6f1 의 module 들에 relink 되어 있음)
- .NET Standard 2.1
- IL2CPP (Android/iOS) 지원

## 사용법 — 콜백 방식 (BeforeSceneLoad 권장)

```csharp
using UnityEngine;
using ViaLink;

public static class ViaLinkBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        // ① 콜백 먼저 등록 (Initialize 가 cold-start 딥링크를 즉시 dispatch 할 수 있음)
        ViaLinkSDK.Instance.OnDeepLink += data =>
        {
            Debug.Log($"[ViaLink] 딥링크: {data.Path}");
        };
        ViaLinkSDK.Instance.OnDeferredDeepLink += (data, error) =>
        {
            if (error != null) { /* 매칭 실패 */ return; }
            if (data == null)  { /* organic install */ return; }
            Debug.Log($"[ViaLink] 디퍼드 딥링크: {data.Path}");
        };

        // ② 그 다음 Initialize
        ViaLinkSDK.Instance.Initialize("YOUR_API_KEY");
    }
}

// 이벤트 추적
ViaLinkSDK.Instance.TrackEvent("purchase", new Dictionary<string, object>
{
    { "product_id", "123" },
    { "revenue", 29900 },
    { "currency", "KRW" },
});
```

## 사용법 — Pull API (v3.2.1+)

콜백 등록을 놓쳤거나 특정 시점에 직접 조회하고 싶을 때:

```csharp
// 즉시 반환 — 캐시된 마지막 딥링크
DeepLinkData last = ViaLinkSDK.Instance.GetDeepLinkData();

// 다음 딥링크 도착 대기 (3초 타임아웃, null 이면 미수신)
ViaLinkSDK.Instance.AwaitDeepLinkData(data =>
{
    Debug.Log(data == null ? "타임아웃" : $"딥링크: {data.Path}");
}, timeoutSeconds: 3f);

// 디퍼드 매칭 결과 즉시 조회
DeepLinkData deferred = ViaLinkSDK.Instance.GetDeferredLinkData();

// 디퍼드 매칭 결과 대기 (이미 결정됐으면 즉시 호출)
ViaLinkSDK.Instance.AwaitDeferredLinkData((data, error) =>
{
    if (error != null) { /* 매칭 실패 */ return; }
    if (data == null)  { /* organic */ return; }
});
```

## 결제 추적 (V1)

```csharp
ViaLinkSDK.Instance.PaymentInitiated(new PaymentInitiatedArgs
{
    OrderId = "ORD-2026-0001",
    Amount = 19900d,
    Currency = "KRW",
    PaymentMethod = "card",
}, result =>
{
    Debug.Log($"paymentEventId={result.PaymentEventId}");
}, err => Debug.LogError(err));
```

## 공개 클래스

| 클래스 | 역할 |
|--------|------|
| `ViaLinkSDK` | 메인 싱글턴 (`Instance`, `Initialize`, `OnDeepLink`, Pull API, `TrackEvent`, `CreateLink`, `PaymentInitiated`) |
| `DeepLinkData` | 딥링크 데이터 (`Path`, `Params`, `ShortCode`, `LinkId`) |
| `DeferredError` | 디퍼드 매칭 실패 모델 (`Code`, `Message`, `Retryable`) |
| `DeviceInfo` | 디바이스 정보 모델 (서버 전송용) |
| `EventPayload` | 이벤트 페이로드 모델 |
| `PaymentInitiatedArgs` / `PaymentInitiatedResult` | 결제 추적 입출력 |

## 문서

- [SDK 가이드](https://docs.vialink.app)
