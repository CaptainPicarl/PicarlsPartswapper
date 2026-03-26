using Microsoft.Xml.Serialization.GeneratedAssembly;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using SpaceEngineers.Game.ModAPI.Ingame;
using SpaceEngineers.ObjectBuilders;
using SpaceEngineers.ObjectBuilders.ObjectBuilders.Definitions;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using VRage.FileSystem;
using VRage.Game;
using VRage.Game.ObjectBuilders;
using VRage.ObjectBuilders;

namespace Partswapper2026.XML
{
    public static class XMLTools
    {

        public static void Deserialize()
        {
            XNamespace xsi = "http://www.w3.org/2001/XMLSchema-instance";
            XNamespace xsd = "http://www.w3.org/2001/XMLSchema";

            string path = "E:\\SteamLibrary\\steamapps\\common\\SpaceEngineers\\Content\\Data\\CubeBlocks\\CubeBlocks_Warfare1.sbc";
            string contentPath = "E:\\SteamLibrary\\steamapps\\common\\SpaceEngineers\\Content";
            XDocument xmlFile = LoadPath(path);

            XElement Definitions = xmlFile.Root;

            XElement CubeBlocks = Definitions.Element("CubeBlocks");

            Assembly vrageGame = Assembly.GetAssembly(typeof(MyObjectBuilder_CargoContainerDefinition));
            Assembly SESerialization = Assembly.GetAssembly(typeof(MyObjectBuilder_CargoContainerDefinitionSerializer));

            //MyObjectBuilder_CargoContainerDefinitionSerializer CargoDeserializer = new MyObjectBuilder_CargoContainerDefinitionSerializer();

            string vrageGameNamespace = vrageGame.ToString().Split(',')[0];
            string SESerializationNamespace = SESerialization.ToString().Split(',')[0];

            Type[] vrageGameTypes = vrageGame.GetTypes();
            Type[] SESerializationTypes = SESerialization.GetTypes();

            XmlSerializerFactory serializerFactory = new XmlSerializerFactory();

            MyFileSystem.Init(contentPath,Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SpaceEngineers"));

            //MyObjectBuilder_Definitions defs = Load<MyObjectBuilder_Definitions>(path);

            CubeBlocks.Elements().ForEach(element =>
            {
                string type = $"{vrageGameNamespace}.{element.FirstAttribute.Value}";
                Type parsedType = vrageGame.GetType(type);
                //Type parsedType = typeof(MyObjectBuilder_Definitions);
                XmlReader xmlReader = element.CreateReader();

                MyObjectBuilder_Base baseObj;
                MyObjectBuilderSerializer.DeserializeXML(path, out baseObj, parsedType);
            });
        }

        // Deserialize from XML.
        public static T Load<T>(string path) where T : MyObjectBuilder_Base
        {
            T result = null;
            MyObjectBuilderSerializer.DeserializeXML<T>(path, out result);
            return result;
        }

        public static XDocument LoadPath(string path)
        {
            return XDocument.Load(path);
        }
    }
}
