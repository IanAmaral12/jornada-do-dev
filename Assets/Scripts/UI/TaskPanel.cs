using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class TaskPanel : MonoBehaviour
{
    [SerializeField] GameObject raiz;
    [SerializeField] Text titulo;
    [SerializeField] Text enunciado;
    [SerializeField] Text codigo;
    [SerializeField] Button[] botoes;
    [SerializeField] Text feedback;
    [SerializeField] AudioSource audioSource;
    [SerializeField] AudioClip sfxClique;
    [SerializeField] AudioClip sfxCorreto;
    [SerializeField] AudioClip sfxErrado;

    public event Action<int> Answered;

    public PythonQuestion PerguntaAtual { get; private set; }
    public bool AguardandoResposta => !travado && raiz.activeSelf;

    PlayerControls controles;
    bool travado = true;

    static readonly string[] Letras = { "1", "2", "3", "4" };

    void Awake()
    {
        controles = new PlayerControls();
        for (int i = 0; i < botoes.Length; i++)
        {
            int indice = i;
            botoes[i].onClick.AddListener(() => Responder(indice));
        }
        raiz.SetActive(false);
    }

    void OnEnable()
    {
        controles.Combat.Enable();
        controles.Combat.Answer1.performed += Tecla1;
        controles.Combat.Answer2.performed += Tecla2;
        controles.Combat.Answer3.performed += Tecla3;
        controles.Combat.Answer4.performed += Tecla4;
    }

    void OnDisable()
    {
        controles.Combat.Answer1.performed -= Tecla1;
        controles.Combat.Answer2.performed -= Tecla2;
        controles.Combat.Answer3.performed -= Tecla3;
        controles.Combat.Answer4.performed -= Tecla4;
        controles.Combat.Disable();
    }

    void OnDestroy() => controles.Dispose();

    void Tecla1(InputAction.CallbackContext _) => Responder(0);
    void Tecla2(InputAction.CallbackContext _) => Responder(1);
    void Tecla3(InputAction.CallbackContext _) => Responder(2);
    void Tecla4(InputAction.CallbackContext _) => Responder(3);

    public void Show(string nomeInimigo, PythonQuestion q)
    {
        raiz.SetActive(true);
        PerguntaAtual = q;
        titulo.text = nomeInimigo;
        enunciado.text = q.prompt;
        codigo.text = PythonHighlighter.Highlight(q.code);
        for (int i = 0; i < botoes.Length; i++)
        {
            botoes[i].GetComponentInChildren<Text>().text = $"{Letras[i]}) {q.options[i]}";
            botoes[i].interactable = true;
        }
        feedback.text = "";
        travado = false;
    }

    public void Lock()
    {
        travado = true;
        foreach (var b in botoes) b.interactable = false;
    }

    public void ShowFeedback(bool correct, string explanation)
    {
        Tocar(correct ? sfxCorreto : sfxErrado);
        feedback.text = correct
            ? "<color=#50fa7b>Correto! Golpe certeiro.</color>"
            : $"<color=#ff5555>Errado!</color> {explanation}";
    }

    public void Hide()
    {
        Lock();
        raiz.SetActive(false);
    }

    public void Responder(int indice)
    {
        if (travado || !raiz.activeSelf) return;
        Lock();
        Tocar(sfxClique);
        Answered?.Invoke(indice);
    }

    void Tocar(AudioClip clip)
    {
        if (clip && audioSource) audioSource.PlayOneShot(clip);
    }
}
