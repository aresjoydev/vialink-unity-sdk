#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace ViaLinkSample.EditorTools
{
    /// 에디터 로드 시 ViaLinkSample/Icons/AppIcon.png 을 PlayerSettings 의 Default Icon 으로
    /// 한 번만 자동 등록한다 (이미 설정돼 있으면 건너뜀).
    /// → iOS App Store 정책 (CFBundleIconName=AppIcon, 1024 marketing 아이콘) 자동 충족.
    [InitializeOnLoad]
    public static class IconAutoSetup
    {
        private const string IconPath = "Assets/ViaLinkSample/Icons/AppIcon.png";
        private const string MarkerKey = "ViaLink.IconAutoSetupApplied.v1";

        static IconAutoSetup()
        {
            if (SessionState.GetBool(MarkerKey, false)) return;
            SessionState.SetBool(MarkerKey, true);
            EditorApplication.delayCall += Apply;
        }

        private static void Apply()
        {
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
            if (tex == null)
            {
                Debug.LogWarning($"[ViaLink] IconAutoSetup: {IconPath} 를 찾을 수 없습니다. Player Settings 에서 수동으로 Default Icon 을 설정하세요.");
                return;
            }

            // 모든 플랫폼 기본 아이콘 — Unity 가 각 플랫폼별 슬롯(iOS 1024 marketing 포함)을 자동 채움
            var current = PlayerSettings.GetIconsForTargetGroup(BuildTargetGroup.Unknown);
            if (current != null && current.Length > 0 && current[0] == tex) return;

            PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Unknown, new[] { tex });
            Debug.Log($"[ViaLink] IconAutoSetup: Default Icon 으로 {IconPath} 등록 완료");
        }
    }
}
#endif
