using UnityEngine;

/// <summary>
/// 메인메뉴 카메라 — 타겟 주변을 천천히 궤도 회전 + 미세한 sway.
/// 마녀의 집 모델을 LookAt 타겟으로 두면 됨.
///
/// 셋업:
///  1. MainMenu.unity 씬에 Camera 배치
///  2. 집 모델 위치에 빈 GameObject "CamTarget" 생성
///  3. 이 컴포넌트 카메라에 부착 → target 슬롯에 CamTarget 연결
/// </summary>
public class MainMenuCameraOrbit : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("궤도 설정")]
    public float orbitRadius = 8f;
    public float orbitHeight = 2.5f;
    public float orbitSpeed = 3f;       // 도/초
    public float startAngle = 0f;       // 시작 각도

    [Header("Sway (살짝 떠다니는 느낌)")]
    public bool enableSway = true;
    public float swayAmplitudeY = 0.15f;
    public float swayFrequency = 0.3f;

    [Header("Look")]
    public Vector3 lookAtOffset = new Vector3(0f, 1.5f, 0f);

    private float currentAngle;

    private void Start()
    {
        currentAngle = startAngle;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        currentAngle += orbitSpeed * Time.deltaTime;
        float rad = currentAngle * Mathf.Deg2Rad;

        float swayY = enableSway
            ? Mathf.Sin(Time.time * swayFrequency * Mathf.PI * 2f) * swayAmplitudeY
            : 0f;

        Vector3 offset = new Vector3(
            Mathf.Cos(rad) * orbitRadius,
            orbitHeight + swayY,
            Mathf.Sin(rad) * orbitRadius
        );

        transform.position = target.position + offset;
        transform.LookAt(target.position + lookAtOffset);
    }
}
