using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CaseLens.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class MakeChunkEmbeddingNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<float[]>(
                name: "embedding",
                table: "document_chunks",
                type: "real[]",
                nullable: true,
                oldClrType: typeof(float[]),
                oldType: "real[]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<float[]>(
                name: "embedding",
                table: "document_chunks",
                type: "real[]",
                nullable: false,
                defaultValue: new float[0],
                oldClrType: typeof(float[]),
                oldType: "real[]",
                oldNullable: true);
        }
    }
}
