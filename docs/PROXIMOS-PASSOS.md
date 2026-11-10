# Próximos passos, ideias e limitações

Estado atual em [`STATUS.md`](STATUS.md).

## Em andamento / decisões em aberto
1. ~~Certificado / assinatura do instalador~~ — **decidido: não será feito** (ver `PROBLEMA-CERTIFICADO.md`). O repositório já está público
   (https://github.com/alesimoes/heroi-do-desktop). Se mudar de ideia: SignPath e Microsoft Store são os caminhos gratuitos (material em `assinatura-arquivada/`).
   Opcional útil: criar uma **Release** no GitHub com o instalador e o `COMO-INSTALAR.txt`.
2. **Correções da revisão em paralelo** (ver `STATUS.md`, item 2): lanche só no automático no desktop, tela inicial e rotação na
   web/celular, tela cheia no Android, direcional, macieiras sob os botões, jogo pausado atrás de telas na web, botão "Tentar de novo"
   após Win+D, pausa longa da web.
3. **Celular.** Opções:
   - **PWA** (recomendado, fácil): manifesto + ícones + service worker na página do Firebase → "instalar" na tela inicial,
     tela cheia, offline. Não flutua sobre outros apps.
   - **App Android nativo** com o herói passeando **sobre** outros apps (serviço com `SYSTEM_ALERT_WINDOW`): projeto à parte
     (JDK + Android SDK/Gradle). **iPhone não permite.**

## Pendências recentes (09/11/2026)
- Copiar o instalador novo para o **pendrive** (`D:\HeroiDoDesktop`) quando conectado.
- Conferir na tela que o **menu de pausa** fica abaixo dos aplicativos (código pronto, não verificado visualmente).
- Mostrar na tela **quantos monstros faltam** para a próxima evolução / baú / fadinha (hoje só no código).
- Evitar edição simultânea dos mesmos arquivos por duas sessões (ver `STATUS.md`, pendência 4).

## Ideias que apareceram
- Ranking online no **Firestore** (ao vencer o dragão) ou progresso por jogador (login anônimo do Firebase). O projeto
  `heroi-games-2026` já está pronto para isso (só ativar o Firestore).
- Vários monitores (hoje só a tela principal, `Screen.PrimaryScreen.WorkingArea`); reagir à mudança de resolução/monitor.
- Desenhar só o que mudou (hoje a tela cheia é redesenhada a cada 30 ms, também nas sombras).
- Sons e música; mais monstros/chefões; outras fases; mais cenários na web (estações do ano, noite).
- Atualização automática do programa; mais evoluções, mais ajudantes além da fadinha, mais inimigos montados.
- Dividir `PetForm` (arquivo grande) em partes (entrada, IA, desenho, poder, evolução) e criar testes automáticos versionados
  (hoje são scripts de reflexão descritos em `COMO-CONSTRUIR-E-PUBLICAR.md`).

## Limitações conhecidas
- Modo jogo no desktop usa teclas/mouse **globais**: digitar 1/2/3 em outro programa também dispara ações (por isso o
  espaço só vale com a área de trabalho em foco). Clicar fora do jogo faz o herói andar até o clique.
- O herói não passa "atrás" dos ícones da área de trabalho (fica por cima deles).
- Como nada do jogo fica por cima de aplicativos, avisos/botões (aviso das maçãs, "Tentar de novo", "USAR ITEM") só são vistos com a
  área de trabalho visível; o jogo pausa nesses momentos (morte e evolução).
- Instalador **não assinado**: SmartScreen / Smart App Control podem bloquear (ver `COMO-INSTALAR.txt`).
- A web mistura "dpr" e escala inteira `S`; em telas muito estreitas o cenário é cortado (sem rolagem lateral).
- O recorde da web fica no navegador (`localStorage`); em contextos sem armazenamento ele se perde ao recarregar.

## Como retomar o trabalho
1. Leia `STATUS.md`, `README.md` e `ARQUITETURA.md`.
2. `HeroiPet.cs` é a fonte de verdade das regras; `REGRAS-DO-JOGO.md` resume os números.
3. Para qualquer mudança de regra, aplicar **nas duas versões** (desktop C# e web JS), testar com harness, reconstruir o instalador
   e republicar a web (Firebase).
