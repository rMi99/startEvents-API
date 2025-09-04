using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StartEvent_API.Migrations
{
    /// <inheritdoc />
    public partial class EnhancedLoyaltyPointsSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PointsEarned",
                table: "Tickets",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PointsRedeemed",
                table: "Tickets",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "LoyaltyPointReservations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    CustomerId = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TicketId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ReservedPoints = table.Column<int>(type: "int", nullable: false),
                    ReservedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsConfirmed = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoyaltyPointReservations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LoyaltyPointReservations_AspNetUsers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LoyaltyPointReservations_Tickets_TicketId",
                        column: x => x.TicketId,
                        principalTable: "Tickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyPointReservations_CustomerId",
                table: "LoyaltyPointReservations",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyPointReservations_TicketId",
                table: "LoyaltyPointReservations",
                column: "TicketId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LoyaltyPointReservations");

            migrationBuilder.DropColumn(
                name: "PointsEarned",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "PointsRedeemed",
                table: "Tickets");
        }
    }
}
