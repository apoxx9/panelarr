using System.Linq;
using FluentValidation.Validators;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Books;

namespace NzbDrone.Core.Validation.Paths
{
    public class SeriesPathValidator : PropertyValidator
    {
        private readonly ISeriesService _authorService;

        public SeriesPathValidator(ISeriesService authorService)
        {
            _authorService = authorService;
        }

        protected override string GetDefaultMessageTemplate() => "Path '{path}' is already configured for another author";

        protected override bool IsValid(PropertyValidatorContext context)
        {
            if (context.PropertyValue == null)
            {
                return true;
            }

            context.MessageFormatter.AppendArgument("path", context.PropertyValue.ToString());

            dynamic instance = context.ParentContext.InstanceToValidate;
            var instanceId = (int)instance.Id;

            return !_authorService.AllSeriesPaths().Any(s => s.Value.PathEquals(context.PropertyValue.ToString()) && s.Key != instanceId);
        }
    }
}
