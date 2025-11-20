// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1861 // Prefer 'static readonly' fields over constant array arguments

namespace NexaMediaServer.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddJobNotificationEntry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedAt",
                table: "MediaParts",
                type: "TEXT",
                nullable: true
            );

            migrationBuilder.CreateTable(
                name: "JobNotificationEntries",
                columns: table => new
                {
                    Id = table
                        .Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    LibrarySectionId = table.Column<int>(type: "INTEGER", nullable: false),
                    JobType = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Progress = table.Column<int>(type: "INTEGER", nullable: false),
                    CompletedItems = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalItems = table.Column<int>(type: "INTEGER", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ErrorMessage = table.Column<string>(
                        type: "TEXT",
                        maxLength: 4096,
                        nullable: true
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobNotificationEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobNotificationEntries_LibrarySections_LibrarySectionId",
                        column: x => x.LibrarySectionId,
                        principalTable: "LibrarySections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_JobNotificationEntries_CompletedAt",
                table: "JobNotificationEntries",
                column: "CompletedAt"
            );

            migrationBuilder.CreateIndex(
                name: "IX_JobNotificationEntries_LibrarySectionId_JobType_Status",
                table: "JobNotificationEntries",
                columns: new[] { "LibrarySectionId", "JobType", "Status" }
            );

            migrationBuilder.CreateIndex(
                name: "IX_JobNotificationEntries_Status",
                table: "JobNotificationEntries",
                column: "Status"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "JobNotificationEntries");

            migrationBuilder.DropColumn(name: "ModifiedAt", table: "MediaParts");
        }
    }
}

#pragma warning restore CA1861
