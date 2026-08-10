# Política administrativa com acesso completo ao Vault
path "*" {
  capabilities = ["create", "read", "update", "delete", "list", "sudo"]
}
