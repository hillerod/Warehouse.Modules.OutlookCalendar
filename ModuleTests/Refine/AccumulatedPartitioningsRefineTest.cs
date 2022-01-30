using Bygdrift.CsvTools;
using Bygdrift.Warehouse;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Module.Refine;
using System.IO;

namespace ModuleTests.Refine
{
    [TestClass]
    public class AccumulatedPartitioningsRefineTest
    {
        [TestMethod]
        public void GetDataPerHourTest()
        {
            var input = LoadAccumulatedBookingsCsv();
            var update = new CsvUpdate(input, true, "Start", "End", "Mail");
            update.Csv.ToCsvFile(Path.Combine(ImporterTest.BasePath, "Files", "Out", "accuTest.csv"));
            var app = new AppBase();
            var accumulatedBookingsRefineCsv = PartitionBookingsRefine.Refine(app, input, LoadLocations(), 2, false);
            accumulatedBookingsRefineCsv.ToCsvFile(Path.Combine(ImporterTest.BasePath, "Files", "Out", "accuTest.csv"));
        }

        private static Csv LoadAccumulatedBookingsCsv()
        {
            var path = Path.Combine(ImporterTest.BasePath, "Files", "In", "FromServer", "accumulated", "bookings.csv");
            using var stream = new FileStream(path, FileMode.Open);
            var csv = new Csv().FromCsvStream(stream);
            return csv;
        }

        private static Csv LoadLocations()
        {
            var path = Path.Combine(ImporterTest.BasePath, "Files", "In", "FromServer", "locations.csv");
            using var stream = new FileStream(path, FileMode.Open);
            var csv = new Csv().FromCsvStream(stream);
            return csv;
        }

    }
}