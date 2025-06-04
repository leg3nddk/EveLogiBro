using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EveLogiBro.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LogiSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    StartTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    SystemName = table.Column<string>(type: "TEXT", nullable: false),
                    SystemSecurity = table.Column<string>(type: "TEXT", nullable: false),
                    RegionName = table.Column<string>(type: "TEXT", nullable: false),
                    YourShipType = table.Column<string>(type: "TEXT", nullable: false),
                    TotalOutgoingReps = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalIncomingReps = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalShieldReps = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalArmorReps = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalIskValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AverageRepsPerSecond = table.Column<double>(type: "REAL", nullable: false),
                    PeakRepsPerSecond = table.Column<double>(type: "REAL", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    EngagementType = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogiSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RepairEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TargetName = table.Column<string>(type: "TEXT", nullable: false),
                    TargetCorporation = table.Column<string>(type: "TEXT", nullable: false),
                    TargetAlliance = table.Column<string>(type: "TEXT", nullable: false),
                    TargetShipType = table.Column<string>(type: "TEXT", nullable: false),
                    RepairType = table.Column<string>(type: "TEXT", nullable: false),
                    Amount = table.Column<int>(type: "INTEGER", nullable: false),
                    LogiPilot = table.Column<string>(type: "TEXT", nullable: false),
                    LogiCorporation = table.Column<string>(type: "TEXT", nullable: false),
                    LogiAlliance = table.Column<string>(type: "TEXT", nullable: false),
                    LogiShipType = table.Column<string>(type: "TEXT", nullable: false),
                    RepairModule = table.Column<string>(type: "TEXT", nullable: false),
                    SystemName = table.Column<string>(type: "TEXT", nullable: false),
                    SystemSecurity = table.Column<string>(type: "TEXT", nullable: false),
                    IskValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SessionId = table.Column<int>(type: "INTEGER", nullable: false),
                    Direction = table.Column<string>(type: "TEXT", nullable: false),
                    DistanceToTarget = table.Column<double>(type: "REAL", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RepairEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RepairEvents_LogiSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "LogiSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RepairEvents_SessionId",
                table: "RepairEvents",
                column: "SessionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RepairEvents");

            migrationBuilder.DropTable(
                name: "LogiSessions");
        }
    }
}
