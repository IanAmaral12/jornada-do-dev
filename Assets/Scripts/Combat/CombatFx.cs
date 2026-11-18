using System.Collections;
using UnityEngine;

public class CombatFx : MonoBehaviour
{
    [SerializeField] Material flashMaterial;
    [SerializeField] AudioSource audioSource;
    [SerializeField] AudioClip sfxSwing;
    [SerializeField] AudioClip sfxHit;
    [SerializeField] AudioClip sfxFinisher;
    [SerializeField] AudioClip sfxHurt;
    [SerializeField] Color corDano = new(1f, 0.85f, 0.2f);
    [SerializeField] Color corDanoJogador = new(1f, 0.3f, 0.3f);

    ParticleSystem faiscas;
    TrailRenderer rastro;
    Transform pivoCorte;

    void Awake()
    {
        var spriteMat = new Material(Shader.Find("Sprites/Default"));
        faiscas = CriarFaiscas(spriteMat);
        CriarCorte(spriteMat);
    }

    void OnDisable() => Time.timeScale = 1f;

    void OnDestroy() => Time.timeScale = 1f;

    public IEnumerator PlayerStrike(Transform player, EnemyController enemy, int damage, bool finisher)
    {
        var anim = player.GetComponent<PlayerCombatAnimator>();
        Vector3 origem = player.position;
        float lado = Mathf.Sign(enemy.transform.position.x - origem.x);
        Vector3 alvo = enemy.transform.position - new Vector3(lado * 0.9f, 0f, 0f);

        if (anim) yield return anim.Squash(0.1f);
        Tocar(sfxSwing);
        if (anim) anim.Attack();
        yield return Mover(player, origem, alvo, 0.12f);

        yield return Corte(enemy.transform.position, lado);

        Tocar(finisher ? sfxFinisher : sfxHit);
        StartCoroutine(Flash(enemy.GetComponent<SpriteRenderer>(), 2));
        faiscas.transform.position = enemy.transform.position;
        faiscas.Emit(finisher ? 24 : 12);
        DamageNumber.Spawn(enemy.transform.position + Vector3.up * 0.8f, "-" + damage, corDano);
        CameraController.Instancia?.Shake(finisher ? 0.3f : 0.15f, finisher ? 0.3f : 0.15f);
        yield return HitStop(finisher ? 0.15f : 0.07f);
        StartCoroutine(Mover(enemy.transform, enemy.transform.position, enemy.transform.position + new Vector3(lado * 0.3f, 0f, 0f), 0.08f));

        if (finisher) yield return CameraLenta(0.3f, 0.4f);

        yield return Mover(player, player.position, origem, 0.15f);
    }

    public IEnumerator EnemyStrike(EnemyController enemy, Transform player, int damage)
    {
        Vector3 origem = enemy.transform.position;
        float lado = Mathf.Sign(player.position.x - origem.x);
        Vector3 alvo = player.position - new Vector3(lado * 0.8f, 0f, 0f);

        yield return Mover(enemy.transform, origem, origem - new Vector3(lado * 0.2f, 0f, 0f), 0.12f);
        yield return Mover(enemy.transform, enemy.transform.position, alvo, 0.1f);

        Tocar(sfxHurt);
        var anim = player.GetComponent<PlayerCombatAnimator>();
        if (anim) anim.Hurt();
        DamageNumber.Spawn(player.position + Vector3.up * 0.8f, "-" + damage, corDanoJogador);
        CameraController.Instancia?.Shake(0.2f, 0.2f);
        HudController.Instancia?.Tremer();
        yield return HitStop(0.06f);
        StartCoroutine(Mover(player, player.position, player.position - new Vector3(lado * 0.3f, 0f, 0f), 0.1f));
        yield return PiscarVermelho(player.GetComponent<SpriteRenderer>(), 3);

        yield return Mover(enemy.transform, enemy.transform.position, origem, 0.15f);
    }

    IEnumerator Corte(Vector3 centro, float lado)
    {
        pivoCorte.position = centro;
        pivoCorte.localScale = new Vector3(lado, 1f, 1f);
        pivoCorte.rotation = Quaternion.Euler(0f, 0f, 80f);
        rastro.Clear();
        rastro.emitting = true;
        const float duracao = 0.08f;
        for (float t = 0f; t < duracao; t += Time.unscaledDeltaTime)
        {
            pivoCorte.rotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(80f, -80f, EaseOutCubic(t / duracao)));
            yield return null;
        }
        rastro.emitting = false;
    }

    static IEnumerator Mover(Transform alvo, Vector3 de, Vector3 para, float duracao)
    {
        for (float t = 0f; t < duracao; t += Time.unscaledDeltaTime)
        {
            alvo.position = Vector3.LerpUnclamped(de, para, EaseOutCubic(t / duracao));
            yield return null;
        }
        alvo.position = para;
    }

    static IEnumerator HitStop(float duracao)
    {
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(duracao);
        Time.timeScale = 1f;
    }

    static IEnumerator CameraLenta(float escala, float duracao)
    {
        Time.timeScale = escala;
        yield return new WaitForSecondsRealtime(duracao);
        Time.timeScale = 1f;
    }

    IEnumerator Flash(SpriteRenderer sr, int quadros)
    {
        if (sr == null || flashMaterial == null) yield break;
        var original = sr.sharedMaterial;
        sr.sharedMaterial = flashMaterial;
        for (int i = 0; i < quadros; i++) yield return null;
        yield return new WaitForSecondsRealtime(0.05f);
        if (sr) sr.sharedMaterial = original;
    }

    static IEnumerator PiscarVermelho(SpriteRenderer sr, int vezes)
    {
        if (sr == null) yield break;
        for (int i = 0; i < vezes; i++)
        {
            sr.color = new Color(1f, 0.3f, 0.3f);
            yield return new WaitForSecondsRealtime(0.06f);
            sr.color = Color.white;
            yield return new WaitForSecondsRealtime(0.06f);
        }
    }

    void Tocar(AudioClip clip)
    {
        if (clip && audioSource) audioSource.PlayOneShot(clip);
    }

    static float EaseOutCubic(float x) => 1f - Mathf.Pow(1f - Mathf.Clamp01(x), 3f);

    ParticleSystem CriarFaiscas(Material mat)
    {
        var go = new GameObject("Faiscas");
        go.transform.SetParent(transform);
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.playOnAwake = false;
        main.loop = false;
        main.startLifetime = 0.35f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 7f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.16f);
        main.startColor = new ParticleSystem.MinMaxGradient(Color.white, corDano);
        main.gravityModifier = 1.5f;
        main.useUnscaledTime = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        var emissao = ps.emission;
        emissao.enabled = false;
        var forma = ps.shape;
        forma.shapeType = ParticleSystemShapeType.Circle;
        forma.radius = 0.1f;
        var r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = mat;
        r.sortingOrder = 50;
        return ps;
    }

    void CriarCorte(Material mat)
    {
        pivoCorte = new GameObject("PivoCorte").transform;
        pivoCorte.SetParent(transform);
        var ponta = new GameObject("PontaCorte").transform;
        ponta.SetParent(pivoCorte);
        ponta.localPosition = new Vector3(0.9f, 0f, 0f);
        rastro = ponta.gameObject.AddComponent<TrailRenderer>();
        rastro.sharedMaterial = mat;
        rastro.time = 0.15f;
        rastro.minVertexDistance = 0.02f;
        rastro.widthCurve = new AnimationCurve(new Keyframe(0f, 0.35f), new Keyframe(1f, 0f));
        rastro.colorGradient = new Gradient
        {
            colorKeys = new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(0.5f, 0.9f, 1f), 1f) },
            alphaKeys = new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
        };
        rastro.emitting = false;
        rastro.sortingOrder = 60;
    }
}
