# Status do projeto (09/11/2026, noite)

Ponto em que o projeto está, para retomar de onde parou. **Comece por aqui.**

## Contexto em poucas linhas
- **O que é:** herói pixel art (estilo Link, arte própria) que vive na área de trabalho do Windows, **atrás dos aplicativos**: luta, colhe maçãs,
  assa lanche, dorme, evolui, ganha roupa de rei e uma **fadinha ajudante**, tem **inventário** (Espaço) e um **modo jogo** com cavaleiros e o Dragão.
  Existe uma versão web com as mesmas regras.
- **Quem:** Ideia Alexandre · Criação Bia Krepel · Programação Claude. Fala-se **português do Brasil**.
- **Como foi feito:** C# 5 (compilador do próprio Windows, sem instalar nada) e HTML+JS puro; testes por reflexão/simulação (ver `ARQUITETURA.md`).
- **Preferências do usuário:** visual da v1 (arte pixel do desktop); **toda mudança de regra vale no desktop E na web**; herói só atrás dos apps; **não pagar nada**;
  só publicar (sem ranking/Firestore). Detalhes em `DECISOES-E-PREFERENCIAS.md`.

## Entregas prontas (última build: 09/11/2026 21:49)

| Entrega | Situação |
|---|---|
| Programa do Windows (`HeroiPet.cs`, ~4650 linhas) | Compila. `installer/saida/HeroiPet.exe` (151.552 bytes) e **instalado** em `%LOCALAPPDATA%\Programs\Heroi do Desktop` |
| Instalador (`Instalador-HeroiDoDesktop.exe`) | 173.056 bytes, **na Área de Trabalho** do usuário. **Sem assinatura digital** (decidido; ver `PROBLEMA-CERTIFICADO.md`) |
| Versão web (`web/heroi-do-desktop.html`, ~1780 linhas) | No ar: https://heroi-games-2026.web.app (Firebase) e artifact privado https://claude.ai/artifact/TZ8SpAMbBCkKQHQKxt2YaN |
| Repositório público | https://github.com/alesimoes/heroi-do-desktop (MIT), `main` em dia (último commit de código: `e652e00`) |
| Documentação | `docs/` (este arquivo, arquitetura, regras, decisões, próximos passos, changelog, certificado) + `README.md` |

## O que o jogo tem hoje (desktop e web, mesmas regras; números em `REGRAS-DO-JOGO.md`)
- **Herói automático:** luta (espada/arco), desvia, colhe maçãs, assa lanche (5+ maçãs), dorme (acena **uma vez** antes), **come maçã crua** quando está com 1 coração
  e sem monstros num raio de 4 Links.
- **Monstros:** octorok (200 de vida), morcego (50), gelatina (50), bokoblin (100), **bokoblin a cavalo com arco** (250), **Dragão** (5000).
  Arco dá 50 de dano (×2 prateado), espada 150 (×2 Espada Mestre), flecha do poder azul 9.999.999.
- **Poder:** 20 gelatinas → 15 s de fúria (invulnerável, 18× monstros, flechas azuis, colhe 3 maçãs depois).
- **Evoluções** (monstros no automático): 1000 arco de metal · +500 gorro roxo · +500 blusa azul · +1000 Espada Mestre e escudo vermelho. Item grande, **jogo pausa até USAR ITEM**.
- **Baú do Rei:** 5000 octoroks+bokoblins **depois** das 4 evoluções → tecla 4 → coroa de ouro, roupa de rei, calça marrom, sapatos roxos e escudo roxo.
- **Fadinha:** +50 monstros depois da roupa de rei. Derrota **20%** dos monstros que nascem; a cada **30 s** fica pronta (pisca amarela): no automático ataca sozinha,
  no jogo só com a tecla **4**. +500 monstros → aparência **rosa** (escolhida no inventário). Brilhinhos azuis.
- **Inventário (Espaço):** abas Armas / Roupas / Comida, herói ampliado centralizado (sem legenda), troca de equipamento na hora, aparência da fadinha;
  Comida: **Comer maçã** (+0,5 coração, animação) e **Colher maçã** (fecha o menu; o herói vai à macieira e colhe **2**; **sem monstros enquanto isso**; manga da cor da roupa).
  No desktop o menu fica na camada do herói (abaixo dos apps).
- **Modo jogo:** níveis NV 1–7 por rupias (25 cada); **NV 7 = onda final** (2 gelatinas + 5 cavaleiros, no máx. 3 vivos) → fogueira de cura → Dragão (fúria, onda de choque) → 1000 rupias → "ESPERE POR MAIS ATUALIZAÇÕES" → créditos.
- **Aviso NOVO ITEM** (pequeno, canto inferior direito, 5 s) para item/arma/roupa/ajudante.
- Tamanho Pequeno/Normal/Grande; sombras translúcidas; **nada fica por cima de outros aplicativos**; web com toque (celular deitado) e tela cheia.
- **Atalhos de teste removidos** (nível 6/7 e luta do dragão). Ainda existem `--teste --lanche --jogar --chefao --poder --evolucao --bau --fada --fada2` (desktop) e `?evolucao ?bau ?fada ?fada2 ?touch` (web).

## Onde fica o progresso de cada pessoa (não vai no instalador)
- Desktop: `HKCU\Software\HeroiDoDesktop` → `Recorde, Tamanho, Monstros, MonstrosBau, Bau, MonstrosFada, Fada, MonstrosFada2, FadaSkin, FadaLook, Equip` (+ `%APPDATA%\HeroiDoDesktop\recorde.txt`).
- Web: `localStorage` → `heroi_recorde, heroi_tamanho, heroi_monstros, heroi_monstros_bau, heroi_bau, heroi_monstros_fada, heroi_fada, heroi_monstros_fada2, heroi_fada_skin, heroi_fada_look, heroi_equip`.
- Quem instalar o jogo em outro computador **começa do zero** (ex.: tio precisa conquistar tudo). Neste computador: Bau=2 (roupa de rei), Fada em contagem.

## Certificado / instalador bloqueado — DECIDIDO: não vamos assinar
Instalador sem assinatura pode ser bloqueado. O usuário decidiu **não fazer certificado nem pagar nada**. Solução: `COMO-INSTALAR.txt` e a versão web.
Histórico em `PROBLEMA-CERTIFICADO.md`; material do SignPath em `assinatura-arquivada/`.

## Pendências
1. **Pendrive** (`D:\HeroiDoDesktop`): quando conectado, copiar o instalador mais novo e `COMO-INSTALAR.txt` (não estava conectado nesta sessão).
2. **Correções da revisão em paralelo** ainda não aplicadas (ver `PROXIMOS-PASSOS.md`): lanche automático no modo jogo no desktop, tela inicial/rotação/tela cheia no celular,
   direcional, macieiras sob botões de toque, jogo atrás das telas de aviso na web, "Tentar de novo" coberto após Win+D, pausa longa da web.
3. **Menu de pausa em camada de fundo:** implementado (`PushLayers`), mas **a ordem das janelas não foi verificada na tela** (teste automático bloqueado). Se o menu
   aparecer na frente de algum aplicativo, reportar qual.
4. **Edição simultânea:** duas sessões do Claude editaram os mesmos arquivos ao mesmo tempo; já houve trechos revertidos/removidos por engano (atalhos do dragão, cavalo antigo na web).
   Antes de publicar, conferir `git diff` e rodar a verificação de funcionalidades (checklist em `ARQUITETURA.md`, seção "Verificação rápida").
5. Ícones das armas também no programa do Windows (hoje só na web); aviso de contagem (quantos monstros faltam) não existe na tela.
6. Celular: PWA instalável ou app Android (ver `PROXIMOS-PASSOS.md`).

## Como verificar rapidamente
```powershell
installer\construir-instalador.bat          # recompila exe + instalador
HeroiPet.exe --fada                          # já com a fadinha
HeroiPet.exe --poder                         # dispara o poder logo
HeroiPet.exe --evolucao                      # o próximo monstro dá a 1a evolução
```
Web: abrir com `?fada`, `?evolucao`, `?bau` ou `?touch`. Testes automáticos usam reflexão e `HEROI_TESTE=1` (não lê nem grava dados reais).
Publicar a web: copiar `web/heroi-do-desktop.html` para `firebase/public/index.html` e `firebase deploy --only hosting --project heroi-games-2026`
(ver `COMO-CONSTRUIR-E-PUBLICAR.md`).
