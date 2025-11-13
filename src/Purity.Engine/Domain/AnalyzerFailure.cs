namespace Purity.Engine.Domain;

public sealed record AnalyzerFailure(string Code, string Reason, Exception? Exception = null);


