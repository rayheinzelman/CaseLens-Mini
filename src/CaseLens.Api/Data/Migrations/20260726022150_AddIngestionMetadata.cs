using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CaseLens.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddIngestionMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "content_hash",
                table: "legal_documents",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "page_count",
                table: "legal_documents",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_legal_documents_source_file_name",
                table: "legal_documents",
                column: "source_file_name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_legal_documents_source_file_name",
                table: "legal_documents");

            migrationBuilder.DropColumn(
                name: "content_hash",
                table: "legal_documents");

            migrationBuilder.DropColumn(
                name: "page_count",
                table: "legal_documents");
        }
    }
}
