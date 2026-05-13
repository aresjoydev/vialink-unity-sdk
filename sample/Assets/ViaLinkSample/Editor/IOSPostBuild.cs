#if UNITY_IOS
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

namespace ViaLinkSample.EditorTools
{
    /// iOS 빌드 후 Xcode 프로젝트에 ViaLink 관련 설정을 자동으로 보강한다.
    ///
    /// 1) Associated Domains entitlement 추가 (Universal Links — applinks:vialink.app)
    ///    - iOS SDK v3.0.13 fix: '?mode=developer' suffix 는 distribution 빌드에서 Universal Link 검증이 실패하므로 사용하지 않음.
    /// 2) Info.plist CFBundleIconName=AppIcon 보강 (iOS SDK eefada4 fix 와 동일한 이슈 — App Store 검증 통과용)
    ///    - Unity 6 는 일반적으로 자동 주입하지만, 일부 경로에서 빠질 수 있어 안전하게 명시.
    public static class IOSPostBuild
    {
        private const string AssociatedDomain = "applinks:vialink.app";

        [PostProcessBuild(100)]
        public static void OnPostProcessBuild(BuildTarget target, string pathToBuiltProject)
        {
            if (target != BuildTarget.iOS) return;

            ApplyAssociatedDomains(pathToBuiltProject);
            EnsureBundleIconName(pathToBuiltProject);

            UnityEngine.Debug.Log("[ViaLink] iOS PostBuild 완료: Associated Domains + CFBundleIconName 적용");
        }

        // ── Associated Domains entitlement ──
        private static void ApplyAssociatedDomains(string pathToBuiltProject)
        {
            string pbxPath = PBXProject.GetPBXProjectPath(pathToBuiltProject);
            var pbx = new PBXProject();
            pbx.ReadFromFile(pbxPath);

            string targetGuid = pbx.GetUnityMainTargetGuid();
            string entitlementsRelativePath = "Unity-iPhone/ViaLinkSample.entitlements";
            string entitlementsFullPath = Path.Combine(pathToBuiltProject, entitlementsRelativePath);

            // 이미 동일한 내용이 들어있으면 다시 쓰지 않는다 (mtime 갱신 회피).
            // Xcode 16+ 가 build 도중 entitlements 파일 mtime 변동을 detected → "modified during the build" 에러를 내므로,
            // PostBuild 가 매번 무조건 덮어쓰면 두 번째 빌드부터 fail. 동일 내용이면 skip 한다.
            if (AlreadyHasDomain(entitlementsFullPath, AssociatedDomain))
            {
                UnityEngine.Debug.Log("[ViaLink] Associated Domains 이미 동일 — write skip");
            }
            else
            {
                var plist = new PlistDocument();
                if (File.Exists(entitlementsFullPath))
                {
                    plist.ReadFromFile(entitlementsFullPath);
                }
                // Associated Domains 배열 작성 — 기존 값 덮어쓰기 (developer 모드 suffix 등 잔재 제거)
                var domains = plist.root.CreateArray("com.apple.developer.associated-domains");
                domains.AddString(AssociatedDomain);
                plist.WriteToFile(entitlementsFullPath);
            }

            pbx.AddFile(entitlementsRelativePath, entitlementsRelativePath);
            pbx.AddBuildProperty(targetGuid, "CODE_SIGN_ENTITLEMENTS", entitlementsRelativePath);
            // Defense in depth: idempotent write 가 race 로 fail 해도 (예: clean build) Xcode 가 reject 하지 않도록 허용.
            // 우리가 PostBuild 에서 이미 정확한 entitlements 를 주입한 상태이므로 서명 무결성에는 영향 없음.
            pbx.AddBuildProperty(targetGuid, "CODE_SIGN_ALLOW_ENTITLEMENTS_MODIFICATION", "YES");
            pbx.WriteToFile(pbxPath);
        }

        private static bool AlreadyHasDomain(string entitlementsFullPath, string expectedDomain)
        {
            if (!File.Exists(entitlementsFullPath)) return false;
            try
            {
                var existing = new PlistDocument();
                existing.ReadFromFile(entitlementsFullPath);
                if (!existing.root.values.ContainsKey("com.apple.developer.associated-domains")) return false;
                var arr = existing.root["com.apple.developer.associated-domains"] as PlistElementArray;
                if (arr == null || arr.values.Count != 1) return false;
                var first = arr.values[0] as PlistElementString;
                return first != null && first.value == expectedDomain;
            }
            catch
            {
                return false;
            }
        }

        // ── Info.plist CFBundleIconName 보강 ──
        private static void EnsureBundleIconName(string pathToBuiltProject)
        {
            string plistPath = Path.Combine(pathToBuiltProject, "Info.plist");
            if (!File.Exists(plistPath)) return;

            var plist = new PlistDocument();
            plist.ReadFromFile(plistPath);

            if (!plist.root.values.ContainsKey("CFBundleIconName"))
            {
                plist.root.SetString("CFBundleIconName", "AppIcon");
                plist.WriteToFile(plistPath);
                UnityEngine.Debug.Log("[ViaLink] CFBundleIconName=AppIcon 보강");
            }
        }
    }
}
#endif
