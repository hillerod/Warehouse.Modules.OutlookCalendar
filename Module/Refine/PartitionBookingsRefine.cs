using Bygdrift.Tools.CsvTool;
using Bygdrift.Tools.CsvTool.TimeStacking;
using Bygdrift.Tools.DataLakeTool;
using Bygdrift.Warehouse;
using Module.Refine.PartitionBookings;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Module.Refine
{
    public class PartitionBookingsRefine
    {
        private static Csv csv = new();

        public static async Task<Csv> RefineAsync(AppBase app, Csv bookings, bool saveToServer)
        {
            if (bookings == null || bookings.RowCount == 0)
                return default;

            CreateCsv(bookings);

            if (saveToServer)
            {
                await app.DataLake.SaveCsvAsync(csv, "Refined", "PartitionBookings.csv", FolderStructure.DatePath);
                app.Mssql.MergeCsv(csv, "PartitionBookings", "Id");
            }

            return csv;
        }

        /// <summary>
        /// Denne er ikke færdigbygget. Mangler at få unikt ID og beregne factor samt indsætte groupId
        /// </summary>
        private static void CreateCsvNew(Csv bookings, Csv locations, int years)
        {
            var timeStack = new TimeStack(bookings, "Name", "Start", "End")
                .AddInfoFormat("Id", "[:From:yyMMddhhmm]-[:Group]")
                .AddInfoFrom("Start")
                .AddInfoTo("End")
                .AddInfoGroup("Group")
                .AddInfoLength("minutes", null, 60)
                .AddInfoLength("percent", null, 50)
                .AddInfoFormat("Factor", "0")
                .AddInfoFormat("GroupId", "[:Group]");

            var spans = timeStack.GetSpansPerHour(8, 18, 2, new int[] { 1, 2, 3, 4, 5 });
            csv = timeStack.GetTimeStackedCsv(spans);
        }

        /// <summary>
        /// Partitions bookingdata into slices of 2 hours: 8-10, 10-12, 12-14, 14-16, 16-18
        /// Goes from now and two year back
        /// Doesn't include weekends
        /// </summary>
        private static void CreateCsv(Csv bookings)
        {
            var partitioning = new Partitioning(bookings, "Start", "End", "Mail", "Factor");
            var from = bookings.GetColRecords<DateTime>("Start").Values.Min();
            var to = bookings.GetColRecords<DateTime>("End").Values.Max();
            var partitons = partitioning.GetPartitionsInTimeslices(from, to, new TimeSpan(8, 0, 0), new TimeSpan(18, 0, 0), 120, false, true);
            csv = partitioning.TimeslotsToCsv(partitons);
        }
    }
}
