# Herói do Desktop (HeroiPet)

Um herói em pixel art que **vive na área de trabalho do Windows**: anda sozinho, luta contra monstros, desvia de projéteis,
colhe maçãs, assa o próprio lanche, dorme de vez em quando e evolui com o tempo. No **modo jogo** você assume o controle e
enfrenta um **Dragão** no final. Existe também uma **versão web** do mesmo jogo.

- **Jogar na web (sem instalar):** https://heroi-games-2026.web.app
- **Programa do Windows:** baixe o instalador na página de [Releases](../../releases)

> Ideia: **Alexandre** · Criação: **Bia Krepel** · Programação: **Claude** (Anthropic)

## O que tem

- Herói que fica **atrás dos aplicativos**, só na área de trabalho (janela transparente, cliques passam direto).
- Monstros (octorok, morcego, gelatina, bokoblin), itens, macieiras, fogueira com maçãs assadas, pausas com soneca.
- **Poder**: ao derrotar 20 gelatinas o herói entra em fúria por 15 s.
- **Evolução**: arco de metal (1000 monstros), gorro roxo, blusa azul, Espada Mestre e escudo vermelho.
- **Modo jogo** com mouse/teclado, dificuldade por rupias e chefão Dragão.
- Tamanho **Pequeno / Normal / Grande** (o cenário acompanha).
- Versão web com controles de toque para celular (deitado).

Todos os desenhos são pixel art **próprios**, gerados por código (tabelas de letras e formas), sem imagens de terceiros.

## Download

- **Versão web (recomendada, sem instalar nada):** https://heroi-games-2026.web.app
- **Programa do Windows:** instalador na página de [Releases](https://github.com/alesimoes/heroi-do-desktop/releases) (ou compile você mesmo, abaixo).

**Assinatura:** o instalador **não é assinado digitalmente** (certificados custam dinheiro e esta é uma obra gratuita), então o Windows pode avisar que o
aplicativo não é reconhecido. Veja [`docs/COMO-INSTALAR.txt`](docs/COMO-INSTALAR.txt) para o que fazer — ou jogue a **versão web**, que não precisa de instalação.

## Como compilar (Windows)

Não precisa instalar nada: o compilador C# já vem no Windows (.NET Framework 4).

```bat
installer\construir-instalador.bat
```

Gera `installer\saida\HeroiPet.exe` e `installer\saida\Instalador-HeroiDoDesktop.exe`.
Mais detalhes (testes, publicação da web) em [`docs/COMO-CONSTRUIR-E-PUBLICAR.md`](docs/COMO-CONSTRUIR-E-PUBLICAR.md).

## Documentação

| Arquivo | Assunto |
|---|---|
| [`docs/STATUS.md`](docs/STATUS.md) | **estado atual do projeto** e pendências |
| [`docs/PROBLEMA-CERTIFICADO.md`](docs/PROBLEMA-CERTIFICADO.md) | por que o Windows bloqueia o instalador e como resolver de graça |
| [`docs/ARQUITETURA.md`](docs/ARQUITETURA.md) | como o código é organizado |
| [`docs/REGRAS-DO-JOGO.md`](docs/REGRAS-DO-JOGO.md) | todas as regras e números |
| [`docs/COMO-CONSTRUIR-E-PUBLICAR.md`](docs/COMO-CONSTRUIR-E-PUBLICAR.md) | build, testes e publicação |
| [`docs/COMO-INSTALAR.txt`](docs/COMO-INSTALAR.txt) | instalação e avisos do Windows |
| [`docs/DECISOES-E-PREFERENCIAS.md`](docs/DECISOES-E-PREFERENCIAS.md) | decisões de design |
| [`docs/PROXIMOS-PASSOS.md`](docs/PROXIMOS-PASSOS.md) | ideias e pendências |
| [`docs/CHANGELOG.md`](docs/CHANGELOG.md) | histórico |

## Assinatura do instalador

Não há assinatura digital (decisão do projeto: não pagar por certificado). O histórico das opções avaliadas está em [`docs/PROBLEMA-CERTIFICADO.md`](docs/PROBLEMA-CERTIFICADO.md); os arquivos preparados para o SignPath ficam arquivados em [`docs/assinatura-arquivada`](docs/assinatura-arquivada).

## Licença

[MIT](LICENSE) — © 2026 Alexandre e Bia Krepel.

Os nomes de monstros e itens inspirados em jogos conhecidos são apenas homenagens; este é um projeto de fãs, sem fins lucrativos
e sem vínculo com os detentores desses jogos.
