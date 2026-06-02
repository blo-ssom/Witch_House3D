using UnityEngine;

/// <summary>
/// 방3 4개 석상의 시선을 일괄 활성화.
/// 시작 시: 4개 석상은 isActive = false 상태 (테이블 향해 정적으로 포즈잡음)
/// 열쇠 획득 시: ActivateAll() 호출 → 4개 모두 HeadLookAt 활성화 → Weeping Angels식 추적 시작
///
/// 처음부터 플레이어를 추적하는 1개 석상은 이 매니저에 등록하지 않고,
/// 그 석상의 HeadLookAt.isActive = true로 직접 둠.
/// </summary>
public class Room3StatueManager : MonoBehaviour
{
    [Header("열쇠 획득 시 활성화할 석상 4마리")]
    [Tooltip("각 석상 머리에 부착된 HeadLookAt 컴포넌트를 등록. 인스펙터에서 isActive는 false로 둘 것.")]
    public HeadLookAt[] statues;

    [Header("열쇠 획득 시 머리가 떨어질 석상 (선택)")]
    [Tooltip("열쇠 줍는 순간 머리가 떨어질 석상. 비워두면 아무 일 없음.")]
    public StatueHeadFall fallingHead;

    private bool activated = false;

    /// <summary>
    /// 열쇠 획득 시점에 KeyItem이 호출. 4개 석상의 추적을 활성화.
    /// </summary>
    public void ActivateAll()
    {
        if (activated) return;
        activated = true;

        if (statues != null)
        {
            foreach (var s in statues)
            {
                if (s == null) continue;
                s.Activate();
            }
        }

        // 지정된 석상의 머리가 떨어짐
        if (fallingHead != null)
            fallingHead.Drop();

        Debug.Log("[Room3StatueManager] 4개 석상이 추적을 시작합니다");
    }
}
