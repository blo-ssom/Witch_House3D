using System.Collections;
using UnityEngine;

/// <summary>
/// 지하 씬 2차 추격 이벤트.
/// 씬 시작 시 암전 → 페이드인(눈 뜨기) → 자유 탐색 → 제단 오브젝트 조사 시 추격 시작.
///
/// 사용법:
///  1. UnderGround 씬에 빈 GameObject 생성 → 이 컴포넌트 부착
///  2. fadePanel: 검정 CanvasGroup 패널 (시작 시 alpha=1)
///  3. ghostChase: 지하 귀신의 GhostChase 컴포넌트
///  4. playerMove / playerLook: 플레이어 컴포넌트 (페이드인 동안 입력 차단용)
///  5. 제단 오브젝트에 AltarInteractable 부착
/// </summary>
public class UndergroundChaseEvent : MonoBehaviour
{
    [Header("페이드인 (눈 뜨기)")]
    public CanvasGroup fadePanel;
    public float fadeInDuration = 2f;

    [Header("귀신")]
    public GhostChase ghostChase;
    public GameObject ghostObject;

    [Header("플레이어")]
    public PlayerMove playerMove;
    public PlayerLook playerLook;
    [Tooltip("지하 진입 시 손전등을 다시 켜고(2층 추격의 강제 OFF/감쇠를 해제) 밝기를 복구한다.")]
    public PlayerFlashlight playerFlashlight;

    [Header("추격 시작 연출")]
    [Tooltip("제단 조사 후 추격 시작까지 딜레이")]
    public float chaseStartDelay = 1.5f;
    [Tooltip("추격 중 손전등을 어둡게 할지 (기본 OFF — 그냥 켜진 채 유지)")]
    public bool dimFlashlightInChase = false;
    [Range(0f, 1f)] public float chaseFlashlightDim = 0.5f;

    [Header("SFX")]
    public AudioSource audioSource;
    public AudioClip ambientSound;
    public AudioClip altarSound;
    [Tooltip("지하 추격 시작 시 루프 재생할 BGM (예: Chase.wav)")]
    public AudioClip chaseBgm;
    [Range(0f, 1f)] public float chaseBgmVolume = 0.7f;
    [Tooltip("추격 직전 멀리서 들리는 경고음(숨소리/비명) — 갑작스러운 등장 완화용")]
    public AudioClip warningSfx;
    [Tooltip("경고음 후 실제 추격이 시작되기까지의 긴장 대기 시간")]
    public float buildupDelay = 2f;

    public static UndergroundChaseEvent Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // Player가 DontDestroyOnLoad로 넘어왔다면 인스펙터 슬롯이 비어있음 → Tag로 자동 검색
        if (playerMove == null || playerLook == null || playerFlashlight == null)
        {
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                if (playerMove == null) playerMove = playerObj.GetComponent<PlayerMove>();
                if (playerLook == null) playerLook = playerObj.GetComponentInChildren<PlayerLook>();
                if (playerFlashlight == null) playerFlashlight = playerObj.GetComponentInChildren<PlayerFlashlight>();
            }
            // 그래도 못 찾으면 씬 전체에서 검색 (지하 자체 플레이어용)
            if (playerFlashlight == null) playerFlashlight = FindObjectOfType<PlayerFlashlight>();
        }

        if (fadePanel != null)
        {
            fadePanel.gameObject.SetActive(true);
            fadePanel.alpha = 1f;
        }

        if (playerMove != null) playerMove.enabled = false;
        if (playerLook != null) playerLook.enabled = false;

        // 안전장치: ghostObject가 실수로 이 매니저(또는 그 부모)를 가리키면
        // 자기 자신을 비활성화해 코루틴이 죽으므로 무시한다.
        if (ghostObject != null && ghostObject != gameObject &&
            !transform.IsChildOf(ghostObject.transform))
            ghostObject.SetActive(false);
        else if (ghostObject == gameObject || (ghostObject != null && transform.IsChildOf(ghostObject.transform)))
            Debug.LogError("[UndergroundChaseEvent] ghostObject가 매니저 자신/부모를 가리킵니다. 지하 귀신으로 다시 연결하세요.", this);

        StartCoroutine(WakeUpSequence());
    }

    private IEnumerator WakeUpSequence()
    {
        yield return new WaitForSeconds(1f);

        if (audioSource != null && ambientSound != null)
            audioSource.PlayOneShot(ambientSound);

        yield return StartCoroutine(FadeIn());

        if (playerMove != null) playerMove.enabled = true;
        if (playerLook != null) playerLook.enabled = true;

        // 지하 진입 시 손전등 복구: 2층 추격에서 강제 OFF/감쇠된 상태를 풀고 다시 켠다.
        if (playerFlashlight != null)
        {
            playerFlashlight.ReleaseForce();
            playerFlashlight.SetDim(1f);
            playerFlashlight.SetOn(true);
        }

        Debug.Log("[UndergroundChaseEvent] 눈 뜨기 완료. 자유 탐색 시작.");
    }

    /// <summary>
    /// 마지막 추격 시작. 촛불 퍼즐(CandlePuzzleManager) 또는 제단 등 어디서든 호출 가능.
    /// 효과음 없이 추격 시퀀스만 시작한다(호출 측에서 자기 SFX를 재생).
    /// </summary>
    public void TriggerChase()
    {
        StartCoroutine(StartChaseSequence());
    }

    /// <summary>
    /// (구) 제단 방식 호환용. AltarInteractable에서 호출 — 제단 SFX 재생 후 추격.
    /// 현재 설계는 촛불 퍼즐 → KeyItem.onPickup → CandlePuzzleManager가 TriggerChase()를 직접 호출.
    /// </summary>
    public void OnAltarInvestigated()
    {
        if (audioSource != null && altarSound != null)
            audioSource.PlayOneShot(altarSound);

        TriggerChase();
    }

    private IEnumerator StartChaseSequence()
    {
        yield return new WaitForSeconds(chaseStartDelay);

        // 빌드업: 추격 직전 멀리서 경고음 + 긴장 대기 (갑작스러운 등장 완화)
        if (audioSource != null && warningSfx != null)
            audioSource.PlayOneShot(warningSfx);
        yield return new WaitForSeconds(buildupDelay);

        // 추격 BGM 루프 시작
        if (chaseBgm != null && audioSource != null)
        {
            audioSource.clip = chaseBgm;
            audioSource.loop = true;
            audioSource.volume = chaseBgmVolume;
            audioSource.Play();
        }

        // (선택) 추격 중 손전등 감쇠
        if (dimFlashlightInChase && playerFlashlight != null)
            playerFlashlight.SetDim(chaseFlashlightDim);

        if (ghostObject != null)
            ghostObject.SetActive(true);

        if (ghostChase != null)
            ghostChase.StartChase();

        Debug.Log("[UndergroundChaseEvent] 제단 조사 → 2차 추격 시작!");
    }

    /// <summary>추격 BGM을 페이드아웃하고 멈춘다 (엔딩 진입 등).</summary>
    public void FadeOutChaseBgm(float dur = 2f)
    {
        if (audioSource != null && audioSource.isPlaying)
            StartCoroutine(FadeBgmRoutine(dur));
    }

    private IEnumerator FadeBgmRoutine(float dur)
    {
        float start = audioSource.volume;
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(start, 0f, t / dur);
            yield return null;
        }
        audioSource.Stop();
        audioSource.volume = start;
    }

    private IEnumerator FadeIn()
    {
        if (fadePanel == null) yield break;

        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            fadePanel.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeInDuration);
            yield return null;
        }

        fadePanel.alpha = 0f;
        fadePanel.gameObject.SetActive(false);
    }
}
