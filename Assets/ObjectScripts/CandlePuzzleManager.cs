using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 지하 촛불 순서 퍼즐 매니저.
///
/// 흐름:
///  촛불을 정해진 순서대로 E → (틀리면 전체 리셋) → 다 맞으면 열쇠 등장
///  → 열쇠 획득(KeyItem.onPickup → OnKeyCollected) → 촛불 꺼지는 SFX + 전체 소등
///  → UndergroundChaseEvent.TriggerChase()로 마지막 추격 시작.
///
/// 정답 순서 = candleOrder 리스트의 등록 순서(Element 0 = 1번째).
/// 코드 수정 없이 인스펙터에서 드래그로 순서만 바꾸면 정답이 바뀐다.
///
/// 사용법:
///  1. 빈 GameObject에 부착
///  2. candleOrder 에 촛불(CandleInteractable)을 "정답 순서대로" 등록
///  3. keyObject = 정답 시 등장할 열쇠(KeyItem). 시작 시 자동 비활성됨
///  4. 그 열쇠의 KeyItem.onPickup 이벤트에 이 컴포넌트의 OnKeyCollected() 연결
/// </summary>
public class CandlePuzzleManager : MonoBehaviour
{
    [Header("촛불 — '정답 순서대로' 등록 (Element 0 = 1번째)")]
    public List<CandleInteractable> candleOrder = new List<CandleInteractable>();

    [Header("시작 상태")]
    [Tooltip("체크 = 시작 시 모든 촛불 켜짐(끄는 순서 퍼즐). 해제 = 꺼진 상태로 시작(켜는 순서 퍼즐).")]
    public bool startLit = false;

    [Header("정답 시 등장할 열쇠")]
    [Tooltip("시작 시 자동 비활성. 퍼즐 완성 시 SetActive(true).")]
    public GameObject keyObject;
    public AudioClip solveSound;     // 열쇠 등장 SFX (선택)

    [Header("열쇠 획득 → 소등 + 추격")]
    public AudioClip candleOutSound; // 촛불 꺼지는 사운드 (한 번 재생)
    [Tooltip("소등 후 추격 시작까지 딜레이(초)")]
    public float chaseDelay = 0.4f;

    [Header("SFX 출력")]
    public AudioSource audioSource;

    private int currentStep = 0;
    private bool solved = false;
    private bool keyTaken = false;

    // 촛불이 도달해야 할 목표 상태. startLit이면 '꺼짐', 아니면 '켜짐'.
    private bool Target => !startLit;

    private void Start()
    {
        foreach (var c in candleOrder)
        {
            if (c == null) continue;
            c.Bind(this);
            c.SetLit(startLit, playSound: false);
        }
        if (keyObject != null) keyObject.SetActive(false);
    }

    /// <summary>CandleInteractable.Interact()에서 호출.</summary>
    public void OnCandleInteracted(CandleInteractable candle)
    {
        if (solved) return;

        // 이미 목표 상태인 촛불 재상호작용 → 무시(관용)
        if (candle.IsLit == Target) return;

        int idx = candleOrder.IndexOf(candle);
        if (idx < 0) return; // 더미/미등록 촛불 — 반응 없음

        if (idx == currentStep)
        {
            candle.SetLit(Target);          // 정답 진행
            currentStep++;
            if (currentStep >= candleOrder.Count)
                Solve();
        }
        else
        {
            ResetCandles();                 // 오답 → 전체 리셋
        }
    }

    private void ResetCandles()
    {
        currentStep = 0;
        foreach (var c in candleOrder)
            if (c != null) c.SetLit(startLit, playSound: false);
        Debug.Log("[CandlePuzzle] 순서 틀림 → 리셋");
    }

    private void Solve()
    {
        solved = true;
        if (audioSource != null && solveSound != null)
            audioSource.PlayOneShot(solveSound);
        if (keyObject != null) keyObject.SetActive(true);
        Debug.Log("[CandlePuzzle] 정답! 열쇠 등장.");
    }

    /// <summary>퍼즐 열쇠의 KeyItem.onPickup 이벤트에 연결. 열쇠 획득 시 호출.</summary>
    public void OnKeyCollected()
    {
        if (keyTaken) return;
        keyTaken = true;
        StartCoroutine(ExtinguishAndChase());
    }

    private IEnumerator ExtinguishAndChase()
    {
        if (audioSource != null && candleOutSound != null)
            audioSource.PlayOneShot(candleOutSound);

        foreach (var c in candleOrder)
        {
            if (c == null) continue;
            c.SetLit(false, playSound: false);
            c.Lock();
        }

        Debug.Log("[CandlePuzzle] 열쇠 획득 → 전체 소등 → 추격");

        yield return new WaitForSeconds(chaseDelay);

        if (UndergroundChaseEvent.Instance != null)
            UndergroundChaseEvent.Instance.TriggerChase();
        else
            Debug.LogWarning("[CandlePuzzle] UndergroundChaseEvent.Instance 없음 — 추격 시작 실패");
    }
}
