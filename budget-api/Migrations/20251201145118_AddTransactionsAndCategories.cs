using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace budget_api.Migrations
{
    /// <inheritdoc />
    public partial class AddTransactionsAndCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BudgetTransactions_Budgets_BudgetId",
                table: "BudgetTransactions");

            migrationBuilder.DropIndex(
                name: "IX_BudgetTransactions_BudgetId",
                table: "BudgetTransactions");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "BudgetTransactions",
                newName: "Title");

            migrationBuilder.AddColumn<int>(
                name: "CategoryId",
                table: "BudgetTransactions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "Date",
                table: "BudgetTransactions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "EndDate",
                table: "BudgetTransactions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Frequency",
                table: "BudgetTransactions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PaymentMethod",
                table: "BudgetTransactions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReceiptImageUrl",
                table: "BudgetTransactions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "BudgetTransactions",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "Jedzenie" },
                    { 2, "Mieszkanie / Rachunki" },
                    { 3, "Transport" },
                    { 4, "Telekomunikacja" },
                    { 5, "Opieka zdrowotna" },
                    { 6, "Ubranie" },
                    { 7, "Higiena" },
                    { 8, "Dzieci" },
                    { 9, "Rozrywka" },
                    { 10, "Edukacja" },
                    { 11, "Spłata długów" },
                    { 12, "Inne" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_BudgetTransactions_CategoryId",
                table: "BudgetTransactions",
                column: "CategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_BudgetTransactions_Categories_CategoryId",
                table: "BudgetTransactions",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BudgetTransactions_Categories_CategoryId",
                table: "BudgetTransactions");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_BudgetTransactions_CategoryId",
                table: "BudgetTransactions");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "BudgetTransactions");

            migrationBuilder.DropColumn(
                name: "Date",
                table: "BudgetTransactions");

            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "BudgetTransactions");

            migrationBuilder.DropColumn(
                name: "Frequency",
                table: "BudgetTransactions");

            migrationBuilder.DropColumn(
                name: "PaymentMethod",
                table: "BudgetTransactions");

            migrationBuilder.DropColumn(
                name: "ReceiptImageUrl",
                table: "BudgetTransactions");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "BudgetTransactions");

            migrationBuilder.RenameColumn(
                name: "Title",
                table: "BudgetTransactions",
                newName: "Description");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetTransactions_BudgetId",
                table: "BudgetTransactions",
                column: "BudgetId");

            migrationBuilder.AddForeignKey(
                name: "FK_BudgetTransactions_Budgets_BudgetId",
                table: "BudgetTransactions",
                column: "BudgetId",
                principalTable: "Budgets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
