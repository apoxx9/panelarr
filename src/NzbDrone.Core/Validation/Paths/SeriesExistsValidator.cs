using FluentValidation.Validators;
using NzbDrone.Core.Issues;

namespace NzbDrone.Core.Validation.Paths
{
    public class SeriesExistsValidator : PropertyValidator
    {
        private readonly ISeriesService _authorService;

        public SeriesExistsValidator(ISeriesService authorService)
        {
            _authorService = authorService;
        }

        protected override string GetDefaultMessageTemplate() => "This author has already been added";

        protected override bool IsValid(PropertyValidatorContext context)
        {
            if (context.PropertyValue == null)
            {
                return true;
            }

            var foreignSeriesId = context.PropertyValue.ToString();

            return _authorService.FindById(foreignSeriesId) == null;
        }
    }
}
