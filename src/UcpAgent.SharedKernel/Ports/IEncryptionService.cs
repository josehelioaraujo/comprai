namespace UcpAgent.SharedKernel.Ports;

/// <summary>
/// Criptografia determinística para campos PII no banco de dados.
/// Implementação: AES-256-GCM com nonce aleatório por operação.
/// Valores criptografados têm prefixo "enc:" para distinguir de legado plaintext.
/// </summary>
public interface IEncryptionService
{
    /// <summary>Criptografa o valor. Retorna null se input for null.</summary>
    string? Encrypt(string? value);

    /// <summary>
    /// Descriptografa o valor.
    /// Se não tiver prefixo "enc:" (dado legado), retorna o valor original sem modificação.
    /// Retorna null se input for null.
    /// </summary>
    string? Decrypt(string? value);
}
