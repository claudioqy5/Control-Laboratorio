using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ControlLaboratorio.API.Migrations
{
    public partial class AgregarConferenciasMedicas : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Multas_Prestamos_PrestamoID1",
                table: "Multas");

            migrationBuilder.DropForeignKey(
                name: "FK_Prestamos_Libros_LibroID1",
                table: "Prestamos");

            migrationBuilder.DropIndex(
                name: "IX_Prestamos_LibroID1",
                table: "Prestamos");

            migrationBuilder.DropIndex(
                name: "IX_Multas_PrestamoID1",
                table: "Multas");

            migrationBuilder.DropColumn(
                name: "LibroID1",
                table: "Prestamos");

            migrationBuilder.DropColumn(
                name: "PrestamoID1",
                table: "Multas");

            migrationBuilder.CreateTable(
                name: "t_conferencia_medica",
                columns: table => new
                {
                    id_conferencia = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    titulo_evento = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    expositor_ponente = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    entidad_editorial = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    fecha_hora_inicio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    fecha_hora_fin = table.Column<DateTime>(type: "datetime2", nullable: false),
                    modalidad = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ubicacion_o_enlace = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    asistencia_abierta = table.Column<bool>(type: "bit", nullable: false),
                    estado_evento = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    auto_purgar_30_dias = table.Column<bool>(type: "bit", nullable: false),
                    fecha_caducidad_purge = table.Column<DateTime>(type: "datetime2", nullable: true),
                    fecha_creacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_conferencia_medica", x => x.id_conferencia);
                });

            migrationBuilder.CreateTable(
                name: "UsuariosAdmin",
                columns: table => new
                {
                    IdUsuarioAdmin = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Nombres = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Correo = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Rol = table.Column<byte>(type: "tinyint", nullable: false),
                    EstadoActivo = table.Column<bool>(type: "bit", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UltimoAcceso = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsuariosAdmin", x => x.IdUsuarioAdmin);
                });

            migrationBuilder.CreateTable(
                name: "PARTICIPACIONES_CONFERENCIA",
                columns: table => new
                {
                    IdParticipacion = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdConferencia = table.Column<int>(type: "int", nullable: false),
                    TipoParticipante = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TipoDocumento = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    NumeroDocumento = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CodigoURP = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Nombres = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ApellidoPaterno = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ApellidoMaterno = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Correo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FechaHoraInscripcion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaHoraAsistencia = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EstadoParticipacion = table.Column<byte>(type: "tinyint", nullable: false),
                    AlumnoID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PARTICIPACIONES_CONFERENCIA", x => x.IdParticipacion);
                    table.ForeignKey(
                        name: "FK_PARTICIPACIONES_CONFERENCIA_Alumnos_AlumnoID",
                        column: x => x.AlumnoID,
                        principalTable: "Alumnos",
                        principalColumn: "AlumnoID",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PARTICIPACIONES_CONFERENCIA_t_conferencia_medica_IdConferencia",
                        column: x => x.IdConferencia,
                        principalTable: "t_conferencia_medica",
                        principalColumn: "id_conferencia",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Token = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    IdUsuarioAdmin = table.Column<int>(type: "int", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReplacedByToken = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_UsuariosAdmin_IdUsuarioAdmin",
                        column: x => x.IdUsuarioAdmin,
                        principalTable: "UsuariosAdmin",
                        principalColumn: "IdUsuarioAdmin",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PARTICIPACIONES_CONFERENCIA_AlumnoID",
                table: "PARTICIPACIONES_CONFERENCIA",
                column: "AlumnoID");

            migrationBuilder.CreateIndex(
                name: "IX_PARTICIPACIONES_CONFERENCIA_IdConferencia_NumeroDocumento",
                table: "PARTICIPACIONES_CONFERENCIA",
                columns: new[] { "IdConferencia", "NumeroDocumento" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PARTICIPACIONES_CONFERENCIA_IdConferencia_NumeroDocumento_CodigoURP",
                table: "PARTICIPACIONES_CONFERENCIA",
                columns: new[] { "IdConferencia", "NumeroDocumento", "CodigoURP" });

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_IdUsuarioAdmin",
                table: "RefreshTokens",
                column: "IdUsuarioAdmin");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_Token",
                table: "RefreshTokens",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UsuariosAdmin_Username",
                table: "UsuariosAdmin",
                column: "Username",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PARTICIPACIONES_CONFERENCIA");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "t_conferencia_medica");

            migrationBuilder.DropTable(
                name: "UsuariosAdmin");

            migrationBuilder.AddColumn<int>(
                name: "LibroID1",
                table: "Prestamos",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PrestamoID1",
                table: "Multas",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Prestamos_LibroID1",
                table: "Prestamos",
                column: "LibroID1");

            migrationBuilder.CreateIndex(
                name: "IX_Multas_PrestamoID1",
                table: "Multas",
                column: "PrestamoID1");

            migrationBuilder.AddForeignKey(
                name: "FK_Multas_Prestamos_PrestamoID1",
                table: "Multas",
                column: "PrestamoID1",
                principalTable: "Prestamos",
                principalColumn: "PrestamoID");

            migrationBuilder.AddForeignKey(
                name: "FK_Prestamos_Libros_LibroID1",
                table: "Prestamos",
                column: "LibroID1",
                principalTable: "Libros",
                principalColumn: "LibroID");
        }
    }
}
