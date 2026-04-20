using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.CustomFormats
{
    public class PageCountConditionValidator : AbstractValidator<PageCountCondition>
    {
        public PageCountConditionValidator()
        {
            RuleFor(c => c.MinPages).GreaterThanOrEqualTo(0);
            RuleFor(c => c.MaxPages).GreaterThan(c => c.MinPages);
        }
    }

    /// <summary>
    /// Matches based on ComicFile.ImageCount (page count) ranges.
    /// Useful to distinguish single issues (~20-32 pages) from TPBs (100+ pages).
    /// </summary>
    public class PageCountCondition : CustomFormatSpecificationBase
    {
        private static readonly PageCountConditionValidator Validator = new PageCountConditionValidator();

        public override int Order => 12;
        public override string ImplementationName => "Page Count";

        [FieldDefinition(1, Label = "Minimum Pages", HelpText = "Release must have at least this many pages.", Type = FieldType.Number)]
        public int MinPages { get; set; }

        [FieldDefinition(2, Label = "Maximum Pages", HelpText = "Release must have fewer than this many pages. Use 99999 for no upper limit.", Type = FieldType.Number)]
        public int MaxPages { get; set; } = 99999;

        protected override bool IsSatisfiedByWithoutNegate(CustomFormatInput input)
        {
            var pages = input.ImageCount ?? input.ComicFile?.ImageCount;

            if (pages == null)
            {
                return false;
            }

            return pages.Value >= MinPages && pages.Value < MaxPages;
        }

        public override NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
