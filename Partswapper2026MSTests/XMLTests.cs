using Partswapper2026;

namespace Partswapper2026MSTests
{
    [TestClass]
    public sealed class XMLTests
    {
        public static string ShipBlueprintSBCPath = Environment.GetEnvironmentVariable("ShipBlueprintSBCPath");
        public static string CubeBlocksSBCPath = Environment.GetEnvironmentVariable("CubeBlocksSBCPath");
        public static string SEContentPath = Environment.GetEnvironmentVariable("SEContentPath");
        public static string WorkshopDirPath = Environment.GetEnvironmentVariable("WorkshopDirPath");

        [TestMethod]
        public void TestDeserialization()
        {
            Partswapper2026.XML.XMLTools.Deserialize();
        }
    }
}
