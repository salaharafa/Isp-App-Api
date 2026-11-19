using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IspAppApi.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePaymentCancellationAndUniqueConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payments_Bills_BillId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_BillId",
                table: "Payments");

            migrationBuilder.AddColumn<string>(
                name: "CancellationReason",
                table: "Payments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CancelledAt",
                table: "Payments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCancelled",
                table: "Payments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "Bills",
                keyColumn: "Id",
                keyValue: 1,
                column: "PaymentStatus",
                value: "Paid");

            migrationBuilder.InsertData(
                table: "Bills",
                columns: new[] { "Id", "CreatedDate", "CustomerId", "DueDate", "PaymentStatus", "TotalAmount" },
                values: new object[] { 2, new DateTime(2024, 1, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), 1, new DateTime(2024, 1, 30, 0, 0, 0, 0, DateTimeKind.Unspecified), "Unpaid", 40m });

            migrationBuilder.UpdateData(
                table: "Payments",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CancellationReason", "CancelledAt", "IsCancelled" },
                values: new object[] { null, null, false });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_BillId",
                table: "Payments",
                column: "BillId",
                unique: true,
                filter: "[IsCancelled] = 0");

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_Bills_BillId",
                table: "Payments",
                column: "BillId",
                principalTable: "Bills",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payments_Bills_BillId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_BillId",
                table: "Payments");

            migrationBuilder.DeleteData(
                table: "Bills",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DropColumn(
                name: "CancellationReason",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "CancelledAt",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "IsCancelled",
                table: "Payments");

            migrationBuilder.UpdateData(
                table: "Bills",
                keyColumn: "Id",
                keyValue: 1,
                column: "PaymentStatus",
                value: "Unpaid");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_BillId",
                table: "Payments",
                column: "BillId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_Bills_BillId",
                table: "Payments",
                column: "BillId",
                principalTable: "Bills",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
