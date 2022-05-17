//using Bygdrift.Warehouse;
//using Microsoft.Extensions.Logging;
//using Microsoft.VisualStudio.TestTools.UnitTesting;
//using Module;
//using Module.Service;
//using Moq;
//using System.Linq;

//namespace ModuleTests.Service
//{
//    [TestClass]
//    public class WebServiceTest
//    {
//        private readonly FTPService service;
//        private readonly AppBase<Settings> App = new();

//        public WebServiceTest() => service = new FTPService(App);

//        [TestMethod]
//        public void GetFirstFile()
//        {
//            var file = service.GetData().First();
//        }

//        [TestMethod]
//        public void BackupFolderContent()
//        {
//            service.MoveFolderContent("Loaded and backed up");
//        }
//    }
//}
