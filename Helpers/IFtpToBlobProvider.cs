using Microsoft.Extensions.Logging;

namespace FunctionApp.Helpers
{
    public interface IFtpToBlobProvider
    {
        Task ProcessAsync(Configuration configuration, ILogger logger);
    }
}
