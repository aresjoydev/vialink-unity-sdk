# ViaLink Unity SDK

ViaLink 딥링크 인프라 서비스를 위한 Unity SDK입니다.
사전 컴파일된 DLL로 배포됩니다.

## 설치

### Unity Package Manager

Window > Package Manager > + > Add package from git URL:
```
https://github.com/aresjoydev/vialink-unity-sdk.git
```

## 요구사항

- Unity 2021.3 이상
- .NET Standard 2.1

## 사용법

```csharp
using ViaLink;

// 초기화
ViaLinkSDK.Instance.Initialize("YOUR_API_KEY");

// 딥링크 콜백
ViaLinkSDK.Instance.OnDeepLink += (data) => {
    Debug.Log($"딥링크: {data.Path}");
};

// 이벤트 추적
ViaLinkSDK.Instance.TrackEvent("purchase", new Dictionary<string, object> {
    { "product_id", "123" }
});
```

## 포함 클래스

| 클래스 | 접근 | 설명 |
|--------|------|------|
| `ViaLinkSDK` | public | 메인 싱글턴 (초기화, 이벤트, 딥링크) |
| `DeepLinkData` | public | 딥링크 데이터 모델 |
| `DeviceInfo` | public | 디바이스 정보 모델 |
| `EventPayload` | public | 이벤트 페이로드 모델 |

## 개발자용 빌드

DLL 재빌드가 필요한 경우 (dotnet SDK 필요):

```bash
.build/build-dll.sh
```

## 문서

- [SDK 가이드](https://docs.vialink.app)
