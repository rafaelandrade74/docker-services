# Keycloak

Container do Keycloak, isolado e independente dos demais serviços deste repositório.

## Objetivo

Este compose sobe **apenas o Keycloak**. Ele não inclui, gerencia ou depende de nenhum container de banco de dados.

O PostgreSQL utilizado pelo Keycloak deve **existir previamente**, seja em outro serviço deste repositório, em outro host, ou gerenciado externamente.

## Requisitos

- Docker 24+
- Docker Compose v2+
- Um banco PostgreSQL já existente e acessível pela rede
- Banco de dados criado previamente (ex.: `keycloak`)
- Usuário PostgreSQL com permissões adequadas sobre esse banco

> Este serviço não cria o banco, não cria o usuário e não gerencia backups do PostgreSQL. Essas responsabilidades pertencem ao serviço de banco externo.

## Configuração

Copie o arquivo de exemplo e preencha as variáveis:

```bash
cp .env.example .env
```

### Administração

| Variável                   | Descrição                              | Padrão |
|-----------------------------|-----------------------------------------|--------|
| `KEYCLOAK_ADMIN`            | Usuário administrador inicial          | —      |
| `KEYCLOAK_ADMIN_PASSWORD`   | Senha do administrador inicial         | —      |

> Essas credenciais só têm efeito na primeira inicialização do banco (bootstrap do realm `master`). Altere a senha pelo painel administrativo assim que possível.
>
> A partir do Keycloak 26, essas variáveis geram um aviso de depreciação em favor de `KC_BOOTSTRAP_ADMIN_USERNAME`/`KC_BOOTSTRAP_ADMIN_PASSWORD`, mas continuam funcionais.

### Banco de dados (PostgreSQL externo)

| Variável              | Descrição                                  | Padrão  |
|-----------------------|---------------------------------------------|---------|
| `KC_DB_URL_HOST`       | Host/endereço do PostgreSQL existente       | —       |
| `KC_DB_URL_PORT`       | Porta do PostgreSQL                         | `5432`  |
| `KC_DB_URL_DATABASE`   | Nome do banco de dados do Keycloak          | —       |
| `KC_DB_USERNAME`       | Usuário do PostgreSQL                       | —       |
| `KC_DB_PASSWORD`       | Senha do usuário do PostgreSQL              | —       |

O driver de banco (`KC_DB=postgres`) já está fixo no `docker-compose.yml`, pois este serviço é exclusivo para PostgreSQL.

### Hostname e proxy reverso

| Variável            | Descrição                                                          | Padrão       |
|---------------------|----------------------------------------------------------------------|--------------|
| `KC_HOSTNAME`        | Hostname público pelo qual o Keycloak será acessado                 | —            |
| `KC_HOSTNAME_STRICT` | Se `true`, rejeita requisições que não usem o hostname configurado  | `false`      |
| `KC_HTTP_ENABLED`    | Habilita HTTP interno (TLS é responsabilidade do proxy reverso)     | `true`       |
| `KC_PROXY_HEADERS`   | Formato dos headers de proxy aceitos (`xforwarded` ou `forwarded`)  | `xforwarded` |
| `KC_HTTP_PORT`       | Porta exposta no host para o Keycloak                               | `8080`       |

### Outras variáveis

| Variável       | Descrição                  | Padrão              |
|----------------|-----------------------------|----------------------|
| `KC_LOG_LEVEL` | Nível de log do Keycloak   | `info`               |
| `TZ`           | Fuso horário do container  | `America/Sao_Paulo`  |

## Inicialização

```bash
cp .env.example .env
docker compose up -d
```

O Keycloak sobe em modo `start` (produção), servindo HTTP internamente para ser consumido por um proxy reverso com TLS.

## Atualização

```bash
docker compose pull
docker compose up -d
```

Sempre revise as notas de versão do Keycloak antes de atualizar em produção — pode haver migrações de schema no banco.

## Backup

Este serviço não possui volume local para dados de aplicação. Todas as informações de usuários, realms, clients e configurações ficam armazenadas exclusivamente no PostgreSQL externo.

O backup e a restauração devem ser realizados no serviço de banco de dados (dump/restore do PostgreSQL), e não neste diretório.

A pasta `themes/` é o único volume local deste serviço e contém apenas temas customizados de interface (opcional).

## Reverse Proxy

O Keycloak deve ficar atrás de um proxy reverso responsável pelo TLS/HTTPS. O container expõe apenas HTTP internamente.

### Nginx (exemplo)

```nginx
server {
    listen 443 ssl;
    server_name auth.example.com;

    location / {
        proxy_pass http://127.0.0.1:8080;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

### Traefik (exemplo de labels)

```yaml
labels:
  - "traefik.enable=true"
  - "traefik.http.routers.keycloak.rule=Host(`auth.example.com`)"
  - "traefik.http.routers.keycloak.entrypoints=websecure"
  - "traefik.http.routers.keycloak.tls.certresolver=letsencrypt"
  - "traefik.http.services.keycloak.loadbalancer.server.port=8080"
```

### Por que isso importa

- `KC_HOSTNAME`: garante que o Keycloak gere URLs (login, tokens, redirects) usando o domínio público correto, mesmo estando por trás do proxy.
- `KC_PROXY_HEADERS=xforwarded`: instrui o Keycloak a confiar nos headers `X-Forwarded-For` e `X-Forwarded-Proto` enviados pelo proxy, em vez da conexão TCP direta (que é HTTP interno).
- Sem essa configuração correta, o Keycloak pode gerar links com `http://` em vez de `https://`, ou rejeitar requisições por hostname divergente.

## Produção

- Utilize sempre HTTPS, terminado no proxy reverso (Nginx, Traefik, etc.).
- Utilize um banco PostgreSQL dedicado ao Keycloak, não compartilhado com outras aplicações.
- Altere a senha administrativa padrão imediatamente após o primeiro acesso.
- Nunca utilize `admin`/`admin` ou qualquer credencial padrão em produção.
- Mantenha a imagem do Keycloak atualizada, acompanhando avisos de segurança.
- Realize backups periódicos do banco PostgreSQL externo — é onde todo o estado do Keycloak reside.
- Considere `KC_HOSTNAME_STRICT=true` em produção, para evitar ataques de host header.

## Estrutura das pastas

```text
keycloak/
├── docker-compose.yml   # Definição do serviço
├── .env.example         # Exemplo de variáveis de ambiente
├── README.md            # Esta documentação
└── themes/              # Temas customizados (opcional)
```

Não há volume de dados local: todo o estado persistente do Keycloak vive no PostgreSQL externo.
