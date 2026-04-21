using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolManager.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentIdModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "scan_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    student_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scan_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    result = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    scanned_by = table.Column<Guid>(type: "uuid", nullable: false),
                    scanned_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("scan_logs_pkey", x => x.id);
                    table.ForeignKey(
                        name: "scan_logs_student_id_fkey",
                        column: x => x.student_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "student_id_cards",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    student_id = table.Column<Guid>(type: "uuid", nullable: false),
                    card_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    issued_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("student_id_cards_pkey", x => x.id);
                    table.ForeignKey(
                        name: "student_id_cards_student_id_fkey",
                        column: x => x.student_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "student_qr_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    student_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_revoked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("student_qr_tokens_pkey", x => x.id);
                    table.ForeignKey(
                        name: "student_qr_tokens_student_id_fkey",
                        column: x => x.student_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_scan_logs_scanned_at",
                table: "scan_logs",
                column: "scanned_at");

            migrationBuilder.CreateIndex(
                name: "IX_scan_logs_student_id",
                table: "scan_logs",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "IX_student_id_cards_card_number",
                table: "student_id_cards",
                column: "card_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_student_id_cards_student_id",
                table: "student_id_cards",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "IX_student_qr_tokens_student_id",
                table: "student_qr_tokens",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "IX_student_qr_tokens_token",
                table: "student_qr_tokens",
                column: "token",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "scan_logs");

            migrationBuilder.DropTable(
                name: "student_id_cards");

            migrationBuilder.DropTable(
                name: "student_qr_tokens");
        }
    }
}
