using Mapan.Application.Analysis;
using Mapan.Application.Common;
using Mapan.Domain.Entities;

namespace Mapan.Application.Tests;

public sealed class AnalysisReportTests
{
    private static Analisis Analisis() => new(){AnalisisId=Guid.NewGuid(),EmpresaId=Guid.NewGuid(),SolicitudCreditoId=Guid.NewGuid(),PoliticaVersionId=Guid.NewGuid(),NumeroEjecucion=1,TipoAnalisis="REGLAS",Estado="COMPLETADO",EjecutadoPorUsuarioEmpresaId=Guid.NewGuid(),IniciadoEn=DateTimeOffset.UtcNow};
    private static SnapshotFinanciero Snapshot() => new(){SnapshotFinancieroId=Guid.NewGuid(),EmpresaId=Guid.NewGuid(),AnalisisId=Guid.NewGuid(),IngresoTotalMensual=1500,GastoTotalMensual=300,IngresoDisponible=1200,FactorCapacidad=0.5m,CapacidadNuevaCuota=600,DeudaTotalActual=500,CuotasActuales=50,CuotaNuevaEstimada=400,CuotaCompatible=true};
    private static Recomendacion Recomendacion() => new(){RecomendacionId=Guid.NewGuid(),EmpresaId=Guid.NewGuid(),AnalisisId=Guid.NewGuid(),CodigoRecomendacion="CAPACIDAD_COMPATIBLE",RequiereRevisionHumana=true,Resumen="Resultado de compatibilidad de cuota con la capacidad calculada.",FechaGeneracion=DateTimeOffset.UtcNow};

    [Fact]
    public void RendersAValidPdfWithNoObligacionesDocumentosOrPrediccion()
    {
        var detail = new AnalysisDetail(Analisis(), Snapshot(), null, Recomendacion(), [], null, null, [], []);
        var bytes = AnalysisReport.Render(detail);
        Assert.True(bytes.Length > 500);
        Assert.Equal("%PDF"u8.ToArray(), bytes[..4]);
    }

    [Fact]
    public void RendersObligacionesDocumentosAlertasAndPrediccion()
    {
        var obligaciones = new[]{new ObligacionResumen("Banco X","CONSUMO",500m,50m,0,"VIGENTE")};
        var documentos = new[]{new DocumentoResumen("CEDULA","cedula.pdf",1,"VALIDADO",true,"IDENTIFICACION")};
        var alerta = new Alerta{AlertaId=Guid.NewGuid(),EmpresaId=Guid.NewGuid(),AnalisisId=Guid.NewGuid(),Codigo="TEST",Nivel="AMARILLO",Titulo="Punto de atención",Descripcion="Detalle",FechaCreacion=DateTimeOffset.UtcNow};
        var prediccion = new Prediccion{PrediccionId=Guid.NewGuid(),EmpresaId=Guid.NewGuid(),AnalisisId=Guid.NewGuid(),ModeloVersionId=Guid.NewGuid(),ProbabilidadIncumplimiento=0.12m,UmbralUtilizado=0.5m,ClasePredicha="NO_INCUMPLIMIENTO",NivelRiesgo="BAJO",TiempoInferenciaMs=5,FechaPrediccion=DateTimeOffset.UtcNow};
        var detail = new AnalysisDetail(Analisis(), Snapshot(), null, Recomendacion(), [alerta], null, prediccion, obligaciones, documentos);
        var bytes = AnalysisReport.Render(detail);
        Assert.True(bytes.Length > 500);
        Assert.Equal("%PDF"u8.ToArray(), bytes[..4]);
    }

    [Fact]
    public void MissingSnapshotIsReported() =>
        Assert.Equal("REPORT_UNAVAILABLE", Assert.Throws<ApplicationError>(() => AnalysisReport.Render(new AnalysisDetail(Analisis(), null, null, Recomendacion(), [], null, null, [], []))).Code);

    [Fact]
    public void MissingRecomendacionIsReported() =>
        Assert.Equal("REPORT_UNAVAILABLE", Assert.Throws<ApplicationError>(() => AnalysisReport.Render(new AnalysisDetail(Analisis(), Snapshot(), null, null, [], null, null, [], []))).Code);
}
