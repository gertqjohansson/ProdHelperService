using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProdHelperService.Migrations
{
    /// <inheritdoc />
    public partial class AddEquipmentNode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EquipmentNodes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ParentId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EquipmentNodes");
        }
    }
}
