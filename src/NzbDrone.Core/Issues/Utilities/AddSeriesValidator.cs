using FluentValidation;
using FluentValidation.Results;
using NzbDrone.Core.Validation;
using NzbDrone.Core.Validation.Paths;

namespace NzbDrone.Core.Issues
{
    public interface IAddSeriesValidator
    {
        ValidationResult Validate(Series instance);
    }

    public class AddSeriesValidator : AbstractValidator<Series>, IAddSeriesValidator
    {
        public AddSeriesValidator(RootFolderValidator rootFolderValidator,
                                  RecycleBinValidator recycleBinValidator,
                                  SeriesPathValidator seriesPathValidator,
                                  SeriesAncestorValidator seriesAncestorValidator,
                                  QualityProfileExistsValidator qualityProfileExistsValidator,
                                  MetadataProfileExistsValidator metadataProfileExistsValidator)
        {
            RuleFor(c => c.Path).Cascade(CascadeMode.Stop)
                                .IsValidPath()
                                .SetValidator(rootFolderValidator)
                                .SetValidator(recycleBinValidator)
                                .SetValidator(seriesPathValidator)
                                .SetValidator(seriesAncestorValidator);

            RuleFor(c => c.QualityProfileId).SetValidator(qualityProfileExistsValidator);

            // MetadataProfileId has been removed from Series; no validation needed
        }
    }
}
