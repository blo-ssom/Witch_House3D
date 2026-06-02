using System.Collections;
using UnityEngine;

/// <summary>
/// 목 절단면 피 연출 — 머리가 떨어진 뒤 Play() 호출 시:
///  1. (선택) startDelay 만큼 대기 (머리가 떨어진 직후 흐르도록)
///  2. 목에서 피 파티클 재생 (뚝뚝 떨어짐 / 흘러내림)
///  3. (선택) 절단면 핏자국 데칼이 서서히 번짐 (알파 페이드인)
///  4. (선택) 질척이는 SFX 재생
///
/// 셋업 (Unity 에디터):
///  1. 석상 '목(절단면)' 위치에 빈 GameObject 생성 → 이 컴포넌트 부착
///  2. bloodParticles: 그 자리에 둔 ParticleSystem(들) 등록
///     · 무료: 빨강~검붉은색 작은 파티클, Gravity Modifier 양수(아래로 떨어짐),
///       Start Speed 낮게, Emission 적당히. 텍스처 없어도 기본 파티클로 충분.
///  3. (선택) bloodDecal: 절단면에 붙인 핏자국 Quad/데칼 Renderer (Transparent 머티리얼)
///  4. (선택) audioSource + bloodSfx
///
/// StatueHeadFall 의 Neck Blood 슬롯에 연결하면 머리 낙하 시 자동 발동.
/// </summary>
public class NeckBloodEffect : MonoBehaviour
{
    [Header("발동 지연")]
    [Tooltip("머리가 떨어진 뒤 피가 흐르기 시작하기까지 지연(초)")]
    public float startDelay = 0.3f;

    [Header("피 파티클")]
    [Tooltip("목에서 흐르는/뚝뚝 떨어지는 ParticleSystem(들). 시작 시 멈춰 있어야 함.")]
    public ParticleSystem[] bloodParticles;

    [Header("절단면 핏자국 데칼 (선택)")]
    [Tooltip("목 절단면에 번질 핏자국 Renderer. Transparent 머티리얼이어야 페이드됨.")]
    public Renderer bloodDecal;
    public float decalFadeDuration = 1.5f;

    [Header("SFX (선택)")]
    public AudioSource audioSource;
    [Tooltip("피가 흐를 때 질척이는 소리")]
    public AudioClip bloodSfx;
    [Range(0f, 1f)] public float sfxVolume = 1f;

    private bool played = false;

    private void Start()
    {
        // 시작 시 파티클/데칼 숨김
        if (bloodParticles != null)
            foreach (var ps in bloodParticles)
                if (ps != null) ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        if (bloodDecal != null)
            bloodDecal.enabled = false;
    }

    /// <summary>
    /// 외부(StatueHeadFall)에서 호출 — 목 피 연출 시작. 1회성.
    /// </summary>
    public void Play()
    {
        if (played) return;
        played = true;
        StartCoroutine(PlayRoutine());
    }

    private IEnumerator PlayRoutine()
    {
        if (startDelay > 0f)
            yield return new WaitForSeconds(startDelay);

        if (bloodParticles != null)
            foreach (var ps in bloodParticles)
                if (ps != null) ps.Play();

        if (audioSource != null && bloodSfx != null)
            audioSource.PlayOneShot(bloodSfx, sfxVolume);

        if (bloodDecal != null)
        {
            bloodDecal.enabled = true;
            if (decalFadeDuration > 0f)
                yield return StartCoroutine(FadeInRenderer(bloodDecal, decalFadeDuration));
        }
    }

    /// <summary>
    /// 핏자국 Renderer 알파를 0→원래값으로 페이드 (번지는 느낌).
    /// URP는 _BaseColor, 구형은 _Color. Transparent 머티리얼이어야 보임.
    /// </summary>
    private IEnumerator FadeInRenderer(Renderer rend, float duration)
    {
        Material mat = rend.material;   // 인스턴스 생성 (1회성 연출이라 허용)
        string prop = mat.HasProperty("_BaseColor") ? "_BaseColor"
                    : (mat.HasProperty("_Color") ? "_Color" : null);
        if (prop == null) yield break;

        Color c = mat.GetColor(prop);
        float endA = c.a <= 0f ? 1f : c.a;

        c.a = 0f;
        mat.SetColor(prop, c);

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            c.a = Mathf.Lerp(0f, endA, t / duration);
            mat.SetColor(prop, c);
            yield return null;
        }

        c.a = endA;
        mat.SetColor(prop, c);
    }
}
