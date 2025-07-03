using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Xml;
using System.Xml.Linq;

namespace PartSwapperXMLSE
{
    // Rules for objects:
    // -> .Rename returns tuples of (oldname,newname) where both types are strings
    // -> .Load loads all sub-components of an object by calling the sub-components' .Load methods
    // -> Naming scheme for objects is: "{$FileDefinitionCanBeFoundIn}_{$NameOfDefinition}"

    // Note: There is some heavy use of ternary (condition ? true:false) operations for null-safety.

    using ReplacementResult = Tuple<string, string>;
    using LoadResult = bool;
    using BlockGroupsDictionary = Dictionary<string, List<Vector3>>;
    using NameResolverDimensionEnum = Dictionary<string, DimensionEnum3D>;
    using NameResolverCubeSizeEnum = Dictionary<string, CubeSize>;
    using NameResolverMountPointSideEnum = Dictionary<string, MountPointSide>;
    using NameResolverGridSizeEnum = Dictionary<string, GridSize>;
    using static PartSwapperXMLSE.BlueprintSBC_CubeGrid;

    public enum GridSize
    {
        Large,
        Small,
        Unknown
    }

    public enum DefinitionSource
    {
        Vanilla,
        Mod,
        Unknown
    }

    public enum CubeSize
    {
        Large,
        Small,
        Unknown
    }

    public enum MountPointSide
    {
        Front,
        Back,
        Right,
        Left,
        Bottom,
        Top
    }

    public enum DimensionEnum3D
    {
        X,
        Y,
        Z,
        XHalfX,
        XHalfY,
        XHalfZ,
        YHalfX,
        YHalfY,
        YHalfZ,
        ZHalfX,
        ZHalfY,
        ZHalfZ,
        MinusHalfX,
        MinusHalfY,
        MinusHalfZ,
        HalfX,
        HalfY,
        HalfZ,
        XMinusHalfX,
        XMinusHalfY,
        XMinusHalfZ,
        YMinusHalfX,
        YMinusHalfY,
        YMinusHalfZ,
        ZMinusHalfX,
        ZMinusHalfY,
        ZMinusHalfZ,
        ZThenOffsetX,
        YThenOffsetX,
        OffsetX,
        ZThenOffsetXOdd,
        YThenOffsetXOdd,
        OffsetXOddTest,
        None
    }
    public static class PartSwapperModelsNameResolvers
    {
        public static NameResolverGridSizeEnum GridSizeNameResolver = new NameResolverGridSizeEnum() { { "Large", GridSize.Large }, { "Small", GridSize.Small } };
        public static NameResolverCubeSizeEnum CubeSizeNameResolver = new NameResolverCubeSizeEnum() { { "Large", CubeSize.Large }, { "Small", CubeSize.Small } };
        public static NameResolverMountPointSideEnum MountPointSideNameResolver = new NameResolverMountPointSideEnum() { { "Front", MountPointSide.Front }, { "Back", MountPointSide.Back }, { "Bottom", MountPointSide.Bottom }, { "Top", MountPointSide.Top }, { "Left", MountPointSide.Left }, { "Right", MountPointSide.Right } };
        public static NameResolverDimensionEnum DimensionNameResolver = new NameResolverDimensionEnum() {
        { "X", DimensionEnum3D.X }, { "Y", DimensionEnum3D.Y }, { "Z", DimensionEnum3D.Z },
        { "XHalfX", DimensionEnum3D.XHalfX }, { "XHalfY", DimensionEnum3D.XHalfY },{ "XHalfZ", DimensionEnum3D.XHalfZ},
        { "YHalfX", DimensionEnum3D.XHalfX }, { "YHalfY", DimensionEnum3D.XHalfY },{ "YHalfZ", DimensionEnum3D.XHalfZ},
        { "ZHalfX", DimensionEnum3D.XHalfX }, { "ZHalfY", DimensionEnum3D.XHalfY },{ "ZHalfZ", DimensionEnum3D.XHalfZ},
        { "MinusHalfX", DimensionEnum3D.MinusHalfX }, { "MinusHalfY", DimensionEnum3D.MinusHalfY },{ "MinusHalfZ", DimensionEnum3D.MinusHalfZ},
        { "HalfX", DimensionEnum3D.HalfX }, { "HalfY", DimensionEnum3D.HalfY },{ "HalfZ", DimensionEnum3D.HalfZ},
        { "XMinusHalfX", DimensionEnum3D.XMinusHalfX }, { "XMinusHalfY", DimensionEnum3D.XMinusHalfY },{ "XMinusHalfZ", DimensionEnum3D.XMinusHalfZ},
        { "YMinusHalfX", DimensionEnum3D.YMinusHalfX }, { "YMinusHalfY", DimensionEnum3D.YMinusHalfY },{ "YMinusHalfZ", DimensionEnum3D.YMinusHalfZ},
        { "ZMinusHalfX", DimensionEnum3D.ZMinusHalfX }, { "ZMinusHalfY", DimensionEnum3D.ZMinusHalfY },{ "ZMinusHalfZ", DimensionEnum3D.ZMinusHalfZ},
        { "ZThenOffsetX", DimensionEnum3D.ZThenOffsetX }, { "YThenOffsetX", DimensionEnum3D.YThenOffsetX },
        { "ZThenOffsetXOdd", DimensionEnum3D.ZThenOffsetXOdd }, { "YThenOffsetXOdd", DimensionEnum3D.YThenOffsetXOdd },
        { "OffsetX", DimensionEnum3D.OffsetX }, { "OffsetXOddTest", DimensionEnum3D.OffsetXOddTest },
        { "None", DimensionEnum3D.None }
        };

        public static string NoDataAssignedString = "NODATA";
        public static int NoDataAssignedInt = -1;
        public static float NoDataAssignedFloat = -1;
    }

    public static class XMLTools
    {

        public static Vector3 ParseVectorXElement(XElement vectorNode)
        {
            string tempVectorX = "";
            string tempVectorY = "";
            string tempVectorZ = "";

            float parsedVectorX;
            float parsedVectorY;
            float parsedVectorZ;

            List<XAttribute> tempAttributes;
            Vector3 resultVector;

            if (vectorNode == null)
            {
                Trace.WriteLine($"ParseVectorXElement was given a null vectorNode parameter! Returning Zero Vector!\n");
                return Vector3.Zero;
            }

            tempAttributes = vectorNode.Attributes().ToList<XAttribute>();

            void ParseTempVariables()
            {

                parsedVectorX = Single.Parse(tempVectorX, CultureInfo.InvariantCulture);
                parsedVectorY = Single.Parse(tempVectorY, CultureInfo.InvariantCulture);
                parsedVectorZ = Single.Parse(tempVectorZ, CultureInfo.InvariantCulture);
            }

            // Note: There are many 'styles' of Vector node representations in the XML files.
            // We will break them into 'cases'.

            // Case 1: The X/Y/Z to be parsed is contained in the attributes.
            // Check that the node we are looking at has the three dimensions needed to make a vector3
            if (tempAttributes.Any(xElement => xElement.Name.LocalName.Equals("x")) &&
                tempAttributes.Any(xElement => xElement.Name.LocalName.Equals("y")) &&
                tempAttributes.Any(xElement => xElement.Name.LocalName.Equals("z")))
            {

                foreach (XAttribute attribute in tempAttributes)
                {
                    if (attribute.Name.LocalName.Equals("x"))
                    {
                        tempVectorX = attribute.Value;
                    }

                    if (attribute.Name.LocalName.Equals("y"))
                    {
                        tempVectorY = attribute.Value;
                    }

                    if (attribute.Name.LocalName.Equals("z"))
                    {
                        tempVectorZ = attribute.Value;
                    }
                }

                ParseTempVariables();

                resultVector = new Vector3(parsedVectorX, parsedVectorY, parsedVectorZ);
                return resultVector;
            }

            if (vectorNode.Elements().Any(node => node.Name.LocalName.Equals("X")) &&
                vectorNode.Elements().Any(node => node.Name.LocalName.Equals("Y")) &&
                vectorNode.Elements().Any(node => node.Name.LocalName.Equals("Z")))
            {
                tempVectorX = vectorNode.Element("X").Value;
                tempVectorY = vectorNode.Element("Y").Value;
                tempVectorZ = vectorNode.Element("Z").Value;

                ParseTempVariables();

                resultVector = new Vector3(parsedVectorX, parsedVectorY, parsedVectorZ);
                return resultVector;
            }


            throw new Exception($"Failed to parse the following Vectornode:\n{vectorNode}\n");
        }

        public static XElement LoadXMLDocument(string filePath)
        {
            XElement documentRootNode;
            string fileExtension = Path.GetExtension(filePath);

            bool fileExists = File.Exists(filePath);
            bool fileExtensionSBC = fileExtension.Equals(".sbc");
            bool fileExtensionXML = fileExtension.Equals(".xml");
            if (fileExists && (fileExtensionSBC || fileExtensionXML))
            {
                documentRootNode = XElement.Load(filePath);
                return documentRootNode;
            }
            else
            {
                throw new ArgumentException($"LoadXMLDocument: Bad filepath!\nInput Filepath:{filePath}\n");
            }

        }
    }


    // This class iterates through folders for a given path and loads Cubeblock Definition instances into a Dictionary with a FileName:List<CubeBlockDefs> structure
    public class DataFolder_Model
    {
        // Dictionary of all the detected blocks
        Dictionary<string, List<CubeBlockDefinitionSBC_CubeBlockDefinition>> CubeBlocksDefDict = new Dictionary<string, List<CubeBlockDefinitionSBC_CubeBlockDefinition>>();
        Dictionary<string, List<ComponentsSBC_ComponentDefinition>> ComponentsDefDict = new Dictionary<string, List<ComponentsSBC_ComponentDefinition>>();

        public DataFolder_Model(string dataDirectoryPath)
        {
            DirectoryInfo[] modFolderSubDirectories;

            DirectoryInfo modFolderRoot;
            DirectoryInfo dataDirectory;

            FileInfo[] dataFiles;

            XElement modfileIterator;
            XElement XElementIterator;

            CubeBlockDefinitionSBC_CubeBlockDefinition cubeblockSBCDefIterator;
            List<CubeBlockDefinitionSBC_CubeBlockDefinition> cubeblocksSBCDefList = new List<CubeBlockDefinitionSBC_CubeBlockDefinition>();

            ComponentsSBC_ComponentDefinition componentSBCDefIterator;
            List<ComponentsSBC_ComponentDefinition> componentsSBCDefList = new List<ComponentsSBC_ComponentDefinition>();

            string filepathIterator;

            DefinitionSource _DetectedDefinitionSource = DefinitionSource.Unknown;
            
            try
            {
                if (dataDirectoryPath == null || dataDirectoryPath.Equals(""))
                {
                    // This happens when the user cancels out of the mod selection folder.
                    // We'll handle this more gracefully in the future. Maybe.
                    return;
                }

                // assign the modFolderRoot
                
                modFolderRoot = new DirectoryInfo(dataDirectoryPath);

                if (!modFolderRoot.FullName.EndsWith("workshop\\content\\244850\\") &&
                    !modFolderRoot.FullName.EndsWith("workshop\\content\\244850") && 
                    !modFolderRoot.FullName.EndsWith("SpaceEngineers\\Content\\") &&
                    !modFolderRoot.FullName.EndsWith("SpaceEngineers\\Content"))
                {
                    throw new InvalidDataException($"Error: Invalid folder!\nPath should end in: workshop\\content\\244850 or SpaceEngineers\\Content, path is: {modFolderRoot.FullName}\n");
                }

                if(modFolderRoot.FullName.EndsWith("SpaceEngineers\\Content\\") ||
                    modFolderRoot.FullName.EndsWith("SpaceEngineers\\Content"))
                {
                    _DetectedDefinitionSource = DefinitionSource.Vanilla;
                } else 
                
                if(modFolderRoot.FullName.EndsWith("workshop\\content\\244850\\") ||
                    modFolderRoot.FullName.EndsWith("workshop\\content\\244850"))
                {
                    _DetectedDefinitionSource = DefinitionSource.Mod;

                } else
                {
                    _DetectedDefinitionSource = DefinitionSource.Unknown;
                }

            }
            catch (Exception ex)
            {
                throw new InvalidDataException($"DataFolder_Model ERROR:\n{ex}");
            }

            modFolderSubDirectories = modFolderRoot.GetDirectories("*", SearchOption.AllDirectories);

            foreach (DirectoryInfo ModSubDirectory in modFolderSubDirectories)
            {
                foreach (FileInfo modFile in ModSubDirectory.GetFiles("*.sbc"))
                {
                    try
                    {
                        modfileIterator = XMLTools.LoadXMLDocument(modFile.FullName);

                        // Begin figuring out what kind of element we have
                        switch (modfileIterator.Elements().First().Name.ToString())
                        {
                            case "CubeBlocks":
                                XElementIterator = modfileIterator.Element("CubeBlocks");

                                if (XElementIterator != null)
                                {
                                    cubeblocksSBCDefList = new List<CubeBlockDefinitionSBC_CubeBlockDefinition>();

                                    foreach (XElement element in XElementIterator.Elements())
                                    {
                                        cubeblockSBCDefIterator = new CubeBlockDefinitionSBC_CubeBlockDefinition(element, _DetectedDefinitionSource, ModSubDirectory.Name);
                                        cubeblocksSBCDefList.Add(cubeblockSBCDefIterator);
                                    }

                                    this.CubeBlocksDefDict[$"{ModSubDirectory.FullName}\\{modFile.Name}"] = cubeblocksSBCDefList;
                                }
                                break;
                            case "Components":
                                XElementIterator = modfileIterator.Element("Components");
                                if (XElementIterator != null)
                                {
                                    componentsSBCDefList = new List<ComponentsSBC_ComponentDefinition>();

                                    foreach (XElement element in XElementIterator.Elements())
                                    {
                                        componentSBCDefIterator = new ComponentsSBC_ComponentDefinition(element, _DetectedDefinitionSource, ModSubDirectory.Name);
                                        componentsSBCDefList.Add(componentSBCDefIterator);
                                    }

                                    this.ComponentsDefDict[$"{ModSubDirectory.FullName}\\{modFile.Name}"] = componentsSBCDefList;
                                }
                                break;
                        }

                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine($"Error loading/parsing modfile at path:\n{modFile.FullName}\nException is:\n{ex}");
                    }
                }
            }

        }

        public Dictionary<string,List<CubeBlockDefinitionSBC_CubeBlockDefinition>> GetCubeBlocksDefDict()
        {
            return this.CubeBlocksDefDict;
        }

        public Dictionary<string, List<ComponentsSBC_ComponentDefinition>> GetComponentsDefDict()
        {
            return this.ComponentsDefDict;
        }
    }

    public class BlockVariantGroupsSBC_BlockVariantGroupsFile
    {
        string filePath;
        XElement rootFileXElement;
        List<BlockVariantGroupsSBC_BlockVariantGroup> blockVariantGroups;

        public BlockVariantGroupsSBC_BlockVariantGroupsFile(string filePath)
        {
            this.filePath = filePath;
            this.rootFileXElement = XElement.Load(filePath);
            this.blockVariantGroups = new List<BlockVariantGroupsSBC_BlockVariantGroup>();
        }

        LoadResult LoadBlockVariantGroups(XElement blockVariantGroupsFileRoot)
        {
            XElement blockVariantGroupsXElement;

            BlockVariantGroupsSBC_BlockVariantGroup blockVariantGroupIterator;

            blockVariantGroupsXElement = blockVariantGroupsFileRoot.Element("BlockVariantGroups");

            this.blockVariantGroups = new List<BlockVariantGroupsSBC_BlockVariantGroup>();

            foreach (XElement blockVariantGroup in blockVariantGroupsXElement.Elements())
            {
                blockVariantGroupIterator = new BlockVariantGroupsSBC_BlockVariantGroup(blockVariantGroup);
                blockVariantGroups.Add(blockVariantGroupIterator);
            }

            //Pass/Fail LoadResult Condition
            if (this.blockVariantGroups.Count == blockVariantGroupsXElement.Elements().Count())
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    public class BlockVariantGroupsSBC_BlockVariantGroup
    {
        XElement blockVariantGroupXElement;

        string Type;
        string Subtype;
        string Icon;
        string DisplayName;
        string Description;

        List<BlockVariantGroupBlock> blocks;

        public BlockVariantGroupsSBC_BlockVariantGroup(XElement blockVariantGroupXElement)
        {
            this.blockVariantGroupXElement = blockVariantGroupXElement;
        }

        public class BlockVariantGroupBlock
        {
            string Type;
            string Subtype;

            public BlockVariantGroupBlock(string Type, string Subtype)
            {
                this.Type = Type;
                this.Subtype = Subtype;
            }
        }

        LoadResult LoadDisplayName()
        {
            this.DisplayName = this.blockVariantGroupXElement.Element("DisplayName").Value;

            //Pass/Fail LoadResult Condition
            if (!this.DisplayName.Equals(""))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        LoadResult LoadDescription()
        {
            this.Description = this.blockVariantGroupXElement.Element("Description").Value;

            //Pass/Fail LoadResult Condition
            if (!this.Description.Equals(""))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        LoadResult LoadIcon()
        {
            this.Icon = this.blockVariantGroupXElement.Element("Icon").Value;

            //Pass/Fail LoadResult Condition
            if (!this.Icon.Equals(""))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        LoadResult LoadBlocks()
        {
            XElement blocksElement = this.blockVariantGroupXElement.Element("Blocks");
            BlockVariantGroupBlock blockIter;
            string TypeIter;
            string SubtypeIter;

            foreach (XElement block in blocksElement.Elements())
            {
                TypeIter = block.Attribute("Type").Value;
                SubtypeIter = block.Attribute("Subtype").Value;
                blockIter = new BlockVariantGroupBlock(TypeIter, SubtypeIter);
            }

            // Pass/Fail condition
            if (this.blocks.Count != 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    public class CubeBlocksDefinitionSBC_BuildProgressModel
    {
        XElement cubeblockXElement;

        double BuildPercentageUpperBound;
        string File;

        public CubeBlocksDefinitionSBC_BuildProgressModel(double BuildPercentageUpperBound, string File)
        {
            this.BuildPercentageUpperBound = BuildPercentageUpperBound;
            this.File = File;
        }

        public CubeBlocksDefinitionSBC_BuildProgressModel(XElement buildProgressModelXElement)
        {
            this.cubeblockXElement = buildProgressModelXElement;
            this.Load();

        }

        LoadResult Load()
        {
            try
            {
                this.BuildPercentageUpperBound = double.Parse(this.cubeblockXElement.Attribute("BuildPercentUpperBound").Value);
                this.File = this.cubeblockXElement.Attribute("File").Value;
                return true;
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Error in CubeBlocksDefinitionSBC_BuildProgressModel Load! Exception:\n{ex}\n");
                return false;
            }
        }
    }
    public class CubeBlocksDefinitionSBC_Bone
    {

        XElement cubeblockXElement;

        Vector3 BonePosition;
        Vector3 BoneOffset;

        public CubeBlocksDefinitionSBC_Bone(XElement cubeblockXElement)
        {
            this.cubeblockXElement = cubeblockXElement;
            this.Load();
        }


        public LoadResult Load()
        {
            try
            {
                this.BoneOffset = XMLTools.ParseVectorXElement(cubeblockXElement.Element("BoneOffset"));
                this.BonePosition = XMLTools.ParseVectorXElement(cubeblockXElement.Element("BonePosition"));
                return true;
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"CubeBlocksDefinitionSBC_Bone load failed due to exception:\n" + ex + "\n");
                return false;
            }

        }
    }
    public class CubeBlocksDefinitionSBC_MountPoint
    {
        XElement cubeblockXElement;

        MountPointSide MountPointSide;
        double StartX;
        double EndX;
        double StartY;
        double EndY;
        bool? Default;

        public CubeBlocksDefinitionSBC_MountPoint(XElement cubeblockXElement)
        {
            this.cubeblockXElement = cubeblockXElement;
            this.Load();
        }

        public CubeBlocksDefinitionSBC_MountPoint(MountPointSide mountPointSide, double startX, double startY, double endX, double endY, bool Default)
        {
            this.MountPointSide = mountPointSide;
            this.StartX = startX;
            this.StartY = startY;
            this.EndX = endX;
            this.EndY = endY;
            this.Default = Default;
        }

        public LoadResult Load()
        {
            try
            {
                this.MountPointSide = PartSwapperModelsNameResolvers.MountPointSideNameResolver[cubeblockXElement.Attribute("Side").Value];
                this.StartX = double.Parse(cubeblockXElement.Attribute("StartX").Value);
                this.StartY = double.Parse(cubeblockXElement.Attribute("StartY").Value);
                this.EndX = double.Parse(cubeblockXElement.Attribute("EndX").Value);
                this.EndY = double.Parse(cubeblockXElement.Attribute("EndY").Value);
                this.Default = cubeblockXElement.Attribute("Default") != null ? bool.Parse(cubeblockXElement.Attribute("Default").Value) : null;

                return true;
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"CubeBlocksDefinitionSBC_MountPoint load failed due to exception:\n" + ex + "\n");
                return false;
            }

        }
    }


    public class CubeBlockDefinitionSBC_CriticalComponent
    {
        XElement cubeblockXElement;

        string Subtype;
        int Index;

        public CubeBlockDefinitionSBC_CriticalComponent(XElement cubeblockXElement)
        {
            this.cubeblockXElement = cubeblockXElement;
            this.Load();
        }

        public CubeBlockDefinitionSBC_CriticalComponent(string Subtype, int count)
        {
            this.Subtype = Subtype;
            this.Index = count;
        }

        public LoadResult Load()
        {
            try
            {
                this.Subtype = cubeblockXElement.Attribute("Subtype") != null ? cubeblockXElement.Attribute("Subtype").Value : "";
                this.Index = cubeblockXElement.Attribute("Index") != null ? int.Parse(cubeblockXElement.Attribute("Index").Value) : 0;
                return true;
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"CubeBlockDefinitionSBC_CriticalComponent Load Failed! Exception is:\n{ex.ToString()}\n");
                return false;
            }

        }
    }

    public class CubeBlocksDefinitionSBC_CubeDefinition
    {
        string CubeTopology;
        bool ShowEdges;
        List<Side> Sides;

        public class Side
        {
            XElement CubeDefinitionSideXElement;
            string Model;
            int PatternWidth;
            int PatternHeight;

            public Side(XElement CubeDefinitionSideXElement)
            {
                this.CubeDefinitionSideXElement = CubeDefinitionSideXElement;
                this.Load();
            }

            LoadResult Load()
            {
                try
                {
                    this.Model = this.CubeDefinitionSideXElement.Attribute("Model").Value;
                    this.PatternWidth = int.Parse(this.CubeDefinitionSideXElement.Attribute("PatternWidth").Value);
                    this.PatternHeight = int.Parse(this.CubeDefinitionSideXElement.Attribute("PatternHeight").Value);
                    return true;
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Error in CubeBlocksDefinitionSBC_CubeDefinition.Side Load! Exception is:\n{ex}\n");
                    return false;
                }
            }
        }

        public CubeBlocksDefinitionSBC_CubeDefinition(XElement CubeDefinitionXElement)
        {
            if (CubeDefinitionXElement != null)
            {
                this.CubeTopology = CubeDefinitionXElement.Element("CubeTopology").Value;
                this.ShowEdges = bool.Parse(CubeDefinitionXElement.Element("ShowEdges").Value);
                this.Sides = new List<Side>();

                // Load sides
                foreach (XElement side in CubeDefinitionXElement.Element("Sides").Elements())
                {
                    Sides.Add(new Side(side));
                }
            }
            else
            {
                throw new Exception("Exception in CubeBlocksDefinitionSBC_CubeDefinition!\nNull Block Found!");
            }

        }
    }

    public class CubeBlockDefinitionSBC_CubeBlockDefinition
    {
        public static string BlockSourceVanillaKey = "VANILLA";

        XElement CubeBlockDefinitionXElement;

        public string TypeId { get => _TypeId; }
        public string  SubtypeID { get => _SubtypeID; }
        public string DisplayName { get => _DisplayName; }
        public string Icon { get => _Icon; }
        public string Description { get => _Description; }

        private string _TypeId = "";
        private string _SubtypeID = "";
        private string _DisplayName = "";
        private string _Icon = "";
        private string _Description = "";

        CubeSize CubeSize;

        bool GUIVisible;
        bool PlaceDecals;
        string BlockTopology;
        Vector3 Size;
        Vector3 ModelOffset;
        string Model;
        CubeBlocksDefinitionSBC_CubeDefinition? CubeDefinition;
        bool UseModelIntersection;

        Queue<BluePrintSBC_Component> Components = new Queue<BluePrintSBC_Component>();
        CubeBlockDefinitionSBC_CriticalComponent CriticalComponent;
        List<CubeBlocksDefinitionSBC_MountPoint> MountPoints = new List<CubeBlocksDefinitionSBC_MountPoint>();
        List<CubeBlocksDefinitionSBC_BuildProgressModel> BuildProgressModels = new List<CubeBlocksDefinitionSBC_BuildProgressModel>();
        List<CubeBlocksDefinitionSBC_Bone> Skeleton = new List<CubeBlocksDefinitionSBC_Bone>();
        BlueprintSBC_CubeBlock? BlockPairName;

        string ActionSound = "";
        string EdgeType = "";
        float BuildTimeSeconds = -1;
        float ExplosionRadius = -1;
        int WarheadExplosionDamage = -1;
        string EmissiveColorPresent = "";
        string DestroyEffect = "";
        string DestroySound = "";
        int PCU = -1;
        int PCUConsole = -1;

        DimensionEnum3D? MirroringY = null;
        DimensionEnum3D? MirroringZ = null;

        bool? IsAirTight = null;

        List<string> TargetingGroups = new List<string>();

        // Non-modeling fields
        DefinitionSource DefinitionSource { get; set; } = DefinitionSource.Unknown;
        string DefinitionSourceString { get; set; } = "UNASSIGNED!";

        public CubeBlockDefinitionSBC_CubeBlockDefinition(XElement cubeblockFileRootXElement, DefinitionSource defSource, string defSourceString = "UNASSIGNED")
        {

            if(defSource == DefinitionSource.Vanilla)
            {
                this.DefinitionSource = defSource;
                this.DefinitionSourceString = BlockSourceVanillaKey;
            } else
            {
                this.DefinitionSource = defSource;
                this.DefinitionSourceString = defSourceString;
            }

            if (cubeblockFileRootXElement != null)
            {
                this.CubeBlockDefinitionXElement = cubeblockFileRootXElement;

                this.Load();
            }
            else
            {
                throw new Exception($"Error in CubeBlockDefinitionSBC_CubeBlockDefinition constructor!\nInvalid Node:\n{cubeblockFileRootXElement.ToString()}");
            }
        }

        public CubeBlockDefinitionSBC_CubeBlockDefinition(BlueprintSBC_CubeBlock cubeblockSBCDef)
        {
            this._SubtypeID = cubeblockSBCDef.SubtypeName;
        }

        public XElement GetCubeBlockDefinitionXElement()
        {
            return this.CubeBlockDefinitionXElement;
        }

        public string GetTypeID()
        {
            return this.TypeId;
        }

        public string GetSubTypeID()
        {
            return this.SubtypeID;
        }

        public string GetDisplayName()
        {
            return this.DisplayName;
        }

        public string GetIcon()
        {
            return this.Icon;
        }

        public string GetDescription()
        {
            return this.Description;
        }

        public CubeSize GetCubeSize()
        {
            return this.CubeSize;
        }

        public bool GetGUIVisible()
        {
            return this.GUIVisible;
        }

        public bool GetPlaceDecals()
        {
            return this.PlaceDecals;
        }

        public string GetBlockTopology()
        {
            return this.BlockTopology;
        }

        public Vector3 GetSize()
        {
            return this.Size;
        }

        public Vector3 GetModelOffset()
        {
            return this.ModelOffset;
        }

        public string GetModel()
        {
            return this.Model;
        }

        public CubeBlocksDefinitionSBC_CubeDefinition? GetCubeDefinition()
        {
            return this.CubeDefinition;
        }

        public bool GetUseModelIntersection()
        {
            return this.UseModelIntersection;
        }

        public Queue<BluePrintSBC_Component> GetComponents()
        {
            return this.Components;
        }

        public CubeBlockDefinitionSBC_CriticalComponent GetCriticalComponent()
        {
            return this.CriticalComponent;
        }

        public List<CubeBlocksDefinitionSBC_MountPoint> GetMountPoints()
        {
            return this.MountPoints;
        }

        public List<CubeBlocksDefinitionSBC_BuildProgressModel> GetBuildProgressModels()
        {
            return this.BuildProgressModels;
        }

        public List<CubeBlocksDefinitionSBC_Bone> GetSkeleton()
        {
            return this.Skeleton;
        }

        public BlueprintSBC_CubeBlock? GetBlockPairName()
        {
            return this.BlockPairName;
        }

        public string GetDefinitionSourceString()
        {
            return this.DefinitionSourceString;
        }

        public int GetPCU()
        {
            return this.PCU;
        }

        public override string ToString()
        {
            return $"SubtypeID:{this.SubtypeID},Definition Source:{this.DefinitionSourceString},PCU:{this.PCU}\n";
        }

        public LoadResult Load()
        {

            try
            {
                try
                {
                    if(CubeBlockDefinitionXElement.Element("Id") != null && CubeBlockDefinitionXElement.Element("Id").Element("TypeId") != null)
                    {
                        this._TypeId = CubeBlockDefinitionXElement.Element("Id").Element("TypeId").Value;
                    }
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Failure in CubeBlockDefinitionSBC_CubeBlockDefinition Load! Exception is:\n{ex}\n");
                }

                try
                {
                    if (CubeBlockDefinitionXElement.Element("Id") != null && CubeBlockDefinitionXElement.Element("Id").Element("SubtypeId") != null)
                    {
                        this._SubtypeID = CubeBlockDefinitionXElement.Element("Id").Element("SubtypeId").Value;
                    } else
                    {
                        this._SubtypeID = "// NO SUBTYPEID PROVIDED //";
                    }
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Failure in CubeBlockDefinitionSBC_CubeBlockDefinition Load! Exception is:\n{ex}\n");
                }

                try
                {
                    if (CubeBlockDefinitionXElement.Element("DisplayName") != null)
                    {
                        this._DisplayName = CubeBlockDefinitionXElement.Element("DisplayName").Value;
                    } else
                    {
                        this._DisplayName = "// NO DISPLAYNAME PROVIDED //";
                    }
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Failure in CubeBlockDefinitionSBC_CubeBlockDefinition Load! Exception is:\n{ex}\n");
                }

                try
                {
                    if(CubeBlockDefinitionXElement.Element("Icon") != null)
                    {
                        this._Icon = CubeBlockDefinitionXElement.Element("Icon").Value;
                    } else
                    {
                        this._Icon = "// NO ICON PATH PROVIDED //";
                    }
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Failure in CubeBlockDefinitionSBC_CubeBlockDefinition Load! Exception is:\n{ex}\n");
                }

                try
                {
                    this._Description = CubeBlockDefinitionXElement.Element("Description") != null ? CubeBlockDefinitionXElement.Element("Description").Value : "";
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Failure in CubeBlockDefinitionSBC_CubeBlockDefinition Load! Exception is:\n{ex}\n");
                }

                try
                {
                    this.CubeSize = CubeBlockDefinitionXElement.Element("CubeSize") != null ? PartSwapperModelsNameResolvers.CubeSizeNameResolver[CubeBlockDefinitionXElement.Element("CubeSize").Value] : CubeSize.Unknown;
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Failure in CubeBlockDefinitionSBC_CubeBlockDefinition Load! Exception is:\n{ex}\n");
                }

                try
                {
                    this.BlockTopology = CubeBlockDefinitionXElement.Element("BlockTopology") != null ? CubeBlockDefinitionXElement.Element("BlockTopology").Value : "";
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Failure in CubeBlockDefinitionSBC_CubeBlockDefinition Load! Exception is:\n{ex}\n");
                }

                try
                {
                    this.Size = XMLTools.ParseVectorXElement(CubeBlockDefinitionXElement.Element("Size"));
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Failure in CubeBlockDefinitionSBC_CubeBlockDefinition Load! Exception is:\n{ex}\n");
                }

                try
                {
                    this.ModelOffset = XMLTools.ParseVectorXElement(CubeBlockDefinitionXElement.Element("ModelOffset"));
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Failure in CubeBlockDefinitionSBC_CubeBlockDefinition Load! Exception is:\n{ex}\n");
                }

                try
                {
                    this.Model = CubeBlockDefinitionXElement.Element("Model") != null ? CubeBlockDefinitionXElement.Element("Model").Value : "";
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Failure in CubeBlockDefinitionSBC_CubeBlockDefinition Load! Exception is:\n{ex}\n");
                }

                try
                {
                    this.CubeDefinition = CubeBlockDefinitionXElement.Element("CubeDefinition") != null ? new CubeBlocksDefinitionSBC_CubeDefinition(CubeBlockDefinitionXElement.Element("CubeDefinition")) : null;
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Failure in CubeBlockDefinitionSBC_CubeBlockDefinition Load! Exception is:\n{ex}\n");
                }


                try
                {
                    //Load Components
                    this.Components = new Queue<BluePrintSBC_Component>();
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Failure in CubeBlockDefinitionSBC_CubeBlockDefinition Load! Exception is:\n{ex}\n");
                }

                try
                {
                    if (this.CubeBlockDefinitionXElement.Element("Components") != null)
                    {
                        foreach (XElement componentXElement in this.CubeBlockDefinitionXElement.Element("Components").Elements())
                        {
                            this.Components.Enqueue(new BluePrintSBC_Component(componentXElement));
                        }
                    }
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Failure in CubeBlockDefinitionSBC_CubeBlockDefinition Load! Exception is:\n{ex}\n");
                }

                try
                {
                    if(this.CubeBlockDefinitionXElement.Element("CriticalComponent") != null)
                    {
                        this.CriticalComponent = new CubeBlockDefinitionSBC_CriticalComponent(this.CubeBlockDefinitionXElement.Element("CriticalComponent"));
                    }
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Failure in CubeBlockDefinitionSBC_CubeBlockDefinition Load! Exception is:\n{ex}\n");
                }


                try
                {
                    // Load Mountpoints
                    if (this.CubeBlockDefinitionXElement.Element("MountPoints") != null)
                    {
                        foreach (XElement mountpointXElement in this.CubeBlockDefinitionXElement.Element("MountPoints").Elements())
                        {
                            this.MountPoints.Add(new CubeBlocksDefinitionSBC_MountPoint(mountpointXElement));
                        }
                    }
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Failure in CubeBlockDefinitionSBC_CubeBlockDefinition Load! Exception is:\n{ex}\n");
                }

                try
                {
                    // Load BuildProgressModels
                    if (this.CubeBlockDefinitionXElement.Element("BuildProgressModels") != null)
                    {
                        foreach (XElement buildProgressModelXElement in this.CubeBlockDefinitionXElement.Element("BuildProgressModels").Elements())
                        {
                            this.BuildProgressModels.Add(new CubeBlocksDefinitionSBC_BuildProgressModel(buildProgressModelXElement));
                        }
                    }
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Failure in CubeBlockDefinitionSBC_CubeBlockDefinition Load! Exception is:\n{ex}\n");
                }


                try
                {

                    // Load Skeleton
                    if (this.CubeBlockDefinitionXElement.Element("Skeleton") != null)
                    {
                        foreach (XElement boneXElement in this.CubeBlockDefinitionXElement.Element("Skeleton").Elements())
                        {
                            this.Skeleton.Add(new CubeBlocksDefinitionSBC_Bone(boneXElement));
                        }
                    }
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Failure in CubeBlockDefinitionSBC_CubeBlockDefinition Load! Exception is:\n{ex}\n");
                }


                try
                {

                    // Blockpair name will start null.
                    // Note: We will want to load all the blocks - *then* resolve the blockPairs
                    this.BlockPairName = null;

                    this.ActionSound = CubeBlockDefinitionXElement.Element("ActionSound") != null ? CubeBlockDefinitionXElement.Element("ActionSound").Value : "";
                    this.EdgeType = CubeBlockDefinitionXElement.Element("EdgeType") != null ? CubeBlockDefinitionXElement.Element("EdgeType").Value : "";
                    this.BuildTimeSeconds = CubeBlockDefinitionXElement.Element("BuildTimeSeconds") != null ? float.Parse(CubeBlockDefinitionXElement.Element("BuildTimeSeconds").Value) : 0;
                    this.ExplosionRadius = CubeBlockDefinitionXElement.Element("ExplosionRadius") != null ? float.Parse(CubeBlockDefinitionXElement.Element("ExplosionRadius").Value) : 0;
                    this.WarheadExplosionDamage = CubeBlockDefinitionXElement.Element("WarheadExplosionDamage") != null ? int.Parse(CubeBlockDefinitionXElement.Element("WarheadExplosionDamage").Value) : 0;
                    this.EmissiveColorPresent = CubeBlockDefinitionXElement.Element("EmissiveColorPresent") != null ? CubeBlockDefinitionXElement.Element("EmissiveColorPresent").Value : "";
                    this.DestroyEffect = CubeBlockDefinitionXElement.Element("DestroyEffect") != null ? CubeBlockDefinitionXElement.Element("DestroyEffect").Value : "";
                    this.DestroySound = CubeBlockDefinitionXElement.Element("DestroySound") != null ? CubeBlockDefinitionXElement.Element("DestroySound").Value : "";
                    this.PCU = CubeBlockDefinitionXElement.Element("PCU") != null ? int.Parse(CubeBlockDefinitionXElement.Element("PCU").Value) : 0;
                    this.MirroringY = CubeBlockDefinitionXElement.Element("MirroringY") != null ? PartSwapperModelsNameResolvers.DimensionNameResolver[CubeBlockDefinitionXElement.Element("MirroringY").Value] : null;
                    this.MirroringZ = CubeBlockDefinitionXElement.Element("MirroringZ") != null ? PartSwapperModelsNameResolvers.DimensionNameResolver[CubeBlockDefinitionXElement.Element("MirroringZ").Value] : null;
                    this.IsAirTight = CubeBlockDefinitionXElement.Element("IsAirTight") != null ? bool.Parse(CubeBlockDefinitionXElement.Element("IsAirTight").Value) : null;

                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Failure in CubeBlockDefinitionSBC_CubeBlockDefinition Load! Exception is:\n{ex}\n");
                }

                try
                {
                    // Load targeting groups
                    if (this.CubeBlockDefinitionXElement.Element("TargetingGroups") != null)
                    {
                        foreach (XElement targetingGroup in CubeBlockDefinitionXElement.Element("TargetingGroups").Elements())
                        {
                            this.TargetingGroups.Add(targetingGroup.Value);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Failure in CubeBlockDefinitionSBC_CubeBlockDefinition Load! Exception is:\n{ex}\n");
                }
                return true;
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Failure in CubeBlockDefinitionSBC_CubeBlockDefinition Load! Exception is:\n{ex}\n");
                return false;
            }
        }


    }

    public class CubeBlockDefinitionSBC_CubeBlockDefinitionFile
    {
        public static string BlockSourceVanillaKey = "VANILLA";

        XElement CubeBlockDefinitionXElement;

        List<CubeBlockDefinitionSBC_CubeBlockDefinition> CubeBlocks = new List<CubeBlockDefinitionSBC_CubeBlockDefinition>();
        DefinitionSource DefinitionSource { get; set; } = DefinitionSource.Unknown;
        string DefinitionSourceString { get; set; } = "UNASSIGNED!";

        public CubeBlockDefinitionSBC_CubeBlockDefinitionFile(XElement cubeblockFileRootXElement, DefinitionSource defSource, string defSourceString = "UNASSIGNED")
        {
            if (defSource == DefinitionSource.Vanilla)
            {
                this.DefinitionSource = defSource;
                this.DefinitionSourceString = BlockSourceVanillaKey;
            }
            else
            {
                this.DefinitionSource = defSource;
                this.DefinitionSourceString = defSourceString;
            }

            if (cubeblockFileRootXElement.Element("CubeBlocks") != null)
            {
                this.CubeBlockDefinitionXElement = cubeblockFileRootXElement.Element("CubeBlocks");

                this.Load();
            }
            else
            {
                throw new Exception($"Error in CubeBlockDefinitionSBC_CubeBlockDefinitionFile constructor!\nInvalid Node:\n{cubeblockFileRootXElement.ToString()}");
            }
        }

        public LoadResult Load()
        {
            try
            {
                // Load CubeBlockDefinitions
                foreach (XElement definitionXElement in this.CubeBlockDefinitionXElement.Elements())
                {
                    this.CubeBlocks.Add(new CubeBlockDefinitionSBC_CubeBlockDefinition(definitionXElement,this.DefinitionSource,this.DefinitionSourceString));
                }
                return true;
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Failure in CubeBlockDefinitionSBC_CubeBlockDefinitionFile Load! Exception is:\n{ex}\n");
                return false;
            }
        }
    }

    public class ComponentsSBC_ComponentDefinitionFile
    {
        public static string BlockSourceVanillaKey = "VANILLA";

        XElement ComponentDefinitionXElement;

        List<ComponentsSBC_ComponentDefinition> ComponentsList = new List<ComponentsSBC_ComponentDefinition>();
        DefinitionSource DefinitionSource { get; set; } = DefinitionSource.Unknown;
        string DefinitionSourceString { get; set; } = "UNASSIGNED!";

        public ComponentsSBC_ComponentDefinitionFile(XElement componentFileRootXElement, DefinitionSource defSource, string defSourceString = "UNASSIGNED")
        {
            if (defSource == DefinitionSource.Vanilla)
            {
                this.DefinitionSource = defSource;
                this.DefinitionSourceString = BlockSourceVanillaKey;
            }
            else
            {
                this.DefinitionSource = defSource;
                this.DefinitionSourceString = defSourceString;
            }

            if (componentFileRootXElement.Element("Components") != null)
            {
                this.ComponentDefinitionXElement = componentFileRootXElement.Element("Components");

                this.Load();
            }
            else
            {
                throw new Exception($"Error in ComponentsSBC_ComponentDefinitionFile constructor!\nInvalid Node:\n{componentFileRootXElement.ToString()}");
            }
        }

        public LoadResult Load()
        {
            try
            {
                // Load CubeBlockDefinitions
                foreach (XElement ComponentdefinitionXElement in this.ComponentDefinitionXElement.Elements())
                {
                    this.ComponentsList.Add(new ComponentsSBC_ComponentDefinition(ComponentdefinitionXElement, this.DefinitionSource, this.DefinitionSourceString));
                }
                return true;
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Failure in ComponentsSBC_ComponentDefinition Load! Exception is:\n{ex}\n");
                return false;
            }
        }
    }


    public class ComponentsSBC_ComponentDefinition
    {
        // Public Properties
        public string TypeID { get => _TypeID; }
        public string SubtypeID { get => _SubtypeID; }
        public string DisplayName { get => _DisplayName; }
        public string IconPath { get => _IconPath; }
        public Vector3 Size { get => _Size; }
        public float Mass { get => _Mass; }
        public float Volume { get => _Volume; }
        public string ModelPath { get => _ModelPath; }
        public string PhysicalMaterial { get => _PhysicalMaterial; }
        public float MaxIntegrity { get => _MaxIntegrity; }
        public float DropProbability { get => _DropProbability; }
        public float Health { get => _Health; }
        public float MinimalPricePerUnit { get => _MinimalPricePerUnit; }
        public float MinimumOfferAmount { get => _MinimumOfferAmount; }
        public float MaximumOfferAmount { get => _MaximumOfferAmount; }
        public float MinimumOrderAmount { get => _MinimumOrderAmount; }
        public float MaximumOrderAmount { get => _MaximumOrderAmount; }
        public bool CanPlayerOrder { get => _CanPlayerOrder; }
        public float MinimumAcquisitionAmount { get => _MinimumAcquisitionAmount; }
        public float MaximumAcquisitionAmount { get => _MaximumAcquisitionAmount; }

        DefinitionSource DefinitionSourceEnum { get => _DefinitionSource; }
        public string DefinitionSourceString { get => _DefinitionSourceString; }
        // Internal Variables
        private XElement _ComponentDefXElement;

        private string _TypeID;
        private string _SubtypeID;
        private string _DisplayName;
        private string _IconPath;
        private Vector3 _Size;
        private float _Mass;
        private float _Volume;
        private string _ModelPath;
        private string _PhysicalMaterial;
        private float _MaxIntegrity;
        private float _DropProbability;
        private float _Health;
        private float _MinimalPricePerUnit;
        private float _MinimumOfferAmount;
        private float _MaximumOfferAmount;
        private float _MinimumOrderAmount;
        private float _MaximumOrderAmount;
        private bool _CanPlayerOrder;
        private float _MinimumAcquisitionAmount;
        private float _MaximumAcquisitionAmount;

        private DefinitionSource _DefinitionSource = DefinitionSource.Unknown;
        private string _DefinitionSourceString = "UNKNOWN";

        public ComponentsSBC_ComponentDefinition(XElement XElement, DefinitionSource definitionSource, string definitionSourceString)
        {
            this._DefinitionSource = definitionSource;
            this._DefinitionSourceString = definitionSourceString;
            this._ComponentDefXElement = XElement;
            this.Load();
        }


        public LoadResult Load()
        {
            try
            {

                this._TypeID = _ComponentDefXElement.Element("Id") != null && _ComponentDefXElement.Element("Id").Element("TypeId") != null ? _ComponentDefXElement.Element("Id").Element("TypeId").Value : "NO TypeId DETECTED!";
                this._SubtypeID = _ComponentDefXElement.Element("Id") != null && _ComponentDefXElement.Element("Id").Element("SubtypeId") != null ? _ComponentDefXElement.Element("Id").Element("SubtypeId").Value : "NO SubtypeId DETECTED!";
                this._DisplayName = _ComponentDefXElement.Element("DisplayName") != null ? _ComponentDefXElement.Element("DisplayName").Value : "NO _DisplayName DETECTED!";
                this._IconPath = _ComponentDefXElement.Element("Icon") != null ? _ComponentDefXElement.Element("Icon").Value : "NO _IconPath DETECTED!";

                this._Size = XMLTools.ParseVectorXElement(_ComponentDefXElement.Element("Size"));

                this._Mass = _ComponentDefXElement.Element("Mass") != null ? float.Parse(_ComponentDefXElement.Element("Mass").Value) : 0f;
                this._Volume = _ComponentDefXElement.Element("Volume") != null ? float.Parse(_ComponentDefXElement.Element("Volume").Value) : 0f;
                this._ModelPath = _ComponentDefXElement.Element("Model") != null ? _ComponentDefXElement.Element("Model").Value : "NO _ModelPath DETECTED!";
                this._PhysicalMaterial = _ComponentDefXElement.Element("PhysicalMaterial") != null ? _ComponentDefXElement.Element("PhysicalMaterial").Value : "NO _ModelPath DETECTED!";
                this._MaxIntegrity = _ComponentDefXElement.Element("MaxIntegrity") != null ? float.Parse(_ComponentDefXElement.Element("MaxIntegrity").Value) : 0f;
                this._DropProbability = _ComponentDefXElement.Element("DropProbability") != null ? float.Parse(_ComponentDefXElement.Element("DropProbability").Value) : 0f;
                this._Health = _ComponentDefXElement.Element("Health") != null ? float.Parse(_ComponentDefXElement.Element("Health").Value) : 0f;
                this._MinimalPricePerUnit = _ComponentDefXElement.Element("MinimalPricePerUnit") != null ? float.Parse(_ComponentDefXElement.Element("MinimalPricePerUnit").Value) : 0f;
                this._MinimumOfferAmount = _ComponentDefXElement.Element("MinimumOfferAmount") != null ? float.Parse(_ComponentDefXElement.Element("MinimumOfferAmount").Value) : 0f;
                this._MaximumOfferAmount = _ComponentDefXElement.Element("MaximumOfferAmount") != null ? float.Parse(_ComponentDefXElement.Element("MaximumOfferAmount").Value) : 0f;
                this._MinimumOrderAmount = _ComponentDefXElement.Element("MinimumOrderAmount") != null ? float.Parse(_ComponentDefXElement.Element("MinimumOrderAmount").Value) : 0f;
                this._MaximumOrderAmount = _ComponentDefXElement.Element("MaximumOrderAmount") != null ? float.Parse(_ComponentDefXElement.Element("MaximumOrderAmount").Value) : 0f;

                this._CanPlayerOrder = _ComponentDefXElement.Element("CanPlayerOrder") != null ? bool.Parse(_ComponentDefXElement.Element("CanPlayerOrder").Value) : false;

                this._MinimumAcquisitionAmount = _ComponentDefXElement.Element("MinimumAcquisitionAmount") != null ? float.Parse(_ComponentDefXElement.Element("MinimumAcquisitionAmount").Value) : 0f;
                this._MaximumAcquisitionAmount = _ComponentDefXElement.Element("MaximumAcquisitionAmount") != null ? float.Parse(_ComponentDefXElement.Element("MaximumAcquisitionAmount").Value) : 0f;

                return true;
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"ComponentsSBC_ComponentDefinition load failed due to exception:\n" + ex + "\n");
                return false;
            }
        }

        public override string ToString()
        {
            return this._DefinitionSourceString + "->Subtype:" + this.SubtypeID + "->Size:" + this.Size.ToString() + "->Mass:" + this.Mass;
        }
    }

    public class BlueprintSBC_BlueprintDefinition
    {
        GridSize gridSizeEnum;

        // Common blueprint variables
        string sbcFilePath;
        string ownerSteamID;
        string workshopID;
        string gridName;
        string displayName;
        string isRespawnGrid;

        bool NPCGridClaimElapsed;

        XElement rootXElement;
        List<XElement> CubegridsXElementList;
        XElement[] CubeGridXElementArray;

        List<BlueprintSBC_CubeGrid> CubeGrids = new List<BlueprintSBC_CubeGrid>();
        List<string> GridDLCs = new List<string>();
        Dictionary<string, List<string>> SBC_BackupFilenameToLogResolver;
        XmlDocument SBC_XMLDocument;

        BlueprintSBC_CubeGrid newCubeGridIterator;

        // Undecided / unimplemented variables
        int Points = 0; // <--- Not sure what 'points' refers to in the SBC?

        // Load success/fail boolean
        bool loadSuccessful = true;

        public BlueprintSBC_BlueprintDefinition(string sbcFilePath)
        {
            this.sbcFilePath = sbcFilePath;
            this.ownerSteamID = "";
            this.gridName = "";
            this.GridDLCs = new List<string>();
            this.SBC_BackupFilenameToLogResolver = new Dictionary<string, List<string>>();

            NPCGridClaimElapsed = true;

            this.LoadFile(sbcFilePath);
            this.LoadRootXElement(sbcFilePath);
            this.LoadCubeGrids();
            this.LoadGridDLCs();
            this.LoadGridName();
            this.LoadOwnerSteamID();
            this.LoadWorkshopId();
        }
        public string GetBlueprintName()
        {
            return this.gridName;
        }

        public ref List<BlueprintSBC_CubeGrid> GetCubegrids()
        {
            return ref this.CubeGrids;
        }

        LoadResult LoadGridDLCs()
        {
            List<XElement> DLCXElements = this.rootXElement.Element("ShipBlueprints").Element("ShipBlueprint").Elements().ToList();
            DLCXElements = DLCXElements.Where(XElement => XElement.Name.LocalName.Equals("DLC")).ToList();

            foreach (XElement dlcXElement in DLCXElements)
            {
                this.GridDLCs.Add(dlcXElement.Value);
            }

            //Pass/Fail LoadResult Condition
            if (this.GridDLCs.Count == DLCXElements.Count)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        LoadResult LoadWorkshopId()
        {
            this.workshopID = this.rootXElement.Element("ShipBlueprints").Element("ShipBlueprint").Element("WorkshopId").Value;

            //Pass/Fail LoadResult Condition
            if (!this.workshopID.Equals(""))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        LoadResult LoadGridName()
        {
            this.gridName = this.rootXElement.Element("ShipBlueprints").Element("ShipBlueprint").Element("Id").Attribute("Subtype").Value;

            //Pass/Fail LoadResult Condition
            if (!this.gridName.Equals(""))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        LoadResult LoadOwnerSteamID()
        {
            this.ownerSteamID = this.rootXElement.Element("ShipBlueprints").Element("ShipBlueprint").Element("OwnerSteamId").Value;

            //Pass/Fail LoadResult Condition
            if (!this.ownerSteamID.Equals(""))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        LoadResult LoadCubeGrids()
        {
            this.CubeGrids = new List<BlueprintSBC_CubeGrid>();

            List<XElement> cubegrids = rootXElement.Element("ShipBlueprints").Element("ShipBlueprint").Element("CubeGrids").Elements().ToList();
            this.CubeGridXElementArray = cubegrids.ToArray<XElement>();

            for (int i = 0; i < this.CubeGridXElementArray.Length; i++)
            {
                this.newCubeGridIterator = new BlueprintSBC_CubeGrid(ref CubeGridXElementArray[i]);
                this.CubeGrids.Add(newCubeGridIterator);
            }

            //Pass/Fail LoadResult Condition
            if (this.CubeGrids.Count == cubegrids.Count)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        LoadResult LoadRootXElement(string sbcFilePath)
        {
            try
            {
                this.rootXElement = XElement.Load(sbcFilePath);
                return true;
            }
            catch (Exception ex)
            {
                Trace.WriteLine($" BlueprintSBC_BlueprintDefinition failed to load root XElement! Exception:\n{ex}\n");
                return false;
            }
        }

        LoadResult LoadFile(string sbcFilePath)
        {
            XmlDocument sbcDocument = new XmlDocument();

            if (sbcFilePath == null)
            {
                return false;
            }

            if (Path.Exists(sbcFilePath))
            {
                try
                {
                    sbcDocument.Load(sbcFilePath);
                    this.SBC_XMLDocument = sbcDocument;
                    this.LoadRootXElement(sbcFilePath);
                    return true;
                }
                catch (Exception e)
                {
                    Trace.WriteLine($"Load error for modFile {sbcFilePath}\n{e}");
                    return false;
                }
            }

            Trace.WriteLine($"Load attempt for modFile path: {sbcFilePath} resulted in generic failure!");
            return false;
        }

        public bool SaveFile(string sbcFilePath)
        {
            try
            {
                this.rootXElement.Save(sbcFilePath);
                return true;
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"BlueprintSBC_BlueprintDefinition: Save failed!\n{ex}");
                return false;
            }
        }

        public bool SaveFile()
        {
            try
            {
                if(this.sbcFilePath == null || this.sbcFilePath.Equals(""))
                {
                    throw new ArgumentException($"BlueprintSBC_BlueprintDefinition ->  SaveFile: Invalid path!\nInvalid Path is:{(this.sbcFilePath == null ? "NULL": this.sbcFilePath)}");
                }

                this.rootXElement.Save(this.sbcFilePath);
                return true;
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"BlueprintSBC_BlueprintDefinition: Save failed!\n{ex}");
                return false;
            }
        }

        public bool ReloadFile()
        {
            try
            {
                this.LoadFile(sbcFilePath);
                this.LoadRootXElement(sbcFilePath);
                this.LoadCubeGrids();
                this.LoadGridDLCs();
                this.LoadGridName();
                this.LoadOwnerSteamID();
                this.LoadWorkshopId();
                return true;
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"BlueprintSBC_BlueprintDefinition: ReloadFile failed!\n{ex}");
                return false;
            }

        }
    }

    public class BlueprintSBC_CubeGrid
    {
        private XElement _CubegridXElement;

        private string _DisplayName;
        private Vector3 _Position;
        private Vector3 _Forward;
        private Vector3 _Up;
        private Vector3 _Orientation;

        private List<BlueprintSBC_CubeBlock> _Blocks;
        private BlockGroupsDictionary _BlockGroups;

        private XElement _CubeBlocksXElement;
        private XElement[] _CubeBlocksArr;

        // Non-modeling variables

        //Statistics-related variables
        private HashSet<BlueprintSBC_CubeBlock> _UniqueBlocksSet = new HashSet<BlueprintSBC_CubeBlock>(new CubeblockSubtypeNameEqualityComparer());
        private Dictionary<BlueprintSBC_CubeBlock, float> _BlockCounterDict = new Dictionary<BlueprintSBC_CubeBlock, float>(new CubeblockSubtypeNameEqualityComparer());

        private bool _LoadSuccessful = false;

        public class CubeblockSubtypeNameEqualityComparer : IEqualityComparer<BlueprintSBC_CubeBlock>
        {
            bool IEqualityComparer<BlueprintSBC_CubeBlock>.Equals(BlueprintSBC_CubeBlock? x, BlueprintSBC_CubeBlock? y)
            {
                if (x == null || y == null)
                {
                    throw new ArgumentException($"Compared a null cubeblock! x == null:{x == null} y == null:{y == null}");
                }
                if (x.GetSubtypeName().Equals(y.GetSubtypeName()))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            int IEqualityComparer<BlueprintSBC_CubeBlock>.GetHashCode(BlueprintSBC_CubeBlock obj)
            {
                return obj.GetSubtypeName().GetHashCode();
            }
        }

        public BlueprintSBC_CubeGrid(ref XElement cubegridXElement)
        {
            this._CubegridXElement = cubegridXElement;
            this.LoadBlocks();
            this.LoadBlockGroups();
            this.LoadDisplayName();
            this.LoadForwardVector();
            this.LoadUpVector();
            this.LoadOrientationVector();
            this.LoadPositionVector();
        }

        public List<BlueprintSBC_CubeBlock> GetCubeBlocks()
        {
            return this._Blocks;
        }

        public string GetDisplayName()
        {
            return this._DisplayName;
        }

        public ref BlockGroupsDictionary GetBlockGroups()
        {
            return ref this._BlockGroups;
        }

        public HashSet<BlueprintSBC_CubeBlock> GetUniqueBlocks()
        {
            return this._UniqueBlocksSet;
        }

        public Dictionary<BlueprintSBC_CubeBlock, float> GetBlockCounterDict()
        {
            return this._BlockCounterDict;
        }

        public Queue<string> PartSwapViaSubtype(string originalSubtypeName, string replacementSubtypeName)
        {
            Queue<string> transactionLog = new Queue<string>();

            foreach(BlueprintSBC_CubeBlock block in this._Blocks)
            {
                if (block.GetSubtypeName().Equals(originalSubtypeName))
                {
                    block.SetSubtypeName(replacementSubtypeName);

                    transactionLog.Enqueue($"Replacing {originalSubtypeName} at position {block.GetMinVector().ToString()} with subtypeID {replacementSubtypeName} at time {DateTime.Now.ToString()}");
                }
            }

            return transactionLog;
        }

        LoadResult LoadForwardVector()
        {
            this._Forward = XMLTools.ParseVectorXElement(this._CubegridXElement.Element("PositionAndOrientation").Element("Forward"));

            //Pass/Fail LoadResult Condition
            if ((this._Forward != Vector3.Zero))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        LoadResult LoadUpVector()
        {
            this._Up = XMLTools.ParseVectorXElement(this._CubegridXElement.Element("PositionAndOrientation").Element("Up"));

            //Pass/Fail LoadResult Condition
            if ((this._Up != Vector3.Zero))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        LoadResult LoadOrientationVector()
        {
            this._Orientation = XMLTools.ParseVectorXElement(this._CubegridXElement.Element("PositionAndOrientation").Element("Orientation"));

            //Pass/Fail LoadResult Condition
            if ((this._Orientation != Vector3.Zero))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        LoadResult LoadPositionVector()
        {
            this._Position = XMLTools.ParseVectorXElement(this._CubegridXElement.Element("PositionAndOrientation").Element("Position"));

            //Pass/Fail LoadResult Condition
            if ((this._Position != Vector3.Zero))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        LoadResult LoadDisplayName()
        {
            this._DisplayName = this._CubegridXElement.Element("DisplayName").Value;

            //Pass/Fail LoadResult Condition
            if (!this._DisplayName.Equals(""))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        LoadResult LoadBlocks()
        {
            try
            {
                this._Blocks = new List<BlueprintSBC_CubeBlock>();

                this._CubeBlocksXElement = this._CubegridXElement.Element("CubeBlocks");
                this._CubeBlocksArr = _CubeBlocksXElement.Elements().ToArray();

                for (int i = 0; i < _CubeBlocksArr.Count(); i++)
                {
                    BlueprintSBC_CubeBlock newCubeBlock = new BlueprintSBC_CubeBlock(ref _CubeBlocksArr[i]);
                    this._Blocks.Add(newCubeBlock);

                    // Go ahead and simply add newCubeBlock to the unique block sets - we should have already initialized the name comparer to handle the uniqueness requirements
                    this._UniqueBlocksSet.Add(newCubeBlock);

                    if (this._BlockCounterDict.ContainsKey(newCubeBlock))
                    {
                        this._BlockCounterDict[newCubeBlock] = this._BlockCounterDict[newCubeBlock] + 1;
                    } else
                    {
                        this._BlockCounterDict.Add(newCubeBlock,1) ;
                    }

                }
                return true;
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"BlueprintSBC_CubeGrid failed to LoadBlocks! Exception:\n{ex}\n");
                return false;
            }

        }

        LoadResult LoadBlockGroups()
        {
            this._BlockGroups = new BlockGroupsDictionary();

            string nameIterator;
            Vector3 vectorIterator;

            // Check if blockgroups exist
            if (this._CubegridXElement.Element("_BlockGroups") == null)
            {
                Trace.WriteLine("BlueprintSBC_CubeGrid LoadBlockGroups did not find any blockgroups! Assuming this is correct and returning true!");
                return true;
            }

            foreach (XElement blockGroup in this._CubegridXElement.Element("BlockGroups").Elements())
            {
                nameIterator = blockGroup.Element("Name").Value;

                if (!this._BlockGroups.ContainsKey(nameIterator))
                {
                    this._BlockGroups[nameIterator] = new List<Vector3>();
                }


                foreach (XElement block in blockGroup.Element("Blocks").Elements())
                {
                    vectorIterator = XMLTools.ParseVectorXElement(block);
                    this._BlockGroups[nameIterator].Add(vectorIterator);
                }
            }

            // Pass/Fail condition
            if (this._BlockGroups.Keys.Count == this._CubegridXElement.Element("BlockGroups").Elements().Count())
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    public class BlueprintSBC_CubeBlock
    {
        // Public Properties
        public string CubeblockType {  get => _CubeblockType; }
        public string SubtypeName { get => _SubtypeName; }
        public string SkinSubtypeID { get => _SkinSubtypeID; }
        public string BuiltBy { get => _BuiltBy; }
        public Vector3 Min { get => _Min; }
        public Vector3 ColorMarkHSVVector { get => _ColorMarkHSVVector; }
        public Color ColorMarkHSV { get => _ColorMarkHSV; }
        public string CustomName { get => _CustomName; }

        // Private variables
        private XElement _CubeblockXElement;

        private string _CubeblockType;
        private string _SubtypeName;
        private string _SkinSubtypeID;
        private string _BuiltBy;

        private Vector3 _Min;
        private Vector3 _ColorMarkHSVVector;

        private Color _ColorMarkHSV;

        private string _CustomName;

        // For use in partswapper/trees/static structures. Reference param.
        public BlueprintSBC_CubeBlock(ref XElement cubeblockXElement)
        {
            this._CubeblockXElement = cubeblockXElement;
            this.Load();
        }

        public void Delete()
        {
            this._CubeblockXElement.Remove();
        }

        [Obsolete("Use SubtypeName property!")]
        public string GetSubtypeName()
        {
            return this._SubtypeName;
        }

        public void SetSubtypeName(string value)
        {
            this._SubtypeName = value;
            this._CubeblockXElement.Element("SubtypeName").Value = this._SubtypeName;
        }

        [Obsolete("Use CustomName property!")]
        public string GetCustomName()
        {
            return this._CustomName;
        }

        [Obsolete("Use Min property!")]
        public Vector3 GetMinVector()
        {
            return this._Min;
        }

        [Obsolete("Use ColorMarkHSVVector property!")]
        public Vector3 GetColorHSVVector()
        {
            return this._ColorMarkHSVVector;
        }

        LoadResult Load()
        {
            try
            {
                this._CubeblockType = this._CubeblockXElement.FirstAttribute.Value;

                if (this._CubeblockXElement.Element("SubtypeName") != null)
                {
                    this._SubtypeName = this._CubeblockXElement.Element("SubtypeName").Value;

                    if (String.IsNullOrEmpty(this._SubtypeName)){

                        if (this._CubeblockType != null)
                        {
                            this._SubtypeName = this._CubeblockType;
                        } else
                        {
                            this._SubtypeName = "//GRID SBC PROVIDED NO SUBTYPE NAME!//";
                        }

                    }
                }
                else
                {
                    Trace.WriteLine($"BlueprintSBC_CubeBlock does not have a subtype name! Printing BlueprintSBC_CubeBlock:\n-----\n{this._CubeblockXElement.ToString()}\n-----\n");
                }

                if (this._CubeblockXElement.Element("Min") != null)
                {
                    this._Min = XMLTools.ParseVectorXElement(this._CubeblockXElement.Element("Min"));
                }
                else
                {
                    Trace.WriteLine($"BlueprintSBC_CubeBlock does not have a min node! Printing BlueprintSBC_CubeBlock:\n-----\n{this._CubeblockXElement.ToString()}\n-----\n");
                }

                if (this._CubeblockXElement.Element("ColorMaskHSV") != null)
                {
                    this._ColorMarkHSVVector = XMLTools.ParseVectorXElement(this._CubeblockXElement.Element("ColorMaskHSV"));
                }
                else
                {
                    Trace.WriteLine($"BlueprintSBC_CubeBlock does not have a ColorMaskHSV node! Printing BlueprintSBC_CubeBlock:\n-----\n{this._CubeblockXElement.ToString()}\n-----\n");
                }

                if (this._CubeblockXElement.Element("SkinSubtypeId") != null)
                {
                    this._SkinSubtypeID = this._CubeblockXElement.Element("SkinSubtypeId").Value;
                }
                else
                {
                    Trace.WriteLine($"BlueprintSBC_CubeBlock does not have a SkinSubtypeId node! Printing BlueprintSBC_CubeBlock:\n-----\n{this._CubeblockXElement.ToString()}\n-----\n");
                }

                if (this._CubeblockXElement.Element("BuiltBy") != null)
                {
                    this._BuiltBy = this._CubeblockXElement.Element("BuiltBy").Value;
                }
                else
                {
                    Trace.WriteLine($"BlueprintSBC_CubeBlock does not have a BuiltBy node! Printing BlueprintSBC_CubeBlock:\n-----\n{this._CubeblockXElement.ToString()}\n-----\n");
                }

                if (this._CubeblockXElement.Element("CustomName") != null)
                {
                    this._CustomName = this._CubeblockXElement.Element("CustomName").Value;
                }
                else
                {
                    Trace.WriteLine($"BlueprintSBC_CubeBlock does not have a BuiltBy node! Printing BlueprintSBC_CubeBlock:\n-----\n{this._CubeblockXElement.ToString()}\n-----\n");
                }

                return true;
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Cubeblock load failed due to exception:\n" + ex + "\n");
                return false;
            }
        }

        public override string ToString()
        {
            return this._SubtypeName.ToString();
        }

        /*
        public override bool Equals(object? obj)
        {
            if(obj == null)
            {
                return false;
            }

            if(!(obj is BlueprintSBC_CubeBlock))
            {
                return false;
            }

            BlueprintSBC_CubeBlock SBCCubeBlock = obj as BlueprintSBC_CubeBlock;

            if (SBCCubeBlock == null)
            {
                return false;
            } 

            if(SBCCubeBlock.SubtypeName == this.SubtypeName)
            {
                return true;
            } else
            {
                return false;
            }
        }
                */

    }

    public class BluePrintSBC_Component
    {
        // Public properties
        public string Subtype { get => this._Subtype; }
        public float Count { get => this._Count; }

        // Private variables
        private XElement _CubeblockXElement;

        private string _Subtype;
        private float _Count;

        public BluePrintSBC_Component(XElement cubeblockXElement)
        {
            this._CubeblockXElement = cubeblockXElement;
            this.Load();
        }

        public BluePrintSBC_Component(string Subtype, int count)
        {
            this._Subtype = Subtype;
            this._Count = count;
        }

        public LoadResult Load()
        {
            try
            {
                this._Subtype = _CubeblockXElement.Attribute("Subtype") != null ? _CubeblockXElement.Attribute("Subtype").Value : "";
                this._Count = _CubeblockXElement.Attribute("Count") != null ? float.Parse(_CubeblockXElement.Attribute("Count").Value) : 0;
                return true;
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"BluePrintSBC_Component Load Failed! Exception is:\n{ex.ToString()}\n");
                return false;
            }

        }

        public float GetCount()
        {
            return this._Count;
        }
        public string GetSubtype()
        {
            return this._Subtype;
        }

        public override string ToString()
        {
            return $"{this._Subtype}";
        }
    }

    public class BlueprintSBC_ConveyorLine
    {
        Vector3 startPosition;
        Vector3 endPosition;
        string StartDirection;
        string EndDirection;
        string ConveyorLineType;
        Dictionary<string, int> Sections;

        BlueprintSBC_ConveyorLine(Vector3 startPosition, Vector3 endPosition, string startDirection, string endDirection, string conveyorLineType, Dictionary<string, int> sections)
        {
            this.startPosition = startPosition;
            this.endPosition = endPosition;
            StartDirection = startDirection;
            EndDirection = endDirection;
            ConveyorLineType = conveyorLineType;
            Sections = sections;
        }
    }
    public class BlueprintSBC_BlockGroup
    {
        string GroupName;
        List<Vector3> Blocks;

        BlueprintSBC_BlockGroup(string groupName, List<Vector3> Blocks)
        {
            this.GroupName = groupName;
            this.Blocks = Blocks;
        }

        ReplacementResult Rename(string newName)
        {
            string oldName = this.GroupName;

            this.GroupName = newName;

            return new ReplacementResult(oldName, newName);
        }
    }

    class BlueprintSBC_OxygenRooms
    {
        // Unimplemented / uncertain how to implement
    }

}
