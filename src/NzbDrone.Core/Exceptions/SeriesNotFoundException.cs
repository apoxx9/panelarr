using NzbDrone.Common.Exceptions;

namespace NzbDrone.Core.Exceptions
{
    public class SeriesNotFoundException : NzbDroneException
    {
        public string ForeignSeriesId { get; set; }

        public SeriesNotFoundException(string foreignSeriesId)
            : base($"Series with id {foreignSeriesId} was not found, it may have been removed from the metadata server.")
        {
            ForeignSeriesId = foreignSeriesId;
        }

        public SeriesNotFoundException(string foreignSeriesId, string message, params object[] args)
            : base(message, args)
        {
            ForeignSeriesId = foreignSeriesId;
        }

        public SeriesNotFoundException(string foreignSeriesId, string message)
            : base(message)
        {
            ForeignSeriesId = foreignSeriesId;
        }
    }
}
