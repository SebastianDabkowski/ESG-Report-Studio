using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SD.ProjectName.Modules.Integrations.Migrations
{
    /// <inheritdoc />
    public partial class AddCanonicalDataModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CanonicalEntityId",
                table: "HREntities",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CanonicalAttributes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EntityType = table.Column<int>(type: "int", nullable: false),
                    SchemaVersion = table.Column<int>(type: "int", nullable: false),
                    AttributeName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DataType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ExampleValues = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ValidationRules = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DefaultValue = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsDeprecated = table.Column<bool>(type: "bit", nullable: false),
                    DeprecatedInVersion = table.Column<int>(type: "int", nullable: true),
                    ReplacedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CanonicalAttributes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CanonicalEntityVersions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EntityType = table.Column<int>(type: "int", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    SchemaDefinition = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeprecated = table.Column<bool>(type: "bit", nullable: false),
                    BackwardCompatibleWithVersion = table.Column<int>(type: "int", nullable: true),
                    MigrationRules = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DeprecatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CanonicalEntityVersions", x => x.Id);
                    table.UniqueConstraint("AK_CanonicalEntityVersions_EntityType_Version", x => new { x.EntityType, x.Version });
                });

            migrationBuilder.CreateTable(
                name: "CanonicalMappings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConnectorId = table.Column<int>(type: "int", nullable: false),
                    TargetEntityType = table.Column<int>(type: "int", nullable: false),
                    TargetSchemaVersion = table.Column<int>(type: "int", nullable: false),
                    ExternalField = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CanonicalAttribute = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TransformationType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TransformationParams = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    DefaultValue = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CanonicalMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CanonicalMappings_Connectors_ConnectorId",
                        column: x => x.ConnectorId,
                        principalTable: "Connectors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CanonicalEntities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EntityType = table.Column<int>(type: "int", nullable: false),
                    SchemaVersion = table.Column<int>(type: "int", nullable: false),
                    ExternalId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Data = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SourceSystem = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SourceVersion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ImportedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ImportedByJobId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    VendorExtensions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsApproved = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CanonicalEntities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CanonicalEntities_CanonicalEntityVersions_EntityType_SchemaVersion",
                        columns: x => new { x.EntityType, x.SchemaVersion },
                        principalTable: "CanonicalEntityVersions",
                        principalColumns: new[] { "EntityType", "Version" },
                        onDelete: ReferentialAction.Restrict);
                });

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
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CanonicalEntityId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceEntities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinanceEntities_CanonicalEntities_CanonicalEntityId",
                        column: x => x.CanonicalEntityId,
                        principalTable: "CanonicalEntities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
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
                name: "IX_HREntities_CanonicalEntityId",
                table: "HREntities",
                column: "CanonicalEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_CanonicalAttributes_EntityType_SchemaVersion_AttributeName",
                table: "CanonicalAttributes",
                columns: new[] { "EntityType", "SchemaVersion", "AttributeName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CanonicalAttributes_IsDeprecated",
                table: "CanonicalAttributes",
                column: "IsDeprecated");

            migrationBuilder.CreateIndex(
                name: "IX_CanonicalAttributes_IsRequired",
                table: "CanonicalAttributes",
                column: "IsRequired");

            migrationBuilder.CreateIndex(
                name: "IX_CanonicalEntities_EntityType",
                table: "CanonicalEntities",
                column: "EntityType");

            migrationBuilder.CreateIndex(
                name: "IX_CanonicalEntities_EntityType_SchemaVersion",
                table: "CanonicalEntities",
                columns: new[] { "EntityType", "SchemaVersion" });

            migrationBuilder.CreateIndex(
                name: "IX_CanonicalEntities_ImportedByJobId",
                table: "CanonicalEntities",
                column: "ImportedByJobId");

            migrationBuilder.CreateIndex(
                name: "IX_CanonicalEntities_IsApproved",
                table: "CanonicalEntities",
                column: "IsApproved");

            migrationBuilder.CreateIndex(
                name: "IX_CanonicalEntities_SchemaVersion",
                table: "CanonicalEntities",
                column: "SchemaVersion");

            migrationBuilder.CreateIndex(
                name: "IX_CanonicalEntities_SourceSystem_ExternalId",
                table: "CanonicalEntities",
                columns: new[] { "SourceSystem", "ExternalId" });

            migrationBuilder.CreateIndex(
                name: "IX_CanonicalEntityVersions_EntityType_Version",
                table: "CanonicalEntityVersions",
                columns: new[] { "EntityType", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CanonicalEntityVersions_IsActive",
                table: "CanonicalEntityVersions",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_CanonicalEntityVersions_IsDeprecated",
                table: "CanonicalEntityVersions",
                column: "IsDeprecated");

            migrationBuilder.CreateIndex(
                name: "IX_CanonicalMappings_ConnectorId",
                table: "CanonicalMappings",
                column: "ConnectorId");

            migrationBuilder.CreateIndex(
                name: "IX_CanonicalMappings_ConnectorId_TargetEntityType",
                table: "CanonicalMappings",
                columns: new[] { "ConnectorId", "TargetEntityType" });

            migrationBuilder.CreateIndex(
                name: "IX_CanonicalMappings_ConnectorId_TargetEntityType_TargetSchemaVersion",
                table: "CanonicalMappings",
                columns: new[] { "ConnectorId", "TargetEntityType", "TargetSchemaVersion" });

            migrationBuilder.CreateIndex(
                name: "IX_CanonicalMappings_IsActive",
                table: "CanonicalMappings",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_CanonicalMappings_IsRequired",
                table: "CanonicalMappings",
                column: "IsRequired");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceEntities_CanonicalEntityId",
                table: "FinanceEntities",
                column: "CanonicalEntityId");

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
                name: "IX_FinanceEntities_ImportJobId",
                table: "FinanceEntities",
                column: "ImportJobId");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceEntities_IsApproved",
                table: "FinanceEntities",
                column: "IsApproved");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceSyncRecords_ConflictDetected",
                table: "FinanceSyncRecords",
                column: "ConflictDetected");

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

            migrationBuilder.AddForeignKey(
                name: "FK_HREntities_CanonicalEntities_CanonicalEntityId",
                table: "HREntities",
                column: "CanonicalEntityId",
                principalTable: "CanonicalEntities",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_HREntities_CanonicalEntities_CanonicalEntityId",
                table: "HREntities");

            migrationBuilder.DropTable(
                name: "CanonicalAttributes");

            migrationBuilder.DropTable(
                name: "CanonicalMappings");

            migrationBuilder.DropTable(
                name: "FinanceSyncRecords");

            migrationBuilder.DropTable(
                name: "FinanceEntities");

            migrationBuilder.DropTable(
                name: "CanonicalEntities");

            migrationBuilder.DropTable(
                name: "CanonicalEntityVersions");

            migrationBuilder.DropIndex(
                name: "IX_HREntities_CanonicalEntityId",
                table: "HREntities");

            migrationBuilder.DropColumn(
                name: "CanonicalEntityId",
                table: "HREntities");
        }
    }
}
