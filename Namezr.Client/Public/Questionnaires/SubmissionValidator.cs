using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Namezr.Client.Studio.Questionnaires.Edit;

namespace Namezr.Client.Public.Questionnaires;

public class SubmissionValidator : ComponentBase
{
    [CascadingParameter]
    private EditContext EditContext { get; set; }

    [Parameter]
    public QuestionnaireConfigModel ConfigModel { get; set; }

    private ValidationMessageStore? _validationMessageStore;

    [Inject]
    private IServiceProvider ServiceProvider { get; set; }

    protected override void OnInitialized()
    {
        if (EditContext is null)
        {
            throw new InvalidOperationException(
                $"{nameof(SubmissionValidator)} requires a cascading " +
                $"parameter of type {nameof(EditContext)}. " +
                $"For example, you can use {nameof(SubmissionValidator)} " +
                $"inside an {nameof(EditForm)}.");
        }

        _validationMessageStore = new ValidationMessageStore(EditContext);

        EditContext.OnValidationRequested += HandleValidationRequested;
        EditContext.OnFieldChanged += HandleFieldChanged;
    }

    private void HandleValidationRequested(object? sender, ValidationRequestedEventArgs e)
    {
        _validationMessageStore?.Clear();

        foreach (QuestionnaireConfigFieldModel field in ConfigModel.Fields)
        {
            ValidateField(field.Id);
        }
    }

    private void HandleFieldChanged(object? sender, FieldChangedEventArgs e)
    {
        _validationMessageStore?.Clear(e.FieldIdentifier);

        // TODO: how to get the GUID correctly
        ValidateField(Guid.Parse(e.FieldIdentifier.FieldName));
    }

    private void ValidateField(Guid fieldId)
    {
        QuestionnaireConfigFieldModel fieldConfig = ConfigModel.Fields.Single(x => x.Id == fieldId);

        // TODO: how to use the GUID correctly
        FieldIdentifier fieldIdentifier = new(Model, fieldId.ToString());

        switch (fieldConfig.Type)
        {
            case QuestionnaireFieldType.Text when fieldConfig.TextOptions is not null:
            {
                string value = (string)Model[fieldId];

                if (
                    fieldConfig.TextOptions.MinLength is not null &&
                    value.Length < fieldConfig.TextOptions.MinLength
                )
                {
                    _validationMessageStore?.Add(
                        fieldIdentifier, $"Value must be at least {fieldConfig.TextOptions.MinLength} characters"
                    );
                }

                if (
                    fieldConfig.TextOptions.MaxLength is not null &&
                    value.Length > fieldConfig.TextOptions.MaxLength
                )
                {
                    _validationMessageStore?.Add(
                        fieldIdentifier, $"Value must be at most {fieldConfig.TextOptions.MaxLength} characters"
                    );
                }

                break;
            }
            
            case QuestionnaireFieldType.Number when fieldConfig.NumberOptions is not null:
            {
                decimal value = (decimal)Model[fieldId];

                if (
                    fieldConfig.NumberOptions.MinValue is not null &&
                    value < fieldConfig.NumberOptions.MinValue
                )
                {
                    _validationMessageStore?.Add(
                        fieldIdentifier, $"Value must be at least {fieldConfig.NumberOptions.MinValue}"
                    );
                }

                if (
                    fieldConfig.NumberOptions.MaxValue is not null &&
                    value > fieldConfig.NumberOptions.MaxValue
                )
                {
                    _validationMessageStore?.Add(
                        fieldIdentifier, $"Value must be at most {fieldConfig.NumberOptions.MaxValue}"
                    );
                }

                break;
            }

            case QuestionnaireFieldType.FileUpload when fieldConfig.FileUploadOptions is not null:
            {
                // TODO: validate file size
                break;
            }
        }
    }

    private Dictionary<Guid, object> Model => (Dictionary<Guid, object>)EditContext.Model;
}