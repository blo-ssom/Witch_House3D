using System.Collections;
using UnityEngine;

/// <summary>
/// 2층 귀신 이벤트.
/// 열쇠 + 노트 둘 다 획득하면:
///  1. 들어왔던 문 잠김
///  2. 거울에서 귀신 페이드인
///  3. 추격 시작
///
/// 사용법:
///  1. 2층 방 안의 빈 오브젝트에 부착
///  2. entranceDoor: 들어왔던 문 (DoorInteract)
///  3. ghostObject: 거울 위치에 배치된 귀신 오브젝트 (비활성화 상태)
///  4. ghostChase: 귀신의 GhostChase 컴포넌트
///  5. mirrorPosition: 거울 앞 스폰 위치
///  6. ghostRenderers: 귀신의 모든 Renderer (페이드인용)
/// </summary>
public class Floor2GhostEvent : MonoBehaviour
{
    [Header("조건 추적")]
    [Tooltip("2층 방 열쇠의 KeyType (Floor2)")]
    public KeyType requiredKey = KeyType.Floor2;

    [Tooltip("트리거 조건이 되는 노트의 ID")]
    public string requiredNoteID = "floor2_note";

    [Header("문")]
    public DoorInteract entranceDoor;

    [Header("귀신")]
    public GameObject ghostObject;
    public GhostChase ghostChase;
    public Transform mirrorPosition;

    [Header("페이드인 설정")]
    public float fadeInDuration = 2f;

    [Header("SFX")]
    public AudioSource audioSource;
    public AudioClip doorLockSound;
    public AudioClip ghostAppearSound;

    private bool hasKey = false;
    private bool hasNote = false;
    private bool eventTriggered = false;

    private PlayerInventory playerInventory;

    private void Start()
    {
        if (ghostObject != null)
            ghostObject.SetActive(false);

        playerInventory = FindObjectOfType<PlayerInventory>();
    }

    private void OnEnable()
    {
        NoteItem.OnNoteRead += OnNoteReadCallback;
    }

    private void OnDisable()
    {
        NoteItem.OnNoteRead -= OnNoteReadCallback;
    }

    private void Update()
    {
        if (eventTriggered) return;

        // 열쇠 획득 체크
        if (!hasKey && playerInventory != null && playerInventory.HasKey(requiredKey))
        {
            hasKey = true;
            CheckTrigger();
        }
    }

    private void OnNoteReadCallback(string noteID)
    {
        if (eventTriggered) return;
        if (noteID != requiredNoteID) return;

        hasNote = true;
        CheckTrigger();
    }

    private void CheckTrigger()
    {
        if (hasKey && hasNote && !eventTriggered)
        {
            eventTriggered = true;
            StartCoroutine(EventSequence());
        }
    }

    private IEnumerator EventSequence()
    {
        // 1. 문 잠금
        if (entranceDoor != null)
        {
            entranceDoor.isLocked = true;
            entranceDoor.requiredKey = KeyType.None;

            if (audioSource != null && doorLockSound != null)
                audioSource.PlayOneShot(doorLockSound);
        }

        yield return new WaitForSeconds(0.5f);

        // 2. 귀신 스폰 및 페이드인
        if (ghostObject != null)
        {
            if (mirrorPosition != null)
                ghostObject.transform.position = mirrorPosition.position;

            ghostObject.SetActive(true);

            if (audioSource != null && ghostAppearSound != null)
                audioSource.PlayOneShot(ghostAppearSound);

            // 페이드인
            Renderer[] renderers = ghostObject.GetComponentsInChildren<Renderer>();
            SetGhostAlpha(renderers, 0f);
            yield return StartCoroutine(FadeInGhost(renderers));
        }

        // 3. 추격 시작
        if (ghostChase != null)
            ghostChase.StartChase();

        Debug.Log("[Floor2GhostEvent] 2층 귀신 이벤트 발동!");
    }

    private void SetGhostAlpha(Renderer[] renderers, float alpha)
    {
        foreach (var renderer in renderers)
        {
            foreach (var mat in renderer.materials)
            {
                Color c = mat.color;
                c.a = alpha;
                mat.color = c;

                // 투명 렌더링 모드 설정
                mat.SetFloat("_Surface", 1); // Transparent
                mat.SetFloat("_Blend", 0);
                mat.SetOverrideTag("RenderType", "Transparent");
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.renderQueue = 3000;
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }
        }
    }

    private IEnumerator FadeInGhost(Renderer[] renderers)
    {
        float elapsed = 0f;

        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Clamp01(elapsed / fadeInDuration);

            foreach (var renderer in renderers)
            {
                foreach (var mat in renderer.materials)
                {
                    Color c = mat.color;
                    c.a = alpha;
                    mat.color = c;
                }
            }

            yield return null;
        }

        // 페이드인 완료 후 불투명으로 복원
        foreach (var renderer in renderers)
        {
            foreach (var mat in renderer.materials)
            {
                Color c = mat.color;
                c.a = 1f;
                mat.color = c;

                mat.SetFloat("_Surface", 0); // Opaque
                mat.SetOverrideTag("RenderType", "Opaque");
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                mat.SetInt("_ZWrite", 1);
                mat.renderQueue = -1;
                mat.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }
        }
    }
}
