# Changelog (resumo)

## 10/11/2026 — Herói de frente no inventário
- No inventário o herói aparece **totalmente de frente** (a mesma aparência atual do herói de frente, a das pausas), com o escudo baixado ao lado e espada ou arco na mão direita (sem armas na aba Roupas). Desktop e web.

## 10/11/2026 — Franja maior
- A franja do herói ficou **um pouco maior**: o cabelo cobre mais a testa e desce um pouco sobre o olho (de perfil) e um pixel a mais na testa (de frente e dormindo).

## 10/11/2026 — Herói de frente
- Cabeça redonda (cabelo em volta do rosto), **franja cobrindo o olho direito do herói** e pé corrigido (alinhado com a perna). Desktop e web.

## 10/11/2026 — 5 conjuntos de roupa por monstros derrotados
- Cinco conjuntos (gorro + blusa + calça + sapatos): **areia** (4500 monstros), **gelo** (6000), **fogo** (8000), **noite** (10500) e **mar** (14000), contados por um **contador próprio, que começa em zero quando esta versão é instalada** (não herda os monstros já derrotados em versões antigas; os itens que já tinha — arco prateado, gorro roxo etc. — continuam). Ao liberar, o conjunto já vem vestido, com o aviso NOVO ITEM. Aparecem em Roupas (Gorros e Roupas); o inventário passou a **rolar com a roda do mouse** quando há muitos itens. Teste: `--conjuntos` / `?conjuntos`. Cores e limites são provisórios (para o usuário refinar).

## 10/11/2026 — Itens de menu removidos
- Removidas as opções **Colher maçãs e dormir (2 min)** e **Dar 5 maçãs (lanche)** (menu da bandeja no desktop e menu da página na web).

## 09/11/2026 — Colher maçã pelo inventário
- O botão **Colher maçã** do inventário agora fecha o menu e o herói **vai até a macieira mais próxima e colhe 2 maçãs** (que voam para o inventário). **Enquanto isso não nascem monstros** (nem a onda final). Vale nos dois modos. O braço esticado para pegar a maçã tem a **manga da cor da roupa** que ele estiver usando (verde, azul ou manto do rei).

## 09/11/2026 — Flechas do poder azul
- As flechas azuis do poder agora dão **9999999** de dano (matam qualquer monstro na hora, como a fadinha).

## 09/11/2026 — Vida dos monstros e dano das armas
- Vida: bokoblin a cavalo **250**, octorok **200**, bokoblin normal **100**, gelatina **50**, morcego **50**. A fadinha continua matando na hora.
- Dano das armas igual ao do dragão em todos os monstros: arco **50** (arco de metal **100**), espada **150** (Espada Mestre **300**).

## 09/11/2026 — Roupa de rei no inventário
- Ícone da **roupa de rei** em pixel art: **blusa** vermelha (mesmo corte das outras) com **faixa dourada no pescoço** e **barra dourada** embaixo.

## 09/11/2026 — Ícones do inventário em pixel art
- **Blusas:** mangas mais finas e curtas (antes grossas demais em relação ao resto). **Coroa de ouro** redesenhada em pixel art (igual ao estilo dos outros itens; também no anúncio do tesouro).

## 09/11/2026 — Inventário sem legenda
- Tirada a legenda (Arco/Espada/Escudo/Gorro/Roupa/Fadinha) debaixo do personagem no menu; o herói ficou **centralizado** no quadro.

## 09/11/2026 — Aceno antes de dormir
- O herói agora diz **Oi! uma vez só** e o aceno dura 2,2 s (antes 3,5 s com dois Oi). O **braço levantado ficou curto e proporcional** (6 S, antes 10 S).

## 09/11/2026 — Atalhos de teste removidos
- Removidos os atalhos para o nível 6 e 7 e o teste da luta do dragão (menu, `--nivel`, `--dragao`, `?nivel`, `?dragao`).

## 09/11/2026 — Fadinha ajudante
- Depois de conseguir a **roupa de rei**, começa a contagem de **mais 50 monstros** (modo automático). Ao zerar, o herói ganha uma **fadinha**.
- A fadinha voa perto da cabeça do herói e **derrota 20% dos monstros que aparecem** (sorteio no nascimento): voa rápido até o monstro, **gira rapidamente em volta da cabeça dele (um pouco acima)** e ele cai (solta itens como qualquer morte). Vale nos modos automático e jogável; não ataca o dragão nem os monstros da onda final. Fica salva. Teste: `--fada` (desktop) ou `?fada` (web).

## 09/11/2026 — Onda final antes do dragão: bokoblins a cavalo
- No **último nível** (NV 7, modo jogo) aparecem **2 gelatinas e 5 bokoblins montados em cavalos com arco** (nunca mais de 3 vivos ao mesmo tempo). O dragão só chega depois que os 7 forem derrotados (e das 5 mortes do nível).
- Os cavaleiros **se aproximam um pouco e depois rodeiam o herói** atirando flechas; velocidade = **95% da do herói**. A mira é perto do herói mas **sem precisão (~50% das flechas erram)** e a flecha **não muda de rumo** depois de solta. Flecha de frente é bloqueada pelo escudo; tira 1 coração.

## 09/11/2026 — Cabelo com mais contraste e aba Roupas sem armas
- Cabelo do herói agora **castanho** (antes amarelo, parecido com o rosto). Na aba **Roupas** do menu o herói aparece **sem espada, arco nem escudo**.

## 09/11/2026 — Comer maçã pelo inventário e fome do automático
- No menu (Espaço) > **Comida**, clicar na maçã faz o herói **comer na tela ampliada**; **maçã crua recupera meio coração** (assada na fogueira continua recuperando mais). Não gasta a maçã se os corações já estão cheios.
- **Modo automático:** com **só 1 coração** (ou menos) e **nenhum monstro num raio de 4 tamanhos do Link**, ele vai até uma árvore, colhe e **come uma maçã** (+0,5). Se um monstro chega perto no caminho, ele desiste e luta.

## 09/11/2026 — Menu de pausa com equipamento
- **Espaço** abre um menu (estilo Zelda) com as abas **Armas / Roupas / Comida** e o **herói ampliado** ao lado; ao escolher armas ou roupas com o mouse o herói muda na hora. Desktop e web. O equipamento escolhido vale no jogo (aparência e dano) e fica salvo.

## 09/11/2026 — Contagem do baú só depois do conjunto das evoluções
- Os 5000 monstros do baú só começam a contar **depois que as 4 evoluções foram adquiridas** (antes disso a contagem fica em zero).

## 09/11/2026 — Baú e roupas nos dois modos
- O baú, uma vez que aparece, fica disponível no **modo jogável e no automático** (aperte 4). Os anúncios de evolução/tesouro e a aparência do herói valem nos dois modos.

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
