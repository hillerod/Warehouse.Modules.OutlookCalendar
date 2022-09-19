using Bygdrift.Tools.CsvTool;
using Bygdrift.Tools.DataLakeTool;
using Bygdrift.Warehouse;
using System.Threading.Tasks;

namespace Module.Refine
{
    public class LocationsRefine
    {
        private static readonly Csv csv = new();

        public static async Task<Csv> RefineAsync(AppBase<Settings> app, bool saveToDataLake, bool saveToDb)
        {
            CreateCsv();
            
            if (saveToDataLake)
                await app.DataLake.SaveCsvAsync(csv, "Static", "Locations.csv", FolderStructure.Path);

            if (saveToDb)
                app.Mssql.MergeCsv(csv, "Locations", "Mail", true, false);

            return csv;
        }

        private static void CreateCsv()
        {
            csv.AddHeaders("Mail,Capacity,Location,Level,Building,Name,Type,ShortName");
            csv.AddRows(new[] {
                "Det-Hvide-Hus-Belle-de-Boskoop@hillerod.dk,6,Trollesmindealle 27,1. sal,Det hvide hus,Belle de Boskob,Room,DHH1_Belle de Boskob",
                "Det-Hvide-Hus-Cox-Orange@hillerod.dk,4,Trollesmindealle 27,1. sal,Det hvide hus,Cox Orange,Room,DHH1_Cox Orange",
                "Det-Hvide-Hus-Graasten@hillerod.dk,8,Trollesmindealle 27,1. sal,Det hvide hus,Gråsten,Room,DHH1_Gråsten",
                "Det-Hvide-Hus-Ingrid-Marie@hillerod.dk,8,Trollesmindealle 27,1. sal,Det hvide hus,Ingrid Marie,Room,DHH1_Ingrid Marie",
                "Det-Hvide-Hus-Pederstrup@hillerod.dk,4,Trollesmindealle 27,1. sal,Det hvide hus,Pederstrup,Room,DHH1_Pederstrup",
                "Det-Hvide-Hus-Per-Smed@hillerod.dk,8,Trollesmindealle 27,1. sal,Det hvide hus,Per Smed,Room,DHH1_Per Smed",
                "andegaarden-1.sal@hillerod.dk,6,Trollesmindealle 27,1. sal,Rådhus,Andergården,Room,RÅD1_Andergården",
                "hoenseriet-1.sal@hillerod.dk,6,Trollesmindealle 27,1. sal,Rådhus,Hønseriet,Room,RÅD1_Hønseriet",
                "hundegaarden-1.sal@hillerod.dk,6,Trollesmindealle 27,1. sal,Rådhus,Hundegården,Room,RÅD1_Hundegården",
                "kaningaarden-1.sal@hillerod.dk,6,Trollesmindealle 27,1. sal,Rådhus,Kaningården,Room,RÅD1_Kaningården",
                "raevegaarden-1.sal@hillerod.dk,6,Trollesmindealle 27,1. sal,Rådhus,Rævegården,Room,RÅD1_Rævegården",
                "fasanburet-2.sal@hillerod.dk,6,Trollesmindealle 27,2. sal,Rådhus,Fasanburet,Room,RÅD2_Fasanburet",
                "hestestalden-2.sal@hillerod.dk,6,Trollesmindealle 27,2. sal,Rådhus,Hestestalden,Room,RÅD2_Hestestalden",
                "kostalden-2.sal@hillerod.dk,6,Trollesmindealle 27,2. sal,Rådhus,Kostalden,Room,RÅD2_Kostalden",
                "plagestalden-2.sal@hillerod.dk,6,Trollesmindealle 27,2. sal,Rådhus,Plagestalden,Room,RÅD2_Plagestalden",
                "rugekassen-2.sal@hillerod.dk,6,Trollesmindealle 27,2. sal,Rådhus,Rugekassen,Room,RÅD2_Rugekassen",
                "byraadssalen-moedecenter@hillerod.dk,30,Trollesmindealle 27,Stuen,Rådhus,Byrådssalen,Room,RÅD0_Byrådssalen",
                "harven-moedecenter@hillerod.dk,4,Trollesmindealle 27,Stuen,Rådhus,Harven,Room,RÅD0_Harven",
                "kantinen-moedecenter@hillerod.dk,100,Trollesmindealle 27,Stuen,Rådhus,Kantinen,Room,RÅD0_Kantinen",
                "leen-moedecenter@hillerod.dk,12,Trollesmindealle 27,Stuen,Rådhus,Leen,Room,RÅD0_Leen",
                "mejeriet-stueetagen@hillerod.dk,8,Trollesmindealle 27,Stuen,Rådhus,Mejeriet,Room,RÅD0_Mejeriet",
                "vindharpen-moedecenter@hillerod.dk,8,Trollesmindealle 27,Stuen,Rådhus,Vindharpen,Room,RÅD0_Vindharpen",
                "ploven-moedecenter@hillerod.dk,8,Trollesmindealle 27,Stuen,Rådhus,Ploven,Room,RÅD0_Ploven",
                "seglet-moedecenter@hillerod.dk,10,Trollesmindealle 27,Stuen,Rådhus,Seglet,Room,RÅD0_Seglet",
                "tromlen-moedecenter@hillerod.dk,4,Trollesmindealle 27,Stuen,Rådhus,Tromlen,Room,RÅD0_Tromlen",
            });
            csv.ColumnChangeCase(0, true);
        }
    }
}
