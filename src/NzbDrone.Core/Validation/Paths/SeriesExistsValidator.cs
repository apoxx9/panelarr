using FluentValidation.Validators;
using NzbDrone.Core.Issues;

namespace NzbDrone.Core.Validation.Paths
{
    public class SeriesExistsValidator : PropertyValidator
    {
        private readonly ISeriesService _seriesService;

        public SeriesExistsValidator(ISeriesService seriesService)
        {
            _seriesService = seriesService;
        }

        protected override string GetDefaultMessageTemplate() => "This series has already been added";

        protected override bool IsValid(PropertyValidatorContext context)
        {
            if (context.PropertyValue == null)
            {
                return true;
            }

            var foreignSeriesId = context.PropertyValue.ToString();

            return _seriesService.FindById(foreignSeriesId) == null;
        }
    }
}
