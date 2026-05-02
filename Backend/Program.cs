using ControlLaboratorio.API.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database Configuration
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("ControlLaboratorioConnection")));

// CORS Configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder => builder.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader());
});

var app = builder.Build();

// Auto-add column if not exists
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try
    {
        db.Database.ExecuteSqlRaw(@"
            IF COL_LENGTH('Sesiones', 'HoraLimite') IS NULL
            BEGIN
                ALTER TABLE Sesiones ADD HoraLimite DATETIME2 NULL
            END
            
            IF COL_LENGTH('Alumnos', 'Telefono') IS NULL
            BEGIN
                ALTER TABLE Alumnos ADD Telefono NVARCHAR(20) NULL
            END
            
            IF COL_LENGTH('Alumnos', 'CorreoInstitucional') IS NULL
            BEGIN
                ALTER TABLE Alumnos ADD CorreoInstitucional NVARCHAR(100) NULL
            END
            
            IF COL_LENGTH('Alumnos', 'CorreoPersonal') IS NULL
            BEGIN
                ALTER TABLE Alumnos ADD CorreoPersonal NVARCHAR(100) NULL
            END
        ");
    }
    catch { /* Ignore if it fails or already exists */ }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowAll");

app.UseAuthorization();

app.MapControllers();

app.Run();
