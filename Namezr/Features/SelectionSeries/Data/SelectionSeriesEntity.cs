using System.ComponentModel.DataAnnotations;
using Namezr.Features.Eligibility.Data;
using Namezr.Features.Questionnaires.Data;

namespace Namezr.Features.SelectionSeries.Data;

public class SelectionSeriesEntity
{
    public Guid Id { get; set; }

    [MaxLength(100)]
    public required string Name { get; init; }

    // Note: until eligibility config, every selection owner (e.g. questionnaire)
    // can own multiple selection series. This is exposed in this UI.
    
    public required EligibilityConfigurationOwnershipType OwnershipType { get; init; }

    public QuestionnaireEntity? Questionnaire { get; set; }
    public Guid QuestionnaireId { get; set; }

    public required int CompleteCyclesCount { get; set; }

    [ConcurrencyCheck]
    public Guid CompletedSelectionMarker { get; set; }

    public ICollection<SelectionBatchEntity>? Batches { get; set; }
    public ICollection<SelectionUserDataEntity>? UserData { get; set; }
}