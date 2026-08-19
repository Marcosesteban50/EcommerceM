using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcommerceAPI.Migrations
{
    /// <inheritdoc />
    public partial class CambiosFavoritos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Favoritos_Productos_ProductoId",
                table: "Favoritos");

            migrationBuilder.DropIndex(
                name: "IX_Favoritos_ProductoId",
                table: "Favoritos");

            migrationBuilder.DropColumn(
                name: "ProductoId",
                table: "Favoritos");

            migrationBuilder.CreateTable(
                name: "FavoritoItems",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProductoId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FavoritoId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FavoritoItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FavoritoItems_Favoritos_FavoritoId",
                        column: x => x.FavoritoId,
                        principalTable: "Favoritos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FavoritoItems_Productos_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "Productos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FavoritoItems_FavoritoId",
                table: "FavoritoItems",
                column: "FavoritoId");

            migrationBuilder.CreateIndex(
                name: "IX_FavoritoItems_ProductoId",
                table: "FavoritoItems",
                column: "ProductoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FavoritoItems");

            migrationBuilder.AddColumn<string>(
                name: "ProductoId",
                table: "Favoritos",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Favoritos_ProductoId",
                table: "Favoritos",
                column: "ProductoId");

            migrationBuilder.AddForeignKey(
                name: "FK_Favoritos_Productos_ProductoId",
                table: "Favoritos",
                column: "ProductoId",
                principalTable: "Productos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
