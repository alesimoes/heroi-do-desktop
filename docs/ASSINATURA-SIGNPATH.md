# Assinatura do instalador com o SignPath Foundation (gratuito, código aberto)

Objetivo: o `Instalador-HeroiDoDesktop.exe` sair **assinado**, para o Windows (SmartScreen / Smart App Control) parar de bloquear.
O certificado é da **SignPath Foundation** (o "editor" que aparece no Windows será "SignPath Foundation").

> **Quem faz o quê.** O cadastro no SignPath, a verificação em dois passos e a autorização do GitHub envolvem a identidade de vocês
> e **precisam ser feitos pelos autores** (uma ferramenta automática não pode criar contas nem digitar senhas/tokens por vocês).
> Tudo o que dá para preparar no repositório já está pronto.

## O que já está pronto no repositório
- Repositório público: https://github.com/alesimoes/heroi-do-desktop (licença **MIT**, que o programa exige).
- `CODE_SIGNING_POLICY.md` com a frase obrigatória *"Free code signing provided by SignPath.io, certificate by SignPath Foundation"*
  e a declaração de privacidade (o programa não envia nada pela internet).
- `.github/workflows/build.yml`: compila o jogo e o instalador a cada envio e guarda os `.exe` como *artifact* (**já testado: passou**).
- `.github/workflows/assinar.yml`: envia o artifact para o SignPath e baixa o instalador assinado. **Só roda depois de configurado**
  (ver abaixo). Este workflow **não foi testado**, porque depende da conta do SignPath.

## Passo a passo (vocês)
1. **Ative a verificação em dois passos** na conta do GitHub (Configurações > Senha e autenticação).
2. Em https://signpath.org/ abra o formulário de pedido para projetos de código aberto ("Apply") e use o texto abaixo.
3. Entre no SignPath com a conta do GitHub (autorize o aplicativo) e ative também a verificação em dois passos lá.
4. Aguarde a aprovação (não é automática; pode pedir mais informações).
5. Quando aprovado, no SignPath anote: **Organization ID**, **Project slug**, **Signing policy slug** (normalmente `release-signing`) e crie um
   **API token** para o CI.
6. No GitHub (repositório > Settings > Secrets and variables > Actions):
   - *Variables* (não secretas): `SIGNPATH_ORGANIZATION_ID`, `SIGNPATH_PROJECT_SLUG`, `SIGNPATH_POLICY_SLUG`
   - *Secret*: `SIGNPATH_API_TOKEN`
7. No SignPath, configure a *artifact configuration* para assinar os dois `.exe` (Authenticode) — exemplo em
   `docs/signpath-artifact-configuration.xml`.
8. Preencha no `CODE_SIGNING_POLICY.md` as contas do GitHub de quem mantém/aprova.
9. Rode o workflow **"Assinar"** (Actions > Assinar > Run workflow). O instalador assinado fica no *artifact* `HeroiDoDesktop-assinado`
   e pode ser anexado a uma *Release*.

## Texto sugerido para o formulário do SignPath
**Projeto:** Herói do Desktop (HeroiPet)
**Repositório:** https://github.com/alesimoes/heroi-do-desktop
**Licença:** MIT
**Descrição:** Jogo de fãs, gratuito e sem fins lucrativos: um herói em pixel art que vive na área de trabalho do Windows (C#/WinForms,
compilado com o csc do .NET Framework) e uma versão web. Todo o código e toda a arte são próprios e estão no repositório. O programa não
usa rede, não coleta dados e não tem componentes proprietários.
**O que será assinado:** `HeroiPet.exe` e `Instalador-HeroiDoDesktop.exe`, compilados pelo GitHub Actions a partir do repositório.
**Por que assinar:** o instalador é bloqueado pelo SmartScreen/Smart App Control em outros computadores por não ter assinatura.
**Política de assinatura:** `CODE_SIGNING_POLICY.md` no repositório.

## Alternativa paga (se o SignPath não aprovar)
Certificado de assinatura para pessoa física (validação individual) de uma autoridade como SSL.com ou Sectigo, em torno de US$150–300 por ano
(confirmar no site de cada uma). Com ele o editor que aparece é o nome do autor. O certificado fica em token USB ou nuvem do fornecedor;
a assinatura é feita com `signtool`. A reputação do SmartScreen ainda leva um tempo para se formar, mesmo com assinatura.
