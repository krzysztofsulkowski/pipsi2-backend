using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace budget_api.Migrations
{
    /// <inheritdoc />
    public partial class AddUserRelationAndFixFK : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BudgetTransactions_Categories_CategoryId",
                table: "BudgetTransactions");
                       
            migrationBuilder.CreateIndex(
                name: "IX_BudgetTransactions_CreatedByUserId",
                table: "BudgetTransactions",
                column: "CreatedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_BudgetTransactions_AspNetUsers_CreatedByUserId",
                table: "BudgetTransactions",
                column: "CreatedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_BudgetTransactions_Categories_CategoryId",
                table: "BudgetTransactions",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BudgetTransactions_AspNetUsers_CreatedByUserId",
                table: "BudgetTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_BudgetTransactions_Categories_CategoryId",
                table: "BudgetTransactions");

            migrationBuilder.DropIndex(
                name: "IX_BudgetTransactions_CreatedByUserId",
                table: "BudgetTransactions");
                       
            migrationBuilder.AddForeignKey(
                name: "FK_BudgetTransactions_Categories_CategoryId",
                table: "BudgetTransactions",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id");
        }
    }
}
