using Bygdrift.Tools.CsvTool;
using Bygdrift.Tools.DataLakeTool;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace ModuleTests
{
    [TestClass]
    public class FilesToDataLake: BaseTests
    {
        public FilesToDataLake():base(true)
        {

        }
        
        //[TestMethod]
        public async Task ImportLocalFilesToDataLake()
        {
            int? take = null;
            var csvFiles = LoadAllCsv(take);
            foreach (var item in csvFiles)
            {
                App.LoadedLocal = item.Date;
                using var stream = new FileStream(item.path, FileMode.Open);
                await App.DataLake.SaveStreamAsync(stream, "Test", item.Name, FolderStructure.DatePath);
            }
        }

        private static List<(DateTime Date, string Name, string path)> LoadAllCsv(int? take = null)
        {
            var res = new List<(DateTime Date, string Name, string path)>();
            var directoryPath = Path.Combine(BasePath, "Files", "In", "_Old", "FromFtp");
            var filePaths = Directory.GetFiles(directoryPath, "*.csv", SearchOption.AllDirectories);
            var counter = 0;
            foreach (var path in filePaths)
            {
                if (take != null && counter == take)
                    break;

                var name = Path.GetFileName(path);
                var saved = DateTime.Parse(name.Substring(0, 10));
                
                res.Add((saved, name, path));
                counter++;
            }
            return res;
        }
    }
}