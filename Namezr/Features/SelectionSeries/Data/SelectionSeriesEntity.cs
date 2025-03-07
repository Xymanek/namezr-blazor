using System.ComponentModel.DataAnnotations;
using Namezr.Features.Eligibility.Data;
using Namezr.Features.Questionnaires.Data;

namespace Namezr.Features.SelectionSeries.Data;

public class SelectionSeriesEntity
{
    public Guid Id { get; set; }

    [MaxLength(100)]
    public required string Name { get; init; }

    public required EligibilityConfigurationOwnershipType OwnershipType { get; init; }

    public QuestionnaireEntity? Questionnaire { get; set; }
    public Guid QuestionnaireId { get; set; }

    public required int CompleteCyclesCount { get; init; }

    [ConcurrencyCheck]
    public Guid CompletedSelectionMarker { get; set; }

    public ICollection<SelectionBatchEntity>? Batches { get; set; }
    public ICollection<SelectionUserDataEntity>? UserData { get; set; }
}