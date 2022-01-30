using Bygdrift.CsvTools;
using Bygdrift.Warehouse;
using Module.Refine.PartitionBookings;
using System;
using System.Linq;

namespace Module.Refine
{
    public class PartitionBookingsRefine
    {
        private static Csv csv = new();

        public static Csv Refine(AppBase app, Csv bookings, Csv locations, int years, bool saveToServer)
        {
            CreateCsv(bookings, locations, years);
            if (saveToServer)
                app.Mssql.MergeCsv(csv, "PartitionBookings", "Id", false, false);

            return csv;
        }

        /// <summary>
        /// Partitions bookingdata into slices of 2 hours: 8-10, 10-12, 12-14, 14-16, 16-18
        /// Goes from now and two year back
        /// Doesn't include weekends
        /// </summary>
        private static void CreateCsv(Csv bookings, Csv locations, int years)
        {
            var partitioning = new Partitioning(FilteredCsv(bookings, locations), "Start", "End", "Mail", "Factor");
            ///TODO: Ændr denne så :
            ///- Den finder startDato og screener helt tilbage fra start (OK for det kommer kun i chunks af 1 md)
            ///- I AppSetting kan man angive timespan og om weekender skal med og hvor mange minutters interval der skal være
            var partitons = partitioning.GetPartitionsInTimeslices(new DateTime(DateTime.Now.Year, 1, 1).AddYears(-years), DateTime.Now, new TimeSpan(8, 0, 0), new TimeSpan(18, 0, 0), 120, false, true);
            csv = partitioning.TimeslotsToCsv(partitons);
        }

        /// <summary>
        /// Filters out all irelevant calendars like vehicles and assets, so it's only Room:
        /// </summary>
        private static Csv FilteredCsv(Csv bookings, Csv locations)
        {
            var mails = locations.GetColRecords("Type", "Mail", "Room").Select(o => o.Value).ToArray();
            return bookings.FilterRows("Mail", mails);
        }
    }
}
