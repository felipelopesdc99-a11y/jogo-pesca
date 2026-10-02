# Como mandar o jogo para amigos testarem

Grátis, sem servidor e sem hospedagem paga. Cada amigo joga no próprio computador (Windows).

## 1. Gerar a versão (uma vez por versão)

1. Na primeira vez: no **Unity Hub → Instalações → engrenagem da versão 6000.3 → Adicionar módulos**,
   marque **Windows Build Support (Mono)** e instale (é gratuito).
2. Abra o projeto no Unity e use o menu **Fishing Idle → Gerar versão para amigos (Windows)**.
3. Em alguns minutos a pasta `dist` abre com o arquivo `FishingIdle_<versão>.zip`. Dentro dele vai o
   jogo e um `LEIA-ME.txt` explicando para o amigo como jogar.

## 2. Mandar para os amigos

Escolha um:

- **Google Drive ou WeTransfer** (mais simples): envie o zip e mande o link.
- **itch.io** (mais organizado, também grátis): crie uma conta, "Upload new project", tipo
  "Downloadable", envie o zip e deixe a página como **Restricted** (só quem tem o link ou a senha
  entra). Cada versão nova é só enviar outro zip na mesma página.

## 3. O que o amigo faz

Extrai o zip e abre `FishingIdle.exe`. Na primeira vez o Windows pode mostrar "O Windows protegeu o
computador": é só clicar em **Mais informações → Executar assim mesmo** (acontece porque o jogo não
tem assinatura digital, que é paga; não é necessária para teste).

## Bom saber

- O progresso de cada amigo fica salvo no computador dele. Uma versão nova continua o progresso.
- Ranking e comércio de Conchas e Dólares ainda não são online: cada um vê só a si mesmo.
- Mac e celular precisam de outra versão; por enquanto é só Windows.
- Lançar na **Steam** custa US$ 100 por jogo (taxa única da Steam Direct). Não é preciso agora: o
  itch.io serve para testes e até para um lançamento inicial gratuito.
