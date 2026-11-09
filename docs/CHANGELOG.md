# Changelog (resumo)

## 09/11/2026 — Escudo roxo no baú
- O tesouro do baú agora inclui um **escudo circular roxo escuro com um quadrado azul**, um pouco menor que o atual.

## 09/11/2026 — Baú do Rei
- Depois de **5000 octoroks e bokoblins** (morcegos e gelatinas não contam) aparece um **baú** no modo automático: "aperte 4 para abrir". Dá **coroa de ouro, roupa de rei, calça marrom e sapatos roxos**.

## 09/11/2026 — Armas evoluídas dão o dobro de dano
- **Arco prateado** (evolução 1): flechas com o dobro de dano. **Espada azulada / Espada Mestre** (evolução 4): o dobro de dano. Vale também contra o dragão (flecha 100, espada 300).

## 09/11/2026 — Onda de choque do dragão
- Nos **últimos 15% de vida**, a cada 8 s o dragão lança uma **onda de choque**: a área pisca 3 vezes, a onda irradia ao redor dele e, se encostar no herói, tira **2 corações**.

## 09/11/2026 — Dano: bolas de fogo e meio coração
- **Cada bola de fogo do dragão tira 1 coração** (antes algumas não davam dano: o escudo bloqueava as de frente e a invulnerabilidade engolia as seguintes).
- **Encostar em um monstro tira meio coração**; corações com metades no HUD.

## 09/11/2026 — Dragão: crescimento lento e brilho
- Ao ficar vermelho (metade da vida) o dragão **cresce devagar (3 s) até 1,2×** (mais 10% além dos 10% anteriores) e **brilha** com faíscas durante o crescimento.

## 09/11/2026 — Dragão reforçado
- Dragão com **5000 de vida**. Na metade: **vermelho**, +10% maior, um pouco mais rápido, **fumaça do nariz**, bolas de fogo 20% mais frequentes.
- Ao morrer: volta ao normal, **explode e solta 1000 rupias** (inclui a **rupia dourada de 300**), aparece **"ESPERE POR MAIS ATUALIZAÇÕES"** e então os créditos.

## 08/11/2026 (fim da noite) — GitHub, ícones e certificado
- Repositório **público** no GitHub (MIT) com build automático; preparação da assinatura gratuita (SignPath): política, workflows, guia e texto do pedido.
- Web: botões de toque com **ícones** (espada, arco, maçã) que acompanham as evoluções; antes eram textos que se sobrepunham.
- **Decisão final:** não assinar o instalador; material do SignPath arquivado em `docs/assinatura-arquivada/`.
- Documentado o problema do certificado (`PROBLEMA-CERTIFICADO.md`): instalador sem assinatura é bloqueado em outros PCs; restrição "não pagar nada".
- Documentação atualizada e consolidada (`STATUS.md`).

## 08/11/2026 (noite 4) — evolução, poder por gelatinas, tela cheia, publicação
- **Evolução** do herói por monstros derrotados (1000 / +500 / +500 / +1000): arco de metal, gorro roxo, blusa azul, Espada Mestre + escudo vermelho. O item aparece grande, o jogo pausa até **USAR ITEM**, o item diminui e voa até o herói.
- **Poder** agora é lançado com **20 gelatinas** (não mais pelo recorde); herói com cores normais; 18x monstros; 5x mais projéteis; colhe 3 maçãs; itens somem em 15 s.
- RECORDE removido da tela (continua guardado e libera rupias de maior valor a partir de 50); contador "GELATINAS n/20".
- **Nada do jogo se sobrepõe a outros aplicativos** (aviso, morte, créditos e botão de evolução ficam no fundo).
- Web: tela cheia ao clicar em Jogar / Deixar rodando + botão "Tela cheia" no menu.
- `docs/COMO-INSTALAR.txt` (avisos do Windows), preparação do repositório aberto (MIT, README, política de assinatura SignPath, workflow de build).

## 08/11/2026 (noite 3) — tamanho do herói e do cenário
- Opção **Tamanho: Pequeno / Normal / Grande** (menu da bandeja no desktop, menu da página na web). Muda a escala S (×0,67 / ×1 / ×1,34), então herói, monstros, árvores, itens e HUD acompanham. A escolha fica salva (registro `Tamanho` no desktop, `localStorage heroi_tamanho` na web).

## 08/11/2026 (noite 2) — Recorde, Poder e sombras
- **RECORDE** no canto superior direito e **PODER de 15 s** ao bater o recorde (desktop e web). Ver `REGRAS-DO-JOGO.md`.
- Sombras do desktop com transparência real (`ShadowLayer`).

## 08/11/2026 (noite) — Bokoblin e dragão
- Novo monstro **Bokoblin** (arte própria), em desktop e web.
- Ao morrer e tentar de novo, o dragão volta com a vida cheia.
- Instalador reconstruído com ambas as mudanças.
- Modo jogo: indicador **NV n** (começa em 1, sobe a cada 25 rupias, máx. 7) ao lado da maçã no topo; a maçã do topo agora tem o tamanho da rupia.

## Web, 08/11/2026 (depois) — controles de celular
- Celular **deitado**: direcional analógico no polegar esquerdo (andar; a mira/espada seguem a última direção), botões ESPADA / ARCO / COLHER no polegar direito.
- Sem zoom (viewport sem escala, bloqueio de pinça/duplo toque), tela cheia + trava de paisagem no 1º toque, aviso "Deite o telefone" em retrato.
- Teste no computador: abrir a página com `?touch`.

## 1.0.0 — 08/11/2026
**Desktop**
- Herói pixel art na área de trabalho (janela transparente atrás dos aplicativos); passeia, luta (espada e arco), desvia de pedras.
- Monstros: octorok, morcego, slime. Itens: corações e rupias. Macieiras e maçãs.
- Pausas com acenar, colheita e soneca (deitado de costas, de perfil). Lanche com fogueira e maçãs assadas (aviso por cima de tudo).
- Piloto automático cuidadoso (metade dos danos, medido por simulação).
- Cabelo no herói (franja/costeletas). Menu na bandeja: Jogar/Automático, Invocar inimigo, Curar, Soneca agora, Colher maçãs,
  Dar 5 maçãs, Pausar, Iniciar com o Windows, **Sobre**, Sair.
- **Modo jogo:** mouse+1/2/3, espaço pausa, dificuldade por rupias, corações extras, rupias 5–50, tela de morte, janela de proteção de cliques (Ctrl libera).
- **Chefão Dragão** (2500 de vida, anda, leque de fogo, bolas teleguiadas) + FIM DE JOGO + créditos.
- Ao entrar no jogo: herói ao meio, monstros somem, rupias/maçãs zeram.
- **Correção:** "Mostrar área de trabalho" (Win+D) escondia o herói; agora ele sobe junto (topmost só nesse momento).
- Instalador (`Instalador-HeroiDoDesktop.exe`) com ícone, atalhos e desinstalador.

**Web**
- Porte completo em página única; cenário elaborado (lagoa, trilha, casinha, floresta, cercas, flores, borboletas).
- Botões de toque para celular, menu, **Sobre**, créditos, pausa com espaço.
- Publicada em https://heroi-games-2026.web.app (Firebase Hosting) e como Claude Artifact.

**Descartado:** v2 (gráfico EPX), v3 (vetorial), web em 16 bits.
