using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using System.Xml;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using OpenTK.Graphics.OpenGL;
using PartSwapperXMLSE;
using ScottPlot;
using SkiaSharp;
using SkiaSharp.Views.WPF;
using static PartSwapperGUI.PartSwapper2024.TransactionLog;

namespace PartSwapperGUI.PartSwapper2024
{
    public interface PartswapOperation
    {
        public bool DebugMode { get; set; }
        public string OperationName { get; }
        public Dictionary<string, object> OperationParameters { get; set; }
        public Dictionary<string, Type> RecognizedParameters { get; }
        public SKRectI UIRect { get; set; }
        public SKPaint UIPaint { get; set; }
        public SKColor UIPaintColor { get; set; }

        public TransactionLog TransactionLog { get; set; }

        public TransactionLog PerformOperation(ref Tuple<CubeGridRenderCellGrid, BlueprintCellGridManager.GridGeometry, GridCursor, SKElement> currentCubegridEntry);
    }

    public class PartSwapper2024
    {
        public ref TransactionLog MasterLogRef { get => ref _MasterTransactionLog; }

        private bool _DebugMode = false;

        private PartSwapper2024 _PS2024MasterRef;

        private BlueprintSBC_BlueprintDefinition? _BlueprintDefinitionMasterInstance;
        private GridRenderer2024? _GridRenderer2024;
        private SKElement _SkiaElement;
        private Window _Window;

        private string _BPSBCFilePath = "";

        private TransactionLog _MasterTransactionLog;

        private Dictionary<string, PartswapOperation> _Operations = new Dictionary<string, PartswapOperation>();
        private Dictionary<string, Tuple<SKRect, SKColor, SKPaint>> _ColorLegendDict = new Dictionary<string, Tuple<SKRect, SKColor, SKPaint>>();

        private DataFolder_Model _SteamWorkshopModDataFolder;
        private DataFolder_Model _VanillaDataFolder;

        public PartSwapper2024(string sbcPath, ref Window window, ref SKElement WPFskiaElement, string SEBaseGameContentFolderPath, string SEWorkshopFolderPath, bool debugMode)
        {
            this._PS2024MasterRef = this;
            this._MasterTransactionLog = new TransactionLog("PS2024Master");
            this._BPSBCFilePath = sbcPath;
            this._SkiaElement = WPFskiaElement;
            this._Window = window;
            this._DebugMode = debugMode;

            // If we are given an empty path: Do not instantiate a GridRenderer.
            if (sbcPath.Equals(""))
            {
                throw new ArgumentException("No SBC Path provided to Partswapper!");
            }

            this._BlueprintDefinitionMasterInstance = new BlueprintSBC_BlueprintDefinition(sbcPath);
            this._GridRenderer2024 = new GridRenderer2024(ref this._PS2024MasterRef, ref _BlueprintDefinitionMasterInstance,ref this._SkiaElement, ref _Window);
            this._SkiaElement = this._GridRenderer2024.GetCurrentSKElement();

            this._Operations.Add("AutoArmor", new PartswapOperations.AutoArmor());
            this._Operations.Add("AutoTech", new PartswapOperations.AutoTech());
            this._Operations.Add("RemoveTool", new PartswapOperations.RemoveTool());
            this._Operations.Add("PartSwap", new PartswapOperations.PartSwap());

            this._VanillaDataFolder = new DataFolder_Model(SEBaseGameContentFolderPath);
            this._SteamWorkshopModDataFolder = new DataFolder_Model(SEWorkshopFolderPath);
        }

        public PartSwapper2024(string sbcPath, ref Window window, ref TabControl TabControlSkiaContainer, string SEBaseGameContentFolderPath, string SEWorkshopFolderPath, bool debugMode)
        {
            this._DebugMode = debugMode;
            this._PS2024MasterRef = this;

            // If we are given an empty path: Do not instantiate a GridRenderer.
            if (sbcPath.Equals(""))
            {
                throw new ArgumentException("No SBC Path provided to Partswapper!");
            }

            this._MasterTransactionLog = new TransactionLog("PS2024Master");
            this._BPSBCFilePath = sbcPath;
            this._Window = window;

            this._BlueprintDefinitionMasterInstance = new BlueprintSBC_BlueprintDefinition(sbcPath);
            this._GridRenderer2024 = new GridRenderer2024(ref this._PS2024MasterRef, ref _BlueprintDefinitionMasterInstance, ref this._SkiaElement, ref _Window);
            this._SkiaElement = this._GridRenderer2024.GetCurrentSKElement();

            this.PopulateTabControl(ref TabControlSkiaContainer);

            this._Operations.Add("AutoArmor", new PartswapOperations.AutoArmor());
            this._Operations.Add("AutoTech", new PartswapOperations.AutoTech());
            this._Operations.Add("RemoveTool", new PartswapOperations.RemoveTool());
            this._Operations.Add("PartSwap", new PartswapOperations.PartSwap());

            this._VanillaDataFolder = new DataFolder_Model(SEBaseGameContentFolderPath);
            this._SteamWorkshopModDataFolder = new DataFolder_Model(SEWorkshopFolderPath);
        }

        public TransactionLog PerformOperation(string operation, Dictionary<string, object> parameters)
        {
            try
            {
                // If the operation exists within our collection of operations: Perform it with provided parameters.
                if (this._Operations.ContainsKey(operation))
                {
                    this._Operations[operation].OperationParameters = parameters;
                    return this._Operations[operation].PerformOperation(ref this._GridRenderer2024.GetCurrentRenderEntry());
                }
                else
                {
                    throw new ArgumentException($"PartSwapper.PerformOperation: No operation {operation} defined!");
                }
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"PartSwapper.PerformOperation: Generic exception while attempting operation {operation}!\nException:\n{ex}");
            }
        }

        public void SubmitWindowSizeUpdate(System.Windows.Size windowSize)
        {
            this._GridRenderer2024.SubmitWindowSizeUpdate(windowSize);
        }

        public bool PopulateTabControl(ref TabControl tabControl)
        {
            try
            {
                if (this._DebugMode)
                {
                    Trace.WriteLine($"PartSwapper2024: Entering PopulateTabControl...");
                }

                this._GridRenderer2024.PopulateTabControl(ref tabControl);

                if (this._DebugMode)
                {
                    Trace.WriteLine($"PartSwapper2024: Assigning this._SkiaElement to SKElement with hash: {this._SkiaElement.GetHashCode()}");
                    Trace.WriteLine($"PartSwapper2024: Exiting PopulateTabControl...");
                }
                return true;
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"PopulateTabControl threw thew following error:\n-----------\n{ex}\n");
                return false;
            }
        }

        public bool DeleteSBC5File()
        {
            string sbc5Path = "";

            try
            {
                // Delete the associated sbcB5 file
                sbc5Path = Path.Combine(Path.GetDirectoryName(this._BPSBCFilePath), Path.GetFileNameWithoutExtension(this._BPSBCFilePath) + ".sbcB5");
                File.Delete(sbc5Path);
                return true;
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Failed to delete SBC5 file!\nYou're gonna need to do that in order to see your grid changes...\nError was:\n{ex}");
            }
            return false;

        }

        public void BackupShipXML()
        {
            XmlDocument xmlDoc = new XmlDocument();

            XmlWriterSettings xwrSettings = new XmlWriterSettings();

            bool debug = false;

            string currentDirectory = Directory.GetCurrentDirectory();
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmm");
            string BackupFilename = _BPSBCFilePath + timestamp + "pps_bp_bak.sbc";

            // Long filepath not necessary. We use relative.
            //string FilePath = Path.Combine(currentDirectory, BackupFilename);


            if (debug)
            {
                Console.WriteLine($"DEBUG: BackupShipXML _BPSBCFilePath input is: {_BPSBCFilePath}");
            }


            using (XmlReader xRead = XmlReader.Create(_BPSBCFilePath))
            {
                xmlDoc.Load(xRead);
            }

            xwrSettings.IndentChars = "\t";
            xwrSettings.NewLineHandling = NewLineHandling.Entitize;
            xwrSettings.Indent = true;
            xwrSettings.NewLineChars = "\n";

            timestamp = DateTime.Now.ToString("yyyyMMdd_HHmm");
            BackupFilename = _BPSBCFilePath + timestamp + "pps_bp_bak.sbc";

            using (XmlWriter xWrite = XmlWriter.Create(BackupFilename, xwrSettings))
            {
                xmlDoc.Save(xWrite);
            }
        }

        public ref SKElement GetCurrentSKElement()
        {
            return ref this._SkiaElement;
        }

        public ref BlueprintSBC_BlueprintDefinition GetBlueprintDefinition()
        {
            return ref this._BlueprintDefinitionMasterInstance;
        }

        public ref GridRenderer2024 GetGridRendererRef()
        {
            return ref this._GridRenderer2024;
        }

        public ref GridCursor GetGridCursorRef()
        {
            return ref this._GridRenderer2024.GetGridCursor();
        }

        public DataFolder_Model GetVanillaDataFolder()
        {
            return this._VanillaDataFolder;
        }

        public DataFolder_Model GetWorkshopModFolder()
        {
            return this._SteamWorkshopModDataFolder;
        }

        public Dictionary<DefinitionSource, List<ComponentsSBC_ComponentDefinition>> SearchComponentsDefinitions(BluePrintSBC_Component componentSBCDefinition)
        {
            string subtype = componentSBCDefinition.Subtype;

            List<ComponentsSBC_ComponentDefinition>? vanillaBlocksQuery = new List<ComponentsSBC_ComponentDefinition>();
            List<ComponentsSBC_ComponentDefinition>? modBlocksQuery = new List<ComponentsSBC_ComponentDefinition>();

            Dictionary<DefinitionSource, List<ComponentsSBC_ComponentDefinition>> QueryDict = new Dictionary<DefinitionSource, List<ComponentsSBC_ComponentDefinition>>();

            if (this._VanillaDataFolder.GetComponentsDefDict().Any(kvp => kvp.Value.Any(componentDef => componentDef.SubtypeID.Equals(subtype))))
            {

                foreach (KeyValuePair<string, List<ComponentsSBC_ComponentDefinition>> kvp in this._VanillaDataFolder.GetComponentsDefDict())
                {
                    foreach (ComponentsSBC_ComponentDefinition blockDef in kvp.Value)
                    {
                        if (blockDef.SubtypeID.Equals(subtype))
                        {
                            vanillaBlocksQuery.Add(blockDef);
                        }
                    }
                }
            }

            if (this._SteamWorkshopModDataFolder.GetComponentsDefDict().Any(kvp => kvp.Value.Any(componentDef => componentDef.SubtypeID.Equals(subtype))))
            {

                foreach (KeyValuePair<string, List<ComponentsSBC_ComponentDefinition>> kvp in this._SteamWorkshopModDataFolder.GetComponentsDefDict())
                {
                    foreach (ComponentsSBC_ComponentDefinition blockDef in kvp.Value)
                    {
                        if (blockDef.SubtypeID.Equals(subtype))
                        {
                            modBlocksQuery.Add(blockDef);
                        }
                    }
                }
            }

            QueryDict.Add(DefinitionSource.Vanilla, vanillaBlocksQuery);
            QueryDict.Add(DefinitionSource.Mod, modBlocksQuery);

            return QueryDict;
        }


        public Dictionary<DefinitionSource, List<CubeBlockDefinitionSBC_CubeBlockDefinition>> SearchCubeBlockDefinitions(BlueprintSBC_CubeBlock cubeBlock)
        {
            string subtype = cubeBlock.GetSubtypeName();

            List<CubeBlockDefinitionSBC_CubeBlockDefinition>? vanillaBlocksQuery = new List<CubeBlockDefinitionSBC_CubeBlockDefinition>();
            List<CubeBlockDefinitionSBC_CubeBlockDefinition>? modBlocksQuery = new List<CubeBlockDefinitionSBC_CubeBlockDefinition>();

            Dictionary<DefinitionSource, List<CubeBlockDefinitionSBC_CubeBlockDefinition>> QueryDict = new Dictionary<DefinitionSource, List<CubeBlockDefinitionSBC_CubeBlockDefinition>>();

            if (this._VanillaDataFolder.GetCubeBlocksDefDict().Any(kvp => kvp.Value.Any(blockDef => blockDef.GetSubTypeID().Equals(subtype))))
            {

                foreach (KeyValuePair<string, List<CubeBlockDefinitionSBC_CubeBlockDefinition>> kvp in this._VanillaDataFolder.GetCubeBlocksDefDict())
                {
                    foreach (CubeBlockDefinitionSBC_CubeBlockDefinition blockDef in kvp.Value)
                    {
                        if (blockDef.GetSubTypeID().Equals(subtype))
                        {
                            vanillaBlocksQuery.Add(blockDef);
                        }
                    }
                }
            }

            if (this._SteamWorkshopModDataFolder.GetCubeBlocksDefDict().Any(kvp => kvp.Value.Any(blockDef => blockDef.GetSubTypeID().Equals(subtype))))
            {

                foreach (KeyValuePair<string, List<CubeBlockDefinitionSBC_CubeBlockDefinition>> kvp in this._SteamWorkshopModDataFolder.GetCubeBlocksDefDict())
                {
                    foreach (CubeBlockDefinitionSBC_CubeBlockDefinition blockDef in kvp.Value)
                    {
                        if (blockDef.GetSubTypeID().Equals(subtype))
                        {
                            modBlocksQuery.Add(blockDef);
                        }
                    }
                }
            }

            QueryDict.Add(DefinitionSource.Vanilla, vanillaBlocksQuery);
            QueryDict.Add(DefinitionSource.Mod, modBlocksQuery);

            return QueryDict;
        }

        public Dictionary<DefinitionSource, List<CubeBlockDefinitionSBC_CubeBlockDefinition>> SearchCubeBlockDefinitions(string subtypeName)
        {
            string subtype = subtypeName;

            List<CubeBlockDefinitionSBC_CubeBlockDefinition>? vanillaBlocksQuery = new List<CubeBlockDefinitionSBC_CubeBlockDefinition>();
            List<CubeBlockDefinitionSBC_CubeBlockDefinition>? modBlocksQuery = new List<CubeBlockDefinitionSBC_CubeBlockDefinition>();

            Dictionary<DefinitionSource, List<CubeBlockDefinitionSBC_CubeBlockDefinition>> QueryDict = new Dictionary<DefinitionSource, List<CubeBlockDefinitionSBC_CubeBlockDefinition>>();

            if (this._VanillaDataFolder.GetCubeBlocksDefDict().Any(kvp => kvp.Value.Any(blockDef => blockDef.GetSubTypeID().Equals(subtype))))
            {

                foreach (KeyValuePair<string, List<CubeBlockDefinitionSBC_CubeBlockDefinition>> kvp in this._VanillaDataFolder.GetCubeBlocksDefDict())
                {
                    foreach (CubeBlockDefinitionSBC_CubeBlockDefinition blockDef in kvp.Value)
                    {
                        if (blockDef.GetSubTypeID().Equals(subtype))
                        {
                            vanillaBlocksQuery.Add(blockDef);
                        }
                    }
                }
            }

            if (this._SteamWorkshopModDataFolder.GetCubeBlocksDefDict().Any(kvp => kvp.Value.Any(blockDef => blockDef.GetSubTypeID().Equals(subtype))))
            {

                foreach (KeyValuePair<string, List<CubeBlockDefinitionSBC_CubeBlockDefinition>> kvp in this._SteamWorkshopModDataFolder.GetCubeBlocksDefDict())
                {
                    foreach (CubeBlockDefinitionSBC_CubeBlockDefinition blockDef in kvp.Value)
                    {
                        if (blockDef.GetSubTypeID().Equals(subtype))
                        {
                            modBlocksQuery.Add(blockDef);
                        }
                    }
                }
            }

            QueryDict.Add(DefinitionSource.Vanilla, vanillaBlocksQuery);
            QueryDict.Add(DefinitionSource.Mod, modBlocksQuery);

            return QueryDict;
        }
        public bool SetDebugMode(bool debugValue)
        {
            this._DebugMode = debugValue;
            this._GridRenderer2024.SetDebugMode(debugValue);
            return debugValue;
        }
    }
    public static class PartswapOperations
    {
        // Idea: Make each "PerformOperation" function return the current transactionLog (which the function itself should have modified).
        public class AutoTech : PartswapOperation
        {
            private bool _DebugMode = false;
            private string _OperationName => "AutoTech";

            private Dictionary<string, object> _OperationParameters = new Dictionary<string, object>();
            private Dictionary<string, Type> _RecognizedParameters = new Dictionary<string, Type> { { "categoryName", typeof(string) }, { "desiredTechlevel", typeof(string) } };
            private SKRectI _UIRect = new SKRectI();
            private SKPaint _UIPaint = new SKPaint();
            private SKColor _UIPaintColor = new SKColor();
            private TransactionLog _TransactionLog;
            private TransactionLogEntry _TransactionLogEntry;

            string PartswapOperation.OperationName => _OperationName;
            Dictionary<string, object> PartswapOperation.OperationParameters { get => _OperationParameters; set => this._OperationParameters = value; }
            Dictionary<string, Type> PartswapOperation.RecognizedParameters => _RecognizedParameters;
            SKRectI PartswapOperation.UIRect { get => _UIRect; set => _UIRect = value; }
            SKPaint PartswapOperation.UIPaint { get => _UIPaint; set => _UIPaint = value; }
            SKColor PartswapOperation.UIPaintColor { get => _UIPaintColor; set => _UIPaintColor = value; }
            TransactionLog PartswapOperation.TransactionLog { get => _TransactionLog; set => _TransactionLog = value; }
            bool PartswapOperation.DebugMode { get => _DebugMode; set => _DebugMode = value; }

            public AutoTech()
            {
                this._TransactionLog = new TransactionLog(_OperationName);
            }

            TransactionLog PartswapOperation.PerformOperation(ref Tuple<CubeGridRenderCellGrid, BlueprintCellGridManager.GridGeometry, GridCursor, SKElement> currentCubegridEntry)
            {
                // TODO: Autotech needs fixed!
                // Block-iterating method is wrong, and needs modeled from other operations
                // Also: Has no change indicators being drawn on the UI.
                if (_OperationParameters["categoryName"] == null ||
                   _OperationParameters["desiredTechlevel"] == null)
                {
                    throw new ArgumentException("PerformOperation needs to have parameters {categoryName,desiredTechlevel} set!");
                }

                string categoryName = _OperationParameters["categoryName"] as string;
                int desiredTechlevel = (int)_OperationParameters["desiredTechlevel"];
                string partNameIterator = "";

                if (desiredTechlevel < 0 || desiredTechlevel > 3)
                {
                    throw new Exception("AutoTech: Invalid desiredTechLevel!");
                }

                foreach (BlueprintCell shipPart in currentCubegridEntry.Item1.GetBlueprintCell3DArrayRef())
                {
                    partNameIterator = shipPart.GetCubeblockDefinition().GetSubtypeName();

                    // Check if we're using a non-subtype based name, such as MyObjectBuilder_...
                    if (partNameIterator.StartsWith("MyObjectBuilder_"))
                    {
                        Trace.WriteLine("Found non-subtype based name. Ignoring!");
                        continue;
                    }

                    // Otherwise - check if the shipPart name equals the category name.
                    if (partNameIterator.Equals(categoryName))
                    {
                        bool isDesiredTechLevel = false;
                        bool isCurrentlyTeched = false;

                        string desiredTechLevelString = "";
                        string blockname;

                        int currTechLevel = -1;
                        int offset = categoryName.Length - 2;

                        if (partNameIterator.EndsWith("2x") || partNameIterator.EndsWith("4x") || partNameIterator.EndsWith("8x"))
                        {
                            isCurrentlyTeched = true;

                            if (partNameIterator.EndsWith("2x"))
                            {
                                currTechLevel = 1;
                                isCurrentlyTeched = true;
                            }

                            if (partNameIterator.EndsWith("4x"))
                            {
                                currTechLevel = 2;
                                isCurrentlyTeched = true;
                            }

                            if (partNameIterator.EndsWith("8x"))
                            {
                                currTechLevel = 3;
                                isCurrentlyTeched = true;
                            }
                        }
                        else
                        {
                            // if we can't find any existing techlevel string - assume not-teched!
                            isCurrentlyTeched = false;
                            currTechLevel = 0;
                        }

                        // generate tech level strings
                        switch (desiredTechlevel)
                        {
                            case 0:
                                if (isCurrentlyTeched)
                                {
                                    desiredTechLevelString = categoryName.Remove(offset);
                                }
                                else
                                {
                                    desiredTechLevelString = categoryName;
                                }
                                break;
                            case 1:
                                if (isCurrentlyTeched)
                                {
                                    desiredTechLevelString = categoryName.Remove(offset) + "2x";
                                }
                                else
                                {
                                    desiredTechLevelString = categoryName + "2x";
                                }
                                break;
                            case 2:
                                if (isCurrentlyTeched)
                                {
                                    desiredTechLevelString = categoryName.Remove(offset) + "4x";
                                }
                                else
                                {
                                    desiredTechLevelString = categoryName + "4x";
                                }
                                break;
                            case 3:
                                if (isCurrentlyTeched)
                                {
                                    desiredTechLevelString = categoryName.Remove(offset) + "8x";
                                }
                                else
                                {
                                    desiredTechLevelString = categoryName + "8x";
                                }
                                break;
                            default:
                                throw new Exception("AutoTech: invalid desiredTechLevel in switch statement!");
                        }

                        blockname = partNameIterator;


                        // If we determine the ship needs to change tech level...
                        if (currTechLevel != desiredTechlevel)
                        {
                            switch (desiredTechlevel)
                            {
                                case 0:
                                    this._TransactionLogEntry = new TransactionLogEntry(LogEntryTypes.LogEntryMessage, $"Changing {blockname} to {desiredTechLevelString}");
                                    this._TransactionLog.AddLogEntry(_TransactionLogEntry);
                                    blockname = desiredTechLevelString;
                                    break;
                                case 1:
                                    this._TransactionLogEntry = new TransactionLogEntry(LogEntryTypes.LogEntryMessage, $"Changing {blockname} to {desiredTechLevelString}");
                                    this._TransactionLog.AddLogEntry(_TransactionLogEntry);
                                    blockname = desiredTechLevelString;
                                    break;
                                case 2:
                                    this._TransactionLogEntry = new TransactionLogEntry(LogEntryTypes.LogEntryMessage, $"Changing {blockname} to {desiredTechLevelString}");
                                    this._TransactionLog.AddLogEntry(_TransactionLogEntry);
                                    blockname = desiredTechLevelString;
                                    break;
                                case 3:
                                    this._TransactionLogEntry = new TransactionLogEntry(LogEntryTypes.LogEntryMessage, $"Changing {blockname} to {desiredTechLevelString}");
                                    this._TransactionLog.AddLogEntry(_TransactionLogEntry);
                                    blockname = desiredTechLevelString;
                                    break;
                                default:
                                    this._TransactionLogEntry = new TransactionLogEntry(LogEntryTypes.LogEntryMessage, $"Anomaly: {blockname} to {desiredTechLevelString} hit 'default' case!");
                                    this._TransactionLog.AddLogEntry(_TransactionLogEntry);
                                    break;
                            }
                        }
                        else
                        {
                            //debug option
                            this._TransactionLogEntry = new TransactionLogEntry(LogEntryTypes.LogEntryMessage, $"{blockname} already at desired tech level {desiredTechLevelString}!");
                            this._TransactionLog.AddLogEntry(_TransactionLogEntry);
                        }

                        // reset current tech level
                        currTechLevel = -1;
                    }
                }

                return this._TransactionLog;
            }
        }
        public class AutoArmor : PartswapOperation
        {
            private bool _DebugMode = false;
            private string _OperationName => "AutoArmor";

            private Dictionary<string, object> _OperationParameters = new Dictionary<string, object>();
            private Dictionary<string, Type> _RecognizedParameters = new Dictionary<string, Type> { { "Operation", typeof(string) } };
            private SKRectI _UIRect = new SKRectI();
            private SKPaint _UIPaint = new SKPaint();
            private SKColor _UIPaintColor = new SKColor();
            private TransactionLog _TransactionLog;
            private TransactionLogEntry _TransactionLogEntry;

            string PartswapOperation.OperationName => _OperationName;
            Dictionary<string, object> PartswapOperation.OperationParameters { get => _OperationParameters; set => this._OperationParameters = value; }
            Dictionary<string, Type> PartswapOperation.RecognizedParameters => _RecognizedParameters;
            SKRectI PartswapOperation.UIRect { get => _UIRect; set => _UIRect = value; }
            SKPaint PartswapOperation.UIPaint { get => _UIPaint; set => _UIPaint = value; }
            SKColor PartswapOperation.UIPaintColor { get => _UIPaintColor; set => _UIPaintColor = value; }
            TransactionLog PartswapOperation.TransactionLog { get => _TransactionLog; set => _TransactionLog = value; }
            bool PartswapOperation.DebugMode { get => _DebugMode; set => _DebugMode = value; }

            public void STC_AutoArmor_From_Tritanium(ref Tuple<CubeGridRenderCellGrid, BlueprintCellGridManager.GridGeometry, GridCursor, SKElement> currentCubegridEntry)
            {
                SKRectI BlockChangedIndicatorRect;
                SKPaint TritaniumToArmorIndicatorPaint = new SKPaint();
                TritaniumToArmorIndicatorPaint.Color = SKColors.DarkSeaGreen;

                string partNameIterator;
                string partNameReplacementIterator;

                // Boolean to indicate we've found a preexisting tritanium block
                bool isTritaniumArmorBlock;

                // Booleans used to detect-and-indicate that we've found relevant blocks to convert to tritanium
                bool isArmorBlock = false;
                bool isHeavyArmorBlock = false;
                bool isPanel = false;
                bool isBeam = false;

                // Used to determine if a block was changed.
                // Useful for logging and debug
                bool blockChanged = false;

                Regex ArmorDiscriminatorPattern = new Regex("(Large|Small)?(HeavyArmor|Armor|HeavyBlock|Block|Half|HeavyHalf)([\\S]+)");

                BlueprintCell[,,] cubeblocks = currentCubegridEntry.Item1.GetBlueprintCell3DArrayRef();

                // iterate through each part on the ship,
                // figure out if it's an armor block,
                // if yes: is it tritanium?
                // if trit: ignore
                // if not-trit: convert

                foreach (BlueprintCell block in cubeblocks)
                {

                    if (block == null || block.GetCubeblockDefinition() == null)
                    {
                        continue;
                    }
                    else
                    {
                        partNameIterator = block.GetCubeblockDefinition().GetSubtypeName();
                        BlockChangedIndicatorRect = block.LoadBaseUIRect();

                        BlockChangedIndicatorRect.Top += 5;
                        BlockChangedIndicatorRect.Bottom -= 5;
                        BlockChangedIndicatorRect.Left += 5;
                        BlockChangedIndicatorRect.Right -= 5;
                    }

                    isTritaniumArmorBlock = partNameIterator.Contains("Tritanium");

                    isArmorBlock = partNameIterator.Contains("Armor");
                    isHeavyArmorBlock = partNameIterator.Contains("Heavy");
                    isPanel = partNameIterator.Contains("Panel");
                    isBeam = partNameIterator.Contains("Beam");

                    // EXISTING TRIT ARMOR
                    if (isTritaniumArmorBlock)
                    {
                        if (_DebugMode)
                        {
                            Trace.WriteLine($"Found tritanium armor:\n----\n{partNameIterator}\n----\n");
                        }
                        blockChanged = false;

                        // VANILLA BEAM
                        if (isBeam)
                        {
                            partNameReplacementIterator = Regex.Replace(partNameIterator, "Tritanium", "");

                            block.GetCubeblockDefinition().SetSubtypeName(partNameReplacementIterator);
                            blockChanged = true;

                            this._TransactionLogEntry = new TransactionLogEntry(LogEntryTypes.LogEntryMessage, $"Replaced {partNameIterator} with {partNameReplacementIterator}");
                            this._TransactionLog.AddLogEntry(_TransactionLogEntry);

                            if (_DebugMode)
                            {
                                Trace.WriteLine($"Replaced {partNameIterator} with {partNameReplacementIterator}");
                            }
                            continue;
                        }

                        // VANILLA PANEL
                        if (isPanel)
                        {
                            partNameReplacementIterator = Regex.Replace(partNameIterator, "Tritanium", "Light");

                            block.GetCubeblockDefinition().SetSubtypeName(partNameReplacementIterator);
                            blockChanged = true;

                            this._TransactionLogEntry = new TransactionLogEntry(LogEntryTypes.LogEntryMessage, $"Replaced {partNameIterator} with {partNameReplacementIterator}");
                            this._TransactionLog.AddLogEntry(_TransactionLogEntry);

                            if (_DebugMode)
                            {
                                Trace.WriteLine($"Replaced {partNameIterator} with {partNameReplacementIterator}");
                            }
                            continue;
                        }

                        // GENERIC ARMOR
                        if (isArmorBlock)
                        {
                            if (partNameIterator.Contains("Tritanium_"))
                            {
                                partNameReplacementIterator = Regex.Replace(partNameIterator, "Tritanium_", "");
                            }
                            else
                            {
                                if (partNameIterator.Contains("Tritanium"))
                                {
                                    partNameReplacementIterator = Regex.Replace(partNameIterator, "Tritanium", "Light");
                                }
                            }

                            partNameReplacementIterator = Regex.Replace(partNameIterator, "Tritanium_", "");

                            Match regexMatch = ArmorDiscriminatorPattern.Match(partNameIterator);
                            string group1 = regexMatch.Groups[1].Value;
                            string group2 = regexMatch.Groups[2].Value;
                            string group3 = regexMatch.Groups[3].Value;
                            string group4 = regexMatch.Groups[4].Value;
                            string group5 = regexMatch.Groups[5].Value;

                            MatchCollection matchCollection = ArmorDiscriminatorPattern.Matches(partNameIterator);

                            // Things we do whenever we have definitely changed a block...
                            block.GetCubeblockDefinition().SetSubtypeName(partNameReplacementIterator);
                            block.AddUIRectangle(new Tuple<string, SKRectI, SKPaint>("LightToHeavyReplacement", BlockChangedIndicatorRect, TritaniumToArmorIndicatorPaint));
                            blockChanged = true;

                            this._TransactionLogEntry = new TransactionLogEntry(LogEntryTypes.LogEntryMessage, $"Replaced {partNameIterator} with {partNameReplacementIterator}");
                            this._TransactionLog.AddLogEntry(_TransactionLogEntry);

                            if (_DebugMode)
                            {
                                Trace.WriteLine($"Replaced {partNameIterator} with {partNameReplacementIterator}");
                            }
                            continue;
                        }

                    }
                    else
                    {
                        if (_DebugMode)
                        {
                            Trace.WriteLine($"Found non-tritanium block: {partNameIterator}");
                        }

                    }

                    if (_DebugMode)
                    {
                        Trace.WriteLine($"Block {partNameIterator} was changed? {blockChanged}");
                    }

                    // Reset our iterating variables
                    isArmorBlock = false;
                    isTritaniumArmorBlock = false;
                    isHeavyArmorBlock = false;
                    blockChanged = false;
                }
            }

            public void AutoArmor_LightToHeavy(ref Tuple<CubeGridRenderCellGrid, BlueprintCellGridManager.GridGeometry, GridCursor, SKElement> currentCubegridEntry)
            {

                SKRectI BlockChangedIndicatorRect;
                SKPaint LightToHeavyChangedIndicatorPaint = new SKPaint();
                LightToHeavyChangedIndicatorPaint.Color = SKColors.IndianRed;

                string partNameIterator;
                string partNameReplacementIterator;

                // Booleans used to detect-and-indicate that we've found relevant blocks to convert to tritanium
                bool isArmorBlock = false;
                bool isHeavyArmorBlock = false;
                bool isPanel = false;
                bool isBeam = false;

                // Used to determine if a block was changed.
                // Useful for logging and debug
                bool blockChanged = false;

                Regex ArmorDiscriminatorPattern = new Regex("(Large|Small)?(HeavyArmor|Armor|HeavyBlock|HeavyHalf|HalfSlope|ArmorSlope|BlockHeavy|Block|Half)([\\S]+)");

                BlueprintCell[,,] cubeblocks = currentCubegridEntry.Item1.GetBlueprintCell3DArrayRef();

                foreach (BlueprintCell block in cubeblocks)
                {
                    if (block == null || block.GetCubeblockDefinition() == null)
                    {
                        continue;
                    }
                    else
                    {
                        partNameIterator = block.GetCubeblockDefinition().GetSubtypeName();
                        BlockChangedIndicatorRect = block.LoadBaseUIRect();

                        BlockChangedIndicatorRect.Top += 5;
                        BlockChangedIndicatorRect.Bottom -= 5;
                        BlockChangedIndicatorRect.Left += 5;
                        BlockChangedIndicatorRect.Right -= 5;
                    }

                    isArmorBlock = partNameIterator.Contains("Armor");
                    isHeavyArmorBlock = partNameIterator.Contains("Heavy");
                    isPanel = partNameIterator.Contains("Panel");
                    isBeam = partNameIterator.Contains("Beam");

                    // EXISTING LIGHT ARMOR
                    if (!isHeavyArmorBlock)
                    {
                        if (_DebugMode)
                        {
                            Trace.WriteLine($"Found light armor:\n----\n{partNameIterator}\n----\nEVALUATING!\n");
                        }

                        blockChanged = false;

                        // VANILLA PANEL
                        if (isPanel && isArmorBlock && !isHeavyArmorBlock)
                        {
                            partNameReplacementIterator = Regex.Replace(partNameIterator, "Light", "Heavy");


                            block.GetCubeblockDefinition().SetSubtypeName(partNameReplacementIterator);
                            blockChanged = true;

                            _TransactionLogEntry = new TransactionLogEntry(LogEntryTypes.LogEntryMessage, $"Replaced {partNameIterator} with {partNameReplacementIterator}");
                            this._TransactionLog.AddLogEntry(_TransactionLogEntry);

                            if (_DebugMode)
                            {
                                Trace.WriteLine($"Replaced {partNameIterator} with {partNameReplacementIterator}");
                            }
                        }

                        // GENERIC ARMOR
                        if (isArmorBlock && !isPanel)
                        {
                            Match regexMatch = ArmorDiscriminatorPattern.Match(partNameIterator);
                            string group1 = regexMatch.Groups[1].Value;
                            string group2 = regexMatch.Groups[2].Value;
                            string group3 = regexMatch.Groups[3].Value;
                            string group4 = regexMatch.Groups[4].Value;
                            string group5 = regexMatch.Groups[5].Value;

                            MatchCollection matchCollection = ArmorDiscriminatorPattern.Matches(partNameIterator);

                            partNameReplacementIterator = partNameIterator; // TODO <-- Change this

                            switch (group1)
                            {

                                case "Large":
                                    switch (group2)
                                    {
                                        case "Block":
                                            switch (group3)
                                            {
                                                case "ArmorBlock":
                                                    partNameReplacementIterator = Regex.Replace(partNameReplacementIterator, (group1 + group2), "LargeHeavyBlock");
                                                    break;
                                                case "ArmorSlope":
                                                    partNameReplacementIterator = Regex.Replace(partNameReplacementIterator, (group1 + group2), "LargeHeavyBlock");
                                                    break;
                                                case "ArmorCorner":
                                                    partNameReplacementIterator = Regex.Replace(partNameReplacementIterator, (group1 + group2), "LargeHeavyBlock");
                                                    break;
                                                case "ArmorCornerInv":
                                                    partNameReplacementIterator = Regex.Replace(partNameReplacementIterator, (group1 + group2), "LargeHeavyBlock");
                                                    break;
                                                case "ArmorRoundSlope":
                                                    partNameReplacementIterator = Regex.Replace(partNameReplacementIterator, (group1 + group2), "LargeHeavyBlock");
                                                    break;
                                                case "ArmorRoundCorner":
                                                    partNameReplacementIterator = Regex.Replace(partNameReplacementIterator, (group1 + group2), "LargeHeavyBlock");
                                                    break;
                                                case "ArmorRoundCornerInv":
                                                    partNameReplacementIterator = Regex.Replace(partNameReplacementIterator, (group1 + group2), "LargeHeavyBlock");
                                                    break;
                                                case "ArmorCornerSquare":
                                                    partNameReplacementIterator = Regex.Replace(partNameReplacementIterator, (group1 + group2), "LargeHeavyBlock");
                                                    break;
                                                case "ArmorCornerSquareInverted":
                                                    partNameReplacementIterator = Regex.Replace(partNameReplacementIterator, (group1 + group2), "LargeHeavyBlock");
                                                    break;
                                                case "ArmorSlope2Base":
                                                    partNameReplacementIterator = Regex.Replace(partNameReplacementIterator, (group1 + group2), "LargeHeavyBlock");
                                                    break;
                                                case "ArmorSlope2Tip":
                                                    partNameReplacementIterator = Regex.Replace(partNameReplacementIterator, (group1 + group2), "LargeHeavyBlock");
                                                    break;
                                                case "ArmorCorner2Base":
                                                    partNameReplacementIterator = Regex.Replace(partNameReplacementIterator, (group1 + group2), "LargeHeavyBlock");
                                                    break;
                                                case "ArmorCorner2Tip":
                                                    partNameReplacementIterator = Regex.Replace(partNameReplacementIterator, (group1 + group2), "LargeHeavyBlock");
                                                    break;
                                                case "ArmorInvCorner2Base":
                                                    partNameReplacementIterator = Regex.Replace(partNameReplacementIterator, (group1 + group2), "LargeHeavyBlock");
                                                    break;
                                                case "ArmorInvCorner2Tip":
                                                    partNameReplacementIterator = Regex.Replace(partNameReplacementIterator, (group1 + group2), "LargeHeavyBlock");
                                                    break;
                                                case "ArmorHalfSlopeInverted":
                                                    partNameReplacementIterator = Regex.Replace(partNameReplacementIterator, (group1 + group2), "LargeHeavyBlock");
                                                    break;
                                                case "ArmorHalfSlopeCorner":
                                                    partNameReplacementIterator = Regex.Replace(partNameReplacementIterator, (group1 + group2), "LargeHeavyBlock");
                                                    break;
                                                case "ArmorHalfSlopeCornerInverted":
                                                    partNameReplacementIterator = Regex.Replace(partNameReplacementIterator, (group1 + group2), "LargeHeavyBlock");
                                                    break;

                                            }
                                            break;
                                        case "Half":
                                            switch (group3)
                                            {
                                                case "ArmorBlock":
                                                    partNameReplacementIterator = Regex.Replace(partNameReplacementIterator, (group1 + group2), "LargeHeavyHalf");
                                                    break;
                                                case "SlopeArmorBlock":
                                                    partNameReplacementIterator = Regex.Replace(partNameReplacementIterator, (group1 + group2), "LargeHeavyHalf");
                                                    break;
                                                default:
                                                    throw new ArgumentException($"Unhandled combination group1 case:{group1}, group2:{group2}, group3:{group3}");
                                            }
                                            break;
                                        case "HalfSlope":
                                            switch (group3)
                                            {
                                                case "ArmorBlock":
                                                    partNameReplacementIterator = Regex.Replace(partNameReplacementIterator, (group2), "HeavyHalfSlope");
                                                    break;
                                                default:
                                                    throw new ArgumentException($"Unhandled combination group1 case:{group1}, group2:{group2}");
                                            }
                                            break;
                                        default:
                                            throw new ArgumentException($"Unhandled combination group1 case:{group1}, group2:{group2}");
                                    }
                                    break;
                                case "Small":
                                    switch (group2)
                                    {
                                        case "Block":
                                            switch (group3)
                                            {
                                                case "ArmorBlock":
                                                    partNameReplacementIterator = Regex.Replace(partNameReplacementIterator, (group1 + group2), "SmallBlockHeavyArmor");
                                                    break;
                                                case "ArmorSlope":
                                                    partNameReplacementIterator = Regex.Replace(partNameReplacementIterator, (group1 + group2), "SmallBlockHeavyArmor");
                                                    break;
                                                case "ArmorCorner":
                                                    partNameReplacementIterator = Regex.Replace(partNameReplacementIterator, (group1 + group2), "SmallBlockHeavyArmor");
                                                    break;
                                                case "ArmorCornerInv":
                                                    partNameReplacementIterator = Regex.Replace(partNameReplacementIterator, (group1 + group2), "SmallBlockHeavyArmor");
                                                    break;
                                                case "ArmorCornerSquareInverted":
                                                    partNameReplacementIterator = Regex.Replace(partNameReplacementIterator, (group1 + group2), "SmallBlockHeavyArmor");
                                                    break;
                                                default:
                                                    throw new ArgumentException($"Unhandled combination group1 case:{group1}, group2:{group2}, group3:{group3}");
                                            }
                                            break;
                                        default:
                                            throw new ArgumentException($"Unhandled combination group1 case:{group1}, group2:{group2}");
                                    }
                                    break;
                                case "":
                                    switch (group2)
                                    {
                                        case "Half":
                                            switch (group3)
                                            {
                                                case "ArmorBlock":
                                                    partNameReplacementIterator = Regex.Replace(partNameReplacementIterator, (group2), "HeavyHalf");
                                                    break;
                                                case "SlopeArmorBlock":
                                                    partNameReplacementIterator = Regex.Replace(partNameReplacementIterator, (group2), "HeavyHalf");
                                                    break;
                                                default:
                                                    throw new ArgumentException($"Unhandled combination group1 case:{group1}, group2:{group2}, group3:{group3}");
                                            }
                                            break;
                                        case "Armor":
                                            switch (group3)
                                            {
                                                case "Center":
                                                    if (_DebugMode)
                                                    {
                                                        Trace.WriteLine($"Found exception block: {partNameIterator}. Skipping!");
                                                    }
                                                    break;
                                                default:
                                                    throw new ArgumentException($"Unhandled combination group1 case:{group1}, group2:{group2}");
                                            }
                                            break;
                                        default:
                                            throw new ArgumentException($"Unhandled combination group1 case:{group1}, group2:{group2}");
                                    }
                                    break;
                                default:
                                    throw new ArgumentException($"Unhandled group1 case:{group1} ");
                            }

                            // Things we do whenever we have definitely changed a block...
                            block.GetCubeblockDefinition().SetSubtypeName(partNameReplacementIterator);
                            block.AddUIRectangle(new Tuple<string, SKRectI, SKPaint>("LightToHeavyReplacement", BlockChangedIndicatorRect, LightToHeavyChangedIndicatorPaint));
                            blockChanged = true;

                            this._TransactionLogEntry = new TransactionLogEntry(LogEntryTypes.LogEntryMessage, $"Replaced {partNameIterator} with {partNameReplacementIterator} at position {block.GetCubeblockDefinition().GetMinVector().ToString()}");
                            this._TransactionLog.AddLogEntry(_TransactionLogEntry);

                            if (_DebugMode)
                            {
                                Trace.WriteLine($"Replaced {partNameIterator} with {partNameReplacementIterator}");
                            }
                            continue;
                        }
                    }

                    if (_DebugMode)
                    {
                        Trace.WriteLine($"Block {partNameIterator} was changed? {blockChanged}");
                    }
                    // Reset our iterating variables
                    isArmorBlock = false;
                    isHeavyArmorBlock = false;
                    blockChanged = false;

                    currentCubegridEntry.Item1.GetBlueprintCell3DArrayRef() = cubeblocks;
                }

            }

            public void AutoArmor_HeavyToLight(ref Tuple<CubeGridRenderCellGrid, BlueprintCellGridManager.GridGeometry, GridCursor, SKElement> currentCubegridEntry)
            {
                SKRectI BlockChangedIndicatorRect;
                SKPaint HeavyToLightPaintedIndicator = new SKPaint();
                HeavyToLightPaintedIndicator.Color = SKColors.DarkSlateBlue;

                string partNameIterator;
                string partNameReplacementIterator;


                // Booleans used to detect-and-indicate that we've found relevant blocks to convert to tritanium
                bool isArmorBlock = false;
                bool isHeavyArmor = false;
                bool isPanel = false;
                bool isBeam = false;

                // Used to determine if a block was changed.
                // Useful for logging and debug
                bool blockChanged = false;

                Regex ArmorDiscriminatorPattern = new Regex("(Large|Small)?(HeavyArmor|Armor|HeavyBlock|HeavyHalf|HalfSlope|ArmorSlope|BlockHeavy|Block|Half)([\\S]+)");

                BlueprintCell[,,] cubeblocks = currentCubegridEntry.Item1.GetBlueprintCell3DArrayRef();

                foreach (BlueprintCell block in cubeblocks)
                {
                    if (block == null || block.GetCubeblockDefinition() == null)
                    {
                        continue;
                    }
                    else
                    {
                        partNameIterator = block.GetCubeblockDefinition().GetSubtypeName();
                        BlockChangedIndicatorRect = block.LoadBaseUIRect();

                        BlockChangedIndicatorRect.Top += 5;
                        BlockChangedIndicatorRect.Bottom -= 5;
                        BlockChangedIndicatorRect.Left += 5;
                        BlockChangedIndicatorRect.Right -= 5;
                    }

                    isArmorBlock = partNameIterator.Contains("Armor");
                    isHeavyArmor = partNameIterator.Contains("Heavy");
                    isPanel = partNameIterator.Contains("Panel");
                    isBeam = partNameIterator.Contains("Beam");

                    // EXISTING HEAVY ARMOR
                    if (isHeavyArmor)
                    {
                        if (_DebugMode)
                        {
                            Trace.WriteLine($"Found light armor:\n----\n{partNameIterator}\n----\nEVALUATING!\n");
                        }
                        blockChanged = false;

                        // VANILLA PANEL
                        if (isPanel)
                        {
                            partNameReplacementIterator = partNameIterator.Replace("Heavy", "Light");

                            block.GetCubeblockDefinition().SetSubtypeName(partNameReplacementIterator);
                            blockChanged = true;

                            this._TransactionLogEntry = new TransactionLogEntry(LogEntryTypes.LogEntryMessage, $"Replaced {partNameIterator} with {partNameReplacementIterator}");
                            this._TransactionLog.AddLogEntry(_TransactionLogEntry);

                            if (_DebugMode)
                            {
                                Trace.WriteLine($"Replaced {partNameIterator} with {partNameReplacementIterator}");
                            }

                            block.AddUIRectangle(new Tuple<string, SKRectI, SKPaint>("LightToHeavyReplacement", block.LoadBaseUIRect(), HeavyToLightPaintedIndicator));

                            this._TransactionLogEntry = new TransactionLogEntry(LogEntryTypes.LogEntryMessage, $"Replaced {partNameIterator} with {partNameReplacementIterator} at position {block.GetCubeblockDefinition().GetMinVector().ToString()}");
                            this._TransactionLog.AddLogEntry(_TransactionLogEntry);

                            blockChanged = true;
                        }

                        // GENERIC ARMOR
                        if (isArmorBlock && !isPanel)
                        {
                            Match regexMatch = ArmorDiscriminatorPattern.Match(partNameIterator);
                            string group1 = regexMatch.Groups[1].Value;
                            string group2 = regexMatch.Groups[2].Value;
                            string group3 = regexMatch.Groups[3].Value;
                            string group4 = regexMatch.Groups[4].Value;
                            string group5 = regexMatch.Groups[5].Value;

                            MatchCollection matchCollection = ArmorDiscriminatorPattern.Matches(partNameIterator);

                            partNameReplacementIterator = partNameIterator;

                            switch (group1)
                            {

                                case "Large":
                                    switch (group2)
                                    {
                                        case "Block":
                                            if (_DebugMode)
                                            {
                                                Trace.WriteLine($"Found non-heavy block: {partNameIterator}. Skipping!");
                                            }
                                            continue;
                                        case "BlockHeavy":
                                            switch (group3)
                                            {
                                                case "ArmorCornerSquare":
                                                    partNameReplacementIterator = Regex.Replace(partNameReplacementIterator, (group1 + group2), "LargeBlock");
                                                    break;
                                                case "ArmorCornerSquareInverted":
                                                    partNameReplacementIterator = Regex.Replace(partNameReplacementIterator, (group1 + group2), "LargeBlock");
                                                    break;
                                                case "ArmorSlopeTransitionBaseMirrored":
                                                    partNameReplacementIterator = Regex.Replace(partNameReplacementIterator, (group1 + group2), "LargeBlock");
                                                    break;
                                                case "ArmorSlopeTransitionBase":
                                                    partNameReplacementIterator = Regex.Replace(partNameReplacementIterator, (group1 + group2), "LargeBlock");
                                                    break;
                                                case "ArmorHalfSlopedCornerBase":
                                                    partNameReplacementIterator = Regex.Replace(partNameReplacementIterator, (group1 + group2), "LargeBlock");
                                                    break;
                                                case "ArmorSlopedCorner":
                                                    partNameReplacementIterator = Regex.Replace(partNameReplacementIterator, (group1 + group2), "LargeBlock");
                                                    break;
                                                case "ArmorSquareSlopedCornerBase":
                                                    partNameReplacementIterator = Regex.Replace(partNameReplacementIterator, (group1 + group2), "LargeBlock");
                                                    break;
                                            }
                                            break;

                                        case "HeavyBlock":
                                            switch (group3)
                                            {
                                                case "ArmorBlock":
                                                    partNameReplacementIterator = Regex.Replace(partNameReplacementIterator, (group1 + group2), "LargeBlock");
                                                    break;
                                                case "ArmorSlope":
                                                    partNameReplacementIterator = Regex.Replace(partNameReplacementIterator, (group1 + group2), "LargeBlock");
                                                    break;
                                                case "ArmorCorner":
                                                    partNameReplacementIterator = Regex.Replace(partNameReplacementIterator, (group1 + group2), "LargeBlock");
                                                    break;
                                                case "ArmorCornerInv":
                                                    partNameReplacementIterator = Regex.Replace(partNameReplacementIterator, (group1 + group2), "LargeBlock");
                                                    break;
                                                case "ArmorRoundSlope":
                                                    partNameReplacementIterator = Regex.Replace(partNameReplacementIterator, (group1 + group2), "LargeBlock");
                                                    break;
                                                case "ArmorRoundCorner":
                                                    partNameReplacementIterator = Regex.Replace(partNameReplacementIterator, (group1 + group2), "LargeBlock");
                                                    break;
                                                case "ArmorRoundCornerInv":
                                                    partNameReplacementIterator = Regex.Replace(partNameReplacementIterator, (group1 + group2), "LargeBlock");
                                                    break;
                                                case "ArmorCornerSquare":
                                                    partNameReplacementIterator = Regex.Replace(partNameReplacementIterator, (group1 + group2), "LargeBlock");
                                                    break;
                                                case "ArmorCornerSquareInverted":
                                                    partNameReplacementIterator = Regex.Replace(partNameReplacementIterator, (group1 + group2), "LargeBlock");
                                                    break;
                                                case "ArmorSlope2Base":
                                                    partNameReplacementIterator = Regex.Replace(partNameReplacementIterator, (group1 + group2), "LargeBlock");
                                                    break;
                                                case "ArmorSlope2Tip":
                                                    partNameReplacementIterator = Regex.Replace(partNameReplacementIterator, (group1 + group2), "LargeBlock");
                                                    break;
                                                case "ArmorCorner2Base":
                                                    partNameReplacementIterator = Regex.Replace(partNameReplacementIterator, (group1 + group2), "LargeBlock");
                                                    break;
                                                case "ArmorCorner2Tip":
                                                    partNameReplacementIterator = Regex.Replace(partNameReplacementIterator, (group1 + group2), "LargeBlock");
                                                    break;
                                                case "ArmorInvCorner2Base":
                                                    partNameReplacementIterator = Regex.Replace(partNameReplacementIterator, (group1 + group2), "LargeBlock");
                                                    break;
                                                case "ArmorInvCorner2Tip":
                                                    partNameReplacementIterator = Regex.Replace(partNameReplacementIterator, (group1 + group2), "LargeBlock");
                                                    break;
                                                case "ArmorHalfSlopeInverted":
                                                    partNameReplacementIterator = Regex.Replace(partNameReplacementIterator, (group1 + group2), "LargeBlock");
                                                    break;
                                                case "ArmorHalfSlopeCorner":
                                                    partNameReplacementIterator = Regex.Replace(partNameReplacementIterator, (group1 + group2), "LargeBlock");
                                                    break;
                                                case "ArmorHalfSlopeCornerInverted":
                                                    partNameReplacementIterator = Regex.Replace(partNameReplacementIterator, (group1 + group2), "LargeBlock");
                                                    break;

                                            }
                                            break;
                                        case "HeavyHalf":
                                            switch (group3)
                                            {
                                                case "ArmorBlock":
                                                    partNameReplacementIterator = Regex.Replace(partNameReplacementIterator, (group1 + group2), "LargeHalf");
                                                    break;
                                                case "SlopeArmorBlock":
                                                    partNameReplacementIterator = Regex.Replace(partNameReplacementIterator, (group1 + group2), "LargeHalf");
                                                    break;
                                                default:
                                                    throw new ArgumentException($"Unhandled combination group1 case:{group1}, group2:{group2}, group3:{group3}");
                                            }
                                            break;
                                        default:
                                            throw new ArgumentException($"Unhandled combination group1 case:{group1}, group2:{group2}");
                                    }
                                    break;
                                case "Small":
                                    switch (group2)
                                    {
                                        case "BlockHeavy":
                                            switch (group3)
                                            {
                                                case "ArmorBlock":
                                                    partNameReplacementIterator = Regex.Replace(partNameReplacementIterator, (group1 + group2), "SmallBlockArmor");
                                                    break;
                                                case "ArmorSlope":
                                                    partNameReplacementIterator = Regex.Replace(partNameReplacementIterator, (group1 + group2), "SmallBlockArmor");
                                                    break;
                                                case "ArmorCorner":
                                                    partNameReplacementIterator = Regex.Replace(partNameReplacementIterator, (group1 + group2), "SmallBlockArmor");
                                                    break;
                                                case "ArmorCornerInv":
                                                    partNameReplacementIterator = Regex.Replace(partNameReplacementIterator, (group1 + group2), "SmallBlockArmor");
                                                    break;
                                                case "ArmorCornerSquareInverted":
                                                    partNameReplacementIterator = Regex.Replace(partNameReplacementIterator, (group1 + group2), "SmallBlockArmor");
                                                    break;
                                                case "ArmorSquareSlopedCornerBase":
                                                    partNameReplacementIterator = Regex.Replace(partNameReplacementIterator, (group1 + group2), "SmallBlockArmor");
                                                    break;
                                                default:
                                                    throw new ArgumentException($"Unhandled combination group1 case:{group1}, group2:{group2}, group3:{group3}");
                                            }
                                            break;
                                        default:
                                            throw new ArgumentException($"Unhandled combination group1 case:{group1}, group2:{group2}");
                                    }
                                    break;
                                case "":
                                    switch (group2)
                                    {
                                        case "HeavyHalf":
                                            switch (group3)
                                            {
                                                case "ArmorBlock":
                                                    partNameReplacementIterator = Regex.Replace(partNameReplacementIterator, (group2), "Half");
                                                    break;
                                                case "SlopeArmorBlock":
                                                    partNameReplacementIterator = Regex.Replace(partNameReplacementIterator, (group2), "Half");
                                                    break;
                                                default:
                                                    throw new ArgumentException($"Unhandled combination group1 case:{group1}, group2:{group2}, group3:{group3}");
                                            }
                                            break;
                                        case "Armor":
                                            switch (group3)
                                            {
                                                case "Center":
                                                    // Do nothing. Special case for Blast Doors (they are named Armor Block/Armor Center/ etc).
                                                    break;
                                                default:
                                                    throw new ArgumentException($"Unhandled combination group1 case:{group1}, group2:{group2}, group3:{group3}");
                                            }
                                            break;
                                        default:
                                            throw new ArgumentException($"Unhandled combination group1 case:{group1}, group2:{group2}");
                                    }
                                    break;
                                default:
                                    throw new ArgumentException($"Unhandled group1 case:{group1} ");
                            }


                            block.GetCubeblockDefinition().SetSubtypeName(partNameReplacementIterator);

                            block.AddUIRectangle(new Tuple<string, SKRectI, SKPaint>("HeavyToLightReplacement", BlockChangedIndicatorRect, HeavyToLightPaintedIndicator));

                            if (partNameIterator.Equals(partNameReplacementIterator))
                            {

                                this._TransactionLogEntry = new TransactionLogEntry(LogEntryTypes.LogEntryMessage, $"Failed to replace {partNameIterator} at position {block.GetCubeblockDefinition().GetMinVector().ToString()}!\nRegex Groups:\nGroup1:{group1}\nGroup2:{group2}\nGroup3:{group3}");
                                this._TransactionLog.AddLogEntry(_TransactionLogEntry);

                                blockChanged = false;
                            }
                            else
                            {
                                this._TransactionLogEntry = new TransactionLogEntry(LogEntryTypes.LogEntryMessage, $"Replaced {partNameIterator} with {partNameReplacementIterator} at position {block.GetCubeblockDefinition().GetMinVector().ToString()}");
                                this._TransactionLog.AddLogEntry(_TransactionLogEntry);

                                blockChanged = true;
                            }

                            if (_DebugMode)
                            {
                                Trace.WriteLine($"Replaced {partNameIterator} with {partNameReplacementIterator}");
                            }
                        }
                    }

                    if (_DebugMode)
                    {
                        Trace.WriteLine($"Block {partNameIterator} at position {block.GetCubeblockDefinition().GetMinVector().ToString()} was changed? {blockChanged}");
                    }

                    // Reset our iterating variables
                    isArmorBlock = false;
                    isHeavyArmor = false;
                    blockChanged = false;

                    currentCubegridEntry.Item1.GetBlueprintCell3DArrayRef() = cubeblocks;
                }

            }

            TransactionLog PartswapOperation.PerformOperation(ref Tuple<CubeGridRenderCellGrid, BlueprintCellGridManager.GridGeometry, GridCursor, SKElement> currentCubegridEntry)
            {
                switch (_OperationParameters["Operation"])
                {
                    case "STC_From_Tritanium":
                        STC_AutoArmor_From_Tritanium(ref currentCubegridEntry);
                        break;
                    case "LightToHeavy":
                        AutoArmor_LightToHeavy(ref currentCubegridEntry);
                        break;
                    case "HeavyToLight":
                        AutoArmor_HeavyToLight(ref currentCubegridEntry);
                        break;
                    case null:
                        throw new ArgumentException("PerformOperation needs to have parameter Operation set!");
                }
                return _TransactionLog;
            }

            public AutoArmor(string operation)
            {
                this._TransactionLog = new TransactionLog(this._OperationName);

                _OperationParameters.Add("Operation", operation);
            }

            public AutoArmor()
            {
                this._TransactionLog = new TransactionLog(this._OperationName);
            }
        }

        public class RemoveTool : PartswapOperation
        {
            private bool _DebugMode = false;
            private string _OperationName => "RemoveTool";

            private Dictionary<string, object> _OperationParameters = new Dictionary<string, object>();
            private Dictionary<string, Type> _RecognizedParameters = new Dictionary<string, Type> { { "Operation", typeof(string) }, { "Remove_Armor", typeof(string) }, { "RemoveAllExcept", typeof(string) }, { "RemoveSpecific", typeof(string) } };
            private SKRectI _UIRect = new SKRectI();
            private SKPaint _UIPaint = new SKPaint();
            private SKColor _UIPaintColor = new SKColor();
            private TransactionLog _TransactionLog;
            private TransactionLogEntry _TransactionLogEntry;

            string PartswapOperation.OperationName => _OperationName;
            Dictionary<string, object> PartswapOperation.OperationParameters { get => _OperationParameters; set => this._OperationParameters = value; }
            Dictionary<string, Type> PartswapOperation.RecognizedParameters => _RecognizedParameters;
            SKRectI PartswapOperation.UIRect { get => _UIRect; set => _UIRect = value; }
            SKPaint PartswapOperation.UIPaint { get => _UIPaint; set => _UIPaint = value; }
            SKColor PartswapOperation.UIPaintColor { get => _UIPaintColor; set => _UIPaintColor = value; }
            TransactionLog PartswapOperation.TransactionLog { get => _TransactionLog; set => _TransactionLog = value; }
            bool PartswapOperation.DebugMode { get => _DebugMode; set => _DebugMode = value; }

            public void Remove_Armor(bool removeHeavyOnly, bool removeLightOnly, Tuple<CubeGridRenderCellGrid, BlueprintCellGridManager.GridGeometry, GridCursor, SKElement> currentCubegridEntry)
            {
                SKRectI BlockChangedIndicatorRect;
                SKPaint HeavyToLightPaintedIndicator = new SKPaint();
                HeavyToLightPaintedIndicator.Color = SKColors.MediumPurple;

                string partNameIterator;
                bool isArmorBlock;
                bool isTritaniumArmorBlock;
                bool isHeavyArmorBlock;

                BlueprintCell[,,] cubeblocks = currentCubegridEntry.Item1.GetBlueprintCell3DArrayRef();

                // iterate through each part on the ship,
                // figure out if it's an armor block,
                // if yes: is it tritanium?
                // if trit: ignore
                // if not-trit: convert
                foreach (BlueprintCell block in cubeblocks)
                {

                    if (block == null || block.GetCubeblockDefinition() == null)
                    {
                        continue;
                    }
                    else
                    {
                        partNameIterator = block.GetCubeblockDefinition().GetSubtypeName();
                        BlockChangedIndicatorRect = block.LoadBaseUIRect();

                        BlockChangedIndicatorRect.Top += 5;
                        BlockChangedIndicatorRect.Bottom -= 5;
                        BlockChangedIndicatorRect.Left += 5;
                        BlockChangedIndicatorRect.Right -= 5;
                    }

                    isArmorBlock = partNameIterator.Contains("Armor");
                    isTritaniumArmorBlock = partNameIterator.Contains("Tritanium");
                    isHeavyArmorBlock = partNameIterator.Contains("Heavy");

                    if (isTritaniumArmorBlock)
                    {
                        Trace.WriteLine($"Found tritanium armor:\n----\n{partNameIterator}\n");
                    }

                    if (isArmorBlock)
                    {

                        // Delete the word 'heavy' from the name of the block.
                        // There are no 'heavy' tritanium variants.

                        // If we are set to removeHeavyOnly==true
                        if (removeHeavyOnly || removeLightOnly)
                        {
                            //...And the block we are looking at is a Heavy Armor Block
                            if (isHeavyArmorBlock && removeHeavyOnly)
                            {
                                //...Delete the heavy armor block!
                                try
                                {
                                    block.Delete();

                                    this._TransactionLogEntry = new TransactionLogEntry(LogEntryTypes.LogEntryMessage, $"Deleted Armor Block:\n----{partNameIterator}----\n");
                                    this._TransactionLog.AddLogEntry(_TransactionLogEntry);

                                    this._UIRect = block.GetUIRect();
                                    this._UIRect.Inflate(-10, -10);

                                    block.AddUIRectangle(new Tuple<string, SKRectI, SKPaint>(this._OperationName, this._UIRect, this._UIPaint));
                                }
                                catch (Exception ex)
                                {
                                    Trace.WriteLine($"Error while deleting {partNameIterator} at array coord {block.GetArrayCoordinate()}!\nError was:\n{ex}");
                                }
                                continue;
                            }

                            // Case where removeLightOnly is true, and we have detected a heavy armor block
                            if (isHeavyArmorBlock && removeLightOnly)
                            {
                                //Make sure we skip and heavy armor blocks we find while deleting only light armor!
                                continue;

                            }

                            // Case where removeLightOnly is true, and we have not detected a heavy armor block (but we *have* detected an armor block)
                            if (!isHeavyArmorBlock && removeLightOnly)
                            {
                                //...Delete the light armor block!
                                try
                                {
                                    block.Delete();

                                    this._TransactionLogEntry = new TransactionLogEntry(LogEntryTypes.LogEntryMessage, $"Deleted Light Armor Block:\n----{partNameIterator}----\n");
                                    this._TransactionLog.AddLogEntry(_TransactionLogEntry);

                                    this._UIRect = block.GetUIRect();
                                    this._UIRect.Inflate(-10, -10);

                                    block.AddUIRectangle(new Tuple<string, SKRectI, SKPaint>(this._OperationName, this._UIRect, this._UIPaint));
                                }
                                catch (Exception ex)
                                {
                                    Trace.WriteLine($"Error while deleting {partNameIterator} at array coord {block.GetArrayCoordinate()}!\nError was:\n{ex}");
                                }
                                continue;
                            }

                            // Case where removeLightOnly is true, and we have not detected a heavy armor block (but we *have* detected an armor block)
                            if (!isHeavyArmorBlock && removeHeavyOnly)
                            {
                                continue;
                            }

                        }
                        else
                        {
                            try
                            {
                                block.Delete();

                                this._TransactionLogEntry = new TransactionLogEntry(LogEntryTypes.LogEntryMessage, $"Deleted {(!isHeavyArmorBlock ? "Heavy" : "Light")} Armor Block:\n----{partNameIterator}----\n");
                                this._TransactionLog.AddLogEntry(_TransactionLogEntry);
                            }
                            catch (Exception ex)
                            {
                                Trace.WriteLine($"Error while deleting {partNameIterator} at array coord {block.GetArrayCoordinate()}!\nError was:\n{ex}");
                            }
                        }
                    }

                    if (_DebugMode)
                    {
                        Trace.WriteLine($"Block {partNameIterator} at position {block.GetCubeblockDefinition().GetMinVector().ToString()} was Deleted? {isArmorBlock}");
                    }

                    // Reset our iterating variables
                    isArmorBlock = false;
                    isTritaniumArmorBlock = false;
                    isHeavyArmorBlock = false;

                    currentCubegridEntry.Item1.GetBlueprintCell3DArrayRef() = cubeblocks;
                }

            }

            public void RemoveAllExceptArmor(Tuple<CubeGridRenderCellGrid, BlueprintCellGridManager.GridGeometry, GridCursor, SKElement> currentCubegridEntry)
            {

                string partNameIterator;
                bool isArmorBlock;
                bool isTritaniumArmorBlock;
                bool isHeavyArmorBlock;

                BlueprintCell[,,] cubeblocks = currentCubegridEntry.Item1.GetBlueprintCell3DArrayRef();

                foreach (BlueprintCell block in cubeblocks)
                {

                    if (block == null || block.GetCubeblockDefinition() == null)
                    {
                        continue;
                    }
                    else
                    {
                        partNameIterator = block.GetCubeblockDefinition().GetSubtypeName();
                    }

                    isArmorBlock = partNameIterator.Contains("Armor");
                    isTritaniumArmorBlock = partNameIterator.Contains("Tritanium");
                    isHeavyArmorBlock = partNameIterator.Contains("Heavy");

                    if (isTritaniumArmorBlock)
                    {
                        Trace.WriteLine($"Found tritanium armor:\n----\n{partNameIterator}\n");
                    }

                    if (isArmorBlock)
                    {
                        continue;
                    }
                    else
                    {
                        //...Delete the non-armor block!
                        try
                        {
                            block.Delete();

                            this._TransactionLogEntry = new TransactionLogEntry(LogEntryTypes.LogEntryMessage, $"Deleted Non-Armor Block:\n----{partNameIterator}----\n");
                            this._TransactionLog.AddLogEntry(_TransactionLogEntry);

                            this._UIRect = block.GetUIRect();
                            this._UIRect.Inflate(-10, -10);

                            block.AddUIRectangle(new Tuple<string, SKRectI, SKPaint>(this._OperationName, this._UIRect, this._UIPaint));
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine($"Error while deleting {partNameIterator}!\nError was:\n{ex}");
                        }
                    }

                    // Reset our iterating variables
                    isArmorBlock = false;
                    isTritaniumArmorBlock = false;
                    isHeavyArmorBlock = false;

                    currentCubegridEntry.Item1.GetBlueprintCell3DArrayRef() = cubeblocks;
                }
            }

            public void RemoveAllExcept(string retainedBlockVariant, ref Tuple<CubeGridRenderCellGrid, BlueprintCellGridManager.GridGeometry, GridCursor, SKElement> currentCubegridEntry)
            {

                string partNameIterator;

                BlueprintCell[,,] cubeblocks = currentCubegridEntry.Item1.GetBlueprintCell3DArrayRef();

                // Iterate through each part in the ship
                foreach (BlueprintCell block in cubeblocks)
                {
                    if (block == null || block.GetCubeblockDefinition() == null)
                    {
                        continue;
                    }
                    else
                    {
                        partNameIterator = block.GetCubeblockDefinition().GetSubtypeName();
                    }

                    // If the part we are looking at (partNameIterator) contains the term that we want to spare - skip!
                    if (partNameIterator.Contains(retainedBlockVariant))
                    {
                        continue;
                    }
                    else
                    {
                        try
                        {
                            block.Delete();


                            this._TransactionLogEntry = new TransactionLogEntry(LogEntryTypes.LogEntryMessage, $"Deleting block: " + partNameIterator + "\n");
                            this._TransactionLog.AddLogEntry(_TransactionLogEntry);

                            this._UIRect = block.GetUIRect();
                            this._UIRect.Inflate(-10, -10);

                            block.AddUIRectangle(new Tuple<string, SKRectI, SKPaint>(this._OperationName, this._UIRect, this._UIPaint));
                        }
                        catch (Exception ex)
                        {
                            this._TransactionLogEntry = new TransactionLogEntry(LogEntryTypes.LogEntryMessage, $"Error while deleting {partNameIterator}!\n");
                            this._TransactionLog.AddLogEntry(_TransactionLogEntry);

                            Trace.WriteLine($"Error while deleting {partNameIterator}!\nError was:\n{ex}");
                        }
                    }
                }

            }

            // Automatically removes all blocks with a given removeSubtypeName
            public void RemoveSpecific(string removeSubtypeName, ref Tuple<CubeGridRenderCellGrid, BlueprintCellGridManager.GridGeometry, GridCursor, SKElement> CurrentRenderEntry)
            {
                BlueprintCell[,,] CubeblocksArray = CurrentRenderEntry.Item1.GetBlueprintCell3DArrayRef();

                string partNameIterator;

                foreach (BlueprintCell block in CubeblocksArray)
                {
                    if (block == null || block.GetCubeblockDefinition() == null)
                    {
                        continue;
                    }
                    else
                    {
                        partNameIterator = block.GetCubeblockDefinition().GetSubtypeName();
                    }

                    if (partNameIterator.Equals(removeSubtypeName))
                    {
                        block.Delete();

                        this._TransactionLogEntry = new TransactionLogEntry(LogEntryTypes.LogEntryMessage, $"Deleting block: " + partNameIterator + "\n");
                        this._TransactionLog.AddLogEntry(_TransactionLogEntry);

                        this._UIRect = block.GetUIRect();
                        this._UIRect.Inflate(-10, -10);

                        block.AddUIRectangle(new Tuple<string, SKRectI, SKPaint>(this._OperationName, this._UIRect, this._UIPaint));
                    }
                }
                CurrentRenderEntry.Item1.GetBlueprintCell3DArrayRef() = CubeblocksArray;

            }

            TransactionLog PartswapOperation.PerformOperation(ref Tuple<CubeGridRenderCellGrid, BlueprintCellGridManager.GridGeometry, GridCursor, SKElement> currentCubegridEntry)
            {
                switch (_OperationParameters["Operation"])
                {
                    case "Remove_Armor":
                        switch (_OperationParameters["Remove_Armor"])
                        {
                            case "Light":
                                this.Remove_Armor(false, true, currentCubegridEntry);
                                break;
                            case "Heavy":
                                this.Remove_Armor(true, false, currentCubegridEntry);
                                break;
                            case "All":
                                this.Remove_Armor(true, true, currentCubegridEntry);
                                break;
                            case null:
                                throw new ArgumentException("Operation Remove_Armor needs to have parameter Remove_Armor set!");
                        }
                        break;
                    case "RemoveAllExceptArmor":
                        this.RemoveAllExceptArmor(currentCubegridEntry);
                        break;
                    case "RemoveAllExcept":
                        if (_OperationParameters["RemoveAllExcept"] != null)
                        {
                            this.RemoveAllExcept(_OperationParameters["RemoveAllExcept"] as string, ref currentCubegridEntry);
                        }
                        else
                        {
                            throw new ArgumentException("Operation RemoveAllExcept needs to have parameter RemoveAllExcept set!");
                        }
                        break;
                    case "RemoveSpecific":
                        if (_OperationParameters["RemoveSpecific"] != null)
                        {
                            this.RemoveSpecific(_OperationParameters["RemoveSpecific"] as string, ref currentCubegridEntry);
                        }
                        else
                        {
                            throw new ArgumentException("Operation RemoveSpecific needs to have parameter RemoveSpecific set!");
                        }
                        break;
                    case null:
                        throw new ArgumentException("PerformOperation needs to have parameter Operation set!");
                }
                return this._TransactionLog;
            }

            public RemoveTool(string operation)
            {
                this._TransactionLog = new TransactionLog(this._OperationName);
                _OperationParameters.Add("Operation", operation);
            }

            public RemoveTool()
            {
                this._TransactionLog = new TransactionLog(this._OperationName);
            }
        }

        public class PartSwap : PartswapOperation
        {
            private bool _DebugMode = false;
            private string _OperationName => "PartSwap";

            private Dictionary<string, object> _OperationParameters = new Dictionary<string, object>();
            private Dictionary<string, Type> _RecognizedParameters = new Dictionary<string, Type> { { "Operation", typeof(string) }, { "PartSwap", typeof(string) }, { "OldPart", typeof(string) }, { "NewPart", typeof(string) } };
            private SKRectI _UIRect = new SKRectI();
            private SKPaint _UIPaint = new SKPaint();
            private SKColor _UIPaintColor = new SKColor();
            private TransactionLog _TransactionLog;
            private TransactionLogEntry _TransactionLogEntry;

            string PartswapOperation.OperationName => _OperationName;
            Dictionary<string, object> PartswapOperation.OperationParameters { get => _OperationParameters; set => this._OperationParameters = value; }
            Dictionary<string, Type> PartswapOperation.RecognizedParameters => _RecognizedParameters;
            SKRectI PartswapOperation.UIRect { get => _UIRect; set => _UIRect = value; }
            SKPaint PartswapOperation.UIPaint { get => _UIPaint; set => _UIPaint = value; }
            SKColor PartswapOperation.UIPaintColor { get => _UIPaintColor; set => _UIPaintColor = value; }
            TransactionLog PartswapOperation.TransactionLog { get => _TransactionLog; set => _TransactionLog = value; }
            bool PartswapOperation.DebugMode { get => _DebugMode; set => _DebugMode = value; }

            TransactionLog PartswapOperation.PerformOperation(ref Tuple<CubeGridRenderCellGrid, BlueprintCellGridManager.GridGeometry, GridCursor, SKElement> currentCubegridEntry)
            {
                switch (_OperationParameters["Operation"])
                {
                    case "PartSwap":
                        switch (_OperationParameters["OldPart"])
                        {
                            case null:
                                throw new ArgumentException("Operation PartSwap needs to have parameter OldPart set!");
                            default:
                                switch (_OperationParameters["NewPart"])
                                {
                                    case null:
                                        throw new ArgumentException("Operation PartSwap needs to have parameter NewPart set!");
                                    default:

                                        BlueprintCell[,,] cubeblocks = currentCubegridEntry.Item1.GetBlueprintCell3DArrayRef();

                                        foreach (BlueprintCell block in cubeblocks)
                                        {
                                            if (block == null || block.GetCubeblockDefinition() == null)
                                            {
                                                continue;
                                            }

                                            if (block.GetCubeblockDefinition().SubtypeName.Equals(_OperationParameters["OldPart"]))
                                            {
                                                block.GetCubeblockDefinition().SetSubtypeName(_OperationParameters["NewPart"] as string);

                                                this._UIRect = block.GetUIRect();
                                                this._UIRect.Inflate(-10, -10);

                                                block.AddUIRectangle(new Tuple<string, SKRectI, SKPaint>(this._OperationName, this._UIRect, this._UIPaint));

                                                this._TransactionLogEntry = new TransactionLogEntry(LogEntryTypes.LogEntryMessage, $"{this._OperationName}: Swapping block at position {block.GetCubeblockDefinition().Min}, SubtypeName: {_OperationParameters["OldPart"]} with new type: {_OperationParameters["NewPart"]}");
                                                this._TransactionLog.AddLogEntry(_TransactionLogEntry);

                                            }

                                        }
                                        currentCubegridEntry.Item1.GetBlueprintCell3DArrayRef() = cubeblocks;

                                        break;
                                }
                                break;
                        }
                        break;
                    case null:
                        throw new ArgumentException("PerformOperation needs to have parameter Operation set!");
                }
                return this._TransactionLog;
            }

            public PartSwap(string operation)
            {
                this._TransactionLog = new TransactionLog(this._OperationName);

                _OperationParameters.Add("Operation", operation);

                this._UIRect = new SKRectI();
                this._UIPaint = new SKPaint();
                this._UIPaintColor = SKColors.HotPink;

                this._UIPaint.Color = this._UIPaintColor;
            }

            public PartSwap()
            {
                this._TransactionLog = new TransactionLog(this._OperationName);

                this._UIRect = new SKRectI();
                this._UIPaint = new SKPaint();
                this._UIPaintColor = SKColors.HotPink;

                this._UIPaint.Color = this._UIPaintColor;
            }
        }

    }

    public class TransactionLog
    {
        public TransactionLogEntry LastLogMessage { get => _TransactionLog.LastOrDefault(); }

        //public Queue<TransactionLogEntry>  TransactionLogActual { get => _TransactionLog; }

        private Queue<TransactionLogEntry> _TransactionLog = new Queue<TransactionLogEntry>();

        public DateTime LogOpenedDateTime { get => _LogOpenedDateTime; }
        public DateTime LogClosedDateTime { get => _LogClosedDateTime; }

        private DateTime _LogOpenedDateTime;
        private DateTime _LogClosedDateTime;

        private string _LogSource;

        public enum LogEntryTypes
        {
            LogEntryMessage = 0,
            GenericPayload = 1
        }

        public TransactionLog(string Source)
        {
            this._LogSource = Source;
            this._LogOpenedDateTime = DateTime.Now;

            TransactionLogEntry InitialEntry = new TransactionLogEntry(LogEntryTypes.LogEntryMessage, $"// Transaction Log Initialized at {this._LogOpenedDateTime} from source {Source}//");

            this._TransactionLog.Enqueue(InitialEntry);
        }

        public class TransactionLogEntry : IComparable
        {
            public LogEntryTypes LogEntryType { get => this._EntryType; }
            public string LogEntryText { get => this._LogEntryText; }
            public DateTime LogEntryTimestamp { get => this._LogEntryDateTime; }
            public object? LogEntryArtifact { get => this._Artifact; }

            private LogEntryTypes _EntryType;
            private string _LogEntryText;
            private DateTime _LogEntryDateTime;
            private object? _Artifact;

            public TransactionLogEntry(LogEntryTypes entryType, string logEntryText = "//NO LOG ENTRY MESSAGE//", object? artifact = null)
            {
                this._EntryType = entryType;
                this._LogEntryText = logEntryText;
                this._Artifact = artifact;
                this._LogEntryDateTime = DateTime.Now;
            }

            public override int GetHashCode()
            {
                return _LogEntryDateTime.GetHashCode() + _EntryType.GetHashCode();
            }

            public override bool Equals(object? obj)
            {
                if (obj == null) return false;
                if (obj is not TransactionLogEntry) return false;

                TransactionLogEntry transactionLogEntry = (TransactionLogEntry)obj;

                if (this._LogEntryText.Equals(transactionLogEntry._LogEntryText) &&
                   this._LogEntryDateTime.Equals(transactionLogEntry._LogEntryDateTime) &&
                   this._EntryType == transactionLogEntry._EntryType)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            public override string ToString()
            {
                return $"{_LogEntryDateTime}: {_LogEntryText} -> {(_Artifact == null ? "No artifact!" : _Artifact.ToString())}";
            }

            int IComparable.CompareTo(object? obj)
            {
                if (obj == null) throw new InvalidDataException("TransactionLogEntry CompareTo failed: Object is null!");
                if (obj is not TransactionLogEntry) throw new InvalidDataException("CompareTo failed: Object is not TransactionLogEntry!");

                TransactionLogEntry transactionLogEntry = (TransactionLogEntry)obj;

                return this.LogEntryTimestamp.CompareTo(transactionLogEntry.LogEntryTimestamp);
            }
        }

        // Merges this log and the input log by adding the input log's transactions to this.TransactionLogActual
        public void Merge(TransactionLog transactionLog)
        {
            foreach (TransactionLogEntry logEntry in transactionLog.GetOrderedLogList())
            {
                this._TransactionLog.Enqueue(logEntry);
            }

            this._TransactionLog.Order();
        }

        public int GetLogEntryCount()
        {
            return this._TransactionLog.Count;
        }

        public void CloseLog()
        {
            TransactionLogEntry ClosingEntry = new TransactionLogEntry(LogEntryTypes.LogEntryMessage, $"// Transaction Log Initialized at {this._LogOpenedDateTime} from source: {this._LogSource} //");

            this._LogClosedDateTime = DateTime.Now;
            this._TransactionLog.Enqueue(ClosingEntry);
        }

        public bool AddLogEntry(TransactionLogEntry logEntry)
        {
            try
            {
                this._TransactionLog.Enqueue(logEntry);
                return true;
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Error thrown while entering Log Entry:\n{ex}\n");
                return false;
            }
        }

        public bool AddLogEntry(LogEntryTypes logEntryType, string LogEntryText, object? logEntryArtifact = null)
        {
            TransactionLogEntry AddedEntry = new TransactionLogEntry(logEntryType, LogEntryText, logEntryArtifact);

            try
            {
                this._TransactionLog.Enqueue(AddedEntry);
                return true;
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Error thrown while entering Log Entry:\n{ex}\n");
                return false;
            }
        }

        public bool LogHasEntries()
        {
            if (this._TransactionLog.Count > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public TransactionLogEntry? GetLatestLogEntry()
        {
            if (this._TransactionLog != null &&
                this._TransactionLog.Count > 0)
            {
                return this._TransactionLog.Peek();
            }
            else
            {
                return null;
            }
        }

        public bool LogHasArtifacts()
        {
            foreach (TransactionLogEntry logEntry in this._TransactionLog)
            {
                // Check if any of the entries have an artifact. Skip if not.
                if (logEntry.LogEntryArtifact != null)
                {
                    return true;
                }
                else
                {
                    continue;
                }
            }
            return false;
        }

        public List<TransactionLogEntry>? GetOrderedLogList()
        {
            if (this._TransactionLog == null)
            {
                return null;
            }


            return this._TransactionLog.Order().ToList();
        }

        public TransactionLogEntry DequeueOne()
        {
            return this._TransactionLog.Dequeue();
        }
    }
}


