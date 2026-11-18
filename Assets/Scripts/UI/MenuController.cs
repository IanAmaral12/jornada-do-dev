using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuController : MonoBehaviour
{
    void Awake() => Time.timeScale = 1f;

    public void Play() => SceneManager.LoadScene("Junior");

    public void Credits() => SceneManager.LoadScene("Credits");

    public void Menu() => SceneManager.LoadScene("MainMenu");

    public void Quit() => Application.Quit();
}
