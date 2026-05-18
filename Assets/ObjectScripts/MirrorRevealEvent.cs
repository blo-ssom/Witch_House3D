using System.Collections;
using UnityEngine;

/// <summary>
/// 방2 거울 — 옵션 B (페이크 텍스처 스왑).
/// 어두운 Quad + 사전 제작 텍스처 2장(빈 의자 / 의자+조각) 스왑 방식.
/// 실시간 반사 카메라 사용 안 함. 2층 친구의 방 거울 시스템과 별도.
///
/// 흐름:
///  1. 플레이어가 거울 앞에서 E키 (Interact)
///  2. 거울 머티리얼이 어두운 색으로 페이드 아웃
///  3. revealTexture(의자+조각)로 swap
///  4. 페이드 인 (거울 속에 의자 위 조각 보임)
///  5. WaitUntil(카메라가 chair 방향 응시) — chairLookDot 임계값
///  6. 실제 의자 위 piecePhotoPiece SetActive(true)
///  7. 거울 다시 페이드 아웃 → emptyTexture 원복 → 페이드 인
///
/// 의도: 거울 = 정보의 매개. 거울 속 단서를 본 후 의자에 가서 실제 조각을 발견.
///       조각이 거울에서 *현실로 이동*했다는 시각적 함의.
///
/// 사용법 (인스펙터 슬롯):
///  - mirrorRenderer  : 거울 Quad의 MeshRenderer
///  - emptyTexture    : 빈 의자 텍스처 (시작 상태, null이면 검정 시작)
///  - revealTexture   : 의자+조각 텍스처 (조사 후 표시)
///  - chair           : 실제 의자 Transform (뒤돌아봄 감지 기준)
///  - piecePhotoPiece : 실제 의자 위 PhotoPiece(id=2) GameObject (시작 시 비활성)
///  - playerCamera    : Main Camera
///
/// 씬 세팅:
///  - 거울 Quad에 BoxCollider(IsTrigger 체크 안 함, 보통 Collider) + 이 컴포넌트
///  - Layer = Interact
///  - 머티리얼: URP/Unlit, 베이스 색 흰색 (텍스처 곱연산 정상화)
/// </summary>
public class MirrorRevealEvent : Interactable
{
    [Header("거울 머티리얼")]
    [Tooltip("거울 Quad의 Renderer. material 호출로 인스턴스화하여 원본 머티리얼 보호")]
    public Renderer mirrorRenderer;
    [Tooltip("시작 상태 텍스처 (빈 의자). null이면 시작 시 검정")]
    public Texture emptyTexture;
    [Tooltip("페이드 인 시 표시될 텍스처 (의자+조각)")]
    public Texture revealTexture;
    [Tooltip("페이드 시 사용할 어두운 색상")]
    public Color darkColor = new Color(0.03f, 0.03f, 0.03f, 1f);

    [Header("조각 활성화")]
    [Tooltip("실제 의자 위에 활성화될 PhotoPiece (id=2, 시작 시 비활성)")]
    public GameObject piecePhotoPiece;
    [Tooltip("뒤돌아봄 감지 대상 — 실제 의자 Transform")]
    public Transform chair;
    public Camera playerCamera;
    [Range(0f, 1f)]
    [Tooltip("카메라 forward · 의자 방향 dot 임계값. 0.5 ≈ 60도 이내. 너무 빡빡하면 트리거 안 됨")]
    public float chairLookDot = 0.5f;

    [Header("타이밍")]
    public float fadeOutDuration = 1f;
    public float holdDuration = 0.2f;
    public float fadeInDuration = 1f;
    [Tooltip("의자 안 보면 이 시간 후 강제로 조각 활성화. 0이면 무한 대기")]
    public float maxWaitForLookback = 0f;

    [Header("SFX")]
    public AudioSource audioSource;
    public AudioClip mirrorActivateSfx;
    public AudioClip pieceRevealSfx;

    private Material mirrorMatInstance;
    private bool triggered = false;

    private static readonly int BaseMapID = Shader.PropertyToID("_BaseMap");
    private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");
    private static readonly int MainTexID = Shader.PropertyToID("_MainTex");
    private static readonly int ColorID = Shader.PropertyToID("_Color");

    private void Awake()
    {
        interactPrompt = "E : 거울을 살펴본다";

        if (piecePhotoPiece != null)
            piecePhotoPiece.SetActive(false);

        if (mirrorRenderer != null)
            mirrorMatInstance = mirrorRenderer.material;

        SetMainTexture(emptyTexture);
        SetMainColor(emptyTexture != null ? Color.white : darkColor);
    }

    public override string GetInteractPrompt()
    {
        return triggered ? "" : interactPrompt;
    }

    public override void Interact(PlayerInventory playerInventory)
    {
        if (triggered) return;
        triggered = true;
        StartCoroutine(RevealSequence());
    }

    private IEnumerator RevealSequence()
    {
        if (audioSource != null && mirrorActivateSfx != null)
            audioSource.PlayOneShot(mirrorActivateSfx);

        Color startColor = emptyTexture != null ? Color.white : darkColor;

        yield return StartCoroutine(FadeColor(startColor, darkColor, fadeOutDuration));

        SetMainTexture(revealTexture);

        yield return new WaitForSeconds(holdDuration);

        yield return StartCoroutine(FadeColor(darkColor, Color.white, fadeInDuration));

        float waitElapsed = 0f;
        while (!IsLookingAtChair())
        {
            if (maxWaitForLookback > 0f && waitElapsed >= maxWaitForLookback)
                break;
            waitElapsed += Time.deltaTime;
            yield return null;
        }

        if (piecePhotoPiece != null)
            piecePhotoPiece.SetActive(true);

        if (audioSource != null && pieceRevealSfx != null)
            audioSource.PlayOneShot(pieceRevealSfx);

        yield return StartCoroutine(FadeColor(Color.white, darkColor, 0.5f));
        SetMainTexture(emptyTexture);
        Color endColor = emptyTexture != null ? Color.white : darkColor;
        yield return StartCoroutine(FadeColor(darkColor, endColor, 0.5f));

        Debug.Log("[MirrorRevealEvent] 조각 3 활성화 완료");
    }

    private bool IsLookingAtChair()
    {
        if (chair == null || playerCamera == null) return false;
        Vector3 toChair = (chair.position - playerCamera.transform.position).normalized;
        return Vector3.Dot(playerCamera.transform.forward, toChair) > chairLookDot;
    }

    private void SetMainTexture(Texture tex)
    {
        if (mirrorMatInstance == null) return;
        if (mirrorMatInstance.HasProperty(BaseMapID))
            mirrorMatInstance.SetTexture(BaseMapID, tex);
        else if (mirrorMatInstance.HasProperty(MainTexID))
            mirrorMatInstance.SetTexture(MainTexID, tex);
    }

    private void SetMainColor(Color c)
    {
        if (mirrorMatInstance == null) return;
        if (mirrorMatInstance.HasProperty(BaseColorID))
            mirrorMatInstance.SetColor(BaseColorID, c);
        else if (mirrorMatInstance.HasProperty(ColorID))
            mirrorMatInstance.SetColor(ColorID, c);
    }

    private IEnumerator FadeColor(Color from, Color to, float duration)
    {
        if (mirrorMatInstance == null || duration <= 0f)
        {
            SetMainColor(to);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            SetMainColor(Color.Lerp(from, to, t));
            yield return null;
        }
        SetMainColor(to);
    }
}
