using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RenderVN.CoreApi.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOwnershipConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RenderJobs_Projects_ProjectId",
                table: "RenderJobs");

            migrationBuilder.DropForeignKey(
                name: "FK_RenderJobs_SourceImages_SourceImageId",
                table: "RenderJobs");

            migrationBuilder.DropForeignKey(
                name: "FK_RenderResults_RenderJobs_RenderJobId",
                table: "RenderResults");

            migrationBuilder.DropForeignKey(
                name: "FK_SourceImages_Projects_ProjectId",
                table: "SourceImages");

            migrationBuilder.DropIndex(
                name: "IX_SourceImages_ProjectId",
                table: "SourceImages");

            migrationBuilder.DropIndex(
                name: "IX_RenderJobs_ProjectId",
                table: "RenderJobs");

            migrationBuilder.DropIndex(
                name: "IX_RenderJobs_SourceImageId",
                table: "RenderJobs");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_SourceImages_Id_UserId",
                table: "SourceImages",
                columns: new[] { "Id", "UserId" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_RenderJobs_Id_UserId",
                table: "RenderJobs",
                columns: new[] { "Id", "UserId" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Projects_Id_UserId",
                table: "Projects",
                columns: new[] { "Id", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_SourceImages_ProjectId_UserId",
                table: "SourceImages",
                columns: new[] { "ProjectId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_RenderResults_RenderJobId_UserId",
                table: "RenderResults",
                columns: new[] { "RenderJobId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RenderJobs_ProjectId_UserId",
                table: "RenderJobs",
                columns: new[] { "ProjectId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_RenderJobs_SourceImageId_UserId",
                table: "RenderJobs",
                columns: new[] { "SourceImageId", "UserId" });

            migrationBuilder.AddForeignKey(
                name: "FK_RenderJobs_Projects_ProjectId_UserId",
                table: "RenderJobs",
                columns: new[] { "ProjectId", "UserId" },
                principalTable: "Projects",
                principalColumns: new[] { "Id", "UserId" });

            migrationBuilder.AddForeignKey(
                name: "FK_RenderJobs_SourceImages_SourceImageId_UserId",
                table: "RenderJobs",
                columns: new[] { "SourceImageId", "UserId" },
                principalTable: "SourceImages",
                principalColumns: new[] { "Id", "UserId" });

            migrationBuilder.AddForeignKey(
                name: "FK_RenderResults_RenderJobs_RenderJobId_UserId",
                table: "RenderResults",
                columns: new[] { "RenderJobId", "UserId" },
                principalTable: "RenderJobs",
                principalColumns: new[] { "Id", "UserId" });

            migrationBuilder.AddForeignKey(
                name: "FK_SourceImages_Projects_ProjectId_UserId",
                table: "SourceImages",
                columns: new[] { "ProjectId", "UserId" },
                principalTable: "Projects",
                principalColumns: new[] { "Id", "UserId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RenderJobs_Projects_ProjectId_UserId",
                table: "RenderJobs");

            migrationBuilder.DropForeignKey(
                name: "FK_RenderJobs_SourceImages_SourceImageId_UserId",
                table: "RenderJobs");

            migrationBuilder.DropForeignKey(
                name: "FK_RenderResults_RenderJobs_RenderJobId_UserId",
                table: "RenderResults");

            migrationBuilder.DropForeignKey(
                name: "FK_SourceImages_Projects_ProjectId_UserId",
                table: "SourceImages");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_SourceImages_Id_UserId",
                table: "SourceImages");

            migrationBuilder.DropIndex(
                name: "IX_SourceImages_ProjectId_UserId",
                table: "SourceImages");

            migrationBuilder.DropIndex(
                name: "IX_RenderResults_RenderJobId_UserId",
                table: "RenderResults");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_RenderJobs_Id_UserId",
                table: "RenderJobs");

            migrationBuilder.DropIndex(
                name: "IX_RenderJobs_ProjectId_UserId",
                table: "RenderJobs");

            migrationBuilder.DropIndex(
                name: "IX_RenderJobs_SourceImageId_UserId",
                table: "RenderJobs");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Projects_Id_UserId",
                table: "Projects");

            migrationBuilder.CreateIndex(
                name: "IX_SourceImages_ProjectId",
                table: "SourceImages",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_RenderJobs_ProjectId",
                table: "RenderJobs",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_RenderJobs_SourceImageId",
                table: "RenderJobs",
                column: "SourceImageId");

            migrationBuilder.AddForeignKey(
                name: "FK_RenderJobs_Projects_ProjectId",
                table: "RenderJobs",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RenderJobs_SourceImages_SourceImageId",
                table: "RenderJobs",
                column: "SourceImageId",
                principalTable: "SourceImages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RenderResults_RenderJobs_RenderJobId",
                table: "RenderResults",
                column: "RenderJobId",
                principalTable: "RenderJobs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SourceImages_Projects_ProjectId",
                table: "SourceImages",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
