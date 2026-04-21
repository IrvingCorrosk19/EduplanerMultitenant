using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolManager.Migrations
{
    /// <inheritdoc />
    public partial class CompletePrematriculationModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "end_date",
                table: "student_assignments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "student_assignments",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            // shift_id ya existe en student_assignments, no lo agregamos de nuevo
            // migrationBuilder.AddColumn<Guid>(
            //     name: "shift_id",
            //     table: "student_assignments",
            //     type: "uuid",
            //     nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "cancelled_by",
                table: "prematriculations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "confirmed_by",
                table: "prematriculations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "rejected_by",
                table: "prematriculations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "required_amount",
                table: "prematriculation_periods",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            // shift_id ya existe en groups, no lo agregamos de nuevo
            // migrationBuilder.AddColumn<Guid>(
            //     name: "shift_id",
            //     table: "groups",
            //     type: "uuid",
            //     nullable: true);

            migrationBuilder.CreateTable(
                name: "prematriculation_histories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    prematriculation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    previous_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    new_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    changed_by = table.Column<Guid>(type: "uuid", nullable: true),
                    reason = table.Column<string>(type: "text", nullable: true),
                    changed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    additional_info = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("prematriculation_histories_pkey", x => x.id);
                    table.ForeignKey(
                        name: "prematriculation_histories_changed_by_fkey",
                        column: x => x.changed_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "prematriculation_histories_prematriculation_id_fkey",
                        column: x => x.prematriculation_id,
                        principalTable: "prematriculations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            // La tabla shifts ya existe, no la creamos de nuevo
            // migrationBuilder.CreateTable(
            //     name: "shifts",
            //     ...
            // );

            // El índice ya existe, no lo creamos de nuevo
            // migrationBuilder.CreateIndex(
            //     name: "IX_student_assignments_shift_id",
            //     table: "student_assignments",
            //     column: "shift_id");

            migrationBuilder.CreateIndex(
                name: "IX_prematriculations_cancelled_by",
                table: "prematriculations",
                column: "cancelled_by");

            migrationBuilder.CreateIndex(
                name: "IX_prematriculations_confirmed_by",
                table: "prematriculations",
                column: "confirmed_by");

            migrationBuilder.CreateIndex(
                name: "IX_prematriculations_rejected_by",
                table: "prematriculations",
                column: "rejected_by");

            // El índice ya existe, no lo creamos de nuevo
            // migrationBuilder.CreateIndex(
            //     name: "IX_groups_shift_id",
            //     table: "groups",
            //     column: "shift_id");

            migrationBuilder.CreateIndex(
                name: "IX_prematriculation_histories_changed_at",
                table: "prematriculation_histories",
                column: "changed_at");

            migrationBuilder.CreateIndex(
                name: "IX_prematriculation_histories_changed_by",
                table: "prematriculation_histories",
                column: "changed_by");

            migrationBuilder.CreateIndex(
                name: "IX_prematriculation_histories_prematriculation_id",
                table: "prematriculation_histories",
                column: "prematriculation_id");

            // Los índices de shifts ya existen, no los creamos de nuevo
            // migrationBuilder.CreateIndex(...);

            // La foreign key ya existe, no la agregamos de nuevo
            // migrationBuilder.AddForeignKey(
            //     name: "groups_shift_id_fkey",
            //     table: "groups",
            //     column: "shift_id",
            //     principalTable: "shifts",
            //     principalColumn: "id",
            //     onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "prematriculations_cancelled_by_fkey",
                table: "prematriculations",
                column: "cancelled_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "prematriculations_confirmed_by_fkey",
                table: "prematriculations",
                column: "confirmed_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "prematriculations_rejected_by_fkey",
                table: "prematriculations",
                column: "rejected_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            // La foreign key ya existe, no la agregamos de nuevo
            // migrationBuilder.AddForeignKey(
            //     name: "student_assignments_shift_id_fkey",
            //     table: "student_assignments",
            //     column: "shift_id",
            //     principalTable: "shifts",
            //     principalColumn: "id",
            //     onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No eliminamos la foreign key porque ya existe
            // migrationBuilder.DropForeignKey(
            //     name: "groups_shift_id_fkey",
            //     table: "groups");

            migrationBuilder.DropForeignKey(
                name: "prematriculations_cancelled_by_fkey",
                table: "prematriculations");

            migrationBuilder.DropForeignKey(
                name: "prematriculations_confirmed_by_fkey",
                table: "prematriculations");

            migrationBuilder.DropForeignKey(
                name: "prematriculations_rejected_by_fkey",
                table: "prematriculations");

            // No eliminamos la foreign key porque ya existe
            // migrationBuilder.DropForeignKey(
            //     name: "student_assignments_shift_id_fkey",
            //     table: "student_assignments");

            migrationBuilder.DropTable(
                name: "prematriculation_histories");

            // No eliminamos shifts porque ya existe
            // migrationBuilder.DropTable(
            //     name: "shifts");

            // No eliminamos el índice porque ya existe
            // migrationBuilder.DropIndex(
            //     name: "IX_student_assignments_shift_id",
            //     table: "student_assignments");

            migrationBuilder.DropIndex(
                name: "IX_prematriculations_cancelled_by",
                table: "prematriculations");

            migrationBuilder.DropIndex(
                name: "IX_prematriculations_confirmed_by",
                table: "prematriculations");

            migrationBuilder.DropIndex(
                name: "IX_prematriculations_rejected_by",
                table: "prematriculations");

            // No eliminamos el índice porque ya existe
            // migrationBuilder.DropIndex(
            //     name: "IX_groups_shift_id",
            //     table: "groups");

            migrationBuilder.DropColumn(
                name: "end_date",
                table: "student_assignments");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "student_assignments");

            // No eliminamos shift_id porque ya existe
            // migrationBuilder.DropColumn(
            //     name: "shift_id",
            //     table: "student_assignments");

            migrationBuilder.DropColumn(
                name: "cancelled_by",
                table: "prematriculations");

            migrationBuilder.DropColumn(
                name: "confirmed_by",
                table: "prematriculations");

            migrationBuilder.DropColumn(
                name: "rejected_by",
                table: "prematriculations");

            migrationBuilder.DropColumn(
                name: "required_amount",
                table: "prematriculation_periods");

            // No eliminamos shift_id porque ya existe
            // migrationBuilder.DropColumn(
            //     name: "shift_id",
            //     table: "groups");
        }
    }
}
