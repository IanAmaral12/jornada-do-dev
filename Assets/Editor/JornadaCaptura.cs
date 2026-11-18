using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class JornadaCaptura
{
    const int Largura = 1280, Altura = 720;

    [MenuItem("Jornada/Capturar Telas")]
    public static void CapturarTodas()
    {
        Directory.CreateDirectory("docs/img");
        Capturar("MainMenu", "menu");
        Capturar("Junior", "junior");
        Capturar("Pleno", "pleno", "Senior");
        Capturar("Senior", "senior-combate", "Senior", true);
        Capturar("Credits", "creditos");
        Debug.Log("Jornada: telas capturadas em docs/img.");
    }

    static void Capturar(string cena, string arquivo, string tierPergunta = null, bool comPainel = false)
    {
        EditorSceneManager.OpenScene($"Assets/Scenes/{cena}.unity");
        var cam = Camera.main;

        if (comPainel)
        {
            var painel = Object.FindFirstObjectByType<TaskPanel>(FindObjectsInactive.Include);
            var banco = QuestionBank.FromJson(Resources.Load<TextAsset>("Questions").text, new System.Random(3));
            var raiz = painel.transform.Find("Janela").gameObject;
            raiz.SetActive(true);
            var tipo = typeof(TaskPanel);
            var q = banco.Next(Tier.Senior, true);
            raiz.transform.Find("Titulo").GetComponent<UnityEngine.UI.Text>().text = "BOSS: Recursão Sem Base";
            raiz.transform.Find("Enunciado").GetComponent<UnityEngine.UI.Text>().text = q.prompt;
            raiz.transform.Find("CaixaCodigo/Codigo").GetComponent<UnityEngine.UI.Text>().text = PythonHighlighter.Highlight(q.code);
            for (int i = 0; i < 4; i++)
                raiz.transform.Find($"Opcao{i + 1}").GetComponentInChildren<UnityEngine.UI.Text>().text = $"{i + 1}) {q.options[i]}";
            raiz.transform.Find("Feedback").GetComponent<UnityEngine.UI.Text>().text = "";
        }

        foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 1f;
        }
        Canvas.ForceUpdateCanvases();

        var rt = new RenderTexture(Largura, Altura, 24);
        cam.targetTexture = rt;
        cam.aspect = (float)Largura / Altura;
        cam.Render();
        cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(Largura, Altura, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, Largura, Altura), 0, 0);
        tex.Apply();
        File.WriteAllBytes($"docs/img/{arquivo}.png", tex.EncodeToPNG());
        RenderTexture.active = null;
        cam.targetTexture = null;
        Object.DestroyImmediate(rt);
    }
}
