using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace budget_api.Migrations
{
    /// <inheritdoc />
    public partial class CreateUserBudgetsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserBudgets",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    BudgetId = table.Column<int>(type: "integer", nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserBudgets", x => new { x.UserId, x.BudgetId });
                    table.ForeignKey(
                        name: "FK_UserBudgets_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserBudgets_Budgets_BudgetId",
                        column: x => x.BudgetId,
                        principalTable: "Budgets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserBudgets_BudgetId",
                table: "UserBudgets",
                column: "BudgetId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserBudgets");
        }
    }
}
