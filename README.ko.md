# ViaLink Unity SDK

[![ViaLink — 6개 플랫폼 딥링크를 무료로 시작하세요](docs/banner-ko.png)](https://vialink.app)

[English](README.md) | **한국어**

ViaLink 딥링크 인프라 서비스를 위한 Unity SDK입니다.
사전 컴파일된 DLL(`Runtime/Plugins/ViaLinkSDK.dll`, netstandard2.1)로 배포됩니다.

게임 빌드 하나로 Android · iOS 딥링크를 처리합니다. 초대 · 복귀 · 캠페인 링크를 누르고
설치한 유저를 첫 실행에서 정확한 화면으로 보내고(디퍼드 딥링킹), 인게임 이벤트와 결제까지
하나의 파이프라인에서 어트리뷰션으로 연결합니다.

많은 딥링크 · 어트리뷰션 도구가 영업 문의와 연간 계약을 요구하는 것과 달리
**ViaLink는 무료로 시작합니다.** 카드 등록 없이, 가입 즉시 6개 플랫폼 SDK를 모두 쓸 수 있습니다.

**→ [vialink.app](https://vialink.app)**

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

## 사용법 — 최소 통합 코드

> ✨ **v3.2.12+** 부터 `ViaLinkSDK` 인스턴스가 **씬 로드 전 자동으로 생성**됩니다 (`Runtime/ViaLinkBootstrap.cs` — `[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]`).
> 별도 GameObject 부착이나 씬 설정은 필요 없습니다. 사용자는 **콜백 등록 + `Initialize` 호출**만 추가하면 됩니다.

`Assets/Scripts/ViaLinkInit.cs` (또는 임의 경로) 에 다음 한 파일만 추가하세요:

```csharp
using UnityEngine;
using ViaLink;

public static class ViaLinkInit
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        // 패키지의 자동 Bootstrap 이 BeforeSceneLoad 에서 ViaLinkSDK GameObject 를
        // 미리 만들어 두므로 AfterSceneLoad 에서 Instance 는 항상 non-null 입니다.

        // 콜백 먼저 등록 (Initialize 가 캐싱된 cold-start 딥링크를 dispatch 할 수 있음)
        ViaLinkSDK.Instance.OnDeepLink += data =>
        {
            if (data == null) return; // 방어적 null 체크
            Debug.Log($"[ViaLink] 딥링크: {data.Path}");
        };
        ViaLinkSDK.Instance.OnDeferredDeepLink += (data, error) =>
        {
            if (error != null) { /* 매칭 실패 */ return; }
            if (data == null)  { /* organic install */ return; }
            Debug.Log($"[ViaLink] 디퍼드 딥링크: {data.Path}");
        };

        // Initialize
        ViaLinkSDK.Instance.Initialize("YOUR_API_KEY");
    }
}

// 이벤트 추적 (게임 코드 어디서나)
ViaLinkSDK.Instance.TrackEvent("purchase", new Dictionary<string, object>
{
    { "product_id", "123" },
    { "revenue", 29900 },
    { "currency", "KRW" },
});
```

> ⓘ **Initialize 이전 도착 딥링크 처리**: cold-start 딥링크가 `Initialize()` 호출 전에 OS 에서 전달되더라도 SDK 내부 `_pendingURL` 큐가 캐싱했다가 `Initialize()` 끝에 `FlushPendingDeepLinks()` 가 재처리하므로 데이터 누락이 없습니다 (v3.2.1+).

<details>
<summary>이전(v3.2.11 이하) 패턴이 궁금하다면</summary>

v3.2.11 이하에서는 `ViaLinkSDK` 가 `MonoBehaviour` 싱글턴이지만 lazy-create 가 없어, 사용자가 `BeforeSceneLoad` 에서 `if (ViaLinkSDK.Instance == null) new GameObject("ViaLinkSDK").AddComponent<ViaLinkSDK>();` 같은 방어 코드를 직접 작성해야 했습니다. v3.2.12 부터는 패키지가 이 부트스트랩을 제공하므로 위 코드를 제거해도 됩니다.

</details>


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

## 플랫폼 설정 — 딥링크 / 유니버셜 링크 (필수)

ViaLink SDK 가 딥링크를 수신하려면 OS 가 앱을 해당 URL 의 핸들러로 인식해야 합니다. 아래 설정 없이는 `OnDeepLink` 콜백이 호출되지 않습니다 (디퍼드 매칭만 동작).

### Android — `AndroidManifest.xml` intent-filter

`Edit > Project Settings > Player > Android > Publishing Settings` 에서 **Custom Main Manifest** 를 체크하면 `Assets/Plugins/Android/AndroidManifest.xml` 에 Unity 기본 manifest 가 복사됩니다. **그 안의 `UnityPlayerActivity` 에 ViaLink intent-filter 만 추가하고, 다른 속성(`launchMode`, `configChanges`, `MAIN/LAUNCHER` intent-filter)은 절대 지우지 마세요.**

> ⚠️ `launchMode="singleTask"` 가 빠지면 딥링크 진입 시 OS 가 Activity 새 인스턴스를 만들어 게임 상태가 초기화됩니다.
> `MAIN/LAUNCHER` intent-filter 가 빠지면 앱 아이콘에서 실행이 안 됩니다.

전체 형태는 다음과 같아야 합니다:

```xml
<?xml version="1.0" encoding="utf-8"?>
<manifest xmlns:android="http://schemas.android.com/apk/res/android">
    <application>
        <activity
            android:name="com.unity3d.player.UnityPlayerActivity"
            android:theme="@style/UnityThemeSelector"
            android:configChanges="mcc|mnc|locale|touchscreen|keyboard|keyboardHidden|navigation|orientation|screenLayout|uiMode|screenSize|smallestScreenSize|fontScale|layoutDirection|density"
            android:launchMode="singleTask"
            android:exported="true">

            <!-- 앱 아이콘 실행 (절대 지우지 말 것) -->
            <intent-filter>
                <action android:name="android.intent.action.MAIN" />
                <category android:name="android.intent.category.LAUNCHER" />
            </intent-filter>

            <!-- ViaLink App Link (https) — 권장 -->
            <intent-filter android:autoVerify="true">
                <action android:name="android.intent.action.VIEW" />
                <category android:name="android.intent.category.DEFAULT" />
                <category android:name="android.intent.category.BROWSABLE" />
                <data
                    android:scheme="https"
                    android:host="vialink.app"
                    android:pathPrefix="/{your-slug}/" /> <!-- ViaLink 대시보드에서 등록한 slug -->
            </intent-filter>

            <!-- ViaLink 커스텀 URL Scheme (선택) -->
            <intent-filter>
                <action android:name="android.intent.action.VIEW" />
                <category android:name="android.intent.category.DEFAULT" />
                <category android:name="android.intent.category.BROWSABLE" />
                <data android:scheme="vialink-example" />
            </intent-filter>
        </activity>
    </application>
</manifest>
```

> 별도로 Android 네이티브 코드(`onCreate`/`onNewIntent`) 를 작성할 필요는 없습니다. Unity 가 OS Intent 를 자동으로 `Application.deepLinkActivated` 이벤트로 변환해 SDK 가 받습니다.

### iOS — Associated Domains + URL Scheme

Unity 빌드 후 Xcode 프로젝트에 추가 설정이 필요합니다. **PostProcessBuild 스크립트로 자동화**하는 것을 권장합니다.

`Assets/Editor/ViaLinkiOSPostProcess.cs`:

```csharp
#if UNITY_IOS
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using System.IO;

public static class ViaLinkiOSPostProcess
{
    [PostProcessBuild(999)]
    public static void OnPostProcessBuild(BuildTarget target, string path)
    {
        if (target != BuildTarget.iOS) return;

        // 1) Associated Domains (Universal Link)
        string projPath = PBXProject.GetPBXProjectPath(path);
        var proj = new PBXProject();
        proj.ReadFromFile(projPath);
        string mainTarget = proj.GetUnityMainTargetGuid();

        var caps = new ProjectCapabilityManager(
            projPath, "Unity-iPhone.entitlements", null, mainTarget);
        caps.AddAssociatedDomains(new[] { "applinks:vialink.app" });
        caps.WriteToFile();

        // 2) URL Scheme (선택)
        string plistPath = Path.Combine(path, "Info.plist");
        var plist = new PlistDocument();
        plist.ReadFromFile(plistPath);

        var urlTypes = plist.root.CreateArray("CFBundleURLTypes");
        var urlType  = urlTypes.AddDict();
        urlType.SetString("CFBundleURLName", "com.example.app");
        urlType.CreateArray("CFBundleURLSchemes").AddString("vialink-example");
        plist.WriteToFile(plistPath);
    }
}
#endif
```

수동으로 설정하려면: Xcode > Target > Signing & Capabilities > `+ Capability` > **Associated Domains** > `applinks:vialink.app` 추가.

> 자세한 내용(Android Gradle 설정, AASA 파일, 디버깅 체크리스트 등)은 [Unity SDK 가이드 §8](https://docs.vialink.app/sdk/unity-sdk-guide) 를 참고하세요.

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
