using LanguageExt;
using Purity.Engine.Domain;

namespace Purity.Engine.Application;

public interface IAnalyzerRunner
{
    Task<Either<AnalyzerFailure, AnalyzerReport>> RunAsync(ScanRequest request, CancellationToken cancellationToken = default);
}


