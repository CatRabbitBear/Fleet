using System.ComponentModel.DataAnnotations;

namespace Fleet.Blazor.Controllers.Dtos;

public sealed record SetCredentialRequest(
    [property: Required, MaxLength(120)] string Target,
    [property: Required, MaxLength(4096)] string Value);

public sealed record DeleteCredentialRequest(
    [property: Required, MaxLength(120)] string Target);
