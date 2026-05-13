//
//  ViaLinkUniversalLinkBridge.mm
//  ViaLink Unity SDK — iOS Universal Link bridge
//
//  배경:
//    iOS 13+ SceneDelegate 환경에서 Universal Link 는 UISceneDelegate 의
//    `scene:continueUserActivity:` 콜백으로 전달된다. Unity 6 의 자동 생성 SceneDelegate
//    (`UnityScene`) 는 이 메서드를 구현하지 않아 warm/hot start UL 이 어디로도 전달되지 않는다.
//
//    또한 UIScene 환경에서 cold start UL 은 `application:willFinishLaunchingWithOptions:` 의
//    launchOptions 가 아니라 `scene:willConnectToSession:options:` 의 `connectionOptions.userActivities`
//    로만 전달된다 (Unity 의 `UnityAppController` 는 launchOptions 경로만 처리하므로 누락).
//
//  Fix (v3.2.5+):
//    +load 시점에 `UnityScene` 의 세 메서드를 후킹:
//
//    (a) scene:continueUserActivity: ── warm/hot start
//        - 미구현 → `class_addMethod` 로 주입 (이미 있으면 swizzle).
//        - 수신 NSURL 을 `UnitySetAbsoluteURL` + `UnitySendMessage("ViaLinkSDK",
//          "_OnNativeUniversalLink", url)` 두 경로로 전달. Unity 가 부팅된 상태이므로 안전.
//
//    (b) scene:willConnectToSession:options: ── cold start (stash 단계)
//        - 이미 구현됨 → swizzle. 원래 동작(URL Scheme `_pendingURLContext` 저장) 후
//          `connectionOptions.userActivities` 에서 UL 을 찾아 **static NSURL 에 stash만** 한다.
//        - 이 시점은 `initUnityWithScene:` 이전이라 Unity API (`UnitySetAbsoluteURL` 포함) 호출 금지
//          → Unity 자체도 cold-start URL Scheme 처리를 sceneWillEnterForeground 로 미룬다.
//          여기서 `UnitySetAbsoluteURL` 을 호출하면 IL2CPP 초기화 hang / cold-start 크래시
//          (Watchdog 0x8BADF00D) 유발.
//
//    (c) sceneWillEnterForeground: ── cold start (apply 단계)
//        - 이미 구현됨 → swizzle. 원래 동작(`initUnityWithScene:` 호출, Unity 부팅, URL Scheme
//          `_pendingURLContext` 적용) 후 stash 한 UL 을 `UnitySetAbsoluteURL` + `UnitySendMessage`
//          로 SDK 에 통지. 이 시점은 Unity 와 SDK 가 모두 부팅된 상태이므로 안전.
//
//  영향 범위:
//    iOS 만 영향. Custom URL Scheme 처리는 그대로 (원래 핸들러를 먼저 호출). `UnityScene` 미발견 시
//    조용히 skip — SDK 다른 동작에는 영향 없음. SDK 의 `HandleDeepLinkActivated` 가 동일 URL
//    1초 재진입 가드를 가지므로 잠재적 중복 호출도 1회로 수렴.
//

#import <Foundation/Foundation.h>
#import <UIKit/UIKit.h>
#import <objc/runtime.h>

extern "C" {
    void UnitySendMessage(const char* obj, const char* method, const char* msg);
    void UnitySetAbsoluteURL(const char* url);
}

// cold start 단계 사이에 URL 을 옮기는 static (sceneWillEnterForeground 에서 소비).
// ARC 가 strong reference 로 유지하므로 별도 retain 불필요.
static NSURL* g_pendingColdStartUniversalLink = nil;

// Unity / SDK 가 모두 부팅된 시점에 호출하는 통지 (warm/hot, cold-apply 공용)
static void ViaLinkNotifyUniversalLink(NSURL* url) {
    if (url == nil) return;
    NSString* urlString = url.absoluteString;
    if (urlString == nil || urlString.length == 0) return;

    NSLog(@"[ViaLink][native] Universal Link 통지: %@", urlString);
    UnitySetAbsoluteURL([urlString UTF8String]);
    UnitySendMessage("ViaLinkSDK", "_OnNativeUniversalLink", [urlString UTF8String]);
}

@interface ViaLinkUniversalLinkBridge : NSObject
@end

@implementation ViaLinkUniversalLinkBridge

+ (void)load {
    static dispatch_once_t once;
    dispatch_once(&once, ^{
        Class unitySceneClass = NSClassFromString(@"UnityScene");
        if (unitySceneClass == nil) {
            NSLog(@"[ViaLink][native] UnityScene 클래스 미발견 — Universal Link bridge 건너뜀");
            return;
        }

        // ── (a) scene:continueUserActivity: ── warm/hot start
        SEL continueSel = @selector(scene:continueUserActivity:);
        const char* continueTypes = "v@:@@";  // void(id, SEL, UIScene*, NSUserActivity*)

        IMP continueImp = imp_implementationWithBlock(^(id self, UIScene* scene, NSUserActivity* userActivity) {
            if ([userActivity.activityType isEqualToString:NSUserActivityTypeBrowsingWeb]) {
                ViaLinkNotifyUniversalLink(userActivity.webpageURL);
            }
        });

        if (class_addMethod(unitySceneClass, continueSel, continueImp, continueTypes)) {
            NSLog(@"[ViaLink][native] UnityScene 에 scene:continueUserActivity: 메서드 추가 완료");
        } else {
            Method origMethod = class_getInstanceMethod(unitySceneClass, continueSel);
            if (origMethod != NULL) {
                IMP origImp = method_getImplementation(origMethod);
                IMP wrapImp = imp_implementationWithBlock(^(id self, UIScene* scene, NSUserActivity* userActivity) {
                    if ([userActivity.activityType isEqualToString:NSUserActivityTypeBrowsingWeb]) {
                        ViaLinkNotifyUniversalLink(userActivity.webpageURL);
                    }
                    ((void(*)(id, SEL, UIScene*, NSUserActivity*))origImp)(self, continueSel, scene, userActivity);
                });
                method_setImplementation(origMethod, wrapImp);
                NSLog(@"[ViaLink][native] UnityScene 의 scene:continueUserActivity: swizzle 완료");
            }
        }

        // ── (b) scene:willConnectToSession:options: ── cold start (stash 단계)
        SEL willConnectSel = @selector(scene:willConnectToSession:options:);
        Method origWillConnect = class_getInstanceMethod(unitySceneClass, willConnectSel);
        if (origWillConnect != NULL) {
            IMP origWcImp = method_getImplementation(origWillConnect);
            IMP wrapWcImp = imp_implementationWithBlock(^(id self, UIScene* scene, UISceneSession* session, UISceneConnectionOptions* options) {
                // 원래 동작 (URL Scheme _pendingURLContext 저장) 먼저
                ((void(*)(id, SEL, UIScene*, UISceneSession*, UISceneConnectionOptions*))origWcImp)(self, willConnectSel, scene, session, options);

                // UL 은 stash 만 — Unity 부팅 전이므로 UnitySetAbsoluteURL/UnitySendMessage 절대 금지
                for (NSUserActivity* activity in options.userActivities) {
                    if ([activity.activityType isEqualToString:NSUserActivityTypeBrowsingWeb]) {
                        g_pendingColdStartUniversalLink = activity.webpageURL;
                        NSLog(@"[ViaLink][native] cold-start UL stash: %@", activity.webpageURL.absoluteString);
                        break;
                    }
                }
            });
            method_setImplementation(origWillConnect, wrapWcImp);
            NSLog(@"[ViaLink][native] UnityScene 의 scene:willConnectToSession:options: stash hook 완료");
        }

        // ── (c) sceneWillEnterForeground: ── cold start (apply 단계)
        SEL foregroundSel = @selector(sceneWillEnterForeground:);
        Method origForeground = class_getInstanceMethod(unitySceneClass, foregroundSel);
        if (origForeground != NULL) {
            IMP origFgImp = method_getImplementation(origForeground);
            IMP wrapFgImp = imp_implementationWithBlock(^(id self, UIScene* scene) {
                // 원래 동작 먼저 — 여기서 initUnityWithScene 이 Unity 와 SDK 를 부팅함
                ((void(*)(id, SEL, UIScene*))origFgImp)(self, foregroundSel, scene);

                // 이제 Unity 가 부팅됐으므로 stash 한 UL 을 SDK 에 통지
                NSURL* pending = g_pendingColdStartUniversalLink;
                g_pendingColdStartUniversalLink = nil;
                if (pending != nil) {
                    NSLog(@"[ViaLink][native] cold-start UL apply (post-init)");
                    ViaLinkNotifyUniversalLink(pending);
                }
            });
            method_setImplementation(origForeground, wrapFgImp);
            NSLog(@"[ViaLink][native] UnityScene 의 sceneWillEnterForeground: apply hook 완료");
        }
    });
}

@end
