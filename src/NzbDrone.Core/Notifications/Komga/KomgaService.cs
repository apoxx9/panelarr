using System;
using FluentValidation.Results;
using NLog;

namespace NzbDrone.Core.Notifications.Komga
{
    public interface IKomgaService
    {
        void TriggerLibraryScan(KomgaSettings settings);
        ValidationFailure Test(KomgaSettings settings);
    }

    public class KomgaService : IKomgaService
    {
        private readonly IKomgaProxy _proxy;
        private readonly Logger _logger;

        public KomgaService(IKomgaProxy proxy, Logger logger)
        {
            _proxy = proxy;
            _logger = logger;
        }

        public void TriggerLibraryScan(KomgaSettings settings)
        {
            _proxy.TriggerLibraryScan(settings);
        }

        public ValidationFailure Test(KomgaSettings settings)
        {
            try
            {
                _proxy.TriggerLibraryScan(settings);
            }
            catch (KomgaException ex)
            {
                _logger.Error(ex, "Unable to connect to Komga server");
                return new ValidationFailure("BaseUrl", "Unable to connect to Komga server");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Unable to connect to Komga server");
                return new ValidationFailure("BaseUrl", "Unable to connect to Komga server");
            }

            return null;
        }
    }
}
