using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.CustomFormats
{
    public class ComicImageResolutionConditionValidator : AbstractValidator<ComicImageResolutionCondition>
    {
        public ComicImageResolutionConditionValidator()
        {
            RuleFor(c => c.MinScore).GreaterThanOrEqualTo(0).LessThanOrEqualTo(1);
            RuleFor(c => c.MaxScore).GreaterThan(c => c.MinScore).LessThanOrEqualTo(1.0001);
        }
    }

    /// <summary>
    /// Matches based on ComicFile.ImageQualityScore ranges (0.0 – 1.0).
    /// Score = avg(page_width * page_height / 1920 / 1080), capped at 1.0.
    /// Example: MinScore=0.8, MaxScore=1.0 matches HD-quality files.
    /// </summary>
    public class ComicImageResolutionCondition : CustomFormatSpecificationBase
    {
        private static readonly ComicImageResolutionConditionValidator Validator = new ComicImageResolutionConditionValidator();

        public override int Order => 11;
        public override string ImplementationName => "Comic Image Resolution";

        [FieldDefinition(1, Label = "Minimum Quality Score", HelpText = "0.0 = low res, 1.0 = full HD or better. Release must have a score >= this value.", Unit = "", Type = FieldType.Number)]
        public double MinScore { get; set; }

        [FieldDefinition(2, Label = "Maximum Quality Score", HelpText = "Release must have a score < this value. Use 1.001 for no upper limit.", Unit = "", Type = FieldType.Number)]
        public double MaxScore { get; set; } = 1.001;

        protected override bool IsSatisfiedByWithoutNegate(CustomFormatInput input)
        {
            var score = input.ImageQualityScore ?? input.ComicFile?.ImageQualityScore;

            if (score == null)
            {
                return false;
            }

            return score.Value >= MinScore && score.Value < MaxScore;
        }

        public override NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
