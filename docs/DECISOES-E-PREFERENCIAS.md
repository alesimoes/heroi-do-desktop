# Decisões e preferências (histórico do que o usuário pediu)

O usuário fala **português do Brasil**. Prefere respostas diretas, em linguagem simples, e que se **verifique antes de
dizer que está pronto**. Pede mudanças pequenas e específicas em sequência; costuma dar ideias de regras de jogo.

## Origem
"Um personagem que fica na área de trabalho, parecido com o Link do Zelda, andando e lutando contra inimigos."
Arte própria inspirada no gênero (não usa sprites/imagens da Nintendo). O usuário chama o herói de "Link".

## Decisões de produto (em ordem de importância)
- **Atrás dos aplicativos, só na área de trabalho.** **Nada** do jogo fica por cima de outros aplicativos: nem o aviso (Toast),
  nem "VOCÊ MORREU", nem os créditos (antes o aviso podia; mudou a pedido em 08/11/2026). Essas janelas ficam no fundo
  (`ZOrder.Bottom`), logo acima do herói. Única exceção técnica: o herói sobe (topmost) só quando a área de trabalho está
  em primeiro plano ("Mostrar área de trabalho", Win+D), pois não há aplicativo a cobrir.
- **Estilo visual:** a **arte pixel da v1** (herói de lado, 14×16 px de sprite) é "a legal". Descartadas: v2 (EPX), v3
  (vetorial estilo Hollow Knight, que o usuário pediu mas depois preferiu a v1) e a web 16 bits estilo Zelda
  ("ficou uma cópia do Zelda"). **A web deve ser igual ao desktop**, só com cenário de fundo mais elaborado.
- **Ícones da área de trabalho** não precisam ser alterados (herói fica sobre eles, não atrás).
- **Piloto automático cuidadoso:** tomar **metade** dos danos sem os inimigos baterem menos (medido por simulação).
- **Pausas:** pausa de 30 s a 2 min a cada 2–5 min; 40% de chance de ser a longa com colheita; sem acenar na longa.
- **Lanche:** com 5 maçãs, aviso por cima de tudo, fogueira no **meio da tela**, maçãs empilhadas 3+2, 10 s para assar.
- **Modo jogo** (escolhido pelo menu, "jogar" ou "deixar rodando"): controles mouse + 1/2/3, espaço pausa, sem acenar/dormir,
  dificuldade por rupias (+10% de monstros a cada 25, até +60%), corações extras a partir de +40%, rupias maiores
  (5–50), octoroks 10% menos frequentes, tela "VOCÊ MORREU" com "Tentar de novo" (−10 rupias).
- **Ao entrar no modo jogo:** herói vai ao meio, monstros somem, rupias e maçãs do automático zeram. Ao voltar ao
  automático: perde corações extras e rupias.
- **Chefão (Dragão):** 1000 → **2500 de vida**, maior, anda, bolas teleguiadas com 10% de vida; flecha 50 / espada 150.
  "FIM DE JOGO" e créditos ao vencer.
- **Créditos / Sobre:** **Ideia: Alexandre; Criação: Bia Krepel; Programação: Claude** (menu "Sobre" no desktop e na web).
- **Publicar sem pontos:** só hospedar o jogo (Firebase Hosting), **sem ranking/Firestore** por enquanto.
- **Celular:** o usuário perguntou se o herói roda no celular como no desktop. Resposta dada: sobreposição na tela
  inicial exige app nativo de **Android** (permissão "exibir sobre outros apps"); iPhone não permite. A web já roda no celular
  com botões de toque 1/2/3 e pode virar PWA instalável. **Aguardando o usuário escolher** (ver `PROXIMOS-PASSOS.md`).

## Decisões recentes (08/11/2026, noite)
- **Poder** passou a ser lançado por **20 gelatinas derrotadas** (não por recorde); o herói **não fica azul**; monstros 18x; 5x mais projéteis;
  só 3 maçãs depois; itens soltos somem normalmente em 15 s; RECORDE some da tela; kills durante o poder não contam gelatinas.
- **Evolução** do herói por monstros derrotados no automático, com item grande no meio da tela e **jogo pausado até apertar USAR ITEM**.
- **Tamanho** Pequeno/Normal/Grande com o cenário acompanhando.
- **Nada se sobrepõe a outros aplicativos** (inclusive aviso, morte e créditos).
- **Instalador bloqueado em outro PC:** escolhido o caminho da **assinatura gratuita SignPath** para projeto de **código aberto no GitHub**
  (licença MIT, repositório público). O usuário prefere publicar o código a pagar certificado.
- Web abre em **tela cheia** ao clicar em Jogar / Deixar rodando.
- **Não pagar nada** e, por fim, **não assinar o instalador** (decisão de 08/11/2026: sem certificado e sem pedir o SignPath). Quem for bloqueado usa o guia
  ou a versão web. Repositório **público no GitHub** (`alesimoes/heroi-do-desktop`, MIT). Ver `PROBLEMA-CERTIFICADO.md`.
- Botões de toque da web com **ícones das armas** (espada, arco, maçã) em vez de texto; acompanham as evoluções.

## Decisões recentes (09/11/2026)
- **Regra de ouro:** toda mudança vale no **desktop e na web**; depois de cada mudança: testar, recompilar, **reinstalar**, publicar (Firebase + artifact), commit/push e atualizar docs.
- **Nada sobre outros aplicativos:** inclui o menu de pausa e o aviso NOVO ITEM (ficam na camada do herói, abaixo dos apps).
- **Baú do Rei** só conta depois das 4 evoluções; **fadinha** só conta depois da roupa de rei; **aparência rosa** depois de mais 500 monstros. O progresso é **por computador** (não vai no instalador).
- **Fadinha:** derrota 20% dos monstros; pronta a cada 30 s (pisca amarela); automático ataca sozinha, no jogo só com a tecla 4; brilhinhos azuis pequenos e transparentes.
- **Cavaleiros do NV 7:** 2 gelatinas + 5 bokoblins a cavalo com arco (95% da velocidade do herói, ~50% das flechas erram, não corrigem a mira); cavalo **simples e pequeno**, patas que **inclinam** no quadril, ritmo rápido.
- **Inventário:** sem legenda sob o herói, herói centralizado; ícones **em pixel art**; blusas com mangas finas; roupa de rei é **blusa** (não vestido) com faixa dourada no pescoço e na barra; coroa em pixel art; braços da aba Roupas pequenos, sem contorno, manga da cor da camisa.
- **Colher maçã** pelo inventário fecha o menu e colhe 2, sem monstros durante a ação; a manga do braço esticado segue a roupa.
- **Atalhos de teste** de nível 6/7 e da luta do dragão foram **removidos** a pedido.
- Cabelo do herói **loiro dourado** (mais escuro que o rosto); aceno antes de dormir com **um Oi só** e braço curto.
- Sem certificado/assinatura e sem pagar nada (mantido).

## Lições técnicas
- Janela transparente com `TransparencyKey` é **clique-através** nos pixels magenta; por isso o modo jogo lê entrada
  globalmente e usa `ClickShield` (janela com opacidade ≥ ~0,4% recebe cliques; 0% não).
- `HWND_TOP` não passa à frente da área de trabalho em foco; `HWND_TOPMOST` passa (usado só no Win+D).
- Windows 11 24H2: ancorar no `WorkerW` (atrás dos ícones) foi avaliado e **não** foi necessário.
- A web usa só mouse/teclado normais; o espaço pausa só no modo jogo e não quando uma tela de aviso está aberta.
- `Select-String`/PowerShell: preferir ferramentas de edição por âncora única; o repositório **tem git** (publicado no GitHub). No PowerShell 5, nomes como `R`/`Rd` são aliases (`Invoke-History`/`Remove-Item`): **nunca** nomear funções assim (um `Rd` quase apagou os documentos); `Replace` não aceita 3º argumento.
- Dois agentes editando o mesmo arquivo sobrescrevem um ao outro: reler antes de editar e conferir o diff antes de publicar.
