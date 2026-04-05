using Fleet.Shared;

namespace Fleet.Tray.Utils;

internal sealed class TrayCredentialHostService : ICredentialHostService
{
    private static readonly HashSet<string> AllowedTargets =
    [
        "FLEET_AZURE_ENDPOINT",
        "FLEET_AZURE_MODEL_ID",
        "FLEET_AZURE_MODEL_KEY",
        "FLEET_CORS_EXCEMPTION"
    ];

    private readonly IAuditRepository _auditRepository;

    public TrayCredentialHostService(IAuditRepository auditRepository)
    {
        _auditRepository = auditRepository;
    }

    public bool IsAllowedTarget(string target) => AllowedTargets.Contains(target);

    public Task<CredentialCommandResult> SetCredentialAsync(string target, string value, string correlationId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Task.FromResult(new CredentialCommandResult(false, correlationId, "Credential value must be provided.", "ValidationError"));
        }

        try
        {
            CredentialManagerHelper.SaveCredential(target, string.Empty, value.Trim(), useLocalMachine: true);
            return Task.FromResult(new CredentialCommandResult(true, correlationId, "Credential saved."));
        }
        catch
        {
            return Task.FromResult(new CredentialCommandResult(false, correlationId, "Credential save failed.", "HostWriteFailed"));
        }
    }

    public Task<CredentialCommandResult> DeleteCredentialAsync(string target, string correlationId, CancellationToken cancellationToken)
    {
        if (!CredentialManagerHelper.TryDeleteCredential(target, out _))
        {
            return Task.FromResult(new CredentialCommandResult(false, correlationId, "Credential delete failed.", "HostDeleteFailed"));
        }

        return Task.FromResult(new CredentialCommandResult(true, correlationId, "Credential deleted."));
    }

    public async Task<CredentialMetadata> GetCredentialMetadataAsync(string target, CancellationToken cancellationToken)
    {
        var (_, secret) = CredentialManagerHelper.LoadCredential(target);
        var lastUpdated = await _auditRepository.GetLatestCredentialUpdateAsync(target, cancellationToken);
        return new CredentialMetadata(target, !string.IsNullOrWhiteSpace(secret), lastUpdated);
    }
}
