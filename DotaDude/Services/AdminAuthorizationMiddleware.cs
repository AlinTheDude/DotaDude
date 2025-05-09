using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace DotaDude.Middleware
{
    public class AdminAuthorizationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<AdminAuthorizationMiddleware> _logger;

        public AdminAuthorizationMiddleware(RequestDelegate next, ILogger<AdminAuthorizationMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                if (context.Request.Path.StartsWithSegments("/Admin") &&
                    !context.Request.Path.StartsWithSegments("/AdminLogin"))
                {
                    // Verifica se l'utente ha il cookie IsAdmin
                    if (!context.Request.Cookies.ContainsKey("IsAdmin"))
                    {
                        _logger.LogWarning($"Tentativo di accesso non autorizzato alla pagina admin: {context.Request.Path}");

                        // Reindirizza alla pagina di login admin
                        context.Response.Redirect("/AdminLogin");
                        return;
                    }
                }

                // Continua con la pipeline della richiesta
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante la verifica dell'autorizzazione admin");
                await _next(context);
            }
        }
    }

    // Estensione per facilitare la registrazione del middleware in Program.cs
    public static class AdminAuthorizationMiddlewareExtensions
    {
        public static IApplicationBuilder UseAdminAuthorization(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<AdminAuthorizationMiddleware>();
        }
    }
}