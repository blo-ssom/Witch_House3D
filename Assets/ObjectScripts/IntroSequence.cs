using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 게임 시작 인트로 시퀀스.
/// 타이틀 → 편지 연출 → 문 열고 입장 → 플레이어 조작 전환.
///
/// 사용법:
///  1. 씬에 빈 GameObject 생성 → 이 컴포넌트 부착
///  2. 카메라 위치 세팅:
///     - letterLookPoint: 편지를 내려다보는 위치/각도 (빈 오브젝트)
///     - doorLookPoint: 현관문을 바라보는 위치/각도 (빈 오브젝트)
///     - playerStartPoint: 플레이어 조작 시작 위치 (빈 오브젝트)
///  3. UI 연결:
///     - titlePanel: 타이틀 화면 (게임시작 버튼 포함)
///     - letterPanel: 편지 UI (CanvasGroup)
///     - letterText: 편지 내용 TextMeshProUGUI
///     - fadePanel: 암전용 CanvasGroup
///  4. entranceDoor: 현관문 Door 컴포넌트
///  5. player 오브젝트에 PlayerMove, PlayerLook 연결
/// </summary>
public class IntroSequence : MonoBehaviour
{
    [Header("타이틀 UI")]
    public GameObject titlePanel;

    [Header("편지 UI")]
    public CanvasGroup letterPanel;
    public TextMeshProUGUI letterText;
    [TextArea(5, 15)]
    public string letterContent = "○○에게,\n\n나 여기 있어.\n이 편지가 도착하면 꼭 와줘.\n주소는 뒷면에 적어뒀어.\n\n기다리고 있을게.\n\n— A";

    [Header("암전")]
    public CanvasGroup fadePanel;
    public float fadeDuration = 1.5f;

    [Header("카메라 위치")]
    [Tooltip("편지를 내려다보는 카메라 위치 (빈 오브젝트)")]
    public Transform letterLookPoint;
    [Tooltip("현관문을 바라보는 카메라 위치 (빈 오브젝트)")]
    public Transform doorLookPoint;

    [Header("카메라 이동")]
    public float cameraMoveDuration = 2f;

    [Header("현관문")]
    public Door entranceDoor;

    [Header("플레이어")]
    public GameObject player;
    public Transform playerStartPoint;

    [Header("SFX")]
    public AudioSource audioSource;
    public AudioClip doorOpenSound;
    public AudioClip ambientSound;

    private Camera mainCamera;
    private bool waitingForInput = false;

    private void Start()
    {
        mainCamera = Camera.main;

        // 초기 상태: 플레이어 비활성화, 커서 표시
        if (player != null) player.SetActive(false);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 편지 숨기기
        if (letterPanel != null)
        {
            letterPanel.alpha = 0f;
            letterPanel.gameObject.SetActive(false);
        }

        // 타이틀 표시
        if (titlePanel != null)
            titlePanel.SetActive(true);

        // 암전 패널 숨기기
        if (fadePanel != null)
        {
            fadePanel.alpha = 0f;
            fadePanel.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 타이틀 화면의 "게임 시작" 버튼 OnClick에 연결
    /// </summary>
    public void OnStartButtonClicked()
    {
        StartCoroutine(IntroSequenceCoroutine());
    }

    private IEnumerator IntroSequenceCoroutine()
    {
        // 1. 타이틀 숨기기 + 암전
        if (titlePanel != null)
            titlePanel.SetActive(false);

        yield return StartCoroutine(Fade(0f, 1f));

        // 앰비언스 시작
        if (audioSource != null && ambientSound != null)
        {
            audioSource.clip = ambientSound;
            audioSource.loop = true;
            audioSource.Play();
        }

        // 2. 카메라를 편지 위치로 이동
        if (letterLookPoint != null)
        {
            mainCamera.transform.position = letterLookPoint.position;
            mainCamera.transform.rotation = letterLookPoint.rotation;
        }

        // 3. 페이드인 (편지를 내려다보는 장면)
        yield return StartCoroutine(Fade(1f, 0f));

        yield return new WaitForSeconds(0.5f);

        // 4. 편지 UI 표시
        if (letterPanel != null)
        {
            letterPanel.gameObject.SetActive(true);
            if (letterText != null)
                letterText.text = letterContent;

            yield return StartCoroutine(FadeCanvasGroup(letterPanel, 0f, 1f, 1f));
        }

        // 5. 입력 대기 (아무 키 또는 클릭)
        waitingForInput = true;
        yield return new WaitUntil(() => !waitingForInput);

        // 6. 편지 닫기
        if (letterPanel != null)
        {
            yield return StartCoroutine(FadeCanvasGroup(letterPanel, 1f, 0f, 0.5f));
            letterPanel.gameObject.SetActive(false);
        }

        yield return new WaitForSeconds(0.3f);

        // 7. 카메라가 천천히 올라가며 현관문을 바라봄
        if (doorLookPoint != null)
        {
            yield return StartCoroutine(MoveCamera(
                mainCamera.transform.position, mainCamera.transform.rotation,
                doorLookPoint.position, doorLookPoint.rotation,
                cameraMoveDuration
            ));
        }

        yield return new WaitForSeconds(0.5f);

        // 8. 현관문 열림
        if (entranceDoor != null)
        {
            if (audioSource != null && doorOpenSound != null)
                audioSource.PlayOneShot(doorOpenSound);

            entranceDoor.isOpen = true;
        }

        yield return new WaitForSeconds(1.5f);

        // 9. 암전 → 플레이어로 전환
        yield return StartCoroutine(Fade(0f, 1f));

        // 인트로 카메라 비활성화, 플레이어 활성화
        if (player != null)
        {
            player.SetActive(true);

            if (playerStartPoint != null)
            {
                player.transform.position = playerStartPoint.position;
                player.transform.rotation = playerStartPoint.rotation;
            }
        }

        // 인트로 카메라 끄기 (플레이어 카메라가 자동으로 활성화됨)
        mainCamera.gameObject.SetActive(false);

        yield return new WaitForSeconds(0.3f);

        // 10. 페이드인 → 플레이 시작
        yield return StartCoroutine(Fade(1f, 0f));

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Debug.Log("[IntroSequence] 인트로 완료. 플레이 시작!");
    }

    private void Update()
    {
        if (waitingForInput && (Input.anyKeyDown || Input.GetMouseButtonDown(0)))
        {
            waitingForInput = false;
        }
    }

    private IEnumerator Fade(float from, float to)
    {
        if (fadePanel == null) yield break;

        fadePanel.gameObject.SetActive(true);
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            fadePanel.alpha = Mathf.Lerp(from, to, elapsed / fadeDuration);
            yield return null;
        }

        fadePanel.alpha = to;

        if (to <= 0f)
            fadePanel.gameObject.SetActive(false);
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup group, float from, float to, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            group.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }

        group.alpha = to;
    }

    private IEnumerator MoveCamera(Vector3 fromPos, Quaternion fromRot, Vector3 toPos, Quaternion toRot, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);

            mainCamera.transform.position = Vector3.Lerp(fromPos, toPos, t);
            mainCamera.transform.rotation = Quaternion.Slerp(fromRot, toRot, t);
            yield return null;
        }

        mainCamera.transform.position = toPos;
        mainCamera.transform.rotation = toRot;
    }
}
