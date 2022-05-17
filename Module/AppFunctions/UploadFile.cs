using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Text;
using Bygdrift.Warehouse;
using Bygdrift.DataLakeTools;

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
                    await App.DataLake.SaveStreamAsync(stream, "raw", file.FileName, FolderStructure.DatePath);
                }

            App.Log.LogInformation($"Uploaded {filesUploaded} files. Names: {fileNames.Trim(',')}.");
            return new OkObjectResult($"Uploaded {filesUploaded} files. Names: {fileNames.Trim(',')}.");
        }
    }
}
