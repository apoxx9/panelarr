using System.Linq;
using FluentValidation.Validators;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Books;

namespace NzbDrone.Core.Validation.Paths
{
    public class SeriesAncestorValidator : PropertyValidator
    {
        private readonly ISeriesService _authorService;

        public SeriesAncestorValidator(ISeriesService authorService)
        {
            _authorService = authorService;
        }

        protected override string GetDefaultMessageTemplate() => "Path '{path}' is an ancestor of an existing author";

        protected override bool IsValid(PropertyValidatorContext context)
        {
            if (context.PropertyValue == null)
            {
                return true;
            }

            context.MessageFormatter.AppendArgument("path", context.PropertyValue.ToString());

            return !_authorService.AllSeriesPaths().Any(s => context.PropertyValue.ToString().IsParentPath(s.Value));
        }
    }
}
