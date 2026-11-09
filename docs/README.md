# Herói do Desktop (HeroiPet)

Um herói em pixel art que **vive na área de trabalho do Windows**: anda sozinho, luta contra monstros, desvia
de projéteis, colhe maçãs, assa o próprio lanche, dorme de vez em quando e, no **modo jogo**, o jogador assume o
controle e enfrenta um **Dragão** no final. Existe também uma **versão web** do mesmo jogo.

- **Ideia:** Alexandre
- **Criação:** Bia Krepel
- **Programação:** Claude (Anthropic)

## O que existe hoje (estado em 08/11/2026)

| Entrega | Onde | Observação |
|---|---|---|
| Programa do Windows (`HeroiPet.exe`) | `HeroiPet.cs` → `installer/saida/HeroiPet.exe` | Uma janela transparente sobre a área de trabalho, atrás dos aplicativos |
| Instalador do Windows | `installer/saida/Instalador-HeroiDoDesktop.exe` | Instala por usuário, sem administrador, com desinstalador |
| Versão web | `web/heroi-do-desktop.html` | Uma página só (canvas + JS), com cenário elaborado |
| Web publicada (Firebase Hosting) | https://heroi-games-2026.web.app | Projeto Firebase `heroi-games-2026` (nome "games"), plano gratuito |

Versões **descartadas** pelo usuário (não retomar sem pedir): v2 (gráfico "melhorado" com EPX), v3 (vetorial estilo
Hollow Knight) e a web em 16 bits (parecida demais com um jogo conhecido). Fica a arte pixel da **v1**, que é a do desktop.

## Mapa de documentos

- `STATUS.md`: **ponto em que o projeto está** (entregas, pendências, como verificar). Comece por aqui.
- `PROBLEMA-CERTIFICADO.md`: por que o instalador é bloqueado, opções gratuitas avaliadas e o que falta.
- `ASSINATURA-SIGNPATH.md`: passo a passo e texto do pedido de assinatura gratuita.
- `COMO-INSTALAR.txt`: instalação e o que fazer quando o Windows bloqueia o instalador.
- `ARQUITETURA.md`: como o código do desktop e da web é organizado.
- `REGRAS-DO-JOGO.md`: todas as regras e números do jogo (IA, pausas, lanche, modo jogo, chefão).
- `COMO-CONSTRUIR-E-PUBLICAR.md`: comandos para compilar, gerar o instalador, publicar e testar.
- `DECISOES-E-PREFERENCIAS.md`: o que o usuário pediu, escolheu e rejeitou (e o porquê).
- `PROXIMOS-PASSOS.md`: ideias, pendências e limitações conhecidas.
- `CHANGELOG.md`: histórico resumido de mudanças.

## Estrutura de pastas

```
HeroiPet.cs                  código-fonte do programa do Windows (arquivo único, C# 5)
HeroiPet.exe                 build de desenvolvimento (sem ícone)
build.bat                    compila só o HeroiPet.exe
installer/
  Instalador.cs              instalador/desinstalador (WinForms), embute o HeroiPet.exe
  InfoDoPrograma.cs          atributos de versão do HeroiPet.exe
  app.manifest               asInvoker (sem pedir administrador)
  heroi.ico                  ícone gerado do sprite do herói
  construir-instalador.bat   reconstrói exe + instalador
  saida/                     HeroiPet.exe e Instalador-HeroiDoDesktop.exe finais
web/
  heroi-do-desktop.html      versão web (a oficial)
  heroi-do-desktop-16bit.html  versão descartada
firebase/
  firebase.json  .firebaserc public/index.html   (cópia da página web que vai ao ar)
docs/                        esta documentação
README.md  LICENSE  CODE_SIGNING_POLICY.md  .gitignore  .github/workflows/build.yml   (preparação do repositório aberto)
```

## Começo rápido

- Rodar o programa: dê dois cliques em `HeroiPet.exe` (ou instale com o instalador).
- Ícone do herói na bandeja do Windows (perto do relógio): botão direito abre o menu (modo, pausa, Sobre, Sair).
- Detalhes de build e publicação em `COMO-CONSTRUIR-E-PUBLICAR.md`.
