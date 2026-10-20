# Arte antiga guardada como fonte (fora do jogo)

Arte pintada do estilo antigo ("Lago Dourado — Clean Premium") que **não vai no build do jogo**: fica aqui, fora de
`client-unity/Assets/Resources`, só para as ferramentas que ainda partem dela (M25-T17, 20/10/2026).

| Pasta | O que é | Quem lê |
|---|---|---|
| `Mapas/LagoSereno`, `Mapas/BancoDasBaleias`, `Mapas/TaludeNoturno` | as 10 laterais e cantos que viraram os provisórios em pixel (pedidos L1-06b, L6-08b e L6-09b) | `python3 tools/Arte/cena_pixel.py provisorios` |
| `Vivos/Animais`, `Vivos/Plantas` | a paisagem viva pintada (222 desenhos) da qual saiu a versão em pixel | `python3 tools/Arte/cena_pixel.py vivos` |

Quando os pedidos L1-06b, L6-08b e L6-09b chegarem em pixel de verdade, os arquivos de `Mapas/` podem ser apagados.
O jogo nunca lê esta pasta.
