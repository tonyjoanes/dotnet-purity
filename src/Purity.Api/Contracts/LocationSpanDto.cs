using Purity.Engine.Domain;

namespace Purity.Api.Contracts;

public sealed record LocationSpanDto(int StartLine, int StartColumn, int EndLine, int EndColumn)
{
    public static LocationSpanDto From(LocationSpan span) =>
        new(span.StartLine, span.StartColumn, span.EndLine, span.EndColumn);
}


