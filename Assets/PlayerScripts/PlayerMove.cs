using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMove : MonoBehaviour
{
    [Header("Move Settings")]
    public float walkSpeed = 3f;
    public float runSpeed = 3.2f;
    public float gravity = -9.81f;

    [Header("Stamina")]
    [Tooltip("끄면 기존처럼 무제한 달리기")]
    public bool  useStamina = true;
    [Tooltip("풀 스태미나로 달릴 수 있는 시간(초)")]
    public float maxStamina = 6f;
    [Tooltip("달리기 멈춘 뒤 회복 시작까지 딜레이(초)")]
    public float regenDelay = 1f;
    [Tooltip("초당 회복량 (2 = 3초에 풀 회복)")]
    public float regenSpeed = 2f;
    [Tooltip("완전 방전 후 다시 달리려면 이만큼 회복돼야 함 (Shift 연타 방지)")]
    public float exhaustRecover = 1.5f;
    [Tooltip("스태미나가 이 비율 이하면 달리기 속도가 점점 처짐 (0.3 = 30%)")]
    public float tiredZone = 0.3f;
    [Tooltip("속도 전환 빠르기(m/s²) — 낮을수록 달리기 시작/멈춤이 부드럽게 이어짐")]
    public float speedChangeRate = 4f;

    [Header("Stamina Feedback (선택 — 비워두면 무시)")]
    [Tooltip("스태미나 낮을수록 커지는 숨소리. Loop 켜둔 AudioSource 연결")]
    public AudioSource breathingLoop;

    // 외부(UI 등) 조회용
    public float StaminaNormalized => useStamina ? stamina / maxStamina : 1f;
    public bool  IsExhausted       => exhausted;

    private CharacterController controller;
    private Vector3 velocity;
    private float stamina;
    private float lastSprintTime = -999f;
    private bool  exhausted = false;
    private float currentSpeed;   // 스무딩된 현재 이동 속도

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        stamina = maxStamina;
        currentSpeed = walkSpeed;
    }

    private void Update()
    {
        Move();
        ApplyGravity();
    }

    private void Move()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 move = transform.right * x + transform.forward * z;

        bool isMoving  = move.sqrMagnitude > 0.01f;
        bool wantsRun  = Input.GetKey(KeyCode.LeftShift);
        bool canSprint = !useStamina || (!exhausted && stamina > 0f);
        bool running   = wantsRun && isMoving && canSprint;

        // 목표 속도 — 지친 구간(tiredZone 이하)에선 달리기가 점점 걷기 쪽으로 처짐
        float targetSpeed = walkSpeed;
        if (running)
        {
            float tiredT = useStamina
                ? Mathf.InverseLerp(0f, tiredZone, StaminaNormalized)
                : 1f;
            float minSprint = Mathf.Lerp(walkSpeed, runSpeed, 0.35f); // 방전 직전 최저 달리기 속도
            targetSpeed = Mathf.Lerp(minSprint, runSpeed, tiredT);
        }

        // 속도 스무딩 — 한 번에 꺼지고/켜지는 느낌 제거
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, speedChangeRate * Time.deltaTime);

        controller.Move(move * currentSpeed * Time.deltaTime);

        if (useStamina)
            UpdateStamina(running);
    }

    private void UpdateStamina(bool running)
    {
        if (running)
        {
            stamina -= Time.deltaTime;
            lastSprintTime = Time.time;

            if (stamina <= 0f)
            {
                stamina = 0f;
                exhausted = true;   // 완전 방전 → exhaustRecover까지 달리기 잠금
            }
        }
        else if (Time.time - lastSprintTime >= regenDelay && stamina < maxStamina)
        {
            stamina = Mathf.Min(stamina + regenSpeed * Time.deltaTime, maxStamina);

            if (exhausted && stamina >= exhaustRecover)
                exhausted = false;
        }

        // 숨소리 피드백 — 스태미나 60% 이하부터 점점 커짐 (UI 없이도 잔량 체감)
        if (breathingLoop != null)
        {
            float strain = Mathf.InverseLerp(0.6f, 0f, StaminaNormalized);
            breathingLoop.volume = strain;
            breathingLoop.pitch  = Mathf.Lerp(1f, 1.15f, strain);

            if (!breathingLoop.isPlaying)
                breathingLoop.Play();
        }
    }

    private void ApplyGravity()
    {
        if (controller.isGrounded && velocity.y < 0f)
        {
            velocity.y = -2f;
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}