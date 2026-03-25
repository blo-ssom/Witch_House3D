using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 2층 계단 근처 바닥 판자 부서짐 + 지하 씬 전환.
///
/// 사용법:
///  1. 빈 GameObject → 이름: FloorBreakTrigger
///  2. Box Collider 추가 → Is Trigger 체크
///  3. FloorBreakTrigger 컴포넌트 부착
///  4. planks 배열에 판자 큐브들 연결 (Rigidbody 없어도 됨, 자동 추가)
///  5. undergroundSceneName → "Underground" 입력
/// </summary>
public class FloorBreakTrigger : MonoBehaviour
{
    [Header("판자 오브젝트들")]
    [Tooltip("부서질 판자 큐브 배열")]
    public GameObject[] planks;

    [Header("씬 전환")]
    public string undergroundSceneName = "Underground";

    [Header("연출 설정")]
    public float breakDelay      = 0.1f;   // 판자 하나씩 부서지는 딜레이
    public float fallDuration    = 1.5f;   // 떨어지는 시간
    public float fadeOutDuration = 1.0f;   // 암전 시간
    public float sceneLoadDelay  = 2.5f;   // 씬 전환까지 대기

    [Header("SFX")]
    public AudioSource audioSource;
    public AudioClip   breakSound;

    [Header("암전 UI")]
    [Tooltip("검정 Panel UI (FadePanel)")]
    public CanvasGroup fadePanel;

    private bool triggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        if (!other.CompareTag("Player")) return;

        triggered = true;
        StartCoroutine(FloorBreakSequence(other.gameObject));
    }

    private IEnumerator FloorBreakSequence(GameObject player)
    {
        Debug.Log("밟힘!");
        // 1. 플레이어 이동 멈춤
        var playerMove = player.GetComponent<PlayerMove>();
        if (playerMove != null) playerMove.enabled = false;

        // 2. 판자 하나씩 Rigidbody 활성화 → 물리적으로 떨어짐
        foreach (var plank in planks)
        {
            if (plank == null) continue;

            // Rigidbody 없으면 자동 추가
            Rigidbody rb = plank.GetComponent<Rigidbody>();
            if (rb == null) rb = plank.AddComponent<Rigidbody>();

            rb.isKinematic = false;

            // 살짝 랜덤한 힘으로 자연스럽게
            rb.AddForce(new Vector3(
                Random.Range(-1f, 1f),
                Random.Range(-3f, -5f),
                Random.Range(-1f, 1f)
            ) * 3f, ForceMode.Impulse);

            rb.AddTorque(Random.insideUnitSphere * 2f, ForceMode.Impulse);

            if (audioSource != null && breakSound != null)
                audioSource.PlayOneShot(breakSound);

            yield return new WaitForSeconds(breakDelay);
        }

        // 3. 플레이어도 아래로 떨어짐
        StartCoroutine(FallPlayer(player));

        // 4. 암전
        yield return new WaitForSeconds(fallDuration);
        yield return StartCoroutine(FadeOut());

        // 5. 씬 전환
        yield return new WaitForSeconds(0.5f);
        SceneManager.LoadScene(undergroundSceneName);
    }

    private IEnumerator FallPlayer(GameObject player)
    {
        // CharacterController 비활성화 후 직접 Y 이동
        var cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        float elapsed = 0f;
        while (elapsed < fallDuration)
        {
            elapsed += Time.deltaTime;
            player.transform.position += Vector3.down * 8f * Time.deltaTime;
            yield return null;
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
