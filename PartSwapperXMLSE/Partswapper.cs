using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using System.Xml;
using System.Xml.Linq;
using System.Windows;
using System.IO;
using System.Text;
using Microsoft.VisualBasic.FileIO;
using System.Numerics;
using System.Text.RegularExpressions;
using System;
using System.Runtime.CompilerServices;
using System.Configuration;
using System.Collections.Specialized;
using System.Security.Policy;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace PartSwapperXMLSE
{
    public class PartSwapper
    {
        // Cubegrid variables
        private string _cubegridSBCfilename;
        private string _cubegridName;

        // The 'root' of an actual blueprint. 26JAN2023: _shipRoot will only be one thing: The root node of the XML document.
        private XElement? _shipRoot;

        // Current cubegrid is what we will use to edit different CubegridsXElementList
        private XElement? _currentCubegridXElement;

        // declarations for collections of ship parts and their category names, as well as modblocks
        private Dictionary<string, List<XElement>> _shipParts = new Dictionary<string, List<XElement>>();

        // Working list of available blockVariant variants
        private Dictionary<string, HashSet<string>> _blockVariantsAvail;

        private Dictionary<string, List<XElement>> _shipGroups = new Dictionary<string, List<XElement>>();

        // Session Variables
        private bool _debug = false;
        private bool _GUIMode = false;

        private string STCArmorModNumber = "3058263463";
        private string _seFolderPath = "";
        private string _seModFolderPath = "";

        //private string _STC_WorkshopDir_DataDirpath = "E:\\SteamLibrary\\steamapps\\workshop\\content\\244850\\2877712858\\Data";

        // Constructor for TUI Partswapper class
        public PartSwapper(string sbcFilename, bool debug)
        {
            _debug = debug;
            _cubegridSBCfilename = sbcFilename;
            _shipRoot = LoadShipRootXElement(sbcFilename, _debug);
            _currentCubegridXElement = GetCubegridTUI(sbcFilename, _debug);
            _cubegridName = _shipRoot.Element("ShipBlueprints").Element("ShipBlueprint").Element("Id").Attribute("Subtype").Value;

            _GUIMode = false;

            if (_shipRoot == null)
            {
                throw new ArgumentException("_shipRoot is null! Assuming bad arguments to SelectWorkingBlueprint!");
            }

            _shipParts = GenerateShipPartsFromXElement(_currentCubegridXElement, debug);

            _blockVariantsAvail = LoadBlockVariantsDict();
            _shipGroups = GetShipGroups();
        }

        // Constructor for GUI PartSwapper
        public PartSwapper(string sbcFilename, string seFolderPath, string seModFolderPath, bool debug)
        {
            _debug = debug;
            _cubegridSBCfilename = sbcFilename;
            _shipRoot = LoadShipRootXElement(sbcFilename, _debug);
            _GUIMode = true;
            _seFolderPath = seFolderPath;
            _seModFolderPath = seModFolderPath;

            // currentCubeGrid will need to be populated when the user selects the cubegrid
            // via the GUI
            _currentCubegridXElement = null;

            // Populate initially with defaults
            _blockVariantsAvail = LoadDefaultBlockVariantsDictWPFSelected(seFolderPath);

            Dictionary<string, HashSet<string>> modVariants = LoadModDirsBlockVariantsDictWPFSelected(seModFolderPath);

            foreach (string categoryKey in modVariants.Keys)
            {

                foreach (string variant in modVariants[categoryKey])
                {
                    // If the current _bloackVariantsAvail contains the current categoryKey...
                    if (_blockVariantsAvail.Keys.Contains(categoryKey))
                    {
                        // If the current category contains our variant in the _blockVariantsAvail...
                        if (_blockVariantsAvail[categoryKey].Contains(variant))
                        {
                            // Do nothing!
                            continue;
                        }
                        else
                        {
                            // But if the category exists, and the variant is not in there - add it!
                            _blockVariantsAvail[categoryKey].Add(variant);
                        }
                    }
                    else
                    {
                        // If the category does not exist in _blockVariantsAvail...

                        // Assign it!
                        _blockVariantsAvail[categoryKey] = modVariants[categoryKey];
                    }
                }
            }
        }

        public XElement? GetShipRoot()
        {
            return _shipRoot;
        }

        public Dictionary<string, List<XElement>>? GetShipParts()
        {
            return _shipParts;
        }

        public List<XElement>? GetShipPartsFlattened()
        {
            List<XElement> result = new List<XElement>();

            if (_shipParts == null)
            {
                return null;
            }
            else
            {
                foreach (string key in _shipParts.Keys)
                {
                    foreach (XElement part in _shipParts[key])
                    {
                        result.Add(part);
                    }
                }
            }
            return result;
        }

        public Dictionary<string, HashSet<string>> GetBlockVariantsAvail()
        {
            return _blockVariantsAvail;
        }

        public XElement GetCurrentCubegrid()
        {
            return _currentCubegridXElement;
        }


        public bool SetCurrentCubegrid(XElement cubegrid)
        {
            // Simple null-guard. Don't know when this would happen.
            // Keeping the structure here in case we wanna filter by anything else.
            if (cubegrid == null && cubegrid.Element("_DisplayName").Value != null)
            {
                return false;
            }
            else
            {
                _currentCubegridXElement = cubegrid;
                _cubegridName = _shipRoot.Element("ShipBlueprints").Element("ShipBlueprint").Element("Id").Attribute("Subtype").Value;
                return true;
            }
        }

        // Creates a backup of the XML document
        public void BackupShipXML()
        {
            XmlDocument xmlDoc = new XmlDocument();

            XmlWriterSettings xwrSettings = new XmlWriterSettings();

            bool debug = false;

            string currentDirectory = Directory.GetCurrentDirectory();
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmm");
            string BackupFilename = _cubegridSBCfilename + timestamp + "pps_bp_bak.sbc";

            // Long filepath not necessary. We use relative.
            //string FilePath = Path.Combine(currentDirectory, BackupFilename);


            if (debug)
            {
                Console.WriteLine($"DEBUG: BackupShipXML sbcFilename input is: {_cubegridSBCfilename}");
            }


            using (XmlReader xRead = XmlReader.Create(_cubegridSBCfilename))
            {
                xmlDoc.Load(xRead);
            }

            xwrSettings.IndentChars = "\t";
            xwrSettings.NewLineHandling = NewLineHandling.Entitize;
            xwrSettings.Indent = true;
            xwrSettings.NewLineChars = "\n";

            timestamp = DateTime.Now.ToString("yyyyMMdd_HHmm");
            BackupFilename = _cubegridSBCfilename + timestamp + "pps_bp_bak.sbc";

            using (XmlWriter xWrite = XmlWriter.Create(BackupFilename, xwrSettings))
            {
                xmlDoc.Save(xWrite);
            }
        }

        public Dictionary<string, List<XElement>> GetShipGroups()
        {
            string cubegridName = _shipRoot.Element("ShipBlueprints").Element("ShipBlueprint").Element("Id").Attribute("Subtype").Value;

            Dictionary<string, List<XElement>> result = new Dictionary<string, List<XElement>>();
            XElement cubeGrids = _shipRoot.Element("ShipBlueprints").Element("ShipBlueprint").Element("CubeGrids");
            List<XElement> cubeGridsList = cubeGrids.Elements().ToList();
            List<XElement>? groupsIter = null;

            foreach (XElement ship in cubeGridsList)
            {
                // Null-check here. Note: We keep the groupsIter 'null' while iterating in order to discern when a group definition is present or not
                if (ship.Element("BlockGroupsDictionary") != null)
                {
                    groupsIter = ship.Element("BlockGroupsDictionary").Elements().ToList();
                }
                else
                {
                    groupsIter = null;
                }

                if (groupsIter != null)
                {
                    result[cubegridName] = groupsIter;
                }
                else
                {
                    Trace.WriteLine($"Found cubegrid without groups:{cubegridName}");
                }
            }

            return result;
        }

        // Needs the currentCubeGrid to be populated before use.
        // Will throw an error if this is not the case.
        // Will set shipParts to 'null' if it can't get a reasonable list (list with at least 1 blockVariant)
        public bool GenerateShipPartsList()
        {
            string partsListBlockTypeName;

            bool debug = false;

            if (_currentCubegridXElement == null)
            {
                // Why this throw? Well - if the _currentCubegridXElement is not populated, and someone is making calls to grid-specific stuff: I want that error thrown sooner than later.
                // _currentCubegridXElement should always be populated in the cases where someone is making calls that enumerates the tree.
                throw new InvalidOperationException("_currentCubegridXElement not populated!");
            }

            Dictionary<string, List<XElement>> shipPartsList = new();
            List<XElement> partsList;
            XElement? cubeBlocks = _currentCubegridXElement.Element("_CubeBlocksXElement");

            Console.WriteLine($"Found ship:{_currentCubegridXElement.Element("_DisplayName").Value}");

            foreach (XElement block in cubeBlocks.Elements())
            {
                // Get the SubTypeName of the blockVariant, aka: blockVariant type name, or whatever. 
                partsListBlockTypeName = block.Element("SubtypeName").Value;

                // Catch situation where the subtypeName is blank
                if (partsListBlockTypeName.Equals(""))
                {
                    partsListBlockTypeName = block.FirstAttribute.Value;
                }

                // Use the blockname to check if the categoryKey exists in the shippartslist already. If so: Update from a temp list we create.
                if (shipPartsList.ContainsKey(partsListBlockTypeName))
                {

                    partsList = shipPartsList[partsListBlockTypeName];

                    partsList.Add(block);

                    // Add the part to the ShipPartsList via categoryKey: Part name, Value: XElement representing that part
                    shipPartsList[partsListBlockTypeName] = partsList;
                }
                else
                {
                    // If the categoryKey does *not* exist (Part is not currently in ShipPartsList) - simply create the first/new list with the XElement already added, and add to the dict.
                    partsList = new List<XElement>() { block };

                    shipPartsList.Add(partsListBlockTypeName, partsList);
                }
            }

            //Iterate through each blockVariant, creating the lists (if necessary) to populate a string:List<Xelement> dictionary that will comprise the catalogue of parts in a ship.
            if (debug)
            {
                Console.WriteLine($"DEBUG: ShipPartsList resultLog is:\n{shipPartsList}");
            }


            if (debug)
            {
                foreach (string type in shipPartsList.Keys)
                {
                    Console.WriteLine($"DEBUG:\nkey: {type},\ncount: {shipPartsList[type].Count()}.");
                    //Console.WriteLine($"categoryKey: {type}, Index: {resultLog[type].Count()}, value: {resultLog[type][0]}");
                }
            }

            // Our measure of "success" here is whether the shipPartsList has more than one entry
            if (shipPartsList.Count > 0)
            {
                _shipParts = shipPartsList;

                return true;
            }
            else
            {
                //...and we assume "failure" is an empty list. Who would make a cubegrid with no blocks in it?
                return false;
            }
        }

        public bool DeleteSBC5File()
        {
            string sbc5Path = "";

            try
            {
                // Delete the associated sbcB5 file
                sbc5Path = Path.Combine(Path.GetDirectoryName(_cubegridSBCfilename), Path.GetFileNameWithoutExtension(_cubegridSBCfilename) + ".sbcB5");
                File.Delete(sbc5Path);
                return true;
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Failed to delete SBC5 file!\nYou're gonna need to do that in order to see your grid changes...\nError was:\n{ex}");
            }
            return false;

        }

        // 27JAN2024: New instance method for partswapping.
        // is pretty crude, and is O(n), but it should work.
        // We're just swapping strings.
        public int PartswapViaSubtypeName(string oldPart, string newPart, bool debug)
        {
            int numPartsReplaced = 0;
            bool nonSubtypeName = false;
            string sbc5Path = "";

            XElement cubeGrid;

            // Check if our _currentCubegridXElement variable is null. We'll be using it!
            if (_currentCubegridXElement == null)
            {
                throw new InvalidDataException("cubeGrid assignment failed!\n_currentCubegrid is null!");

            }
            else
            {
                // If currentCubegridXElement is not null - assign it as our local cubegrid element!
                cubeGrid = _currentCubegridXElement;
                _cubegridName = _shipRoot.Element("ShipBlueprints").Element("ShipBlueprint").Element("Id").Attribute("Subtype").Value;
            }

            // Edge case where we recieve an empty newPart name. Do nothing!
            if (newPart.Equals(""))
            {
                throw new Exception("newPart was blank! This should never happen!\n");
            }

            // Edge case where we are given a non-subtype name.
            // Currently: We are expecting strings that start with MyObjectBuilder_
            // Determine if we are looking at a normal subtypeName, or a MyObjectBuilder name
            if (oldPart.StartsWith("MyObjectBuilder_"))
            {
                nonSubtypeName = true;

                // If we have found an oldPart MyObjectBuilder_ name, immediately check if the newpart is named appropriately

                if (!newPart.StartsWith("MyObjectBuilder_"))
                {
                    throw new Exception($"Non-subtype oldPart:\n{oldPart}\n...does not have an appropriate replacement newPart:\n{newPart}\n");
                }

            }
            else
            {
                nonSubtypeName = false;
            }

            // Backup the XML of the ship
            BackupShipXML();

            foreach (XElement block in cubeGrid.Element("_CubeBlocksXElement").Elements())
            {
                if (nonSubtypeName)
                {
                    if (block.FirstAttribute.Value == oldPart)
                    {
                        block.FirstAttribute.Value = newPart;
                        numPartsReplaced++;
                    }
                }
                else
                {
                    if (block.Element("SubtypeName").Value == oldPart)
                    {
                        block.Element("SubtypeName").Value = newPart;
                        numPartsReplaced++;
                    }
                }

            }

            // Overwrite the live document
            if (_shipRoot == null)
            {
                throw new InvalidDataException("_shipRoot was null when trying to save!");
            }
            else
            {
                // Delete the associated sbcB5 file
                sbc5Path = Path.Combine(Path.GetDirectoryName(_cubegridSBCfilename), Path.GetFileNameWithoutExtension(_cubegridSBCfilename) + ".sbcB5");
                File.Delete(sbc5Path);

                // Then save the blueprint
                _shipRoot.Save(_cubegridSBCfilename);
            }

            // Update our internal ShipParts list
            _shipParts = GenerateShipPartsFromXElement(_currentCubegridXElement, debug);

            return numPartsReplaced;
        }

        public static bool s_isPartnameTeched(string partname)
        {
            if (partname.EndsWith("2x") || partname.EndsWith("4x") || partname.EndsWith("8x"))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public List<string> SimilarParts(string similarTo)
        {
            List<string> result = new List<string>();
            char[] similarToArr = similarTo.ToCharArray();
            char currChar;
            int upperCounter = 0;
            int thirdUpperIndex = -1;
            string substring;
            string currentBlockname;

            if (similarToArr == null || similarToArr.Length == 0)
            {
                //throw new Exception("SimilarParts unable to generate array!");
                result.Add("Invalid blockname!");
                return result;
            }

            if (s_isPartnameTeched(similarTo))
            {
                similarTo = similarTo.Remove(similarTo.Length - 2);
                similarToArr = similarTo.ToCharArray();

            }

            // Iterate backwards to find the index of the third-last uppercase letter
            for (int i = similarToArr.Length - 1; i >= 0; i--)
            {
                currChar = similarToArr[i];

                if (char.IsUpper(currChar))
                {
                    upperCounter++;
                }

                if (upperCounter == 3)
                {
                    // we found the third-upper index! Use this to get the substring!
                    thirdUpperIndex = i;
                    break;
                }
            }

            // at this point - if we haven't found a third index: It ain't happening.
            if (thirdUpperIndex == -1)
            {
                //MessageBox.Show("Similar Parts: Failed to find third index!");
                result.Add($"Unable to find blocks similar to {similarTo}");
                return result;
            }

            substring = similarTo.Substring(thirdUpperIndex);

            // Now that we have the substring we 
            foreach (string blockVariantGroup in _blockVariantsAvail.Keys)
            {

                foreach (string blockVariant in _blockVariantsAvail[blockVariantGroup])
                {
                    if (blockVariant.Contains(substring) || blockVariant.Contains(similarTo))
                    {
                        result.Add(blockVariant);
                    }
                }
            }

            return result;
        }

        public List<string> STC_AutoArmor_To_Tritanium()
        {
            // Return a list of strings that represents a log of all the blocks swapped.
            List<string> result = new List<string>();

            string stcArmorVariantGroupName = "STC_Tritanium_Armor";

            // An entry for every variant of tritanium armor naming scheme.
            string tritArmorVariantName1;
            string tritArmorVariantName2;
            string tritArmorVariantName3;
            string tritArmorVariantName4;
            string tritArmorVariantName5;
            string tritArmorVariantName6;
            string tritArmorVariantName7;
            string tritArmorVariantName8;

            string partNameIterator;
            string partNameIteratorModified;

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

            int heavyWordIndex;

            HashSet<string> STC_Armor = new HashSet<string>();

            // Populate all the STC armor we can find...
            foreach (string blockVarKey in _blockVariantsAvail.Keys)
            {
                if (blockVarKey.Contains("STC_Tritanium"))
                {
                    foreach (string blockVariant in _blockVariantsAvail[blockVarKey])
                    {
                        STC_Armor.Add(blockVariant);
                    }
                }
            }

            // Throw error if we cannot find armor.
            if (STC_Armor.Count == 0)
            {
                throw new Exception("Unable to find any STC Armor!");
            }

            // iterate through each part on the ship,
            // figure out if it's an armor block,
            // if yes: is it tritanium?
            // if trit: ignore
            // if not-trit: convert
            foreach (string blockVariant in _shipParts.Keys)
            {
                List<XElement> parts = _shipParts[blockVariant];

                foreach (XElement part in parts)
                {

                    partNameIterator = part.Element("SubtypeName").Value;

                    isTritaniumArmorBlock = part.Element("SubtypeName").Value.Contains("Tritanium");

                    isArmorBlock = partNameIterator.Contains("Armor");
                    isHeavyArmorBlock = part.Element("SubtypeName").Value.Contains("Heavy");
                    isPanel = part.Element("SubtypeName").Value.Contains("Panel");
                    isBeam = part.Element("SubtypeName").Value.Contains("Beam");

                    tritArmorVariantName1 = "Tritanium_" + partNameIterator;
                    tritArmorVariantName2 = partNameIterator + "Tritanium";
                    tritArmorVariantName3 = partNameIterator.Replace("Heavy", "Tritanium");
                    tritArmorVariantName4 = partNameIterator.Replace("Light", "Tritanium");
                    tritArmorVariantName5 = "Tritanium_" + partNameIterator.Replace("Heavy", "");
                    tritArmorVariantName6 = partNameIterator.Replace("Heavy", "") + "Tritanium";
                    tritArmorVariantName7 = "Tritanium_" + partNameIterator.Replace("Light", "");
                    tritArmorVariantName8 = partNameIterator.Replace("Light", "") + "Tritanium";

                    // EXISTING TRIT ARMOR
                    if (isTritaniumArmorBlock)
                    {
                        Trace.WriteLine($"Found tritanium armor:\n----\n{blockVariant}\n----\nSKIPPING!\n");
                        blockChanged = false;
                        continue;
                    }

                    // VANILLA BEAM
                    if (isBeam)
                    {
                        Trace.WriteLine($"Found armor block eligible for replacing:\n----\n{partNameIterator}\n----\nSWAPPING!");


                        // if the first variant of trit name is present - convert.
                        if (STC_Armor.Contains(tritArmorVariantName1))
                        {
                            part.Element("SubtypeName").Value = tritArmorVariantName1;
                            blockChanged = true;
                            result.Add($"Replaced {partNameIterator} with {tritArmorVariantName1}");
                            Trace.WriteLine($"Replaced {partNameIterator} with {tritArmorVariantName1}");
                            continue;
                        }

                        // if the second variant of trit name is present - convert.
                        if (STC_Armor.Contains(tritArmorVariantName2))
                        {
                            part.Element("SubtypeName").Value = tritArmorVariantName2;
                            blockChanged = true;
                            result.Add($"Replaced {partNameIterator} with {tritArmorVariantName2}");
                            Trace.WriteLine($"Replaced {partNameIterator} with {tritArmorVariantName2}");
                            continue;
                        }
                        // if the third variant of trit name is present - convert.
                        if (STC_Armor.Contains(tritArmorVariantName3))
                        {
                            part.Element("SubtypeName").Value = tritArmorVariantName3;
                            blockChanged = true;
                            result.Add($"Replaced {partNameIterator} with {tritArmorVariantName3}");
                            Trace.WriteLine($"Replaced {partNameIterator} with {tritArmorVariantName3}");
                            continue;
                        }

                        // if the fourth variant of trit name is present - convert.
                        if (STC_Armor.Contains(tritArmorVariantName4))
                        {
                            part.Element("SubtypeName").Value = tritArmorVariantName4;
                            blockChanged = true;
                            result.Add($"Replaced {partNameIterator} with {tritArmorVariantName4}");
                            Trace.WriteLine($"Replaced {partNameIterator} with {tritArmorVariantName4}");
                            continue;
                        }

                        // if the fifth variant of trit name is present - convert.
                        if (STC_Armor.Contains(tritArmorVariantName5))
                        {
                            part.Element("SubtypeName").Value = tritArmorVariantName5;
                            blockChanged = true;
                            result.Add($"Replaced {partNameIterator} with {tritArmorVariantName5}");
                            Trace.WriteLine($"Replaced {partNameIterator} with {tritArmorVariantName5}");
                            continue;
                        }

                        // if the sixth variant of trit name is present - convert.
                        if (STC_Armor.Contains(tritArmorVariantName6))
                        {
                            part.Element("SubtypeName").Value = tritArmorVariantName6;
                            blockChanged = true;
                            result.Add($"Replaced {partNameIterator} with {tritArmorVariantName6}");
                            Trace.WriteLine($"Replaced {partNameIterator} with {tritArmorVariantName6}");
                            continue;
                        }

                        // if the seventh variant of trit name is present - convert.
                        if (STC_Armor.Contains(tritArmorVariantName7))
                        {
                            part.Element("SubtypeName").Value = tritArmorVariantName7;
                            blockChanged = true;
                            result.Add($"Replaced {partNameIterator} with {tritArmorVariantName7}");
                            Trace.WriteLine($"Replaced {partNameIterator} with {tritArmorVariantName7}");
                            continue;
                        }

                        // if the eight variant of trit name is present - convert.
                        if (STC_Armor.Contains(tritArmorVariantName8))
                        {
                            part.Element("SubtypeName").Value = tritArmorVariantName8;
                            blockChanged = true;
                            result.Add($"Replaced {partNameIterator} with {tritArmorVariantName8}");
                            Trace.WriteLine($"Replaced {partNameIterator} with {tritArmorVariantName8}");
                            continue;
                        }

                        // We 'continue' in order to avoid falling into another case. Similar to a switch!
                        continue;
                    }

                    // VANILLA PANEL
                    if (isPanel)
                    {
                        Trace.WriteLine($"Found armor block eligible for replacing:\n----\n{partNameIterator}\n----\nSWAPPING!");

                        // if the first variant of trit name is present - convert.
                        if (STC_Armor.Contains(tritArmorVariantName1))
                        {
                            part.Element("SubtypeName").Value = tritArmorVariantName1;
                            blockChanged = true;
                            result.Add($"Replaced {partNameIterator} with {tritArmorVariantName1}");
                            Trace.WriteLine($"Replaced {partNameIterator} with {tritArmorVariantName1}");
                            continue;
                        }

                        // if the second variant of trit name is present - convert.
                        if (STC_Armor.Contains(tritArmorVariantName2))
                        {
                            part.Element("SubtypeName").Value = tritArmorVariantName2;
                            blockChanged = true;
                            result.Add($"Replaced {partNameIterator} with {tritArmorVariantName2}");
                            Trace.WriteLine($"Replaced {partNameIterator} with {tritArmorVariantName2}");
                            continue;
                        }


                        // if the third variant of trit name is present - convert.
                        if (STC_Armor.Contains(tritArmorVariantName3))
                        {
                            part.Element("SubtypeName").Value = tritArmorVariantName3;
                            blockChanged = true;
                            result.Add($"Replaced {partNameIterator} with {tritArmorVariantName3}");
                            Trace.WriteLine($"Replaced {partNameIterator} with {tritArmorVariantName3}");
                            continue;
                        }

                        // if the fourth variant of trit name is present - convert.
                        if (STC_Armor.Contains(tritArmorVariantName4))
                        {
                            part.Element("SubtypeName").Value = tritArmorVariantName4;
                            blockChanged = true;
                            result.Add($"Replaced {partNameIterator} with {tritArmorVariantName4}");
                            Trace.WriteLine($"Replaced {partNameIterator} with {tritArmorVariantName4}");
                            continue;
                        }

                        // if the fifth variant of trit name is present - convert.
                        if (STC_Armor.Contains(tritArmorVariantName5))
                        {
                            part.Element("SubtypeName").Value = tritArmorVariantName5;
                            blockChanged = true;
                            result.Add($"Replaced {partNameIterator} with {tritArmorVariantName5}");
                            Trace.WriteLine($"Replaced {partNameIterator} with {tritArmorVariantName5}");
                            continue;
                        }

                        // if the sixth variant of trit name is present - convert.
                        if (STC_Armor.Contains(tritArmorVariantName6))
                        {
                            part.Element("SubtypeName").Value = tritArmorVariantName6;
                            blockChanged = true;
                            result.Add($"Replaced {partNameIterator} with {tritArmorVariantName6}");
                            Trace.WriteLine($"Replaced {partNameIterator} with {tritArmorVariantName6}");
                            continue;
                        }

                        // if the seventh variant of trit name is present - convert.
                        if (STC_Armor.Contains(tritArmorVariantName7))
                        {
                            part.Element("SubtypeName").Value = tritArmorVariantName7;
                            blockChanged = true;
                            result.Add($"Replaced {partNameIterator} with {tritArmorVariantName7}");
                            Trace.WriteLine($"Replaced {partNameIterator} with {tritArmorVariantName7}");
                            continue;
                        }

                        // if the eight variant of trit name is present - convert.
                        if (STC_Armor.Contains(tritArmorVariantName8))
                        {
                            part.Element("SubtypeName").Value = tritArmorVariantName8;
                            blockChanged = true;
                            result.Add($"Replaced {partNameIterator} with {tritArmorVariantName8}");
                            Trace.WriteLine($"Replaced {partNameIterator} with {tritArmorVariantName8}");
                            continue;
                        }
                        // We 'continue' in order to avoid falling into another case. Similar to a switch!
                        continue;
                    }

                    // GENERIC ARMOR
                    if (isArmorBlock)
                    {
                        Trace.WriteLine($"Found armor block eligible for replacing:{partNameIterator} --> SWAPPING!");

                        // if the first variant of trit name is present - convert.
                        if (STC_Armor.Contains(tritArmorVariantName1))
                        {
                            part.Element("SubtypeName").Value = tritArmorVariantName1;
                            blockChanged = true;
                            result.Add($"Replaced {partNameIterator} with {tritArmorVariantName1}");
                            Trace.WriteLine($"Replaced {partNameIterator} with {tritArmorVariantName1}");
                            continue;
                        }

                        // if the second variant of trit name is present - convert.
                        if (STC_Armor.Contains(tritArmorVariantName2))
                        {
                            part.Element("SubtypeName").Value = tritArmorVariantName2;
                            blockChanged = true;
                            result.Add($"Replaced {partNameIterator} with {tritArmorVariantName2}");
                            Trace.WriteLine($"Replaced {partNameIterator} with {tritArmorVariantName2}");
                            continue;
                        }


                        // if the third variant of trit name is present - convert.
                        if (STC_Armor.Contains(tritArmorVariantName3))
                        {
                            part.Element("SubtypeName").Value = tritArmorVariantName3;
                            blockChanged = true;
                            result.Add($"Replaced {partNameIterator} with {tritArmorVariantName3}");
                            Trace.WriteLine($"Replaced {partNameIterator} with {tritArmorVariantName3}");
                            continue;
                        }

                        // if the fourth variant of trit name is present - convert.
                        if (STC_Armor.Contains(tritArmorVariantName4))
                        {
                            part.Element("SubtypeName").Value = tritArmorVariantName4;
                            blockChanged = true;
                            result.Add($"Replaced {partNameIterator} with {tritArmorVariantName4}");
                            Trace.WriteLine($"Replaced {partNameIterator} with {tritArmorVariantName4}");
                            continue;
                        }


                        // if the fifth variant of trit name is present - convert.
                        if (STC_Armor.Contains(tritArmorVariantName5))
                        {
                            part.Element("SubtypeName").Value = tritArmorVariantName5;
                            blockChanged = true;
                            result.Add($"Replaced {partNameIterator} with {tritArmorVariantName5}");
                            Trace.WriteLine($"Replaced {partNameIterator} with {tritArmorVariantName5}");
                            continue;
                        }

                        // if the sixth variant of trit name is present - convert.
                        if (STC_Armor.Contains(tritArmorVariantName6))
                        {
                            part.Element("SubtypeName").Value = tritArmorVariantName6;
                            blockChanged = true;
                            result.Add($"Replaced {partNameIterator} with {tritArmorVariantName6}");
                            Trace.WriteLine($"Replaced {partNameIterator} with {tritArmorVariantName6}");
                            continue;
                        }

                        // if the seventh variant of trit name is present - convert.
                        if (STC_Armor.Contains(tritArmorVariantName7))
                        {
                            part.Element("SubtypeName").Value = tritArmorVariantName7;
                            blockChanged = true;
                            result.Add($"Replaced {partNameIterator} with {tritArmorVariantName7}");
                            Trace.WriteLine($"Replaced {partNameIterator} with {tritArmorVariantName7}");
                            continue;
                        }

                        // if the eight variant of trit name is present - convert.
                        if (STC_Armor.Contains(tritArmorVariantName8))
                        {
                            part.Element("SubtypeName").Value = tritArmorVariantName8;
                            blockChanged = true;
                            result.Add($"Replaced {partNameIterator} with {tritArmorVariantName8}");
                            Trace.WriteLine($"Replaced {partNameIterator} with {tritArmorVariantName8}");
                            continue;
                        }

                        // Below: "Last chance" cases: The block is either heavy armor, or light armor.
                        // Any other blocks need to be handled above!
                        // Remove the word 'heavy' from the name of the block.
                        // There are no 'heavy' tritanium variants.
                        if (isHeavyArmorBlock)
                        {
                            heavyWordIndex = partNameIterator.IndexOf("Heavy");

                            // 5, because the word heavy is 5 characters long.
                            // H E A V Y
                            partNameIteratorModified = partNameIterator.Remove(heavyWordIndex, 5);
                            part.Element("SubtypeName").Value = partNameIterator;
                            blockChanged = true;
                            Trace.WriteLine($"Replaced {partNameIterator} with {partNameIteratorModified}");
                            continue;
                        }
                        // We 'continue' in order to avoid falling into another case. Similar to a switch!
                        continue;
                    }

                    Trace.WriteLine($"Block {partNameIterator} was changed? {blockChanged}");

                    // Reset our iterating variables
                    isArmorBlock = false;
                    isTritaniumArmorBlock = false;
                    isHeavyArmorBlock = false;
                    blockChanged = false;
                }
            }

            // Save the file
            BackupShipXML();
            DeleteSBC5File();
            _shipRoot.Save(_cubegridSBCfilename);
            SetCubeGridPostLoad(_cubegridName);
            return result;
        }
        public List<string> AutoArmor_LightToHeavy()
        {
            // Return a list of strings that represents a log of all the blocks swapped.
            List<string> result = new List<string>();

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

            // iterate through each part on the ship,
            // figure out if it's an armor block,
            // if yes: is it tritanium?
            // if trit: ignore
            // if not-trit: convert
            foreach (string blockVariant in _shipParts.Keys)
            {
                List<XElement> parts = _shipParts[blockVariant];

                foreach (XElement part in parts)
                {

                    partNameIterator = part.Element("SubtypeName").Value;

                    isArmorBlock = partNameIterator.Contains("Armor");
                    isHeavyArmorBlock = part.Element("SubtypeName").Value.Contains("Heavy");
                    isPanel = part.Element("SubtypeName").Value.Contains("Panel");
                    isBeam = part.Element("SubtypeName").Value.Contains("Beam");

                    // EXISTING LIGHT ARMOR
                    if (!isHeavyArmorBlock)
                    {
                        Trace.WriteLine($"Found light armor:\n----\n{blockVariant}\n----\nEVALUATING!\n");
                        blockChanged = false;

                        // VANILLA PANEL
                        if (isPanel && isArmorBlock && !isHeavyArmorBlock)
                        {
                            partNameReplacementIterator = Regex.Replace(partNameIterator, "Light", "Heavy");

                            part.Element("SubtypeName").Value = partNameReplacementIterator;
                            blockChanged = true;
                            result.Add($"Replaced {partNameIterator} with {partNameReplacementIterator}");
                            Trace.WriteLine($"Replaced {partNameIterator} with {partNameReplacementIterator}");
                            continue;
                        }

                        // GENERIC ARMOR
                        if (isArmorBlock)
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
                                                    Trace.WriteLine($"Found exception block: {partNameIterator}. Skipping!");
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

                            part.Element("SubtypeName").Value = partNameReplacementIterator;
                            blockChanged = true;
                            result.Add($"Replaced {partNameIterator} with {partNameReplacementIterator}");
                            Trace.WriteLine($"Replaced {partNameIterator} with {partNameReplacementIterator}");
                            continue;
                        }

                    }

                    Trace.WriteLine($"Block {partNameIterator} was changed? {blockChanged}");

                    // Reset our iterating variables
                    isArmorBlock = false;
                    isHeavyArmorBlock = false;
                    blockChanged = false;
                }
            }

            // Save the file
            BackupShipXML();
            DeleteSBC5File();
            _shipRoot.Save(_cubegridSBCfilename);
            SetCubeGridPostLoad(_cubegridName);
            return result;
        }

        public List<string> AutoArmor_HeavyToLight()
        {
            // Return a list of strings that represents a log of all the blocks swapped.
            List<string> result = new List<string>();

            string partNameIterator;
            string partNameReplacementIterator;


            // Booleans used to detect-and-indicate that we've found relevant blocks to convert to tritanium
            bool isArmorBlock = false;
            bool isLightArmorBlock = false;
            bool isPanel = false;
            bool isBeam = false;

            // Used to determine if a block was changed.
            // Useful for logging and debug
            bool blockChanged = false;

            Regex ArmorDiscriminatorPattern = new Regex("(Large|Small)?(HeavyArmor|Armor|HeavyBlock|HeavyHalf|HalfSlope|ArmorSlope|BlockHeavy|Block|Half)([\\S]+)");

            // iterate through each part on the ship,
            // figure out if it's an armor block,
            // if yes: is it tritanium?
            // if trit: ignore
            // if not-trit: convert
            foreach (string blockVariant in _shipParts.Keys)
            {
                List<XElement> parts = _shipParts[blockVariant];

                foreach (XElement part in parts)
                {

                    partNameIterator = part.Element("SubtypeName").Value;

                    isArmorBlock = partNameIterator.Contains("Armor");
                    isLightArmorBlock = !part.Element("SubtypeName").Value.Contains("Heavy");
                    isPanel = part.Element("SubtypeName").Value.Contains("Panel");
                    isBeam = part.Element("SubtypeName").Value.Contains("Beam");

                    // EXISTING HEAVY ARMOR
                    if (isLightArmorBlock)
                    {
                        Trace.WriteLine($"Found light armor:\n----\n{blockVariant}\n----\nEVALUATING!\n");
                        blockChanged = false;

                        // VANILLA BEAM
                        if (isBeam)
                        {
                            partNameReplacementIterator = partNameIterator; // TODO <-- Change this

                            part.Element("SubtypeName").Value = partNameReplacementIterator;

                            blockChanged = true;
                            result.Add($"Replaced {partNameIterator} with {partNameReplacementIterator}");
                            Trace.WriteLine($"Replaced {partNameIterator} with {partNameReplacementIterator}");
                            continue;
                        }

                        // VANILLA PANEL
                        if (isPanel)
                        {
                            partNameReplacementIterator = partNameIterator; // TODO <-- Change this

                            part.Element("SubtypeName").Value = partNameReplacementIterator;
                            blockChanged = true;
                            result.Add($"Replaced {partNameIterator} with {partNameReplacementIterator}");
                            Trace.WriteLine($"Replaced {partNameIterator} with {partNameReplacementIterator}");
                            continue;
                        }

                        // GENERIC ARMOR
                        if (isArmorBlock)
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
                                            Trace.WriteLine($"Found non-heavy block: {partNameIterator}. Skipping!");
                                            continue;
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

                            part.Element("SubtypeName").Value = partNameReplacementIterator;
                            blockChanged = true;
                            result.Add($"Replaced {partNameIterator} with {partNameReplacementIterator}");
                            Trace.WriteLine($"Replaced {partNameIterator} with {partNameReplacementIterator}");
                            continue;
                        }

                    }

                    Trace.WriteLine($"Block {partNameIterator} was changed? {blockChanged}");

                    // Reset our iterating variables
                    isArmorBlock = false;
                    isLightArmorBlock = false;
                    blockChanged = false;
                }
            }

            // Save the file
            BackupShipXML();
            DeleteSBC5File();
            _shipRoot.Save(_cubegridSBCfilename);
            SetCubeGridPostLoad(_cubegridName);
            return result;
        }


        public List<string> STC_AutoArmor_From_Tritanium()
        {
            // Return a list of strings that represents a log of all the blocks swapped.
            List<string> result = new List<string>();

            string stcArmorVariantGroupName = "STC_Tritanium_Armor";

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

            int heavyWordIndex;

            HashSet<string> STC_Armor = new HashSet<string>();

            Regex ArmorDiscriminatorPattern = new Regex("(Large|Small)?(HeavyArmor|Armor|HeavyBlock|Block|Half|HeavyHalf)([\\S]+)");

            // Populate all the STC armor we can find...
            foreach (string blockVarKey in _blockVariantsAvail.Keys)
            {
                if (blockVarKey.Contains("STC_Tritanium"))
                {
                    foreach (string blockVariant in _blockVariantsAvail[blockVarKey])
                    {
                        STC_Armor.Add(blockVariant);
                    }
                }
            }

            // Throw error if we cannot find armor.
            if (STC_Armor.Count == 0)
            {
                throw new Exception("Unable to find any STC Armor!");
            }

            // iterate through each part on the ship,
            // figure out if it's an armor block,
            // if yes: is it tritanium?
            // if trit: ignore
            // if not-trit: convert
            foreach (string blockVariant in _shipParts.Keys)
            {
                List<XElement> parts = _shipParts[blockVariant];

                foreach (XElement part in parts)
                {

                    partNameIterator = part.Element("SubtypeName").Value;

                    isTritaniumArmorBlock = part.Element("SubtypeName").Value.Contains("Tritanium");

                    isArmorBlock = partNameIterator.Contains("Armor");
                    isHeavyArmorBlock = part.Element("SubtypeName").Value.Contains("Heavy");
                    isPanel = part.Element("SubtypeName").Value.Contains("Panel");
                    isBeam = part.Element("SubtypeName").Value.Contains("Beam");

                    // EXISTING TRIT ARMOR
                    if (isTritaniumArmorBlock)
                    {
                        Trace.WriteLine($"Found tritanium armor:\n----\n{blockVariant}\n----\n");
                        blockChanged = false;

                        // VANILLA BEAM
                        if (isBeam)
                        {
                            partNameReplacementIterator = Regex.Replace(partNameIterator, "Tritanium", "");

                            part.Element("SubtypeName").Value = partNameReplacementIterator;
                            blockChanged = true;
                            result.Add($"Replaced {partNameIterator} with {partNameReplacementIterator}");
                            Trace.WriteLine($"Replaced {partNameIterator} with {partNameReplacementIterator}");
                            continue;
                        }

                        // VANILLA PANEL
                        if (isPanel)
                        {
                            partNameReplacementIterator = Regex.Replace(partNameIterator, "Tritanium", "Light");

                            part.Element("SubtypeName").Value = partNameReplacementIterator;
                            blockChanged = true;
                            result.Add($"Replaced {partNameIterator} with {partNameReplacementIterator}");
                            Trace.WriteLine($"Replaced {partNameIterator} with {partNameReplacementIterator}");
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

                            part.Element("SubtypeName").Value = partNameReplacementIterator;
                            blockChanged = true;
                            result.Add($"Replaced {partNameIterator} with {partNameReplacementIterator}");
                            Trace.WriteLine($"Replaced {partNameIterator} with {partNameReplacementIterator}");
                            continue;
                        }

                    }
                    else
                    {
                        Trace.WriteLine($"Found non-tritanium block: {partNameIterator}");
                    }

                    Trace.WriteLine($"Block {partNameIterator} was changed? {blockChanged}");

                    // Reset our iterating variables
                    isArmorBlock = false;
                    isTritaniumArmorBlock = false;
                    isHeavyArmorBlock = false;
                    blockChanged = false;
                }
            }

            // Save the file
            BackupShipXML();
            DeleteSBC5File();
            _shipRoot.Save(_cubegridSBCfilename);
            SetCubeGridPostLoad(_cubegridName);
            return result;
        }

        public List<string> Remove_Armor(bool removeHeavyOnly, bool removeLightOnly)
        {
            // Return a list of strings that represents a log of all the blocks swapped.
            List<string> result = new List<string>();

            string partNameIterator;
            bool isArmorBlock;
            bool isTritaniumArmorBlock;
            bool isHeavyArmorBlock;
            int heavyWordIndex;

            // iterate through each part on the ship,
            // figure out if it's an armor block,
            // if yes: is it tritanium?
            // if trit: ignore
            // if not-trit: convert
            foreach (string blockVariant in _shipParts.Keys)
            {
                List<XElement> parts = _shipParts[blockVariant];

                foreach (XElement part in parts)
                {
                    partNameIterator = part.Element("SubtypeName").Value;
                    isArmorBlock = partNameIterator.Contains("Armor");
                    isTritaniumArmorBlock = part.Element("SubtypeName").Value.Contains("Tritanium");
                    isHeavyArmorBlock = part.Element("SubtypeName").Value.Contains("Heavy");

                    if (isTritaniumArmorBlock)
                    {
                        Trace.WriteLine($"Found tritanium armor:\n----\n{blockVariant}\n");
                    }

                    if (isArmorBlock)
                    {

                        // Remove the word 'heavy' from the name of the block.
                        // There are no 'heavy' tritanium variants.

                        // If we are set to removeHeavyOnly==true
                        if (removeHeavyOnly || removeLightOnly)
                        {
                            //...And the block we are looking at is a Heavy Armor Block
                            if (isHeavyArmorBlock && removeHeavyOnly)
                            {
                                //...Remove the heavy armor block!
                                try
                                {
                                    part.Remove();
                                    result.Add($"Deleted Heavy Armor Block:\n----{partNameIterator}----\n");
                                }
                                catch (Exception ex)
                                {
                                    Trace.WriteLine($"Error while deleting {part.Name.ToString()}!\nError was:\n{ex}");
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
                                //...Remove the light armor block!
                                try
                                {
                                    part.Remove();
                                    result.Add($"Deleted Heavy Armor Block:\n----{partNameIterator}----\n");
                                }
                                catch (Exception ex)
                                {
                                    Trace.WriteLine($"Error while deleting {part.Name.ToString()}!\nError was:\n{ex}");
                                }
                                continue;
                            }

                        }
                        else
                        {
                            try
                            {
                                part.Remove();
                                result.Add($"Deleted Armor Block:\n----{partNameIterator}----\n");
                            }
                            catch (Exception ex)
                            {
                                Trace.WriteLine($"Error while deleting {part.Name.ToString()}!\nError was:\n{ex}");
                            }
                        }
                    }

                    // Reset our iterating variables
                    isArmorBlock = false;
                    isTritaniumArmorBlock = false;
                    isHeavyArmorBlock = false;
                }
            }

            // Save the file
            BackupShipXML();
            DeleteSBC5File();
            _shipRoot.Save(_cubegridSBCfilename);
            SetCubeGridPostLoad(_cubegridName);
            return result;
        }

        public List<string> RemoveAllExceptArmor()
        {
            // Return a list of strings that represents a log of all the blocks swapped.
            List<string> result = new List<string>();

            string partNameIterator;
            bool isArmorBlock;
            bool isTritaniumArmorBlock;
            bool isHeavyArmorBlock;
            int heavyWordIndex;

            // iterate through each part on the ship,
            // figure out if it's an armor block,
            // if yes: is it tritanium?
            // if trit: detect
            // if not-trit: detect
            foreach (string blockVariant in _shipParts.Keys)
            {
                List<XElement> parts = _shipParts[blockVariant];

                foreach (XElement part in parts)
                {
                    partNameIterator = part.Element("SubtypeName").Value;
                    isArmorBlock = partNameIterator.Contains("Armor");
                    isTritaniumArmorBlock = part.Element("SubtypeName").Value.Contains("Tritanium");
                    isHeavyArmorBlock = part.Element("SubtypeName").Value.Contains("Heavy");

                    if (isTritaniumArmorBlock)
                    {
                        Trace.WriteLine($"Found tritanium armor:\n----\n{blockVariant}\n");
                    }

                    if (isArmorBlock)
                    {
                        continue;
                    }
                    else
                    {
                        //...Remove the non-armor block!
                        try
                        {
                            part.Remove();
                            result.Add($"Deleted Non-Armor Block:\n----{partNameIterator}----\n");
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine($"Error while deleting {part.Name.ToString()}!\nError was:\n{ex}");
                        }
                    }

                    // Reset our iterating variables
                    isArmorBlock = false;
                    isTritaniumArmorBlock = false;
                    isHeavyArmorBlock = false;
                }
            }

            // Save the file
            BackupShipXML();
            DeleteSBC5File();
            _shipRoot.Save(_cubegridSBCfilename);
            SetCubeGridPostLoad(_cubegridName);
            return result;
        }


        public List<string> RemoveAllExcept(string retainedBlockVariant)
        {
            // Return a list of strings that represents a log of all the blocks swapped.
            List<string> resultLog = new List<string>();

            string partNameIterator;
            bool isArmorBlock;
            bool isTritaniumArmorBlock;
            bool isHeavyArmorBlock;
            int heavyWordIndex;

            // iterate through each part on the ship,
            // figure out if it's an armor block,
            // if yes: is it tritanium?
            // if trit: ignore
            // if not-trit: convert
            foreach (string blockVariant in _shipParts.Keys)
            {
                List<XElement> parts = _shipParts[blockVariant];

                // Iterate through each part in the ship
                foreach (XElement part in parts)
                {
                    partNameIterator = part.Element("SubtypeName").Value;

                    // If the part we are looking at (partNameIterator) contains the term that we want to spare - skip!
                    if (partNameIterator.Contains(retainedBlockVariant))
                    {
                        continue;
                    }
                    else
                    {
                        try
                        {
                            part.Remove();
                            resultLog.Add($"Deleting block: " + partNameIterator + "\n");
                        }
                        catch (Exception ex)
                        {
                            resultLog.Add($"Error while deleting {part.Name.ToString()}!\n");
                            Trace.WriteLine($"Error while deleting {part.Name.ToString()}!\nError was:\n{ex}");
                        }
                    }
                }
            }

            // Save the file
            BackupShipXML();
            DeleteSBC5File();
            _shipRoot.Save(_cubegridSBCfilename);
            SetCubeGridPostLoad(_cubegridName);
            return resultLog;
        }

        // Automatically removes all blocks with a given removeSubtypeName
        public List<string> RemoveSpecific(string removeSubtypeName)
        {
            List<string> resultLog = new List<string>();

            foreach (string shipPart in _shipParts.Keys)
            {
                if (shipPart.Equals(removeSubtypeName))
                {
                    List<XElement> partList = _shipParts[removeSubtypeName];

                    string blockname;

                    foreach (XElement block in partList)
                    {
                        blockname = block.Element("SubtypeName").Value;

                        if (blockname.Equals(removeSubtypeName))
                        {

                            try
                            {
                                block.Remove();
                                resultLog.Add($"Deleted block: " + blockname + "\n");
                            }
                            catch (Exception ex)
                            {
                                resultLog.Add($"Error deleting block: " + blockname + "\n");
                                Trace.WriteLine($"Error deleting block: " + blockname + "\nException is:\n" + ex);
                            }
                        }
                        else
                        {
                            continue;
                        }
                    }
                }
            }
            // Save the file
            BackupShipXML();
            DeleteSBC5File();
            _shipRoot.Save(_cubegridSBCfilename);
            SetCubeGridPostLoad(_cubegridName);
            return resultLog;
        }

        public List<string> AutoTech(string categoryName, int desiredTechlevel)
        {
            List<string> result = new List<string>();

            if (desiredTechlevel < 0 || desiredTechlevel > 3)
            {
                throw new Exception("AutoTech: Invalid desiredTechLevel!");
            }

            foreach (string shipPart in _shipParts.Keys)
            {
                // Check if we're using a non-subtype based name, such as MyObjectBuilder_...
                if (shipPart.StartsWith("MyObjectBuilder_"))
                {
                    Trace.WriteLine("Found non-subtype based name. Ignoring!");
                    continue;
                }

                // Otherwise - check if the shipPart name equals the category name.
                if (shipPart.Equals(categoryName))
                {
                    List<XElement> shipPartXElement = _shipParts[categoryName];

                    bool isDesiredTechLevel = false;
                    bool isCurrentlyTeched = false;

                    string desiredTechLevelString = "";
                    string blockname;

                    int currTechLevel = -1;
                    int offset = categoryName.Length - 2;

                    if (shipPart.EndsWith("2x") || shipPart.EndsWith("4x") || shipPart.EndsWith("8x"))
                    {
                        isCurrentlyTeched = true;

                        if (shipPart.EndsWith("2x"))
                        {
                            currTechLevel = 1;
                            isCurrentlyTeched = true;
                        }

                        if (shipPart.EndsWith("4x"))
                        {
                            currTechLevel = 2;
                            isCurrentlyTeched = true;
                        }

                        if (shipPart.EndsWith("8x"))
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

                    foreach (XElement block in shipPartXElement)
                    {
                        blockname = block.Element("SubtypeName").Value;


                        // If we determine the ship needs to change tech level...
                        if (currTechLevel != desiredTechlevel)
                        {
                            switch (desiredTechlevel)
                            {
                                case 0:
                                    result.Add($"Changing {blockname} to {desiredTechLevelString}");
                                    block.Element("SubtypeName").Value = desiredTechLevelString;
                                    break;
                                case 1:
                                    result.Add($"Changing {blockname} to {desiredTechLevelString}");
                                    block.Element("SubtypeName").Value = desiredTechLevelString;
                                    break;
                                case 2:
                                    result.Add($"Changing {blockname} to {desiredTechLevelString}");
                                    block.Element("SubtypeName").Value = desiredTechLevelString;
                                    break;
                                case 3:
                                    result.Add($"Changing {blockname} to {desiredTechLevelString}");
                                    block.Element("SubtypeName").Value = desiredTechLevelString;
                                    break;
                                default:
                                    result.Add($"Anomaly: {blockname} to {desiredTechLevelString} hit 'default' case!");
                                    break;
                            }
                        }
                        else
                        {
                            //debug option
                            result.Add($"{blockname} already at desired tech level {desiredTechLevelString}!");

                        }

                        // reset current tech level
                        currTechLevel = -1;
                    }
                }
            }
            BackupShipXML();
            DeleteSBC5File();
            _shipRoot.Save(_cubegridSBCfilename);
            SetCubeGridPostLoad(_cubegridName);
            return result;
        }

        // Creates a backup of the XML document
        public void BackupShipXML(string filename)
        {
            XmlDocument xmlDoc = new XmlDocument();

            XmlWriterSettings xwrSettings = new XmlWriterSettings();

            bool debug = false;

            string currentDirectory = Directory.GetCurrentDirectory();
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmm");
            string BackupFilename = filename + timestamp + "pps_bp_bak.sbc";

            // Long filepath not necessary. We use relative.
            //string FilePath = Path.Combine(currentDirectory, BackupFilename);


            if (debug)
            {
                Console.WriteLine($"DEBUG: BackupShipXML sbcFilename input is: {filename}");
            }


            using (XmlReader xRead = XmlReader.Create(filename))
            {
                xmlDoc.Load(xRead);
            }

            xwrSettings.IndentChars = "\t";
            xwrSettings.NewLineHandling = NewLineHandling.Entitize;
            xwrSettings.Indent = true;
            xwrSettings.NewLineChars = "\n";

            timestamp = DateTime.Now.ToString("yyyyMMdd_HHmm");
            BackupFilename = filename + timestamp + "pps_bp_bak.sbc";

            using (XmlWriter xWrite = XmlWriter.Create(BackupFilename, xwrSettings))
            {
                xmlDoc.Save(xWrite);
            }
        }

        // Loads blockvariants from mod directories starting at a given mod folder path
        // modFolderPath should, ideally, be the workshop mod path $steamapps$/workshop/content/244850
        // LoadModDirectories modifies _availableBlockCategories
        public Dictionary<string, HashSet<string>> LoadModDirsBlockVariantsDictWPFSelected(string modFolderPath)
        {
            // Modpath should ideally be the root folder that contains all the mod folders we want to iterate through
            DirectoryInfo modFolderRoot;
            DirectoryInfo[] modFolderDirectories;
            DirectoryInfo dataDirectory;

            FileInfo blockVariantGroupFile;
            FileInfo[] dataFiles;

            HashSet<string> parts;
            HashSet<string> appendedSet;

            // Loading the blockVariantGroupsXML file into an XMLDocument we can read
            XElement blockVariantGroupsXML;
            XElement blockVariantGroupsNode;
            XElement blockVariantGroupNode;

            string categoryName;
            string partName;

            Dictionary<string, HashSet<string>> BlockVariantsDict = new Dictionary<string, HashSet<string>>();

            HashSet<string> BlockVariantsSet = new HashSet<string>();

            // Clear the lists we are about to repopulate
            BlockVariantsDict.Clear();

            try
            {
                if (modFolderPath == null || modFolderPath.Equals(""))
                {
                    // This happens when the user cancels out of the mod selection folder.
                    // We'll handle this more gracefully in the future. Maybe.
                    throw new InvalidProgramException("Entered LoadModDirsBlockVariantsDictWPFSelected with null modFolderPath!\nThis should never happen!\n");
                }

                // assign the modFolderRoot
                modFolderRoot = new DirectoryInfo(modFolderPath);

            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Error in LoadModDirectories!\n{ex}");
                throw new InvalidDataException("modFolderPath invalid!");
            }



            if (!modFolderRoot.FullName.Contains("workshop\\content\\244850"))
            {
                Trace.WriteLine("Error: Invalid folder!\nPath should end in: workshop\\content\\244850\n");
                throw new InvalidProgramException("Entered LoadModDirsBlockVariantsDictWPFSelected with null modFolderPath!\nThis should never happen!\n");
            }

            // We iterate through each of those mod directories, to begin our search for relevant files that contain part definitions
            foreach (DirectoryInfo modFolder in modFolderRoot.GetDirectories())
            {
                modFolderDirectories = modFolder.GetDirectories();

                parts = new HashSet<string>();
                appendedSet = new HashSet<string>();

                // First, check if the modFolder (specifically: its directories via modFolderSubDirectories) contains a "Data" folder
                if (modFolderDirectories.Any(x => x.Name == "Data"))
                {

                    // If a "Data" folder exists, look for the BlockVariantGroupsSBC_BlockVariantGroup files
                    // TODO: Maybe someday we can also use BlockCategories? Not sure which would be best...
                    dataDirectory = new DirectoryInfo(Path.Combine(modFolder.FullName, "Data"));
                    dataFiles = dataDirectory.GetFiles();

                    // If one of the dataFiles is named "BlockVariantGroups.sbc"
                    foreach (FileInfo file in dataFiles)
                    {
                        if (file.Name.EndsWith("BlockVariantGroups.sbc"))
                        {
                            // Get the fileInfo via path-combining the data directory path with the word "BlockVariantGroups". Then loaad the file into XML.
                            blockVariantGroupFile = new FileInfo(file.FullName);

                            // Loading the blockVariantGroupsXML file into an XMLDocument we can read
                            blockVariantGroupsXML = XElement.Load(file.ToString());
                            blockVariantGroupsNode = blockVariantGroupsXML.Element("BlockVariantGroups");
                            blockVariantGroupNode = blockVariantGroupsXML.Element("BlockVariantGroupsSBC_BlockVariantGroup");

                            // Some definitions only have a single BlockVariantGroupsSBC_BlockVariantGroup.
                            // Let's discern from the two cases we are interested in: When a file has BlockVariantGroupsSBC_BlockVariantGroup(s), or when it is a singular group.
                            //NOTE: The blockVariantGroupsNode and blockVarianGroupNode could possibly be null or non-null.
                            // 

                            // First case: There are multiple blockVariantGroups
                            if (blockVariantGroupsNode != null)
                            {
                                foreach (XElement blockVariantGroup in blockVariantGroupsNode.Elements())
                                {

                                    categoryName = blockVariantGroup.Element("Id").Attribute("Subtype").Value;

                                    foreach (XElement block in blockVariantGroup.Element("_Blocks").Elements())
                                    {
                                        // Add the blockVariant subtype to the set of part categories we are building
                                        partName = block.Attribute("Subtype").Value;
                                        parts.Add(partName);
                                    }

                                    if (BlockVariantsDict.ContainsKey(categoryName))
                                    {
                                        appendedSet = BlockVariantsDict[categoryName].Union(parts).ToHashSet();
                                        BlockVariantsDict[categoryName] = appendedSet;
                                    }
                                    else
                                    {
                                        // And this is where we add the newly-generated categoryName:partCategoriesSet pair I referenced earlier!
                                        BlockVariantsDict.Add(categoryName, parts);
                                    }

                                    parts = new HashSet<string>();
                                    appendedSet = new HashSet<string>();
                                }
                            }
                            else
                            {
                                //Second case: There is a singular blockVariantGroup
                                if (blockVariantGroupNode != null)
                                {
                                    categoryName = blockVariantGroupNode.Element("Id").Attribute("Subtype").Value;

                                    foreach (XElement block in blockVariantGroupNode.Element("_Blocks").Elements())
                                    {
                                        // Add the blockVariant subtype to the set of part categories we are building
                                        partName = block.Attribute("Subtype").Value;
                                        parts.Add(partName);
                                    }

                                    if (BlockVariantsDict.ContainsKey(categoryName))
                                    {
                                        BlockVariantsDict[categoryName] = BlockVariantsDict[categoryName].Union(parts).ToHashSet();
                                    }
                                    else
                                    {
                                        // And this is where we add the newly-generated categoryName:partCategoriesSet pair I referenced earlier!
                                        BlockVariantsDict.Add(categoryName, parts);
                                    }

                                    parts = new HashSet<string>();
                                    appendedSet = new HashSet<string>();
                                }
                            }
                        }
                    }
                }

            }
            return BlockVariantsDict;
        }

        // Idea behind loadDefinitions:
        // You put named variants of "BlockVariantGroups.sbc" into the program directory (Ex: Aryx_BlockVariantGroups.sbc,SWTTBlockVariantGroups.sbc)
        // and this method will return Dict<string,List<String>> such that the categoryKey is the full name of the file we are pulling
        // definitions from, and the associated set
        public Dictionary<string, HashSet<string>> LoadBlockVariantsDict()
        {
            bool debug = false;

            string CurrentWorkingDirStr = Directory.GetCurrentDirectory();

            FileInfo[] BlockVariantFilesList;

            DirectoryInfo CurrentWorkingDirInfo = new DirectoryInfo(CurrentWorkingDirStr);

            Dictionary<string, HashSet<string>> BlockVariantsDict = new Dictionary<string, HashSet<string>>();

            HashSet<string> BlockVariantsSet = new HashSet<string>();

            BlockVariantFilesList = CurrentWorkingDirInfo.GetFiles("*BlockVariantGroups.sbc");

            // Iterate through each file...
            foreach (FileInfo File in BlockVariantFilesList)
            {
                XElement BlockVariantRoot = XElement.Load(File.ToString());

                XElement BlockVariantGroups = BlockVariantRoot.Element("BlockVariantGroups");

                string Subtype;

                if (debug)
                {
                    Console.WriteLine($"DEBUG: BlockVariantGroups looks like:\n {BlockVariantGroups}");
                    Console.WriteLine("DEBUG: Iterating through all BlockVariantGroups. \n");

                    foreach (XElement Category in BlockVariantGroups.Elements())
                    {
                        Console.WriteLine(Category);
                    }
                }

                // Iterate through each element of Definitions/BlockVariantGroups
                foreach (XElement blockVariantGroup in BlockVariantGroups.Elements())
                {
                    if (debug)
                    {
                        Console.WriteLine($"DEBUG: Current blockVariantGroup is:\n{blockVariantGroup}\n");
                    }

                    // Skip this blockVariantGroup if it has no elements
                    if (!blockVariantGroup.HasElements)
                    {
                        if (debug)
                        {
                            Console.WriteLine($"DEBUG: blockVariantGroup {blockVariantGroup.Name} has no elements! Is it malformed?");
                        }
                        continue;
                    }

                    foreach (XElement block in blockVariantGroup.Element("_Blocks").Elements())
                    {
                        if (debug)
                        {
                            Console.WriteLine($"DEBUG: Found blockvariant {block.Attribute("Subtype").Value}");
                        }

                        BlockVariantsSet.Add(block.Attribute("Subtype").Value);
                    }
                }

                if (debug)
                {
                    Console.WriteLine($"DEBUG: Saving Blockvariants in dict with categoryKey {File.Name}");
                }

                BlockVariantsDict[File.Name.ToString()] = BlockVariantsSet;
            }
            return BlockVariantsDict;
        }

        // Loads blockvariants from the seFolderPath
        public Dictionary<string, HashSet<string>> LoadDefaultBlockVariantsDictWPFSelected(string seFolderPath)
        {
            bool debug = false;

            string CurrentWorkingDirStr = Path.Combine(seFolderPath, "Content", "Data");

            FileInfo[] BlockVariantFilesList;

            DirectoryInfo CurrentWorkingDirInfo = new DirectoryInfo(CurrentWorkingDirStr);

            Dictionary<string, HashSet<string>> BlockVariantsDict = new Dictionary<string, HashSet<string>>();

            HashSet<string> BlockVariantsSet = new HashSet<string>();

            BlockVariantFilesList = CurrentWorkingDirInfo.GetFiles("*BlockVariantGroups.sbc");

            // Iterate through each file...
            foreach (FileInfo File in BlockVariantFilesList)
            {
                XElement BlockVariantRoot = XElement.Load(File.ToString());

                XElement BlockVariantGroups = BlockVariantRoot.Element("BlockVariantGroups");

                string VariantGroupID;
                string Subtype;

                if (debug)
                {
                    Console.WriteLine($"DEBUG: BlockVariantGroups looks like:\n {BlockVariantGroups}");
                    Console.WriteLine("DEBUG: Iterating through all BlockVariantGroups. \n");

                    foreach (XElement Category in BlockVariantGroups.Elements())
                    {
                        Console.WriteLine(Category);
                    }
                }

                // Iterate through each element of Definitions/BlockVariantGroups
                foreach (XElement blockVariantGroup in BlockVariantGroups.Elements())
                {
                    BlockVariantsSet = new HashSet<string>();
                    VariantGroupID = "";

                    if (debug)
                    {
                        Console.WriteLine($"DEBUG: Current blockVariantGroup is:\n{blockVariantGroup}\n");
                    }

                    // Skip this blockVariantGroup if it has no elements
                    if (!blockVariantGroup.HasElements)
                    {
                        if (debug)
                        {
                            Console.WriteLine($"DEBUG: blockVariantGroup {blockVariantGroup.Name} has no elements! Is it malformed?");
                        }
                        continue;
                    }

                    foreach (XElement block in blockVariantGroup.Element("_Blocks").Elements())
                    {
                        if (debug)
                        {
                            Console.WriteLine($"DEBUG: Found blockvariant {block.Attribute("Subtype").Value}");
                        }

                        BlockVariantsSet.Add(block.Attribute("Subtype").Value);
                    }

                    VariantGroupID = blockVariantGroup.Element("Id").Attribute("Subtype").Value;

                    BlockVariantsDict[VariantGroupID] = BlockVariantsSet;

                }

                if (debug)
                {
                    Console.WriteLine($"DEBUG: Saving Blockvariants in dict with categoryKey {File.Name}");
                }


            }
            return BlockVariantsDict;
        }
        public XElement? LoadShipRootXElement(string filename, bool debug)
        {
            XmlWriterSettings XMLWriterSettings = new XmlWriterSettings();

            string currentDirectory = Directory.GetCurrentDirectory();
            string shipFilepath = Path.Combine(currentDirectory, filename);
            string GridName;

            XElement RootNode;

            try
            {
                RootNode = XElement.Load(shipFilepath);
                GridName = RootNode.Element("ShipBlueprints").Element("ShipBlueprint").Element("Id").Attribute("Subtype").Value;

                if (debug)
                {
                    Console.WriteLine($"DEBUG: SelectWorkingBlueprint found blueprint with name\"{GridName}\"");
                }

                return RootNode;
            }
            catch (Exception e)
            {
                Console.WriteLine("Error while making assignments in SelectWorkingBlueprint! Printing error:");
                Console.WriteLine(e);
                return null;
            }
        }

        public void ReloadShipRootXElement()
        {
            // Re-load the file
            _shipRoot = LoadShipRootXElement(_cubegridSBCfilename, _debug);

            // If GUI: Set in state consistent with constructor
            // as of 20JUN2024 - that simply means _shipParts = null
            if (_GUIMode)
            {
                _shipParts = GenerateShipPartsFromXElement(_currentCubegridXElement, _debug);
            }
            else
            {
                _shipParts = null;
            }

            // refresh available blockvariants, just to be thorough.
            _blockVariantsAvail = LoadBlockVariantsDict();
            _shipGroups = GetShipGroups();
        }


        public void RenderTextIntro()
        {
            string PicarlsWelcomeTag = " // Picarl's PartSwapper //\n";
            RenderSlowColoredText(PicarlsWelcomeTag, 20, ConsoleColor.Black, ConsoleColor.Green);
        }


        public void REPL(string inputShipSBC)
        {
            // flags
            bool quitflag = false;
            bool debug = false;

            // ints
            int userInputInt = -1;

            // strings
            string userInput = "";
            string inputPartSwapOut;
            string inputPartSwapIn;

            //...you get it by now, yes?
            List<string> UserPartCategoriesOpts = new List<string>();
            List<string> UserShipCurrCatParts = new List<string>();

            // partSwapper represents the ship we want to work on.
            PartSwapper partSwapper;

            while (!quitflag)
            {
                partSwapper = new PartSwapper(inputShipSBC, debug);

                RenderSlowColoredText("Ship loaded.\n", 10, ConsoleColor.Cyan);

                if (debug)
                {
                    foreach (string blocktype in partSwapper._blockVariantsAvail.Keys)
                    {
                        Console.WriteLine("Found the following blockvariant categoryKey:");
                        Console.WriteLine(blocktype);
                        Console.WriteLine("Found the following blockvariant values:");
                        foreach (string value in partSwapper._blockVariantsAvail[blocktype])
                        {
                            Console.WriteLine(value);
                        }
                    }
                }

                RenderSlowColoredText("Found the following parts on your ship:\n", 10, ConsoleColor.Cyan);

                foreach (string category in partSwapper._shipParts.Keys)
                {
                    //Console.WriteLine($"{category} = {partSwapper.parts[category].Count}");
                    RenderSlowColoredText($"{category} = {partSwapper._shipParts[category].Count}\n", 0, ConsoleColor.DarkYellow);
                }

                RenderSlowColoredText($"\nPlease select which category of parts you would like to swap:\n", 10, ConsoleColor.Magenta);

                RenderSlowColoredText($"1. Gyroscopes\n2. Ion Thrusters\n3. Hydrogen Thrusters \n4. Batteries\n" +
                    $"5. Jump Drives\n6. Hydrogen Tanks\n7. Oxygen Tanks\n" +
                    $"8. Cargo Containers\n9. Drills\n10. Conveyors\n11.ArmorSwapper\n12.RefinerySwapper\n13.AssemblerSwapper" +
                    $"\nQ to quit editing this file.\nSelection > ", 10, ConsoleColor.DarkMagenta);


                userInput = Console.ReadLine();

                if (IsUserQuitting(userInput.ToUpper()))
                {
                    return;
                }

                switch (int.Parse(userInput))
                {
                    case 1:
                        GyroSwapperTier(partSwapper, inputShipSBC);
                        break;
                    case 2:
                        IonThrusterSwapperTier(partSwapper, inputShipSBC);
                        break;
                    case 3:
                        HydrogenThrusterSwapperTier(partSwapper, inputShipSBC);
                        break;
                    case 4:
                        BatterySwapperTier(partSwapper, inputShipSBC);
                        break;
                    case 5:
                        JumpDriveSwapperTier(partSwapper, inputShipSBC);
                        break;
                    case 6:
                        HydroTankSwapperTier(partSwapper, inputShipSBC);
                        break;
                    case 7:
                        OxygenTankSwapperTier(partSwapper, inputShipSBC);
                        break;
                    case 8:
                        CargoContainerSwapperTier(partSwapper, inputShipSBC);
                        break;
                    case 9:
                        DrillSwapperTier(partSwapper, inputShipSBC);
                        break;
                    case 10:
                        ConveyorSwapperTier(partSwapper, inputShipSBC);
                        break;
                    case 11:
                        ArmorSwapper(partSwapper, inputShipSBC);
                        break;
                    case 12:
                        RefinerySwapper(partSwapper, inputShipSBC);
                        break;
                    case 13:
                        AssemblerSwapper(partSwapper, inputShipSBC);
                        break;
                    default:
                        Console.WriteLine("You chose something absurd. I'm quitting. Bye!\n");
                        return;
                }

                Console.WriteLine("Swap another part in this ship? Y/N");
                userInput = Console.ReadLine();

                switch (userInput.ToUpper())
                {
                    case "Y":
                        RenderSlowColoredText("Swapping another part!\n", 5, ConsoleColor.Red);
                        break;
                    case "N":
                        Console.WriteLine("Ending Program!");
                        quitflag = true;
                        break;
                }
            }
        }

        public void RenderSlowConsoleText(string text, int delay)
        {
            foreach (char letter in text)
            {
                Thread.Sleep(delay);
                Console.Write(letter);
            }

            // might need to add a newline here, test behavior.
        }

        public void RenderSlowColoredText(string text, int delay, ConsoleColor color)
        {
            ConsoleColor preserve = Console.ForegroundColor;

            Console.ForegroundColor = color;
            Console.BackgroundColor = ConsoleColor.Black;

            foreach (char letter in text)
            {
                Thread.Sleep(delay);
                Console.Write(letter);
            }
            Console.ForegroundColor = preserve;
        }

        public void RenderSlowColoredText(string text, int delay, ConsoleColor foregroundColor, ConsoleColor backgroundColor)
        {
            ConsoleColor preserveFG = Console.ForegroundColor;
            ConsoleColor preserveBG = Console.BackgroundColor;

            Console.ForegroundColor = foregroundColor;
            Console.BackgroundColor = backgroundColor;

            foreach (char letter in text)
            {
                Thread.Sleep(delay);
                Console.Write(letter);
            }

            Console.ForegroundColor = preserveFG;
            Console.BackgroundColor = preserveBG;
        }


        public void PartswapViaPartname(string filename, string oldPart, string newPart, bool debug)
        {
            XmlWriterSettings XMLWriterSettings = new XmlWriterSettings();

            string currentDirectory = Directory.GetCurrentDirectory();
            string shipFilepath = Path.Combine(currentDirectory, filename);

            XElement shipTree = XElement.Load(shipFilepath);
            XElement ShipBPs = shipTree.Element("ShipBlueprints");
            XElement ShipBP = ShipBPs.Element("ShipBlueprint");
            XElement CubeGrids = ShipBP.Element("CubeGrids");

            string GridName = ShipBP.Element("Id").Attribute("Subtype").Value;

            // iterating blockVariant
            XElement currPart;
            if (debug)
            {
                Console.WriteLine($"DEBUG: PartswapViaSubtypeName found GridName: {GridName}");
            }

            //backup the original grid
            BackupShipXML(filename);

            if (debug)
            {
                Console.WriteLine($"Printing shipTree Nodes and self:\n{shipTree.DescendantNodesAndSelf().ToString()}");
            }

            // Iterate _CubeBlocksXElement
            foreach (XElement cubeGrid in CubeGrids.Elements())
            {
                IEnumerable<XElement> CubeBlocks = cubeGrid.Element("_CubeBlocksXElement").Elements();

                foreach (XElement cubeBlock in CubeBlocks)
                {
                    if (debug)
                    {
                        Console.WriteLine($"DEBUG: Checking element:{cubeBlock}");
                    }

                    // I dont know why this is inverted, but that's how it happened.
                    // If the blockVariant does NOT match the old part - we skip it...
                    if (!cubeBlock.Element("SubtypeName").Value.ToString().Equals(oldPart))
                    {
                        if (debug)
                        {
                            Console.WriteLine($"DEBUG: cubeBlock.Element(\"SubtypeName\") {cubeBlock.Element("SubtypeName").Value} does not equal {oldPart}");
                        }

                        // skip
                        continue;
                    }
                    else
                    {
                        // If we have a match... Change the partname, batman.
                        currPart = cubeBlock.Element("SubtypeName");

                        //_debug output
                        if (debug)
                        {
                            // if it does match - swap the part
                            Console.WriteLine($"DEBUG: Found part: {currPart}\n Replacing {oldPart} with {newPart}");
                        }

                        // This is where the magic happens.
                        currPart.SetValue(newPart);
                        // The magic has occurred. 

                        //_debug output
                        if (debug)
                        {
                            Console.WriteLine($"DEBUG: part name post-swap:{currPart}");
                        }
                    }
                }

            }

            // Save the file.
            shipTree.Save(filename);
        }

        public void PartSwapTUI(PartSwapper partSwapper, string inputShipSBC, string partCategoryString)
        {
            string userInput = "";
            int userInputInt = -1;
            string inputPartSwapOut;
            string inputPartSwapIn;

            List<string> UserPartCategoriesOpts = new List<string>();
            List<string> UserShipCurrCatParts = new List<string>();

            foreach (string blockvariant in partSwapper._blockVariantsAvail.Keys)
            {
                foreach (string blocktype in partSwapper._blockVariantsAvail[blockvariant])
                {

                    if (blocktype.ToUpper().Contains(partCategoryString))
                    {
                        UserPartCategoriesOpts.Add(blocktype);
                    }
                    else
                    {
                        continue;
                    }

                }
            }

            foreach (string blocktype in partSwapper._shipParts.Keys)
            {
                if (blocktype.ToUpper().Contains(partCategoryString))
                {

                    RenderSlowColoredText($"Found blockVariant type {blocktype} on your ship! Eligible for replacement!\n", 10, ConsoleColor.Green);
                    UserShipCurrCatParts.Add(blocktype);

                }
            }

            UserPruneList(UserShipCurrCatParts);

            // Iterates through the blockvariants that are on the ship already, and the relevant blockvariants we found from blockvariant files,
            // And then we offer the user all the options for swapping out parts.
            foreach (string partCategory in UserShipCurrCatParts)
            {
                string blockvarSubstring = partCategory.Remove(partCategory.Length - 3);

                // the declaration of relatedCategories pulls in categories from the entirety of categories of ship parts, sorting by those categories that contain our partCategory string, minus 2 chars.
                List<string> relatedCategories = UserPartCategoriesOpts.Where(item => item.Contains(partCategory.Substring(0, partCategory.Length - 2))).Distinct().ToList();

                for (int i = 0; i < relatedCategories.Count(); i++)
                {
                    Console.WriteLine($"{i} - {relatedCategories[i]}");
                }

                Console.WriteLine($"What should we replace {partCategory} with? C to continue.");

                userInput = Console.ReadLine();

                if (IsUserQuitting(userInput.ToUpper()))
                {
                    return;
                }

                if (userInput.ToUpper() == "C")
                {
                    Console.WriteLine("Continuing.");
                    continue;
                }

                userInputInt = int.Parse(userInput);

                PartswapViaPartname(inputShipSBC, partCategory, relatedCategories[userInputInt], false);

                Console.WriteLine($"{partCategory} has been replaced with {relatedCategories[userInputInt]}");
            }
        }

        public void AssemblerSwapper(PartSwapper partSwapper, string inputShipSBC)
        {
            string userInput = "";
            int userInputInt = -1;
            string inputPartSwapOut;
            string inputPartSwapIn;

            List<string> UserPartCategoriesOpts = new List<string>();
            List<string> UserShipCurrCatParts = new List<string>();

            foreach (string blockvariant in partSwapper._blockVariantsAvail.Keys)
            {
                foreach (string blocktype in partSwapper._blockVariantsAvail[blockvariant])
                {

                    if (blocktype.ToUpper().Contains("ASSEMBLER"))
                    {
                        UserPartCategoriesOpts.Add(blocktype);
                    }
                    else
                    {
                        continue;
                    }

                }
            }

            foreach (string blocktype in partSwapper._shipParts.Keys)
            {
                if (blocktype.ToUpper().Contains("ASSEMBLER"))
                {

                    RenderSlowColoredText($"Found blockVariant type {blocktype} on your ship! Eligible for replacement!\n", 10, ConsoleColor.Green);
                    UserShipCurrCatParts.Add(blocktype);

                }
            }

            UserPruneList(UserShipCurrCatParts);

            // Iterates through the blockvariants that are on the ship already, and the relevant blockvariants we found from blockvariant files,
            // And then we offer the user all the options for swapping out parts.
            foreach (string blockvariant in UserShipCurrCatParts)
            {
                string blockvarSubstring = blockvariant.Remove(blockvariant.Length - 3);
                List<string> tieredBlocks = UserPartCategoriesOpts.Where(item => item.Contains(blockvariant.Substring(0, blockvariant.Length - 2))).Distinct().ToList();

                for (int i = 0; i < tieredBlocks.Count(); i++)
                {
                    Console.WriteLine($"{i} - {tieredBlocks[i]}");
                }

                Console.WriteLine($"What should we replace {blockvariant} with? C to continue.");

                userInput = Console.ReadLine();

                if (IsUserQuitting(userInput.ToUpper()))
                {
                    return;
                }

                if (userInput.ToUpper() == "C")
                {
                    Console.WriteLine("Continuing.");
                    continue;
                }

                userInputInt = int.Parse(userInput);

                PartswapViaPartname(inputShipSBC, blockvariant, tieredBlocks[userInputInt], false);

                Console.WriteLine($"{blockvariant} has been replaced with {tieredBlocks[userInputInt]}");
            }
        }

        public void RefinerySwapper(PartSwapper partSwapper, string inputShipSBC)
        {
            string userInput = "";
            int userInputInt = -1;
            string inputPartSwapOut;
            string inputPartSwapIn;

            List<string> UserPartCategoriesOpts = new List<string>();
            List<string> UserShipCurrCatParts = new List<string>();

            foreach (string blockvariant in partSwapper._blockVariantsAvail.Keys)
            {
                foreach (string blocktype in partSwapper._blockVariantsAvail[blockvariant])
                {

                    if (blocktype.ToUpper().Contains("REFINERY"))
                    {
                        UserPartCategoriesOpts.Add(blocktype);
                    }
                    else
                    {
                        continue;
                    }

                }
            }

            foreach (string blocktype in partSwapper._shipParts.Keys)
            {
                if (blocktype.ToUpper().Contains("REFINERY"))
                {

                    RenderSlowColoredText($"Found blockVariant type {blocktype} on your ship! Eligible for replacement!\n", 10, ConsoleColor.Green);
                    UserShipCurrCatParts.Add(blocktype);

                }
            }

            UserPruneList(UserShipCurrCatParts);

            // Iterates through the blockvariants that are on the ship already, and the relevant blockvariants we found from blockvariant files,
            // And then we offer the user all the options for swapping out parts.
            foreach (string blockvariant in UserShipCurrCatParts)
            {
                string blockvarSubstring = blockvariant.Remove(blockvariant.Length - 3);
                List<string> tieredBlocks = UserPartCategoriesOpts.Where(item => item.Contains(blockvariant.Substring(0, blockvariant.Length - 2))).Distinct().ToList();

                for (int i = 0; i < tieredBlocks.Count(); i++)
                {
                    Console.WriteLine($"{i} - {tieredBlocks[i]}");
                }

                Console.WriteLine($"What should we replace {blockvariant} with? C to continue.");

                userInput = Console.ReadLine();

                if (IsUserQuitting(userInput.ToUpper()))
                {
                    return;
                }

                if (userInput.ToUpper() == "C")
                {
                    Console.WriteLine("Continuing.");
                    continue;
                }

                userInputInt = int.Parse(userInput);

                PartswapViaPartname(inputShipSBC, blockvariant, tieredBlocks[userInputInt], false);

                Console.WriteLine($"{blockvariant} has been replaced with {tieredBlocks[userInputInt]}");
            }
        }

        public void IonThrusterSwapper(PartSwapper partSwapper, string inputShipSBC)
        {
            string userInput = "";
            int userInputInt = -1;
            string inputPartSwapOut;
            string inputPartSwapIn;

            List<string> UserPartCategoriesOpts = new List<string>();
            List<string> UserShipCurrCatParts = new List<string>();

            foreach (string blockvariant in partSwapper._blockVariantsAvail.Keys)
            {
                foreach (string blocktype in partSwapper._blockVariantsAvail[blockvariant])
                {
                    if (blocktype.ToUpper().Contains("REFINERY"))
                    {

                        UserPartCategoriesOpts.Add(blocktype);

                    }
                }
            }


            foreach (string blocktype in partSwapper._shipParts.Keys)
            {
                if (blocktype.ToUpper().Contains("THRUST"))
                {
                    if (!blocktype.ToUpper().Contains("HYDRO") && !blocktype.ToUpper().Contains("ATMO"))
                    {
                        UserShipCurrCatParts.Add(blocktype);
                        Console.WriteLine($"Found blocktype {blocktype}, all instances will be replaced!");
                    }
                    else
                    {
                        continue;
                    }
                }
            }

            UserPruneList(UserShipCurrCatParts);

            Console.WriteLine("What should we replace the above parts with?\n Selection > ");

            // Next, Iterate through what we found, and offer the user options:
            for (int i = 0; i < UserPartCategoriesOpts.Count; i++)
            {
                Console.WriteLine($"{i} - {UserPartCategoriesOpts[i]}");
            }

            userInput = Console.ReadLine();
            userInputInt = int.Parse(userInput);

            Console.WriteLine($"Replacing all with {UserPartCategoriesOpts[userInputInt]}");

            // Finally: Replace.
            foreach (string option in UserShipCurrCatParts)
            {
                PartswapViaPartname(inputShipSBC, option, UserPartCategoriesOpts[userInputInt], false);
            }
        }

        public void HydrogenThrusterSwapper(PartSwapper partSwapper, string inputShipSBC)
        {
            string userInput = "";
            int userInputInt = -1;
            string inputPartSwapOut;
            string inputPartSwapIn;

            List<string> UserPartCategoriesOpts = new List<string>();
            List<string> UserShipCurrCatParts = new List<string>();

            foreach (string blockvariant in partSwapper._blockVariantsAvail.Keys)
            {
                foreach (string blocktype in partSwapper._blockVariantsAvail[blockvariant])
                {
                    Console.WriteLine($"{blocktype}");

                    if (blocktype.ToUpper().Contains("HYDRO") && blocktype.ToUpper().Contains("THRUST"))
                    {
                        UserPartCategoriesOpts.Add(blocktype);
                    }
                    else
                    {
                        continue;
                    }
                }
            }


            foreach (string blocktype in partSwapper._shipParts.Keys)
            {
                if (blocktype.ToUpper().Contains("HYDRO") && blocktype.ToUpper().Contains("THRUST"))
                {
                    UserShipCurrCatParts.Add(blocktype);
                    Console.WriteLine($"Found blocktype {blocktype}, all instances of this type will be replaced!");
                }
                else
                {
                    continue;
                }

            }

            UserPruneList(UserShipCurrCatParts);

            Console.WriteLine("What should we replace the above parts with?");

            // Next, Iterate through what we found, and offer the user options:
            for (int i = 0; i < UserPartCategoriesOpts.Count; i++)
            {
                Console.WriteLine($"{i} - {UserPartCategoriesOpts[i]}");
            }

            userInput = Console.ReadLine();
            userInputInt = int.Parse(userInput);

            Console.WriteLine($"Replacing all with {UserPartCategoriesOpts[userInputInt]}");

            // Finally: Replace.
            foreach (string option in UserShipCurrCatParts)
            {
                PartswapViaPartname(inputShipSBC, option, UserPartCategoriesOpts[userInputInt], false);
            }
        }

        public void BatterySwapper(PartSwapper partSwapper, string inputShipSBC)
        {
            string userInput = "";
            int userInputInt = -1;
            string inputPartSwapOut;
            string inputPartSwapIn;

            List<string> UserPartCategoriesOpts = new List<string>();
            List<string> UserShipCurrCatParts = new List<string>();

            foreach (string blockvariant in partSwapper._blockVariantsAvail.Keys)
            {
                foreach (string blocktype in partSwapper._blockVariantsAvail[blockvariant])
                {
                    if (blocktype.ToUpper().Contains("BATTERY"))
                    {
                        UserPartCategoriesOpts.Add(blocktype);
                    }
                    else
                    {
                        continue;
                    }
                }
            }


            foreach (string blocktype in partSwapper._shipParts.Keys)
            {
                if (blocktype.ToUpper().Contains("BATTERY"))
                {
                    UserShipCurrCatParts.Add(blocktype);
                    Console.WriteLine($"Found blocktype {blocktype}, all instances of this type will be replaced!");
                }
                else
                {
                    continue;
                }

            }

            UserPruneList(UserShipCurrCatParts);

            Console.WriteLine("What should we replace the above parts with?");

            // Next, Iterate through what we found, and offer the user options:
            for (int i = 0; i < UserPartCategoriesOpts.Count; i++)
            {
                Console.WriteLine($"{i} - {UserPartCategoriesOpts[i]}");
            }

            userInput = Console.ReadLine();
            userInputInt = int.Parse(userInput);

            Console.WriteLine($"Replacing all with {UserPartCategoriesOpts[userInputInt]}");

            // Finally: Replace.
            foreach (string option in UserShipCurrCatParts)
            {
                PartswapViaPartname(inputShipSBC, option, UserPartCategoriesOpts[userInputInt], false);
            }
        }

        public void GyroSwapper(PartSwapper partSwapper, string inputShipSBC)
        {
            string userInput = "";
            int userInputInt = -1;
            string inputPartSwapOut;
            string inputPartSwapIn;

            List<string> UserPartCategoriesOpts = new List<string>();
            List<string> UserShipCurrCatParts = new List<string>();

            foreach (string blockvariant in partSwapper._blockVariantsAvail.Keys)
            {
                foreach (string blocktype in partSwapper._blockVariantsAvail[blockvariant])
                {
                    if (blocktype.ToUpper().Contains("GYRO"))
                    {
                        UserPartCategoriesOpts.Add(blocktype);
                    }
                    else
                    {
                        continue;
                    }
                }
            }

            foreach (string blocktype in partSwapper._shipParts.Keys)
            {
                if (blocktype.ToUpper().Contains("GYRO"))
                {
                    UserShipCurrCatParts.Add(blocktype);
                    Console.WriteLine($"Found blocktype {blocktype}, all instances of this type will be replaced!");
                }
                else
                {
                    continue;
                }
            }

            UserPruneList(UserShipCurrCatParts);

            Console.WriteLine("What should we replace the above parts with?");

            // Next, Iterate through what we found, and offer the user options:
            for (int i = 0; i < UserPartCategoriesOpts.Count; i++)
            {
                Console.WriteLine($"{i} - {UserPartCategoriesOpts[i]}");
            }

            userInput = Console.ReadLine();
            userInputInt = int.Parse(userInput);

            Console.WriteLine($"Replacing all with {UserPartCategoriesOpts[userInputInt]}");

            // Finally: Replace.
            foreach (string option in UserShipCurrCatParts)
            {
                PartswapViaPartname(inputShipSBC, option, UserPartCategoriesOpts[userInputInt], false);
            }
        }

        public void AtmoThrusterSwapperTier(PartSwapper partSwapper, string inputShipSBC)
        {
            string userInput = "";
            int userInputInt = -1;
            string inputPartSwapOut;
            string inputPartSwapIn;

            List<string> UserPartCategoriesOpts = new List<string>();
            List<string> UserShipCurrCatParts = new List<string>();

            foreach (string blockvariant in partSwapper._blockVariantsAvail.Keys)
            {
                foreach (string blocktype in partSwapper._blockVariantsAvail[blockvariant])
                {

                    if (!blocktype.ToUpper().Contains("HYDRO") && blocktype.ToUpper().Contains("ATMO") && blocktype.ToUpper().Contains("THRUST"))
                    {
                        UserPartCategoriesOpts.Add(blocktype);
                    }
                    else
                    {
                        continue;
                    }

                }
            }

            foreach (string blocktype in partSwapper._shipParts.Keys)
            {
                if (blocktype.ToUpper().Contains("THRUST"))
                {
                    if (!blocktype.ToUpper().Contains("HYDRO") && blocktype.ToUpper().Contains("ATMO") && blocktype.ToUpper().Contains("THRUST"))
                    {
                        RenderSlowColoredText($"Found blockVariant type {blocktype} on your ship! Eligible for replacement!\n", 10, ConsoleColor.Green);
                        UserShipCurrCatParts.Add(blocktype);

                    }
                    else
                    {
                        continue;
                    }
                }
            }

            UserPruneList(UserShipCurrCatParts);

            // Iterates through the blockvariants that are on the ship already, and the relevant blockvariants we found from blockvariant files,
            // And then we offer the user all the options for swapping out parts.
            foreach (string blockvariant in UserShipCurrCatParts)
            {
                string blockvarSubstring = blockvariant.Remove(blockvariant.Length - 3);
                List<string> tieredBlocks = UserPartCategoriesOpts.Where(item => item.Contains(blockvariant.Substring(0, blockvariant.Length - 2))).Distinct().ToList();

                for (int i = 0; i < tieredBlocks.Count(); i++)
                {
                    Console.WriteLine($"{i} - {tieredBlocks[i]}");
                }

                Console.WriteLine($"What should we replace {blockvariant} with? C to continue.");

                userInput = Console.ReadLine();

                if (IsUserQuitting(userInput.ToUpper()))
                {
                    return;
                }

                if (userInput.ToUpper() == "C")
                {
                    Console.WriteLine("Continuing.");
                    continue;
                }

                userInputInt = int.Parse(userInput);

                PartswapViaPartname(inputShipSBC, blockvariant, tieredBlocks[userInputInt], false);

                Console.WriteLine($"{blockvariant} has been replaced with {tieredBlocks[userInputInt]}");
            }
        }

        public void IonThrusterSwapperTier(PartSwapper partSwapper, string inputShipSBC)
        {
            string userInput = "";
            int userInputInt = -1;
            string inputPartSwapOut;
            string inputPartSwapIn;

            List<string> UserPartCategoriesOpts = new List<string>();
            List<string> UserShipCurrCatParts = new List<string>();

            foreach (string blockvariant in partSwapper._blockVariantsAvail.Keys)
            {
                foreach (string blocktype in partSwapper._blockVariantsAvail[blockvariant])
                {

                    if (!blocktype.ToUpper().Contains("HYDRO") && !blocktype.ToUpper().Contains("ATMO") && blocktype.ToUpper().Contains("THRUST"))
                    {
                        UserPartCategoriesOpts.Add(blocktype);
                    }
                    else
                    {
                        continue;
                    }

                }
            }

            foreach (string blocktype in partSwapper._shipParts.Keys)
            {
                if (blocktype.ToUpper().Contains("THRUST"))
                {
                    if (!blocktype.ToUpper().Contains("HYDRO") && !blocktype.ToUpper().Contains("ATMO"))
                    {
                        RenderSlowColoredText($"Found blockVariant type {blocktype} on your ship! Eligible for replacement!\n", 10, ConsoleColor.Green);
                        UserShipCurrCatParts.Add(blocktype);

                    }
                    else
                    {
                        continue;
                    }
                }
            }

            UserPruneList(UserShipCurrCatParts);

            // Iterates through the blockvariants that are on the ship already, and the relevant blockvariants we found from blockvariant files,
            // And then we offer the user all the options for swapping out parts.
            foreach (string blockvariant in UserShipCurrCatParts)
            {
                string blockvarSubstring = blockvariant.Remove(blockvariant.Length - 3);
                List<string> tieredBlocks = UserPartCategoriesOpts.Where(item => item.Contains(blockvariant.Substring(0, blockvariant.Length - 2))).Distinct().ToList();

                for (int i = 0; i < tieredBlocks.Count(); i++)
                {
                    Console.WriteLine($"{i} - {tieredBlocks[i]}");
                }

                Console.WriteLine($"What should we replace {blockvariant} with? C to continue.");

                userInput = Console.ReadLine();

                if (IsUserQuitting(userInput.ToUpper()))
                {
                    return;
                }

                if (userInput.ToUpper() == "C")
                {
                    Console.WriteLine("Continuing.");
                    continue;
                }

                userInputInt = int.Parse(userInput);

                PartswapViaPartname(inputShipSBC, blockvariant, tieredBlocks[userInputInt], false);

                Console.WriteLine($"{blockvariant} has been replaced with {tieredBlocks[userInputInt]}");
            }
        }

        public void HydrogenThrusterSwapperTier(PartSwapper partSwapper, string inputShipSBC)
        {
            string userInput = "";
            int userInputInt = -1;
            string inputPartSwapOut;
            string inputPartSwapIn;

            List<string> UserPartCategoriesOpts = new List<string>();
            List<string> UserShipCurrCatParts = new List<string>();

            foreach (string blockvariant in partSwapper._blockVariantsAvail.Keys)
            {
                foreach (string blocktype in partSwapper._blockVariantsAvail[blockvariant])
                {

                    if (blocktype.ToUpper().Contains("HYDRO") && blocktype.ToUpper().Contains("THRUST"))
                    {
                        UserPartCategoriesOpts.Add(blocktype);
                    }
                    else
                    {
                        continue;
                    }
                }
            }


            foreach (string blocktype in partSwapper._shipParts.Keys)
            {
                if (blocktype.ToUpper().Contains("HYDRO") && blocktype.ToUpper().Contains("THRUST"))
                {
                    RenderSlowColoredText($"Found blockVariant type {blocktype} on your ship! Eligible for replacement!\n", 10, ConsoleColor.Green);
                    UserShipCurrCatParts.Add(blocktype);
                }
                else
                {
                    continue;
                }

            }

            UserPruneList(UserShipCurrCatParts);

            RenderSlowColoredText("", 10, ConsoleColor.Cyan);
            // Iterates through the blockvariants that are on the ship already, and the relevant blockvariants we found from blockvariant files,
            // And then we offer the user all the options for swapping out parts.
            foreach (string blockvariant in UserShipCurrCatParts)
            {
                string blockvarSubstring = blockvariant.Remove(blockvariant.Length - 3);
                List<string> tieredBlocks = UserPartCategoriesOpts.Where(item => item.Contains(blockvariant.Substring(0, blockvariant.Length - 2))).Distinct().ToList();

                for (int i = 0; i < tieredBlocks.Count(); i++)
                {
                    Console.WriteLine($"{i} - {tieredBlocks[i]}");
                }

                Console.WriteLine($"What should we replace {blockvariant} with? C to continue.");

                userInput = Console.ReadLine();

                if (IsUserQuitting(userInput.ToUpper()))
                {
                    return;
                }

                if (userInput.ToUpper() == "C")
                {
                    Console.WriteLine("Continuing.");
                    continue;
                }

                userInputInt = int.Parse(userInput);

                PartswapViaPartname(inputShipSBC, blockvariant, tieredBlocks[userInputInt], false);

                Console.WriteLine($"{blockvariant} has been replaced with {tieredBlocks[userInputInt]}");
            }

        }

        public void BatterySwapperTier(PartSwapper partSwapper, string inputShipSBC)
        {
            string userInput = "";
            int userInputInt = -1;
            string inputPartSwapOut;
            string inputPartSwapIn;

            List<string> UserPartCategoriesOpts = new List<string>();
            List<string> UserShipCurrCatParts = new List<string>();

            foreach (string blockvariant in partSwapper._blockVariantsAvail.Keys)
            {
                foreach (string blocktype in partSwapper._blockVariantsAvail[blockvariant])
                {
                    if (blocktype.ToUpper().Contains("BATTERY"))
                    {

                        UserPartCategoriesOpts.Add(blocktype);
                    }
                    else
                    {
                        continue;
                    }
                }
            }

            foreach (string blocktype in partSwapper._shipParts.Keys)
            {
                if (blocktype.ToUpper().Contains("BATTERY"))
                {
                    RenderSlowColoredText($"Found blockVariant type {blocktype} on your ship! Eligible for replacement!\n", 10, ConsoleColor.Green);
                    UserShipCurrCatParts.Add(blocktype);
                }
                else
                {
                    continue;
                }
            }

            UserPruneList(UserShipCurrCatParts);

            // Iterates through the blockvariants that are on the ship already, and the relevant blockvariants we found from blockvariant files,
            // And then we offer the user all the options for swapping out parts.
            foreach (string blockvariant in UserShipCurrCatParts)
            {
                string blockvarSubstring = blockvariant.Remove(blockvariant.Length - 3);

                List<string> tieredBlocks = UserPartCategoriesOpts.Where(item => item.Contains(blockvariant.Substring(0, blockvariant.Length - 2))).Distinct().ToList();

                for (int i = 0; i < tieredBlocks.Count(); i++)
                {
                    Console.WriteLine($"{i} - {tieredBlocks[i]}");
                }

                Console.WriteLine($"What should we replace {blockvariant} with? C to continue.");

                userInput = Console.ReadLine();

                if (IsUserQuitting(userInput.ToUpper()))
                {
                    return;
                }

                if (userInput.ToUpper() == "C")
                {
                    Console.WriteLine("Continuing.");
                    continue;
                }

                userInputInt = int.Parse(userInput);

                PartswapViaPartname(inputShipSBC, blockvariant, tieredBlocks[userInputInt], false);

                Console.WriteLine($"{blockvariant} has been replaced with {tieredBlocks[userInputInt]}");
            }

        }

        public void GyroSwapperTier(PartSwapper partSwapper, string inputShipSBC)
        {
            string userInput = "";
            int userInputInt = -1;
            string inputPartSwapOut;
            string inputPartSwapIn;

            List<string> UserPartCategoriesOpts = new List<string>();
            List<string> UserShipCurrCatParts = new List<string>();

            foreach (string blockvariant in partSwapper._blockVariantsAvail.Keys)
            {
                foreach (string blocktype in partSwapper._blockVariantsAvail[blockvariant])
                {
                    if (blocktype.ToUpper().Contains("GYRO"))
                    {
                        UserPartCategoriesOpts.Add(blocktype);
                    }
                    else
                    {
                        continue;
                    }
                }
            }


            foreach (string blocktype in partSwapper._shipParts.Keys)
            {
                if (blocktype.ToUpper().Contains("GYRO"))
                {
                    RenderSlowColoredText($"Found blockVariant type {blocktype} on your ship! Eligible for replacement!\n", 10, ConsoleColor.Green);
                    UserShipCurrCatParts.Add(blocktype);
                }
                else
                {
                    continue;
                }

            }

            UserPruneList(UserShipCurrCatParts);

            // Iterates through the blockvariants that are on the ship already, and the relevant blockvariants we found from blockvariant files,
            // And then we offer the user all the options for swapping out parts.
            foreach (string blockvariant in UserShipCurrCatParts)
            {
                string blockvarSubstring = blockvariant.Remove(blockvariant.Length - 3);
                List<string> tieredBlocks = UserPartCategoriesOpts.Where(item => item.Contains(blockvariant.Substring(0, blockvariant.Length - 2))).Distinct().ToList();

                for (int i = 0; i < tieredBlocks.Count(); i++)
                {
                    Console.WriteLine($"{i} - {tieredBlocks[i]}");
                }

                Console.WriteLine($"What should we replace {blockvariant} with? C to continue.");

                userInput = Console.ReadLine();

                if (IsUserQuitting(userInput.ToUpper()))
                {
                    return;
                }

                if (userInput.ToUpper() == "C")
                {
                    Console.WriteLine("Continuing.");
                    continue;
                }

                userInputInt = int.Parse(userInput);

                PartswapViaPartname(inputShipSBC, blockvariant, tieredBlocks[userInputInt], false);

                Console.WriteLine($"{blockvariant} has been replaced with {tieredBlocks[userInputInt]}");
            }

        }

        public void JumpDriveSwapperTier(PartSwapper partSwapper, string inputShipSBC)
        {
            string userInput = "";
            int userInputInt = -1;
            string inputPartSwapOut;
            string inputPartSwapIn;

            List<string> UserPartCategoriesOpts = new List<string>();
            List<string> UserShipCurrCatParts = new List<string>();

            foreach (string blockvariant in partSwapper._blockVariantsAvail.Keys)
            {
                foreach (string blocktype in partSwapper._blockVariantsAvail[blockvariant])
                {
                    if (blocktype.ToUpper().Contains("JUMPDRIVE"))
                    {
                        UserPartCategoriesOpts.Add(blocktype);
                    }
                    else
                    {
                        continue;
                    }
                }
            }


            foreach (string blocktype in partSwapper._shipParts.Keys)
            {
                if (blocktype.ToUpper().Contains("JUMPDRIVE"))
                {
                    RenderSlowColoredText($"Found blockVariant type {blocktype} on your ship! Eligible for replacement!\n", 10, ConsoleColor.Green);
                    UserShipCurrCatParts.Add(blocktype);
                }
                else
                {
                    continue;
                }

            }

            UserPruneList(UserShipCurrCatParts);

            // Iterates through the blockvariants that are on the ship already, and the relevant blockvariants we found from blockvariant files,
            // And then we offer the user all the options for swapping out parts.
            foreach (string blockvariant in UserShipCurrCatParts)
            {
                string blockvarSubstring = blockvariant.Remove(blockvariant.Length - 3);
                List<string> tieredBlocks = UserPartCategoriesOpts.Where(item => item.Contains(blockvariant.Substring(0, blockvariant.Length - 2))).Distinct().ToList();

                for (int i = 0; i < tieredBlocks.Count(); i++)
                {
                    Console.WriteLine($"{i} - {tieredBlocks[i]}");
                }

                Console.WriteLine($"What should we replace {blockvariant} with? C to continue.");

                userInput = Console.ReadLine();

                if (IsUserQuitting(userInput.ToUpper()))
                {
                    return;
                }

                if (userInput.ToUpper() == "C")
                {
                    Console.WriteLine("Continuing.");
                    continue;
                }

                userInputInt = int.Parse(userInput);

                PartswapViaPartname(inputShipSBC, blockvariant, tieredBlocks[userInputInt], false);

                Console.WriteLine($"{blockvariant} has been replaced with {tieredBlocks[userInputInt]}");
            }

        }

        public void ArmorSwapper(PartSwapper partSwapper, string inputShipSBC)
        {
            string userInput = "";
            int userInputInt = -1;
            string inputPartSwapOut;
            string inputPartSwapIn;

            List<string> UserPartCategoriesOpts = new List<string>();
            List<string> UserShipCurrCatParts = new List<string>();

            // Warn the user. This is "all" or "None"
            RenderSlowColoredText("<--WARNING-->\nArmorSwapper currently replaces EVERY ARMOR BLOCK IT FINDS!\nCancel this operation if this is unacceptable to you!\n", 5, ConsoleColor.Red);

            // With armorswapper: the user only has two choices - HEAVY or LIGHT armor.
            UserPartCategoriesOpts.Add("HEAVY ARMOR");
            UserPartCategoriesOpts.Add("LIGHT ARMOR");
            UserPartCategoriesOpts.Add("STRIP ARMOR");

            // Make the user choose what armor they want
            RenderSlowColoredText("Set all armor to:\n1.Heavy\n2.Light\n3.Strip Armor\n0 to Quit Gracefully\n", 5, ConsoleColor.Red);

            userInputInt = Convert.ToInt32(Console.ReadLine());

            // Credit to SEToolbox for providing logic hints for this switch case
            switch (userInputInt)
            {
                case 0:
                    return;
                case 1:
                    foreach (string armor in partSwapper._shipParts.Keys)
                    {
                        // These checks ensure that we don't accidentally overwrite some blocks!
                        if (armor.Contains("Battery") || armor.Contains("Cockpit"))
                        {
                            continue;
                        }
                        else

                        if (armor.Contains("LargeBlockArmor"))
                        {
                            string HeavyToLightString = armor.Replace("LargeBlockArmor", "LargeHeavyBlockArmor");
                            Console.WriteLine($"Replacing {armor} with {HeavyToLightString} via PartSwapper!");
                            PartswapViaPartname(inputShipSBC, armor, HeavyToLightString, false);
                        }
                        else if (armor.StartsWith("Large") && (armor.EndsWith("HalfArmorBlock") || armor.EndsWith("HalfSlopeArmorBlock")))
                        {
                            string HeavyToLightString = armor.Replace("LargeHalf", "LargeHeavyHalf");
                            Console.WriteLine($"Replacing {armor} with {HeavyToLightString} via PartSwapper!");
                            PartswapViaPartname(inputShipSBC, armor, HeavyToLightString, false);
                        }
                        else if (armor.StartsWith("SmallBlockArmor"))
                        {
                            string HeavyToLightString = armor.Replace("SmallBlockArmor", "SmallHeavyBlockArmor");
                            Console.WriteLine($"Replacing {armor} with {HeavyToLightString} via PartSwapper!");
                            PartswapViaPartname(inputShipSBC, armor, HeavyToLightString, false);
                        }
                        else if (!armor.StartsWith("Large") && (armor.EndsWith("HalfArmorBlock") || armor.EndsWith("HalfSlopeArmorBlock")))
                        {
                            string HeavyToLightString = Regex.Replace(armor, "^(Half)(.*)", "HeavyHalf$2", RegexOptions.IgnoreCase);
                            Console.WriteLine($"Replacing {armor} with {HeavyToLightString} via PartSwapper!");
                            PartswapViaPartname(inputShipSBC, armor, HeavyToLightString, false);
                        }
                        else if (armor.Contains("PanelLight"))
                        {
                            string HeavyToLightString = armor.Replace("PanelLight", "PanelHeavy");
                            Console.WriteLine($"Replacing {armor} with {HeavyToLightString} via PartSwapper!");
                            PartswapViaPartname(inputShipSBC, armor, HeavyToLightString, false);
                        }
                    }
                    break;
                case 2:
                    foreach (string armor in partSwapper._shipParts.Keys)
                    {
                        if (armor.Contains("LargeHeavyBlockHeavyArmor"))
                        {
                            string LightToHeavyString = armor.Replace("LargeHeavyBlockHeavyArmor", "LargeBlockArmor");
                            Console.WriteLine($"Replacing {armor} with {LightToHeavyString} via PartSwapper!");
                            PartswapViaPartname(inputShipSBC, armor, LightToHeavyString, false);
                        }
                        else if (armor.StartsWith("Large") && (armor.EndsWith("HalfArmorBlock") || armor.EndsWith("HalfSlopeArmorBlock")))
                        {
                            string LightToHeavyString = armor.Replace("LargeHeavyHalf", "LargeHalf");
                            Console.WriteLine($"Replacing {armor} with {LightToHeavyString} via PartSwapper!");
                            PartswapViaPartname(inputShipSBC, armor, LightToHeavyString, false);

                        }
                        else if (armor.StartsWith("SmallHeavyBlockArmor"))
                        {
                            var LightToHeavyString = armor.Replace("SmallHeavyBlockArmor", "SmallBlockArmor");
                            Console.WriteLine($"Replacing {armor} with {LightToHeavyString} via PartSwapper!");
                            PartswapViaPartname(inputShipSBC, armor, LightToHeavyString, false);
                        }
                        else if (!armor.StartsWith("Large") && (armor.EndsWith("HalfArmorBlock") || armor.EndsWith("HalfSlopeArmorBlock")))
                        {
                            var LightToHeavyString = Regex.Replace(armor, "^(HeavyHalf)(.*)", "Half$2", RegexOptions.IgnoreCase);
                            Console.WriteLine($"Replacing {armor} with {LightToHeavyString} via PartSwapper!");
                            PartswapViaPartname(inputShipSBC, armor, LightToHeavyString, false);
                        }
                        else if (armor.StartsWith("LargeHeavyBlockArmor"))
                        {
                            var LightToHeavyString = armor.Replace("LargeHeavyBlockArmor", "LargeBlockArmor");
                            Console.WriteLine($"Replacing {armor} with {LightToHeavyString} via PartSwapper!");
                            PartswapViaPartname(inputShipSBC, armor, LightToHeavyString, false);
                        }
                        else if (armor.StartsWith("LargeBlockHeavyArmorSlope"))
                        {
                            var LightToHeavyString = armor.Replace("LargeBlockHeavyArmorSlope", "LargeBlockArmorSlope");
                            Console.WriteLine($"Replacing {armor} with {LightToHeavyString} via PartSwapper!");
                            PartswapViaPartname(inputShipSBC, armor, LightToHeavyString, false);
                        }
                        else if (armor.EndsWith("HeavyArmorHalfCorner"))
                        {
                            var LightToHeavyString = armor.Replace("HeavyArmorHalfCorner", "ArmorHalfCorner");
                            Console.WriteLine($"Replacing {armor} with {LightToHeavyString} via PartSwapper!");
                            PartswapViaPartname(inputShipSBC, armor, LightToHeavyString, false);
                        }
                        else if (armor.Contains("PanelHeavy"))
                        {
                            string HeavyToLightString = armor.Replace("PanelHeavy", "PanelLight");
                            Console.WriteLine($"Replacing {armor} with {HeavyToLightString} via PartSwapper!");
                            PartswapViaPartname(inputShipSBC, armor, HeavyToLightString, false);
                        };
                    }
                    break;
                case 3:

                    break;
            }
        }

        public void HydroTankSwapperTier(PartSwapper partSwapper, string inputShipSBC)
        {
            string userInput = "";
            int userInputInt = -1;
            string inputPartSwapOut;
            string inputPartSwapIn;

            List<string> UserPartCategoriesOpts = new List<string>();
            List<string> UserShipCurrCatParts = new List<string>();

            foreach (string blockvariant in partSwapper._blockVariantsAvail.Keys)
            {
                foreach (string blocktype in partSwapper._blockVariantsAvail[blockvariant])
                {
                    if (blocktype.ToUpper().Contains("HYDROGENTANK"))
                    {
                        UserPartCategoriesOpts.Add(blocktype);
                    }
                    else
                    {
                        continue;
                    }
                }
            }


            foreach (string blocktype in partSwapper._shipParts.Keys)
            {
                if (blocktype.ToUpper().Contains("HYDROGENTANK"))
                {
                    RenderSlowColoredText($"Found blockVariant type {blocktype} on your ship! Eligible for replacement!\n", 10, ConsoleColor.Green);
                    UserShipCurrCatParts.Add(blocktype);
                }
                else
                {
                    continue;
                }

            }

            UserPruneList(UserShipCurrCatParts);

            // Iterates through the blockvariants that are on the ship already, and the relevant blockvariants we found from blockvariant files,
            // And then we offer the user all the options for swapping out parts.
            foreach (string blockvariant in UserShipCurrCatParts)
            {
                string blockvarSubstring = blockvariant.Remove(blockvariant.Length - 3);
                List<string> tieredBlocks = UserPartCategoriesOpts.Where(item => item.Contains(blockvariant.Substring(0, blockvariant.Length - 2))).Distinct().ToList();

                for (int i = 0; i < tieredBlocks.Count(); i++)
                {
                    Console.WriteLine($"{i} - {tieredBlocks[i]}");
                }

                Console.WriteLine($"What should we replace {blockvariant} with? C to continue.");

                userInput = Console.ReadLine();

                if (IsUserQuitting(userInput.ToUpper()))
                {
                    return;
                }

                if (userInput.ToUpper() == "C")
                {
                    Console.WriteLine("Continuing.");
                    continue;
                }

                userInputInt = int.Parse(userInput);

                PartswapViaPartname(inputShipSBC, blockvariant, tieredBlocks[userInputInt], false);

                Console.WriteLine($"{blockvariant} has been replaced with {tieredBlocks[userInputInt]}");
            }

        }

        public void OxygenTankSwapperTier(PartSwapper partSwapper, string inputShipSBC)
        {
            string userInput = "";
            int userInputInt = -1;
            string inputPartSwapOut;
            string inputPartSwapIn;

            List<string> UserPartCategoriesOpts = new List<string>();
            List<string> UserShipCurrCatParts = new List<string>();

            foreach (string blockvariant in partSwapper._blockVariantsAvail.Keys)
            {
                foreach (string blocktype in partSwapper._blockVariantsAvail[blockvariant])
                {
                    if (blocktype.ToUpper().Contains("OXYGENTANK"))
                    {
                        UserPartCategoriesOpts.Add(blocktype);
                    }
                    else
                    {
                        continue;
                    }
                }
            }


            foreach (string blocktype in partSwapper._shipParts.Keys)
            {
                if (blocktype.ToUpper().Contains("OXYGENTANK"))
                {
                    RenderSlowColoredText($"Found blockVariant type {blocktype} on your ship! Eligible for replacement!\n", 10, ConsoleColor.Green);
                    UserShipCurrCatParts.Add(blocktype);
                }
                else
                {
                    continue;
                }

            }

            UserPruneList(UserShipCurrCatParts);

            // Iterates through the blockvariants that are on the ship already, and the relevant blockvariants we found from blockvariant files,
            // And then we offer the user all the options for swapping out parts.
            foreach (string blockvariant in UserShipCurrCatParts)
            {
                string blockvarSubstring = blockvariant.Remove(blockvariant.Length - 3);
                List<string> tieredBlocks = UserPartCategoriesOpts.Where(item => item.Contains(blockvariant.Substring(0, blockvariant.Length - 2))).Distinct().ToList();

                for (int i = 0; i < tieredBlocks.Count(); i++)
                {
                    Console.WriteLine($"{i} - {tieredBlocks[i]}");
                }

                Console.WriteLine($"What should we replace {blockvariant} with? C to continue.");

                userInput = Console.ReadLine();

                if (IsUserQuitting(userInput.ToUpper()))
                {
                    return;
                }

                if (userInput.ToUpper() == "C")
                {
                    Console.WriteLine("Continuing.");
                    continue;
                }

                userInputInt = int.Parse(userInput);

                PartswapViaPartname(inputShipSBC, blockvariant, tieredBlocks[userInputInt], false);

                Console.WriteLine($"{blockvariant} has been replaced with {tieredBlocks[userInputInt]}");
            }

        }

        public void CargoContainerSwapperTier(PartSwapper partSwapper, string inputShipSBC)
        {
            string userInput = "";
            int userInputInt = -1;
            string inputPartSwapOut;
            string inputPartSwapIn;

            List<string> UserPartCategoriesOpts = new List<string>();
            List<string> UserShipCurrCatParts = new List<string>();

            foreach (string blockvariant in partSwapper._blockVariantsAvail.Keys)
            {
                foreach (string blocktype in partSwapper._blockVariantsAvail[blockvariant])
                {
                    if (blocktype.ToUpper().Contains("CONTAINER"))
                    {
                        UserPartCategoriesOpts.Add(blocktype);
                    }
                    else
                    {
                        continue;
                    }
                }
            }


            foreach (string blocktype in partSwapper._shipParts.Keys)
            {
                if (blocktype.ToUpper().Contains("CONTAINER"))
                {
                    RenderSlowColoredText($"Found blockVariant type {blocktype} on your ship! Eligible for replacement!\n", 10, ConsoleColor.Green);
                    UserShipCurrCatParts.Add(blocktype);
                }
                else
                {
                    continue;
                }

            }

            UserPruneList(UserShipCurrCatParts);

            // Iterates through the blockvariants that are on the ship already, and the relevant blockvariants we found from blockvariant files,
            // And then we offer the user all the options for swapping out parts.
            foreach (string blockvariant in UserShipCurrCatParts)
            {
                string blockvarSubstring = blockvariant.Remove(blockvariant.Length - 3);
                List<string> tieredBlocks = UserPartCategoriesOpts.Where(item => item.Contains(blockvariant.Substring(0, blockvariant.Length - 2))).Distinct().ToList();

                for (int i = 0; i < tieredBlocks.Count(); i++)
                {
                    Console.WriteLine($"{i} - {tieredBlocks[i]}");
                }

                Console.WriteLine($"What should we replace {blockvariant} with? C to continue.");

                userInput = Console.ReadLine();

                if (IsUserQuitting(userInput.ToUpper()))
                {
                    return;
                }

                if (userInput.ToUpper() == "C")
                {
                    Console.WriteLine("Continuing.");
                    continue;
                }

                userInputInt = int.Parse(userInput);

                PartswapViaPartname(inputShipSBC, blockvariant, tieredBlocks[userInputInt], false);

                Console.WriteLine($"{blockvariant} has been replaced with {tieredBlocks[userInputInt]}");
            }

        }

        public void DrillSwapperTier(PartSwapper partSwapper, string inputShipSBC)
        {
            string userInput = "";
            int userInputInt = -1;
            string inputPartSwapOut;
            string inputPartSwapIn;

            List<string> UserPartCategoriesOpts = new List<string>();
            List<string> UserShipCurrCatParts = new List<string>();

            foreach (string blockvariant in partSwapper._blockVariantsAvail.Keys)
            {
                foreach (string blocktype in partSwapper._blockVariantsAvail[blockvariant])
                {
                    if (blocktype.ToUpper().Contains("DRILL"))
                    {
                        UserPartCategoriesOpts.Add(blocktype);
                    }
                    else
                    {
                        continue;
                    }
                }
            }


            foreach (string blocktype in partSwapper._shipParts.Keys)
            {
                if (blocktype.ToUpper().Contains("DRILL"))
                {
                    RenderSlowColoredText($"Found blockVariant type {blocktype} on your ship! Eligible for replacement!\n", 10, ConsoleColor.Green);
                    UserShipCurrCatParts.Add(blocktype);
                }
                else
                {
                    continue;
                }

            }

            UserPruneList(UserShipCurrCatParts);

            // Iterates through the blockvariants that are on the ship already, and the relevant blockvariants we found from blockvariant files,
            // And then we offer the user all the options for swapping out parts.
            foreach (string blockvariant in UserShipCurrCatParts)
            {
                string blockvarSubstring = blockvariant.Remove(blockvariant.Length - 3);
                List<string> tieredBlocks = UserPartCategoriesOpts.Where(item => item.Contains(blockvariant.Substring(0, blockvariant.Length - 2))).Distinct().ToList();

                for (int i = 0; i < tieredBlocks.Count(); i++)
                {
                    Console.WriteLine($"{i} - {tieredBlocks[i]}");
                }

                Console.WriteLine($"What should we replace {blockvariant} with? C to continue.");

                userInput = Console.ReadLine();

                if (IsUserQuitting(userInput.ToUpper()))
                {
                    return;
                }

                if (userInput.ToUpper() == "C")
                {
                    Console.WriteLine("Continuing.");
                    continue;
                }

                userInputInt = int.Parse(userInput);

                PartswapViaPartname(inputShipSBC, blockvariant, tieredBlocks[userInputInt], false);

                Console.WriteLine($"{blockvariant} has been replaced with {tieredBlocks[userInputInt]}");
            }

        }

        public void ConveyorSwapperTier(PartSwapper partSwapper, string inputShipSBC)
        {
            string userInput = "";
            int userInputInt = -1;
            string inputPartSwapOut;
            string inputPartSwapIn;

            List<string> UserPartCategoriesOpts = new List<string>();
            List<string> UserShipCurrCatParts = new List<string>();

            foreach (string blockvariant in partSwapper._blockVariantsAvail.Keys)
            {
                foreach (string blocktype in partSwapper._blockVariantsAvail[blockvariant])
                {
                    if (blocktype.ToUpper().Contains("CONVEYOR"))
                    {
                        UserPartCategoriesOpts.Add(blocktype);
                    }
                    else
                    {
                        continue;
                    }
                }
            }


            foreach (string blocktype in partSwapper._shipParts.Keys)
            {
                if (blocktype.ToUpper().Contains("CONVEYOR"))
                {
                    RenderSlowColoredText($"Found blockVariant type {blocktype} on your ship! Eligible for replacement!\n", 10, ConsoleColor.Green);
                    UserShipCurrCatParts.Add(blocktype);
                }
                else
                {
                    continue;
                }

            }

            UserPruneList(UserShipCurrCatParts);

            // Iterates through the blockvariants that are on the ship already, and the relevant blockvariants we found from blockvariant files,
            // And then we offer the user all the options for swapping out parts.
            foreach (string blockvariant in UserShipCurrCatParts)
            {
                string blockvarSubstring = "CONVEYOR";
                List<string> tieredBlocks = UserPartCategoriesOpts.Where(item => item.ToUpper().Contains(blockvarSubstring)).Distinct().ToList();

                for (int i = 0; i < tieredBlocks.Count(); i++)
                {
                    Console.WriteLine($"{i} - {tieredBlocks[i]}");
                }

                Console.WriteLine($"What should we replace {blockvariant} with? C to continue.");

                userInput = Console.ReadLine();

                if (IsUserQuitting(userInput.ToUpper()))
                {
                    return;
                }

                if (userInput.ToUpper() == "C")
                {
                    Console.WriteLine("Continuing.");
                    continue;
                }

                userInputInt = int.Parse(userInput);

                PartswapViaPartname(inputShipSBC, blockvariant, tieredBlocks[userInputInt], false);

                Console.WriteLine($"{blockvariant} has been replaced with {tieredBlocks[userInputInt]}");
            }

        }

        public void UserPruneList(List<string> list)
        {
            // this function gives the user an input to modify a list.
            int UserSelection = -1;
            string UserInput = "";
            bool QuitFlag = false;

            while (!QuitFlag)
            {
                Console.WriteLine("We will be swapping out the following parts. Please select the category of parts you DO NOT WANT TO SWAP OUT!\nWhen finished, type 'C' to continue and begin swapping parts. (case insensitive)");

                for (int i = 0; i < list.Count; i++)
                {
                    Console.WriteLine($"{i} - {list[i]}");
                }
                UserInput = Console.ReadLine();

                if (UserInput.ToUpper() == "C")
                {
                    return;
                }
                else
                {

                    if (IsUserQuitting(UserInput.ToUpper()))
                    {
                        return;
                    }

                    UserSelection = int.Parse(UserInput);

                    list.RemoveAt(UserSelection);
                }
            }
        }

        public bool IsUserQuitting(string userInp)
        {
            if (userInp.ToUpper() == "Q")
            {
                Console.WriteLine("Quitting!");
                return true;
            }
            else
            {
                return false;
            }
        }

        // This method assumes the following:
        // 1. _shipRoot is assigned appropriately
        // 2. cubegridName is valid
        //Returns true if we were able to set the Cubegrid successfully.
        //Returns false, otherwise.
        public bool SetCubeGridPostLoad(string cubegridName)
        {
            XElement cubeGrids = _shipRoot.Element("ShipBlueprints").Element("ShipBlueprint").Element("CubeGrids");
            bool successBool = false;

            foreach (XElement cubegrid in cubeGrids.Elements())
            {
                if (cubegrid.Element("_DisplayName").Value == cubegridName)
                {
                    successBool = SetCurrentCubegrid(cubegrid);
                    _blockVariantsAvail = LoadBlockVariantsDict();
                    _shipGroups = GetShipGroups();
                    return successBool;
                }
            }
            return successBool;
        }
        public XElement? GetCubegridGUI(string filename, bool debug)
        {
            XmlWriterSettings XMLWriterSettings = new XmlWriterSettings();

            string currentDirectory = Directory.GetCurrentDirectory();
            string shipFilepath = Path.Combine(currentDirectory, filename);
            string GridName;
            string UserInput = "";

            int UserInputInt = -1;

            XElement RootNode;
            XElement ShipBlueprintsNode;
            XElement ShipBlueprintNode;
            XElement CubeGridsNode;

            XElement[] CubeGridsArr;

        BlueprintSelectionLoopStart:
            Console.WriteLine("Please select which Subgrid definition to work on.\nQ to Quit.\n");

            try
            {
                // Assignments
                RootNode = XElement.Load(shipFilepath);
                ShipBlueprintsNode = RootNode.Element("ShipBlueprints");
                ShipBlueprintNode = ShipBlueprintsNode.Element("ShipBlueprint");
                CubeGridsNode = ShipBlueprintNode.Element("CubeGrids");
                GridName = ShipBlueprintNode.Element("Id").Attribute("Subtype").Value;
                CubeGridsArr = CubeGridsNode.Elements().ToArray();


                if (debug)
                {
                    Console.WriteLine($"DEBUG: SelectWorkingBlueprint found blueprint with name\"{GridName}\"");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error while making assignments in SelectWorkingBlueprint! Printing error:");
                Console.WriteLine(e);
                return null;
            }

            // Iterate through all the CubegridsXElementList we found, in order to offer a selection
            for (int i = 0; i < CubeGridsArr.Length; i++)
            {
                Console.WriteLine($"{i} - {CubeGridsArr[i].Element("_DisplayName").Value}");
            }

            // We're just gonna assume the user gives us something reasonable here.
            // Null-checking should probably happen here, but I ain't gettin' paid for this.
            UserInput = Console.ReadLine();

            UserInputInt = int.Parse(UserInput);

            if (IsUserQuitting(UserInput))
            {
                Console.WriteLine("User declined to select any CubeGrids definitions.");
                return null;
            }
            else
            {
                if (UserInputInt >= 0 && UserInputInt < CubeGridsArr.Length && UserInput != null)
                {
                    return CubeGridsArr[UserInputInt];
                }
                else
                {
                    Console.WriteLine("Invalid selection made! Try again.");
                    goto BlueprintSelectionLoopStart;
                }
            }
        }

        public XElement? GetCubegridTUI(string filename, bool debug)
        {
            XmlWriterSettings XMLWriterSettings = new XmlWriterSettings();

            string currentDirectory = Directory.GetCurrentDirectory();
            string shipFilepath = Path.Combine(currentDirectory, filename);
            string GridName;
            string UserInput = "";

            int UserInputInt = -1;

            XElement RootNode;
            XElement ShipBlueprintsNode;
            XElement ShipBlueprintNode;
            XElement CubeGridsNode;

            XElement[] CubeGridsArr;

        BlueprintSelectionLoopStart:
            Console.WriteLine("Please select which Subgrid definition to work on.\nQ to Quit.\n");

            try
            {
                // Assignments
                RootNode = XElement.Load(shipFilepath);
                ShipBlueprintsNode = RootNode.Element("ShipBlueprints");
                ShipBlueprintNode = ShipBlueprintsNode.Element("ShipBlueprint");
                CubeGridsNode = ShipBlueprintNode.Element("CubeGrids");
                GridName = ShipBlueprintNode.Element("Id").Attribute("Subtype").Value;
                CubeGridsArr = CubeGridsNode.Elements().ToArray();


                if (debug)
                {
                    Console.WriteLine($"DEBUG: SelectWorkingBlueprint found blueprint with name\"{GridName}\"");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error while making assignments in SelectWorkingBlueprint! Printing error:");
                Console.WriteLine(e);
                return null;
            }

            // Iterate through all the CubegridsXElementList we found, in order to offer a selection
            for (int i = 0; i < CubeGridsArr.Length; i++)
            {
                Console.WriteLine($"{i} - {CubeGridsArr[i].Element("_DisplayName").Value}");
            }

            // We're just gonna assume the user gives us something reasonable here.
            // Null-checking should probably happen here, but I ain't gettin' paid for this.
            UserInput = Console.ReadLine();

            UserInputInt = int.Parse(UserInput);

            if (IsUserQuitting(UserInput))
            {
                Console.WriteLine("User declined to select any CubeGrids definitions.");
                return null;
            }
            else
            {
                if (UserInputInt >= 0 && UserInputInt < CubeGridsArr.Length && UserInput != null)
                {
                    return CubeGridsArr[UserInputInt];
                }
                else
                {
                    Console.WriteLine("Invalid selection made! Try again.");
                    goto BlueprintSelectionLoopStart;
                }
            }
        }

        public DirectoryInfo[] GetLocalDirectories()
        {
            DirectoryInfo Directory = new DirectoryInfo(System.IO.Directory.GetCurrentDirectory());
            DirectoryInfo[] LocalDirectories = Directory.GetDirectories();
            bool debug = false;

            if (debug)
            {
                Console.WriteLine($"DEBUG: Partswapper is working out of the following directory:\n{Directory}");
            }

            return LocalDirectories;
        }

        // Deprecated
        /*
        private static Dictionary<string, List<XElement>> GenerateShipPartsList(string sbcFilename)
        {
            Dictionary<string, List<XElement>> ShipPartsList = new Dictionary<string, List<XElement>>();

            XElement Root = null;

            XElement CubeGrids = null;

            List<XElement> NewListTemp = new List<XElement>();

            string BlockNameTemp = "INIT";

            bool debug = false;

            try
            {
                Root = XElement.Load(sbcFilename);

                CubeGrids = Root.Element("ShipBlueprints").Element("ShipBlueprint").Element("CubeGrids");
            }
            catch (Exception e)
            {
                Console.WriteLine("Error generating parts list! Printing error...");
                Console.WriteLine(e);
            }

            foreach (XElement cubeGrid in CubeGrids.Elements())
            {
                Console.WriteLine($"Found ship:{cubeGrid.Element("_DisplayName").Value}");

                XElement cubeblocks = cubeGrid.Element("_CubeBlocksXElement");

                foreach (XElement blockVariant in cubeblocks.Elements())
                {
                    // Get the SubTypeName of the blockVariant, aka: blockVariant type name, or whatever. 
                    BlockNameTemp = blockVariant.Element("SubtypeName").Value;

                    // Use the blockname to check if the categoryKey exists in the shippartslist already. If so: Update from a temp list we create.
                    if (ShipPartsList.ContainsKey(BlockNameTemp))
                    {
                        // create a new list, start fresh.
                        NewListTemp = ShipPartsList.GetValueOrDefault(BlockNameTemp);
                        // Not sure if this would be 'null' here or what the "Default" value of List<XElement> is...
                        // (I hope it's an empty list). We're gonna assume that for the moment.
                        // TODO: If this causes crashes/bad behavior - Make NewListTemp a new() list if we detect a null or something.

                        NewListTemp.Add(blockVariant);

                        // Add the part to the ShipPartsList via categoryKey: Part name, Value: XElement representing that part
                        ShipPartsList[BlockNameTemp] = NewListTemp;
                    }
                    else
                    {
                        // If the categoryKey does *not* exist (Part is not currently in ShipPartsList) - simply create the first/new list with the XElement already added, and add to the dict.
                        NewListTemp = new List<XElement>() { blockVariant };

                        ShipPartsList.Add(BlockNameTemp, NewListTemp);
                    }
                }

            }
            //Iterate through each blockVariant, creating the lists (if necessary) to populate a string:List<Xelement> dictionary that will comprise the catalogue of parts in a ship.
            if (debug)
            {
                Console.WriteLine($"DEBUG: ShipPartsList resultLog is:\n{ShipPartsList}");
            }


            if (debug)
            {
                foreach (string type in ShipPartsList.Keys)
                {
                    Console.WriteLine($"DEBUG:\nkey: {type},\ncount: {ShipPartsList[type].Count()}.");
                    //Console.WriteLine($"categoryKey: {type}, Index: {resultLog[type].Count()}, value: {resultLog[type][0]}");
                }
            }


            return ShipPartsList;
        }
        */

        private Dictionary<string, List<XElement>> GenerateShipPartsFromXElement(XElement RootNode, bool Debug)
        {
            string PartsListBlockTypeName = "INIT";

            Dictionary<string, List<XElement>> ShipPartsList = new Dictionary<string, List<XElement>>();

            List<XElement> PartsList = new List<XElement>();

            Trace.WriteLine($"Found ship:{RootNode.Element("_DisplayName").Value}");

            XElement CubeBlocks = RootNode.Element("_CubeBlocksXElement");

            foreach (XElement block in CubeBlocks.Elements())
            {
                // Get the SubTypeName of the blockVariant, aka: blockVariant type name, or whatever. 
                PartsListBlockTypeName = block.Element("SubtypeName").Value;

                if (PartsListBlockTypeName.Equals(""))
                {
                    throw new Exception($"The following block had an invalid SubtypeName:\n{PartsListBlockTypeName}");
                }

                // Use the blockname to check if the categoryKey exists in the shippartslist already. If so: Update from a temp list we create.
                if (ShipPartsList.ContainsKey(PartsListBlockTypeName))
                {
                    // Update 08OCT2023 - Still can't quite find what the default value of an XElement is.
                    // It's possible that 'default' is just an empty list. I am literally too lazy to test this.
                    // So long as there isn't a null here: This will still work.
                    PartsList = ShipPartsList.GetValueOrDefault(PartsListBlockTypeName);

                    PartsList.Add(block);

                    // Add the part to the ShipPartsList via categoryKey: Part name, Value: XElement representing that part
                    ShipPartsList[PartsListBlockTypeName] = PartsList;
                }
                else
                {
                    // If the categoryKey does *not* exist (Part is not currently in ShipPartsList) - simply create the first/new list with the XElement already added, and add to the dict.
                    PartsList = new List<XElement>() { block };

                    ShipPartsList.Add(PartsListBlockTypeName, PartsList);
                }
            }

            //Iterate through each blockVariant, creating the lists (if necessary) to populate a string:List<Xelement> dictionary that will comprise the catalogue of parts in a ship.
            if (Debug)
            {
                Console.WriteLine($"DEBUG: ShipPartsList resultLog is:\n{ShipPartsList}");
            }


            if (Debug)
            {
                foreach (string type in ShipPartsList.Keys)
                {
                    Trace.WriteLine($"DEBUG:\nkey: {type},\ncount: {ShipPartsList[type].Count()}.");
                    //Console.WriteLine($"categoryKey: {type}, Index: {resultLog[type].Count()}, value: {resultLog[type][0]}");
                }
            }

            return ShipPartsList;
        }

        public static void Main()
        {
            DirectoryInfo[] BlueprintsDirInfo;
            FileInfo[] BlueprintFiles;
            string UserInput = "";
            bool QuitFlag = false;
            bool debug = false;
            PartSwapper partswapperInstance = new PartSwapper("", debug);

            partswapperInstance.RenderTextIntro();

            while (!QuitFlag)
            {

                BlueprintsDirInfo = partswapperInstance.GetLocalDirectories();

                Console.WriteLine("PartSwapper found the following eligible blueprint folders:");

                if (BlueprintsDirInfo.Length == 0)
                {
                    Console.WriteLine("ERROR: Unable to find any blueprint directories!\nQuitting!");
                    return;
                }

                // iterates through all the blueprint directories we found and 
                for (int i = 0; i < BlueprintsDirInfo.Length; i++)
                {
                    BlueprintFiles = BlueprintsDirInfo[i].GetFiles("bp.sbc");

                    for (int j = 0; j < BlueprintFiles.Length; j++)
                    {
                        if (BlueprintFiles[j].Name == "bp.sbc")
                        {
                            Console.WriteLine($"{i} - {BlueprintsDirInfo[i].Name}");
                        }
                    }
                }

                partswapperInstance.RenderSlowColoredText("Which blueprint you would like to swap parts out of?\nType 'Q' to quit. (case insensitive)\nSelection >", 5, ConsoleColor.Magenta);

                UserInput = Console.ReadLine();

                if (UserInput.ToUpper() == "Q")
                {
                    Console.WriteLine("Quitting Partswapper!");
                    return;
                }

                try
                {
                    if (BlueprintsDirInfo[int.Parse(UserInput)] == null)
                    {
                        Console.WriteLine("Invalid blueprint selection!");
                    }
                    else
                    {
                        BlueprintFiles = BlueprintsDirInfo[int.Parse(UserInput)].GetFiles("bp.sbc");

                        foreach (FileInfo BlueprintFile in BlueprintFiles)
                        {
                            if (debug)
                            {
                                Console.WriteLine($"Found blueprint file {BlueprintFile.Name} in subdirectory {BlueprintFile.DirectoryName}");
                            }

                            if (BlueprintFile.Name == "bp.sbc")
                            {
                                if (debug)
                                {
                                    Console.WriteLine($"DEBUG: Found bp.sbc in {BlueprintFile.DirectoryName}! Opening the definition!");
                                }

                                partswapperInstance.REPL(BlueprintFile.FullName);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("ERROR: Bad input or failure while partswapping! Printing Exception!");
                    Console.WriteLine(e.ToString());
                    continue;
                }

                partswapperInstance.RenderSlowColoredText("Returning to ship selection menu...\n", 10, ConsoleColor.DarkCyan);
            }
        }


    }

}
