using UnityEngine;

/// <summary>
/// Mirror Branch 거울 E키 상호작용 (helper, Interactable 상속).
/// 일기를 읽기 전에는 콜라이더를 꺼서 프롬프트가 뜨지 않게 하고,
/// 매니저가 SetReady(true)를 호출하면 상호작용 가능해진다.
///
/// 사용법:
///  - 거울 GameObject (Layer=Interact, Collider IsTrigger=OFF)에 부착
///  - manager 슬롯에 FakeRoomMirrorEvent 드래그
/// </summary>
[RequireComponent(typeof(Collider))]
public class MirrorBranchInteract : Interactable
{
    [Tooltip("거울 연출을 소유한 매니저")]
    public FakeRoomMirrorEvent manager;

    [Tooltip("준비됐을 때(일기 읽은 후) 표시할 프롬프트")]
    public string readyPrompt = "E : 거울을 들여다본다";

    private Collider col;
    private bool ready = false;

    private void Awake()
    {
        col = GetComponent<Collider>();
        // 시작은 잠김 — 콜라이더를 꺼서 레이캐스트가 안 맞고 프롬프트도 안 뜸
        if (col != null) col.enabled = false;
    }

    /// <summary>매니저가 호출. 콜라이더 토글로 거울 상호작용 ON/OFF.</summary>
    public void SetReady(bool value)
    {
        ready = value;
        if (col != null) col.enabled = value;
    }

    public override string GetInteractPrompt()
    {
        return ready ? readyPrompt : "";
    }

    public override void Interact(PlayerInventory playerInventory)
    {
        if (!ready || manager == null) return;
        manager.TriggerMirrorSequence();
    }
}
