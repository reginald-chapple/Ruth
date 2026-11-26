using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ruth.Modules.Lms.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialLmsWave6 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "lms");

            migrationBuilder.CreateTable(
                name: "inbox_messages",
                schema: "lms",
                columns: table => new
                {
                    message_id = table.Column<Guid>(type: "uuid", nullable: false),
                    consumer_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    processed_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inbox_messages", x => new { x.message_id, x.consumer_name });
                });

            migrationBuilder.CreateTable(
                name: "learning_activity_completions",
                schema: "lms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LearningClassId = table.Column<Guid>(type: "uuid", nullable: false),
                    LearningActivityId = table.Column<Guid>(type: "uuid", nullable: false),
                    LearningParticipantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsStudentCompleted = table.Column<bool>(type: "boolean", nullable: false),
                    IsFacilitatorCompleted = table.Column<bool>(type: "boolean", nullable: false),
                    RequiresGrading = table.Column<bool>(type: "boolean", nullable: false),
                    PointsEarned = table.Column<int>(type: "integer", nullable: false),
                    DueDateTimeUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedDateTimeUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    WasCompletedOnTime = table.Column<bool>(type: "boolean", nullable: false),
                    StudentComment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    FacilitatorComment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    SubmissionDataJson = table.Column<string>(type: "jsonb", nullable: true),
                    BinaryFileId = table.Column<Guid>(type: "uuid", nullable: true),
                    GradedByPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    GradedDateTimeUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_learning_activity_completions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "learning_classes",
                schema: "lms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LearningCourseId = table.Column<Guid>(type: "uuid", nullable: false),
                    LearningTermId = table.Column<Guid>(type: "uuid", nullable: true),
                    LearningGradingSystemId = table.Column<Guid>(type: "uuid", nullable: true),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: true),
                    VirtualRoomId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    IsPublic = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    TakesAttendance = table.Column<bool>(type: "boolean", nullable: false),
                    AllowOverbooking = table.Column<bool>(type: "boolean", nullable: false),
                    MaxStudents = table.Column<int>(type: "integer", nullable: true),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_learning_classes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "learning_courses",
                schema: "lms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LearningProgramId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    PublicName = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    CourseCode = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Summary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Credits = table.Column<int>(type: "integer", nullable: false),
                    MaxStudents = table.Column<int>(type: "integer", nullable: true),
                    AllowHistoricalAccess = table.Column<bool>(type: "boolean", nullable: false),
                    EnableAnnouncements = table.Column<bool>(type: "boolean", nullable: false),
                    IsPublic = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_learning_courses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "learning_grading_systems",
                schema: "lms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_learning_grading_systems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "learning_participants",
                schema: "lms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LearningClassId = table.Column<Guid>(type: "uuid", nullable: false),
                    PersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    LearningCompletionStatus = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    LearningCompletionDateTimeUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CurrentGradePercent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    CurrentGradeScaleName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    EnrolledDateTimeUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_learning_participants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "learning_programs",
                schema: "lms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    PublicName = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    Summary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    ConfigurationMode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    DefaultGradingSystemId = table.Column<Guid>(type: "uuid", nullable: true),
                    HighlightColor = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    IconCssClass = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    IsPublic = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsCompletionStatusTracked = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_learning_programs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "learning_terms",
                schema: "lms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LearningProgramId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    PublicName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: true),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    EnrollmentCloseDate = table.Column<DateOnly>(type: "date", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_learning_terms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                schema: "lms",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    occurred_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    processed_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    dead_lettered_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    retry_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    next_retry_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    locked_until_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    lock_token = table.Column<Guid>(type: "uuid", nullable: true),
                    error = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox_messages", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "learning_activities",
                schema: "lms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LearningClassId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ComponentType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    AssignTo = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    Points = table.Column<int>(type: "integer", nullable: false),
                    AvailabilityCriteria = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    AvailableDateDefault = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AvailableDateOffsetDays = table.Column<int>(type: "integer", nullable: true),
                    DueDateCriteria = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    DueDateDefault = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DueDateOffsetDays = table.Column<int>(type: "integer", nullable: true),
                    IsStudentCommentingEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    ActivityConfigJson = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_learning_activities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_learning_activities_learning_classes_LearningClassId",
                        column: x => x.LearningClassId,
                        principalSchema: "lms",
                        principalTable: "learning_classes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "learning_class_announcements",
                schema: "lms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LearningClassId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    Summary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DescriptionHtml = table.Column<string>(type: "text", nullable: false),
                    PublishDateTimeUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_learning_class_announcements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_learning_class_announcements_learning_classes_LearningClass~",
                        column: x => x.LearningClassId,
                        principalSchema: "lms",
                        principalTable: "learning_classes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "learning_class_content_pages",
                schema: "lms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LearningClassId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    ContentHtml = table.Column<string>(type: "text", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    StartDateTimeUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_learning_class_content_pages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_learning_class_content_pages_learning_classes_LearningClass~",
                        column: x => x.LearningClassId,
                        principalSchema: "lms",
                        principalTable: "learning_classes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "learning_course_requirements",
                schema: "lms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LearningCourseId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequiredLearningCourseId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequirementType = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_learning_course_requirements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_learning_course_requirements_learning_courses_LearningCours~",
                        column: x => x.LearningCourseId,
                        principalSchema: "lms",
                        principalTable: "learning_courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "learning_grading_scales",
                schema: "lms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LearningGradingSystemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ThresholdPercentage = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    IsPassing = table.Column<bool>(type: "boolean", nullable: false),
                    HighlightColor = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_learning_grading_scales", x => x.Id);
                    table.ForeignKey(
                        name: "FK_learning_grading_scales_learning_grading_systems_LearningGr~",
                        column: x => x.LearningGradingSystemId,
                        principalSchema: "lms",
                        principalTable: "learning_grading_systems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_learning_activities_LearningClassId_Order",
                schema: "lms",
                table: "learning_activities",
                columns: new[] { "LearningClassId", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_learning_activity_completions_LearningClassId_RequiresGradi~",
                schema: "lms",
                table: "learning_activity_completions",
                columns: new[] { "LearningClassId", "RequiresGrading" });

            migrationBuilder.CreateIndex(
                name: "IX_learning_activity_completions_LearningParticipantId_Learnin~",
                schema: "lms",
                table: "learning_activity_completions",
                columns: new[] { "LearningParticipantId", "LearningActivityId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_learning_activity_completions_PersonId",
                schema: "lms",
                table: "learning_activity_completions",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_learning_class_announcements_LearningClassId_PublishDateTim~",
                schema: "lms",
                table: "learning_class_announcements",
                columns: new[] { "LearningClassId", "PublishDateTimeUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_learning_class_content_pages_LearningClassId_Order",
                schema: "lms",
                table: "learning_class_content_pages",
                columns: new[] { "LearningClassId", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_learning_classes_LearningCourseId",
                schema: "lms",
                table: "learning_classes",
                column: "LearningCourseId");

            migrationBuilder.CreateIndex(
                name: "IX_learning_course_requirements_LearningCourseId_RequiredLearn~",
                schema: "lms",
                table: "learning_course_requirements",
                columns: new[] { "LearningCourseId", "RequiredLearningCourseId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_learning_courses_LearningProgramId_CourseCode",
                schema: "lms",
                table: "learning_courses",
                columns: new[] { "LearningProgramId", "CourseCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_learning_grading_scales_LearningGradingSystemId_Order",
                schema: "lms",
                table: "learning_grading_scales",
                columns: new[] { "LearningGradingSystemId", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_learning_participants_LearningClassId_PersonId",
                schema: "lms",
                table: "learning_participants",
                columns: new[] { "LearningClassId", "PersonId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_learning_participants_PersonId",
                schema: "lms",
                table: "learning_participants",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_learning_terms_LearningProgramId",
                schema: "lms",
                table: "learning_terms",
                column: "LearningProgramId");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_processed_on_utc_next_retry_on_utc",
                schema: "lms",
                table: "outbox_messages",
                columns: new[] { "processed_on_utc", "next_retry_on_utc" },
                filter: "\"processed_on_utc\" IS NULL AND \"dead_lettered_on_utc\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "inbox_messages",
                schema: "lms");

            migrationBuilder.DropTable(
                name: "learning_activities",
                schema: "lms");

            migrationBuilder.DropTable(
                name: "learning_activity_completions",
                schema: "lms");

            migrationBuilder.DropTable(
                name: "learning_class_announcements",
                schema: "lms");

            migrationBuilder.DropTable(
                name: "learning_class_content_pages",
                schema: "lms");

            migrationBuilder.DropTable(
                name: "learning_course_requirements",
                schema: "lms");

            migrationBuilder.DropTable(
                name: "learning_grading_scales",
                schema: "lms");

            migrationBuilder.DropTable(
                name: "learning_participants",
                schema: "lms");

            migrationBuilder.DropTable(
                name: "learning_programs",
                schema: "lms");

            migrationBuilder.DropTable(
                name: "learning_terms",
                schema: "lms");

            migrationBuilder.DropTable(
                name: "outbox_messages",
                schema: "lms");

            migrationBuilder.DropTable(
                name: "learning_classes",
                schema: "lms");

            migrationBuilder.DropTable(
                name: "learning_courses",
                schema: "lms");

            migrationBuilder.DropTable(
                name: "learning_grading_systems",
                schema: "lms");
        }
    }
}
