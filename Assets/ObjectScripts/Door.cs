using UnityEngine;

public class Door : MonoBehaviour
{
    public bool isOpen = false;
    public float openAngle = 90f; // 열릴 각도
    public float smooth = 3f;     // 열리는 속도

    private Quaternion targetRotation;
    private Quaternion defaultRotation;

    void Start()
    {
        // 처음 시작할 때의 각도를 기억해둡니다.
        defaultRotation = transform.localRotation;
    }

    void Update()
    {
        // 상태에 따라 목표 각도 설정
        if (isOpen)
            targetRotation = defaultRotation * Quaternion.Euler(0, openAngle, 0);
        else
            targetRotation = defaultRotation;

        // 부드럽게 회전시키기
        transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation, Time.deltaTime * smooth);
    }

    // 외부(Player)에서 호출할 함수
    public void Interaction()
    {
        isOpen = !isOpen; // 상태 반전 (열렸으면 닫고, 닫혔으면 열기)
        Debug.Log("문 상태: " + (isOpen ? "열림" : "닫힘"));
    }
}