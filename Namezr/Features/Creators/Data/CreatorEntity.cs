using System.ComponentModel.DataAnnotations;
using Namezr.Features.Questionnaires.Data;

namespace Namezr.Features.Creators.Data;

public class CreatorEntity
{
    public Guid Id { get; set; }

    [MaxLength(100)]
    public required string DisplayName { get; set; }
    
    // TODO: Banner & small image

    public ICollection<SupportTargetEntity>? SupportTargets { get; set; }
    public ICollection<CreatorStaffEntity>? Staff { get; set; }
    
    public ICollection<QuestionnaireEntity>? Questionnaires { get; set; }
}