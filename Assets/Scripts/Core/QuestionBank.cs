using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class QuestionBank
{
    readonly List<PythonQuestion> all;
    readonly System.Random rng;
    readonly Dictionary<(Tier, bool), Queue<PythonQuestion>> filas = new();
    readonly Dictionary<(Tier, bool), PythonQuestion> ultima = new();

    QuestionBank(List<PythonQuestion> questions, System.Random rng)
    {
        all = questions;
        this.rng = rng;
    }

    public static QuestionBank FromJson(string json, System.Random rng)
    {
        var set = JsonUtility.FromJson<QuestionSet>(json);
        var list = set?.questions?.ToList() ?? new List<PythonQuestion>();
        foreach (var q in list)
        {
            if (q.options == null || q.correctIndex < 0 || q.correctIndex >= q.options.Length)
                throw new ArgumentException($"Pergunta '{q.id}' com correctIndex inválido.");
            if (!Enum.TryParse(q.tier, out Tier _))
                throw new ArgumentException($"Pergunta '{q.id}' com tier inválido: {q.tier}.");
        }
        return new QuestionBank(list, rng);
    }

    public int Count(Tier tier, bool boss) => Pool(tier, boss).Count;

    public PythonQuestion Next(Tier tier, bool boss)
    {
        var chave = (tier, boss);
        if (!filas.TryGetValue(chave, out var fila) || fila.Count == 0)
        {
            var embaralhada = Pool(tier, boss).OrderBy(_ => rng.Next()).ToList();
            ultima.TryGetValue(chave, out var anterior);
            if (embaralhada.Count > 1 && embaralhada[0] == anterior)
                (embaralhada[0], embaralhada[^1]) = (embaralhada[^1], embaralhada[0]);
            fila = new Queue<PythonQuestion>(embaralhada);
            filas[chave] = fila;
        }
        if (fila.Count == 0)
            throw new InvalidOperationException($"Nenhuma pergunta para {tier}.");
        var q = fila.Dequeue();
        ultima[chave] = q;
        return q;
    }

    List<PythonQuestion> Pool(Tier tier, bool boss)
    {
        var doTier = all.Where(q => q.TierValue == tier).ToList();
        var filtradas = doTier.Where(q => q.boss == boss).ToList();
        return filtradas.Count > 0 ? filtradas : doTier;
    }
}
