using UnityEngine;

/// <summary>
/// 시계 똑딱 소리를 일정 간격으로 재생.
/// AudioSource의 Loop는 틈 없이 반복돼서 너무 빠르게 들리므로,
/// Loop를 끄고 interval(초)마다 한 번씩 재생해 간격을 준다.
///
/// 사용법:
///  - 시계 GameObject에 AudioSource + 이 컴포넌트 부착
///  - AudioSource: Spatial Blend = 1(3D), Loop = OFF, Play On Awake = OFF
///    (3D 설정과 Min/Max Distance는 그대로 두면 거리별 볼륨은 유지됨)
///  - tickClip을 비워두면 AudioSource에 설정된 clip을 사용
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class TickingClock : MonoBehaviour
{
    [Tooltip("똑 소리 하나 사이의 간격(초). 1 = 1초에 한 번.")]
    public float interval = 1f;

    [Tooltip("재생할 똑딱 클립. 비워두면 AudioSource.clip 사용.")]
    public AudioClip tickClip;

    [Range(0f, 1f)]
    public float volume = 1f;

    [Tooltip("시작 시 자동 재생 시작")]
    public bool playOnStart = true;

    private AudioSource source;

    private void Awake()
    {
        source = GetComponent<AudioSource>();
        source.loop = false;          // 스크립트가 간격을 제어하므로 루프 끔
        source.playOnAwake = false;
        if (tickClip != null) source.clip = tickClip;
    }

    private void Start()
    {
        if (playOnStart) StartTicking();
    }

    /// <summary>똑딱 재생 시작.</summary>
    public void StartTicking()
    {
        CancelInvoke(nameof(Tick));
        InvokeRepeating(nameof(Tick), 0f, Mathf.Max(0.05f, interval));
    }

    /// <summary>똑딱 정지.</summary>
    public void StopTicking()
    {
        CancelInvoke(nameof(Tick));
    }

    private void Tick()
    {
        if (source.clip == null) return;
        // PlayOneShot이 아니라 Play를 쓰면 3D 공간화/거리 감쇠가 그대로 적용됨
        source.volume = volume;
        source.Play();
    }
}
