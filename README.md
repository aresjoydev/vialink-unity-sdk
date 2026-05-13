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

## 변경 이력

- **3.2.6** — iOS Universal Link cold-start hang/watchdog 회피:
  - 3.2.5 가 `scene:willConnectToSession:options:` 에서 `UnitySetAbsoluteURL` 을 호출했는데, 이 시점은 `initUnityWithScene:` 이전이라 IL2CPP 부팅이 완료되지 않은 상태. Unity API 호출이 초기화를 망쳐 scene-create 가 19.81s 안에 끝나지 않고 watchdog `0x8BADF00D` 로 강제 종료되는 회귀 발생.
  - 브릿지를 3단 hook 으로 재설계: `willConnectToSession` 은 NSURL 을 static 에 **stash만**, `sceneWillEnterForeground` swizzle 을 새로 추가해 원래 동작(initUnityWithScene)이 끝난 뒤에 `UnitySetAbsoluteURL` + `UnitySendMessage` 로 SDK 에 통지. Unity 자체도 cold-start URL Scheme 처리를 같은 패턴(willConnect 에서 stash → sceneWillEnterForeground 에서 apply)으로 한다.
- **3.2.5** — iOS Universal Link cold-start 누락 회복:
  - 3.2.4 에서 cold-start UL 처리 swizzle 을 통째로 제거했더니 UIScene 환경에서 cold-start UL 이 어디에서도 수신되지 않는 회귀가 생김. UIScene + iOS 13+ 에서는 cold-start UL 이 `application:willFinishLaunchingWithOptions:` 의 launchOptions 가 아니라 `scene:willConnectToSession:options:` 의 `connectionOptions.userActivities` 로만 전달되기 때문 (Unity 의 `UnityAppController` 는 launchOptions 경로만 처리).
  - `scene:willConnectToSession:options:` swizzle 을 복원하되, 이번엔 **`UnitySetAbsoluteURL` 만** 호출 (이전 3.2.3 의 크래시 원인이었던 `UnitySendMessage` 는 IL2CPP 부팅 전이라 호출 금지). SDK 의 `CheckColdStartDeepLink()` 가 Initialize 시점에 `Application.absoluteURL` 을 픽업.
  - 결과: cold-start UL → absoluteURL 경로 1회, warm/hot UL → `_OnNativeUniversalLink` 경로 1회 — 서로 다른 경로라 중복 호출 없음. crash 도 없음.
- **3.2.4** — iOS Universal Link 중복/크래시 hotfix: 이전 버전의 `scene:willConnectToSession:options:` swizzle 이 (a) Unity 의 cold-start UL 처리(`UnityAppController.application:willFinishLaunchingWithOptions:` 가 launchOptions 에서 UL 을 꺼내 `UnitySetAbsoluteURL` 호출)와 중복돼 `OnDeepLink` 가 2회 fire 되고, (b) IL2CPP 런타임(`initUnityWithScene`) 부팅 전에 `UnitySendMessage` 가 호출돼 cold-start 크래시를 유발하던 문제 수정. 브릿지는 이제 `scene:continueUserActivity:` (warm/hot start) 만 처리, cold-start 는 Unity 의 willFinishLaunching 경로에 일임. 추가로 SDK 의 `HandleDeepLinkActivated` 에 동일 URL 1초 재진입 가드 추가.
- **3.2.3** — iOS Universal Link 콜백 fix: Unity 6 의 `UnityScene` (SceneDelegate) 가 `scene:continueUserActivity:` 를 구현하지 않아 Universal Link 가 `Application.deepLinkActivated` 로 전달되지 않던 한계 우회. `Runtime/Plugins/iOS/ViaLinkUniversalLinkBridge.mm` 가 +load 시점에 `UnityScene` 클래스에 메서드 주입(또는 swizzle) → URL 을 SDK 의 `_OnNativeUniversalLink` 핸들러로 전달. cold-start (`willConnectToSession`) + warm/hot start 모두 처리. Custom URL Scheme 처리에는 영향 없음.
- **3.2.2** — 디퍼드 매칭 fix: `device_info.os` 가 모바일에서 `"Other"` 로 송신되던 버그 수정 (`SystemInfo.operatingSystemFamily` → `Application.platform` 분기). 이제 Android/iOS 네이티브 SDK 와 동일하게 `"Android"`/`"iOS"` 송신 → 서버 fingerprint (IP+OS) 매칭 정상화
- **3.2.1** — Pull API 4개 추가 (`GetDeepLinkData`/`AwaitDeepLinkData`/`GetDeferredLinkData`/`AwaitDeferredLinkData`), Initialize 이전 도착 딥링크 캐싱(`FlushPendingDeepLinks`), iOS/Android v3.2.x API 표면 정합화
- **3.0.0** — 디퍼드 콜백 redesign (`OnDeferredDeepLink(data, error)`), Payment.Initiated V1
- **1.0.x** — 초기 릴리즈

## 문서

- [SDK 가이드](https://docs.vialink.app)
