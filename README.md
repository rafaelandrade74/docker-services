# docker-services

Repositório centralizado de configurações Docker Compose para serviços utilizados em produção.

## Objetivo

Armazenar e versionar configurações prontas para uso de serviços de infraestrutura, garantindo reprodutibilidade e facilidade de manutenção.

## Padrão utilizado

Cada serviço reside em sua própria pasta e é completamente independente dos demais. Não existe qualquer dependência entre serviços.

## Organização por serviço

Cada pasta representa um único serviço e contém todos os arquivos necessários para executá-lo:

- `docker-compose.yml` — definição do serviço
- `.env.example` — variáveis de ambiente (sem valores reais)
- `README.md` — documentação completa do serviço
- `volumes/` — dados persistentes (mapeados no host)
- `backups/` — dumps e arquivos de backup

## Como adicionar novos serviços

1. Crie uma nova pasta com o nome do serviço (ex.: `redis/`).
2. Adicione `docker-compose.yml`, `.env.example` e `README.md`.
3. Crie as pastas `volumes/` e `backups/` com um `.gitkeep` em cada uma.
4. Documente o serviço seguindo o padrão dos existentes.
5. Nunca compartilhe volumes, redes ou arquivos entre serviços diferentes.

## Convenções adotadas

- Compose Specification atual (sem campo `version` obsoleto)
- `restart: unless-stopped` em todos os serviços
- Healthcheck obrigatório
- Variáveis de ambiente definidas em `.env` (nunca comitadas)
- Redes dedicadas por serviço
- Imagens oficiais em versões estáveis
- Nenhuma credencial real nos arquivos versionados

## Estrutura do projeto

```text
docker-services/
│
├── CLAUDE.md            # Diretrizes para o Claude Code
├── README.md            # Este arquivo
│
└── postgres/
    ├── docker-compose.yml
    ├── .env.example
    ├── README.md
    ├── volumes/
    │   └── .gitkeep
    └── backups/
        └── .gitkeep
```
