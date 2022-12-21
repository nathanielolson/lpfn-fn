using WinSCP;
using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using FunctionApp.Helpers;

namespace FunctionApp.Functions
{
    public class SftToBlobHttpTrigger
    {
        private readonly ILogger _logger;
        private readonly IFtpToBlobProvider _ftpToBlobsService;

        public SftToBlobHttpTrigger(ILoggerFactory loggerFactory, IFtpToBlobProvider ftpToBlobsService)
        {
            _logger = loggerFactory.CreateLogger<SftToBlobHttpTrigger>();
            _ftpToBlobsService = ftpToBlobsService;
        }

        [Function("AzureBlobToFtpServer")]
        public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req)
        {
            _logger.LogInformation("Starting AzureBlobToFtpServer function ");
            try
            {
                var configuration = new Configuration()
                {
                    FtpRootDirectory        = GetEnvirnomentVariable("RemoteRootDirectory"),
                    Protocol                = GetEnvirnomentVariable("RemoteProtocole") != null && GetEnvirnomentVariable("FtpProtocol").ToLower() == "ftp" 
                                              ? Protocol.Ftp : Protocol.Sftp,
                    FtpHost                 = GetEnvirnomentVariable("RemoteHostName"),
                    FtpUserName             = GetEnvirnomentVariable("RemoteUserName"),
                    FtpPassword             = GetEnvirnomentVariable("RemotePassword"),
                    FtpPort                 = Convert.ToInt32(GetEnvirnomentVariable("RemotePort")),
                    SshHostKeyFingerprint   = GetEnvirnomentVariable("SshHostKeyFingerprint"),
                    PrivateKeyName          = GetEnvirnomentVariable("PrivateKeyName"),
                    PrivateKeyPassPharase   = GetEnvirnomentVariable("PrivateKeyPassPharase"),
                    BlobConnectionString    = GetEnvirnomentVariable("BlobConnectionString"),
                    BlobContainer           = GetEnvirnomentVariable("BlobContainer"),
                    ContainerFolder         = GetEnvirnomentVariable("ContainerFolder"),
                    RemoveFilesORemote      = GetEnvirnomentVariable("RemoveFilesOnRemote") != null 
                                              ? Convert.ToBoolean(GetEnvirnomentVariable("RemoveFilesORemote")) : false,
                    ShouldDecrypt           = GetEnvirnomentVariable("ShouldDecrypt") != null 
                                              ? Convert.ToBoolean(GetEnvirnomentVariable("ShouldDecrypt")) : false
                };

                await _ftpToBlobsService.ProcessAsync(configuration, _logger);

            }
            catch (Exception ex)
            {

                _logger.LogError($"Error occurred {ex.Message}");
            }

            _logger.LogInformation("Completed AzureBlobToFtpServer function execution");

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

            response.WriteString("done");

            return response;
        }
        private string GetEnvirnomentVariable(string name)
        {
            return Environment.GetEnvironmentVariable(name);
        }
    }
}