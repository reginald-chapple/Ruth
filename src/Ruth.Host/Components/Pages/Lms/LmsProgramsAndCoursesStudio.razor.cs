using Microsoft.AspNetCore.Components;
using Ruth.Modules.Lms.Contracts.Classes;
using Ruth.Modules.Lms.Contracts.Courses;
using Ruth.Modules.Lms.Contracts.Grading;
using Ruth.Modules.Lms.Contracts.Programs;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Host.Components.Pages.Lms;

public sealed partial class LmsProgramsAndCoursesStudio : ComponentBase
{
    private IReadOnlyList<LearningProgramResponse> _programs = [];
    private IReadOnlyList<LearningCourseResponse> _courses = [];
    private string _programName = "Leadership Certification Track";
    private string _courseName = "Foundations of Community Leadership";
    private string _courseCode = "LEAD-101";
    private string? _statusMessage;
    private bool _isError;

    [Inject]
    private IQueryDispatcher QueryDispatcher { get; set; } = default!;

    [Inject]
    private ICommandDispatcher CommandDispatcher { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        await ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        var pRes = await QueryDispatcher.QueryAsync<
            GetLearningProgramsQuery,
            IReadOnlyList<LearningProgramResponse>
        >(new GetLearningProgramsQuery());
        if (pRes.IsSuccess)
        {
            _programs = pRes.Value;
        }

        var cRes = await QueryDispatcher.QueryAsync<
            GetLearningCoursesQuery,
            IReadOnlyList<LearningCourseResponse>
        >(new GetLearningCoursesQuery());
        if (cRes.IsSuccess)
        {
            _courses = cRes.Value;
        }
    }

    private async Task CreateProgramAndCourseAsync()
    {
        var gradingRes = await CommandDispatcher.SendAsync<
            CreateLearningGradingSystemCommand,
            LearningGradingSystemResponse
        >(
            new CreateLearningGradingSystemCommand(
                $"{_programName} Scale",
                [
                    new LearningGradingScaleSpec("Pass", 70m, IsPassing: true),
                    new LearningGradingScaleSpec("Incomplete", 0m, IsPassing: false),
                ]
            )
        );
        if (gradingRes.IsFailure)
        {
            _isError = true;
            _statusMessage = gradingRes.PrimaryError.Description;
            return;
        }

        var progRes = await CommandDispatcher.SendAsync<
            CreateLearningProgramCommand,
            LearningProgramResponse
        >(
            new CreateLearningProgramCommand(
                _programName,
                ConfigurationMode: LearningProgramConfigurationMode.OnDemandLearning,
                DefaultGradingSystemId: gradingRes.Value.Id
            )
        );
        if (progRes.IsFailure)
        {
            _isError = true;
            _statusMessage = progRes.PrimaryError.Description;
            return;
        }

        var courseRes = await CommandDispatcher.SendAsync<
            CreateLearningCourseCommand,
            LearningCourseResponse
        >(new CreateLearningCourseCommand(progRes.Value.Id, _courseName, _courseCode, Credits: 2));
        if (courseRes.IsFailure)
        {
            _isError = true;
            _statusMessage = courseRes.PrimaryError.Description;
            return;
        }

        _ = await CommandDispatcher.SendAsync<CreateLearningClassCommand, LearningClassResponse>(
            new CreateLearningClassCommand(
                courseRes.Value.Id,
                "Open Self-Paced Cohort",
                LearningGradingSystemId: gradingRes.Value.Id
            )
        );

        _isError = false;
        _statusMessage =
            $"Created program '{progRes.Value.PublicName}' and course '{courseRes.Value.CourseCode}'.";
        await ReloadAsync();
    }
}
