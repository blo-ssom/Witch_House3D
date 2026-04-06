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

    [Header("추격 시작 연출")]
    [Tooltip("제단 조사 후 추격 시작까지 딜레이")]
    public float chaseStartDelay = 1.5f;

    [Header("SFX")]
    public AudioSource audioSource;
    public AudioClip ambientSound;
    public AudioClip altarSound;

    public static UndergroundChaseEvent Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (fadePanel != null)
        {
            fadePanel.gameObject.SetActive(true);
            fadePanel.alpha = 1f;
        }

        if (playerMove != null) playerMove.enabled = false;
        if (playerLook != null) playerLook.enabled = false;

        if (ghostObject != null)
            ghostObject.SetActive(false);

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

        Debug.Log("[UndergroundChaseEvent] 눈 뜨기 완료. 자유 탐색 시작.");
    }

    /// <summary>
    /// AltarInteractable에서 호출. 제단 조사 시 추격 시작.
    /// </summary>
    public void OnAltarInvestigated()
    {
        if (audioSource != null && altarSound != null)
            audioSource.PlayOneShot(altarSound);

        StartCoroutine(StartChaseSequence());
    }

    private IEnumerator StartChaseSequence()
    {
        yield return new WaitForSeconds(chaseStartDelay);

        if (ghostObject != null)
            ghostObject.SetActive(true);

        if (ghostChase != null)
            ghostChase.StartChase();

        Debug.Log("[UndergroundChaseEvent] 제단 조사 → 2차 추격 시작!");
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
