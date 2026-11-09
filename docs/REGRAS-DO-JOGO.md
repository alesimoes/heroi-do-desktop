# Regras e números do jogo

Unidades: distâncias em "unidades de sprite" (múltiplos de `S`). Tempos em segundos.

## Cenário e itens
- 3 macieiras nas bordas (2 à direita, 1 embaixo à esquerda), 5 maçãs cada; cada maçã colhida volta em 60–100 s.
- Monstros derrotados soltam: coração 20% ou rupia 45% (valores abaixo). Itens somem em 15 s (piscam depois de 12).
- Rupias **no automático**: verde 1 (60%), azul 5 (30%), vermelha 20 (10%).
- Rupias **no automático com recorde ≥ 50**: verde 1 (30%), azul 5 (30%), amarela 10 (20%), vermelha 20 (15%), roxa 50 (5%).
- Rupias **no modo jogo**: azul 5 (40%), amarela 10 (30%), vermelha 20 (20%), roxa 50 (10%). Nunca a de 1.
- Coração cura 1. Itens são pegos ao passar por cima (raio 8).

## Monstros
| Tipo | Vida | Comportamento |
|---|---|---|
| Octorok (vermelho) | 2 | Anda em linha reta, para 0,6 s, cospe pedra mirada (80/s) a cada 1,5–3 s |
| Morcego | 1 | Voa ondulando até o herói (30/s) |
| Slime | 2 | Pula em ciclos (55/s por 0,45 s) |
| Bokoblin (rosado, chifres, porrete) | 3 | Persegue (26/s); a menos de 16 levanta o porrete (0,55 s) e bate (acerta se o herói estiver a menos de 20); descansa 0,8 s e repete. Contato também machuca |
- Sorteio no modo jogo: octorok 25%, morcego 30%, slime 30%, **bokoblin 15%**. No automático: 15% bokoblin, o resto 1/3 cada.
- Quando o herói morre e tenta de novo, o **dragão recupera toda a vida** (5000) e as bolas de fogo somem.
- Aparecem nas bordas a cada 3–7 s (dividido pelo multiplicador de rupias no modo jogo), máx. 4 (automático).
- Contato machuca (raio 9). Herói perde 1 coração e fica invulnerável 1,2 s.
- No **modo jogo** o sorteio é: octorok 30%, morcego 35%, slime 35% (octoroks 10% menos frequentes).

## Herói
- 5 corações. Espada: 1 de dano em raio 18 (golpe 0,32 s, acerta aos 0,11 s), corta pedras. Arco: flecha 210/s, 1 de dano,
  puxa em 0,4 s, recarga 0,35 s. Escudo bloqueia pedra/bola que vem de frente.
- Morte (automático): fica caído 3 s e volta sozinho.

## Piloto automático (cuidadoso)
- Constantes `Tune*`: segurança 22 (30 com vida ≤ 2), desvio de pedra 13, morcego 12, item 22, início do golpe 13,5.
- Octoroks/slimes: luta de espada (aproxima, golpeia). Morcegos: arco à distância, recua se chegam perto.
- Calibração: ~76 danos/hora simulados contra ~152 do comportamento antigo (**metade**), mesmos ~460 abates/hora.
  Quase todo dano vem de slimes. Há um harness de simulação descrito em `COMO-CONSTRUIR-E-PUBLICAR.md`.

## Pausas (só automático)
- A cada 2–5 min sem monstros novos: o herói termina a luta e pega o que caiu, depois descansa 30–110 s
  (acena, agacha, dorme). **40% de chance** de pausa longa de **120 s** com **colheita**: vai à macieira, colhe 3 maçãs,
  baixa arma e escudo e dorme (sem acenar).
- Com **5 maçãs**: aviso por cima de tudo + **lanche** (fogueira no meio, 10 s assando, come e cura).

## Recorde e Poder (só automático)
- **RECORDE** (canto superior direito): maior número de rupias já juntado no automático. Desktop: registro do Windows
  (`HKCU\Software\HeroiDoDesktop\Recorde`); web: `localStorage` (`heroi_recorde`).
- O poder é **lançado quando o Link derrota 20 gelatinas (slimes)** no modo automático (contador "GELATINAS n/20" no canto superior direito; **zera ao chegar a 20**, e o poder começa assim que o herói estiver livre; kills durante o poder ou o loot não contam). **O RECORDE não aparece mais na tela** (internamente o maior valor de rupias ainda é guardado e continua liberando rupias de maior valor a partir de 50). As rupias do contador ainda zeram quando o poder termina. O **PODER de 15 s**:
  herói **com as cores normais** (não fica azul; só faíscas douradas em volta), **invulnerável**, girando em órbita pela tela e atirando flechas de poder (1 a cada 0,05 s = 20 por segundo, mira no mais próximo).
- Durante o poder: **18x mais monstros** (cap 72, intervalo de nascimento /18, leva inicial de 9) que vão **direto no herói com o dobro da velocidade**.
- Ao acabar: os monstros restantes **explodem** (sem drops), ele colhe 3 maçãs; só se juntar 5 ou mais o ciclo normal do lanche assa e come; depois volta tudo ao normal.
- **Regra pedida pelo usuário:** o recorde **zera quando passa de 80** (grava 0 e o próximo poder volta a exigir 25 rupias). Fora isso:
- O recorde **nunca diminui**: no desktop fica no registro e em `%APPDATA%\HeroiDoDesktop\recorde.txt` (vale o maior); na web, no `localStorage`.
  Testes automáticos devem definir a variável de ambiente `HEROI_TESTE=1` para não mexer no recorde real.
- **Depois do poder:** o herói tenta pegar o que os monstros soltaram (rupias e corações) **antes** do lanche; os itens **somem normalmente em 15 s** (a regra de "não sumir" foi removida a pedido).
- Ao terminar, o herói colhe **3 maçãs** (só dá para pegar 3 durante o poder). O lanche **só acontece quando ele juntar 5 ou mais** maçãs, então nem sempre há fogueira depois do poder; ele **não pisca** durante o lanche. Entrar no modo jogo durante o poder o encerra sem recompensa.
- Teste rápido no desktop: `HeroiPet.exe --poder`.

## Evolução do herói (só automático)
Conta **todo monstro derrotado no modo automático** (inclui os do poder). O total é salvo (registro `Monstros` no desktop,
`localStorage heroi_monstros` na web) e as evoluções já conquistadas voltam sozinhas ao abrir.

| Evolução | Monstros | Item |
|---|---|---|
| 1 | 1000 | **Arco de metal** (prateado) |
| 2 | +500 (1500) | **Gorro roxo** |
| 3 | +500 (2000) | **Blusa azul** |
| 4 | +1000 (3000) | **Espada Mestre** (lâmina azul-clara, guarda azul, punho roxo) e **escudo vermelho** com quadrado amarelo |

Ao conquistar: o item aparece **bem grande no meio da tela** com o número de monstros que foram precisos (1000, 500, 500, 1000) e
o **jogo pausa**. Ao apertar o botão **USAR ITEM** o jogo volta, o item **diminui até o tamanho normal e voa até o herói**, que o veste.
Teste rápido: `HeroiPet.exe --evolucao` (desktop) ou `?evolucao` na URL (web) — o próximo monstro dá a 1ª evolução.

## Modo jogo
- Entrar pelo menu "Jogar": herói vai ao **meio**, monstros somem, **rupias e maçãs zeram**. Sair (automático): rupias
  zeram e vida máxima volta a 5.
- Controles: **mouse apertado** anda até o cursor; **1** espada; **2** arco mirando no cursor; **3** colher (perto da
  árvore) / pôr maçã na fogueira / comer; **Espaço** pausa. Sem acenar nem dormir.
- **Dificuldade por rupias:** a cada 25 rupias, +10% de monstros (base = 2/3 de 4), até +60% (150 rupias).
  A partir de +40% (100, 125, 150) cada degrau dá **+1 coração máximo** (até 8).
- **Morte:** tela "VOCÊ MORREU" + "Tentar de novo"; volta com 10 rupias a menos (mínimo 0); nível/corações recalculados.

## Chefão (Dragão) — só no modo jogo
1. No nível máximo (150 rupias) e **5 mortes** depois: os monstros que sobraram explodem, aviso grande aparece.
2. Fogueira no meio. O jogador colhe, põe maçãs (3+2) e come **assadas** (10 s) com a tecla 3; cada uma cura **2**.
3. Com vida cheia (depois de comer ao menos uma): herói se afasta, a fogueira explode, fumaça, **Dragão** nasce.
4. Dragão: **5000 de vida** (o dobro), ~1,7× maior, **anda** em volta do herói (14/s, não pisa nele), cospe bola de fogo mirada
   a cada 1,5–3 s. Flecha 50, espada 150. **Com metade da vida (fúria)**: fica **vermelho**, **+10% de tamanho** (hitboxes acompanham), anda
   **um pouco mais rápido** (18/s, ainda bem mais lento que o herói, 56/s), **solta fumaça do nariz de vez em quando** (a cada 2,5–5,5 s),
   bolas de fogo **20% mais frequentes**, 3 bolas em leque. **Com 10%**: bolas **teleguiadas** (52/s, curvam 1,8 rad/s) que **explodem em 3 s** (raio 16).
5. **Morte do dragão:** ele **volta ao normal** (roxo, tamanho normal) e pisca por 1,3 s; depois **explode** e solta **1000 rupias** em valores
   diferentes (2×**300 dourada**, 4×50, 5×20, 8×10, 3×5, 5×1 = 1000), que se espalham e **não somem**. Cerca de 2 s depois da explosão aparece
   **"ESPERE POR MAIS ATUALIZAÇÕES"** por 4,5 s e então os **créditos** (que rolam com o jogo ainda ativo para pegar as rupias) → volta ao modo automático.
