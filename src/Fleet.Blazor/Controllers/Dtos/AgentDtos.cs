using Fleet.Shared;

namespace Fleet.Blazor.Controllers.Dtos;

public sealed class CreateAgentRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string PromptTemplate { get; set; } = string.Empty;
    public string? ModelPolicy { get; set; }
    public string[]? AllowedTools { get; set; }
    public string[]? AllowedResources { get; set; }
    public ExtensionPermissionTier ExtensionTier { get; set; } = ExtensionPermissionTier.Medium;
}
