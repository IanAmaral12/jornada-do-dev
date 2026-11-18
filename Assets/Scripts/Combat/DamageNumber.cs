using UnityEngine;

public class DamageNumber : MonoBehaviour
{
    public static Font FontePadrao;

    const float Duracao = 0.6f;
    const float Subida = 0.8f;

    TextMesh texto;
    Vector3 inicio;
    Color cor;
    float t;

    public static void Spawn(Vector3 pos, string value, Color color)
    {
        var go = new GameObject("DamageNumber");
        go.transform.position = pos;
        var tm = go.AddComponent<TextMesh>();
        tm.text = value;
        tm.color = color;
        tm.fontSize = 64;
        tm.characterSize = 0.06f;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        if (FontePadrao != null)
        {
            tm.font = FontePadrao;
            go.GetComponent<MeshRenderer>().sharedMaterial = FontePadrao.material;
        }
        go.GetComponent<MeshRenderer>().sortingOrder = 100;
        var dn = go.AddComponent<DamageNumber>();
        dn.texto = tm;
        dn.inicio = pos;
        dn.cor = color;
    }

    void Update()
    {
        t += Time.unscaledDeltaTime;
        float k = t / Duracao;
        transform.position = inicio + Vector3.up * (Subida * (1f - (1f - k) * (1f - k)));
        texto.color = new Color(cor.r, cor.g, cor.b, 1f - k);
        if (k >= 1f) Destroy(gameObject);
    }
}
