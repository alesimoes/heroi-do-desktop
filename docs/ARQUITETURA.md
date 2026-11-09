# Arquitetura

## 1. Programa do Windows (`HeroiPet.cs`)

Arquivo único, **C# 5** (compilado pelo `csc.exe` que já vem no Windows, .NET Framework 4). Sem pacotes externos.
Isso importa: **não use recursos de C# 6+** (interpolação de string, `?.`, funções locais, `out var`...). Parâmetros
opcionais (C# 4) funcionam.

### Classes principais (namespace `HeroiPet`)

| Classe | Papel |
|---|---|
| `Program` | `Main`: exclusão mútua (uma instância só), `SetProcessDPIAware`, abre `PetForm` |
| `Sprite`, `Art` | Sprites como **tabelas de letras** + paleta (`Art.Pal`). `Art.Make(rows, escala, sobrescrita)` gera os bitmaps (direita e espelhado). Geradores por código: `TreeRows()` (árvore), `DragonRows(bool boca)` (dragão), `AppleSm`, etc. |
| `Enemy`, `Rock`, `Arrow`, `Tree`, `Item`, `Puff`, `Smoke`, `FlyApple`, `CookApple`, `Fire` | Entidades do jogo |
| `PetForm : Form` | **Quase tudo**: janela, entrada, lógica, desenho, menu da bandeja |
| `Toast` | Aviso (não rouba foco, clique atravessa). Fica **atrás dos aplicativos**, como o herói |
| `EndShow` | Tela "FIM DE JOGO" + créditos (lista `Lines`) |
| `DeathForm` | "VOCÊ MORREU" + botão "Tentar de novo" (esta recebe cliques) |
| `EvoForm` | Botão **USAR ITEM** da evolução (janela pequena que recebe clique) |
| `ClickShield` | Janela quase invisível (opacidade 1%) que engole os cliques na área de trabalho no modo jogo |
| `ShadowLayer` | Janela de tela cheia com alfa por pixel só para as sombras |
| `ZOrder` | Auxiliar: `ZOrder.Bottom(hwnd)` põe uma janela no fundo (aviso, morte, créditos, evolução) |
| `EvoAnim` | Estado da animação de evolução (`Stage`, `T`, `Wait`) |

### Janela do herói

- `PetForm` ocupa a **área útil** da tela principal, sem borda, com `TransparencyKey` magenta (pixel magenta = transparente
  e clique atravessa) + `WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE`.
- Fica **atrás dos aplicativos**: a cada ~180 ms é empurrada para o fundo (`HWND_BOTTOM`).
- **"Mostrar área de trabalho" (Win+D):** a área de trabalho (`Progman`) sobe e cobriria o herói. `DesktopShown()` percorre
  as janelas de topo (ignorando as sempre-por-cima, minimizadas, "cloaked" e as do próprio processo); se a primeira
  janela encontrada é `Progman`/`WorkerW`, o herói vira `HWND_TOPMOST` só enquanto isso. `HWND_TOP` **não** funcionou
  (o Windows não deixa passar à frente da área de trabalho em foco). Ao restaurar um app, volta ao fundo.
- Desenho: `OnPaint` desenha tudo num `Graphics` com sprites inteiros (sem suavização), cor magenta como fundo.
- DPI: o processo é DPI-aware; escala `S = max(3, round(alturaDaTela / 360))`. **Todas as medidas do jogo são
  múltiplos de `S`** (1 "pixel" de sprite = `S` pixels da tela).

### Sombras (`ShadowLayer`)
A janela do herói usa cor-chave (só liga/desliga), então não tem sombra translúcida. As sombras são coletadas em
`shadowList` durante `PaintWorld` e desenhadas por `ShadowLayer`: outra janela de tela cheia com **alfa por pixel**
(`UpdateLayeredWindow` + DIB), posta logo **abaixo** da janela do herói (`SetWindowPos(shadow, hero.Handle, ...)` no `Tick`).
Ajuste da opacidade: cor do `brush` em `ShadowLayer` (alfa 125).

### Camadas de janelas (de cima para baixo, tudo atrás dos aplicativos)
aviso/morte/créditos/evolução → janela do herói (cor-chave) → `ShadowLayer` → `ClickShield` (modo jogo). Só o herói (e a camada
de sombras) sobe a `HWND_TOPMOST`, e apenas quando "Mostrar área de trabalho" (Win+D) deixa a área de trabalho em primeiro plano.

### Tamanho (Pequeno / Normal / Grande)
`S = SizeToS(sizeMode)`: pequeno ×0,67, normal ×1, grande ×1,34 sobre `baseS` (altura da tela / 360). `SetSizeMode` reconstrói os sprites
(`BuildSprites`) e as fontes; posições não mudam. Salvo em `HKCU\Software\HeroiDoDesktop\Tamanho`. Na web: `sizeMode` em `resize()`.

### Poder, evolução e persistência
- **Poder** (`powerT`, `StartPower/EndPower/UpdatePowerHero`): disparado por `powerPending` (20 gelatinas, `slimeKills`/`SlimesForPower`).
  Efeitos em `EnemyCap`, `SpawnRateMul`, `UpdateEnemies` (todos vão direto no herói a 2x), `HurtHero` (invulnerável).
  `lootHold` faz o herói pegar o que caiu antes do lanche; `EndPower` colhe 3 maçãs.
- **Evolução** (`CountKill`, `UpdateEvo`, `DrawEvo`, `DrawEvoItem`, `UseEvoItem`): `evoKills` salvo em `Monstros`; limites em `EvoThreshold`.
  Durante `evo.Wait` o `Tick` não chama `Step` (jogo pausado) e aparece o `EvoForm`. O equipamento muda por paleta
  (`Art.HeroRecolor` separa gorro `H` e roupa `Q`; `hov` em `BuildSprites`) e por cores em `ShieldColor/BladeColor/GuardColor/GripColor/TunicColor`.
- **Recorde** (`record`, `LoadRecord/SaveRecord`): nunca diminui; registro + `%APPDATA%\HeroiDoDesktop\recorde.txt`. Não aparece na tela.
  A partir de 50 libera rupias de maior valor (`RupeeKind`).
- Com `HEROI_TESTE` definido, nada disso lê/grava dados reais.

### Ciclo do jogo

`Tick()` (timer ~30 ms) → reposiciona a janela → `Step(dt)` (se não pausado e sem evolução esperando) → `Invalidate()`.
`Step`: pausas/ciclo calmo → spawn de monstros → `UpdateHero` → nível por rupias (modo jogo) → `UpdateBoss` → lanche
→ inimigos, pedras, flechas, itens → maçãs voando/árvores → partículas.

### Herói (`UpdateHero`)

Ordem de prioridade: lanche (`cookState`) → chefão fases 2/3 → pausa calma (`calmState==2`) → morto → ataque em curso
→ **modo jogo** (`PlayerControl`) ou **piloto automático** (`AutoControl`).

- **Piloto automático** (`AutoControl`, `Retreat`, `AimAndShoot`, `TryDodge`): cuidadoso, constantes `TuneXxx`. Calibrado
  por simulação para o herói tomar **metade** dos danos do comportamento antigo (ver `REGRAS-DO-JOGO.md`).
- **Modo jogo** (`PlayerControl`): lê mouse/teclado **globalmente** (`GetAsyncKeyState`, `Cursor.Position`) porque a janela
  deixa os cliques passarem. `ClickShield` impede que a área de trabalho receba os cliques; **Ctrl** deixa passar.
  `Espaço` pausa (só vale com a área de trabalho em foco).

### Estados e máquinas

- **Pausa calma** (`calmState` 0/1/2, `phase` 0–5): 0 anda até a macieira, 1 colhe, 2 baixa espada/escudo, 3 acena
  (só pausa curta), 4 agacha, 5 dorme.
- **Lanche** (`cookState` 1–7): vai ao meio, acende fogueira, joga 5 maçãs (3 embaixo, 2 em cima), 10 s assando,
  pega (de cima para baixo), come (cada uma cura 1), apaga.
- **Chefão** (`bossPhase` 0–5): 0 normal; 1 fogueira de cura; 2 herói se afasta; 3 explosão+fumaça+dragão nasce;
  4 luta; 5 vitória → `EndShow` → volta ao automático.
- **Morte (modo jogo):** `deathWait` congela o jogo e mostra `DeathForm`; `DoRetry` cobra 10 rupias.

### Ganchos para teste automatizado

Campos estáticos `KeyHook` (`Func<int,bool>`), `CursorHook` (`Func<Point>`), `FgHook` (`Func<bool>`) substituem
entradas reais. Contadores `hurtCount`, `killCount`. Argumentos de linha de comando de teste: `--teste` (pausa longa
rápida), `--lanche` (começa com 5 maçãs), `--jogar` (liga o modo jogo), `--chefao` (pula para a fase da fogueira),
`--poder` (dispara o poder), `--evolucao` (o próximo monstro dá a 1ª evolução). Variável de ambiente `HEROI_TESTE=1` isola os testes dos dados reais.
Os testes usam **reflexão** para chamar métodos privados (`Step`, `SetPlayMode`, `OnPaint`...).

### Outros

- Iniciar com o Windows: chave `HKCU\...\Run` valor `HeroiPet` (item de menu).
- Salva (registro `HKCU\Software\HeroiDoDesktop`): `Recorde`, `Tamanho`, `Monstros`; reserva do recorde em `%APPDATA%\HeroiDoDesktop\recorde.txt`.

## 2. Instalador (`installer/`)

`Instalador.cs` é um WinForms que embute `HeroiPet.exe` como recurso. Instala em
`%LOCALAPPDATA%\Programs\Heroi do Desktop`, cria atalhos (Menu Iniciar, opcional área de trabalho), registra em
"Aplicativos instalados" (`HKCU\...\Uninstall\HeroiDoDesktop`) e copia a si mesmo como `Desinstalar.exe`.
Modos: assistente, `/S` (silencioso), `/uninstall [/S]`, `/dir=caminho`. O manifesto `asInvoker` evita o UAC
automático que o Windows aplicaria a executáveis chamados "Setup/Install".

## 3. Versão web (`web/heroi-do-desktop.html`)

Arquivo único (HTML+CSS+JS, sem bibliotecas). **Porte direto da lógica do desktop** para JavaScript:
mesmas regras, mesmos números e mesma arte (as mesmas tabelas de letras). Diferenças: fundo é um campo com cenário
(lagoa, trilha, casinha, floresta; `buildBackground`, `PROPS`, `drawAmbient`), mouse/teclado/toque normais, menu HTML,
avisos/morte/créditos/Sobre em HTML. `window.__jogo` expõe ganchos de teste (estado, `step`, `render`...).
Escala: `S = max(3, round(altura/300))` em pixels do canvas (considera `devicePixelRatio`), multiplicada pelo tamanho escolhido.
Também na web: controles de toque (`#dpad`, `#touch`), tela cheia (`goFullscreen`), poder, evolução (botão `#evoBtn` e pausa em `evoWaiting()`),
`buildSprites()` recria o objeto `SP` inteiro (chame `buildProps()` junto). Parâmetros de teste na URL: `?touch`, `?evolucao`.

## 4. Publicação web

- **Firebase Hosting** (projeto `heroi-games-2026`, nome "games"): `firebase/public/index.html` é cópia da página.
