# Status do projeto (08/11/2026, fim da noite)

Ponto em que o projeto está, para retomar de onde parou. **Comece por aqui.**

## Contexto em poucas linhas
- **O que é:** herói pixel art (estilo Link, arte própria) que vive na área de trabalho do Windows, atrás dos aplicativos, luta, colhe maçãs,
  assa lanche, dorme, evolui e tem um modo jogo com Dragão. Existe uma versão web com as mesmas regras.
- **Quem:** Ideia Alexandre · Criação Bia Krepel · Programação Claude. Fala-se **português do Brasil**.
- **Como foi feito:** C# 5 (compilador do próprio Windows, sem instalar nada) e HTML+JS puro; testes por reflexão/simulação (ver `ARQUITETURA.md`).
- **Preferências do usuário:** visual da v1 (arte pixel do desktop); web igual ao desktop; herói só atrás dos apps; **não pagar nada**;
  só publicar (sem ranking/Firestore). Detalhes em `DECISOES-E-PREFERENCIAS.md`.

## Entregas prontas e testadas

| Entrega | Situação |
|---|---|
| Programa do Windows (`HeroiPet.cs`) | Compila. Última build: `installer/saida/HeroiPet.exe` (22:12) |
| Instalador (`Instalador-HeroiDoDesktop.exe`) | Gerado às 22:12, ~124 KB, **sem assinatura digital** → o Windows pode bloquear (ver `PROBLEMA-CERTIFICADO.md`) |
| Versão web (`web/heroi-do-desktop.html`) | No ar: https://heroi-games-2026.web.app (Firebase) e artifact privado; inclui botões de toque com **ícones** (espada, arco, maçã) |
| Repositório público | https://github.com/alesimoes/heroi-do-desktop (MIT); o build automático passa |
| Documentação | `docs/` (este arquivo, arquitetura, regras, decisões, próximos passos, changelog, certificado) + `README.md` |

## Funcionalidades (desktop e web, mesmas regras)
- Herói automático (luta, desvia, colhe maçãs, assa lanche, dorme) e **modo jogo** com Dragão (chefão).
- Monstros: octorok, morcego, **gelatina**, **bokoblin**. Itens: corações e rupias (5 tipos).
- **Poder:** 20 gelatinas derrotadas → 15 s de fúria (invulnerável, órbita, flechas de poder, 18x monstros, depois colhe 3 maçãs).
- **Evolução:** 1000 monstros → arco de metal; +500 gorro roxo; +500 blusa azul; +1000 Espada Mestre e escudo vermelho. O item aparece grande,
  **o jogo pausa** até apertar **USAR ITEM**, depois diminui e voa até o herói.
- Tamanho **Pequeno / Normal / Grande** (cenário acompanha). Sombras translúcidas. **Nada do jogo fica por cima de outros aplicativos.**
- Web: controles de toque (celular deitado), tela cheia ao clicar em Jogar/Deixar rodando, menu com Sobre.
- Persistência (desktop): registro `HKCU\Software\HeroiDoDesktop` (`Recorde`, `Tamanho`, `Monstros`) + `%APPDATA%\HeroiDoDesktop\recorde.txt`.
  Web: `localStorage` (`heroi_recorde`, `heroi_tamanho`, `heroi_monstros`).

Detalhes: `REGRAS-DO-JOGO.md` (números) e `ARQUITETURA.md` (código).

## Certificado / instalador bloqueado — DECIDIDO: não vamos assinar
Instalador sem assinatura é bloqueado em alguns computadores. **O usuário decidiu não fazer certificado nem pedir assinatura** (não paga nada).
Solução adotada: `COMO-INSTALAR.txt` e a **versão web**. Histórico completo em `PROBLEMA-CERTIFICADO.md`; o material do SignPath está arquivado em
`assinatura-arquivada/`.

## Outras pendências
1. **Correções da revisão em paralelo** ainda **não aplicadas**: lanche automático no modo jogo no desktop (deveria ser só automático, como na web), tela
   inicial cortada e rotação/redimensionamento da web no celular (herói pode travar), tela cheia no Android, direcional desacelera fora do círculo,
   macieiras sob os botões de toque, jogo roda atrás das telas de aviso na web, botão "Tentar de novo" coberto após Win+D, pausa longa da web (40% sem a
   trava de "duas seguidas"). Itens maiores: redesenhar só o que mudou; trocar de monitor/resolução.
2. **Recorde zerando acima de 70:** relatado, **não reproduzido** no desktop. Hoje o recorde nem aparece na tela. Se voltar, descobrir se é web ou desktop.
3. **Celular:** PWA instalável ou app Android nativo (ver `PROXIMOS-PASSOS.md`).
4. **Pendrive** (`D:\HeroiDoDesktop`): quando conectado, receber o instalador mais novo e `COMO-INSTALAR.txt`. Última cópia com sucesso: instalador das 22:12.
5. Ícones das armas também no programa do Windows (hoje só na web).

## Como verificar rapidamente
```bat
installer\construir-instalador.bat
HeroiPet.exe --poder        :: dispara o poder logo
HeroiPet.exe --evolucao     :: o próximo monstro dá a 1a evolução
```
Web: abrir com `?evolucao` ou `?touch` na URL. Testes automáticos usam reflexão e `HEROI_TESTE=1` (não lê nem grava recorde, tamanho e contagem de monstros reais).
Publicar a web: copiar `web/heroi-do-desktop.html` para `firebase/public/index.html` e `firebase deploy --only hosting --project heroi-games-2026`
(ver `COMO-CONSTRUIR-E-PUBLICAR.md`).
