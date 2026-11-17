using System.Text.Json;
using GleisGrain.Common.Errors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace GleisGrain.Common.Tests;

public sealed class GlobalExceptionHandlerTests
{
    [Fact]
    public async Task TryHandle_RequestTheFrameworkCouldNotBind_AnswersWith400AndTheReason()
    {
        var problems = new CapturingProblemDetails();
        var handler = new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance, problems);
        var invalidValue = new JsonException("'x' is not a valid Nickname.");
        var exception = new BadHttpRequestException("Failed to read parameter from the request body as JSON.", StatusCodes.Status400BadRequest, invalidValue);

        var handled = await handler.TryHandleAsync(new DefaultHttpContext(), exception, TestContext.Current.CancellationToken);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status400BadRequest, problems.Written?.Status);
        Assert.Equal("'x' is not a valid Nickname.", problems.Written?.Detail);
        Assert.Equal("common.request.invalid", problems.Written?.Extensions["code"]);
    }

    [Fact]
    public async Task TryHandle_UnexpectedException_AnswersWith500AndHidesTheDetails()
    {
        var problems = new CapturingProblemDetails();
        var handler = new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance, problems);

        await handler.TryHandleAsync(new DefaultHttpContext(), new InvalidOperationException("secret"), TestContext.Current.CancellationToken);

        Assert.Equal(StatusCodes.Status500InternalServerError, problems.Written?.Status);
        Assert.Null(problems.Written?.Detail);
    }

    private sealed class CapturingProblemDetails : IProblemDetailsService
    {
        public ProblemDetails? Written { get; private set; }

        public ValueTask WriteAsync(ProblemDetailsContext context)
        {
            Written = context.ProblemDetails;
            return ValueTask.CompletedTask;
        }

        public ValueTask<bool> TryWriteAsync(ProblemDetailsContext context)
        {
            Written = context.ProblemDetails;
            return ValueTask.FromResult(true);
        }
    }
}
