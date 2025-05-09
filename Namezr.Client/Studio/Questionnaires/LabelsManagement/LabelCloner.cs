using Namezr.Client.Shared;
using Riok.Mapperly.Abstractions;

namespace Namezr.Client.Studio.Questionnaires.LabelsManagement;

[Mapper]
public static partial class LabelCloner
{
    public static partial SubmissionLabelModel Clone(this SubmissionLabelModel model);
}