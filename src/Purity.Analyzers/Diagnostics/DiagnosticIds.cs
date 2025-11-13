namespace Purity.Analyzers.Diagnostics;

public static class DiagnosticIds
{
    public const string AwaitInsideLoop = "PURITY001";
    public const string SyncOverAsync = "PURITY002";
    public const string StaticCollectionLeak = "PURITY003";
    public const string DisposableNotDisposed = "PURITY004";
    public const string EventHandlerLeak = "PURITY005";
    public const string MultipleEnumeration = "PURITY006";
    public const string NullReferenceDereference = "PURITY007";
    public const string SwallowedException = "PURITY008";
    public const string StringConcatenationInLoop = "PURITY009";
    public const string InsecureAlgorithm = "PURITY010";
    public const string ThreadSafetyViolation = "PURITY013";
}


