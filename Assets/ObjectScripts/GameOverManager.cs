using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 게임오버 UI 관리 + 재시작 버튼.
///
/// 사용법:
///  1. GameOverUI 패널 오브젝트에 부착
///  2. RestartButton의 OnClick() → GameOverManager.Restart() 연결
/// </summary>
public class GameOverManager : MonoBehaviour
{
    public static GameOverManager Instance;

    [Header("UI")]
    public GameObject gameOverPanel;

    private void Awake()
    {
        Instance = this;

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
    }

    /// <summary>
    /// GhostChase에서 호출
    /// </summary>
    public void ShowGameOver()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);

        // 플레이어 멈추기
        Time.timeScale = 0f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    /// <summary>
    /// 재시작 버튼 OnClick에 연결
    /// </summary>
    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
