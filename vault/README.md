# HashiCorp Vault

Serviço de gerenciamento de segredos utilizando [HashiCorp Vault](https://www.vaultproject.io/) com interface Web habilitada.

---

## Estrutura

```
vault/
├── config/
│   └── vault.hcl              # Configuração principal do Vault
├── policies/
│   ├── admin-policy.hcl       # Política administrativa (acesso total)
│   └── api-read-policy.hcl    # Política de leitura para aplicações via AppRole
├── scripts/
│   └── entrypoint.sh          # Corrige permissões dos volumes e inicia o Vault
├── examples/
│   └── csharp/
│       ├── VaultService.cs    # Serviço de leitura de segredos via AppRole
│       └── Program.cs         # Exemplo de uso
├── volumes/               # Diretório local (referência; dados em volumes nomeados)
├── .env.example           # Variáveis de ambiente de referência
├── docker-compose.yml
└── README.md
```

---

## Requisitos

- Docker
- Docker Compose

---

## Configuração

1. Copie o arquivo de exemplo e ajuste conforme necessário:

```bash
cp .env.example .env
```

2. Edite o `.env` com os valores desejados:

```env
VAULT_PORT=8200
VAULT_LOG_LEVEL=info
VAULT_ADMIN_USERNAME=admin
VAULT_ADMIN_PASSWORD=TroqueEstaSenha
```

> **Atenção:** `VAULT_ADMIN_USERNAME` e `VAULT_ADMIN_PASSWORD` são variáveis de referência para o processo de configuração inicial executado manualmente. O Vault não cria usuários a partir de variáveis de ambiente.

---

## Configuração Inicial

Execute o fluxo completo uma única vez após subir o container pela primeira vez.

### 1. Subir o container

```bash
docker compose up -d
```

### 2. Inicializar o Vault

O Vault começa em estado selado (sealed). A inicialização gera as Unseal Keys e o Root Token.

```bash
docker exec -it vault vault operator init
```

Saída esperada (exemplo):

```
Unseal Key 1: <chave1>
Unseal Key 2: <chave2>
Unseal Key 3: <chave3>
Unseal Key 4: <chave4>
Unseal Key 5: <chave5>

Initial Root Token: hvs.XXXXXXXXXXXXXXXXXXXXXXXX
```

> **Guarde as Unseal Keys e o Root Token em local seguro e offline.** Sem eles não é possível recuperar o acesso ao Vault.

### 3. Executar o Unseal

O Vault requer ao menos 3 das 5 Unseal Keys para ser deselado. Repita o comando três vezes com chaves diferentes:

```bash
docker exec -it vault vault operator unseal <chave1>
docker exec -it vault vault operator unseal <chave2>
docker exec -it vault vault operator unseal <chave3>
```

Verifique o status:

```bash
docker exec -it vault vault status
```

A saída deve indicar `Sealed: false`.

### 4. Login com o Root Token

```bash
docker exec -it vault vault login <root-token>
```

### 5. Habilitar o método de autenticação Userpass

```bash
docker exec -it vault vault auth enable userpass
```

### 6. Criar a política administrativa

O arquivo `policies/admin-policy.hcl` já está disponível dentro do container em `/vault/policies/admin-policy.hcl`.

```bash
docker exec -it vault vault policy write admin-policy /vault/policies/admin-policy.hcl
```

Verifique:

```bash
docker exec -it vault vault policy list
docker exec -it vault vault policy read admin-policy
```

### 7. Criar o usuário administrador

Execute **no host** (não dentro do container). O `source .env` carrega as variáveis antes de passar o comando ao container:

```bash
source vault/.env && docker exec -it vault vault write auth/userpass/users/$VAULT_ADMIN_USERNAME \
  password="$VAULT_ADMIN_PASSWORD" \
  policies=admin-policy
```

> **Atenção:** Se a senha contiver caracteres especiais do shell (`$`, `` ` ``, `!`, `\`), execute o comando diretamente dentro do container usando **aspas simples** para evitar expansão incorreta:
>
> ```bash
> docker exec -it vault sh
> vault write auth/userpass/users/admin password='SuaSenha$Aqui' policies=admin-policy
> ```

### 8. Verificar o usuário criado

```bash
docker exec -it vault vault list auth/userpass/users
```

### 9. Logout do Root Token

```bash
docker exec -it vault vault token revoke -self
```

Após essa etapa, utilize exclusivamente o usuário administrador para operações do dia a dia.

---

## Configuração AppRole para Aplicações

O AppRole permite que aplicações se autentiquem no Vault usando um `role-id` e um `secret-id`, sem precisar de credenciais humanas.

### 1. Habilitar o método AppRole

```bash
docker exec -it vault vault auth enable approle
```

### 2. Criar a política de leitura

```bash
docker exec -it vault vault policy write api-read /vault/policies/api-read-policy.hcl
```

### 3. Criar a role vinculada à política

```bash
docker exec -it vault vault write auth/approle/role/api-read \
  token_policies=api-read \
  token_ttl=1h \
  token_max_ttl=4h \
  secret_id_ttl=0
```

> `secret_id_ttl=0` significa que o Secret ID não expira. Ajuste conforme sua política de segurança.

### 4. Obter o Role ID

```bash
docker exec -it vault vault read auth/approle/role/api-read/role-id
```

Guarde o valor de `role_id`.

### 5. Gerar o Secret ID

```bash
docker exec -it vault vault write -f auth/approle/role/api-read/secret-id
```

Guarde o valor de `secret_id`.

### 6. Criar um segredo de exemplo para testar

```bash
docker exec -it vault vault secrets enable -path=secret kv-v2
docker exec -it vault vault kv put secret/minha-app chave=valor outro=exemplo
```

### 7. Testar o login via AppRole

```bash
docker exec -it vault vault write auth/approle/login \
  role_id=<role-id> \
  secret_id=<secret-id>
```

---

## Exemplo C# — Leitura de Segredos via AppRole

Instale o pacote NuGet:

```bash
dotnet add package VaultSharp
```

Configure as variáveis de ambiente da aplicação:

```env
VAULT_ADDR=http://localhost:8200
VAULT_ROLE_ID=<role-id obtido no passo 4>
VAULT_SECRET_ID=<secret-id obtido no passo 5>
```

Os arquivos de exemplo estão em [`examples/csharp/`](examples/csharp/):

- **`VaultService.cs`** — serviço que autentica via AppRole e lê segredos
- **`Program.cs`** — exemplo de uso

```csharp
var vault = new VaultService(
    vaultAddress: Environment.GetEnvironmentVariable("VAULT_ADDR"),
    roleId:       Environment.GetEnvironmentVariable("VAULT_ROLE_ID"),
    secretId:     Environment.GetEnvironmentVariable("VAULT_SECRET_ID")
);

var data = await vault.ReadSecretAsync("minha-app");
// data["chave"] == "valor"
```

---

## Acesso pela Interface Web

### URL de acesso

```
http://localhost:8200
```

> Substitua `8200` pelo valor da variável `VAULT_PORT` caso tenha alterado.

### Primeiro acesso

Ao acessar a URL pela primeira vez, o Vault exibirá a tela de inicialização caso não tenha sido inicializado, ou a tela de Unseal caso esteja selado.

Após o Unseal, a tela de login será exibida.

### Processo de login

1. Acesse `http://localhost:8200`.
2. Selecione o método **Username** no campo *Method*.
3. Informe o usuário e a senha configurados na etapa de Configuração Inicial.
4. Clique em **Sign In**.

### Utilização do usuário administrador

O usuário administrador criado com a política `admin-policy` possui acesso completo ao Vault. Após o login, é possível:

- Gerenciar Secrets Engines
- Criar e revogar Tokens
- Gerenciar Policies
- Habilitar e configurar Auth Methods
- Visualizar e rotacionar segredos

### Boas práticas de segurança

- Nunca utilize o Root Token para operações do dia a dia.
- Utilize o usuário administrador para operações administrativas.
- Armazene o Root Token e as Unseal Keys em local seguro e offline.
- Nunca versione tokens, chaves ou credenciais no Git.
- Habilite HTTPS em produção.

---

## Login pela CLI

### Utilizando o usuário administrador

```bash
source vault/.env && docker exec -it vault vault login -method=userpass \
  username="$VAULT_ADMIN_USERNAME" \
  password="$VAULT_ADMIN_PASSWORD"
```

### Verificar o token ativo

```bash
docker exec -it vault vault token lookup
```

### Logout

```bash
docker exec -it vault vault token revoke -self
```

---

## Administração

### Gerenciamento de Secrets Engines

Listar engines habilitadas:

```bash
docker exec -it vault vault secrets list
```

Habilitar uma nova engine (exemplo: KV versão 2):

```bash
docker exec -it vault vault secrets enable -path=secret kv-v2
```

Desabilitar uma engine:

```bash
docker exec -it vault vault secrets disable secret/
```

### Gerenciamento de Policies

Listar políticas:

```bash
docker exec -it vault vault policy list
```

Criar ou atualizar uma política:

```bash
docker exec -it vault vault policy write nome-da-politica /vault/policies/arquivo.hcl
```

Ler uma política:

```bash
docker exec -it vault vault policy read nome-da-politica
```

Excluir uma política:

```bash
docker exec -it vault vault policy delete nome-da-politica
```

### Gerenciamento de Auth Methods

Listar métodos habilitados:

```bash
docker exec -it vault vault auth list
```

Habilitar um novo método:

```bash
docker exec -it vault vault auth enable <método>
```

Desabilitar um método:

```bash
docker exec -it vault vault auth disable <método>/
```

### Gerenciamento de Tokens

Criar um token:

```bash
docker exec -it vault vault token create -policy=nome-da-politica -ttl=1h
```

Revogar um token:

```bash
docker exec -it vault vault token revoke <token>
```

Inspecionar o token ativo:

```bash
docker exec -it vault vault token lookup
```

### Gerenciamento de usuários (Userpass)

Listar usuários:

```bash
docker exec -it vault vault list auth/userpass/users
```

Criar um usuário:

```bash
docker exec -it vault vault write auth/userpass/users/<username> \
  password=<senha> \
  policies=<politica>
```

Atualizar a senha de um usuário:

```bash
docker exec -it vault vault write auth/userpass/users/<username>/password \
  password=<nova-senha>
```

Excluir um usuário:

```bash
docker exec -it vault vault delete auth/userpass/users/<username>
```

### Auditoria

Habilitar o Audit Log para arquivo:

```bash
docker exec -it vault vault audit enable file file_path=/vault/logs/audit.log
```

Listar backends de auditoria:

```bash
docker exec -it vault vault audit list
```

### Rotação de segredos

Rotacionar um segredo no KV:

```bash
docker exec -it vault vault kv put secret/meu-app senha=NovaSenha123
```

Listar versões de um segredo:

```bash
docker exec -it vault vault kv metadata get secret/meu-app
```

---

## Segurança

- Nunca utilize o Root Token no dia a dia. Utilize o usuário administrador.
- Aplique políticas com o menor privilégio necessário (princípio do mínimo privilégio).
- Habilite Audit Logs para rastreabilidade de todas as operações.
- Utilize Auto Unseal (AWS KMS, GCP KMS, Azure Key Vault) em ambientes de produção para evitar intervenção manual.
- Utilize HTTPS com TLS em produção. A configuração atual com `tls_disable = true` é adequada apenas para desenvolvimento.
- Armazene o Root Token e as Unseal Keys em cofre físico ou gerenciador de senhas offline.
- Nunca versione tokens, Unseal Keys ou credenciais no Git.
- Revogue o Root Token após a configuração inicial quando utilizar Auto Unseal.
- Realize rotação periódica de segredos e credenciais.
- Monitore o Audit Log regularmente para detectar acessos não autorizados.

---

## Comandos úteis

| Ação                        | Comando                                                                 |
|-----------------------------|-------------------------------------------------------------------------|
| Subir o serviço             | `docker compose up -d`                                                  |
| Parar o serviço             | `docker compose down`                                                   |
| Ver logs                    | `docker compose logs -f vault`                                          |
| Status do Vault             | `docker exec -it vault vault status`                                    |
| Unseal                      | `docker exec -it vault vault operator unseal <chave>`                   |
| Login (userpass)            | `docker exec -it vault vault login -method=userpass username=<usuario>` |
| Login (token)               | `docker exec -it vault vault login <token>`                             |
| Logout                      | `docker exec -it vault vault token revoke -self`                        |

---

## Volumes

| Volume       | Finalidade                                  |
|--------------|---------------------------------------------|
| `vault-data` | Dados persistentes do Vault (segredos, etc) |
| `vault-logs` | Logs do Vault e Audit Log                   |
