using UnityEngine;

/// <summary>
/// 방1: 열쇠 획득 후 액자가 "안 보고 있을 때" Rigidbody 물리로 자연 낙하.
/// PaintingFallEvent의 transform 텔레포트 방식 대안 — 둘 중 하나만 부착해 사용.
///
/// 동작:
///  1. KeyItem이 방3 열쇠 픽업 시 Trigger() 호출 → armed = true
///  2. 매 LateUpdate마다 카메라 frustum 안에 액자가 있는지 확인
///  3. 시야 밖으로 나간 순간 → Rigidbody.isKinematic = false 해제 → 자연 낙하
///     선택적으로 dropTorque/dropImpulse를 줘서 옆으로 쓰러지듯 떨어지게 함
///  4. 바닥과 충돌 시 1회 thud SFX 재생, 텍스처 교체
///
/// 인스펙터 셋업:
///  - 액자 부모 오브젝트에 부착 (Rigidbody도 같은 오브젝트에 부착)
///  - paintingBody: 시작 시 isKinematic = true 권장 (스크립트가 강제로 켬)
///  - paintingRenderer: 사진 부분 Renderer (텍스처 교체용)
///  - beforeTexture / afterTexture: 얼굴 있음 / 얼굴 없음
///  - dropTorque: 떨어질 때 회전 임펄스 (Z축 살짝 → 옆으로 쓰러지듯)
///  - 액자에 Collider 필수 (MeshCollider는 Convex 체크), 바닥에도 Collider 필요
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PaintingFallEventRB : MonoBehaviour
{
    [Header("Rigidbody (시작 시 isKinematic 강제)")]
    public Rigidbody paintingBody;

    [Header("Painting Renderer (텍스처 교체 대상)")]
    [Tooltip("액자 안 사진 부분의 Renderer. material.mainTexture를 교체.")]
    public Renderer paintingRenderer;

    [Header("Textures")]
    [Tooltip("처음 사진 (얼굴 있음)")]
    public Texture beforeTexture;
    [Tooltip("떨어진 후 사진 (얼굴 없음)")]
    public Texture afterTexture;
    [Tooltip("true면 충돌 시점에 교체, false면 낙하 시작 시점에 교체.")]
    public bool swapTextureOnImpact = true;

    [Header("Drop Physics")]
    [Tooltip("떨어질 때 회전 임펄스. (0,0,2) 정도면 옆으로 살짝 쓰러지듯.")]
    public Vector3 dropTorque = new Vector3(0f, 0f, 2f);
    [Tooltip("떨어질 때 위치 임펄스. 벽에서 살짝 밀려나오게 하려면 액자의 forward 방향 값을 줄 것.")]
    public Vector3 dropImpulse = Vector3.zero;

    [Header("Visibility Check")]
    [Tooltip("시야 판정에 사용할 Renderer. 비워두면 paintingRenderer 사용. 둘 다 비면 자기 transform 기준.")]
    public Renderer visibilityTarget;
    [Tooltip("얇은 액자(벽에 붙은) AABB 보정. 0.1~0.3 권장. frustum 가장자리에서 잘못 false 나오는 것 방지.")]
    public float boundsPadding = 0.2f;
    [Tooltip("시야에서 벗어났다고 판정되어도 이 시간(초) 동안 다시 보이면 취소. 깜빡임 방지.")]
    public float outOfViewHoldTime = 0.15f;
    [Tooltip("열쇠 픽업 후 이 시간(초)이 지나야 낙하 가능. 너무 빠른 트리거 방지.")]
    public float armDelay = 0.3f;
    [Tooltip("매 프레임 가시성 판정 결과를 콘솔에 찍어 디버깅.")]
    public bool debugVisibility = false;

    [Header("SFX")]
    public AudioSource audioSource;
    public AudioClip thudSound;
    [Tooltip("이 값 이상의 충돌 임팩트가 들어와야 thud 재생. 0이면 첫 충돌에 무조건 재생.")]
    public float thudImpactThreshold = 1f;

    private bool armed = false;
    private bool fired = false;
    private bool thudPlayed = false;
    private float armTime = -1f;
    private float lastVisibleTime = -1f;
    private Camera cachedCam;

    private void Start()
    {
        if (paintingBody == null)
            paintingBody = GetComponent<Rigidbody>();
        if (paintingBody != null)
        {
            paintingBody.isKinematic = true;
            paintingBody.useGravity = true;
        }

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
        Debug.Log("[PaintingFallEventRB] armed - 시야 밖으로 나가면 Rigidbody 낙하");
    }

    private void LateUpdate()
    {
        if (!armed || fired) return;
        if (Time.time - armTime < armDelay) return;

        bool visible = IsVisibleToPlayer();
        if (debugVisibility)
            Debug.Log($"[PaintingFallEventRB] visible={visible} t={Time.time:F2}");

        if (visible)
        {
            lastVisibleTime = Time.time;
            return;
        }

        // 마지막으로 보였던 시점에서 outOfViewHoldTime 이상 흘러야 낙하
        if (lastVisibleTime < 0f) lastVisibleTime = armTime; // 초기값
        if (Time.time - lastVisibleTime < outOfViewHoldTime) return;

        FallNow();
    }

    private bool IsVisibleToPlayer()
    {
        if (cachedCam == null) cachedCam = Camera.main;
        if (cachedCam == null) return true; // 카메라 못 찾으면 안전하게 "보임" 처리 → 낙하 방지

        // visibilityTarget 없으면 transform 위치 기준으로 fallback
        Bounds b;
        if (visibilityTarget != null)
            b = visibilityTarget.bounds;
        else
            b = new Bounds(transform.position, Vector3.one * 0.5f);

        if (boundsPadding > 0f)
            b.Expand(boundsPadding);

        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(cachedCam);
        return GeometryUtility.TestPlanesAABB(planes, b);
    }

    private void FallNow()
    {
        fired = true;
        armed = false;

        if (paintingBody != null)
        {
            paintingBody.isKinematic = false;
            if (dropTorque != Vector3.zero)
                paintingBody.AddTorque(dropTorque, ForceMode.Impulse);
            if (dropImpulse != Vector3.zero)
                paintingBody.AddForce(dropImpulse, ForceMode.Impulse);
        }

        if (!swapTextureOnImpact)
            SwapTexture();

        Debug.Log("[PaintingFallEventRB] Rigidbody 해제 - 자연 낙하 시작");
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!fired || thudPlayed) return;
        if (thudImpactThreshold > 0f && collision.relativeVelocity.magnitude < thudImpactThreshold) return;

        if (audioSource != null && thudSound != null)
            audioSource.PlayOneShot(thudSound);

        if (swapTextureOnImpact)
            SwapTexture();

        thudPlayed = true;
    }

    private void SwapTexture()
    {
        if (paintingRenderer != null && afterTexture != null)
            paintingRenderer.material.mainTexture = afterTexture;
    }
}
