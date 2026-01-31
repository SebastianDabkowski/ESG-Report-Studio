using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SD.ProjectName.Modules.Integrations.Migrations
{
    /// <inheritdoc />
    public partial class AddIntegrationJobMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IntegrationJobMetadata",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JobId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ConnectorId = table.Column<int>(type: "int", nullable: false),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    JobType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DurationMs = table.Column<long>(type: "bigint", nullable: true),
                    TotalRecords = table.Column<int>(type: "int", nullable: false),
                    SuccessCount = table.Column<int>(type: "int", nullable: false),
                    FailureCount = table.Column<int>(type: "int", nullable: false),
                    SkippedCount = table.Column<int>(type: "int", nullable: false),
                    ErrorSummary = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InitiatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrationJobMetadata", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IntegrationJobMetadata_Connectors_ConnectorId",
                        column: x => x.ConnectorId,
                        principalTable: "Connectors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationJobMetadata_ConnectorId",
                table: "IntegrationJobMetadata",
                column: "ConnectorId");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationJobMetadata_CorrelationId",
                table: "IntegrationJobMetadata",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationJobMetadata_JobId",
                table: "IntegrationJobMetadata",
                column: "JobId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationJobMetadata_JobType",
                table: "IntegrationJobMetadata",
                column: "JobType");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationJobMetadata_StartedAt",
                table: "IntegrationJobMetadata",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationJobMetadata_Status",
                table: "IntegrationJobMetadata",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IntegrationJobMetadata");
        }
    }
}
