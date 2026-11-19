using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace IspAppApi.Migrations
{
    /// <inheritdoc />
    public partial class AddSubscriptionIdToBill : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Bills",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Payments",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Bills",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Subscriptions");

            migrationBuilder.AlterColumn<DateTime>(
                name: "EndDate",
                table: "Subscriptions",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AddColumn<int>(
                name: "SubscriptionId",
                table: "Bills",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Bills_SubscriptionId",
                table: "Bills",
                column: "SubscriptionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Bills_Subscriptions_SubscriptionId",
                table: "Bills",
                column: "SubscriptionId",
                principalTable: "Subscriptions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bills_Subscriptions_SubscriptionId",
                table: "Bills");

            migrationBuilder.DropIndex(
                name: "IX_Bills_SubscriptionId",
                table: "Bills");

            migrationBuilder.DropColumn(
                name: "SubscriptionId",
                table: "Bills");

            migrationBuilder.AlterColumn<DateTime>(
                name: "EndDate",
                table: "Subscriptions",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Subscriptions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.InsertData(
                table: "Bills",
                columns: new[] { "Id", "CreatedDate", "CustomerId", "DueDate", "PaymentStatus", "TotalAmount" },
                values: new object[,]
                {
                    { 1, new DateTime(2024, 1, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), 1, new DateTime(2024, 1, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), "Paid", 20m },
                    { 2, new DateTime(2024, 1, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), 1, new DateTime(2024, 1, 30, 0, 0, 0, 0, DateTimeKind.Unspecified), "Unpaid", 40m }
                });

            migrationBuilder.InsertData(
                table: "Payments",
                columns: new[] { "Id", "Amount", "BillId", "CancellationReason", "CancelledAt", "IsCancelled", "PaymentDate" },
                values: new object[] { 1, 20m, 1, null, null, false, new DateTime(2024, 1, 10, 0, 0, 0, 0, DateTimeKind.Unspecified) });
        }
    }
}
