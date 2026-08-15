// Exemplo de uso do VaultService com AppRole
// Configure as variáveis de ambiente antes de rodar:
//   VAULT_ADDR      = http://localhost:8200
//   VAULT_ROLE_ID   = <role-id gerado no Vault>
//   VAULT_SECRET_ID = <secret-id gerado no Vault>

var vaultAddr = Environment.GetEnvironmentVariable("VAULT_ADDR")
    ?? throw new InvalidOperationException("VAULT_ADDR não definido");

var roleId = Environment.GetEnvironmentVariable("VAULT_ROLE_ID")
    ?? throw new InvalidOperationException("VAULT_ROLE_ID não definido");

var secretId = Environment.GetEnvironmentVariable("VAULT_SECRET_ID")
    ?? throw new InvalidOperationException("VAULT_SECRET_ID não definido");

var vault = new VaultService(vaultAddr, roleId, secretId);

// Lê o segredo em secret/minha-app
var data = await vault.ReadSecretAsync("minha-app");

foreach (var entry in data)
    Console.WriteLine($"{entry.Key} = {entry.Value}");
