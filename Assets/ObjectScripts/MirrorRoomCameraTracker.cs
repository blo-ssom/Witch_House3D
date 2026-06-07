using UnityEngine;

/// <summary>
/// 미러룸 카메라에 플레이어 시점 시차(parallax)를 더해 "3D 거울" 느낌을 만든다.
///
/// === 동작 원리 (roomAnchor/realMirror 불필요) ===
/// 게임 시작 시점에:
///  - 미러 카메라의 현재 위치/회전 = "기본 화면"으로 캡처 (이미 잘 맞던 그 배치)
///  - playerReferencePoint(플레이어가 거울 볼 때 서는 지점) = "기준 시점"으로 캡처
/// 이후 매 프레임:
///  - 플레이어가 기준점에서 움직인 만큼만 미러 카메라를 이동/회전
/// → 기본 화면은 항상 맞고(원래 배치 그대로), 움직이면 깊이감(시차)이 생김.
/// 반사 행렬/oblique/스케일 문제 전혀 없음.
///
/// === 셋업 (간단) ===
///  1. 미러룸 카메라(RT_Mirror에 출력 중, 위치가 이미 잘 맞는 그 카메라)에 이 컴포넌트 부착
///  2. playerCamera         : 플레이어 메인 카메라 Transform
///  3. playerReferencePoint : 빈 GameObject를 "플레이어가 거울 앞에서 보는 위치(눈높이)"에 두고 연결
///       └ 비워두면 게임 시작 순간의 플레이어 위치를 기준으로 자동 캡처
///         (단, 시작 시 플레이어가 거울 앞에 있어야 정확)
///
/// 좌우 반전(진짜 거울 느낌)은 거울 머티리얼 Base Map Tiling X = -1 로.
/// </summary>
[DisallowMultipleComponent]
public class MirrorRoomCameraTracker : MonoBehaviour
{
    [Header("필수 참조")]
    [Tooltip("미러룸을 찍는 카메라. 비우면 이 오브젝트의 Camera 사용")]
    public Camera mirrorCamera;
    [Tooltip("플레이어 메인 카메라 Transform")]
    public Transform playerCamera;
    [Tooltip("플레이어가 거울을 보는 기준 위치(눈높이) 빈 오브젝트. 비우면 시작 시 플레이어 위치로 자동 캡처")]
    public Transform playerReferencePoint;

    [Header("옵션")]
    [Tooltip("플레이어 카메라 FOV를 미러 카메라에 복사. 끄면 미러 카메라 자체 FOV 유지")]
    public bool matchPlayerFOV = false;
    [Tooltip("화면이 옆으로 굴러가면(귀신 눕는 현상) 카메라 수평(roll=0) 강제")]
    public bool keepUpright = true;
    [Tooltip("플레이어 이동 대비 거울 속 시차 강도. 1=실제, 작게=은은하게")]
    public float parallaxStrength = 1f;

    [Header("성능 최적화")]
    [Tooltip("기준점에서 이 거리 안에 있을 때만 미러 카메라 렌더. 0 이하면 항상 켬")]
    public float activationDistance = 6f;

    // 시작 시 캡처되는 기준값
    private Vector3 baseCamPos;
    private Quaternion mapRot;       // baseCamRot * Inverse(refRot)
    private Vector3 refPos;
    private bool calibrated = false;

    private bool active = true;
    private Camera playerCam;

    private void Reset()
    {
        mirrorCamera = GetComponent<Camera>();
    }

    private void Start()
    {
        if (mirrorCamera == null) mirrorCamera = GetComponent<Camera>();
        if (playerCamera != null) playerCam = playerCamera.GetComponent<Camera>();

        if (mirrorCamera == null || playerCamera == null)
        {
            Debug.LogError("[MirrorRoomCameraTracker] mirrorCamera/playerCamera를 연결하세요.");
            enabled = false;
            return;
        }

        Calibrate();
    }

    /// <summary>기준 화면/시점 캡처. 필요 시 런타임에 다시 호출 가능.</summary>
    public void Calibrate()
    {
        Vector3 rPos = playerReferencePoint != null ? playerReferencePoint.position : playerCamera.position;
        Quaternion rRot = playerReferencePoint != null ? playerReferencePoint.rotation : playerCamera.rotation;

        baseCamPos = mirrorCamera.transform.position;
        Quaternion baseCamRot = mirrorCamera.transform.rotation;

        mapRot = baseCamRot * Quaternion.Inverse(rRot);
        refPos = rPos;
        calibrated = true;
    }

    private void LateUpdate()
    {
        if (!calibrated || mirrorCamera == null || playerCamera == null) return;

        UpdateActivation();
        if (!active) return;

        // 플레이어가 기준점에서 움직인 양만큼만 미러 카메라를 옮긴다
        Vector3 delta = (playerCamera.position - refPos) * parallaxStrength;
        mirrorCamera.transform.position = baseCamPos + mapRot * delta;

        Quaternion rot = mapRot * playerCamera.rotation;

        if (keepUpright)
        {
            Vector3 fwd = rot * Vector3.forward;
            Vector3 upRef = Mathf.Abs(Vector3.Dot(fwd.normalized, Vector3.up)) > 0.99f
                ? rot * Vector3.up : Vector3.up;
            mirrorCamera.transform.rotation = Quaternion.LookRotation(fwd, upRef);
        }
        else
        {
            mirrorCamera.transform.rotation = rot;
        }

        if (matchPlayerFOV && playerCam != null)
            mirrorCamera.fieldOfView = playerCam.fieldOfView;
    }

    private void UpdateActivation()
    {
        if (activationDistance <= 0f) { SetActive(true); return; }
        float dist = Vector3.Distance(playerCamera.position, refPos);
        SetActive(dist <= activationDistance);
    }

    private void SetActive(bool on)
    {
        if (active == on) return;
        active = on;
        mirrorCamera.enabled = on; // 멀면 렌더 멈춰 성능 절약
    }
}
