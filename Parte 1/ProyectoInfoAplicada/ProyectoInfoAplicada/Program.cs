using Hangfire;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.Data.SqlClient;
using ProyectoInfoAplicada.Repository;
using ProyectoInfoAplicada.Services;
using System.Data;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

//Obtener el string con el SQLSERVER para hangFire
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");


builder.Services.AddTransient<IDbConnection>(sp => new SqlConnection(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();


//registrar servicios

builder.Services.AddSingleton<ILoggerService, LoggerService>();
builder.Services.AddScoped<ICustomerRepository, CustomerRepositoryDapper>();
builder.Services.AddScoped<ISendPdfEnpointService, SendPdfEndpointService>();

builder.Services.AddAuthentication(NegotiateDefaults.AuthenticationScheme)
   .AddNegotiate();

builder.Services.AddAuthorization(options =>
{
    // By default, all incoming requests will be authorized according to the default policy.
    options.FallbackPolicy = options.DefaultPolicy;
});

/////////////////////////////////////////////////////////////////////////////////////
// configuracion para el arranque de Hangfire usando (SQL Server storage)
builder.Services.AddHangfire(configuration =>
    configuration
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UseSqlServerStorage(connectionString, new SqlServerStorageOptions
        {
            CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
            SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
            QueuePollInterval = TimeSpan.Zero,
            UseRecommendedIsolationLevel = true,
            DisableGlobalLocks = true
        }));
/////////////////////////////////////////////////////////////////////////////////////

builder.Services.AddHangfireServer();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Montar dashboard(UI) en /hangfire
app.UseHangfireDashboard("/hangfire"); //
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
