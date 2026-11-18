using UnityEngine;

public class PhaseExit : MonoBehaviour
{
    [SerializeField] GameObject bloqueio;
    [SerializeField] SpriteRenderer porta;
    [SerializeField] Sprite portaAberta;

    bool aberta;

    public void Abrir()
    {
        aberta = true;
        if (bloqueio) bloqueio.SetActive(false);
        if (porta && portaAberta) porta.sprite = portaAberta;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (aberta && other.GetComponent<PlayerController>() != null)
            GameManager.Instancia.GoToNextPhase();
    }
}
