﻿using FluentValidation;
using Namezr.Features.Creators.Data;

namespace Namezr.Features.Creators.Models;

public class CreatorProfileFromModel
{
    public string DisplayName { get; set; } = string.Empty;

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