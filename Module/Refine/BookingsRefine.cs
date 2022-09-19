using Bygdrift.Tools.CsvTool;
using Bygdrift.Tools.DataLakeTool;
using Bygdrift.Warehouse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Module.Refine
{
    public class BookingsRefine
    {
        private static Csv csv;

        public static async Task<Csv> RefineAsync(AppBase app, IEnumerable<KeyValuePair<DateTime, Csv>> csvFiles, Csv locations, bool saveToDataLake, bool saveToDb)
        {
            //var a = new Bygdrift.Tools.CsvTool.Csv()
            CreateCsv(csvFiles, locations);
            if (csvFiles.Any())
            {
                app.LoadedLocal = csvFiles.Max(o => o.Key);

                if (saveToDataLake)
                    await app.DataLake.SaveCsvAsync(csv, "Refined", "Bookings.csv", FolderStructure.DatePath);

                if (saveToDb)
                app.Mssql.MergeCsv(csv, "Bookings", "Id", false, false);
            }
            return csv;
        }

        private static void CreateCsv(IEnumerable<KeyValuePair<DateTime, Csv>> csvFiles, Csv locations)
        {
            csv = new Csv("Id, Start, End, Participants, Name, Capacity, Mail, Factor");
            foreach (var item in csvFiles.OrderBy(o => o.Key))
            {
                var ftpCsv = FilteredCsv(item.Value, locations);
                var toRow = csv.RowLimit.Max;
                for (int fromRow = ftpCsv.RowLimit.Min; fromRow < ftpCsv.RowLimit.Max; fromRow++)
                {
                    toRow++;
                    InsertId(ftpCsv, fromRow, 1, toRow);  //Id
                    InsertDate(ftpCsv, 1, fromRow, 2, toRow);  //Start
                    InsertDate(ftpCsv, 2, fromRow, 3, toRow);  //End
                    InsertValue(ftpCsv, 3, fromRow, 4, toRow);  //Participants
                    InsertValue(ftpCsv, 4, fromRow, 5, toRow);  //Name
                    var mail = InsertValue(ftpCsv, 8, fromRow, 7, toRow, true);  //Mail
                    var capacity = locations.GetRowRecordsFirstMatch("Mail", mail, true, false)[2];
                    csv.AddRecord(toRow,6, capacity);  //Capacity
                }
            }

            //UpdateCapacity("Mail", "KANTINEN-MOEDECENTER@HILLEROD.DK", "Capacity", 100);
            AddFactorCol("Factor", "Participants", "Capacity", 8);
            csv.RemoveRedundantRows("Id", false);
        }

        /// <summary>
        /// Filters out all irelevant calendars like vehicles and assets, so it's only Room:
        /// </summary>
        private static Csv FilteredCsv(Csv bookings, Csv locations)
        {
            var mails = locations.GetColRecords("Type", "Mail", "Room", true).Select(o => o.Value).ToArray();
            var res = bookings.FilterRows("Mailadresse", true, mails);
            return res;
        }

        private static string InsertId(Csv ftpCsv, int fromRow, int toCol, int toRow)
        {
            //var start = ftpCsv.GetRecord<DateTime>(fromRow, 1).ToLocalTime().ToString("yyMMddhhmm");
            var start = DateTime.Parse(ftpCsv.GetRecord(fromRow, 1).ToString().Replace('.', ':')).ToLocalTime().ToString("yyMMddhhmm");
            //var end = DateTime.Parse(csv.GetRecord(1, fromRow).ToString().Replace('.', ':')).ToLocalTime().ToString("yyMMddhhmm");
            var mail = ftpCsv.GetRecord(fromRow, 8).ToString().ToUpper().Replace("@HILLEROD.DK", "");
            var res = start + "-" + mail;
            csv.AddRecord(toRow, toCol, res);
            return res;
        }

        private static void InsertDate(Csv ftpCsv, int fromCol, int fromRow, int toCol, int toRow)
        {
            if (ftpCsv.TryGetRecord(fromRow, fromCol, out object val))
            {
                var date = DateTime.Parse(val.ToString().Replace('.', ':')).ToLocalTime().ToString("s");
                csv.AddRecord(toRow, toCol, date);
            }
        }

        private static string InsertValue(Csv ftpCsv, int fromCol, int fromRow, int toCol, int toRow, bool toUpper = false)
        {
            var val = ftpCsv.GetRecord<string>(fromRow, fromCol);
            var res = toUpper ? val.ToString().ToUpper() : val;
            csv.AddRecord(toRow, toCol, res);
            return res;
        }

        private static void UpdateCapacity(string headerNameLookup, string lookup, string writeIntoHeader, int capacity)
        {
            var rows = csv.GetRowsRecords(headerNameLookup, true, lookup);
            csv.TryGetColId(writeIntoHeader, out int col);

            foreach (var row in rows)
                csv.AddRecord(row.Key, col, capacity);
        }

        private static void AddFactorCol(string headerName, string headerNameNumerator, string headerNameDenominator, int col) //Numerator=tæller, denominator=nævner
        {
            var numerators = csv.GetColRecords(headerNameNumerator, false);
            var denominators = csv.GetColRecords(headerNameDenominator, false);

            foreach (var denimonatorRecord in denominators)
            {
                if (double.TryParse(denimonatorRecord.Value.ToString(), out double denominator) && double.TryParse(numerators[denimonatorRecord.Key].ToString(), out double numerator))
                {
                    var val = numerator < denominator ? Math.Round(numerator / denominator, 3) : 1;
                    csv.AddRecord(denimonatorRecord.Key, col, val);
                }
            }
        }
    }
}
