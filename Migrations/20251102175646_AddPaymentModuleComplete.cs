using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolManager.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentModuleComplete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:pgcrypto", ",,")
                .Annotation("Npgsql:PostgresExtension:uuid-ossp", ",,");

            migrationBuilder.CreateTable(
                name: "area",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    is_global = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    display_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("area_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "activities",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    school_id = table.Column<Guid>(type: "uuid", nullable: true),
                    subject_id = table.Column<Guid>(type: "uuid", nullable: true),
                    teacher_id = table.Column<Guid>(type: "uuid", nullable: true),
                    group_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    trimester = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: true),
                    pdf_url = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    due_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    grade_level_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ActivityTypeId = table.Column<Guid>(type: "uuid", nullable: true),
                    TrimesterId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("activities_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "activity_attachments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    activity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    storage_path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    mime_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    uploaded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("activity_attachments_pkey", x => x.id);
                    table.ForeignKey(
                        name: "activity_attachments_activity_id_fkey",
                        column: x => x.activity_id,
                        principalTable: "activities",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "activity_types",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    school_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    icon = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    color = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    is_global = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    display_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("activity_types_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "attendance",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    school_id = table.Column<Guid>(type: "uuid", nullable: true),
                    student_id = table.Column<Guid>(type: "uuid", nullable: true),
                    teacher_id = table.Column<Guid>(type: "uuid", nullable: true),
                    group_id = table.Column<Guid>(type: "uuid", nullable: true),
                    grade_id = table.Column<Guid>(type: "uuid", nullable: true),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    status = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("attendance_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    school_id = table.Column<Guid>(type: "uuid", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    user_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    user_role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    action = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    resource = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    details = table.Column<string>(type: "text", nullable: true),
                    ip_address = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("audit_logs_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "counselor_assignments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    school_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    grade_id = table.Column<Guid>(type: "uuid", nullable: true),
                    group_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_counselor = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("counselor_assignments_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "discipline_reports",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    school_id = table.Column<Guid>(type: "uuid", nullable: true),
                    student_id = table.Column<Guid>(type: "uuid", nullable: true),
                    teacher_id = table.Column<Guid>(type: "uuid", nullable: true),
                    date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    report_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    subject_id = table.Column<Guid>(type: "uuid", nullable: true),
                    group_id = table.Column<Guid>(type: "uuid", nullable: true),
                    grade_level_id = table.Column<Guid>(type: "uuid", nullable: true),
                    category = table.Column<string>(type: "text", nullable: true),
                    documents = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("discipline_reports_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "email_configurations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    school_id = table.Column<Guid>(type: "uuid", nullable: false),
                    smtp_server = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    smtp_port = table.Column<int>(type: "integer", nullable: false, defaultValue: 587),
                    smtp_username = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    smtp_password = table.Column<string>(type: "text", nullable: false),
                    smtp_use_ssl = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    smtp_use_tls = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    from_email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    from_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("email_configurations_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "grade_levels",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    school_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("grade_levels_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "groups",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    school_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    grade = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    max_capacity = table.Column<int>(type: "integer", nullable: true),
                    shift = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("groups_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    sender_id = table.Column<Guid>(type: "uuid", nullable: false),
                    school_id = table.Column<Guid>(type: "uuid", nullable: true),
                    subject = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    content = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: false),
                    message_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    recipient_id = table.Column<Guid>(type: "uuid", nullable: true),
                    group_id = table.Column<Guid>(type: "uuid", nullable: true),
                    sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    is_read = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    read_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    priority = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Normal"),
                    parent_message_id = table.Column<Guid>(type: "uuid", nullable: true),
                    attachments = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("messages_pkey", x => x.id);
                    table.ForeignKey(
                        name: "messages_group_id_fkey",
                        column: x => x.group_id,
                        principalTable: "groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "messages_parent_message_id_fkey",
                        column: x => x.parent_message_id,
                        principalTable: "messages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "orientation_reports",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    school_id = table.Column<Guid>(type: "uuid", nullable: true),
                    student_id = table.Column<Guid>(type: "uuid", nullable: true),
                    teacher_id = table.Column<Guid>(type: "uuid", nullable: true),
                    date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    report_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    subject_id = table.Column<Guid>(type: "uuid", nullable: true),
                    group_id = table.Column<Guid>(type: "uuid", nullable: true),
                    grade_level_id = table.Column<Guid>(type: "uuid", nullable: true),
                    category = table.Column<string>(type: "text", nullable: true),
                    documents = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_orientation_reports", x => x.id);
                    table.ForeignKey(
                        name: "orientation_reports_grade_level_id_fkey",
                        column: x => x.grade_level_id,
                        principalTable: "grade_levels",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "orientation_reports_group_id_fkey",
                        column: x => x.group_id,
                        principalTable: "groups",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "payment_concepts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    school_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    periodicity = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("payment_concepts_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "payments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    school_id = table.Column<Guid>(type: "uuid", nullable: false),
                    prematriculation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    registered_by = table.Column<Guid>(type: "uuid", nullable: true),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    payment_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    receipt_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    payment_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Pendiente"),
                    payment_method = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    receipt_image = table.Column<string>(type: "text", nullable: true),
                    payment_concept_id = table.Column<Guid>(type: "uuid", nullable: true),
                    student_id = table.Column<Guid>(type: "uuid", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    confirmed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("payments_pkey", x => x.id);
                    table.ForeignKey(
                        name: "payments_payment_concept_id_fkey",
                        column: x => x.payment_concept_id,
                        principalTable: "payment_concepts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "prematriculation_periods",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    school_id = table.Column<Guid>(type: "uuid", nullable: false),
                    start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    end_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    max_capacity_per_group = table.Column<int>(type: "integer", nullable: false, defaultValue: 30),
                    auto_assign_by_shift = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("prematriculation_periods_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "prematriculations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    school_id = table.Column<Guid>(type: "uuid", nullable: false),
                    student_id = table.Column<Guid>(type: "uuid", nullable: false),
                    parent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    grade_id = table.Column<Guid>(type: "uuid", nullable: true),
                    group_id = table.Column<Guid>(type: "uuid", nullable: true),
                    prematriculation_period_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Pendiente"),
                    failed_subjects_count = table.Column<int>(type: "integer", nullable: true),
                    academic_condition_valid = table.Column<bool>(type: "boolean", nullable: true),
                    rejection_reason = table.Column<string>(type: "text", nullable: true),
                    prematriculation_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    payment_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    matriculation_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("prematriculations_pkey", x => x.id);
                    table.ForeignKey(
                        name: "prematriculations_grade_id_fkey",
                        column: x => x.grade_id,
                        principalTable: "grade_levels",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "prematriculations_group_id_fkey",
                        column: x => x.group_id,
                        principalTable: "groups",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "prematriculations_period_id_fkey",
                        column: x => x.prematriculation_period_id,
                        principalTable: "prematriculation_periods",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "schools",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    address = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false, defaultValueSql: "''::character varying"),
                    phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValueSql: "''::character varying"),
                    logo_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP"),
                    admin_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("schools_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "security_settings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    school_id = table.Column<Guid>(type: "uuid", nullable: true),
                    password_min_length = table.Column<int>(type: "integer", nullable: true, defaultValue: 8),
                    require_uppercase = table.Column<bool>(type: "boolean", nullable: true, defaultValue: true),
                    require_lowercase = table.Column<bool>(type: "boolean", nullable: true, defaultValue: true),
                    require_numbers = table.Column<bool>(type: "boolean", nullable: true, defaultValue: true),
                    require_special = table.Column<bool>(type: "boolean", nullable: true, defaultValue: true),
                    expiry_days = table.Column<int>(type: "integer", nullable: true, defaultValue: 90),
                    prevent_reuse = table.Column<int>(type: "integer", nullable: true, defaultValue: 5),
                    max_login_attempts = table.Column<int>(type: "integer", nullable: true, defaultValue: 5),
                    session_timeout_minutes = table.Column<int>(type: "integer", nullable: true, defaultValue: 30),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("security_settings_pkey", x => x.id);
                    table.ForeignKey(
                        name: "security_settings_school_id_fkey",
                        column: x => x.school_id,
                        principalTable: "schools",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "trimester",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    school_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    order = table.Column<int>(type: "integer", nullable: false),
                    start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    end_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("trimester_pkey", x => x.id);
                    table.ForeignKey(
                        name: "trimester_school_id_fkey",
                        column: x => x.school_id,
                        principalTable: "schools",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    school_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    status = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true, defaultValueSql: "'active'::character varying"),
                    two_factor_enabled = table.Column<bool>(type: "boolean", nullable: true, defaultValue: false),
                    last_login = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, defaultValueSql: "''::character varying"),
                    document_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    date_of_birth = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    cellphone_primary = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    cellphone_secondary = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    disciplina = table.Column<bool>(type: "boolean", nullable: true, defaultValue: false),
                    inclusion = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    orientacion = table.Column<bool>(type: "boolean", nullable: true, defaultValue: false),
                    inclusivo = table.Column<bool>(type: "boolean", nullable: true, defaultValue: false),
                    shift = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("users_pkey", x => x.id);
                    table.ForeignKey(
                        name: "FK_users_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_users_users_updated_by",
                        column: x => x.updated_by,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "users_school_id_fkey",
                        column: x => x.school_id,
                        principalTable: "schools",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "specialties",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    school_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("specialties_pkey", x => x.id);
                    table.ForeignKey(
                        name: "FK_specialties_schools_school_id",
                        column: x => x.school_id,
                        principalTable: "schools",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_specialties_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_specialties_users_updated_by",
                        column: x => x.updated_by,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "student_activity_scores",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    school_id = table.Column<Guid>(type: "uuid", nullable: true),
                    student_id = table.Column<Guid>(type: "uuid", nullable: false),
                    activity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    score = table.Column<decimal>(type: "numeric(2,1)", precision: 2, scale: 1, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("student_activity_scores_pkey", x => x.id);
                    table.ForeignKey(
                        name: "FK_student_activity_scores_schools_school_id",
                        column: x => x.school_id,
                        principalTable: "schools",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_student_activity_scores_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_student_activity_scores_users_updated_by",
                        column: x => x.updated_by,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "student_activity_scores_activity_id_fkey",
                        column: x => x.activity_id,
                        principalTable: "activities",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "student_activity_scores_student_id_fkey",
                        column: x => x.student_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "student_assignments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    student_id = table.Column<Guid>(type: "uuid", nullable: false),
                    grade_id = table.Column<Guid>(type: "uuid", nullable: false),
                    group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("student_assignments_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_grade",
                        column: x => x.grade_id,
                        principalTable: "grade_levels",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_group",
                        column: x => x.group_id,
                        principalTable: "groups",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_student",
                        column: x => x.student_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "students",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    school_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    birth_date = table.Column<DateOnly>(type: "date", nullable: true),
                    grade = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    group_name = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    parent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("students_pkey", x => x.id);
                    table.ForeignKey(
                        name: "students_parent_id_fkey",
                        column: x => x.parent_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "students_school_id_fkey",
                        column: x => x.school_id,
                        principalTable: "schools",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "subjects",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    school_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<bool>(type: "boolean", nullable: true, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    AreaId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("subjects_pkey", x => x.id);
                    table.ForeignKey(
                        name: "FK_subjects_area_AreaId",
                        column: x => x.AreaId,
                        principalTable: "area",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_subjects_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_subjects_users_updated_by",
                        column: x => x.updated_by,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "subjects_school_id_fkey",
                        column: x => x.school_id,
                        principalTable: "schools",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_grades",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    grade_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("user_grades_pkey", x => new { x.user_id, x.grade_id });
                    table.ForeignKey(
                        name: "fk_user_grades_grade",
                        column: x => x.grade_id,
                        principalTable: "grade_levels",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_user_grades_user",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "user_groups",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    group_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("user_groups_pkey", x => new { x.user_id, x.group_id });
                    table.ForeignKey(
                        name: "fk_user_groups_group",
                        column: x => x.group_id,
                        principalTable: "groups",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_user_groups_user",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "subject_assignments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    specialty_id = table.Column<Guid>(type: "uuid", nullable: false),
                    area_id = table.Column<Guid>(type: "uuid", nullable: false),
                    subject_id = table.Column<Guid>(type: "uuid", nullable: false),
                    grade_level_id = table.Column<Guid>(type: "uuid", nullable: false),
                    group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP"),
                    status = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    SchoolId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("subject_assignments_pkey", x => x.id);
                    table.ForeignKey(
                        name: "FK_subject_assignments_schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "schools",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "subject_assignments_area_id_fkey",
                        column: x => x.area_id,
                        principalTable: "area",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "subject_assignments_grade_level_id_fkey",
                        column: x => x.grade_level_id,
                        principalTable: "grade_levels",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "subject_assignments_group_id_fkey",
                        column: x => x.group_id,
                        principalTable: "groups",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "subject_assignments_specialty_id_fkey",
                        column: x => x.specialty_id,
                        principalTable: "specialties",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "subject_assignments_subject_id_fkey",
                        column: x => x.subject_id,
                        principalTable: "subjects",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "user_subjects",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    subject_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("user_subjects_pkey", x => new { x.user_id, x.subject_id });
                    table.ForeignKey(
                        name: "fk_user_subjects_subject",
                        column: x => x.subject_id,
                        principalTable: "subjects",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_user_subjects_user",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "teacher_assignments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    teacher_id = table.Column<Guid>(type: "uuid", nullable: false),
                    subject_assignment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("teacher_assignments_pkey", x => x.id);
                    table.ForeignKey(
                        name: "teacher_assignments_subject_assignment_id_fkey",
                        column: x => x.subject_assignment_id,
                        principalTable: "subject_assignments",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "teacher_assignments_teacher_id_fkey",
                        column: x => x.teacher_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "idx_activities_group",
                table: "activities",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "idx_activities_teacher",
                table: "activities",
                column: "teacher_id");

            migrationBuilder.CreateIndex(
                name: "idx_activities_trimester",
                table: "activities",
                column: "trimester");

            migrationBuilder.CreateIndex(
                name: "idx_activities_unique_lookup",
                table: "activities",
                columns: new[] { "name", "type", "subject_id", "group_id", "teacher_id", "trimester" });

            migrationBuilder.CreateIndex(
                name: "IX_activities_ActivityTypeId",
                table: "activities",
                column: "ActivityTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_activities_created_by",
                table: "activities",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_activities_school_id",
                table: "activities",
                column: "school_id");

            migrationBuilder.CreateIndex(
                name: "IX_activities_subject_id",
                table: "activities",
                column: "subject_id");

            migrationBuilder.CreateIndex(
                name: "IX_activities_TrimesterId",
                table: "activities",
                column: "TrimesterId");

            migrationBuilder.CreateIndex(
                name: "IX_activities_updated_by",
                table: "activities",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "idx_attach_activity",
                table: "activity_attachments",
                column: "activity_id");

            migrationBuilder.CreateIndex(
                name: "activity_types_name_key",
                table: "activity_types",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_activity_types_created_by",
                table: "activity_types",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_activity_types_school_id",
                table: "activity_types",
                column: "school_id");

            migrationBuilder.CreateIndex(
                name: "IX_activity_types_updated_by",
                table: "activity_types",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "area_name_key",
                table: "area",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_attendance_created_by",
                table: "attendance",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_attendance_grade_id",
                table: "attendance",
                column: "grade_id");

            migrationBuilder.CreateIndex(
                name: "IX_attendance_group_id",
                table: "attendance",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "IX_attendance_school_id",
                table: "attendance",
                column: "school_id");

            migrationBuilder.CreateIndex(
                name: "IX_attendance_student_id",
                table: "attendance",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "IX_attendance_teacher_id",
                table: "attendance",
                column: "teacher_id");

            migrationBuilder.CreateIndex(
                name: "IX_attendance_updated_by",
                table: "attendance",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_school_id",
                table: "audit_logs",
                column: "school_id");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_user_id",
                table: "audit_logs",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "counselor_assignments_school_grade_group_key",
                table: "counselor_assignments",
                columns: new[] { "school_id", "grade_id", "group_id" },
                unique: true,
                filter: "grade_id IS NOT NULL AND group_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "counselor_assignments_school_user_key",
                table: "counselor_assignments",
                columns: new[] { "school_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_counselor_assignments_grade",
                table: "counselor_assignments",
                column: "grade_id");

            migrationBuilder.CreateIndex(
                name: "idx_counselor_assignments_group",
                table: "counselor_assignments",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "idx_counselor_assignments_school",
                table: "counselor_assignments",
                column: "school_id");

            migrationBuilder.CreateIndex(
                name: "idx_counselor_assignments_user",
                table: "counselor_assignments",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_discipline_reports_created_by",
                table: "discipline_reports",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_discipline_reports_grade_level_id",
                table: "discipline_reports",
                column: "grade_level_id");

            migrationBuilder.CreateIndex(
                name: "IX_discipline_reports_group_id",
                table: "discipline_reports",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "IX_discipline_reports_school_id",
                table: "discipline_reports",
                column: "school_id");

            migrationBuilder.CreateIndex(
                name: "IX_discipline_reports_student_id",
                table: "discipline_reports",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "IX_discipline_reports_subject_id",
                table: "discipline_reports",
                column: "subject_id");

            migrationBuilder.CreateIndex(
                name: "IX_discipline_reports_teacher_id",
                table: "discipline_reports",
                column: "teacher_id");

            migrationBuilder.CreateIndex(
                name: "IX_discipline_reports_updated_by",
                table: "discipline_reports",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "idx_email_configurations_school_id",
                table: "email_configurations",
                column: "school_id");

            migrationBuilder.CreateIndex(
                name: "grade_levels_name_key",
                table: "grade_levels",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_grade_levels_created_by",
                table: "grade_levels",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_grade_levels_school_id",
                table: "grade_levels",
                column: "school_id");

            migrationBuilder.CreateIndex(
                name: "IX_grade_levels_updated_by",
                table: "grade_levels",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "IX_groups_created_by",
                table: "groups",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_groups_school_id",
                table: "groups",
                column: "school_id");

            migrationBuilder.CreateIndex(
                name: "IX_groups_updated_by",
                table: "groups",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "idx_messages_recipient",
                table: "messages",
                column: "recipient_id");

            migrationBuilder.CreateIndex(
                name: "idx_messages_recipient_unread",
                table: "messages",
                columns: new[] { "recipient_id", "is_read" });

            migrationBuilder.CreateIndex(
                name: "idx_messages_school",
                table: "messages",
                column: "school_id");

            migrationBuilder.CreateIndex(
                name: "idx_messages_sender",
                table: "messages",
                column: "sender_id");

            migrationBuilder.CreateIndex(
                name: "idx_messages_sent_at",
                table: "messages",
                column: "sent_at");

            migrationBuilder.CreateIndex(
                name: "IX_messages_group_id",
                table: "messages",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "IX_messages_parent_message_id",
                table: "messages",
                column: "parent_message_id");

            migrationBuilder.CreateIndex(
                name: "IX_orientation_reports_created_by",
                table: "orientation_reports",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_orientation_reports_grade_level_id",
                table: "orientation_reports",
                column: "grade_level_id");

            migrationBuilder.CreateIndex(
                name: "IX_orientation_reports_group_id",
                table: "orientation_reports",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "IX_orientation_reports_school_id",
                table: "orientation_reports",
                column: "school_id");

            migrationBuilder.CreateIndex(
                name: "IX_orientation_reports_student_id",
                table: "orientation_reports",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "IX_orientation_reports_subject_id",
                table: "orientation_reports",
                column: "subject_id");

            migrationBuilder.CreateIndex(
                name: "IX_orientation_reports_teacher_id",
                table: "orientation_reports",
                column: "teacher_id");

            migrationBuilder.CreateIndex(
                name: "IX_orientation_reports_updated_by",
                table: "orientation_reports",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "IX_payment_concepts_created_by",
                table: "payment_concepts",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_payment_concepts_school_id",
                table: "payment_concepts",
                column: "school_id");

            migrationBuilder.CreateIndex(
                name: "IX_payment_concepts_updated_by",
                table: "payment_concepts",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "IX_payments_payment_concept_id",
                table: "payments",
                column: "payment_concept_id");

            migrationBuilder.CreateIndex(
                name: "IX_payments_prematriculation_id",
                table: "payments",
                column: "prematriculation_id");

            migrationBuilder.CreateIndex(
                name: "IX_payments_receipt_number",
                table: "payments",
                column: "receipt_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_payments_registered_by",
                table: "payments",
                column: "registered_by");

            migrationBuilder.CreateIndex(
                name: "IX_payments_school_id",
                table: "payments",
                column: "school_id");

            migrationBuilder.CreateIndex(
                name: "IX_payments_student_id",
                table: "payments",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "IX_prematriculation_periods_created_by",
                table: "prematriculation_periods",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_prematriculation_periods_dates",
                table: "prematriculation_periods",
                columns: new[] { "school_id", "start_date", "end_date" });

            migrationBuilder.CreateIndex(
                name: "IX_prematriculation_periods_school_id",
                table: "prematriculation_periods",
                column: "school_id");

            migrationBuilder.CreateIndex(
                name: "IX_prematriculation_periods_updated_by",
                table: "prematriculation_periods",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "IX_prematriculations_code",
                table: "prematriculations",
                column: "prematriculation_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_prematriculations_grade_id",
                table: "prematriculations",
                column: "grade_id");

            migrationBuilder.CreateIndex(
                name: "IX_prematriculations_group_id",
                table: "prematriculations",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "IX_prematriculations_parent_id",
                table: "prematriculations",
                column: "parent_id");

            migrationBuilder.CreateIndex(
                name: "IX_prematriculations_period_id",
                table: "prematriculations",
                column: "prematriculation_period_id");

            migrationBuilder.CreateIndex(
                name: "IX_prematriculations_school_id",
                table: "prematriculations",
                column: "school_id");

            migrationBuilder.CreateIndex(
                name: "IX_prematriculations_student_id",
                table: "prematriculations",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "idx_schools_admin_id",
                table: "schools",
                column: "admin_id");

            migrationBuilder.CreateIndex(
                name: "IX_security_settings_school_id",
                table: "security_settings",
                column: "school_id");

            migrationBuilder.CreateIndex(
                name: "IX_specialties_created_by",
                table: "specialties",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_specialties_school_id",
                table: "specialties",
                column: "school_id");

            migrationBuilder.CreateIndex(
                name: "IX_specialties_updated_by",
                table: "specialties",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "specialties_name_key",
                table: "specialties",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_scores_activity",
                table: "student_activity_scores",
                column: "activity_id");

            migrationBuilder.CreateIndex(
                name: "idx_scores_student",
                table: "student_activity_scores",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "IX_student_activity_scores_created_by",
                table: "student_activity_scores",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_student_activity_scores_school_id",
                table: "student_activity_scores",
                column: "school_id");

            migrationBuilder.CreateIndex(
                name: "IX_student_activity_scores_updated_by",
                table: "student_activity_scores",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "uq_scores",
                table: "student_activity_scores",
                columns: new[] { "student_id", "activity_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_student_assignments_grade_id",
                table: "student_assignments",
                column: "grade_id");

            migrationBuilder.CreateIndex(
                name: "IX_student_assignments_group_id",
                table: "student_assignments",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "IX_student_assignments_student_id",
                table: "student_assignments",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "IX_students_parent_id",
                table: "students",
                column: "parent_id");

            migrationBuilder.CreateIndex(
                name: "IX_students_school_id",
                table: "students",
                column: "school_id");

            migrationBuilder.CreateIndex(
                name: "IX_subject_assignments_area_id",
                table: "subject_assignments",
                column: "area_id");

            migrationBuilder.CreateIndex(
                name: "IX_subject_assignments_grade_level_id",
                table: "subject_assignments",
                column: "grade_level_id");

            migrationBuilder.CreateIndex(
                name: "IX_subject_assignments_group_id",
                table: "subject_assignments",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "IX_subject_assignments_SchoolId",
                table: "subject_assignments",
                column: "SchoolId");

            migrationBuilder.CreateIndex(
                name: "IX_subject_assignments_specialty_id",
                table: "subject_assignments",
                column: "specialty_id");

            migrationBuilder.CreateIndex(
                name: "IX_subject_assignments_subject_id",
                table: "subject_assignments",
                column: "subject_id");

            migrationBuilder.CreateIndex(
                name: "IX_subjects_AreaId",
                table: "subjects",
                column: "AreaId");

            migrationBuilder.CreateIndex(
                name: "IX_subjects_created_by",
                table: "subjects",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_subjects_school_id",
                table: "subjects",
                column: "school_id");

            migrationBuilder.CreateIndex(
                name: "IX_subjects_updated_by",
                table: "subjects",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "IX_teacher_assignments_subject_assignment_id",
                table: "teacher_assignments",
                column: "subject_assignment_id");

            migrationBuilder.CreateIndex(
                name: "teacher_assignments_teacher_id_subject_assignment_id_key",
                table: "teacher_assignments",
                columns: new[] { "teacher_id", "subject_assignment_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_trimester_school_id",
                table: "trimester",
                column: "school_id");

            migrationBuilder.CreateIndex(
                name: "trimester_name_school_key",
                table: "trimester",
                columns: new[] { "name", "school_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_grades_grade_id",
                table: "user_grades",
                column: "grade_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_groups_group_id",
                table: "user_groups",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_subjects_subject_id",
                table: "user_subjects",
                column: "subject_id");

            migrationBuilder.CreateIndex(
                name: "IX_users_created_by",
                table: "users",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_users_school_id",
                table: "users",
                column: "school_id");

            migrationBuilder.CreateIndex(
                name: "IX_users_updated_by",
                table: "users",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "users_document_id_key",
                table: "users",
                column: "document_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "users_email_key",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_activities_activity_types_ActivityTypeId",
                table: "activities",
                column: "ActivityTypeId",
                principalTable: "activity_types",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_activities_trimester_TrimesterId",
                table: "activities",
                column: "TrimesterId",
                principalTable: "trimester",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_activities_users_created_by",
                table: "activities",
                column: "created_by",
                principalTable: "users",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_activities_users_updated_by",
                table: "activities",
                column: "updated_by",
                principalTable: "users",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "activities_teacher_id_fkey",
                table: "activities",
                column: "teacher_id",
                principalTable: "users",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "activities_group_id_fkey",
                table: "activities",
                column: "group_id",
                principalTable: "groups",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "activities_school_id_fkey",
                table: "activities",
                column: "school_id",
                principalTable: "schools",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "activities_subject_id_fkey",
                table: "activities",
                column: "subject_id",
                principalTable: "subjects",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_activity_types_schools_school_id",
                table: "activity_types",
                column: "school_id",
                principalTable: "schools",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_activity_types_users_created_by",
                table: "activity_types",
                column: "created_by",
                principalTable: "users",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_activity_types_users_updated_by",
                table: "activity_types",
                column: "updated_by",
                principalTable: "users",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_attendance_schools_school_id",
                table: "attendance",
                column: "school_id",
                principalTable: "schools",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_attendance_users_created_by",
                table: "attendance",
                column: "created_by",
                principalTable: "users",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_attendance_users_updated_by",
                table: "attendance",
                column: "updated_by",
                principalTable: "users",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "attendance_student_id_fkey",
                table: "attendance",
                column: "student_id",
                principalTable: "users",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "attendance_teacher_id_fkey",
                table: "attendance",
                column: "teacher_id",
                principalTable: "users",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "attendance_grade_id_fkey",
                table: "attendance",
                column: "grade_id",
                principalTable: "grade_levels",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "attendance_group_id_fkey",
                table: "attendance",
                column: "group_id",
                principalTable: "groups",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "audit_logs_school_id_fkey",
                table: "audit_logs",
                column: "school_id",
                principalTable: "schools",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "audit_logs_user_id_fkey",
                table: "audit_logs",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "counselor_assignments_grade_id_fkey",
                table: "counselor_assignments",
                column: "grade_id",
                principalTable: "grade_levels",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "counselor_assignments_group_id_fkey",
                table: "counselor_assignments",
                column: "group_id",
                principalTable: "groups",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "counselor_assignments_school_id_fkey",
                table: "counselor_assignments",
                column: "school_id",
                principalTable: "schools",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "counselor_assignments_user_id_fkey",
                table: "counselor_assignments",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_discipline_reports_schools_school_id",
                table: "discipline_reports",
                column: "school_id",
                principalTable: "schools",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_discipline_reports_users_created_by",
                table: "discipline_reports",
                column: "created_by",
                principalTable: "users",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_discipline_reports_users_updated_by",
                table: "discipline_reports",
                column: "updated_by",
                principalTable: "users",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "discipline_reports_student_id_fkey",
                table: "discipline_reports",
                column: "student_id",
                principalTable: "users",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "discipline_reports_teacher_id_fkey",
                table: "discipline_reports",
                column: "teacher_id",
                principalTable: "users",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "discipline_reports_grade_level_id_fkey",
                table: "discipline_reports",
                column: "grade_level_id",
                principalTable: "grade_levels",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "discipline_reports_group_id_fkey",
                table: "discipline_reports",
                column: "group_id",
                principalTable: "groups",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "discipline_reports_subject_id_fkey",
                table: "discipline_reports",
                column: "subject_id",
                principalTable: "subjects",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "email_configurations_school_id_fkey",
                table: "email_configurations",
                column: "school_id",
                principalTable: "schools",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_grade_levels_schools_school_id",
                table: "grade_levels",
                column: "school_id",
                principalTable: "schools",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_grade_levels_users_created_by",
                table: "grade_levels",
                column: "created_by",
                principalTable: "users",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_grade_levels_users_updated_by",
                table: "grade_levels",
                column: "updated_by",
                principalTable: "users",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_groups_users_created_by",
                table: "groups",
                column: "created_by",
                principalTable: "users",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_groups_users_updated_by",
                table: "groups",
                column: "updated_by",
                principalTable: "users",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "groups_school_id_fkey",
                table: "groups",
                column: "school_id",
                principalTable: "schools",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "messages_recipient_id_fkey",
                table: "messages",
                column: "recipient_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "messages_sender_id_fkey",
                table: "messages",
                column: "sender_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "messages_school_id_fkey",
                table: "messages",
                column: "school_id",
                principalTable: "schools",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_orientation_reports_schools_school_id",
                table: "orientation_reports",
                column: "school_id",
                principalTable: "schools",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_orientation_reports_users_created_by",
                table: "orientation_reports",
                column: "created_by",
                principalTable: "users",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_orientation_reports_users_updated_by",
                table: "orientation_reports",
                column: "updated_by",
                principalTable: "users",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "orientation_reports_student_id_fkey",
                table: "orientation_reports",
                column: "student_id",
                principalTable: "users",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "orientation_reports_teacher_id_fkey",
                table: "orientation_reports",
                column: "teacher_id",
                principalTable: "users",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "orientation_reports_subject_id_fkey",
                table: "orientation_reports",
                column: "subject_id",
                principalTable: "subjects",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "payment_concepts_created_by_fkey",
                table: "payment_concepts",
                column: "created_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "payment_concepts_updated_by_fkey",
                table: "payment_concepts",
                column: "updated_by",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "payment_concepts_school_id_fkey",
                table: "payment_concepts",
                column: "school_id",
                principalTable: "schools",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "payments_prematriculation_id_fkey",
                table: "payments",
                column: "prematriculation_id",
                principalTable: "prematriculations",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "payments_registered_by_fkey",
                table: "payments",
                column: "registered_by",
                principalTable: "users",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "payments_student_id_fkey",
                table: "payments",
                column: "student_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "payments_school_id_fkey",
                table: "payments",
                column: "school_id",
                principalTable: "schools",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "prematriculation_periods_created_by_fkey",
                table: "prematriculation_periods",
                column: "created_by",
                principalTable: "users",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "prematriculation_periods_updated_by_fkey",
                table: "prematriculation_periods",
                column: "updated_by",
                principalTable: "users",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "prematriculation_periods_school_id_fkey",
                table: "prematriculation_periods",
                column: "school_id",
                principalTable: "schools",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "prematriculations_parent_id_fkey",
                table: "prematriculations",
                column: "parent_id",
                principalTable: "users",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "prematriculations_student_id_fkey",
                table: "prematriculations",
                column: "student_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "prematriculations_school_id_fkey",
                table: "prematriculations",
                column: "school_id",
                principalTable: "schools",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_schools_users_admin_id",
                table: "schools",
                column: "admin_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_schools_users_admin_id",
                table: "schools");

            migrationBuilder.DropTable(
                name: "activity_attachments");

            migrationBuilder.DropTable(
                name: "attendance");

            migrationBuilder.DropTable(
                name: "audit_logs");

            migrationBuilder.DropTable(
                name: "counselor_assignments");

            migrationBuilder.DropTable(
                name: "discipline_reports");

            migrationBuilder.DropTable(
                name: "email_configurations");

            migrationBuilder.DropTable(
                name: "messages");

            migrationBuilder.DropTable(
                name: "orientation_reports");

            migrationBuilder.DropTable(
                name: "payments");

            migrationBuilder.DropTable(
                name: "security_settings");

            migrationBuilder.DropTable(
                name: "student_activity_scores");

            migrationBuilder.DropTable(
                name: "student_assignments");

            migrationBuilder.DropTable(
                name: "students");

            migrationBuilder.DropTable(
                name: "teacher_assignments");

            migrationBuilder.DropTable(
                name: "user_grades");

            migrationBuilder.DropTable(
                name: "user_groups");

            migrationBuilder.DropTable(
                name: "user_subjects");

            migrationBuilder.DropTable(
                name: "payment_concepts");

            migrationBuilder.DropTable(
                name: "prematriculations");

            migrationBuilder.DropTable(
                name: "activities");

            migrationBuilder.DropTable(
                name: "subject_assignments");

            migrationBuilder.DropTable(
                name: "prematriculation_periods");

            migrationBuilder.DropTable(
                name: "activity_types");

            migrationBuilder.DropTable(
                name: "trimester");

            migrationBuilder.DropTable(
                name: "grade_levels");

            migrationBuilder.DropTable(
                name: "groups");

            migrationBuilder.DropTable(
                name: "specialties");

            migrationBuilder.DropTable(
                name: "subjects");

            migrationBuilder.DropTable(
                name: "area");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "schools");
        }
    }
}
