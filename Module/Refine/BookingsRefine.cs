using Bygdrift.CsvTools;
using Bygdrift.DataLakeTools;
using Bygdrift.Warehouse;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Module.Refine
{
    public class BookingsRefine
    {
        private static readonly Csv csv = new();

        public static async Task<Csv> RefineAsync(AppBase app, IEnumerable<(DateTime Saved, string Name, Csv Csv)> csvFiles, bool saveToServer)
        {
            CreateCsv(csvFiles);
            if (saveToServer)
            {
                await app.DataLake.SaveCsvAsync(csv, "Refined", "Bookings.csv", FolderStructure.Path);
                app.Mssql.MergeCsv(csv, "Bookings", "Id", false, false);
            }
            return csv;
        }

        private static void CreateCsv(IEnumerable<(DateTime Saved, string Name, Csv Csv)> csvFiles)
        {
            csv.AddHeaders("Id, Start, End, Participants, Name, Capacity, Mail, Factor");

            foreach (var item in csvFiles)
            {
                var ftpCsv = item.Csv;
                var toRow = csv.RowLimit.Max;
                for (int fromRow = ftpCsv.RowLimit.Min; fromRow < ftpCsv.RowLimit.Max; fromRow++)
                {
                    toRow++;
                    InsertId(ftpCsv, fromRow, 1, toRow);  //Id
                    InsertDate(ftpCsv, 1, fromRow, 2, toRow);  //Start
                    InsertDate(ftpCsv, 2, fromRow, 3, toRow);  //End
                    InsertValue(ftpCsv, 3, fromRow, 4, toRow);  //Participants
                    InsertValue(ftpCsv, 4, fromRow, 5, toRow);  //Name
                    InsertValue(ftpCsv, 6, fromRow, 6, toRow);  //Capacity
                    InsertValue(ftpCsv, 8, fromRow, 7, toRow, true);  //Mail
                    //InsertFactor(csv, fromRow, 7, toRow);  //Factor
                }
            }

            UpdateCapacity("Mail", "KANTINEN-MOEDECENTER@HILLEROD.DK", "Capacity", 100);
            AddFactorCol("Factor", "Participants", "Capacity", 8);
            csv.RemoveRedundantRows("Id", false);
        }


        private static void InsertId(Csv csv, int fromRow, int toCol, int toRow)
        {
            var start = DateTime.Parse(csv.GetRecord(fromRow,1).ToString().Replace('.', ':')).ToLocalTime().ToString("yyMMddhhmm");
            //var end = DateTime.Parse(csv.GetRecord(1, fromRow).ToString().Replace('.', ':')).ToLocalTime().ToString("yyMMddhhmm");
            var mail = csv.GetRecord(fromRow, 8).ToString().ToUpper().Replace("@HILLEROD.DK", "");
            csv.AddRecord(toRow, toCol, start + "-" + mail);
        }

        private static void InsertDate(Csv csv, int fromCol, int fromRow, int toCol, int toRow)
        {
            if (csv.TryGetRecord(fromRow, fromCol, out object val))
            {
                var date = DateTime.Parse(val.ToString().Replace('.', ':')).ToLocalTime().ToString("s");
                csv.AddRecord(toRow, toCol, date);
            }
        }

        private static void InsertValue(Csv csv, int fromCol, int fromRow, int toCol, int toRow, bool toUpper = false)
        {
            if (csv.TryGetRecord(fromRow, fromCol, out object val))
                csv.AddRecord(toRow, toCol, toUpper ? val.ToString().ToUpper() : val);
        }

        private static void UpdateCapacity(string headerNameLookup, string lookup, string writeIntoHeader, int capacity)
        {
            var rows = csv.GetRowsRecords(headerNameLookup, lookup);
            csv.TryGetColId(writeIntoHeader, out int col);

            foreach (var row in rows)
                csv.AddRecord(row.Key, col, capacity);
        }

        private static void AddFactorCol(string headerName, string headerNameNumerator, string headerNameDenominator, int col) //Numerator=tæller, denominator=nævner
        {
            //var numerators = csv.GetRecordCol(headerNameNumerator).Records;
            var numerators = csv.GetColRecords(headerNameNumerator);
            //var denominators = csv.GetRecordCol(headerNameDenominator).Records;
            var denominators = csv.GetColRecords(headerNameDenominator);
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
