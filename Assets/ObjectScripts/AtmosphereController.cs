using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 런타임 분위기 컨트롤러 — Fog(안개) + Ambient(전역 환경광)를 어둡게 설정해
/// 같은 공간을 즉시 호러 톤으로 바꾼다.
///
/// 핵심 아이디어:
///  - Ambient를 거의 검정으로 깔면 "기본 상태 = 어둠"이 되고,
///    손전등 / Point Light가 비추는 곳만 보이게 된다 (공포의 기본).
///  - 짧은 거리 Fog로 복도 끝을 어둠에 잠기게 해 휑함을 가린다.
///
/// 사용법:
///  1. 빈 GameObject 하나 생성 (이름 예: _Atmosphere)
///  2. 이 스크립트 부착
///  3. 인스펙터에서 값 조정 → 에디터 씬 뷰/게임 뷰에 즉시 반영됨
///     (안 보이면 컴포넌트 우클릭 → "지금 적용")
///
/// 주의: 전역 RenderSettings를 바꾸므로 씬당 하나만 두면 된다.
/// </summary>
[ExecuteAlways]
public class AtmosphereController : MonoBehaviour
{
    [Header("Ambient (전역 환경광)")]
    [Tooltip("거의 검정으로 두면 손전등 / Point Light만 보인다. 기획 권장: #050505 근처")]
    [ColorUsage(false, false)]
    public Color ambientColor = new Color(0.02f, 0.02f, 0.02f, 1f);

    [Header("Fog (안개)")]
    public bool enableFog = true;

    [Tooltip("안개 색. 약간 차가운 어두운 색이 호러에 잘 맞음")]
    [ColorUsage(false, false)]
    public Color fogColor = new Color(0.03f, 0.03f, 0.04f, 1f);

    public enum FogKind { Linear, Exponential }
    [Tooltip("Linear: 시작~끝 거리로 직관적 제어 / Exponential: 밀도로 제어")]
    public FogKind fogKind = FogKind.Linear;

    [Header("Linear 모드 (fogKind = Linear 일 때)")]
    [Tooltip("이 거리부터 안개가 끼기 시작 (m)")]
    public float linearStart = 2f;
    [Tooltip("이 거리에서 완전히 안개에 잠김 (m). 손전등 사거리(약 14m) 근처 권장")]
    public float linearEnd = 14f;

    [Header("Exponential 모드 (fogKind = Exponential 일 때)")]
    [Tooltip("안개 밀도. 0.02(옅음) ~ 0.1(짙음) 권장")]
    [Range(0f, 0.2f)]
    public float density = 0.06f;

    private void OnEnable()   { Apply(); }
    private void OnValidate() { Apply(); }   // 인스펙터 값 변경 시 즉시 반영

    [ContextMenu("지금 적용")]
    public void Apply()
    {
        // --- Ambient: 단색(Flat)으로 어둡게 ---
        RenderSettings.ambientMode  = AmbientMode.Flat;
        RenderSettings.ambientLight = ambientColor;

        // --- Fog ---
        RenderSettings.fog = enableFog;
        if (enableFog)
        {
            RenderSettings.fogColor = fogColor;

            if (fogKind == FogKind.Linear)
            {
                RenderSettings.fogMode          = FogMode.Linear;
                RenderSettings.fogStartDistance = linearStart;
                RenderSettings.fogEndDistance   = Mathf.Max(linearStart + 0.1f, linearEnd);
            }
            else
            {
                RenderSettings.fogMode    = FogMode.ExponentialSquared;
                RenderSettings.fogDensity = density;
            }
        }
    }
}
