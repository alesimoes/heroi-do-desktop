# Como construir, testar e publicar

Tudo em **PowerShell**, a partir da pasta do projeto. Compilador: `csc.exe` do .NET Framework 4 (já no Windows).

```powershell
$csc = "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
```

## Programa do Windows
```powershell
Stop-Process -Name HeroiPet -Force -ErrorAction SilentlyContinue   # fecha o que estiver rodando
& $csc /nologo /target:winexe /out:HeroiPet.exe /r:System.Windows.Forms.dll /r:System.Drawing.dll HeroiPet.cs
Start-Process .\HeroiPet.exe
```
Atalhos de teste: `HeroiPet.exe --teste` (pausa longa logo), `--lanche`, `--jogar`, `--chefao`, `--poder`, `--evolucao`.
Nos testes automáticos defina `$env:HEROI_TESTE='1'` antes de criar o `PetForm` (não lê nem grava recorde, tamanho e contagem de monstros reais).

## Instalador (executável final)
```powershell
.\installer\construir-instalador.bat
# ou manualmente:
& $csc /nologo /target:winexe /optimize+ /out:installer\saida\HeroiPet.exe /win32icon:installer\heroi.ico `
   /r:System.Windows.Forms.dll /r:System.Drawing.dll HeroiPet.cs installer\InfoDoPrograma.cs
& $csc /nologo /target:winexe /optimize+ /out:installer\saida\Instalador-HeroiDoDesktop.exe /win32icon:installer\heroi.ico `
   /win32manifest:installer\app.manifest /resource:installer\saida\HeroiPet.exe,HeroiPet.exe `
   /r:System.Windows.Forms.dll /r:System.Drawing.dll installer\Instalador.cs
```
Teste do instalador: `Instalador-HeroiDoDesktop.exe /S /dir=%TEMP%\HeroiTeste`, depois
`%TEMP%\HeroiTeste\Desinstalar.exe /uninstall /S`. (Cria e remove atalhos/registro reais do usuário.)
O arquivo não tem assinatura digital: o SmartScreen avisa ("Mais informações > Executar assim mesmo").

## Versão web
Arquivo único `web/heroi-do-desktop.html`. Para testar localmente sem instalar nada, um servidor mínimo em PowerShell
(`HttpListener` em `http://localhost:8123/`) e abrir no navegador. Dica: `window.__jogo` dá acesso a `hero`, `enemies`,
`step(dt)`, `render()`, `st` (estado), `set({mx,my,mouse,k:{1,2,3}})` para simular entradas.

### Firebase Hosting (já criado)
- Projeto **`heroi-games-2026`** (nome "games"), plano Spark gratuito, só Hosting (Firestore **não** ativado).
- Ferramentas instaladas: Node.js LTS (via `winget`, por usuário) e `firebase-tools` global (`%APPDATA%\npm`).
  Se `node`/`firebase` não forem achados, ajustar o PATH da sessão:
  `$env:Path=[Environment]::GetEnvironmentVariable('Path','User')+';'+[Environment]::GetEnvironmentVariable('Path','Machine')+";$env:APPDATA\npm"`.
- Login: `firebase login --no-localhost` mostra um link; o usuário entra com a conta Google e informa o código;
  então `firebase login <codigo>`. O login fica salvo no computador. (Use uma conta Google sua; o projeto de hospedagem
  é o `heroi-games-2026`, definido em `firebase/.firebaserc`.)
- Publicar:
```powershell
Copy-Item web\heroi-do-desktop.html firebase\public\index.html -Force
cd firebase; firebase deploy --only hosting --project heroi-games-2026
```
URL: https://heroi-games-2026.web.app

## Testes automatizados (padrão usado no projeto)
Sem framework: compila-se um **harness** junto com o código e usa-se reflexão.
```powershell
& $csc /nologo /target:winexe /main:HeroiPet.Harness /out:teste.exe /r:System.Windows.Forms.dll /r:System.Drawing.dll HeroiPet.cs Harness.cs
```
`Harness.cs` define `namespace HeroiPet { static class Harness { [STAThread] static void Main() {...} } }`, cria
`new PetForm()`, ajusta campos privados por reflexão, chama `Step`, `SetPlayMode`, `OnPaint(new PaintEventArgs(g, rect))`
e imprime OK/FALHA. Para entradas, setar `PetForm.KeyHook`/`CursorHook`/`FgHook`.
Simulação de dano do piloto: loop de `Step(0.03f)` por 120 min simulados lendo `hurtCount`/`killCount`.
**Cuidados aprendidos:** não iniciar o harness com `-WindowStyle Hidden` (o Windows esconde a 1ª janela); `Add-Type` do PowerShell
trata avisos como erro (variável sem uso quebra a compilação).

## Reproduzir o bug "Mostrar área de trabalho"
`keybd_event` com Win+D (VK 0x5B + 0x44), duas vezes (mostra e restaura), e listar a pilha Z com
`GetTopWindow/GetWindow(GW_HWNDNEXT)`; o herói deve ter posição menor que a de `Progman` enquanto a área de trabalho
está na frente.
