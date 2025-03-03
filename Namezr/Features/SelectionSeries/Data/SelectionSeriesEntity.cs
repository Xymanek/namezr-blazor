using Namezr.Features.Eligibility.Data;
using Namezr.Features.Questionnaires.Data;

namespace Namezr.Features.SelectionSeries.Data;

public class SelectionSeriesEntity
{
    public long Id { get; set; }
    
    public required EligibilityConfigurationOwnershipType OwnershipType { get; init; }

    public QuestionnaireEntity? Questionnaire { get; set; }

    public required int CompleteCyclesCount { get; init; }

    public ICollection<SelectionBatchEntity>? Batches { get; set; }
    public ICollection<SelectionUserDataEntity>? UserData { get; set; }
}