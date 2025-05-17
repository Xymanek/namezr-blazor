using Namezr.Client.Types;
using Namezr.Features.Eligibility.Services;
using Riok.Mapperly.Abstractions;

namespace Namezr.Features.Eligibility.Mappers;

[Mapper(UseDeepCloning = true)]
public static partial class EligibilityResultMapper
{
    public static partial EligibilityResultModel ToModel(this EligibilityResult result);
}