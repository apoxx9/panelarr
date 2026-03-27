using System.Collections.Generic;
using FluentValidation;
using NzbDrone.Core.Annotations;

namespace NzbDrone.Core.ImportLists.Goodreads
{
    public class GoodreadsBookshelfImportListSettingsValidator : GoodreadsSettingsBaseValidator<GoodreadsBookshelfImportListSettings>
    {
        public GoodreadsBookshelfImportListSettingsValidator()
        : base()
        {
            RuleFor(c => c.IssueshelfIds).NotEmpty();
        }
    }

    public class GoodreadsBookshelfImportListSettings : GoodreadsSettingsBase<GoodreadsBookshelfImportListSettings>
    {
        public GoodreadsBookshelfImportListSettings()
        {
            IssueshelfIds = new string[] { };
        }

        [FieldDefinition(1, Label = "Issueshelves", Type = FieldType.Issueshelf)]
        public IEnumerable<string> IssueshelfIds { get; set; }

        protected override AbstractValidator<GoodreadsBookshelfImportListSettings> Validator => new GoodreadsBookshelfImportListSettingsValidator();
    }
}
