using UnityEngine;

/// <summary>
/// 지하 제단 상호작용 오브젝트.
/// E키로 조사하면 UndergroundChaseEvent.OnAltarInvestigated() 호출 → 추격 시작.
///
/// 사용법:
///  1. 제단 오브젝트에 부착
///  2. Collider 필요 (Raycast 감지용)
///  3. promptMessage: 상호작용 프롬프트 텍스트
/// </summary>
public class AltarInteractable : Interactable
{
    [Header("프롬프트")]
    public string promptMessage = "조사하기";

    private bool investigated = false;

    public override void Interact(PlayerInventory inventory)
    {
        if (investigated) return;
        investigated = true;

        if (UndergroundChaseEvent.Instance != null)
            UndergroundChaseEvent.Instance.OnAltarInvestigated();

        Debug.Log("[AltarInteractable] 제단 조사 완료.");
    }

    public override string GetInteractPrompt()
    {
        if (investigated) return "";
        return promptMessage;
    }
}
