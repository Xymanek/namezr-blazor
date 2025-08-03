using System.Text.RegularExpressions;

namespace Namezr.Features.Questionnaires.Components;

public partial class MultiLineString
{
    [GeneratedRegex(@"(\r\n|\r|\n)")]
    private partial Regex GetNewLineRegex();
}