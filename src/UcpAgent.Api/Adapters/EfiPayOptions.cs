namespace UcpAgent.Api.Adapters;

public sealed class EfiPayOptions
{
    public string ClientId            { get; set; } = "";
    public string ClientSecret        { get; set; } = "";
    public string CertificatePath     { get; set; } = "";
    public string CertificateBase64   { get; set; } = "";
    public string CertificatePassword { get; set; } = "";
    public string ChavePix            { get; set; } = "";
    public bool   Sandbox             { get; set; } = true;

    public string BaseUrl => Sandbox
        ? "https://pix-h.api.efipay.com.br"
        : "https://pix.api.efipay.com.br";
}
