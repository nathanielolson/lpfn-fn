using WinSCP;
using PgpCore;
using Microsoft.Extensions.Logging;
using Azure.Storage.Blobs;

namespace FunctionApp.Helpers
{
    public class FtpToBlobService : IFtpToBlobProvider
    {
        private readonly ILogger _logger;
        public FtpToBlobService(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<FtpToBlobService>();
        }
        public async Task ProcessAsync(Configuration configuration, ILogger _logger)
        {
            var currentDateAsNumber = DateTime.Now.ToString("fffffff");
            var downloadFilesPath = Path.GetTempPath() + currentDateAsNumber;
            var decryptedFilesPath = Path.GetTempPath() + currentDateAsNumber + "\\decryptedPath";
            var remoteDirectory = configuration.FtpRootDirectory;

            try
            {
                SessionOptions sessionOptions;
                if (configuration.Protocol == Protocol.Sftp)
                {

                    sessionOptions = new SessionOptions
                    {
                        Protocol = Protocol.Sftp,
                        HostName = configuration.FtpHost,
                        UserName = configuration.FtpUserName,
                        PortNumber = configuration.FtpPort,
                        Password = configuration.FtpPassword,
                        SshHostKeyFingerprint = configuration.SshHostKeyFingerprint
                    };
                }
                else
                {
                    sessionOptions = new SessionOptions
                    {
                        Protocol = Protocol.Ftp,
                        HostName = configuration.FtpHost,
                        UserName = configuration.FtpUserName,
                        PortNumber = configuration.FtpPort,
                        Password = configuration.FtpPassword,
                    };
                }

                using (Session session = new Session())
                {
                    // Look for winscp.exe in the root folder of the function
                    string test = Directory.GetCurrentDirectory();
                    session.ExecutablePath = Path.Combine(test, "winscp.exe");
                    session.FileTransferProgress += SessionFileTransferProgress;

                    _logger.LogInformation($"Connecting to Host => {configuration.FtpHost} through , Protocol => {configuration.Protocol} and Port =>  {configuration.FtpPort}");
                    session.Open(sessionOptions);
                    _logger.LogInformation($"Connected Successfully");

                    _logger.LogInformation($"Temp files download path {downloadFilesPath}");
                    _logger.LogInformation($"Temp decrypted files path {decryptedFilesPath}");

                    if (!Directory.Exists(decryptedFilesPath))
                        Directory.CreateDirectory(decryptedFilesPath);

                    if (!Directory.Exists(downloadFilesPath))
                        Directory.CreateDirectory(decryptedFilesPath);

                    var transferOperationResult = session.GetFiles(remoteDirectory, downloadFilesPath);
                    if (transferOperationResult.IsSuccess)
                    {
                        _logger.LogInformation($"Files downloaded form {configuration.FtpHost}");
                        _logger.LogInformation($"ShouldDecrypt configuration => {configuration.ShouldDecrypt} ");
                        if (configuration.ShouldDecrypt)
                        {
                            // Load keys
                            _logger.LogInformation($"Decrypting file");
                            string passPhrase = configuration.PrivateKeyPassPharase;
                            string privateKeyFilePath = Path.Combine(Directory.GetCurrentDirectory() + "\\Keys", configuration.PrivateKeyName);

                            using (PGP pgp = new PGP())
                            {
                                var downloadedFiles = Directory.GetFiles(downloadFilesPath);
                                foreach (var inputFilePath in downloadedFiles)
                                {
                                    try
                                    {
                                        _logger.LogInformation($"Decrypting {inputFilePath}");

                                        string outputFilePath = Path.Join(decryptedFilesPath, Path.GetFileNameWithoutExtension(inputFilePath) + ".decrypted");

                                        await pgp.DecryptFileAsync(inputFilePath, outputFilePath, privateKeyFilePath, passPhrase);

                                        _logger.LogInformation($"Decrypted successfully to file {outputFilePath}");

                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogWarning(ex.Message);
                                    }
                                }
                            }
                        }
                        else
                        {
                            _logger.LogInformation("Skipping decryption");
                        }
                    }
                    else
                    {
                        _logger.LogInformation($"Error in downloading Files form {configuration.FtpHost} through {configuration.Protocol}");

                    }

                    await UploadFilesToBlobStorage(configuration, decryptedFilesPath);
                    var removeFilesOnRemote = configuration.RemoveFilesORemote;
                    if (removeFilesOnRemote == true)
                    {
                        _logger.LogInformation($"Deleting files on remote");
                        session.RemoveFiles(remoteDirectory);
                        _logger.LogInformation($"Deleted files");

                    }
                    else
                    {
                        _logger.LogInformation("Skipping FTP Delete");
                    }

                    // Close SFTP Session
                    _logger.LogInformation("Closing session");
                    session.Close();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error Occurred ", ex.Message);
            }
            finally
            {
                _logger.LogInformation("Deleting temp directories if it exsist");
                if (Directory.Exists(decryptedFilesPath))
                    Directory.Delete(decryptedFilesPath, true);

                if (Directory.Exists(downloadFilesPath))
                    Directory.Delete(downloadFilesPath, true);
                _logger.LogInformation("Deleted temp directories");
            }
        }

        public async Task UploadFilesToBlobStorage(Configuration configuration, string decryptedPath)
        {
            foreach (var filePath in Directory.GetFiles(decryptedPath))
            {
                BlobContainerClient blobContainerClient = new BlobContainerClient(
                    configuration.BlobConnectionString,
                    configuration.BlobContainer);
                await blobContainerClient.CreateIfNotExistsAsync();
                await UploadToBlobStorage(blobContainerClient, filePath, configuration.ContainerFolder);
            }
        }

        public async Task UploadToBlobStorage(BlobContainerClient blobClient, string file, string containerFolder)
        {
            var blobNameWithFolder = containerFolder + "/" + Path.GetFileName(file);
            _logger.LogInformation($"Uploading file to blob storage blob name => {blobNameWithFolder}");

            var client = blobClient
                .GetBlobClient(blobNameWithFolder);
            var result = await client.UploadAsync(file, true);

            _logger.LogInformation($"File uploaded successfully to blob storage => {result.GetRawResponse().Status}");
        }
        private void SessionFileTransferProgress(object sender, FileTransferProgressEventArgs e)
        {
            _logger.LogInformation("Downloading File {0} ({1:P0})", e.FileName, e.FileProgress);
        }
    }
}
