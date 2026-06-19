using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ControlLaboratorio.API.Migrations
{
    /// <inheritdoc />
    public partial class UpdateScanLogsModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AlumnoCodigo",
                table: "ScanLogs",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AlumnoNombre",
                table: "ScanLogs",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsExitoso",
                table: "ScanLogs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Mensaje",
                table: "ScanLogs",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AlumnoCodigo",
                table: "ScanLogs");

            migrationBuilder.DropColumn(
                name: "AlumnoNombre",
                table: "ScanLogs");

            migrationBuilder.DropColumn(
                name: "IsExitoso",
                table: "ScanLogs");

            migrationBuilder.DropColumn(
                name: "Mensaje",
                table: "ScanLogs");
        }
    }
}
