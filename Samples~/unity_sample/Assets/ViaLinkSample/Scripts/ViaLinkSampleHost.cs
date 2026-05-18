using System.Collections.Generic;
using UnityEngine;
using ViaLink;

namespace ViaLinkSample
{
    /// 메인 샘플 UI. IMGUI(OnGUI) 기반이라 별도 씬 편집이 필요 없다.
    /// RuntimeInitializeOnLoad 로 DontDestroyOnLoad 오브젝트에 자동 부착된다.
    public class ViaLinkSampleHost : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateHost()
        {
            var go = new GameObject("ViaLinkSampleHost");
            DontDestroyOnLoad(go);
            go.AddComponent<ViaLinkSampleHost>();
        }

        // ─── 상태 ───
        private DeepLinkBus.Result _activeDialog;
        private string _toastMessage;
        private float _toastUntil;
        private Vector2 _scroll;

        private void Update()
        {
            // 콜백/비동기 결과 polling
            if (_activeDialog == null)
            {
                var next = DeepLinkBus.TryDequeue();
                if (next != null) _activeDialog = next;
            }
        }

        private void OnGUI()
        {
            // 모바일 DPI 보정 — IMGUI 의 fontSize/fixedHeight 는 픽셀 단위라 고DPI 디바이스에서 너무 작게 보임.
            // GUI.matrix 로 전체를 scale 하면 좌표/폰트 모두 한꺼번에 확대됨. logical 좌표 (실제 px / scale) 로 계산.
            // baseline 160dpi (mdpi). 모바일은 보통 320~460dpi 라 scale 2~3 배. clamp 로 너무 작거나 큰 값 방지.
            float scale = Screen.dpi <= 0 ? 1f : Mathf.Clamp(Screen.dpi / 160f, 1f, 4f);
            Matrix4x4 prevMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));

            // 안전영역(노치/홈인디케이터) 보정 — 모두 logical 좌표
            Rect safe = new Rect(
                Screen.safeArea.x / scale,
                Screen.safeArea.y / scale,
                Screen.safeArea.width / scale,
                Screen.safeArea.height / scale
            );

            GUI.skin.label.fontSize = 16;
            GUI.skin.button.fontSize = 18;
            GUI.skin.button.fixedHeight = 56;
            GUI.skin.box.fontSize = 16;

            // Modal: dialog 가 활성일 때 background controls 의 click 흡수.
            // GUI.enabled = false 로 만들면 그 이후 모든 control 이 hit test 자체를 건너뜀 → 순서/위치 무관하게
            // 뒷화면 버튼 절대 안 눌림. 시각적으로는 살짝 회색으로 보일 수 있음 (Unity disabled 기본 스타일).
            bool modal = (_activeDialog != null);
            GUI.enabled = !modal;

            DrawTopBar(safe);
            DrawScrollContent(safe);
            DrawToast(safe);

            // Dialog 자체는 정상 인터랙션
            GUI.enabled = true;
            DrawAlertDialog(scale);

            GUI.matrix = prevMatrix;
        }

        // ─── 상단 앱바 ───
        private void DrawTopBar(Rect safe)
        {
            var rect = new Rect(safe.x, safe.y, safe.width, 70);
            GUI.Box(rect, "");
            var labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
            };
            GUI.Label(rect, "Vialink Unity Sample", labelStyle);
        }

        // ─── 본문 ───
        private void DrawScrollContent(Rect safe)
        {
            var rect = new Rect(safe.x + 16, safe.y + 80, safe.width - 32, safe.height - 96);
            var contentHeight = 1400;
            _scroll = GUI.BeginScrollView(rect, _scroll, new Rect(0, 0, rect.width - 24, contentHeight));

            float y = 0;
            float w = rect.width - 24;

            y = Section("이벤트 추적", y, w);
            y = Button("회원가입 이벤트 전송", y, w, () =>
            {
                ViaLinkSDK.Instance.TrackEvent("signup", null);
                Toast("✅ 회원가입 이벤트 전송 완료");
                Debug.Log("[ViaLink] track: signup");
            });
            y = Button("구매 이벤트 전송", y, w, () =>
            {
                ViaLinkSDK.Instance.TrackEvent("purchase", new Dictionary<string, object>
                {
                    { "product_id", "12345" },
                    { "revenue", 29900 },
                    { "currency", "KRW" },
                });
                Toast("✅ 구매 이벤트 전송 완료");
                Debug.Log("[ViaLink] track: purchase");
            });
            y = Button("장바구니 추가 이벤트 전송", y, w, () =>
            {
                ViaLinkSDK.Instance.TrackEvent("add_to_cart", new Dictionary<string, object>
                {
                    { "product_id", "12345" },
                });
                Toast("✅ 장바구니 추가 이벤트 전송 완료");
                Debug.Log("[ViaLink] track: add_to_cart");
            });

            y = Divider(y, w);
            y = Section("링크 생성", y, w);
            y = Button("딥링크 생성 (referral, dynamic)", y, w, () => OnCreateLink("dynamic", "referral"));
            y = Button("정적 링크 생성 (notice, static)", y, w, () => OnCreateLink("static", "notice"));

            y = Divider(y, w);
            y = Section("데이터 가져오기 (Pull API)", y, w);
            y = Button("딥링크 가져오기 (Sync)", y, w, OnGetDeepLinkData);
            y = Button("딥링크 대기 (Async, 3초 타임아웃)", y, w, OnAwaitDeepLinkData);
            y = Button("디퍼드 딥링크 (Sync)", y, w, OnGetDeferredLinkData);
            y = Button("디퍼드 딥링크 대기 (Async)", y, w, OnAwaitDeferredLinkData);

            y = Divider(y, w);
            y = Section("결제 추적", y, w);
            y = Button("결제 시도 (initiated)", y, w, OnPaymentInitiated);

            y = Divider(y, w);
            y = DrawInfoCard(y, w);

            GUI.EndScrollView();
        }

        // ─── 버튼 핸들러 ───
        private void OnCreateLink(string linkType, string campaign)
        {
            Toast($"🔗 {linkType} 링크 생성 요청 중...");
            Debug.Log($"[ViaLink] CreateLink 호출 (linkType={linkType}, campaign={campaign})");

            // CreateLink 는 비동기. 결과는 DeepLinkBus 를 통해 AlertDialog 로 표시.
            ViaLinkSDK.Instance.CreateLink(
                path: "/product/12345",
                campaign: campaign,
                linkType: linkType,
                onSuccess: shortUrl =>
                {
                    Debug.Log($"[ViaLink] [{linkType}] 링크 생성 성공: {shortUrl}");
                    DeepLinkBus.Post($"[{linkType}] 링크 생성 성공", shortUrl, copyableText: shortUrl);
                },
                onError: err =>
                {
                    Debug.LogError($"[ViaLink] [{linkType}] 링크 생성 실패: {err}");
                    DeepLinkBus.Post($"[{linkType}] 링크 생성 실패", string.IsNullOrEmpty(err) ? "알 수 없는 오류" : err);
                });
        }

        // ─── Pull API 핸들러 (SDK v3.1.0+) ───

        private void OnGetDeepLinkData()
        {
            var data = ViaLinkSDK.Instance.GetDeepLinkData();
            if (data == null)
            {
                DeepLinkBus.Post(
                    "GetDeepLinkData() 결과",
                    "캐시된 딥링크 없음.\n앱이 딥링크로 진입한 적이 없거나, 아직 /v1/resolve 응답이 도착하지 않은 상태입니다."
                );
                return;
            }
            DeepLinkBus.Post(
                "GetDeepLinkData() 결과",
                $"경로: {data.Path}\n파라미터: {FormatParams(data.Params)}\nshortCode: {data.ShortCode ?? "없음"}"
            );
        }

        private void OnAwaitDeepLinkData()
        {
            Toast("⏳ 다음 딥링크 대기 중 (3초)...");
            ViaLinkSDK.Instance.AwaitDeepLinkData(data =>
            {
                if (data == null)
                {
                    DeepLinkBus.Post(
                        "AwaitDeepLinkData() 결과",
                        "3초 안에 딥링크가 도착하지 않았습니다.\n버튼을 누른 직후 adb / Universal Link 로 vialink.app/unity/<code> 를 열어보세요."
                    );
                    return;
                }
                DeepLinkBus.Post(
                    "AwaitDeepLinkData() 결과",
                    $"경로: {data.Path}\n파라미터: {FormatParams(data.Params)}\nshortCode: {data.ShortCode ?? "없음"}"
                );
            }, timeoutSeconds: 3f);
        }

        private void OnGetDeferredLinkData()
        {
            var data = ViaLinkSDK.Instance.GetDeferredLinkData();
            if (data == null)
            {
                DeepLinkBus.Post(
                    "GetDeferredLinkData() 결과",
                    "디퍼드 매칭 결과 없음 (organic install 이거나 아직 매칭 진행 중)."
                );
                return;
            }
            DeepLinkBus.Post(
                "GetDeferredLinkData() 결과",
                $"경로: {data.Path}\n파라미터: {FormatParams(data.Params)}\nshortCode: {data.ShortCode ?? "없음"}"
            );
        }

        private void OnAwaitDeferredLinkData()
        {
            Toast("⏳ 디퍼드 매칭 결과 대기 중...");
            ViaLinkSDK.Instance.AwaitDeferredLinkData((data, error) =>
            {
                if (error != null)
                {
                    DeepLinkBus.Post(
                        "AwaitDeferredLinkData() 실패",
                        $"코드: {error.Code}\n메시지: {error.Message}\n재시도 가능: {error.Retryable}"
                    );
                    return;
                }
                if (data == null)
                {
                    DeepLinkBus.Post(
                        "AwaitDeferredLinkData() 결과",
                        "매칭 결과 없음 (organic install)."
                    );
                    return;
                }
                DeepLinkBus.Post(
                    "AwaitDeferredLinkData() 결과",
                    $"경로: {data.Path}\n파라미터: {FormatParams(data.Params)}\nshortCode: {data.ShortCode ?? "없음"}"
                );
            });
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

        private void OnPaymentInitiated()
        {
            Toast("💳 결제 시도 요청 중...");
            Debug.Log("[ViaLink] PaymentInitiated 호출");

            var args = new PaymentInitiatedArgs
            {
                OrderId = "ORD-2026-0001",
                Amount = 19900d,
                Currency = "KRW",
                PaymentMethod = "card",
                Metadata = new Dictionary<string, object>
                {
                    { "product_id", "prod-001" },
                },
            };

            ViaLinkSDK.Instance.PaymentInitiated(
                args,
                onSuccess: result =>
                {
                    Debug.Log($"[ViaLink] 결제 시도 완료: paymentEventId={result.PaymentEventId}");
                    DeepLinkBus.Post(
                        "결제 시도 완료",
                        $"success: {result.Success}\npaymentEventId: {result.PaymentEventId}"
                    );
                },
                onError: err =>
                {
                    Debug.LogError($"[ViaLink] 결제 시도 실패: {err}");
                    DeepLinkBus.Post("결제 시도 실패", string.IsNullOrEmpty(err) ? "알 수 없는 오류" : err);
                });
        }

        // ─── UI 헬퍼 ───
        private float Section(string title, float y, float w)
        {
            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
            };
            GUI.Label(new Rect(0, y, w, 36), title, style);
            return y + 40;
        }

        private float Divider(float y, float w)
        {
            GUI.Box(new Rect(0, y, w, 2), "");
            return y + 14;
        }

        private float Button(string label, float y, float w, System.Action onClick)
        {
            if (GUI.Button(new Rect(0, y, w, 56), label))
            {
                onClick?.Invoke();
            }
            return y + 64;
        }

        private float DrawInfoCard(float y, float w)
        {
            string sdkVersion = SafeGetSdkVersion();
            string body =
                $"• SDK 버전: {sdkVersion}\n" +
                "• 딥링크/디퍼드 결과는 AlertDialog로 표시됩니다\n" +
                "• 로그는 'ViaLink' 태그로 Unity Console / logcat / Xcode 콘솔에 출력됩니다\n" +
                "• 이벤트는 SDK 내부에서 배치 전송됩니다 (30초 주기)";
            GUI.Box(new Rect(0, y, w, 140), "SDK 정보\n\n" + body);
            return y + 160;
        }

        private static string SafeGetSdkVersion()
        {
            try { return ViaLinkSDK.SdkVersion; }
            catch { return "알 수 없음"; }
        }

        // ─── Toast / AlertDialog ───
        private void Toast(string message)
        {
            _toastMessage = message;
            _toastUntil = Time.realtimeSinceStartup + 2.0f;
        }

        private void DrawToast(Rect safe)
        {
            if (string.IsNullOrEmpty(_toastMessage)) return;
            if (Time.realtimeSinceStartup > _toastUntil)
            {
                _toastMessage = null;
                return;
            }
            var style = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 16,
            };
            float w = Mathf.Min(safe.width - 80, 600);
            var rect = new Rect(safe.x + (safe.width - w) / 2, safe.y + safe.height - 130, w, 60);
            GUI.Box(rect, _toastMessage, style);
        }

        private void DrawAlertDialog(float scale)
        {
            if (_activeDialog == null) return;

            // GUI.matrix scale 적용 상태이므로 Screen.width/Height 도 logical 좌표로 환산
            float screenW = Screen.width / scale;
            float screenH = Screen.height / scale;

            // 어두운 dim 배경 — 화면 전체에 반투명 검정 깔아서 modal 임을 시각적으로 강조.
            // 입력 차단은 OnGUI 의 GUI.enabled 가 처리하므로 여기 invisible button 불필요.
            // (이전에 깔았던 invisible button 이 dialog 버튼보다 먼저 hit 받아서 확인 버튼이 안 눌렸던 버그 수정.)
            Color prevColor = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.55f);
            GUI.DrawTexture(new Rect(0, 0, screenW, screenH), Texture2D.whiteTexture);
            GUI.color = prevColor;

            float w = Mathf.Min(screenW - 80, 700);
            float h = 320;
            float x = (screenW - w) / 2;
            float y = (screenH - h) / 2;

            GUI.Box(new Rect(x, y, w, h), "");

            var titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
            };
            GUI.Label(new Rect(x, y + 16, w, 32), _activeDialog.Title, titleStyle);

            var bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                wordWrap = true,
            };
            GUI.Label(new Rect(x + 20, y + 60, w - 40, h - 140), _activeDialog.Message, bodyStyle);

            // 버튼 영역
            float btnW = _activeDialog.CopyableText != null ? (w - 60) / 2 : w - 40;
            if (_activeDialog.CopyableText != null)
            {
                if (GUI.Button(new Rect(x + 20, y + h - 70, btnW, 50), "복사하기"))
                {
                    GUIUtility.systemCopyBuffer = _activeDialog.CopyableText;
                    Toast("📋 링크가 복사되었습니다");
                    _activeDialog = null;
                    return;
                }
                if (GUI.Button(new Rect(x + 40 + btnW, y + h - 70, btnW, 50), "확인"))
                {
                    _activeDialog = null;
                }
            }
            else
            {
                if (GUI.Button(new Rect(x + 20, y + h - 70, btnW, 50), "확인"))
                {
                    _activeDialog = null;
                }
            }
        }
    }
}
