using System.Collections;
using UnityEngine;

/// <summary>
/// 시선 추적 초상화에 천을 씌우는 상호작용.
/// 기존 Interactable 상속 → PlayerInteraction 자동 연동.
///
/// 사용법:
///  1. 같은 초상화 오브젝트에 PortraitTracker 와 함께 부착
///  2. coverObject  : 천 오브젝트 연결 (시작 시 비활성화)
///  3. keyToReveal  : 방2 열쇠 KeyItem 오브젝트 연결 (시작 시 비활성화)
///  4. Layer → Interact 레이어 설정
/// </summary>
public class PortraitCover : Interactable
{
    [Header("덮개")]
    [Tooltip("천/덮개 오브젝트 (시작 시 비활성화)")]
    public GameObject coverObject;

    [Header("보상")]
    [Tooltip("클리어 시 등장할 KeyItem 오브젝트 (시작 시 비활성화)")]
    public GameObject keyToReveal;

    [Header("연출")]
    public AudioSource audioSource;
    public AudioClip   coverSound;
    public AudioClip   keyRevealSound;
    public Light[]     roomLights;
    [Tooltip("천 씌운 후 조명 밝기 비율")]
    [Range(0f, 1f)]
    public float afterLightRatio = 0.5f;

    private bool isCovered = false;
    private PortraitTracker tracker;

    private void Start()
    {
        interactPrompt = "E : 천을 씌우다";
        tracker = GetComponent<PortraitTracker>();

        if (coverObject != null)
            coverObject.SetActive(false);

        if (keyToReveal != null)
            keyToReveal.SetActive(false);
    }

    public override string GetInteractPrompt()
    {
        // 아직 시선 추적 시작 전이면 상호작용 안 보임
        if (tracker != null && !tracker.enabled)
            return "";

        return isCovered ? "" : interactPrompt;
    }

    public override void Interact(PlayerInventory playerInventory)
    {
        if (isCovered) return;

        isCovered = true;

        // 시선 추적 멈춤
        if (tracker != null)
            tracker.StopTracking();

        StartCoroutine(CoverSequence());
    }

    private IEnumerator CoverSequence()
    {
        // 1. 천 씌우기 사운드
        if (audioSource != null && coverSound != null)
            audioSource.PlayOneShot(coverSound);

        // 2. 덮개 오브젝트 활성화
        if (coverObject != null)
            coverObject.SetActive(true);

        yield return new WaitForSeconds(0.5f);

        // 3. 조명 변화 (분위기 전환)
        DimLights();

        yield return new WaitForSeconds(0.3f);

        // 4. 열쇠 등장
        if (keyToReveal != null)
        {
            keyToReveal.SetActive(true);

            if (audioSource != null && keyRevealSound != null)
                audioSource.PlayOneShot(keyRevealSound);
        }

        Debug.Log("[PortraitCover] 퍼즐 클리어 → 열쇠 등장");
    }

    private void DimLights()
    {
        if (roomLights == null) return;

        foreach (var l in roomLights)
        {
            if (l != null)
                l.intensity *= afterLightRatio;
        }
    }
}
