using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace GleisGrain.Common.Errors;

public sealed partial class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IProblemDetailsService problemDetails)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var problem = exception switch
        {
            DomainException domain => new ProblemDetails
            {
                Status = domain.Error.Status,
                Title = domain.Error.Title,
                Detail = domain.Error.Detail,
                Extensions = { ["code"] = domain.Error.Code },
            },

            // The framework could not bind the request: malformed JSON, or a value such as a nickname that
            // its id type refused. That is the caller's mistake, so a 400 and no error log.
            BadHttpRequestException badRequest => new ProblemDetails
            {
                Status = badRequest.StatusCode,
                Title = "Invalid request",
                Detail = badRequest.InnerException is JsonException invalidValue ? invalidValue.Message : badRequest.Message,
                Extensions = { ["code"] = "common.request.invalid" },
            },

            _ => new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An unexpected error occurred.",
            },
        };

        switch (exception)
        {
            case DomainException domainException:
                LogDomainError(logger, domainException.Error.Code);
                break;
            case BadHttpRequestException badRequest:
                LogBadRequest(logger, badRequest.Message);
                break;
            default:
                LogUnhandled(logger, exception);
                break;
        }

        httpContext.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;

        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = problem,
        });
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Invalid request: {Reason}")]
    private static partial void LogBadRequest(ILogger logger, string reason);

    [LoggerMessage(Level = LogLevel.Information, Message = "Domain error {Code}")]
    private static partial void LogDomainError(ILogger logger, string code);

    [LoggerMessage(Level = LogLevel.Error, Message = "Unhandled exception")]
    private static partial void LogUnhandled(ILogger logger, Exception exception);
}
