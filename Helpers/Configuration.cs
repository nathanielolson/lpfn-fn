using WinSCP;

namespace FunctionApp.Helpers
{
    public class Configuration
    {
        public int FtpPort { get; set; }
        public string FtpHost { get; set; }
        public string FtpUserName { get; set; }
        public string FtpPassword { get; set; }
        public string SshHostKeyFingerprint { get; set; }
        public string FtpRootDirectory { get; set; }
        public string BlobConnectionString { get; set; }
        public string BlobContainer { get; set; }
        public string ContainerFolder { get; set; }
        public string PrivateKeyName { get; set; }
        public string PrivateKeyPassPharase { get; set; }
        public bool ShouldDecrypt { get; set; }
        public bool RemoveFilesORemote { get; set; }
        public Protocol Protocol { get; set; }
    }
}
