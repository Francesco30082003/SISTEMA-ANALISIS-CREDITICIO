namespace Mapan.DemoSeeder;

public sealed record SucursalSeed(string Codigo, string Nombre, string Provincia, string Ciudad);
public sealed record ProductoSeed(string Codigo, string Nombre, decimal MontoMinimo, decimal MontoMaximo, int PlazoMinimo, int PlazoMaximo, decimal TasaInteresAnualPct, string Categoria);
public sealed record ProveedorSeed(string Codigo, string Nombre, string Tipo, string Descripcion);
public sealed record RolSeed(string Codigo, string Nombre, string[] Permisos);
public sealed record UsuarioSeed(string NombreUsuario, string Nombres, string Apellidos, string RolCodigo, string? SucursalCodigo, string Password);
public sealed record ClienteSeed(string NumeroIdentificacion, string TipoPersona, string TipoActividad, string? Nombres, string? Apellidos, string? RazonSocial, string? EmpleadorNegocio);

/// <summary>Estado objetivo de una solicitud demo y, cuando aplica, en qué paso de aprobación y banda de mora queda.</summary>
public sealed record SolicitudPlan(int Indice, string EstadoObjetivo, string? EtapaAprobacion, string? BandaMora, int SucursalIndex, int AnalistaIndex, int ClienteIndex);

public static class SeedData
{
    public const string EmpresaCodigo = "DEMO-COOP";
    public const string EmpresaNombreLegal = "Cooperativa Demo Mapan";

    // Catálogo completo de permisos usados hoy por Mapan.Application (Solicitudes/Clientes/Documentos/Productos/Seguridad
    // no estaban en el bootstrap original de tools/Mapan.PermissionBootstrap, que solo cubre 14 de los 29 códigos reales).
    public static readonly string[] TodosLosPermisos =
    [
        "Politicas:Read", "Politicas:Manage",
        "Analisis:Read", "Analisis:Execute",
        "Alertas:Read",
        "Auditoria:Read",
        "Prestamos:Read", "Prestamos:Manage",
        "Workflow:Read", "Workflow:Manage",
        "Integraciones:Read", "Integraciones:Manage",
        "Modelos:Read", "Modelos:Manage",
        "Solicitudes:Read", "Solicitudes:Create", "Solicitudes:Update", "Solicitudes:Send",
        "Clientes:Read", "Clientes:Create", "Clientes:Update", "Clientes:Inactivate",
        "Documentos:Read", "Documentos:Upload", "Documentos:Validate", "Documentos:Delete",
        "Productos:Read", "Productos:Create", "Productos:Update",
        "Seguridad:Manage",
        "Mora:Read", "Mora:Notificar",
        "Analistas:Read",
        "Investigacion:Read", "Investigacion:Execute",
        "Preevaluacion:Read", "Preevaluacion:Execute",
        "Excepciones:Read", "Excepciones:Solicitar", "Excepciones:Aprobar",
        "Verificacion:Read", "Verificacion:Execute",
        "Cosechas:Read",
        "VisitaNegocio:Read", "VisitaNegocio:Create",
        "Solicitudes:Decidir",
    ];

    public static readonly SucursalSeed[] Sucursales =
    [
        new("SUC-01", "Quito Centro", "Pichincha", "Quito"),
        new("SUC-02", "Guayaquil Norte", "Guayas", "Guayaquil"),
        new("SUC-03", "Cuenca", "Azuay", "Cuenca"),
    ];

    public static readonly ProductoSeed[] Productos =
    [
        new("MICRO-01", "Microcrédito", 300m, 15000m, 3, 36, 22m, "MICROCREDITO"),
        new("CONS-01", "Consumo", 500m, 20000m, 6, 48, 16m, "CONSUMO"),
        new("COM-01", "Comercial", 2000m, 100000m, 6, 60, 14m, "COMERCIAL"),
    ];

    // Catálogo de proveedores de investigación del cliente (Fase 3). Codigo/Tipo son independientes:
    // Tipo es la categoría por la que InvestigacionRepository resuelve el proveedor activo de la empresa
    // (así una cooperativa puede sustituir "EQUIFAX-MOCK" por un vendor real sin tocar código).
    public static readonly ProveedorSeed[] Proveedores =
    [
        new("EQUIFAX-MOCK", "Equifax Ecuador (mock)", "BURO_CREDITO", "Reporte de central de riesgo simulado — sin credenciales reales configuradas."),
        new("JUDICIAL-MOCK", "Consejo de la Judicatura (mock)", "JUDICIAL", "Sin API oficial de consulta por cédula disponible en este entorno; deja lista la integración real."),
        new("AVAL-MOCK", "Consulta de Aval/Garante (mock)", "AVAL", "Servicio privado sin credenciales reales configuradas."),
        new("DOCVERIF-MOCK", "Verificación forense de documentos (mock)", "VERIFICACION_DOCUMENTAL", "Servicio de verificación de autenticidad documental simulado — sin proveedor real configurado."),
        new("IESS-MOCK", "IESS - Verificación laboral (mock)", "VERIFICACION_LABORAL", "Sin API oficial de consulta por cédula disponible en este entorno; deja lista la integración real."),
    ];

    public static readonly RolSeed[] Roles =
    [
        new("ADMIN", "Administrador", TodosLosPermisos),
        new("GERENCIA", "Alta Gerencia", [
            "Solicitudes:Read", "Clientes:Read", "Documentos:Read", "Productos:Read", "Productos:Update",
            "Politicas:Read", "Politicas:Manage", "Analisis:Read", "Alertas:Read", "Prestamos:Read",
            "Workflow:Read", "Workflow:Manage", "Auditoria:Read", "Modelos:Read", "Integraciones:Read",
            "Mora:Read", "Mora:Notificar", "Analistas:Read", "Investigacion:Read",
            "Preevaluacion:Read", "Excepciones:Read", "Excepciones:Aprobar", "Verificacion:Read", "Cosechas:Read", "VisitaNegocio:Read", "Solicitudes:Decidir",
        ]),
        new("JEFE_AGENCIA", "Jefe de Agencia", [
            "Solicitudes:Read", "Solicitudes:Create", "Solicitudes:Update", "Solicitudes:Send",
            "Clientes:Read", "Clientes:Create", "Clientes:Update",
            "Documentos:Read", "Documentos:Upload", "Documentos:Validate", "Documentos:Delete",
            "Analisis:Read", "Analisis:Execute", "Alertas:Read", "Prestamos:Read",
            "Workflow:Read", "Workflow:Manage", "Productos:Read", "Mora:Read", "Analistas:Read",
            "Investigacion:Read", "Investigacion:Execute",
            "Preevaluacion:Read", "Preevaluacion:Execute", "Excepciones:Read", "Excepciones:Solicitar", "Excepciones:Aprobar",
            "Verificacion:Read", "Verificacion:Execute", "Cosechas:Read", "VisitaNegocio:Read", "VisitaNegocio:Create", "Solicitudes:Decidir",
        ]),
        new("ANALISTA", "Analista de Crédito", [
            "Solicitudes:Read", "Solicitudes:Create", "Solicitudes:Update", "Solicitudes:Send",
            "Clientes:Read", "Clientes:Create", "Clientes:Update",
            "Documentos:Read", "Documentos:Upload", "Documentos:Validate", "Documentos:Delete",
            "Analisis:Read", "Analisis:Execute", "Alertas:Read", "Prestamos:Read", "Productos:Read",
            "Investigacion:Read", "Investigacion:Execute",
            "Preevaluacion:Read", "Preevaluacion:Execute", "Excepciones:Read", "Excepciones:Solicitar",
            "Verificacion:Read", "Verificacion:Execute", "VisitaNegocio:Read", "VisitaNegocio:Create",
        ]),
        new("AUDITOR", "Auditor", [
            "Auditoria:Read", "Analisis:Read", "Alertas:Read", "Prestamos:Read", "Politicas:Read",
            "Solicitudes:Read", "Clientes:Read", "Workflow:Read", "Productos:Read", "Documentos:Read",
            "Modelos:Read", "Integraciones:Read", "Mora:Read", "Analistas:Read", "Investigacion:Read",
            "Preevaluacion:Read", "Excepciones:Read", "Verificacion:Read", "Cosechas:Read", "VisitaNegocio:Read",
        ]),
    ];

    public const string PasswordGerencia = "Demo#Gerencia2026!";
    public const string PasswordJefe = "Demo#Jefe2026!";
    public const string PasswordAnalista = "Demo#Analista2026!";

    public static readonly UsuarioSeed[] Usuarios =
    [
        new("demo.admin", "Admin", "Demo", "ADMIN", null, "Demo#Admin2026!"),
        new("demo.gerente1", "Andrea", "Salinas", "GERENCIA", null, PasswordGerencia),
        new("demo.gerente2", "Roberto", "Vega", "GERENCIA", null, PasswordGerencia),
        new("demo.jefe.quito", "Carla", "Ponce", "JEFE_AGENCIA", "SUC-01", PasswordJefe),
        new("demo.jefe.guayaquil", "Diego", "Farfan", "JEFE_AGENCIA", "SUC-02", PasswordJefe),
        new("demo.jefe.cuenca", "Lucia", "Torres", "JEFE_AGENCIA", "SUC-03", PasswordJefe),
        new("demo.analista1", "Maria", "Rivas", "ANALISTA", "SUC-01", PasswordAnalista),
        new("demo.analista2", "Jose", "Chamba", "ANALISTA", "SUC-01", PasswordAnalista),
        new("demo.analista3", "Paola", "Suquilanda", "ANALISTA", "SUC-02", PasswordAnalista),
        new("demo.analista4", "Kevin", "Mora", "ANALISTA", "SUC-02", PasswordAnalista),
        new("demo.analista5", "Fernanda", "Guaman", "ANALISTA", "SUC-03", PasswordAnalista),
        new("demo.auditor", "Marcos", "Iza", "AUDITOR", null, "Demo#Auditor2026!"),
    ];

    // Índices (0-based) de demo.analista1..5 dentro de Usuarios, agrupados por sucursal para repartir solicitudes.
    public static readonly int[] AnalistasPorSucursal0 = [6, 7];   // SUC-01
    public static readonly int[] AnalistasPorSucursal1 = [8, 9];   // SUC-02
    public static readonly int[] AnalistasPorSucursal2 = [10];     // SUC-03
    public static int[] AnalistasDe(int sucursalIndex) => sucursalIndex switch
    {
        0 => AnalistasPorSucursal0,
        1 => AnalistasPorSucursal1,
        _ => AnalistasPorSucursal2,
    };
    public static readonly string[] JefeUsuarioPorSucursal = ["demo.jefe.quito", "demo.jefe.guayaquil", "demo.jefe.cuenca"];
    public static readonly string[] GerentesUsuarios = ["demo.gerente1", "demo.gerente2"];

    public static readonly string[] TiposActividad = ["COMERCIO", "AGRICULTURA", "SERVICIOS", "MANUFACTURA", "TRANSPORTE"];

    public static readonly ClienteSeed[] Clientes =
    [
        new("DEMO0000001", "NATURAL", "COMERCIO", "Luis", "Andrango", null, "Tienda Luis Andrango"),
        new("DEMO0000002", "NATURAL", "AGRICULTURA", "Rosa", "Chimbo", null, "Finca Rosa Chimbo"),
        new("DEMO0000003", "NATURAL", "SERVICIOS", "Pedro", "Yupangui", null, "Taller Pedro Yupangui"),
        new("DEMO0000004", "NATURAL", "MANUFACTURA", "Elena", "Guanoluisa", null, "Confecciones Elena"),
        new("DEMO0000005", "NATURAL", "TRANSPORTE", "Manuel", "Cando", null, null),
        new("DEMO0000006", "NATURAL", "COMERCIO", "Sofia", "Pilamunga", null, "Bazar Sofia"),
        new("DEMO0000007", "NATURAL", "AGRICULTURA", "Jorge", "Tenesaca", null, "Sembrios Jorge Tenesaca"),
        new("DEMO0000008", "NATURAL", "SERVICIOS", "Diana", "Ushiña", null, "Estética Diana"),
        new("DEMO0000009", "NATURAL", "COMERCIO", "Carlos", "Quishpe", null, "Minimarket Quishpe"),
        new("DEMO0000010", "NATURAL", "TRANSPORTE", "Monica", "Sisa", null, null),
        new("DEMO0000011", "NATURAL", "MANUFACTURA", "Franklin", "Toapanta", null, "Carpinteria Toapanta"),
        new("DEMO0000012", "NATURAL", "SERVICIOS", "Gabriela", "Chuquimarca", null, "Consultorio Gabriela"),
        new("DEMO0000013", "JURIDICA", "COMERCIO", null, null, "Comercial Los Andes S.A.", null),
        new("DEMO0000014", "JURIDICA", "AGRICULTURA", null, null, "Agroindustrias Sierra Cia. Ltda.", null),
        new("DEMO0000015", "JURIDICA", "MANUFACTURA", null, null, "Textiles Cotopaxi S.A.", null),
        new("DEMO0000016", "JURIDICA", "TRANSPORTE", null, null, "Transportes Rio Norte S.A.", null),
        new("DEMO0000017", "JURIDICA", "SERVICIOS", null, null, "Servicios Contables Austro Cia. Ltda.", null),
        new("DEMO0000018", "JURIDICA", "COMERCIO", null, null, "Distribuidora Pacifico S.A.", null),
        new("DEMO0000019", "JURIDICA", "MANUFACTURA", null, null, "Metalmecanica Chimborazo S.A.", null),
        new("DEMO0000020", "JURIDICA", "AGRICULTURA", null, null, "Exportadora Frutas del Valle S.A.", null),
        new("DEMO0000021", "JURIDICA", "SERVICIOS", null, null, "Soluciones Logisticas Azuay Cia. Ltda.", null),
        new("DEMO0000022", "JURIDICA", "TRANSPORTE", null, null, "Fletes Express Guayas S.A.", null),
    ];

    /// <summary>32 solicitudes repartidas en todos los estados reales, con bandeja de aprobación y cartera en mora representadas.</summary>
    public static IReadOnlyList<SolicitudPlan> PlanSolicitudes()
    {
        var plan = new List<SolicitudPlan>();
        var i = 0;
        void Add(string estado, string? etapa, string? mora)
        {
            var sucursal = i % 3;
            var analistas = AnalistasDe(sucursal);
            var analista = analistas[i % analistas.Length];
            var cliente = i % Clientes.Length;
            plan.Add(new SolicitudPlan(i + 1, estado, etapa, mora, sucursal, analista, cliente));
            i++;
        }
        for (var n = 0; n < 5; n++) Add("BORRADOR", null, null);
        for (var n = 0; n < 2; n++) Add("DOCUMENTACION", null, null);
        for (var n = 0; n < 2; n++) Add("VALIDACION", null, null);
        for (var n = 0; n < 4; n++) Add("ANALISIS", null, null);
        for (var n = 0; n < 4; n++) Add("REVISION", "JEFE_PENDIENTE", null);
        for (var n = 0; n < 2; n++) Add("REVISION", "GERENCIA_PENDIENTE", null);
        for (var n = 0; n < 3; n++) Add("APROBADA", "APROBADA", null);
        Add("APROBADA", "APROBADA", "15");
        Add("APROBADA", "APROBADA", "15");
        Add("APROBADA", "APROBADA", "30");
        Add("APROBADA", "APROBADA", "30");
        Add("APROBADA", "APROBADA", "60");
        Add("APROBADA", "APROBADA", "60");
        Add("APROBADA", "APROBADA", "90");
        for (var n = 0; n < 3; n++) Add("RECHAZADA", "RECHAZADA", null);
        return plan;
    }
}
