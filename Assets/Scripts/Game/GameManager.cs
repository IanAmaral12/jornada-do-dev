using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [SerializeField] Tier tier;
    [SerializeField] int vida = 5;
    [SerializeField] PhaseExit porta;
    [SerializeField] EndScreen telaFinal;
    [SerializeField] AudioSource audioSource;
    [SerializeField] AudioClip jingleInicio;
    [SerializeField] AudioClip jingleBossDerrotado;
    [SerializeField] AudioClip jingleGameOver;
    [SerializeField] AudioClip jingleVitoria;

    public static GameManager Instancia { get; private set; }

    static QuestionBank bancoCompartilhado;

    public Tier CurrentTier => tier;
    public Health PlayerHealth { get; private set; }
    public QuestionBank Bank => bancoCompartilhado;
    public bool Acabou { get; private set; }

    void Awake()
    {
        Instancia = this;
        Time.timeScale = 1f;
        PlayerHealth = new Health(vida);
        bancoCompartilhado ??= QuestionBank.FromJson(Resources.Load<TextAsset>("Questions").text, new System.Random());
    }

    void Start()
    {
        HudController.Instancia?.Bind(PlayerHealth, PhaseInfo.Title(tier));
        Tocar(jingleInicio);
    }

    public void OnPlayerDefeated()
    {
        if (Acabou) return;
        Acabou = true;
        Tocar(jingleGameOver);
        telaFinal.MostrarGameOver();
    }

    public void OnBossDefeated()
    {
        Tocar(jingleBossDerrotado);
        porta.Abrir();
    }

    public void GoToNextPhase()
    {
        if (Acabou) return;
        var proxima = PhaseInfo.Next(tier);
        if (proxima.HasValue)
        {
            SceneManager.LoadScene(PhaseInfo.SceneFor(proxima.Value));
            return;
        }
        Acabou = true;
        Tocar(jingleVitoria);
        telaFinal.MostrarVitoria();
    }

    public void Retry() => SceneManager.LoadScene(SceneManager.GetActiveScene().name);

    public void Menu() => SceneManager.LoadScene("MainMenu");

    public void Creditos() => SceneManager.LoadScene("Credits");

    void Tocar(AudioClip clip)
    {
        if (clip && audioSource) audioSource.PlayOneShot(clip);
    }
}
