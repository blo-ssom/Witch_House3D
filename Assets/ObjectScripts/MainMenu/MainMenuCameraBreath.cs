using UnityEngine;

/// <summary>
/// 메인메뉴 카메라 — Layers of Fear Remake 스타일.
/// 정적 1인칭 시점에서 호흡/스웨이로 살아있는 느낌만 줌.
///
/// 셋업:
///  1. Camera에 이 컴포넌트 부착
///  2. 인스펙터에서 카메라를 메뉴 방 안의 원하는 위치/회전으로 배치
///  3. Play 시 자동으로 시작 위치/회전을 기억하고 그 주변에서 미세하게 흔들림
/// </summary>
public class MainMenuCameraBreath : MonoBehaviour
{
    [Header("Breath (호흡 — 위아래 미세 이동)")]
    public bool enableBreath = true;
    public float breathAmplitude = 0.015f;   // 위아래 진폭 (m)
    public float breathFrequency = 0.25f;    // Hz (0.25 = 4초에 한 번)

    [Header("Head Sway (시점이 살짝 떠다님)")]
    public bool enableHeadSway = true;
    public float swayAmplitudeX = 0.5f;      // pitch 진폭 (도)
    public float swayAmplitudeY = 0.7f;      // yaw 진폭 (도)
    public float swayFrequencyX = 0.18f;     // Hz
    public float swayFrequencyY = 0.12f;     // Hz

    [Header("Slow Pan (매우 느린 좌우 패닝)")]
    [Tooltip("켜면 한쪽으로 천천히 돌다가 반대로 돌아옴 (시네마틱).")]
    public bool enableSlowPan = false;
    public float panAmplitude = 3f;          // 도
    public float panFrequency = 0.04f;       // 매우 느림

    [Header("Mouse Parallax (선택 — 마우스 따라 살짝 기울임)")]
    public bool enableMouseParallax = true;
    public float parallaxStrength = 1.5f;    // 도 (최대)
    public float parallaxSmoothness = 4f;

    private Vector3 basePosition;
    private Quaternion baseRotation;
    private Vector2 currentParallax;

    private void Start()
    {
        basePosition = transform.localPosition;
        baseRotation = transform.localRotation;
    }

    private void LateUpdate()
    {
        float t = Time.time;

        // ---- Breath: 위아래 미세 이동 ----
        Vector3 breath = Vector3.zero;
        if (enableBreath)
        {
            float b = Mathf.Sin(t * breathFrequency * Mathf.PI * 2f);
            breath.y = b * breathAmplitude;
        }

        // ---- Head Sway: 회전 ----
        float pitch = 0f, yaw = 0f;
        if (enableHeadSway)
        {
            pitch = Mathf.Sin(t * swayFrequencyX * Mathf.PI * 2f) * swayAmplitudeX;
            yaw   = Mathf.Sin(t * swayFrequencyY * Mathf.PI * 2f) * swayAmplitudeY;
        }

        // ---- Slow Pan: 시네마틱 좌우 ----
        float pan = 0f;
        if (enableSlowPan)
        {
            pan = Mathf.Sin(t * panFrequency * Mathf.PI * 2f) * panAmplitude;
        }

        // ---- Mouse Parallax ----
        Vector2 parallaxTarget = Vector2.zero;
        if (enableMouseParallax)
        {
            // -1..1 범위로 정규화 (화면 중앙=0)
            Vector3 m = Input.mousePosition;
            parallaxTarget.x = (m.x / Screen.width  - 0.5f) * 2f;
            parallaxTarget.y = (m.y / Screen.height - 0.5f) * 2f;
            parallaxTarget = Vector2.ClampMagnitude(parallaxTarget, 1f) * parallaxStrength;
        }
        currentParallax = Vector2.Lerp(currentParallax, parallaxTarget,
                                       Time.deltaTime * parallaxSmoothness);

        // ---- 합성 ----
        transform.localPosition = basePosition + breath;
        transform.localRotation = baseRotation
            * Quaternion.Euler(pitch - currentParallax.y, yaw + pan + currentParallax.x, 0f);
    }
}
