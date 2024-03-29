﻿using Bygdrift.Tools.CsvTool;
using Bygdrift.Tools.DataLakeTool;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Module.Refine;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ModuleTests
{
    [TestClass]
    public class ImporterTests : BaseTests
    {
        const bool UseLocalDb = false;

        public ImporterTests() : base(UseLocalDb)
        {

        }

        [TestMethod]
        public void GetUndreadFiles()
        {
            //SetFile:
            var csv = new Csv("File, Path, Received, Imported, IsImported").AddRow("file", "path", App.LoadedUtc, new DateTime(1900, 1, 1), false);
            App.Mssql.MergeCsv(csv, "ImportedFiles", "File");

            var csvUnloadedFiles = App.Mssql.GetAsCsvQuery($"SELECT * FROM [{App.ModuleName}].[ImportedFiles] WHERE Imported = 0");

            var files = new List<KeyValuePair<DateTime, Csv>>();
            for (int r = csvUnloadedFiles.RowLimit.Min; r <= csvUnloadedFiles.RowLimit.Max; r++)
            {
                var name = csvUnloadedFiles.GetRecord<string>(r, "File");
                var path = Path.GetDirectoryName(csvUnloadedFiles.GetRecord<string>(r, "Path"));
                var Received = csvUnloadedFiles.GetRecord<DateTime>(r, "Received");
                if (App.DataLake.GetCsv(path, name, FolderStructure.Path, out Csv val))
                    files.Add(new KeyValuePair<DateTime, Csv>(Received, val));
            }
        }


        [TestMethod]
        public async Task TestRunModuleFromDataLake()
        {
            var startClock = DateTime.Now;
            bool saveToDataLake = true;
            bool saveToDB = true;
            var csvFiles = GetLocalCsvs(Path.Combine(BasePath, "Files", "In", "raw")).OrderBy(o => o.Date);

            var locationsRefinedCsv = await LocationsRefine.RefineAsync(App, saveToDataLake, saveToDB);

            foreach (var monthGroup in csvFiles.GroupBy(o => o.Date.ToString("yy-MM")))
            {
                Trace.WriteLine($"Group {monthGroup.Key}. Elapsed {DateTime.Now.Subtract(startClock).TotalSeconds}:");
                var files = new List<KeyValuePair<DateTime, Csv>>();
                foreach (var item in monthGroup)
                {
                    var csv = new Csv().FromCsvFile(item.Path);
                    files.Add(new KeyValuePair<DateTime, Csv>(item.Date, csv));
                }

                Trace.WriteLine($"\tBookings...");
                var bookingsRefinedCsv = await BookingsRefine.RefineAsync(App, files, locationsRefinedCsv, saveToDataLake, saveToDB);
                Trace.WriteLine($"\tPartitions...");
                await PartitionBookingsRefine.RefineAsync(App, bookingsRefinedCsv, saveToDataLake, saveToDB);
                var errors = App.Log.GetErrorsAndCriticals();
                Assert.IsFalse(errors.Any());
            }
        }

        private static IEnumerable<(DateTime Date, string Path)> GetLocalCsvs(string directoryPath, int? take = null)
        {
            var res = new List<(DateTime Date, string Path)>();
            var filePaths = Directory.GetFiles(directoryPath, "*.csv", SearchOption.AllDirectories);
            var counter = 0;
            foreach (var path in filePaths)
            {
                if (take != null && counter == take)
                    break;

                var name = Path.GetFileName(path);
                var date = DateTime.Parse(name.Substring(0, 10));
                res.Add((date, path));
                counter++;
            }
            return res;
        }
    }
}