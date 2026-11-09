# Status do projeto (08/11/2026, noite)

Resumo do ponto em que o projeto está, para retomar de onde parou.

## Entregas prontas e testadas

| Entrega | Situação |
|---|---|
| Programa do Windows (`HeroiPet.cs`) | Compila (C# 5, `csc.exe` do Windows). Última build: `installer/saida/HeroiPet.exe` (22:12) |
| Instalador (`Instalador-HeroiDoDesktop.exe`) | Gerado às 22:12, ~124 KB, **sem assinatura digital** (o Windows pode bloquear; ver `COMO-INSTALAR.txt`) |
| Versão web (`web/heroi-do-desktop.html`) | Publicada em https://heroi-games-2026.web.app (Firebase Hosting) |
| Documentação | Esta pasta `docs/` + `README.md` na raiz |
| Repositório GitHub público | **Preparado, ainda não publicado** (ver "Pendências") |

## Funcionalidades (desktop e web, mesmas regras)

- Herói automático (luta, desvia, colhe maçãs, assa lanche, dorme) e **modo jogo** com Dragão (chefão).
- Monstros: octorok, morcego, **gelatina**, **bokoblin**. Itens: corações e rupias (5 tipos).
- **Poder:** 20 gelatinas derrotadas → 15 s de fúria (invulnerável, órbita, flechas de poder, 18x monstros, depois colhe 3 maçãs).
- **Evolução:** 1000 monstros → arco de metal; +500 gorro roxo; +500 blusa azul; +1000 Espada Mestre e escudo vermelho.
  O item aparece grande no meio da tela, **o jogo pausa** até apertar **USAR ITEM**, depois o item diminui e voa até o herói.
- Tamanho **Pequeno / Normal / Grande** (cenário acompanha).
- Sombras translúcidas no desktop (`ShadowLayer`); **nada do jogo fica por cima de outros aplicativos**.
- Web: controles de toque (celular deitado), tela cheia ao clicar em Jogar/Deixar rodando, menu com Sobre.
- Persistência (desktop): registro `HKCU\Software\HeroiDoDesktop` (`Recorde`, `Tamanho`, `Monstros`) + `%APPDATA%\HeroiDoDesktop\recorde.txt`.
  Web: `localStorage` (`heroi_recorde`, `heroi_tamanho`, `heroi_monstros`).

Detalhes das regras: `REGRAS-DO-JOGO.md`. Como o código funciona: `ARQUITETURA.md`.

## Pendências e decisões em aberto

1. **GitHub + assinatura (SignPath).** O repositório está pronto (`LICENSE` MIT, `README.md`, `CODE_SIGNING_POLICY.md`, `.gitignore`,
   `.github/workflows/build.yml`), mas falta: instalar Git e GitHub CLI, fazer login no GitHub, criar o repositório público
   `heroi-do-desktop` e enviar. Depois, pedir aprovação em signpath.org (feito pelos autores, com verificação em dois passos).
2. **Correções da revisão em paralelo** ainda **não aplicadas** (itens aprovados por outra sessão, sem confirmação neste chat):
   lanche automático no modo jogo no desktop (deveria ser só automático, como na web), tela inicial cortada no celular deitado,
   redimensionar/rotacionar a web trava o herói, tela cheia no Android, direcional desacelera fora do círculo, macieiras sob os
   botões de toque, jogo roda atrás das telas de aviso na web, botão "Tentar de novo" coberto após Win+D, pausa longa da web
   (40% sem a trava de "duas seguidas"), texto contraditório de sorteio em `REGRAS-DO-JOGO.md` (já corrigido). Itens 12 e 13
   (redesenho só quando muda; trocar de monitor/resolução) são mudanças maiores.
3. **Recorde zerando acima de 70:** relatado pelo usuário, **não reproduzido** no desktop (armazenamento correto). Hoje o recorde
   nem aparece na tela (só alimenta rupias de maior valor a partir de 50). Se voltar a acontecer, descobrir se é web ou desktop.
4. **Celular:** PWA instalável ou app Android nativo (ver `PROXIMOS-PASSOS.md`).
5. **Pendrive** (`D:\HeroiDoDesktop`): quando conectado, receber `Instalador-HeroiDoDesktop.exe` e `COMO-INSTALAR.txt`.
   A última cópia com sucesso foi a do instalador das 22:12.

## Como verificar rapidamente

```bat
installer\construir-instalador.bat
HeroiPet.exe --poder        :: dispara o poder logo
HeroiPet.exe --evolucao     :: o próximo monstro dá a 1a evolução
```
Web: abrir com `?evolucao` ou `?touch` na URL. Testes automáticos usam reflexão e a variável de ambiente `HEROI_TESTE=1`
(impede ler/gravar recorde, tamanho e contagem de monstros reais).
