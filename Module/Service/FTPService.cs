using Bygdrift.CsvTools;
using Bygdrift.Warehouse;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Module.Service
{
    public class FTPService
    {
        private readonly FTPClientHelper client;
        public FTPService(AppBase<Settings> app)
        {
            App = app;
            client = new FTPClientHelper(App.Settings.FTPConnectionString);
            try
            {
                client.Connect();
                App.Log.LogInformation($"Connected to ftp: {FTPClientHelper.Host}, at path: {FTPClientHelper.Path}.");
            }
            catch (Exception)
            {
                App.Log.LogError("Could not connect to FTP.");
                throw;
            }
        }

        public AppBase<Settings> App { get; }

        public IEnumerable<(DateTime Saved, string Name, Csv Csv)> GetData(int? take = null)
        {
            var count = 0;
            foreach (var item in client.ListDirectory(FTPClientHelper.Path).Where(o => !o.IsDirectory))
            {
                if (take != null && count == take)
                    break;

                App.Log.LogInformation($"- File: {item.FullName}. Created: {item.LastWriteTime}. Bytes: {item.Length}");
                var sourceFilePath = FTPClientHelper.Path + "/" + item.Name;
                var extension = Path.GetExtension(item.FullName).ToLower();
                using var stream = new MemoryStream();
                client.DownloadFile(sourceFilePath, stream);

                if (extension == ".csv")
                {
                    var csv = new Csv().FromCsvStream(stream);
                    yield return (item.LastAccessTime, item.Name, csv);
                }
                count ++;
            }
        }

        /// <summary>
        /// Moves content from current folder and over to another folder
        /// </summary>
        /// <param name="folderName">A name like "backup"</param>
        public void MoveFolderContent(string folderName)
        {
            var backupFolder = FTPClientHelper.Path + "/" + folderName;
            if (!client.Exists(backupFolder))
                client.CreateDirectory(backupFolder);

            foreach (var item in client.ListDirectory(FTPClientHelper.Path).Where(o => !o.IsDirectory))
            {
                var sourceFilePath = FTPClientHelper.Path + "/" + item.Name;
                var backupFilePath = backupFolder + "/" + item.Name;
                if (!client.Exists(backupFilePath))
                {
                    using var stream = new MemoryStream();
                    client.DownloadFile(sourceFilePath, stream);
                    client.UploadFile(stream, backupFilePath);
                    client.DeleteFile(sourceFilePath);
                }
            }
        }

        /// <summary>
        /// Deletes the content of the ftp folder
        /// </summary>
        public void DeleteFolderContent()
        {
            foreach (var item in client.ListDirectory(FTPClientHelper.Path).Where(o => !o.IsDirectory))
            {
                var sourceFilePath = FTPClientHelper.Path + "/" + item.Name;
                client.DeleteFile(sourceFilePath);
            }
        }
    }
}