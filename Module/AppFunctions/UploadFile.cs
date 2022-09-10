using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Bygdrift.Warehouse;
using Bygdrift.Tools.DataLakeTool;
using Bygdrift.Tools.CsvTool;
using System;

namespace Module.AppFunctions
{
    public class UploadFile
    {
        public AppBase<Settings> App { get; }

        public UploadFile(ILogger<UploadFile> logger) => App = new AppBase<Settings>(logger);

        [FunctionName(nameof(UploadFile))]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req)
        {
            var filesUploaded = 0;
            var fileNames = "";

            if (req.Form.Files != null && req.Form.Files.Count > 0)
                foreach (var file in req.Form.Files)
                {
                    filesUploaded++;
                    fileNames += file.FileName + ",";
                    using MemoryStream stream = new();
                    file.CopyTo(stream);
                    var path = await App.DataLake.SaveStreamAsync(stream, "Raw", file.FileName, FolderStructure.DatePath);
                    var csv = new Csv("File, Path, Received, Imported, IsImported").AddRow(file.FileName, path, App.LoadedUtc, new DateTime(1900, 1, 1), false);
                    App.Mssql.MergeCsv(csv, "ImportedFiles", "File");
                }

            App.Log.LogInformation($"Uploaded {filesUploaded} files. Names: {fileNames.Trim(',')}.");
            return new OkObjectResult($"Uploaded {filesUploaded} files. Names: {fileNames.Trim(',')}.");
        }
    }
}