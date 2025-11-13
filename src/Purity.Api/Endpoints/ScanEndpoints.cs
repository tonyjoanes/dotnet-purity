using LanguageExt;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Purity.Api.Contracts;
using Purity.Engine.Application;
using static LanguageExt.Prelude;

namespace Purity.Api.Endpoints;

public static class ScanEndpoints
{
    public static RouteGroupBuilder MapScanEndpoints(this IEndpointRouteBuilder app)
    {
        // TODO: Re-enable authentication once GitHub token validation is implemented
        // .RequireAuthorization() temporarily disabled for development (Option A)
        var group = app.MapGroup("/scan")
            .WithTags("Scan");

        group.MapPost("/", HandleScanAsync)
             .WithName("RunScan")
             .WithSummary("Executes Purity analyzers against the provided repository path.")
             .Produces<ScanResponseDto>(StatusCodes.Status200OK)
             .ProducesProblem(StatusCodes.Status400BadRequest);

        return group;
    }

    private static async Task<IResult> HandleScanAsync(
        [FromBody] ScanRequestDto request,
        IAnalyzerRunner runner,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger("Purity.Scan");

        if (string.IsNullOrWhiteSpace(request.RepositoryPath))
        {
            const string reason = "Repository path must be provided.";
            logger.LogWarning("Scan request rejected: {Reason}", reason);
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["repositoryPath"] = new[] { reason }
            });
        }

        var outcome = await runner.RunAsync(request.ToDomain(), cancellationToken);

        return outcome.Match(
            Right: report => Results.Ok(ScanResponseDto.From(report)),
            Left: failure =>
            {
                logger.LogError(failure.Exception, "Analyzer run failed with code {Code}: {Reason}", failure.Code, failure.Reason);
                return Results.Problem(
                    title: "Analyzer run failed",
                    detail: failure.Reason,
                    statusCode: StatusCodes.Status400BadRequest,
                    extensions: new Dictionary<string, object?>
                    {
                        ["code"] = failure.Code
                    });
            });
    }
}


