using System.Collections;
using UnityEngine;

/// <summary>
/// 방1 핵심 퍼즐: 넘어진 액자를 바로 세우면 뒤에서 열쇠가 등장.
///
/// 기존 시스템 연동:
///  - hiddenKey 슬롯에 KeyItem 컴포넌트가 붙은 오브젝트 연결 (시작 시 비활성화)
///  - NoteItem + NoteUI 시스템과 동일한 구조 사용
///
/// 사용법:
///  1. 넘어진 액자 메쉬 오브젝트에 이 컴포넌트 부착
///  2. fallenRotation   : 현재 쓰러진 localEulerAngles (인스펙터에서 확인)
///  3. uprightRotation  : 세워진 상태 localEulerAngles (ex. 0,0,0)
///  4. hiddenKey        : KeyItem 붙은 열쇠 오브젝트 연결 (시작 시 비활성화)
///  5. Layer → Interact 레이어로 설정
/// </summary>
public class PaintingFlip : Interactable
{
    [Header("Painting Rotation")]
    public Vector3 fallenRotation  = new Vector3(90f, 0f, 0f);
    public Vector3 uprightRotation = new Vector3(0f,  0f, 0f);
    public float   flipDuration    = 1.2f;

    [Header("Hidden Key (KeyItem 오브젝트 연결)")]
    public GameObject hiddenKey;

    [Header("SFX")]
    public AudioSource audioSource;
    public AudioClip   flipSound;
    public AudioClip   keyRevealSound;

    private bool isFlipped   = false;
    private bool isAnimating = false;

    private void Start()
    {
        interactPrompt = "E : 액자를 바로 세우다";

        if (hiddenKey != null)
            hiddenKey.SetActive(false);
    }

    public override string GetInteractPrompt()
    {
        return isFlipped ? "E : 액자 뒤를 조사하다" : interactPrompt;
    }

    public override void Interact(PlayerInventory playerInventory)
    {
        Debug.Log("[PaintingFlip] Interact 호출됨");  // 추가
        if (isAnimating) return;

        if (!isFlipped)
            StartCoroutine(FlipPainting());
        // 세워진 후에는 hiddenKey의 KeyItem이 별도로 상호작용 처리
    }

    private IEnumerator FlipPainting()
    {
        isAnimating = true;

        // 사운드 재생
        if (audioSource != null && flipSound != null)
            audioSource.PlayOneShot(flipSound);

        // 회전 애니메이션
        Quaternion from = Quaternion.Euler(fallenRotation);
        Quaternion to   = Quaternion.Euler(uprightRotation);
        float elapsed   = 0f;

        while (elapsed < flipDuration)
        {
            elapsed += Time.deltaTime;
            float t  = Mathf.SmoothStep(0f, 1f, elapsed / flipDuration);
            transform.localRotation = Quaternion.Lerp(from, to, t);
            yield return null;
        }

        transform.localRotation = to;
        isFlipped  = true;
        isAnimating = false;

        // 0.3초 후 열쇠 등장
        yield return new WaitForSeconds(0.3f);
        RevealKey();
    }

    private void RevealKey()
    {
        if (hiddenKey == null) return;

        hiddenKey.SetActive(true);

        if (audioSource != null && keyRevealSound != null)
            audioSource.PlayOneShot(keyRevealSound);

        Debug.Log("[PaintingFlip] 열쇠 등장!");
    }
}
