using UnityEngine;

/// <summary>
/// 방2 퇴장 여운 (P.T.식).
/// Room2MirrorEvent 완료 후 활성화됨. 플레이어가 퇴장 트리거를 지나가는 순간
/// 뒤에서 의자 넘어지는 소리를 재생하고, 의자 하나를 실제로 넘어뜨림.
///
/// 사용법:
///  1. 방2 출입구 앞에 빈 GameObject + BoxCollider (isTrigger) 배치
///  2. 이 컴포넌트 부착
///  3. chairToTopple: 넘어질 의자 Transform
///  4. toppleEuler: 넘어질 회전값 (예: (90, 0, 0))
///  5. Room2MirrorEvent 인스펙터의 exitCue 슬롯에 이 오브젝트 연결
/// </summary>
[RequireComponent(typeof(Collider))]
public class Room2ExitCue : MonoBehaviour
{
    [Header("의자")]
    public Transform chairToTopple;
    public Vector3 toppleEuler = new Vector3(90f, 0f, 0f);

    [Header("SFX")]
    public AudioSource audioSource;
    public AudioClip chairFallSfx;

    [Header("감지")]
    public string playerTag = "Player";

    private bool armed = false;
    private bool played = false;

    public void SetArmed(bool value)
    {
        armed = value;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!armed || played) return;
        if (!other.CompareTag(playerTag)) return;

        played = true;

        if (audioSource != null && chairFallSfx != null)
            audioSource.PlayOneShot(chairFallSfx);

        if (chairToTopple != null)
            chairToTopple.Rotate(toppleEuler);
    }
}
