using System.Collections;
using UnityEngine;

public class PlayerCombatAnimator : MonoBehaviour
{
    Animator animator;
    Vector3 escalaBase;

    void Awake()
    {
        animator = GetComponent<Animator>();
        escalaBase = transform.localScale;
    }

    public void Attack()
    {
        if (animator) animator.SetTrigger("Attack");
    }

    public void Hurt()
    {
        if (animator) animator.SetTrigger("Hurt");
    }

    public IEnumerator Squash(float duracao)
    {
        yield return Escalar(new Vector3(1.25f, 0.8f, 1f), duracao * 0.5f);
        yield return Escalar(Vector3.one, duracao * 0.5f);
    }

    public IEnumerator Stretch(float duracao)
    {
        yield return Escalar(new Vector3(0.8f, 1.2f, 1f), duracao * 0.5f);
        yield return Escalar(Vector3.one, duracao * 0.5f);
    }

    IEnumerator Escalar(Vector3 fator, float duracao)
    {
        Vector3 de = transform.localScale;
        Vector3 para = Vector3.Scale(escalaBase, fator);
        for (float t = 0f; t < duracao; t += Time.unscaledDeltaTime)
        {
            transform.localScale = Vector3.Lerp(de, para, t / duracao);
            yield return null;
        }
        transform.localScale = para;
    }
}
