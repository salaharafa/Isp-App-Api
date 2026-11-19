using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace IspAppApi.Migrations
{
    /// <inheritdoc />
    public partial class addseeddata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Customers",
                columns: new[] { "Id", "BillingAddress", "Email", "IsActive", "Name", "ServiceAddress" },
                values: new object[] { 1, "Aleppo - Billing", "ahmad@gmail.com", true, "Ahmad Abohmid", "Aleppo - Service" });

            migrationBuilder.InsertData(
                table: "ServicePlans",
                columns: new[] { "Id", "IsActive", "Name", "Price", "Speed", "Status" },
                values: new object[,]
                {
                    { 1, true, "Internet 4G", 20m, 10, "Active" },
                    { 2, true, "Internet 5G", 40m, 50, "Active" }
                });

            migrationBuilder.InsertData(
                table: "Bills",
                columns: new[] { "Id", "CreatedDate", "CustomerId", "DueDate", "PaymentStatus", "TotalAmount" },
                values: new object[] { 1, new DateTime(2024, 1, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), 1, new DateTime(2024, 1, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), "Unpaid", 20m });

            migrationBuilder.InsertData(
                table: "Tickets",
                columns: new[] { "Id", "CreatedDate", "CustomerId", "Name", "Status" },
                values: new object[] { 1, new DateTime(2024, 1, 2, 0, 0, 0, 0, DateTimeKind.Unspecified), 1, "Internet not working", "New" });

            migrationBuilder.InsertData(
                table: "Payments",
                columns: new[] { "Id", "Amount", "BillId", "PaymentDate" },
                values: new object[] { 1, 20m, 1, new DateTime(2024, 1, 10, 0, 0, 0, 0, DateTimeKind.Unspecified) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Payments",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "ServicePlans",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "ServicePlans",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Tickets",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Bills",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Customers",
                keyColumn: "Id",
                keyValue: 1);
        }
    }
}
