# Redis

## Objetivo

Serviço Redis para uso em produção como solução de armazenamento em memória de alta performance.

## Recursos

- **Cache** — armazenamento temporário de dados para reduzir carga em bancos de dados e APIs
- **Session Storage** — armazenamento de sessões de usuários com TTL configurável
- **Filas** — gerenciamento de filas de tarefas com estruturas de lista e sorted sets
- **Pub/Sub** — comunicação assíncrona entre serviços via canais de mensagens
- **Rate Limiting** — controle de taxa de requisições com contadores atômicos e expiração

## Estrutura

```text
redis/
├── docker-compose.yml   # Definição do serviço
├── .env.example         # Variáveis de ambiente (modelo)
├── redis.conf           # Configuração do Redis
├── README.md            # Esta documentação
├── data/                # Dados persistentes (volume)
│   └── .gitkeep
└── backups/             # Diretório para backups manuais
    └── .gitkeep
```

## Configuração

Copie o arquivo de exemplo e ajuste os valores:

```bash
cp .env.example .env
```

### Variáveis de ambiente

| Variável                | Padrão              | Descrição                                                              |
|-------------------------|---------------------|------------------------------------------------------------------------|
| `REDIS_PORT`            | `6379`              | Porta exposta do Redis no host                                         |
| `REDIS_PASSWORD`        | —                   | Senha de autenticação. **Obrigatório alterar antes de usar**           |
| `REDIS_MAXMEMORY`       | `512mb`             | Limite máximo de memória. Ajuste conforme o servidor disponível        |
| `REDIS_MAXMEMORY_POLICY`| `allkeys-lru`       | Política de remoção quando o limite é atingido (ver abaixo)            |
| `TZ`                    | `America/Sao_Paulo` | Fuso horário do container                                              |

### Políticas de remoção (`maxmemory-policy`)

| Política          | Comportamento                                                    |
|-------------------|------------------------------------------------------------------|
| `allkeys-lru`     | Remove qualquer chave menos recentemente usada (recomendado para cache geral) |
| `volatile-lru`    | Remove apenas chaves com TTL, usando LRU                         |
| `allkeys-lfu`     | Remove qualquer chave menos frequentemente usada                 |
| `volatile-ttl`    | Remove chaves com TTL menor primeiro                             |
| `noeviction`      | Retorna erro quando a memória está cheia (recomendado para filas) |

## Inicialização

```bash
cp .env.example .env
# Edite o .env com os valores corretos
docker compose up -d
```

Verifique os logs:

```bash
docker compose logs -f
```

## Teste da conexão

```bash
redis-cli -h localhost -p 6379 -a <senha> ping
```

Informações detalhadas do servidor:

```bash
redis-cli -h localhost -p 6379 -a <senha> info
```

Informações de memória:

```bash
redis-cli -h localhost -p 6379 -a <senha> info memory
```

## Persistência

Este serviço utiliza estratégia de persistência dupla:

- **RDB (snapshots)** — gera snapshots periódicos em `dump.rdb`:
  - A cada 900 s se houve ao menos 1 alteração
  - A cada 300 s se houve ao menos 10 alterações
  - A cada 60 s se houve ao menos 10.000 alterações
- **AOF (Append-Only File)** — registra cada operação de escrita em `appendonly.aof`, com fsync a cada segundo

Os arquivos ficam armazenados no volume `redis-data` mapeado para `/data` dentro do container.

## Backup

### Backup manual

```bash
# Forçar gravação do snapshot imediatamente
docker exec redis redis-cli -a <senha> BGSAVE

# Copiar os arquivos de dados para o diretório de backups
docker cp redis:/data/dump.rdb ./backups/dump-$(date +%Y%m%d%H%M%S).rdb
docker cp redis:/data/appendonly.aof ./backups/appendonly-$(date +%Y%m%d%H%M%S).aof
```

### Restauração

```bash
# Parar o container
docker compose down

# Substituir os arquivos de dados
docker run --rm -v redis_redis-data:/data -v $(pwd)/backups:/backup alpine \
  cp /backup/dump.rdb /data/dump.rdb

# Reiniciar o container
docker compose up -d
```

## Atualização da imagem

```bash
# Baixar a nova imagem
docker compose pull

# Recriar o container preservando os dados
docker compose up -d
```

Os dados persistem porque estão em um volume nomeado (`redis-data`), independente do container.

## Produção — Recomendações de segurança

- **Nunca deixar sem senha** — defina `REDIS_PASSWORD` com um valor forte (mínimo 32 caracteres aleatórios)
- **Não expor para a Internet** — o Redis não foi projetado para exposição direta; use sempre uma rede privada ou VPN
- **Utilize firewall** — restrinja acesso à porta `6379` somente para os IPs/serviços autorizados
- **Utilize rede privada** — conecte outros serviços via rede Docker interna em vez de expor a porta do host
- **Monitore o uso de memória** — acompanhe `used_memory` e `maxmemory` para evitar evicção inesperada
- **Realize backups periódicos** — automatize cópias do `dump.rdb` e `appendonly.aof`
- **Mantenha a imagem atualizada** — acompanhe as versões estáveis do Redis para receber patches de segurança
- **Revise a política de evicção** — use `noeviction` para filas onde a perda de dados é inaceitável
