using FluentValidation;
using Namezr.Features.Creators.Data;

namespace Namezr.Features.Creators.Models;

public class CreatorProfileFromModel
{
    public string DisplayName { get; set; } = string.Empty;
    public CreatorVisibility Visibility { get; set; }

    public IFormFile? LogoReplacement { get; set; }

    [RegisterSingleton(typeof(IValidator<CreatorProfileFromModel>))]
    public class Validator : AbstractValidator<CreatorProfileFromModel>
    {
        public Validator()
        {
            RuleFor(x => x.DisplayName)
                .NotEmpty()
                .MinimumLength(3)
                .MaximumLength(CreatorEntity.MaxDisplayNameLength);
        }
    }
}

// Underlying values are persisted in the database and thus must never change
public enum CreatorVisibility
{
    Public = 0,
    Unlisted = 1,
}