using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrepDiplomacia.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class TipoPublicacionNoticias : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Tipo",
                table: "Posts",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Tipo",
                table: "Posts");
        }
    }
}
