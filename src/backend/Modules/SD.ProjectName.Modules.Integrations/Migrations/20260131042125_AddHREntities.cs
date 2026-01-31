using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SD.ProjectName.Modules.Integrations.Migrations
{
    /// <inheritdoc />
    public partial class AddHREntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HREntities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConnectorId = table.Column<int>(type: "int", nullable: false),
                    ExternalId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Data = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MappedData = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsApproved = table.Column<bool>(type: "bit", nullable: false),
                    ImportedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HREntities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HREntities_Connectors_ConnectorId",
                        column: x => x.ConnectorId,
                        principalTable: "Connectors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HRSyncRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConnectorId = table.Column<int>(type: "int", nullable: false),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ExternalId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    RawData = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    OverwroteApprovedData = table.Column<bool>(type: "bit", nullable: false),
                    SyncedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    InitiatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    HREntityId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HRSyncRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HRSyncRecords_Connectors_ConnectorId",
                        column: x => x.ConnectorId,
                        principalTable: "Connectors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HRSyncRecords_HREntities_HREntityId",
                        column: x => x.HREntityId,
                        principalTable: "HREntities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HREntities_ConnectorId",
                table: "HREntities",
                column: "ConnectorId");

            migrationBuilder.CreateIndex(
                name: "IX_HREntities_ConnectorId_ExternalId",
                table: "HREntities",
                columns: new[] { "ConnectorId", "ExternalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HREntities_EntityType",
                table: "HREntities",
                column: "EntityType");

            migrationBuilder.CreateIndex(
                name: "IX_HREntities_IsApproved",
                table: "HREntities",
                column: "IsApproved");

            migrationBuilder.CreateIndex(
                name: "IX_HRSyncRecords_ConnectorId",
                table: "HRSyncRecords",
                column: "ConnectorId");

            migrationBuilder.CreateIndex(
                name: "IX_HRSyncRecords_CorrelationId",
                table: "HRSyncRecords",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_HRSyncRecords_HREntityId",
                table: "HRSyncRecords",
                column: "HREntityId");

            migrationBuilder.CreateIndex(
                name: "IX_HRSyncRecords_Status",
                table: "HRSyncRecords",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_HRSyncRecords_SyncedAt",
                table: "HRSyncRecords",
                column: "SyncedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HRSyncRecords");

            migrationBuilder.DropTable(
                name: "HREntities");
        }
    }
}
