﻿using FluentValidation;

namespace Namezr.Client.Studio.Questionnaires.Selection;

public class NewSelectionBatchRequest
{
    public required Guid SeriesId { get; init; }
    public required NewSelectionBatchOptionsModel BatchOptions { get; init; }

    [RegisterSingleton(typeof(IValidator<NewSelectionBatchRequest>))]
    public class Validator : AbstractValidator<NewSelectionBatchRequest>
    {
        public Validator(IValidator<NewSelectionBatchOptionsModel> optionsValidator)
        {
            RuleFor(x => x.SeriesId)
                .NotEqual(Guid.Empty);

            RuleFor(x => x.BatchOptions)
                .SetValidator(optionsValidator);
        }
    }
}