using Microsoft.AspNetCore.Identity;
using Namezr.Features.Questionnaires.Data;

namespace Namezr.Features.Identity.Data;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public ApplicationUser()
    {
        Id = Guid.NewGuid();
    }

    public ICollection<QuestionnaireSubmissionEntity>? Submissions { get; set; }
}