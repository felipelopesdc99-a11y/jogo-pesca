"""Boss Mundial (M24-T29, A-165): gera a renda de referência por nível usada nas recompensas das faixas.

Uso:
  python3 tools/Progressao/boss_mundial.py
      # reescreve config/world_boss.json → reference_income a partir de config/pass.json

Conta (sem simulação longa, de propósito):
  - as Moedas grátis de cada marco do Passe do Pescador são ~10 minutos da renda (Tripulação + pesca) de quem abre o
    jogo 4 vezes por dia ao chegar àquele nível (tools/Progressao/passe_fragmentos.py); então a renda por hora é 6 × elas;
  - as Conchas grátis do marco são ~30 minutos das Conchas da pesca; por hora, 2 × elas;
  - o nível 1 usa a linha do primeiro marco (Nv.10).
As faixas (world_boss.json → bands) dão income_minutes / shell_minutes dessa renda. O proprietário ajusta jogando.
"""
import json
import os

RAIZ = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
CONFIG = os.path.join(RAIZ, "config")

MINUTOS_MOEDAS_DO_PASSE = 10.0
MINUTOS_CONCHAS_DO_PASSE = 30.0


def main():
    with open(os.path.join(CONFIG, "pass.json"), encoding="utf-8") as f:
        passe = json.load(f)
    caminho = os.path.join(CONFIG, "world_boss.json")
    with open(caminho, encoding="utf-8") as f:
        boss = json.load(f)

    linhas = []
    for i, marco in enumerate(passe["milestones"]):
        gratis = marco.get("free") or {}
        moedas = int(round(gratis.get("coins", 0) * 60.0 / MINUTOS_MOEDAS_DO_PASSE))
        conchas = int(round(gratis.get("shells", 0) * 60.0 / MINUTOS_CONCHAS_DO_PASSE))
        linhas.append({"level": 1 if i == 0 else marco["level"], "coins_per_hour": moedas, "shells_per_hour": conchas})

    boss["reference_income"] = linhas
    with open(caminho, "w", encoding="utf-8") as f:
        json.dump(boss, f, ensure_ascii=False, indent=2)
        f.write("\n")
    print("world_boss.json: %d linhas de renda de referência (Nv.1: %s Moedas/h; Nv.%d: %s Moedas/h)."
          % (len(linhas), linhas[0]["coins_per_hour"], linhas[-1]["level"], linhas[-1]["coins_per_hour"]))


if __name__ == "__main__":
    main()
