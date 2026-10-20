using System.Text;
using Erp.Application.Abstractions.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using ILogger = Serilog.ILogger;

namespace Erp.Api.Security;

public class BegohubHmacMiddleware(
    RequestDelegate next,
    IHmacService hmacService,
    IConfiguration configuration,
    ILogger logger)
{
    private readonly ILogger _logger = logger.ForContext<BegohubHmacMiddleware>();

    public async Task InvokeAsync(HttpContext context)
    {
        // Only apply to Begohub integration data pull endpoints
        if (!context.Request.Path.StartsWithSegments("/api/integrations/begohub", StringComparison.OrdinalIgnoreCase))
        {
            await next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue("X-Begohub-Signature", out var signatureHeader) ||
            !context.Request.Headers.TryGetValue("X-Begohub-Timestamp", out var timestampHeader) ||
            !long.TryParse(timestampHeader.ToString(), out var timestamp))
        {
            _logger.Warning("Begohub HMAC authentication failed: Missing required signature or timestamp headers.");
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { Error = "HMAC headers missing (X-Begohub-Signature, X-Begohub-Timestamp)" });
            return;
        }

        // Enable multiple reads of request body
        context.Request.EnableBuffering();
        using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        context.Request.Body.Position = 0;

        var secretKey = configuration["Begohub:SecretKey"] ?? "begohub-default-secure-hmac-secret-key-2026";

        var isValid = hmacService.ValidateSignature(body, secretKey, signatureHeader.ToString(), timestamp);
        if (!isValid)
        {
            _logger.Warning("Begohub HMAC signature mismatch or replay attack detected for timestamp {Timestamp}.", timestamp);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { Error = "Invalid HMAC signature or expired timestamp" });
            return;
        }

        _logger.Information("Begohub HMAC authentication succeeded for path: {Path}", context.Request.Path);
        await next(context);
    }
}
