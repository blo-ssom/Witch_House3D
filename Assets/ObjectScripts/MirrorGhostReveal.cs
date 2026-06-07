using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 길1(방법 A) 거울 연출 — "검은 거울 + 귀신 겹침".
/// 거울은 방을 비추지 않는 어두운 면일 뿐이고, 트리거되면 거울 표면에
/// 겹쳐둔 귀신(별도 GameObject)이 서서히 떠오른다(페이드인).
/// 플레이어가 뒤돌아보면 사라진다. 사진 캡처/실시간 반사 없음.
///
/// 사진 티가 안 나는 이유: 방 반사를 흉내내지 않고 검은 면에 귀신만
/// 올리므로 "시점이 안 맞는다"는 문제 자체가 없다.
///
/// === 에디터 셋업 (요약) ===
///  1. 거울 Quad에 검고 어두운 Unlit 머티리얼 (방을 안 비춤)
///  2. 거울 바로 앞(플레이어 쪽)으로 0.01~0.05m 띄워 귀신 오브젝트를 겹쳐 배치
///     - 귀신 머티리얼은 반드시 Transparent (URP Lit/Unlit Surface=Transparent)
///       → 알파 페이드가 먹는다. Opaque면 즉시 등장으로 폴백
///     - 평면 귀신이면 거울과 같은 방향으로, 3D 모델이면 살짝 안쪽
///  3. 이 스크립트를 거울 Quad에 부착 (Collider 필요 — E키 레이캐스트)
///  4. Layer = Interact, ghost / playerCamera 슬롯 연결
///  5. (옵션) 다른 이벤트가 TriggerReveal()을 직접 호출해도 됨
/// </summary>
[RequireComponent(typeof(Collider))]
public class MirrorGhostReveal : Interactable
{
    [Header("핵심 슬롯")]
    [Tooltip("거울 표면에 겹쳐둔 귀신 오브젝트 (시작 시 자동 비활성)")]
    public GameObject ghost;
    [Tooltip("플레이어 카메라 — 뒤돌아봄 감지용 (비우면 Camera.main 시도)")]
    public Camera playerCamera;

    [Header("페이드 타이밍 (초)")]
    [Tooltip("귀신이 서서히 떠오르는 시간")]
    public float fadeInDuration = 2f;
    [Tooltip("귀신이 사라지는 시간 (뒤돌아본 순간)")]
    public float fadeOutDuration = 0.4f;

    [Header("뒤돌아봄 감지")]
    [Tooltip("거울 반대쪽을 이만큼 보면 '뒤돌아봤다'로 판정 (1=정반대, 낮출수록 헐겁게)")]
    [Range(-1f, 1f)] public float lookAwayDot = 0.3f;
    [Tooltip("뒤돌아보지 않아도 이 시간이 지나면 자동 종료 (0=무한 대기)")]
    public float maxWait = 8f;
    [Tooltip("페이드인 직후 이 시간 동안은 뒤돌아봐도 안 사라짐 (귀신을 충분히 보게)")]
    public float minVisible = 0.5f;

    [Header("프롬프트")]
    public string readyPrompt = "E : 거울을 들여다본다";

    [Header("연출 종료 후 (선택)")]
    [Tooltip("연출이 끝나면 발행 — 거울 깨짐 / 추격 시작 등 다음 단계 연결")]
    public UnityEvent onRevealFinished;

    private bool ready = true;
    private bool playing = false;
    private Renderer[] ghostRenderers;
    private MaterialPropertyBlock mpb;
    private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor"); // URP Lit/Unlit
    private static readonly int ColorID = Shader.PropertyToID("_Color");         // 구형/Standard

    private void Awake()
    {
        interactPrompt = readyPrompt;
        mpb = new MaterialPropertyBlock();

        if (ghost != null)
        {
            ghostRenderers = ghost.GetComponentsInChildren<Renderer>(true);
            ghost.SetActive(false);
        }
        if (playerCamera == null) playerCamera = Camera.main;
    }

    // 일기 등 선행 조건 전에는 거울 상호작용을 막고 싶을 때 사용
    public void SetReady(bool value) => ready = value;

    public override string GetInteractPrompt()
        => (ready && !playing) ? readyPrompt : "";

    public override void Interact(PlayerInventory playerInventory) => TriggerReveal();

    /// <summary>외부(Floor2MirrorEvent 등)에서도 직접 호출 가능. 1회성.</summary>
    public void TriggerReveal()
    {
        if (playing || !ready) return;
        if (ghost == null)
        {
            Debug.LogWarning("[MirrorGhostReveal] ghost 슬롯이 비어 있어 연출을 건너뜀");
            return;
        }
        StartCoroutine(RevealSequence());
    }

    private IEnumerator RevealSequence()
    {
        playing = true;
        ready = false;

        ghost.SetActive(true);
        SetGhostAlpha(0f);

        // 1) 서서히 떠오름
        yield return Fade(0f, 1f, fadeInDuration);

        // 2) 최소 노출 시간 보장 (이 동안은 뒤돌아봐도 안 꺼짐)
        if (minVisible > 0f) yield return new WaitForSeconds(minVisible);

        // 3) 뒤돌아봄 또는 타임아웃까지 대기
        float t = 0f;
        while (true)
        {
            if (LookedAway()) break;
            if (maxWait > 0f && t >= maxWait) break;
            t += Time.deltaTime;
            yield return null;
        }

        // 4) 사라짐
        yield return Fade(1f, 0f, fadeOutDuration);
        ghost.SetActive(false);

        onRevealFinished?.Invoke();
        playing = false;
    }

    private bool LookedAway()
    {
        if (playerCamera == null) return false;
        // 거울이 바라보는 방향(transform.forward)의 반대를 보면 뒤돌아본 것
        float dot = Vector3.Dot(playerCamera.transform.forward, -transform.forward);
        return dot > lookAwayDot;
    }

    private IEnumerator Fade(float from, float to, float dur)
    {
        if (dur <= 0f) { SetGhostAlpha(to); yield break; }
        float e = 0f;
        while (e < dur)
        {
            e += Time.deltaTime;
            SetGhostAlpha(Mathf.Lerp(from, to, e / dur));
            yield return null;
        }
        SetGhostAlpha(to);
    }

    private void SetGhostAlpha(float a)
    {
        if (ghostRenderers == null) return;
        foreach (var r in ghostRenderers)
        {
            if (r == null) continue;
            r.GetPropertyBlock(mpb);

            Color baseCol = Color.white;
            var mat = r.sharedMaterial;
            if (mat != null)
            {
                if (mat.HasProperty(BaseColorID)) baseCol = mat.GetColor(BaseColorID);
                else if (mat.HasProperty(ColorID)) baseCol = mat.GetColor(ColorID);
            }
            baseCol.a = a;

            mpb.SetColor(BaseColorID, baseCol);
            mpb.SetColor(ColorID, baseCol);
            r.SetPropertyBlock(mpb);
        }
    }
}
