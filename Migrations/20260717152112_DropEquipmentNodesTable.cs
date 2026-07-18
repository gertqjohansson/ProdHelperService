using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProdHelperService.Migrations
{
    /// <inheritdoc />
    public partial class DropEquipmentNodesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EquipmentNodes");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EquipmentNodes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ParentId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EquipmentNodes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentNodes_ParentId",
                table: "EquipmentNodes",
                column: "ParentId");
        }
    }
}
