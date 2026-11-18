using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HudController : MonoBehaviour
{
    [SerializeField] RectTransform painel;
    [SerializeField] Transform coracoes;
    [SerializeField] Sprite coracaoCheio;
    [SerializeField] Sprite coracaoVazio;
    [SerializeField] Text titulo;
    [SerializeField] GameObject barraInimigo;
    [SerializeField] Text nomeInimigo;
    [SerializeField] Image preenchimentoInimigo;

    public static HudController Instancia { get; private set; }

    readonly List<Image> imagens = new();
    Health inimigo;

    void Awake()
    {
        Instancia = this;
        barraInimigo.SetActive(false);
    }

    public void Bind(Health jogador, string tituloFase)
    {
        titulo.text = tituloFase;
        jogador.Changed += AtualizarCoracoes;
        AtualizarCoracoes(jogador.Current, jogador.Max);
    }

    void AtualizarCoracoes(int atual, int max)
    {
        while (imagens.Count < max)
        {
            var go = new GameObject("Coracao", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(coracoes, false);
            var img = go.GetComponent<Image>();
            img.preserveAspect = true;
            imagens.Add(img);
        }
        for (int i = 0; i < imagens.Count; i++)
            imagens[i].sprite = i < atual ? coracaoCheio : coracaoVazio;
    }

    public void MostrarInimigo(EnemyController e)
    {
        if (inimigo != null) inimigo.Changed -= AtualizarInimigo;
        inimigo = e.Health;
        inimigo.Changed += AtualizarInimigo;
        nomeInimigo.text = e.displayName;
        AtualizarInimigo(inimigo.Current, inimigo.Max);
        barraInimigo.SetActive(true);
    }

    public void EsconderInimigo()
    {
        if (inimigo != null) inimigo.Changed -= AtualizarInimigo;
        inimigo = null;
        barraInimigo.SetActive(false);
    }

    void AtualizarInimigo(int atual, int max) => preenchimentoInimigo.fillAmount = (float)atual / max;

    public void Tremer() => StartCoroutine(TremerRotina());

    IEnumerator TremerRotina()
    {
        Vector2 base_ = painel.anchoredPosition;
        for (float t = 0f; t < 0.25f; t += Time.unscaledDeltaTime)
        {
            painel.anchoredPosition = base_ + Random.insideUnitCircle * 8f;
            yield return null;
        }
        painel.anchoredPosition = base_;
    }
}
