using System.ComponentModel.DataAnnotations;
using Purity.Engine.Domain;

namespace Purity.Api.Contracts;

public sealed record ScanRequestDto(
    [property: Required(AllowEmptyStrings = false)]
    string RepositoryPath)
{
    public ScanRequest ToDomain() => new(RepositoryPath);
}


