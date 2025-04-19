using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Namezr.Features.Identity.Data;

namespace Namezr.Features.Polls.Data;

[EntityTypeConfiguration(typeof(PollChoiceEntityTypeConfiguration))]
public class PollChoiceEntity
{
    public Guid Id { get; set; }

    public PollEntity Poll { get; set; } = null!;
    public Guid PollId { get; set; }

    /// <summary>
    /// Must belong to the <see cref="Poll"/>.
    /// </summary>
    // If we ever want to support multiple options selected per user,
    // convert this FK to a "sub-table", i.e.
    // Poll -> Option  \ 
    // Poll -> Choice -> SelectedOption
    public PollOptionEntity Option { get; set; } = null!;

    public Guid OptionId { get; set; }

    public ApplicationUser User { get; set; } = null!;
    public Guid UserId { get; set; }

    public decimal Weight { get; set; }
}

internal class PollChoiceEntityTypeConfiguration : IEntityTypeConfiguration<PollChoiceEntity>
{
    public void Configure(EntityTypeBuilder<PollChoiceEntity> builder)
    {
        builder.HasAlternateKey(choice => new { choice.UserId, choice.PollId });
    }
}