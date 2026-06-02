using System.Collections;
using UnityEngine;

/// <summary>
/// Mirror Branch (2층 east 분기, dead-end) 거울 손자국 연출 — B안.
///
/// 흐름 (GAME_DESIGN.md "Mirror Branch" / 손자국 안):
///  1. 콘솔 위 엄마 일기(NoteItem) 읽기
///     → 거울에서 낮은 울림(mirrorAwakenSfx) + 거울 E키 상호작용 활성화
///  2. 거울에 E키 (MirrorBranchInteract)
///     → 손자국이 거울 "안쪽에서" 하나씩 스르륵 떠오름 + SFX
///       ("거울 안에서 뭔가 나오려 한다" — 친구의 방 7단계 '귀신이 거울에서 튀어나옴'의 복선)
///  3. 연출 완료 → 조명 1회 깜빡 → 친구의 방 south 문 ForceUnlock (자동 해제)
///
/// 의도: "거울에서 뭔가 나오려 한다"를 학습 → 친구의 방에서 실제로 나옴으로써 회수.
///        거울 깨짐(파괴 payoff)은 친구의 방 클라이맥스에 양보하고, 여기선 손자국까지만.
///
/// 씬 셋업:
///  - 거울 GameObject: Layer=Interact, Collider(IsTrigger=OFF) + MirrorBranchInteract
///      · MirrorBranchInteract.manager = 본 컴포넌트
///  - handprints: 거울 표면에 붙인 손자국 데칼/Quad들 (시작 시 비활성). 떠오를 순서대로 배열에 등록.
///      · 페이드를 쓰려면 데칼 머티리얼이 Transparent (URP Lit/Unlit Transparent)여야 함.
/// </summary>
public class FakeRoomMirrorEvent : MonoBehaviour
{
    [Header("트리거 조건")]
    [Tooltip("콘솔 위 엄마 일기 NoteItem의 noteID")]
    public string motherDiaryNoteID = "mirrorbranch_mother_diary";

    [Header("발동 방식")]
    [Tooltip("켜면 일기를 읽고 메모창을 닫는 순간 손자국 연출이 뜸(거울 E키 불필요). 끄면 기존처럼 거울 상호작용 필요.")]
    public bool triggerImmediatelyOnDiary = true;

    [Header("거울 상호작용 (일기 읽은 뒤 활성화)")]
    [Tooltip("거울에 부착된 MirrorBranchInteract. 즉시 발동 모드에선 비워둬도 됨.")]
    public MirrorBranchInteract mirrorInteract;

    [Header("손자국 연출")]
    [Tooltip("거울 안쪽에서 떠오를 손자국들. 시작 시 모두 비활성. 떠오를 순서대로 등록.")]
    public GameObject[] handprints;
    [Tooltip("손자국 하나하나 사이 간격(초)")]
    public float handprintInterval = 0.6f;
    [Tooltip("손자국이 서서히 떠오르는 페이드 시간(초). 0이면 즉시 등장.")]
    public float handprintFadeDuration = 0.8f;

    [Header("조명 깜빡 (연출 마무리)")]
    public Light[] roomLights;
    public float flickerDuration = 0.4f;
    public float flickerInterval = 0.07f;

    [Header("문 잠금 해제 (연출 완료 시)")]
    [Tooltip("연출 완료 시 잠금 해제할 친구의 방 south 문")]
    public DoorInteract friendRoomSouthDoor;
    [Tooltip("연출 종료 후 잠금 해제까지 딜레이")]
    public float unlockDelay = 1.0f;

    [Header("SFX")]
    public AudioSource audioSource;
    [Tooltip("일기 읽은 직후 거울에서 나는 낮은 울림 (상호작용 활성 신호)")]
    public AudioClip mirrorAwakenSfx;
    [Tooltip("연출 시작 시 낮은 쿵/유리 울림")]
    public AudioClip mirrorRumbleSfx;
    [Tooltip("손자국 하나가 떠오를 때마다 재생")]
    public AudioClip handprintSfx;
    [Tooltip("south 문 잠금 해제 알림 (선택)")]
    public AudioClip unlockSfx;

    private bool diaryRead = false;
    private bool sequencePlayed = false;
    private bool waitingForNoteClose = false;

    private void Start()
    {
        // 손자국 전부 숨김
        if (handprints != null)
            foreach (var hp in handprints)
                if (hp != null) hp.SetActive(false);
    }

    private void OnEnable()
    {
        NoteItem.OnNoteRead += OnNoteRead;
    }

    private void OnDisable()
    {
        NoteItem.OnNoteRead -= OnNoteRead;
    }

    private void OnNoteRead(string noteID)
    {
        if (diaryRead) return;
        if (noteID != motherDiaryNoteID) return;

        diaryRead = true;

        if (triggerImmediatelyOnDiary)
        {
            // 일기를 읽고 메모창을 닫는 순간 발동. 노트가 열려있으면 닫힐 때까지 대기.
            if (NoteUI.Instance != null && NoteUI.Instance.IsOpen())
                waitingForNoteClose = true;
            else
                TriggerMirrorSequence();   // 노트 UI를 못 찾으면 즉시 폴백
            return;
        }

        // (기존 방식) 거울 상호작용 활성화 + 낮은 울림으로 "거울이 깨어났다" 신호
        if (mirrorInteract != null)
            mirrorInteract.SetReady(true);

        if (audioSource != null && mirrorAwakenSfx != null)
            audioSource.PlayOneShot(mirrorAwakenSfx);
    }

    private void Update()
    {
        // 메모창이 닫히는 순간 손자국 연출 발동
        if (waitingForNoteClose && (NoteUI.Instance == null || !NoteUI.Instance.IsOpen()))
        {
            waitingForNoteClose = false;
            TriggerMirrorSequence();
        }
    }

    /// <summary>
    /// 거울 E키 상호작용(MirrorBranchInteract)이 호출. 1회성.
    /// </summary>
    public void TriggerMirrorSequence()
    {
        if (sequencePlayed) return;
        if (!diaryRead) return;            // 안전장치 — 일기 안 읽었으면 무시
        sequencePlayed = true;

        // 거울 상호작용 닫기 (1회성)
        if (mirrorInteract != null)
            mirrorInteract.SetReady(false);

        StartCoroutine(HandprintSequence());
    }

    private IEnumerator HandprintSequence()
    {
        // 1. 낮은 쿵/유리 울림
        if (audioSource != null && mirrorRumbleSfx != null)
            audioSource.PlayOneShot(mirrorRumbleSfx);

        yield return new WaitForSeconds(0.4f);

        // 2. 손자국이 거울 안쪽에서 하나씩 떠오름
        if (handprints != null)
        {
            foreach (var hp in handprints)
            {
                if (hp == null) continue;

                hp.SetActive(true);

                if (audioSource != null && handprintSfx != null)
                    audioSource.PlayOneShot(handprintSfx);

                if (handprintFadeDuration > 0f)
                    yield return StartCoroutine(FadeInRenderer(hp, handprintFadeDuration));

                yield return new WaitForSeconds(handprintInterval);
            }
        }

        // 3. 마무리 조명 깜빡 (불안 마침표)
        yield return StartCoroutine(FlickerLights(flickerDuration));

        // 4. 친구의 방 south 문 잠금 자동 해제
        yield return new WaitForSeconds(unlockDelay);

        if (friendRoomSouthDoor != null)
            friendRoomSouthDoor.ForceUnlock();

        if (audioSource != null && unlockSfx != null)
            audioSource.PlayOneShot(unlockSfx);
    }

    /// <summary>
    /// 손자국 Renderer 머티리얼 알파를 0→1로 페이드 (안쪽에서 스르륵 떠오르는 느낌).
    /// 데칼 머티리얼이 Transparent여야 보임. URP는 _BaseColor, 구형은 _Color.
    /// Opaque 머티리얼이면 페이드 효과 없이 SetActive 등장으로 폴백.
    /// </summary>
    private IEnumerator FadeInRenderer(GameObject go, float duration)
    {
        Renderer rend = go.GetComponentInChildren<Renderer>();
        if (rend == null) yield break;

        Material mat = rend.material;   // 인스턴스 생성 (1회성 연출이라 허용)
        string prop = mat.HasProperty("_BaseColor") ? "_BaseColor"
                    : (mat.HasProperty("_Color") ? "_Color" : null);
        if (prop == null) yield break;

        Color c = mat.GetColor(prop);
        float endA = c.a <= 0f ? 1f : c.a;   // 머티리얼 알파가 0이면 목표를 1로

        c.a = 0f;                            // 시작은 투명 (등장 프레임 팝 방지)
        mat.SetColor(prop, c);

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            c.a = Mathf.Lerp(0f, endA, t / duration);
            mat.SetColor(prop, c);
            yield return null;
        }

        c.a = endA;
        mat.SetColor(prop, c);
    }

    private IEnumerator FlickerLights(float duration)
    {
        if (roomLights == null || roomLights.Length == 0) yield break;

        // 원래 상태 저장
        bool[] originalEnabled = new bool[roomLights.Length];
        for (int i = 0; i < roomLights.Length; i++)
            if (roomLights[i] != null) originalEnabled[i] = roomLights[i].enabled;

        float t = 0f;
        while (t < duration)
        {
            bool on = Random.value > 0.5f;
            for (int i = 0; i < roomLights.Length; i++)
                if (roomLights[i] != null) roomLights[i].enabled = on;
            yield return new WaitForSeconds(flickerInterval);
            t += flickerInterval;
        }

        // 원래 상태로 복원
        for (int i = 0; i < roomLights.Length; i++)
            if (roomLights[i] != null) roomLights[i].enabled = originalEnabled[i];
    }
}
