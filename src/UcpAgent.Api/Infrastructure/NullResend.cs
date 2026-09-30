using Resend;

namespace UcpAgent.Api.Infrastructure;

/// <summary>
/// Implementação nula do IResend — usada quando RESEND_API_KEY não está configurada.
/// O EmailNotificationWorker usa isto e loga o email em vez de enviar de verdade.
/// </summary>
public sealed class NullResend : IResend
{
    public static readonly NullResend Instance = new();
    private NullResend() { }

    public Task<ResendResponse<Guid>> EmailSendAsync(EmailMessage message, CancellationToken ct = default)
    {
        Console.WriteLine($"[NullResend] Email fake para {string.Join(",", message.To)}: {message.Subject}");
        return Task.FromResult(ResendResponse.Success(Guid.NewGuid()));
    }

    // Stubs obrigatórios pela interface
    public Task<ResendResponse<List<Email>>> EmailListAsync(CancellationToken ct = default) => Task.FromResult(ResendResponse.Success(new List<Email>()));
    public Task<ResendResponse<Email>> EmailGetAsync(Guid emailId, CancellationToken ct = default) => Task.FromResult(ResendResponse.Success(new Email()));
    public Task<ResendResponse<UpdateEmailResponse>> EmailUpdateAsync(Guid emailId, EmailUpdateRequest req, CancellationToken ct = default) => Task.FromResult(ResendResponse.Success(new UpdateEmailResponse()));
    public Task<ResendResponse<EmailCancelResponse>> EmailCancelAsync(Guid emailId, CancellationToken ct = default) => Task.FromResult(ResendResponse.Success(new EmailCancelResponse()));
    public Task<ResendResponse<Guid>> EmailSendBatchAsync(IEnumerable<EmailMessage> messages, CancellationToken ct = default) => Task.FromResult(ResendResponse.Success(Guid.NewGuid()));
    public Task<ResendResponse<DomainRecord>> DomainCreateAsync(string name, DomainRegion region, CancellationToken ct = default) => Task.FromResult(ResendResponse.Success(new DomainRecord()));
    public Task<ResendResponse<Domain>> DomainGetAsync(Guid domainId, CancellationToken ct = default) => Task.FromResult(ResendResponse.Success(new Domain()));
    public Task<ResendResponse<List<Domain>>> DomainListAsync(CancellationToken ct = default) => Task.FromResult(ResendResponse.Success(new List<Domain>()));
    public Task<ResendResponse> DomainUpdateAsync(Guid domainId, DomainUpdateRequest req, CancellationToken ct = default) => Task.FromResult(ResendResponse.Success());
    public Task<ResendResponse> DomainVerifyAsync(Guid domainId, CancellationToken ct = default) => Task.FromResult(ResendResponse.Success());
    public Task<ResendResponse> DomainDeleteAsync(Guid domainId, CancellationToken ct = default) => Task.FromResult(ResendResponse.Success());
    public Task<ResendResponse<ApiKey>> ApiKeyCreateAsync(string name, Permission? perm = null, Guid? domainId = null, CancellationToken ct = default) => Task.FromResult(ResendResponse.Success(new ApiKey()));
    public Task<ResendResponse<List<ApiKey>>> ApiKeyListAsync(CancellationToken ct = default) => Task.FromResult(ResendResponse.Success(new List<ApiKey>()));
    public Task<ResendResponse> ApiKeyDeleteAsync(Guid apiKeyId, CancellationToken ct = default) => Task.FromResult(ResendResponse.Success());
    public Task<ResendResponse<Audience>> AudienceCreateAsync(string name, CancellationToken ct = default) => Task.FromResult(ResendResponse.Success(new Audience()));
    public Task<ResendResponse<Audience>> AudienceGetAsync(Guid audienceId, CancellationToken ct = default) => Task.FromResult(ResendResponse.Success(new Audience()));
    public Task<ResendResponse<List<Audience>>> AudienceListAsync(CancellationToken ct = default) => Task.FromResult(ResendResponse.Success(new List<Audience>()));
    public Task<ResendResponse> AudienceDeleteAsync(Guid audienceId, CancellationToken ct = default) => Task.FromResult(ResendResponse.Success());
    public Task<ResendResponse<Contact>> ContactCreateAsync(Guid audienceId, ContactData data, CancellationToken ct = default) => Task.FromResult(ResendResponse.Success(new Contact()));
    public Task<ResendResponse<Contact>> ContactGetAsync(Guid audienceId, Guid contactId, CancellationToken ct = default) => Task.FromResult(ResendResponse.Success(new Contact()));
    public Task<ResendResponse<List<Contact>>> ContactListAsync(Guid audienceId, CancellationToken ct = default) => Task.FromResult(ResendResponse.Success(new List<Contact>()));
    public Task<ResendResponse> ContactUpdateAsync(Guid audienceId, Guid contactId, ContactData data, CancellationToken ct = default) => Task.FromResult(ResendResponse.Success());
    public Task<ResendResponse> ContactDeleteAsync(Guid audienceId, Guid contactId, CancellationToken ct = default) => Task.FromResult(ResendResponse.Success());
}
