using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StartEvent_API.Migrations
{
    /// <inheritdoc />
    public partial class AddRowVersionToEvent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Events",
                type: "longblob",
                rowVersion: true,
                nullable: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Events");
        }
    }
}
