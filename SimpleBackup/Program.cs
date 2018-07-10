using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Threading.Tasks;
using CoreFtp;
using Microsoft.Extensions.Configuration;

namespace SimpleBackup
{
    class Program
    {
        static void Main(string[] args)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            using (var client =
                new FtpClient(new FtpClientConfiguration()
                {
                    Host = config["ftp:host"],
                    Username = config["ftp:user"],
                    Password = config["ftp:password"],
                    BaseDirectory = config["ftp:baseDir"]
                }))
            {
                var backupFile = new FileInfo(string.Format(config["backup:name"], DateTime.Now));
                client.LoginAsync().Wait();
                MakeBackup(backupFile, new DirectoryInfo(config["backup:dir"])).Wait();
                UploadBackup(client, backupFile).Wait();
                backupFile.Delete();
            }
        }

        private static async Task MakeBackup(FileInfo backupFile, DirectoryInfo dir)
        {
            using (var zip = new ZipArchive(backupFile.OpenWrite(), ZipArchiveMode.Create))
            {
                foreach (var file in dir.GetFiles("*", SearchOption.AllDirectories))
                {
                    var entry = zip.CreateEntryFromFile(file.FullName, file.FullName.Substring(dir.FullName.Length + 1));
                    Console.WriteLine($"Compressed {entry.FullName}");
                }
            }
        }

        private static async Task UploadBackup(FtpClient client, FileInfo backupFile)
        {
            using (var input = backupFile.OpenRead())
            using (var output = await client.OpenFileWriteStreamAsync(backupFile.Name))
            {
                await input.CopyToAsync(output);
            }
        }
    }
}
