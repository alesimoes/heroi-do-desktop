# Política de assinatura de código

Free code signing provided by [SignPath.io](https://signpath.io), certificate by [SignPath Foundation](https://signpath.org).

> Esta política vale a partir do momento em que o projeto for aprovado no programa SignPath Foundation para projetos de código aberto.
> Até lá, os instaladores publicados **não** são assinados.

## O que é assinado

Somente binários **compilados a partir do código deste repositório**, pelo fluxo automático do GitHub Actions
([`.github/workflows/build.yml`](.github/workflows/build.yml)):

- `HeroiPet.exe` (o jogo)
- `Instalador-HeroiDoDesktop.exe` (o instalador)

Nenhum componente proprietário é incluído. O jogo usa apenas o .NET Framework do próprio Windows.

## Papéis

| Papel | Quem |
|---|---|
| Autores / mantenedores (editam o código) | Alexandre e Bia Krepel (contas do GitHub: _a preencher_) |
| Revisores de commits e releases | _a preencher_ |
| Aprovadores de assinatura | _a preencher_ |

Todos os membros da equipe usam **verificação em dois passos** no GitHub e no SignPath.

## Privacidade

Este programa **não envia nenhuma informação** para a internet. Ele não coleta dados pessoais, não tem telemetria e funciona
totalmente offline. O que ele grava no computador do usuário (em `HKCU\Software\HeroiDoDesktop` e `%APPDATA%\HeroiDoDesktop`)
é apenas o tamanho escolhido, a pontuação e o progresso das evoluções.

## Como verificar

A assinatura aparece em Propriedades do arquivo > Assinaturas digitais. O editor será "SignPath Foundation".
Os mesmos binários podem ser reproduzidos compilando o código com `installer\construir-instalador.bat`.
