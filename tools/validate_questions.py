"""Valida o banco de perguntas executando cada trecho no Python 3.14+."""
import json
import subprocess
import sys

TIERS = ("Junior", "Pleno", "Senior")
MIN_POR_TIER = 10
MIN_BOSS_POR_TIER = 3


def validar(caminho: str) -> list[str]:
    with open(caminho, encoding="utf-8") as f:
        perguntas = json.load(f)["questions"]

    erros: list[str] = []
    ids = [q.get("id") for q in perguntas]
    for dup in {i for i in ids if ids.count(i) > 1}:
        erros.append(f"{dup}: id duplicado")

    for tier in TIERS:
        do_tier = [q for q in perguntas if q.get("tier") == tier]
        boss = [q for q in do_tier if q.get("boss")]
        if len(do_tier) < MIN_POR_TIER:
            erros.append(f"{tier}: {len(do_tier)}/{MIN_POR_TIER} perguntas")
        if len(boss) < MIN_BOSS_POR_TIER:
            erros.append(f"{tier}: {len(boss)}/{MIN_BOSS_POR_TIER} perguntas de boss")

    for q in perguntas:
        qid = q.get("id", "?")
        if q.get("tier") not in TIERS:
            erros.append(f"{qid}: tier inválido {q.get('tier')!r}")
        opcoes = q.get("options", [])
        if len(opcoes) != 4:
            erros.append(f"{qid}: precisa de 4 opções, tem {len(opcoes)}")
            continue
        if len(set(opcoes)) != 4:
            erros.append(f"{qid}: opções duplicadas")
        indice = q.get("correctIndex", -1)
        if not 0 <= indice < 4:
            erros.append(f"{qid}: correctIndex fora do intervalo")
            continue
        try:
            r = subprocess.run(
                [sys.executable, "-I", "-c", q["code"]],
                capture_output=True, text=True, timeout=5, encoding="utf-8",
            )
        except subprocess.TimeoutExpired:
            erros.append(f"{qid}: tempo esgotado")
            continue
        if r.returncode != 0:
            erros.append(f"{qid}: erro ao executar: {r.stderr.strip().splitlines()[-1:]}")
            continue
        saida = r.stdout.strip()
        if "\n" in saida:
            erros.append(f"{qid}: o código deve imprimir uma única linha")
        elif saida != opcoes[indice]:
            erros.append(f"{qid}: Python imprime {saida!r}, gabarito diz {opcoes[indice]!r}")
    return erros


def main() -> int:
    if sys.version_info < (3, 14):
        print(f"Requer Python 3.14+, encontrado {sys.version.split()[0]}")
        return 1
    caminho = sys.argv[1] if len(sys.argv) > 1 else "Assets/Resources/Questions.json"
    erros = validar(caminho)
    if erros:
        print("\n".join(erros))
        return 1
    with open(caminho, encoding="utf-8") as f:
        total = len(json.load(f)["questions"])
    print(f"OK {total} perguntas")
    return 0


if __name__ == "__main__":
    sys.exit(main())
