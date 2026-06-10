using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 방3 석상 시선 사슬 퍼즐 매니저.
///
/// 흐름:
///  입장 시 석상 전원이 initialGazeTarget(중앙 사자상)을 응시.
///  시작 단서 = 사자상이 첫 석상을 보고 있음 (에디터에서 정적으로 회전해 둠).
///  정답 순서대로 E:
///   - 정답: 그 석상이 끼익 돌아 "다음 석상"을 응시 (진행 피드백 + 다음 단서 공개)
///   - 오답: 전원이 다시 사자상을 응시하러 복귀 + wrongSound
///  전부 맞으면 4개가 일제히 finalGazeTarget(열쇠 테이블)을 돌아봄 → 열쇠 등장.
///  열쇠 획득 후 추적은 기존 KeyItem(KeyType.Room2) → Room3StatueManager 그대로.
///
/// 정답 순서 = statueOrder 리스트의 등록 순서(Element 0 = 1번째).
/// 코드 수정 없이 인스펙터에서 드래그로 순서만 바꾸면 정답이 바뀐다.
///
/// 사용법:
///  1. 빈 GameObject에 부착
///  2. statueOrder 에 석상(StatueGazeInteract)을 "정답 순서대로" 등록
///  3. finalGazeTarget = 열쇠가 등장할 지점 Transform (빈 테이블 위)
///  4. keyObject = 정답 시 등장할 열쇠(KeyItem). 시작 시 자동 비활성됨
/// </summary>
public class StatueGazePuzzleManager : MonoBehaviour
{
    [Header("석상 — '정답 순서대로' 등록 (Element 0 = 1번째)")]
    public List<StatueGazeInteract> statueOrder = new List<StatueGazeInteract>();

    [Header("시작 포즈 — 전원이 응시하는 곳 (중앙 사자상)")]
    [Tooltip("입장 시 모든 석상이 이곳을 응시. 오답 시에도 이곳으로 복귀.")]
    public Transform initialGazeTarget;

    [Header("시선 사슬의 끝 = 열쇠 등장 지점")]
    [Tooltip("마지막 석상이 정답 시 응시하는 곳. 완성 시 전원이 이곳을 돌아보고 열쇠가 등장.")]
    public Transform finalGazeTarget;

    [Header("완성 연출 — 사자상 붕괴 (선택)")]
    [Tooltip("연결하면 완성 시 사자상이 떨며 쓰러지고 그 자리에 열쇠 등장 (열쇠는 LionBreakEvent.keyObject에). 비우면 아래 keyObject를 즉시 활성.")]
    public LionBreakEvent lionBreak;

    [Header("정답 시 등장할 열쇠 (lionBreak 미사용 시)")]
    [Tooltip("시작 시 자동 비활성. 퍼즐 완성 시 SetActive(true). lionBreak를 쓰면 비워둘 것.")]
    public GameObject keyObject;
    public AudioClip solveSound;     // 완성 SFX (선택)

    [Header("오답 피드백")]
    [Tooltip("순서 틀렸을 때 재생 — 무겁게 갈리는/쿵 소리 권장. 비워두면 무음 (선택)")]
    public AudioClip wrongSound;

    [Header("SFX 출력")]
    public AudioSource audioSource;

    private int currentStep = 0;
    private bool solved = false;

    private void Start()
    {
        foreach (var s in statueOrder)
            if (s != null) s.Bind(this);

        if (keyObject != null) keyObject.SetActive(false);

        // 시작 포즈: 전원이 사자상을 응시 (즉시·무음)
        PoseInitial(instant: true);
    }

    /// <summary>전원이 initialGazeTarget(사자상)을 응시.</summary>
    private void PoseInitial(bool instant)
    {
        if (initialGazeTarget == null) return;

        foreach (var s in statueOrder)
            if (s != null) s.GazeAt(initialGazeTarget.position, instant);
    }

    /// <summary>idx번째 석상이 정답 시 응시할 곳 — 다음 석상, 마지막이면 finalGazeTarget.</summary>
    private Vector3 ChainTargetPos(int idx)
    {
        bool isLast = idx >= statueOrder.Count - 1;
        if (!isLast && statueOrder[idx + 1] != null)
        {
            var next = statueOrder[idx + 1];
            return (next.head != null ? next.head : next.transform).position;
        }
        return finalGazeTarget != null ? finalGazeTarget.position : transform.position;
    }

    /// <summary>StatueGazeInteract.Interact()에서 호출.</summary>
    public void OnStatueInteracted(StatueGazeInteract statue)
    {
        if (solved) return;

        int idx = statueOrder.IndexOf(statue);
        if (idx < 0) return;          // 미등록 석상 — 반응 없음

        if (idx < currentStep) return; // 이미 맞춘 석상 재상호작용 → 무시(관용)

        if (idx == currentStep)
        {
            // 정답: 끼익 돌아 다음 석상(마지막이면 열쇠 지점)을 응시 → 다음 단서 공개
            statue.GazeAt(ChainTargetPos(idx));
            currentStep++;
            Debug.Log($"[StatueGazePuzzle] 정답 진행 {currentStep}/{statueOrder.Count}");

            if (currentStep >= statueOrder.Count)
                Solve();
        }
        else
        {
            ResetChain();              // 오답 → 전원 사자상으로 복귀
        }
    }

    private void ResetChain()
    {
        currentStep = 0;
        PoseInitial(instant: false);   // 갈리는 소리와 함께 일제히 사자상으로 복귀

        if (audioSource != null && wrongSound != null)
            audioSource.PlayOneShot(wrongSound);

        Debug.Log("[StatueGazePuzzle] 순서 틀림 → 리셋");
    }

    private void Solve()
    {
        solved = true;

        // 전원이 일제히 열쇠 지점을 돌아봄 + 상호작용 잠금
        foreach (var s in statueOrder)
        {
            if (s == null) continue;
            if (finalGazeTarget != null)
                s.GazeAt(finalGazeTarget.position);
            s.Lock();
        }

        if (audioSource != null && solveSound != null)
            audioSource.PlayOneShot(solveSound);

        if (lionBreak != null)
            lionBreak.TriggerBreak();           // 사자상 붕괴 → 열쇠는 LionBreakEvent가 등장시킴
        else if (keyObject != null)
            keyObject.SetActive(true);

        Debug.Log("[StatueGazePuzzle] 정답! 완성 연출 시작.");
    }
}
