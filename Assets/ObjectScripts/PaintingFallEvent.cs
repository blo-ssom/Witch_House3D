using UnityEngine;

/// <summary>
/// 방1: 열쇠 획득 후 액자가 "안 보고 있을 때" 떨어진다.
///
/// 동작:
///  1. KeyItem이 방3 열쇠 픽업 시 Trigger() 호출 → armed = true
///  2. 매 LateUpdate마다 카메라 frustum 안에 액자가 있는지 확인
///  3. 시야 밖으로 나간 순간 → 위치/회전/스케일 교체 + 텍스처 교체 + 쿵 SFX (1회성)
///
/// 인스펙터 셋업:
///  - 액자 부모 오브젝트에 부착 (이동 대상이 이 transform)
///  - paintingRenderer: 사진 부분의 Renderer (텍스처 교체 대상)
///  - beforeTexture / afterTexture: 얼굴 있음 / 얼굴 없음 두 장
///  - fallenPosition / fallenRotation / fallenScale: 떨어진 후 좌표 (인스펙터 입력)
///  - audioSource + thudSound: "쿵" 사운드
/// </summary>
public class PaintingFallEvent : MonoBehaviour
{
    [Header("Painting Renderer (텍스처 교체 대상)")]
    [Tooltip("액자 안 사진 부분의 Renderer. material.mainTexture를 교체.")]
    public Renderer paintingRenderer;

    [Header("Textures")]
    [Tooltip("처음 사진 (얼굴 있음)")]
    public Texture beforeTexture;
    [Tooltip("떨어진 후 사진 (얼굴 없음)")]
    public Texture afterTexture;

    [Header("Fallen Transform (떨어진 후 좌표)")]
    public Vector3 fallenPosition = new Vector3(-29.567f, 1.004f, -35.85f);
    public Vector3 fallenRotation = new Vector3(716.08f, -2.3f, 59.003f);
    public Vector3 fallenScale    = new Vector3(2f, 0.1f, 3f);

    [Header("Visibility Check")]
    [Tooltip("시야 판정에 사용할 Renderer. 비워두면 paintingRenderer 사용.")]
    public Renderer visibilityTarget;
    [Tooltip("열쇠 픽업 후 이 시간(초)이 지나야 낙하 가능. 너무 빠른 트리거 방지.")]
    public float armDelay = 0.3f;

    [Header("SFX")]
    public AudioSource audioSource;
    public AudioClip   thudSound;

    private bool armed = false;
    private bool fired = false;
    private float armTime = -1f;

    private void Start()
    {
        if (paintingRenderer != null && beforeTexture != null)
            paintingRenderer.material.mainTexture = beforeTexture;

        if (visibilityTarget == null)
            visibilityTarget = paintingRenderer;
    }

    /// <summary>
    /// KeyItem.Interact()에서 방3 열쇠 픽업 시 호출.
    /// </summary>
    public void Trigger()
    {
        if (fired || armed) return;
        armed = true;
        armTime = Time.time;
        Debug.Log("[PaintingFallEvent] armed - 시야 밖으로 나가면 낙하");
    }

    private void LateUpdate()
    {
        if (!armed || fired) return;
        if (Time.time - armTime < armDelay) return;
        if (IsVisibleToPlayer()) return;

        FallNow();
    }

    private bool IsVisibleToPlayer()
    {
        if (visibilityTarget == null) return false;
        Camera cam = Camera.main;
        if (cam == null) return false;

        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(cam);
        return GeometryUtility.TestPlanesAABB(planes, visibilityTarget.bounds);
    }

    private void FallNow()
    {
        fired = true;
        armed = false;

        transform.position    = fallenPosition;
        transform.eulerAngles = fallenRotation;
        transform.localScale  = fallenScale;

        if (paintingRenderer != null && afterTexture != null)
            paintingRenderer.material.mainTexture = afterTexture;

        if (audioSource != null && thudSound != null)
            audioSource.PlayOneShot(thudSound);

        Debug.Log("[PaintingFallEvent] 액자 낙하!");
    }
}
