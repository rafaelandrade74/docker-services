# Política de leitura para aplicações via AppRole
# Permite apenas leitura de segredos no caminho secret/
path "secret/data/*" {
  capabilities = ["read"]
}

path "secret/metadata/*" {
  capabilities = ["read", "list"]
}
