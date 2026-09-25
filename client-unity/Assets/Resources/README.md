# Assets/Resources

O arquivo `BackendSettings.asset` pertence a esta pasta, mas **não** é versionado: o cliente roda
sem ele, usando `http://localhost:5080` como padrão, que é o endereço servido pelo ambiente Docker
Compose local.

Para apontar o cliente a outro servidor, escolha o que for mais conveniente:

1. **Variável de ambiente** — defina `FISHING_IDLE_API_BASE_URL` antes de abrir o editor ou o
   executável. Ela vence sobre o arquivo.
2. **Arquivo de configuração** — no editor, use *Assets > Create > Fishing Idle > Backend Settings*,
   salve aqui como `BackendSettings.asset` e edite os campos no inspetor.

O arquivo fica de fora do controle de versão de propósito: o endereço do servidor de cada
desenvolvedor é uma questão local, e a referência ao script dentro dele (um GUID) é gerada pelo
editor, não escrita à mão.
