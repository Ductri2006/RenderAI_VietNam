using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RenderVN.CoreApi.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCreditWalletConcurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "CreditWallets",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Version",
                table: "CreditWallets");
        }
    }
}
