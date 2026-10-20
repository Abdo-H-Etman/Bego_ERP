using MediatR;
using Serilog;

namespace Erp.Application.Abstractions.Behaviors;

public class LoggingBehavior<TRequest, TResponse>(ILogger logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger _logger = logger.ForContext<LoggingBehavior<TRequest, TResponse>>();

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        _logger.Information("Processing command/query {RequestName}", requestName);

        var response = await next();

        _logger.Information("Completed command/query {RequestName}", requestName);
        return response;
    }
}
