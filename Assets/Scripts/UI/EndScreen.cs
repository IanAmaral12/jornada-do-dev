using UnityEngine;

public class EndScreen : MonoBehaviour
{
    [SerializeField] GameObject gameOver;
    [SerializeField] GameObject vitoria;

    void Awake()
    {
        gameOver.SetActive(false);
        vitoria.SetActive(false);
    }

    public void MostrarGameOver() => gameOver.SetActive(true);

    public void MostrarVitoria() => vitoria.SetActive(true);
}
