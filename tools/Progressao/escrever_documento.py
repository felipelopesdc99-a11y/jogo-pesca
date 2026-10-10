"""Escreve docs/PROGRESSAO_MAPAS_5_A_10_ADAPTADA.md a partir de docs/propostas/mapas_5_10.json."""
import json, os
ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
P = json.load(open(os.path.join(ROOT, 'docs', 'propostas', 'mapas_5_10.json'), encoding='utf-8'))
br = lambda n: '{:,}'.format(int(n)).replace(',', '.')
pct = lambda x: ('%.1f' % (x * 100)).replace('.', ',').replace(',0', '') + '%'
dec = lambda x: ('%.2f' % x).replace('.', ',')
RN = {'common': 'Comum', 'rare': 'Raro', 'epic': 'Épico', 'legendary': '**Lendário**', 'mythic': '**Mítico**'}
MAPS = {'map_05': ('Costa de Coral', 40, 'Maré Dourada', 'Finalmente cheguei ao mar aberto da costa.'),
        'map_06': ('Arquipélago do Sol', 50, 'Corrente Mestra', 'Longe da costa, os peixes viram troféus.'),
        'map_07': ('Corrente Azul', 60, 'Corrente Mestra', 'Só o horizonte e o mar azul em volta.'),
        'map_08': ('Banco das Baleias', 70, 'Atlântico Nobre', 'Aqui o oceano mostra o seu tamanho.'),
        'map_09': ('Talude Noturno', 80, 'Atlântico Nobre', 'A noite revela o que vive no fundo.'),
        'map_10': ('Abismo Atlântico', 90, 'Soberana Abissal', 'O destino final de todo pescador.')}
L = []
w = L.append
w('# Fishing Idle — Progressão dos Mapas 5 a 10, adaptada ao jogo atual')
w('')
w('**Base:** `docs/PROGRESSAO_MAPAS_5_A_10.md`, enviado pelo proprietário em 05/10/2026. **Adaptado em:** 05/10/2026, a pedido dele:')
w('"mantenha as progressões de mapas e os peixes; adapte o que for preciso às regras de agora". **Situação:** proposta aprovada no conteúdo, ainda **não** está no jogo (nada em `/config` mudou).')
w('')
w('Os números deste documento saem de `tools/Progressao/adaptar_mapas_5_10.py` e ficam em `docs/propostas/mapas_5_10.json`, pronto para virar `/config` na implementação (roadmap M21).')
w('')
w('## O que fica exatamente como o proprietário mandou')
w('')
w('- Os 6 mapas, os nomes, os níveis (40, 50, 60, 70, 80, 90), a vara mínima de cada um, a direção visual, a luz, a fauna e as paletas (seção 4 do original).')
w('- As 60 espécies, os IDs, os nomes, as raridades, os tamanhos e os pesos de captura de cada mapa. A ordem de valor entre as espécies de um mesmo mapa também não muda.')
w('- Lendário a partir do Mapa 6 e Mítico só no Mapa 10, com os multiplicadores do documento (atributos 1,55 e 1,85; XP do Pescador ×9 e ×15; alimento ×6 e ×10; venda ×8 e ×14) e chance-base de puxar 14% e 8%.')
w('- As Varas 3 a 5 (Corrente Mestra, Atlântico Nobre, Soberana Abissal), os níveis de liberação, os mapas que cada uma atende e os bônus de raridade, tamanho e Conchas por nível.')
w('- Barcos e iscas: nada novo (os níveis do documento já são os do jogo). Nenhum prestígio ou reinício no Nv.100. A curva de XP do Pescador não muda.')
w('')
w('## O que foi adaptado às regras de agora, e por quê')
w('')
w('| Ponto | No documento original | Adaptado | Regra atual que pede isso |')
w('|---|---|---|---|')
w('| Valor de venda dos peixes | Mapa 5 vale menos que o Mapa 4 atual (ex.: Raro 2.000–2.300 contra 2.635–3.410 no Estuário) | Cada raridade recebe um fator que faz o Mapa 5 render 1,5× o Mapa 4 real (Comum ×%s, Raro ×%s, Épico/Lendário/Mítico ×%s). O mesmo fator vale nos Mapas 6 a 10, então a escada entre os mapas do documento continua igual | O documento foi escrito com números antigos e pede para adaptar à economia real (seção 18) |' % (dec(P['factors']['sale/common']), dec(P['factors']['sale/rare']), dec(P['factors']['sale/epic'])))
w('| XP do Pescador por peixe | Mapa 5 dá menos XP que o Mapa 4; do Mapa 7 em diante cada faixa de 10 níveis ficaria cada vez mais curta (o 90–100 em menos de 1 h online) | O XP de cada mapa foi calibrado para a faixa dele levar o mesmo tempo online que a faixa 30–40 leva hoje (cerca de %s h), com uma trava: em cada raridade, o XP de um mapa nunca fica abaixo do mapa anterior. Proporção entre as espécies mantida. Por causa da trava, as faixas do 60 ao 100 ficam um pouco mais rápidas (decisão OD-047) | A curva de XP do jogo é a fonte de verdade (seção 9) e o ritmo atual dos Mapas 3 e 4 (A-095) |' % dec(P['target_hours_per_band']))
w('| XP de alimento | Abaixo do Mapa 4 | Mesmo método da venda, mirando 1,3× o Mapa 4 (Comum ×%s, Raro ×%s, Épico/Lendário/Mítico ×%s) | Economia real |' % (dec(P['factors']['feed_xp/common']), dec(P['factors']['feed_xp/rare']), dec(P['factors']['feed_xp/epic'])))
w('| Atributos | Alguns Raros e Épicos do Mapa 5 mais fracos que os do Mapa 4 | Só sobem quando o Mapa 5 ficaria abaixo do Mapa 4 +15%%: Ataque do Raro ×%s; Vida ×%s, Ataque ×%s e Defesa ×%s do Épico (o mesmo vale para Lendário e Mítico). Velocidade não muda | Peixe de mapa novo deve substituir o antigo no Cardume (seção 8 do original) |' % (dec(P['factors']['attack/rare']), dec(P['factors']['hp/epic']), dec(P['factors']['attack/epic']), dec(P['factors']['defense/epic'])))
w('| Chance de puxar das varas | +12 a +40 p.p. | Um terço menor: +8 a +26,7 p.p. A vara nova começa abaixo do Nv.10 da anterior em chance, mas acima em raridade, tamanho e Conchas, igual a Maré Dourada faz hoje com a Ponta Selvagem (+5,5 contra +8) | A-095: todos os bônus de chance caíram um terço; o documento usou a escala antiga |')
w('| Conchas nas varas | Corrente Mestra e as melhorias das Varas 3 a 5 só em Moedas | Toda compra e melhoria pede Conchas: Corrente Mestra 85 (entre os 40 da Maré Dourada e os 180 da Atlântico Nobre); melhorias no mesmo formato da Maré Dourada, proporcional à compra | A-099: todo equipamento pede Conchas |')
w('| Preço das varas em Moedas | 450 mil / 1,8 mi / 8 mi | Multiplicado pelo quanto a renda por peixe subiu no mapa onde o jogador junta o dinheiro, para o tempo de compra continuar o que o documento pensou (cerca de 3 h online cada) | Seção 6: "ajustar custos, não reescrever a curva" |')
w('| Cor do Lendário | Laranja `#F97316` | Vermelho-rubi `#E5484D` | O laranja colide com o tamanho Grande (`#FF9F6B`) e o dourado com Excepcional; o tema hoje usa dourado provisório para Lendário |')
w('| Cor do Mítico | Rosa-framboesa `#EC4899` | Igual | — (o tema hoje tem um coral provisório, que sai) |')
w('| Tamanhos de Lendário e Mítico | Não fala | Um pouco menos Grande, Excepcional e Perfeição, continuando a regra do Raro e do Épico (Lendário: Grande ×0,6, Excepcional e Perfeição ×0,35; Mítico: ×0,5 e ×0,25) | Regra de tamanho por raridade (Raro ×0,85/0,7; Épico ×0,7/0,45) |')
w('| Frase de chegada e capítulo | Não fala | Capítulos 5 a 10, cada um com uma frase de chegada (tabela abaixo) | A-085: cada mapa é um capítulo com título ao chegar |')
w('| Proteção de peixes valiosos | "Raro ou superior" | A venda em lote passa a listar Lendário e Mítico também; a alimentação já usa "Raro ou acima" | economy.json hoje lista só Raro e Épico |')
w('')
w('Ficam para o balanceamento depois da implementação (M21-T07 e T08), sem mudar nada agora: recompensas das Expedições, adversários da Arena e anúncios simulados do Mercado, que hoje são pensados até o Mapa 4.')
w('')
w('## Ritmo esperado')
w('')
w('Estimativa simples, sem isca e sem efeito de tamanho, contando só peixes puxados, 120 tentativas por hora online e o equipamento esperado em cada faixa. A simulação completa da seção 15 do original (M21-T07) confirma ou corrige.')
w('')
w('| Faixa | Mapa | Moedas por tentativa | × anterior | XP por tentativa | × anterior | Horas online na faixa |')
w('|---|---|---:|---:|---:|---:|---:|')
for mid, c in P['chain'].items():
    w('| Nv.%s | %s | %s | %s | %s | %s | %s |' % (c['niveis'].replace('-', '–'), MAPS[mid][0], br(c['moedas_por_tentativa']), dec(c['moedas_x_anterior']), dec(c['xp_por_tentativa']).replace(',', ',', 1), dec(c['xp_x_anterior']), dec(c['horas_online_na_faixa'])))
w('')
w('As Moedas sobem cerca de 2× por mapa, o mesmo salto que o jogo já tem do Mapa 3 para o Mapa 4 (×2,1). O XP por tentativa sobe de 1,34× a 1,54× por mapa. Até o Nv.60 cada faixa leva o mesmo tempo da faixa 30–40 de hoje; depois fica mais rápida, até cerca de 2 h no 90–100. O motivo é o próprio desenho dos mapas finais: muito mais Raros, Épicos e Lendários no pool, e Lendário e Mítico dão ×9 e ×15 de XP. Para manter todas as faixas com o mesmo tempo, o XP dos peixes Comuns teria de cair nos últimos mapas (um Comum do Abismo valeria menos que um do Estuário). Por isso a escolha ficou com o proprietário (OD-047).')
w('')
w('## Raridades novas')
w('')
w('| Raridade | Chance-base de puxar | Atributos | XP do Pescador | Alimento | Venda | Cor |')
w('|---|---:|---:|---:|---:|---:|---|')
for r in P['rarities_new']:
    w('| %s | %s | ×%s | ×%s | ×%s | ×%s | `%s` |' % (r['display_name'], pct(r['catch_success_base']), dec(r['stat_multiplier']), dec(r['fisher_xp_multiplier']), dec(r['feed_xp_multiplier']), dec(r['sale_value_multiplier']), r['color']))
w('')
w('## Mapas, capítulos e frases de chegada')
w('')
w('| Capítulo | Mapa | Nível | Vara mínima | Frase ao chegar |')
w('|---:|---|---:|---|---|')
for i, (mid, m) in enumerate(MAPS.items()):
    w('| %d | %s | %d | %s | %s |' % (5 + i, m[0], m[1], m[2], m[3]))
w('')
w('## Varas 3 a 5')
w('')
w('| Vara | Libera | Compra | Chance de puxar Nv.1 → Nv.10 | Melhorias Nv.2 a 10 (total) | No original |')
w('|---|---:|---|---|---|---|')
for r in P['rods']:
    b = r['bonuses_per_level']['catch_success']; d = r['doc_values']
    up_c = sum(u['cost_coins'] for u in r['upgrade_costs']); up_s = sum(u['cost_shells'] for u in r['upgrade_costs'])
    w('| %s | Nv.%d | %s Moedas + %d Conchas | +%s → +%s p.p. | %s Moedas + %d Conchas | %s Moedas%s; +%d → +%d p.p.; melhorias %s Moedas |' % (
        r['display_name'], r['unlock_fisher_level'], br(r['purchase_cost_coins']), r['purchase_cost_shells'], dec(b[0] * 100), dec(b[-1] * 100), br(up_c), up_s,
        br(d['purchase_cost_coins']), (' + %d Conchas' % d['purchase_cost_shells']) if d['purchase_cost_shells'] else '', d['catch_success_pp'][0], d['catch_success_pp'][-1], br(sum(d['upgrade_costs_coins']))))
w('')
w('Custo de cada melhoria (Moedas / Conchas):')
w('')
w('| Vara | ' + ' | '.join('→ Nv.%d' % i for i in range(2, 11)) + ' |')
w('|---|' + '---:|' * 9)
for r in P['rods']:
    w('| %s | ' % r['display_name'] + ' | '.join('%s / %d' % (br(u['cost_coins']), u['cost_shells']) for u in r['upgrade_costs']) + ' |')
w('')
w('Conchas: com a chance de 7%% por peixe puxado (A-099), juntar as Conchas da compra leva cerca de 5 h online para a Corrente Mestra, 8 h para a Atlântico Nobre e 24 h para a Soberana Abissal (os 650 do original foram mantidos). É uma estimativa; a ideia da A-099 é que os jogadores também negociem Conchas.')
w('')
w('## As 60 espécies com os números adaptados')
w('')
w('Tamanho, peso de captura, raridade e Velocidade são os do original. Venda, XP e atributos já adaptados.')
for mid, m in MAPS.items():
    w('')
    w('### %s (Nv.%d)' % (m[0], m[1]))
    w('')
    w('| ID | Espécie | Raridade | Tamanho cm | Vida | Ataque | Defesa | Velocidade | Venda | XP de alimento | XP do Pescador | Peso |')
    w('|---|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|')
    for s in [x for x in P['species'] if x['primary_map_id'] == mid]:
        st = s['base_stats']
        w('| `%s` | %s | %s | %d–%d | %d | %d | %d | %d | %s | %s | %s | %d |' % (s['id'], s['display_name'], RN[s['rarity']], s['size_cm']['min'], s['size_cm']['max'], st['hp'], st['attack'], st['defense'], st['speed'], br(s['base_sale_value_coins']), br(s['base_feed_xp']), br(s['base_fisher_xp']), s['catch_weight']))
w('')
w('## O que vem depois')
w('')
w('A ordem de implementação é a da seção 16 do original (roadmap M21): raridades novas, espécies, mapas com cenário provisório, varas, proteções e filtros por hierarquia, integração e migração de save, simulação por mapa, ajuste fino, pedidos de arte e arte final mapa a mapa.')
open(os.path.join(ROOT, 'docs', 'PROGRESSAO_MAPAS_5_A_10_ADAPTADA.md'), 'w', encoding='utf-8').write('\n'.join(L) + '\n')
print('ok', len(L), 'linhas')
