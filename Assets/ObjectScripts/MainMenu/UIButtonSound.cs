using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 버튼에 붙이면 클릭/호버 효과음을 재생. 메인메뉴·일시정지 메뉴 공용.
///
/// - 클립은 Resources/UISfx/click, Resources/UISfx/hover 에서 로드(빌드 포함 보장).
/// - 재생용 AudioSource는 DontDestroyOnLoad 싱글톤 1개 공유.
/// - 일시정지(timeScale=0)에서도 오디오는 실시간이라 정상 재생(AudioListener.pause=false 전제).
///
/// 부착: Button이 있는 오브젝트에 AddComponent. PauseMenu는 런타임에 자동 부착.
/// </summary>
[RequireComponent(typeof(Button))]
public class UIButtonSound : MonoBehaviour, IPointerEnterHandler
{
    private static AudioClip clickClip;
    private static AudioClip hoverClip;
    private static AudioSource src;
    private static bool loaded;

    private void Awake()
    {
        EnsureLoaded();
        var btn = GetComponent<Button>();
        if (btn != null) btn.onClick.AddListener(PlayClick);
    }

    private static void EnsureLoaded()
    {
        if (loaded) return;
        loaded = true;

        clickClip = Resources.Load<AudioClip>("UISfx/click");
        hoverClip = Resources.Load<AudioClip>("UISfx/hover");

        var go = new GameObject("~UISfxSource");
        Object.DontDestroyOnLoad(go);
        src = go.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.spatialBlend = 0f; // 2D
    }

    public void OnPointerEnter(PointerEventData eventData) => Play(hoverClip);

    private void PlayClick() => Play(clickClip);

    private static void Play(AudioClip c)
    {
        if (c != null && src != null) src.PlayOneShot(c);
    }
}
