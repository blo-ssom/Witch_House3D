using UnityEngine;

/// <summary>
/// 지하 촛불 순서 퍼즐의 개별 촛불.
/// 순서 판정은 CandlePuzzleManager가 하고, 이 컴포넌트는 자기 상태(켜짐/꺼짐)와
/// 불꽃 시각·사운드만 담당한다. E키 상호작용 시 매니저에게 자신을 알린다.
///
/// 사용법:
///  1. 각 촛불 오브젝트에 부착 (Collider 필요, Layer = Interact)
///  2. flameVisual: 불꽃 파티클/메시 오브젝트, flameLight: Point Light (둘 다 선택)
///  3. CandlePuzzleManager의 candleOrder 리스트에 "정답 순서대로" 등록
///     (리스트에 등록되지 않은 촛불은 더미 — 상호작용해도 반응 없음)
/// </summary>
public class CandleInteractable : Interactable
{
    [Header("불꽃 시각")]
    [Tooltip("켜짐/꺼짐 시 On/Off 되는 불꽃 오브젝트 (파티클·메시)")]
    public GameObject flameVisual;
    [Tooltip("켜짐/꺼짐 시 On/Off 되는 Light")]
    public Light flameLight;

    [Header("SFX (선택 — 매니저가 일괄 처리하면 비워둬도 됨)")]
    public AudioSource audioSource;
    public AudioClip igniteSound;     // 켤 때
    public AudioClip extinguishSound; // 끌 때

    [Header("프롬프트")]
    public string promptMessage = "촛불";

    public bool IsLit { get; private set; }

    private CandlePuzzleManager manager;
    private bool locked = false;  // 퍼즐 완료 후 잠금

    public void Bind(CandlePuzzleManager m) => manager = m;

    public override void Interact(PlayerInventory inventory)
    {
        if (locked || manager == null) return;
        manager.OnCandleInteracted(this);
    }

    public override string GetInteractPrompt()
    {
        if (locked) return "";
        return promptMessage;
    }

    /// <summary>매니저가 호출. 시각/사운드만 갱신.</summary>
    public void SetLit(bool lit, bool playSound = true)
    {
        IsLit = lit;
        if (flameVisual != null) flameVisual.SetActive(lit);
        if (flameLight != null)  flameLight.enabled = lit;

        if (playSound && audioSource != null)
        {
            var clip = lit ? igniteSound : extinguishSound;
            if (clip != null) audioSource.PlayOneShot(clip);
        }
    }

    /// <summary>퍼즐 종료 후 더 이상 상호작용 못 하게 잠금.</summary>
    public void Lock() => locked = true;
}
