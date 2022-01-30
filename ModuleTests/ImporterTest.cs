using Bygdrift.CsvTools;
using Bygdrift.Warehouse;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Module;
using Module.Refine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

/// <summary>
/// To use this test, fill out 'ModuleTests/appsettings.json'
/// You can upload data directly to Azure, by stting saveToServer = true
/// You can fetch data from webservice, by setting useDataFromService = true.
/// It takes some time to fetch data each time from webservice, so download instead the data to local files to a folder: 'ModuleTests/Files/In/' and set useDataFromService=false.
/// Download the datafiles by using: ModuleTests.Service.WebServiceTest
/// </summary>

namespace ModuleTests
{
    [TestClass]
    public class ImporterTest
    {
        /// <summary>Path to project base</summary>
        public static readonly string BasePath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..\\..\\..\\"));

        public AppBase<Settings> App = new();

        [TestMethod]
        public async Task TestRunModule()
        {
            int? take = 1;
            bool saveToServer = false;
            bool saveLocal = true;
            bool useDataFromService = false;

            var ftpService = new Module.Service.FTPService(App);
            var csvFiles = useDataFromService ? ftpService.GetData(take) : LoadAllCsv(take);

            var locationsRefinedCsv = await LocationsRefine.RefineAsync(App, saveToServer);
            var bookingsRefinedCsv = await BookingsRefine.RefineAsync(App, csvFiles, saveToServer);
            var partitionsRefinedCsv = PartitionBookingsRefine.Refine(App, bookingsRefinedCsv, locationsRefinedCsv, 2, saveToServer);

            var errors = App.Log.GetErrorsAndCriticals();
            Assert.IsFalse(errors.Any());

            if (saveLocal)
            {
                locationsRefinedCsv.ToCsvFile(Path.Combine(BasePath, "Files", "Out", "Locations.csv"));
                bookingsRefinedCsv.ToCsvFile(Path.Combine(BasePath, "Files", "Out", "boobkingsRefined.csv"));
                partitionsRefinedCsv.ToCsvFile(Path.Combine(BasePath, "Files", "Out", "PartitionsRefined.csv"));
            }
        }

        private static List<(DateTime Saved, string Name, Csv Csv)> LoadAllCsv(int? take = null)
        {
            var res = new List<(DateTime Saved, string Name, Csv Csv)>();
            var directoryPath = Path.Combine(BasePath, "Files", "In", "FromFtp");
            var filePaths = Directory.GetFiles(directoryPath, "*.csv", SearchOption.AllDirectories);
            var counter = 0;
            foreach (var path in filePaths)
            {
                if (take != null && counter == take)
                    break;

                var saved = File.GetLastWriteTime(path);
                var name = Path.GetFileName(path);
                using var stream = new FileStream(path, FileMode.Open);
                var csv = new Csv().FromCsvStream(stream);
                res.Add((saved, name, csv));
                counter++;
            }
            return res;
        }
    }
}