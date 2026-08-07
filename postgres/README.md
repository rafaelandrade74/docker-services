# PostgreSQL

Instância PostgreSQL isolada gerenciada via Docker Compose.

## Requisitos

- Docker 24+
- Docker Compose v2+

## Configuração

Copie o arquivo de exemplo e preencha as variáveis:

```bash
cp .env.example .env
```

Edite o `.env` com os valores desejados:

| Variável            | Descrição                    | Padrão             |
|---------------------|------------------------------|--------------------|
| `POSTGRES_DB`       | Nome do banco de dados       | —                  |
| `POSTGRES_USER`     | Usuário do banco             | —                  |
| `POSTGRES_PASSWORD` | Senha do usuário             | —                  |
| `POSTGRES_PORT`     | Porta exposta no host        | `5432`             |
| `TZ`                | Fuso horário do container    | `America/Sao_Paulo`|

## Iniciar

```bash
docker compose up -d
```

## Parar

```bash
docker compose down
```

## Atualizar a imagem

```bash
docker compose pull
docker compose up -d
```

## Backup

Gera um dump do banco e salva na pasta `backups/`:

```bash
docker compose exec postgres pg_dump -U $POSTGRES_USER $POSTGRES_DB > backups/backup_$(date +%Y%m%d_%H%M%S).sql
```

## Restaurar backup

```bash
docker compose exec -T postgres psql -U $POSTGRES_USER -d $POSTGRES_DB < backups/backup_YYYYMMDD_HHMMSS.sql
```

## Persistência dos dados

Os dados do PostgreSQL são armazenados no volume nomeado `postgres-data`, gerenciado pelo Docker. Remover o container não apaga os dados.

> **Nota:** não é utilizado bind mount (`./volumes/data`) para o diretório de dados do Postgres porque, no Windows, o NTFS não preserva as permissões Unix (`0700`) exigidas pelo `initdb`, o que impede a inicialização do banco. Um volume nomeado é gerenciado dentro do filesystem Linux da engine do Docker e não sofre esse problema.

Para inspecionar ou remover o volume:

```bash
docker volume ls
docker volume rm postgres_postgres-data
```

## Portas

| Porta | Descrição                        |
|-------|----------------------------------|
| `5432`| Porta padrão do PostgreSQL (configurável via `POSTGRES_PORT`) |

## Estrutura das pastas

```text
postgres/
├── docker-compose.yml   # Definição do serviço
├── .env.example         # Exemplo de variáveis de ambiente
├── README.md            # Esta documentação
└── backups/             # Dumps do banco de dados
```

Os dados persistentes ficam no volume nomeado `postgres-data` (fora da árvore de pastas do projeto).
