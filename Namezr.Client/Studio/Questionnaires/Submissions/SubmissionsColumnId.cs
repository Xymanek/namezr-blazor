using System.Diagnostics.CodeAnalysis;

namespace Namezr.Client.Studio.Questionnaires.Submissions;

internal record SubmissionsColumnId
{
    public SubmissionsColumnId()
    {
    }

    [SetsRequiredMembers]
    public SubmissionsColumnId(SubmissionsColumnType type)
    {
        Type = type;
    }
    
    public required SubmissionsColumnType Type { get; init; }
    
    public Guid? FieldId { get; init; }
};

internal enum SubmissionsColumnType
{
    SubmissionNumber,
    
    UserDisplayName,
    InitiallySubmittedAt,
    LastUpdateAt,
    
    IsApproved,
    Weight,
    
    Labels,
    EligibilityPlans,
    
    FieldValue,
}