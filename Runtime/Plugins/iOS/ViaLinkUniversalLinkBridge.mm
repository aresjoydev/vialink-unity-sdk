//
//  ViaLinkUniversalLinkBridge.mm
//  ViaLink Unity SDK — iOS Universal Link bridge
//
//  배경:
//    iOS 13+ 의 SceneDelegate 환경에서 Universal Link 는 UISceneDelegate 의
//    `scene:continueUserActivity:` 콜백으로 전달된다 (AppDelegate 의 동명 메서드는 호출되지 않는다).
//    그러나 Unity 6 의 자동 생성 SceneDelegate (`UnityScene`) 가 이 메서드를 구현하지 않아
//    Universal Link 가 어디로도 전달되지 않는다 — `Application.deepLinkActivated` 가 fire 되지
//    않고, `UnitySetAbsoluteURL` 도 호출되지 않아 SDK 가 URL 을 받지 못한다.
//    Custom URL Scheme 만 `scene:openURLContexts:` 에서 처리되고 있다.
//
//  Fix:
//    +load 시점에 `UnityScene` 클래스에 runtime 으로 다음 두 메서드를 주입(또는 swizzle)한다:
//      - scene:continueUserActivity:                Universal Link (warm/hot start)
//      - scene:willConnectToSession:options:        Universal Link (cold start, connectionOptions.userActivities)
//    수신된 NSURL 을 `UnitySetAbsoluteURL` (cold-start absoluteURL 채우기) +
//    `UnitySendMessage("ViaLinkSDK", "_OnNativeUniversalLink", url)` (SDK 에 즉시 통지) 두 가지 경로로 전달.
//    SDK 의 ViaLinkSDK 싱글턴 GameObject 가 `_OnNativeUniversalLink` 메서드에서 기존 핸들러로 라우팅한다.
//
//  영향 범위:
//    iOS 만 영향. Custom URL Scheme 처리는 그대로 (이 plugin 은 universal link 케이스만 추가).
//    UnityScene 클래스가 미래에 자체 구현을 추가하면 swizzle 로 wrap 되어 두 핸들러가 모두 실행됨.
//    `UnityScene` 미발견 시 (Unity 가 클래스 이름을 바꾸면) 조용히 skip — SDK 다른 동작에는 영향 없음.
//

#import <Foundation/Foundation.h>
#import <UIKit/UIKit.h>
#import <objc/runtime.h>

// Unity 가 export 하는 함수들. UnityFramework 가 link 시점에 resolve.
extern "C" {
    void UnitySendMessage(const char* obj, const char* method, const char* msg);
    void UnitySetAbsoluteURL(const char* url);
}

static void ViaLinkDispatchUniversalLinkURL(NSURL* url) {
    if (url == nil) return;
    NSString* urlString = url.absoluteString;
    if (urlString == nil || urlString.length == 0) return;

    NSLog(@"[ViaLink][native] Universal Link 수신: %@", urlString);

    // (1) Application.absoluteURL 에 세팅 — SDK 의 cold-start 경로와 정합 유지
    UnitySetAbsoluteURL([urlString UTF8String]);

    // (2) SDK GameObject 에 즉시 통지 — warm/hot start 도 처리
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

        // ── (a) scene:continueUserActivity: ── Universal Link warm/hot start
        SEL continueSel = @selector(scene:continueUserActivity:);
        const char* continueTypes = "v@:@@";  // void(id, SEL, UIScene*, NSUserActivity*)

        IMP continueImp = imp_implementationWithBlock(^(id self, UIScene* scene, NSUserActivity* userActivity) {
            if ([userActivity.activityType isEqualToString:NSUserActivityTypeBrowsingWeb]) {
                ViaLinkDispatchUniversalLinkURL(userActivity.webpageURL);
            }
        });

        if (class_addMethod(unitySceneClass, continueSel, continueImp, continueTypes)) {
            NSLog(@"[ViaLink][native] UnityScene 에 scene:continueUserActivity: 메서드 추가 완료");
        } else {
            // 이미 존재 — swizzle 로 wrap 해서 우리 핸들러 + 기존 동작 모두 실행
            Method origMethod = class_getInstanceMethod(unitySceneClass, continueSel);
            if (origMethod != NULL) {
                IMP origImp = method_getImplementation(origMethod);
                IMP wrapImp = imp_implementationWithBlock(^(id self, UIScene* scene, NSUserActivity* userActivity) {
                    if ([userActivity.activityType isEqualToString:NSUserActivityTypeBrowsingWeb]) {
                        ViaLinkDispatchUniversalLinkURL(userActivity.webpageURL);
                    }
                    ((void(*)(id, SEL, UIScene*, NSUserActivity*))origImp)(self, continueSel, scene, userActivity);
                });
                method_setImplementation(origMethod, wrapImp);
                NSLog(@"[ViaLink][native] UnityScene 의 scene:continueUserActivity: swizzle 완료");
            }
        }

        // ── (b) scene:willConnectToSession:options: ── Universal Link cold start
        // Unity 6 의 UnityScene 는 이 메서드를 이미 구현하고 있으므로 (URL Scheme cold-start 처리용)
        // 항상 swizzle 경로로 들어간다.
        SEL willConnectSel = @selector(scene:willConnectToSession:options:);
        Method origWillConnect = class_getInstanceMethod(unitySceneClass, willConnectSel);
        if (origWillConnect != NULL) {
            IMP origImp = method_getImplementation(origWillConnect);
            IMP wrapImp = imp_implementationWithBlock(^(id self, UIScene* scene, UISceneSession* session, UISceneConnectionOptions* options) {
                // 기존 동작 (URL Scheme cold-start 처리) 먼저 실행
                ((void(*)(id, SEL, UIScene*, UISceneSession*, UISceneConnectionOptions*))origImp)(self, willConnectSel, scene, session, options);

                // 그 다음 universal link cold-start 추가 처리
                for (NSUserActivity* activity in options.userActivities) {
                    if ([activity.activityType isEqualToString:NSUserActivityTypeBrowsingWeb]) {
                        ViaLinkDispatchUniversalLinkURL(activity.webpageURL);
                        break;  // 보통 1개
                    }
                }
            });
            method_setImplementation(origWillConnect, wrapImp);
            NSLog(@"[ViaLink][native] UnityScene 의 scene:willConnectToSession:options: cold-start hook 완료");
        }
    });
}

@end
