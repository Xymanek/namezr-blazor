﻿using System.ComponentModel.DataAnnotations;
using Namezr.Client.Studio.Questionnaires.Edit;
using Namezr.Features.Creators.Data;

namespace Namezr.Features.Questionnaires.Data;

public class QuestionnaireEntity
{
    public Guid Id { get; set; }

    public CreatorEntity Creator { get; set; } = null!;
    public Guid CreatorId { get; set; }
    
    [MaxLength(QuestionnaireEditModel.TitleMaxLength)]
    public required string Title { get; set; }

    [MaxLength(QuestionnaireEditModel.DescriptionMaxLength)]
    public required string? Description { get; set; }

    public required QuestionnaireApprovalMode ApprovalMode { get; set; }

    public ICollection<QuestionnaireFieldEntity>? Fields { get; set; }
    public ICollection<QuestionnaireVersionEntity>? Versions { get; set; }
}