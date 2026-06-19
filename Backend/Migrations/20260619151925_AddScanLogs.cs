using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ControlLaboratorio.API.Migrations
{
    /// <inheritdoc />
    public partial class AddScanLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Multas_Prestamos_PrestamoID",
                table: "Multas");

            migrationBuilder.DropForeignKey(
                name: "FK_Prestamos_Libros_LibroID",
                table: "Prestamos");

            migrationBuilder.DropIndex(
                name: "IX_Libros_NroRegistro",
                table: "Libros");

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

            migrationBuilder.CreateTable(
                name: "Categorias",
                columns: table => new
                {
                    CategoriaID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Codigo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categorias", x => x.CategoriaID);
                });

            migrationBuilder.CreateTable(
                name: "ScanLogs",
                columns: table => new
                {
                    ScanLogID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RealizadoPor = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScanLogs", x => x.ScanLogID);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Prestamos_LibroID1",
                table: "Prestamos",
                column: "LibroID1");

            migrationBuilder.CreateIndex(
                name: "IX_Multas_PrestamoID1",
                table: "Multas",
                column: "PrestamoID1");

            migrationBuilder.CreateIndex(
                name: "IX_Libros_NroRegistro",
                table: "Libros",
                column: "NroRegistro");

            migrationBuilder.AddForeignKey(
                name: "FK_Multas_Prestamos_PrestamoID",
                table: "Multas",
                column: "PrestamoID",
                principalTable: "Prestamos",
                principalColumn: "PrestamoID");

            migrationBuilder.AddForeignKey(
                name: "FK_Multas_Prestamos_PrestamoID1",
                table: "Multas",
                column: "PrestamoID1",
                principalTable: "Prestamos",
                principalColumn: "PrestamoID");

            migrationBuilder.AddForeignKey(
                name: "FK_Prestamos_Libros_LibroID",
                table: "Prestamos",
                column: "LibroID",
                principalTable: "Libros",
                principalColumn: "LibroID");

            migrationBuilder.AddForeignKey(
                name: "FK_Prestamos_Libros_LibroID1",
                table: "Prestamos",
                column: "LibroID1",
                principalTable: "Libros",
                principalColumn: "LibroID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Multas_Prestamos_PrestamoID",
                table: "Multas");

            migrationBuilder.DropForeignKey(
                name: "FK_Multas_Prestamos_PrestamoID1",
                table: "Multas");

            migrationBuilder.DropForeignKey(
                name: "FK_Prestamos_Libros_LibroID",
                table: "Prestamos");

            migrationBuilder.DropForeignKey(
                name: "FK_Prestamos_Libros_LibroID1",
                table: "Prestamos");

            migrationBuilder.DropTable(
                name: "Categorias");

            migrationBuilder.DropTable(
                name: "ScanLogs");

            migrationBuilder.DropIndex(
                name: "IX_Prestamos_LibroID1",
                table: "Prestamos");

            migrationBuilder.DropIndex(
                name: "IX_Multas_PrestamoID1",
                table: "Multas");

            migrationBuilder.DropIndex(
                name: "IX_Libros_NroRegistro",
                table: "Libros");

            migrationBuilder.DropColumn(
                name: "LibroID1",
                table: "Prestamos");

            migrationBuilder.DropColumn(
                name: "PrestamoID1",
                table: "Multas");

            migrationBuilder.CreateIndex(
                name: "IX_Libros_NroRegistro",
                table: "Libros",
                column: "NroRegistro",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Multas_Prestamos_PrestamoID",
                table: "Multas",
                column: "PrestamoID",
                principalTable: "Prestamos",
                principalColumn: "PrestamoID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Prestamos_Libros_LibroID",
                table: "Prestamos",
                column: "LibroID",
                principalTable: "Libros",
                principalColumn: "LibroID",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
