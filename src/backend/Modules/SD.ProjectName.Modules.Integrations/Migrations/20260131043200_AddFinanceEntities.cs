using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SD.ProjectName.Modules.Integrations.Migrations
{
    /// <inheritdoc />
    public partial class AddFinanceEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FinanceEntities",
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
                    SourceSystem = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ExtractTimestamp = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ImportJobId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ImportedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceEntities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinanceEntities_Connectors_ConnectorId",
                        column: x => x.ConnectorId,
                        principalTable: "Connectors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FinanceSyncRecords",
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
                    ConflictDetected = table.Column<bool>(type: "bit", nullable: false),
                    ConflictResolution = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ApprovedOverrideBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SyncedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    InitiatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FinanceEntityId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceSyncRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinanceSyncRecords_Connectors_ConnectorId",
                        column: x => x.ConnectorId,
                        principalTable: "Connectors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinanceSyncRecords_FinanceEntities_FinanceEntityId",
                        column: x => x.FinanceEntityId,
                        principalTable: "FinanceEntities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceEntities_ConnectorId",
                table: "FinanceEntities",
                column: "ConnectorId");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceEntities_ConnectorId_ExternalId",
                table: "FinanceEntities",
                columns: new[] { "ConnectorId", "ExternalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinanceEntities_EntityType",
                table: "FinanceEntities",
                column: "EntityType");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceEntities_IsApproved",
                table: "FinanceEntities",
                column: "IsApproved");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceEntities_ImportJobId",
                table: "FinanceEntities",
                column: "ImportJobId");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceSyncRecords_ConnectorId",
                table: "FinanceSyncRecords",
                column: "ConnectorId");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceSyncRecords_CorrelationId",
                table: "FinanceSyncRecords",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceSyncRecords_FinanceEntityId",
                table: "FinanceSyncRecords",
                column: "FinanceEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceSyncRecords_Status",
                table: "FinanceSyncRecords",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceSyncRecords_SyncedAt",
                table: "FinanceSyncRecords",
                column: "SyncedAt");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceSyncRecords_ConflictDetected",
                table: "FinanceSyncRecords",
                column: "ConflictDetected");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FinanceSyncRecords");

            migrationBuilder.DropTable(
                name: "FinanceEntities");
        }
    }
}
