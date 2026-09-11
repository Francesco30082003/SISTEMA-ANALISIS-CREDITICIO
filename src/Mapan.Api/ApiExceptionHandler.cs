using Mapan.Application.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Mapan.Api;

public sealed class ApiExceptionHandler(IProblemDetailsService problems) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        var applicationError = exception as ApplicationError;
        var conflict = exception is DbUpdateException { InnerException: PostgresException { SqlState: "23505" } };
        var status = applicationError?.Status ?? (conflict ? 409 : 500);
        context.Response.StatusCode = status;
        if (status == 401) context.Response.Headers.WWWAuthenticate = "Bearer";
        return await problems.TryWriteAsync(new ProblemDetailsContext {
            HttpContext = context,
            ProblemDetails = new ProblemDetails {
                Status = status, Title = applicationError?.Message ?? (conflict ? "El registro ya existe." : "Error inesperado."),
                Extensions = { ["code"] = applicationError?.Code ?? (conflict ? "CONFLICT" : "UNEXPECTED") }
            }
        });
    }
}
