using UnityEngine;

/// <summary>
/// 캐릭터 애니메이션 테스트용 드라이버.
/// WASD로 움직이면 Walk, Shift 누르면 Run 으로 Animator의 Speed 파라미터를 채워준다.
/// (이동 자체는 기존 PlayerMove가 담당. 이 스크립트는 애니 전환 확인용)
/// </summary>
[RequireComponent(typeof(Animator))]
public class PlayerAnimDriver : MonoBehaviour
{
    [Header("Speed Values (Animator 트랜지션 임계값 3 기준)")]
    public float walkValue = 2f;
    public float runValue = 5f;

    [Tooltip("Speed 파라미터가 부드럽게 변하는 시간(초)")]
    public float damp = 0.1f;

    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");
        bool moving = new Vector2(x, z).sqrMagnitude > 0.01f;

        float target = 0f;
        if (moving)
        {
            target = Input.GetKey(KeyCode.LeftShift) ? runValue : walkValue;
        }

        animator.SetFloat("Speed", target, damp, Time.deltaTime);
    }
}
