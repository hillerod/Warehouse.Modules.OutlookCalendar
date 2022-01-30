using Bygdrift.Warehouse.Helpers.Attributes;

namespace Module
{
    public class Settings
    {
        [ConfigSecret(NotSet = NotSet.ThrowError)]
        public string FTPConnectionString { get; set; }
    }
}
