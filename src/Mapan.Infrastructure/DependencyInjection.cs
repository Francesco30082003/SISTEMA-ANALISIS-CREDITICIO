using Mapan.Application.Empresas;
using Mapan.Infrastructure.Persistence.Repositories;
using Mapan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Mapan.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("MapanDatabase");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Falta la configuración 'ConnectionStrings:MapanDatabase'. " +
                "Configúrela mediante User Secrets en Mapan.Api antes de iniciar la aplicación.");
        }

        services.AddDbContext<MapanDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IEmpresaRepository, EmpresaRepository>();
        services.AddScoped<AuditWriter>();
        services.AddScoped<Mapan.Application.Clientes.IClienteRepository, ClienteRepository>();
        services.AddScoped<Mapan.Application.Productos.IProductoRepository, ProductoRepository>();
        services.AddScoped<Mapan.Application.Organizacion.IOrganizacionRepository, OrganizacionRepository>();
        services.AddScoped<Mapan.Application.Solicitudes.ISolicitudRepository, SolicitudRepository>();
        services.AddScoped<Mapan.Application.Expedientes.IExpedienteRepository, ExpedienteRepository>();
        services.AddScoped<Mapan.Application.Documentos.IDocumentoRepository, DocumentoRepository>();
        services.AddScoped<Mapan.Application.Analysis.ICreditAnalysisRepository, CreditAnalysisRepository>();
        services.AddScoped<Mapan.Application.Policies.IPolicyRepository, PolicyRepository>();
        services.AddScoped<Mapan.Application.Modelos.IModeloRepository, ModeloRepository>();
        services.AddScoped<Mapan.Application.Operations.IOperationsRepository, OperationsRepository>();
        services.AddScoped<Mapan.Application.Cartera.ICarteraMoraRepository, CarteraMoraRepository>();
        services.AddScoped<Mapan.Application.Analistas.IAnalistaDesempenoRepository, AnalistaDesempenoRepository>();
        services.AddScoped<Mapan.Application.Common.IEmailSender, Mapan.Infrastructure.Notifications.SmtpEmailSender>();
        var maxFileBytes=configuration.GetValue("Storage:MaxBytes",20L*1024*1024);
        if(maxFileBytes<8||maxFileBytes>100L*1024*1024)throw new InvalidOperationException("Storage:MaxBytes debe estar entre 8 y 104857600.");
        services.AddSingleton(new Mapan.Infrastructure.Storage.FileStorageSettings(
            configuration["Storage:RootPath"]??Path.Combine(Directory.GetCurrentDirectory(),".local-storage"),maxFileBytes,
            new HashSet<string>(configuration.GetSection("Storage:AllowedMimeTypes").Get<string[]>()??["application/pdf","image/png","image/jpeg"])));
        services.AddScoped<Mapan.Application.Documentos.IFileStorage, Mapan.Infrastructure.Storage.LocalFileStorage>();
        services.AddHealthChecks().AddCheck<PostgresHealthCheck>("postgresql", tags: ["ready"]);
        services.AddHttpClient<Mapan.Infrastructure.Integrations.RiskModelClient>(client => client.Timeout=TimeSpan.FromSeconds(30));
        services.AddScoped<Mapan.Application.Integrations.IRiskModelClient, Mapan.Infrastructure.Integrations.HybridRiskModelClient>();
        services.AddScoped<Mapan.Application.Integrations.ICreditBureauProvider, Mapan.Infrastructure.Integrations.MockEquifaxProvider>();
        services.AddScoped<Mapan.Application.Integrations.IJudicialProvider, Mapan.Infrastructure.Integrations.MockJudicialProvider>();
        services.AddScoped<Mapan.Application.Integrations.IAvalProvider, Mapan.Infrastructure.Integrations.MockAvalProvider>();
        services.AddScoped<Mapan.Application.Investigacion.IInvestigacionRepository, Mapan.Infrastructure.Persistence.Repositories.InvestigacionRepository>();
        services.AddScoped<Mapan.Application.Preevaluacion.IPreevaluacionRepository, Mapan.Infrastructure.Persistence.Repositories.PreevaluacionRepository>();
        services.AddScoped<Mapan.Application.Excepciones.IExcepcionRepository, Mapan.Infrastructure.Persistence.Repositories.ExcepcionRepository>();
        services.AddScoped<Mapan.Application.Integrations.IDocumentVerificationProvider, Mapan.Infrastructure.Integrations.MockDocumentVerificationProvider>();
        services.AddScoped<Mapan.Application.Integrations.IVerificacionLaboralProvider, Mapan.Infrastructure.Integrations.MockIessProvider>();
        services.AddScoped<Mapan.Application.Verificacion.IVerificacionRepository, Mapan.Infrastructure.Persistence.Repositories.VerificacionRepository>();
        services.AddScoped<Mapan.Application.Cartera.ICosechaRepository, Mapan.Infrastructure.Persistence.Repositories.CosechaRepository>();
        services.AddScoped<Mapan.Application.Visitas.IVisitaNegocioRepository, Mapan.Infrastructure.Persistence.Repositories.VisitaNegocioRepository>();
        services.AddScoped<Mapan.Infrastructure.Integrations.TextPdfDocumentExtractionService>();
        services.AddScoped<Mapan.Application.Integrations.IDocumentExtractionService, Mapan.Infrastructure.Integrations.HybridDocumentExtractionService>();

        return services;
    }
}
