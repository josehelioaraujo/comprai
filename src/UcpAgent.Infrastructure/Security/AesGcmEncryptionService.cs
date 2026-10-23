using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using UcpAgent.SharedKernel.Ports;

namespace UcpAgent.Infrastructure.Security;

/// <summary>
/// Criptografia AES-256-GCM para campos PII.
/// Chave: env var PII_ENCRYPTION_KEY (base64, 32 bytes).
/// Formato armazenado: "enc:&lt;base64(nonce[12] + ciphertext + tag[16])&gt;"
/// Dados legado (sem prefixo "enc:") são retornados sem alteração no Decrypt.
///
/// NOTA: a validação da chave é lazy (ocorre na primeira chamada a Encrypt/Decrypt).
/// Isso garante que o DI container consiga construir o serviço mesmo quando
/// PII_ENCRYPTION_KEY não está configurada; nesse caso o erro é lançado durante
/// a operação e capturado pelo try/catch do F1 em RedisCheckoutAdapter,
/// fazendo o checkout prosseguir via Redis sem afetar o usuário final.
/// </summary>
public sealed class AesGcmEncryptionService : IEncryptionService
{
    private const string Prefix    = "enc:";
    private const int    NonceSize = 12; // AES-GCM recomendado
    private const int    TagSize   = 16; // 128 bits

    // Chave resolvida lazy e thread-safe na primeira chamada a Encrypt/Decrypt.
    private readonly Lazy<byte[]> _key;

    public AesGcmEncryptionService(IConfiguration configuration)
    {
        // Apenas captura o valor bruto — validação ocorre na primeira operação.
        // Usar Lazy garante inicialização thread-safe para o Singleton.
        var raw = configuration["PII_ENCRYPTION_KEY"];
        _key = new Lazy<byte[]>(() => ResolveKey(raw));
    }

    /// <summary>
    /// Resolve e valida a chave AES-256. Chamado lazy na primeira criptografia.
    /// Lança InvalidOperationException se PII_ENCRYPTION_KEY estiver ausente ou inválida.
    /// </summary>
    private static byte[] ResolveKey(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            throw new InvalidOperationException(
                "Variável PII_ENCRYPTION_KEY não configurada. " +
                "Gere com: openssl rand -base64 32 | tr '+/' '-_' | tr -d '='");

        // Suporta base64 padrão e URL-safe (sem padding)
        var normalized = raw.Replace('-', '+').Replace('_', '/');
        var padded     = normalized.PadRight(normalized.Length + (4 - normalized.Length % 4) % 4, '=');
        var key        = Convert.FromBase64String(padded);

        if (key.Length != 32)
            throw new InvalidOperationException(
                $"PII_ENCRYPTION_KEY deve ter 32 bytes (AES-256). Tamanho atual: {key.Length}");

        return key;
    }

    public string? Encrypt(string? value)
    {
        if (value is null) return null;
        if (value.StartsWith(Prefix, StringComparison.Ordinal)) return value; // já criptografado

        var plaintext  = Encoding.UTF8.GetBytes(value);
        var nonce      = new byte[NonceSize];
        var ciphertext = new byte[plaintext.Length];
        var tag        = new byte[TagSize];

        RandomNumberGenerator.Fill(nonce);

        using var aes = new AesGcm(_key.Value, TagSize);
        aes.Encrypt(nonce, plaintext, ciphertext, tag);

        // Layout: nonce (12) | ciphertext (n) | tag (16)
        var combined = new byte[NonceSize + ciphertext.Length + TagSize];
        nonce.CopyTo(combined, 0);
        ciphertext.CopyTo(combined, NonceSize);
        tag.CopyTo(combined, NonceSize + ciphertext.Length);

        return Prefix + Convert.ToBase64String(combined);
    }

    public string? Decrypt(string? value)
    {
        if (value is null) return null;

        // Dado legado — plaintext
        if (!value.StartsWith(Prefix, StringComparison.Ordinal)) return value;

        var combined   = Convert.FromBase64String(value[Prefix.Length..]);
        var nonce      = combined[..NonceSize];
        var tag        = combined[^TagSize..];
        var ciphertext = combined[NonceSize..^TagSize];
        var plaintext  = new byte[ciphertext.Length];

        using var aes = new AesGcm(_key.Value, TagSize);
        aes.Decrypt(nonce, ciphertext, tag, plaintext);

        return Encoding.UTF8.GetString(plaintext);
    }
}
