using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 현관 탈출 트리거 → 페이드아웃 → 엔딩 씬 전환.
///
/// 사용법:
///  1. 현관 문 앞에 빈 GameObject + Box Collider (Is Trigger) 배치
///  2. 이 컴포넌트 부착
///  3. fadePanel: 검정 CanvasGroup 패널
///  4. endingSceneName: 엔딩 씬 이름 (없으면 크레딧 텍스트 표시 후 종료)
/// </summary>
public class EscapeTrigger : MonoBehaviour
{
    [Header("페이드아웃")]
    public CanvasGroup fadePanel;
    public float fadeOutDuration = 2f;

    [Header("씬 전환")]
    [Tooltip("엔딩 씬 이름 (비워두면 현재 씬에서 엔딩 텍스트 표시)")]
    public string endingSceneName = "";

    [Header("엔딩 UI (씬 전환 안 할 경우)")]
    public GameObject endingPanel;

    [Header("SFX")]
    public AudioSource audioSource;
    public AudioClip doorOpenSound;

    private bool triggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        if (!other.CompareTag("Player")) return;

        triggered = true;
        StartCoroutine(EscapeSequence(other.gameObject));
    }

    private IEnumerator EscapeSequence(GameObject player)
    {
        // 플레이어 이동 멈춤
        var move = player.GetComponent<PlayerMove>();
        if (move != null) move.enabled = false;
        var look = player.GetComponent<PlayerLook>();
        if (look != null) look.enabled = false;

        if (audioSource != null && doorOpenSound != null)
            audioSource.PlayOneShot(doorOpenSound);

        yield return new WaitForSeconds(0.5f);

        // 페이드아웃
        yield return StartCoroutine(FadeOut());

        yield return new WaitForSeconds(1f);

        // 씬 전환 또는 엔딩 연출
        if (!string.IsNullOrEmpty(endingSceneName))
        {
            SceneManager.LoadScene(endingSceneName);
        }
        else if (EndingSequence.Instance != null)
        {
            EndingSequence.Instance.StartEnding();
        }
        else if (endingPanel != null)
        {
            endingPanel.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private IEnumerator FadeOut()
    {
        if (fadePanel == null) yield break;

        fadePanel.gameObject.SetActive(true);
        float elapsed = 0f;

        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            fadePanel.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeOutDuration);
            yield return null;
        }

        fadePanel.alpha = 1f;
    }
}
