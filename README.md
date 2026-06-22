# ViaLink Unity SDK

**English** | [한국어](README.ko.md)

Unity SDK for the ViaLink deep link infrastructure service.
Distributed as a precompiled DLL (`Runtime/Plugins/ViaLinkSDK.dll`, netstandard2.1).

## Installation

### Unity Package Manager

Window > Package Manager > + > Add package from git URL:
```
https://github.com/aresjoydev/vialink-unity-sdk.git
```

## Requirements

- Unity 2021.3 or later (Unity 6 recommended — the DLL is relinked against the modules of 6000.4.6f1 as of 2026-05)
- .NET Standard 2.1
- IL2CPP (Android/iOS) support

## Usage — minimal integration code

> ✨ Since **v3.2.12+**, the `ViaLinkSDK` instance is **created automatically before scene load** (`Runtime/ViaLinkBootstrap.cs` — `[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]`).
> No separate GameObject attachment or scene setup is required. You only need to add **callback registration + an `Initialize` call**.

Add just this one file at `Assets/Scripts/ViaLinkInit.cs` (or any path):

```csharp
using UnityEngine;
using ViaLink;

public static class ViaLinkInit
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        // The package's automatic Bootstrap creates the ViaLinkSDK GameObject ahead of time
        // at BeforeSceneLoad, so Instance is always non-null at AfterSceneLoad.

        // Register callbacks first (Initialize may dispatch a cached cold-start deep link)
        ViaLinkSDK.Instance.OnDeepLink += data =>
        {
            if (data == null) return; // defensive null check
            Debug.Log($"[ViaLink] deep link: {data.Path}");
        };
        ViaLinkSDK.Instance.OnDeferredDeepLink += (data, error) =>
        {
            if (error != null) { /* match failed */ return; }
            if (data == null)  { /* organic install */ return; }
            Debug.Log($"[ViaLink] deferred deep link: {data.Path}");
        };

        // Initialize
        ViaLinkSDK.Instance.Initialize("YOUR_API_KEY");
    }
}

// Event tracking (anywhere in your game code)
ViaLinkSDK.Instance.TrackEvent("purchase", new Dictionary<string, object>
{
    { "product_id", "123" },
    { "revenue", 29900 },
    { "currency", "KRW" },
});
```

> ⓘ **Handling deep links that arrive before Initialize**: even if a cold-start deep link is delivered by the OS before `Initialize()` is called, the SDK's internal `_pendingURL` queue caches it, and `FlushPendingDeepLinks()` reprocesses it at the end of `Initialize()`, so no data is lost (v3.2.1+).

<details>
<summary>If you're curious about the older (v3.2.11 and below) pattern</summary>

In v3.2.11 and below, `ViaLinkSDK` is a `MonoBehaviour` singleton but has no lazy-create, so you had to write defensive code yourself at `BeforeSceneLoad`, such as `if (ViaLinkSDK.Instance == null) new GameObject("ViaLinkSDK").AddComponent<ViaLinkSDK>();`. Since v3.2.12 the package provides this bootstrap, so you can remove that code.

</details>


## Usage — Pull API (v3.2.1+)

When you missed callback registration or want to query directly at a specific point:

```csharp
// Returns immediately — the last cached deep link
DeepLinkData last = ViaLinkSDK.Instance.GetDeepLinkData();

// Wait for the next deep link to arrive (3-second timeout, null if none received)
ViaLinkSDK.Instance.AwaitDeepLinkData(data =>
{
    Debug.Log(data == null ? "timeout" : $"deep link: {data.Path}");
}, timeoutSeconds: 3f);

// Query the deferred match result immediately
DeepLinkData deferred = ViaLinkSDK.Instance.GetDeferredLinkData();

// Wait for the deferred match result (called immediately if already decided)
ViaLinkSDK.Instance.AwaitDeferredLinkData((data, error) =>
{
    if (error != null) { /* match failed */ return; }
    if (data == null)  { /* organic */ return; }
});
```

## Payment tracking (V1)

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

## Platform setup — deep links / universal links (required)

For the ViaLink SDK to receive deep links, the OS must recognize your app as the handler for the URL. Without the setup below, the `OnDeepLink` callback is not invoked (only deferred matching works).

### Android — `AndroidManifest.xml` intent-filter

When you enable **Custom Main Manifest** under `Edit > Project Settings > Player > Android > Publishing Settings`, Unity's default manifest is copied to `Assets/Plugins/Android/AndroidManifest.xml`. **Add only the ViaLink intent-filter to its `UnityPlayerActivity`, and never remove the other attributes (`launchMode`, `configChanges`, the `MAIN/LAUNCHER` intent-filter).**

> ⚠️ If `launchMode="singleTask"` is missing, the OS creates a new Activity instance on deep link entry and the game state is reset.
> If the `MAIN/LAUNCHER` intent-filter is missing, the app won't launch from its icon.

The full form should look like this:

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

            <!-- App icon launch (never remove) -->
            <intent-filter>
                <action android:name="android.intent.action.MAIN" />
                <category android:name="android.intent.category.LAUNCHER" />
            </intent-filter>

            <!-- ViaLink App Link (https) — recommended -->
            <intent-filter android:autoVerify="true">
                <action android:name="android.intent.action.VIEW" />
                <category android:name="android.intent.category.DEFAULT" />
                <category android:name="android.intent.category.BROWSABLE" />
                <data
                    android:scheme="https"
                    android:host="vialink.app"
                    android:pathPrefix="/{your-slug}/" /> <!-- the slug registered in the ViaLink dashboard -->
            </intent-filter>

            <!-- ViaLink custom URL Scheme (optional) -->
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

> You do not need to write any separate Android native code (`onCreate`/`onNewIntent`). Unity automatically converts the OS Intent into an `Application.deepLinkActivated` event that the SDK receives.

### iOS — Associated Domains + URL Scheme

Additional setup is required in the Xcode project after the Unity build. **Automating it with a PostProcessBuild script is recommended.**

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

        // 2) URL Scheme (optional)
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

To set it up manually: Xcode > Target > Signing & Capabilities > `+ Capability` > **Associated Domains** > add `applinks:vialink.app`.

> For details (Android Gradle setup, AASA file, debugging checklist, etc.), see [Unity SDK Guide §8](https://docs.vialink.app/sdk/unity-sdk-guide).

## Public classes

| Class | Role |
|--------|------|
| `ViaLinkSDK` | Main singleton (`Instance`, `Initialize`, `OnDeepLink`, Pull API, `TrackEvent`, `CreateLink`, `PaymentInitiated`) |
| `DeepLinkData` | Deep link data (`Path`, `Params`, `ShortCode`, `LinkId`) |
| `DeferredError` | Deferred match failure model (`Code`, `Message`, `Retryable`) |
| `DeviceInfo` | Device info model (for server transmission) |
| `EventPayload` | Event payload model |
| `PaymentInitiatedArgs` / `PaymentInitiatedResult` | Payment tracking input/output |

## Documentation

- [SDK Guide](https://docs.vialink.app)
