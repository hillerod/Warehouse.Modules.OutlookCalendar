using Bygdrift.Tools.MssqlTool;
using Bygdrift.Warehouse;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.IO;

namespace ModuleTests
{
    public class BaseTests
    {

        /// <summary>Path to project base</summary>
        public static readonly string BasePath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..\\..\\..\\"));
        public AppBase<Module.Settings> App = new();

        public BaseTests(bool useLocalDb)
        {
            if (useLocalDb)
            {
                var dbPath = Path.Combine(BasePath, "Files\\DB\\OutlookCalendar.mdf");
                var conn = $"Data Source=(LocalDB)\\MSSQLLocalDB;AttachDbFilename={dbPath};Integrated Security=True";
                var mssql = new Mssql(conn, App.ModuleName, App.Log);
                App.Mssql.Connection = mssql.Connection;
                Assert.IsNull(mssql.DeleteAllTables());
            }
        }

        //[TestCleanup]
        //public void TestCleanup()
        //{
        //    Assert.IsNull(App.Mssql.DeleteAllTables());
        //    App.Mssql.Dispose();
        //}

        public static string MethodName
        {
            get
            {
                var methodInfo = new StackTrace().GetFrame(1).GetMethod();
                return methodInfo.Name;
            }
        }
    }
}
