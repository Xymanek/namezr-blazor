using System.Collections.Generic;

namespace Namezr.Client.Helpers
{
    public class ValidationProblemDetailsDto
    {
        public Dictionary<string, string[]> Errors { get; set; } = new Dictionary<string, string[]>();
    }
}