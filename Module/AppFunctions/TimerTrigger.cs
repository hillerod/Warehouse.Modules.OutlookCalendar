using Bygdrift.Tools.CsvTool;
using Bygdrift.Tools.DataLakeTool;
using Bygdrift.Warehouse;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Module.Refine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Module.AppFunctions
{
    public class TimerTrigger
    {
        public AppBase<Settings> App { get; }

        public TimerTrigger(ILogger<UploadFile> logger) => App = new AppBase<Settings>(logger);

        [FunctionName(nameof(TimerTriggerAsync))]
        public async Task TimerTriggerAsync([TimerTrigger("%ScheduleExpression%")] TimerInfo myTimer)
        {
            var csvUnloadedFiles = App.Mssql.GetAsCsvQuery($"SELECT [Path] FROM [{App.ModuleName}].[ImportedFiles] WHERE Imported IS NULL");
            if(csvUnloadedFiles.RowCount == 0)
            {
                App.Log.LogInformation("No files found for upload i database log");
                return;
            }

            var files = new List<KeyValuePair<DateTime, Csv>>();
            for (int r = csvUnloadedFiles.RowLimit.Min; r <= csvUnloadedFiles.RowLimit.Max; r++)
            {
                var name = csvUnloadedFiles.GetRecord<string>(r, "File");
                var path = Path.GetDirectoryName(csvUnloadedFiles.GetRecord<string>(r, "Path"));
                var Received = csvUnloadedFiles.GetRecord<DateTime>(r, "Received");
                if(App.DataLake.GetCsv(path, name, FolderStructure.DatePath, out Csv val ))
                    files.Add(new KeyValuePair<DateTime, Csv>(Received, val));
            }

            if (files.Any())
            {
                var locationsRefinedCsv = await LocationsRefine.RefineAsync(App, true);
                var bookingsRefinedCsv = await BookingsRefine.RefineAsync(App, files, locationsRefinedCsv, true);
                await PartitionBookingsRefine.RefineAsync(App, bookingsRefinedCsv, true);

                for (int r = csvUnloadedFiles.RowLimit.Min; r <= csvUnloadedFiles.RowLimit.Max; r++)
                    csvUnloadedFiles.AddRecord(r, "Imported", App.LoadedUtc);

                App.Mssql.MergeCsv(csvUnloadedFiles, "ImportedFiles", "File");
            }

            App.Log.LogInformation($"Imported {files.Count} files.");
        }
    }
}
