# Docker Services

## Objetivo

Este repositório armazena configurações Docker Compose de serviços utilizados em produção.

Cada serviço é completamente independente.

Não existe dependência entre serviços.

Cada pasta representa um único serviço.

---

# Estrutura

Cada serviço deve possuir sua própria pasta contendo toda a documentação e arquivos necessários.

Exemplo:

/postgres
/redis
/rabbitmq
/nginx

Nunca compartilhar arquivos entre serviços.

---

# Organização

Cada serviço deve possuir, quando aplicável:

- docker-compose.yml
- .env.example
- README.md
- volumes/
- backups/

---

# Obrigações

Antes de qualquer tarefa:

1. Ler completamente este arquivo (CLAUDE.md).
2. Entender a estrutura existente.
3. Manter o padrão do repositório.
4. Nunca modificar outros serviços sem solicitação explícita.
5. Nunca criar dependências entre serviços.
6. Sempre utilizar boas práticas Docker.
7. Utilizar imagens oficiais sempre que possível.
8. Utilizar versões estáveis.
9. Nunca incluir credenciais reais.
10. Sempre documentar qualquer novo serviço criado.
11. Manter READMEs atualizados.
12. Manter arquivos de exemplo (.env.example).
13. Garantir que todos os exemplos funcionem.
14. Revisar toda alteração antes de finalizar.

---

# Docker Compose

Sempre priorizar:

- Compose Specification atual
- restart: unless-stopped
- healthcheck
- volumes persistentes
- redes dedicadas
- variáveis em .env
- nomes claros
- organização simples
- fácil manutenção

---

# Documentação

Todo novo serviço deve possuir documentação suficiente para que qualquer desenvolvedor consiga utilizá-lo sem consultar fontes externas.

---

# Qualidade

Antes de finalizar qualquer atividade:

- validar a estrutura criada
- validar a sintaxe dos arquivos YAML
- revisar documentação
- remover arquivos desnecessários
- garantir consistência com este documento
