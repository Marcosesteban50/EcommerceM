using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcommerceAPI.Migrations
{
    /// <inheritdoc />
    public partial class CambiosCategoriaEnProductoHistorial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductoHistorial_Categorias_CategoriaId",
                table: "ProductoHistorial");

            migrationBuilder.DropIndex(
                name: "IX_ProductoHistorial_CategoriaId",
                table: "ProductoHistorial");

            migrationBuilder.AlterColumn<string>(
                name: "CategoriaId",
                table: "ProductoHistorial",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "CategoriaId",
                table: "ProductoHistorial",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductoHistorial_CategoriaId",
                table: "ProductoHistorial",
                column: "CategoriaId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductoHistorial_Categorias_CategoriaId",
                table: "ProductoHistorial",
                column: "CategoriaId",
                principalTable: "Categorias",
                principalColumn: "Id");
        }
    }
}
