using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ControlLaboratorio.API.Migrations
{
    /// <inheritdoc />
    public partial class AddAliasToEquipo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Alumnos",
                columns: table => new
                {
                    AlumnoID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CodigoUniversitario = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DNI = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    Nombres = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ApellidoPaterno = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ApellidoMaterno = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Carrera = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Telefono = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CorreoInstitucional = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CorreoPersonal = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Estado = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Alumnos", x => x.AlumnoID);
                });

            migrationBuilder.CreateTable(
                name: "Equipos",
                columns: table => new
                {
                    EquipoID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NombreRed = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Ubicacion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Estado = table.Column<bool>(type: "bit", nullable: false),
                    PosicionMapa = table.Column<int>(type: "int", nullable: true),
                    Alias = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    AgentVersion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AgentVersionFecha = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Equipos", x => x.EquipoID);
                });

            migrationBuilder.CreateTable(
                name: "Sesiones",
                columns: table => new
                {
                    SesionID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AlumnoID = table.Column<int>(type: "int", nullable: false),
                    EquipoID = table.Column<int>(type: "int", nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    HoraInicio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    HoraFin = table.Column<DateTime>(type: "datetime2", nullable: true),
                    HoraLimite = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sesiones", x => x.SesionID);
                    table.ForeignKey(
                        name: "FK_Sesiones_Alumnos_AlumnoID",
                        column: x => x.AlumnoID,
                        principalTable: "Alumnos",
                        principalColumn: "AlumnoID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Sesiones_Equipos_EquipoID",
                        column: x => x.EquipoID,
                        principalTable: "Equipos",
                        principalColumn: "EquipoID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Alumnos_CodigoUniversitario",
                table: "Alumnos",
                column: "CodigoUniversitario",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Equipos_NombreRed",
                table: "Equipos",
                column: "NombreRed",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sesiones_AlumnoID",
                table: "Sesiones",
                column: "AlumnoID");

            migrationBuilder.CreateIndex(
                name: "IX_Sesiones_EquipoID",
                table: "Sesiones",
                column: "EquipoID");

            migrationBuilder.CreateIndex(
                name: "IX_Sesiones_Fecha",
                table: "Sesiones",
                column: "Fecha");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Sesiones");

            migrationBuilder.DropTable(
                name: "Alumnos");

            migrationBuilder.DropTable(
                name: "Equipos");
        }
    }
}
