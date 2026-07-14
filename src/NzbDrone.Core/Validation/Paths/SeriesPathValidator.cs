using System.Linq;
using FluentValidation.Validators;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Issues;

namespace NzbDrone.Core.Validation.Paths
{
    public class SeriesPathValidator : PropertyValidator
    {
        private readonly ISeriesService _seriesService;
        private readonly IDiskProvider _diskProvider;

        public SeriesPathValidator(ISeriesService seriesService, IDiskProvider diskProvider)
        {
            _seriesService = seriesService;
            _diskProvider = diskProvider;
        }

        protected override string GetDefaultMessageTemplate() => "Path '{path}' is already configured for another series";

        protected override bool IsValid(PropertyValidatorContext context)
        {
            if (context.PropertyValue == null)
            {
                return true;
            }

            context.MessageFormatter.AppendArgument("path", context.PropertyValue.ToString());

            dynamic instance = context.ParentContext.InstanceToValidate;
            var instanceId = (int)instance.Id;

            var path = context.PropertyValue.ToString();
            var sharedWithOtherSeries = _seriesService.AllSeriesPaths()
                .Any(s => s.Value.PathEquals(path) && s.Key != instanceId);

            if (!sharedWithOtherSeries)
            {
                return true;
            }

            // Multiple series may share an EXISTING folder (Mylar-style layouts
            // keep annuals and collection lines beside their parent series).
            // A shared path that doesn't exist on disk is still rejected — that
            // is a typo, not a layout.
            return _diskProvider.FolderExists(path);
        }
    }
}
