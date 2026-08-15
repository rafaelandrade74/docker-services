using VaultSharp;
using VaultSharp.V1.AuthMethods.AppRole;
using VaultSharp.V1.Commons;

/// <summary>
/// Exemplo de leitura de segredos do HashiCorp Vault via AppRole em C#.
/// Pacote NuGet: VaultSharp (https://www.nuget.org/packages/VaultSharp)
/// </summary>
public class VaultService
{
    private readonly IVaultClient _client;

    public VaultService(string vaultAddress, string roleId, string secretId)
    {
        var authMethod = new AppRoleAuthMethodInfo(roleId, secretId);
        var settings = new VaultClientSettings(vaultAddress, authMethod);
        _client = new VaultClient(settings);
    }

    /// <summary>
    /// Lê um segredo do caminho secret/<path> e retorna o dicionário de valores.
    /// </summary>
    public async Task<IDictionary<string, object>> ReadSecretAsync(string path)
    {
        Secret<SecretData> secret = await _client.V1.Secrets.KeyValue.V2.ReadSecretAsync(
            path: path,
            mountPoint: "secret"
        );

        return secret.Data.Data;
    }
}
