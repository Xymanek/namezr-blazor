using Namezr.Features.Questionnaires.Data;

namespace Namezr.Features.Eligibility.Data;

public class EligibilityConfigurationEntity
{
    public long Id { get; set; }

    public required EligibilityConfigurationOwnershipType OwnershipType { get; init; }

    public QuestionnaireEntity? Questionnaire { get; set; }
    public Guid? QuestionnaireId { get; set; }

    public ICollection<EligibilityOptionEntity>? Options { get; set; }
}

public enum EligibilityConfigurationOwnershipType
{
    Questionnaire = 1,
    Poll = 2,
    Raffle = 3,
}