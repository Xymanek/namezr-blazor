using System.ComponentModel.DataAnnotations;
using Namezr.Features.Questionnaires.Data;

namespace Namezr.Features.Creators.Data;

public class CreatorEntity
{
    public Guid Id { get; set; }

    [MaxLength(MaxDisplayNameLength)]
    public required string DisplayName { get; set; }

    public Guid? LogoFileId { get; set; }

    // TODO: Banner?

    public ICollection<SupportTargetEntity>? SupportTargets { get; set; }
    public ICollection<CreatorStaffEntity>? Staff { get; set; }

    public ICollection<QuestionnaireEntity>? Questionnaires { get; set; }
    
    public const int MaxDisplayNameLength = 100;
}