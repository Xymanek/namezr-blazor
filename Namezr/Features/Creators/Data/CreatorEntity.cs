using System.ComponentModel.DataAnnotations;
using Namezr.Features.Creators.Models;
using Namezr.Features.Polls.Data;
using Namezr.Features.Questionnaires.Data;

namespace Namezr.Features.Creators.Data;

public class CreatorEntity
{
    public Guid Id { get; set; }

    [MaxLength(MaxDisplayNameLength)]
    public required string DisplayName { get; set; }

    public CreatorVisibility Visibility { get; set; } = CreatorVisibility.Public;

    public Guid? LogoFileId { get; set; }

    // TODO: Banner?

    public ICollection<SupportTargetEntity>? SupportTargets { get; set; }
    public ICollection<CreatorStaffEntity>? Staff { get; set; }

    public ICollection<QuestionnaireEntity>? Questionnaires { get; set; }
    public ICollection<PollEntity>? Polls { get; set; }

    public const int MaxDisplayNameLength = 100;
}