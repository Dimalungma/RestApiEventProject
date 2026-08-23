using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EventsService.Infrastructure.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class InitialEventsDatabase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BookingReservations",
                columns: table => new
                {
                    BookingId = table.Column<long>(type: "bigint", nullable: false),
                    EventId = table.Column<int>(type: "integer", nullable: false),
                    SeatsCount = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingReservations", x => x.BookingId);
                });

            migrationBuilder.CreateTable(
                name: "Events",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    StartAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TotalSeats = table.Column<int>(type: "integer", nullable: false),
                    AvailableSeats = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Events", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BookingReservations");

            migrationBuilder.DropTable(
                name: "Events");
        }
    }
}
