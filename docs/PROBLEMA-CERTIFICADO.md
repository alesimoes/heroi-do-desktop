# Problema: o instalador é bloqueado pelo Windows (falta de certificado)

Registrado em 08/11/2026. Este é o principal problema em aberto de distribuição do jogo.

## Sintoma
Uma pessoa executou `Instalador-HeroiDoDesktop.exe` em outro computador e o Windows disse que o programa "não pode ser seguro" e
**bloqueou**. No computador de desenvolvimento o instalador roda normalmente.

## Causa
Os `.exe` (jogo e instalador) **não têm assinatura digital** (Authenticode). Por isso:
- **SmartScreen** ("O Windows protegeu o computador") desconfia de arquivos sem assinatura e sem reputação; a reputação é por arquivo
  e **recomeça do zero a cada versão nova** quando não há assinatura.
- No **Windows 11 com Smart App Control**, programas sem assinatura/reputação são **bloqueados de vez** (sem a opção "Executar assim mesmo").
- Arquivos vindos da internet, e-mail, WhatsApp ou pendrive de outra pessoa recebem a marca "Mark of the Web", que dispara essas checagens.

Não é vírus: o código é aberto e o programa não usa rede (ver `CODE_SIGNING_POLICY.md`).

## DECISÃO FINAL (08/11/2026): **não vamos assinar o instalador**
O usuário decidiu **encerrar este assunto**: sem certificado e sem pedir assinatura gratuita. O instalador continua **sem assinatura**; quem for bloqueado usa o
guia [`COMO-INSTALAR.txt`](COMO-INSTALAR.txt) ou joga a **versão web**. Os arquivos preparados para o SignPath foram **arquivados** em
[`assinatura-arquivada/`](assinatura-arquivada) (não estão ativos: o workflow virou `.txt` e o repositório não afirma ter assinatura).
Se um dia mudar de ideia, o material está pronto (o SignPath e a Microsoft Store continuam sendo os caminhos gratuitos).

## Restrição do usuário
**Não pagar nada.** Isso exclui certificados comerciais (cerca de US$150–300/ano) e a opção mais barata encontrada (Certum open source, ~€49/ano).

## Opções avaliadas

| Opção | Custo | Quem faz | Situação |
|---|---|---|---|
| **SignPath Foundation** (assinatura gratuita para código aberto) | grátis | autores (cadastro, 2FA, aprovação) | Repositório público e workflows prontos; **falta o cadastro/pedido**. Aprovação não é automática |
| **Microsoft Store** (conta de desenvolvedor individual agora gratuita) | grátis | autores (conta + documento e selfie); empacotamento MSIX pelo desenvolvimento | **Não iniciado.** Falta o Windows SDK (`makeappx.exe`; não instalado) e confirmar se a revisão aceita um programa de confiança total |
| **winget** | grátis | desenvolvimento | Não resolve sozinho: relatos de SmartScreen barrando instaladores sem assinatura mesmo pelo winget |
| Assinar por conta própria (auto-assinado) | grátis | — | **Não serve**: o certificado não é confiável em outro computador |
| Azure Artifact Signing (Microsoft) | pago | — | Provavelmente indisponível para pessoa física no Brasil |
| Certificado comercial / Certum open source | US$150–300 / ~€49 por ano | autores | **Descartado** (não pagar) |
| **Versão web** | grátis | — | **Funciona hoje**, sem instalar e sem aviso: https://heroi-games-2026.web.app |

## O que já foi feito
- Guia para quem recebe o instalador: [`COMO-INSTALAR.txt`](COMO-INSTALAR.txt) (Mais informações > Executar assim mesmo; Desbloquear nas Propriedades; Smart App Control).
- Repositório público no GitHub com licença MIT: https://github.com/alesimoes/heroi-do-desktop.
- `CODE_SIGNING_POLICY.md` (frase obrigatória do SignPath, privacidade, papéis).
- Build automático (`.github/workflows/build.yml`, **testado e passando**) e fluxo de assinatura (`.github/workflows/assinar.yml`, **não testado**; só roda depois de configurado).
- Passo a passo e texto do pedido: [`ASSINATURA-SIGNPATH.md`](ASSINATURA-SIGNPATH.md); configuração de artefato em `signpath-artifact-configuration.xml`.

## O que faltaria (ARQUIVADO: não será feito)
1. **Autores:** ativar 2FA no GitHub; pedir a aprovação em signpath.org usando o texto de `ASSINATURA-SIGNPATH.md`; ativar 2FA no SignPath.
2. Quando aprovado: criar as variáveis `SIGNPATH_ORGANIZATION_ID`, `SIGNPATH_PROJECT_SLUG`, `SIGNPATH_POLICY_SLUG` e o segredo `SIGNPATH_API_TOKEN`
   no GitHub (o token é colocado pelos autores, não por ferramentas); preencher as contas em `CODE_SIGNING_POLICY.md`; rodar o fluxo **Assinar**;
   conferir a assinatura (Propriedades > Assinaturas digitais) e publicar o instalador assinado em **Releases**.
3. **Em paralelo (opcional):** Microsoft Store — instalar o Windows SDK, empacotar em MSIX, cadastro gratuito de desenvolvedor (autores) e envio.
4. Enquanto nada disso acontece: distribuir o instalador **com** o `COMO-INSTALAR.txt` e indicar a **versão web** como alternativa.

## Limites do que pode ser feito por ferramentas automáticas
Criar contas, digitar senhas/tokens e passar por verificação de identidade (2FA, documento e selfie) **precisam ser feitos pelos autores**,
mesmo com autorização. O desenvolvimento pode preparar arquivos, workflows, pacotes e documentação, e rodar os fluxos depois de configurados.

## Fontes
- SignPath para código aberto: https://signpath.org/ · https://about.signpath.io/product/open-source
- Reputação do SmartScreen: https://learn.microsoft.com/windows/apps/package-and-deploy/smartscreen-reputation
- Opções de assinatura: https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/code-signing-options
- Cadastro gratuito na Microsoft Store (pessoa física): https://blogs.windows.com/windowsdeveloper/2025/09/10/free-developer-registration-for-individual-developers-on-microsoft-store/
- Certum open source (referência de preço): https://shop.certum.eu/open-source-code-signing-on-simplysign.html
