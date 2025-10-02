using Microsoft.EntityFrameworkCore;
using PDFGenerationServer.Models.DB;
using PDFGenerationServer.Data;
using PDFGenerationServer.Services;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
var builder = WebApplication.CreateBuilder(args);
//DATABASE
//Crea variable para la cadena de conexion
var connectionString = builder.Configuration.GetConnectionString("AdventureWorks");
//registra servicio  para la conexion
builder.Services.AddDbContext<AppDbContext>(options => 
options.UseSqlServer(connectionString));
// Registrar OrdersData y PdfReportService
builder.Services.AddScoped<OrdersData>();
builder.Services.AddScoped<PdfReportService>();
// Add services to the container.
QuestPDF.Settings.License = LicenseType.Community; //LICENCIA QUESTPDF
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
//Logs
builder.Services.AddSingleton<ILogProducer>(sp => new FileLogProducer()); 
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
