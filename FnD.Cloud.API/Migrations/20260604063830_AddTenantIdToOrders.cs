using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FnD.Cloud.API.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantIdToOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderItems_Orders_CloudOrderId",
                table: "OrderItems");

            migrationBuilder.DropPrimaryKey(
                name: "PK_OrderItems",
                table: "OrderItems");

            migrationBuilder.RenameTable(
                name: "OrderItems",
                newName: "CloudOrderItem");

            migrationBuilder.RenameIndex(
                name: "IX_OrderItems_CloudOrderId",
                table: "CloudOrderItem",
                newName: "IX_CloudOrderItem_CloudOrderId");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "Products",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "OrderId",
                table: "CloudOrderItem",
                type: "int",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_CloudOrderItem",
                table: "CloudOrderItem",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "Order",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LocalOrderId = table.Column<int>(type: "int", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OrderDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Order", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CloudOrderItem_OrderId",
                table: "CloudOrderItem",
                column: "OrderId");

            migrationBuilder.AddForeignKey(
                name: "FK_CloudOrderItem_Order_OrderId",
                table: "CloudOrderItem",
                column: "OrderId",
                principalTable: "Order",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CloudOrderItem_Orders_CloudOrderId",
                table: "CloudOrderItem",
                column: "CloudOrderId",
                principalTable: "Orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CloudOrderItem_Order_OrderId",
                table: "CloudOrderItem");

            migrationBuilder.DropForeignKey(
                name: "FK_CloudOrderItem_Orders_CloudOrderId",
                table: "CloudOrderItem");

            migrationBuilder.DropTable(
                name: "Order");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CloudOrderItem",
                table: "CloudOrderItem");

            migrationBuilder.DropIndex(
                name: "IX_CloudOrderItem_OrderId",
                table: "CloudOrderItem");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "OrderId",
                table: "CloudOrderItem");

            migrationBuilder.RenameTable(
                name: "CloudOrderItem",
                newName: "OrderItems");

            migrationBuilder.RenameIndex(
                name: "IX_CloudOrderItem_CloudOrderId",
                table: "OrderItems",
                newName: "IX_OrderItems_CloudOrderId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_OrderItems",
                table: "OrderItems",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderItems_Orders_CloudOrderId",
                table: "OrderItems",
                column: "CloudOrderId",
                principalTable: "Orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
