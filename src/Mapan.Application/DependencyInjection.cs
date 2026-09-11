using Mapan.Application.Empresas;
using Mapan.Application.Security;
using Microsoft.Extensions.DependencyInjection;

namespace Mapan.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<EmpresaService>();
        services.AddScoped<AuthenticationService>();
        services.AddScoped<PasswordResetService>();
        services.AddMemoryCache();
        services.AddScoped<SessionService>();
        services.AddScoped<SecurityAdministrationService>();
        services.AddScoped<Mapan.Application.Clientes.ClienteService>();
        services.AddScoped<Mapan.Application.Productos.ProductoService>();
        services.AddScoped<Mapan.Application.Organizacion.OrganizacionService>();
        services.AddScoped<Mapan.Application.Solicitudes.SolicitudService>();
        services.AddScoped<Mapan.Application.Expedientes.ExpedienteService>();
        services.AddScoped<Mapan.Application.Documentos.DocumentoService>();
        services.AddScoped<Mapan.Application.Analysis.CreditAnalysisService>();
        services.AddScoped<Mapan.Application.Policies.PolicyService>();
        services.AddScoped<Mapan.Application.Modelos.ModeloService>();
        services.AddScoped<Mapan.Application.Operations.OperationsService>();
        services.AddScoped<Mapan.Application.Cartera.CarteraMoraService>();
        services.AddScoped<Mapan.Application.Analistas.AnalistaDesempenoService>();
        services.AddScoped<Mapan.Application.Investigacion.InvestigacionService>();
        services.AddScoped<Mapan.Application.Preevaluacion.PreevaluacionService>();
        services.AddScoped<Mapan.Application.Excepciones.ExcepcionService>();
        services.AddScoped<Mapan.Application.Verificacion.VerificacionService>();
        services.AddScoped<Mapan.Application.Cartera.CosechaService>();
        services.AddScoped<Mapan.Application.Visitas.VisitaNegocioService>();
        services.AddSingleton(TimeProvider.System);
        return services;
    }
}
