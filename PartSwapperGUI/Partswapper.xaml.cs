using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Xml;
using System.Xml.Linq;
using Microsoft.VisualBasic.FileIO;
using Microsoft.Win32;
using PartSwapperXMLSE;

namespace PartSwapperGUI
{
    /// <summary>
    /// Interaction logic for Partswapper.xaml
    /// </summary>
    public partial class PartswapperGUI : Window
    {
        private static string _appDataPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PicarlsPartswapper", "settings.json");

        // Keys for the options dictionary used with ConfigOptions objects
        private static string _modsDirOptKey = "modsDir";

        private Dictionary<string, HashSet<string>> _availableBlockCategories;
        private Dictionary<string, List<XElement>> _currentShipParts;

        private ObservableCollection<string> _replacementPartsListOC;
        private ObservableCollection<string> _availableBlockCategoriesOC;
        private ObservableCollection<string> _currentShipPartsOC;
        private ObservableCollection<string> _similarToBlocksOC;
        private ObservableCollection<string> _transactionLog;


        private PartSwapper _partswapper;

        private string _originalPartSelection;
        private string _replacementPartSelection;
        private string _replacementPartCategorySelection;

        private string _selectedCubegridName;

        private ConfigOptions? _configOptions;

        private bool _debug;

        private int numPartsReplaced = 0;

        private void updateObservableAvailBlockCategories()
        {
            foreach (string category in _availableBlockCategories.Keys)
            {
                if (_availableBlockCategoriesOC.Contains<string>(category))
                {
                    continue;
                }
                else
                {
                    _availableBlockCategoriesOC.Add(category);
                }
            }
        }

        private void updateObservableSimilarTo()
        {
            _similarToBlocksOC.Clear();

            List<string> similarToTemp = _partswapper.SimilarParts(_originalPartSelection);

            foreach (string similar in similarToTemp)
            {
                _similarToBlocksOC.Add(similar);
            }

            similarBlockList.InvalidateVisual();
        }

        private void updateObservableCurrentShipParts()
        {

            ReloadShipParts();

            if (_currentShipParts != null)
            {
                foreach (string shipPart in _currentShipParts.Keys)
                {
                    if (_currentShipPartsOC.Contains<string>(shipPart))
                    {

                        continue;
                    }
                    else
                    {
                        _currentShipPartsOC.Add(shipPart);

                    }
                }

                currentPartsListbox.InvalidateVisual();
            }
            else
            {
                throw new Exception("_currentShipParts is null! This should never happen!");
            }
        }

        public PartswapperGUI(PartSwapper ps, string selectedCubegridName)
        {

            InitializeComponent();

            _partswapper = ps;
            _availableBlockCategories = ps.GetBlockVariantsAvail();
            _currentShipParts = ps.GetShipParts();

            _replacementPartsListOC = new ObservableCollection<string>();
            _availableBlockCategoriesOC = new ObservableCollection<string>();
            _currentShipPartsOC = new ObservableCollection<string>();
            _similarToBlocksOC = new ObservableCollection<string>();

            _transactionLog = new ObservableCollection<string>();

            _originalPartSelection = "";
            _replacementPartSelection = "";

            // Clear Listboxes and set their sources
            currentPartsListbox.Items.Clear();
            replacementPartCategoriesListbox.Items.Clear();
            replacementPartsListbox.Items.Clear();
            similarBlockList.Items.Clear();

            currentPartsListbox.ItemsSource = _currentShipPartsOC;
            replacementPartCategoriesListbox.ItemsSource = _availableBlockCategoriesOC;
            replacementPartsListbox.ItemsSource = _replacementPartsListOC;
            similarBlockList.ItemsSource = _similarToBlocksOC;

            // retain the selected cubegrid name, for use in reloading post-change
            this._selectedCubegridName = selectedCubegridName;

            updateObservableAvailBlockCategories();
            updateObservableCurrentShipParts();

            // Load config file from global/static appdata path
            this.LoadOrCreateConfigFile();

            // This block takes the newly-loaded-or-created _configOptions and attempts to safely iterate through the Mod folders
            if (this._configOptions != null)
            {
                modsDirTextBlock.Text = this._configOptions.GetOption(_modsDirOptKey);

            }
            else
            {
                MessageBox.Show("Error: _configOptions is null!");
                throw new InvalidDataException("_configOptions null!");
            }

            // redraw screen
            this.InvalidateVisual();
        }

        public void ReloadShipFile()
        {
            _partswapper.ReloadShipRootXElement();
            _partswapper.SetCubeGridPostLoad(_selectedCubegridName);
            _partswapper.GenerateShipPartsList();

            currentPartsListbox.InvalidateVisual();
        }

        public void ReloadShipParts()
        {
            _currentShipPartsOC.Clear();

            if (this._partswapper != null)
            {
                this._currentShipParts = this._partswapper.GetShipParts();

                foreach (string shipPart in _currentShipParts.Keys)
                {
                    _currentShipPartsOC.Add(shipPart);
                }

                // redraw screen
                this.InvalidateVisual();
                return;
            }
            else
            {
                throw new InvalidDataException("_partswapper is null!");
            }
        }

        // attempts to load the config file. Returns true if successful load, false if we had to create a new file.
        private bool LoadOrCreateConfigFile()
        {
            // This is where we actually make the call to load the config file
            try
            {

                // case where _ConfigOptions is null
                if (this._configOptions == null)
                {
                    // create new CO object and attempt to load
                    this._configOptions = new ConfigOptions(_appDataPath);
                    this._configOptions.LoadOrCreateConfig();

                    return false;
                }
                else
                {
                    this._configOptions.LoadOrCreateConfig();

                    return true;
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                throw new InvalidDataException("LoadOrCreateConfigFile method failed! See error message:\n" + ex.Message);
            }
        }

        private void PartCategoriesListbox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string? selectedCategory = replacementPartCategoriesListbox.SelectedItem.ToString();
            HashSet<string> selectedPartsViaCategory;

            if (selectedCategory != null)
            {
                selectedPartsViaCategory = _availableBlockCategories[selectedCategory];
            }
            else
            {
                throw new Exception("selectedCategory is null!");
            }

            if (replacementPartCategoriesListbox.SelectedItem != null)
            {
                try
                {
                    selectedCategory = replacementPartCategoriesListbox.SelectedItem.ToString();
                    selectedPartsViaCategory = _availableBlockCategories[selectedCategory];
                }
                catch (Exception ex)
                {
                    Trace.WriteLine("Error assigning selectedCaategory or selectedPartsViaCategory\n" + ex.Message);
                }


                // null and invalid-state guard
                if (selectedPartsViaCategory == null)
                {
                    MessageBox.Show("Error - selectedPartsViaCategory null, or invalid state!");
                    throw new InvalidOperationException("selectedPartsViaCategory is null!");
                }
                else
                {

                    _replacementPartsListOC.Clear();
                    replacementPartsListbox.SelectedIndex = -1;

                    foreach (string part in _availableBlockCategories[selectedCategory])
                    {
                        _replacementPartsListOC.Add(part);
                    }
                }
            }

            // redraw screen
            this.InvalidateVisual();
        }
        private void SimilarBlockList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // If there's currently a selected part - unselect it!
            if (_replacementPartCategorySelection != null)
            {
                _replacementPartCategorySelection = null;
                replacementPartCategoriesListbox.SelectedIndex = -1;
            }

            // Now that we know no other part will be selected, assign this 'similarTo' value to the 
            // replacement part selection

            // Null-guard, since the replacementPartsListbox can be erased from other entities randomly
            if (similarBlockList.SelectedValue != null)
            {
                _replacementPartSelection = similarBlockList.SelectedValue.ToString();
                replacementPartTextBlock.Text = _replacementPartSelection;
            }
            else
            {
                // If it's null...set the index correctly?
                similarBlockList.SelectedIndex = -1;
                replacementPartTextBlock.Text = "[NO REPLACEMENT PART SELECTED]\n";
            }
            // redraw screen
            this.InvalidateVisual();

        }
        private void DetectedPartsListbox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // null-guard, because things happen?
            if (currentPartsListbox.SelectedItem != null)
            {
                // If there is an item selected in the currentPartsListbox: That is the 'original' part that
                // partswapper will replace.
                _originalPartSelection = currentPartsListbox.SelectedItem.ToString();
                originalPartTextBlock.Text = _originalPartSelection;

                // Update Similar blocks
                updateObservableSimilarTo();

            }
            else
            {
                _originalPartSelection = "";
                originalPartTextBlock.Text = "[NO PART SELECTED]";
            }
        }

        private void PartsListbox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            // Null-guard, since the replacementPartsListbox can be erased from other entities randomly
            if (replacementPartsListbox.SelectedValue != null)
            {
                _replacementPartSelection = replacementPartsListbox.SelectedValue.ToString();
                replacementPartTextBlock.Text = _replacementPartSelection;
            }
            else
            {
                // If it's null...set the index correctly?
                replacementPartsListbox.SelectedIndex = -1;
                replacementPartTextBlock.Text = "[NO REPLACEMENT PART SELECTED]\n";
            }
            // redraw screen
            this.InvalidateVisual();
        }

        private void SwapPartsButton_Click(object sender, RoutedEventArgs e)
        {
            if (_replacementPartSelection != null && _originalPartSelection != null)
            {
                numPartsReplaced = _partswapper.PartswapViaSubtypeName(_originalPartSelection, _replacementPartSelection, _debug);
                numberReplacedTextBlock.Text = numPartsReplaced.ToString() + "parts replaced!\n";
                transactionsListBox.Items.Add($"Replaced:\n{numPartsReplaced} {_originalPartSelection}\nWith:\n{_replacementPartSelection}\n-----");
                // Reload the lists of ship parts, hopefully changes are reflected
                updateObservableAvailBlockCategories();
                updateObservableCurrentShipParts();
                updateObservableSimilarTo();
            }
            // redraw screen
            this.InvalidateVisual();
        }

        // At the end of LocateModsDir_Click the modsDirPathKey should be set to some value - even if empty!
        // This method prompts the user to find their mods directory,
        // Then saves that chosen directory to a config file, as well as recursively loads block variants into various datastructures
        private void LocateModsDir_Click(object sender, RoutedEventArgs e)
        {
            OpenFolderDialog folderDialogue = new OpenFolderDialog();
            folderDialogue.Title = "Please choose the root workshop mod folder";
            folderDialogue.DefaultDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            folderDialogue.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            folderDialogue.ShowDialog();

            // If the dialogue returns the DefaultDirectory/InitialDirectory...we assume the user chose nothing.
            if (!folderDialogue.FolderName.Equals("") && folderDialogue.FolderName.Equals(folderDialogue.DefaultDirectory) && folderDialogue.FolderName.Equals(folderDialogue.InitialDirectory))
            {
                // Assume that nothing was chosen. Return without effect.
                // 05FEB2024: Turns out - if nothing is chosen: folderDialogue.FolderName ends up being NULL
                return;
            }
            else
            {
                // Otherwise: Assume the user pointed us to the correct directory and start loading.
                LoadModDirectories(folderDialogue.FolderName);

                // Update the _ConfigOptions instance to reflect the new 
                if (_configOptions == null)
                {
                    // create a new ConfigOptions
                    _configOptions = new ConfigOptions(_modsDirOptKey, folderDialogue.FolderName, _appDataPath);

                    // Save the new _configOptions immediately
                    _configOptions.SaveConfig();
                }
                else
                {
                    _configOptions.SetOption(_modsDirOptKey, folderDialogue.FolderName);
                    _configOptions.SaveConfig();
                }
            }
        }



        // modpath should, ideally, be the workshop mod path $steamapps$/workshop/content/244850
        // LoadModDirectories modifies _availableBlockCategories
        private void LoadModDirectories(string modpath)
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

            // Clear the lists we are about to repopulate
            _availableBlockCategoriesOC.Clear();

            try
            {
                if (modpath == null || modpath.Equals(""))
                {
                    // This happens when the user cancels out of the mod selection folder.
                    // We'll handle this more gracefully in the future. Maybe.
                    return;
                }

                // assign the modFolderRoot
                modFolderRoot = new DirectoryInfo(modpath);

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in LoadModDirectories!\n{ex}");
                throw new InvalidDataException("modpath invalid!");
            }



            if (!modFolderRoot.FullName.Contains("workshop\\content\\244850"))
            {
                MessageBox.Show("Error: Invalid folder!\nPath should end in: workshop\\content\\244850\n");
                return;
            }

            // We iterate through each of those mod directories, to begin our search for relevant files that contain part definitions
            foreach (DirectoryInfo modFolder in modFolderRoot.GetDirectories())
            {
                modFolderDirectories = modFolder.GetDirectories();

                parts = new HashSet<string>();
                appendedSet = new HashSet<string>();

                // First, check if the modFolder (specifically: its directories via modFolderDirectories) contains a "Data" folder
                if (modFolderDirectories.Any(x => x.Name == "Data"))
                {
                    // If a "Data" folder exists, look for the BlockVariantGroupsSBC_BlockVariantGroup files
                    // TODO: Maybe someday we can also use BlockCategories? Not sure which would be best...
                    dataDirectory = new DirectoryInfo(System.IO.Path.Combine(modFolder.FullName, "Data"));
                    dataFiles = dataDirectory.GetFiles();

                    // If one of the dataFiles is named "BlockVariantGroups.sbc"
                    if (dataFiles.Any(x => x.Name.Equals("BlockVariantGroups.sbc")))
                    {
                        // Get the fileInfo via path-combining the data directory path with the word "BlockVariantGroups". Then loaad the file into XML.
                        blockVariantGroupFile = new FileInfo(System.IO.Path.Combine(dataDirectory.FullName, "BlockVariantGroups.sbc"));

                        // Loading the blockVariantGroupsXML file into an XMLDocument we can read
                        blockVariantGroupsXML = XElement.Load(blockVariantGroupFile.OpenRead());
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

                                // Add the category name to the list visible via GUI
                                _availableBlockCategoriesOC.Add(categoryName);

                                foreach (XElement block in blockVariantGroup.Element("Blocks").Elements())
                                {
                                    // Add the block subtype to the set of part categories we are building
                                    partName = block.Attribute("Subtype").Value;
                                    parts.Add(partName);
                                }

                                if (_availableBlockCategories.ContainsKey(categoryName))
                                {
                                    appendedSet = _availableBlockCategories[categoryName].Union(parts).ToHashSet();
                                    _availableBlockCategories[categoryName] = appendedSet;
                                }
                                else
                                {
                                    // And this is where we add the newly-generated categoryName:partCategoriesSet pair I referenced earlier!
                                    _availableBlockCategories.Add(categoryName, parts);
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

                                foreach (XElement block in blockVariantGroupNode.Element("Blocks").Elements())
                                {
                                    // Add the block subtype to the set of part categories we are building
                                    partName = block.Attribute("Subtype").Value;
                                    parts.Add(partName);
                                }

                                if (_availableBlockCategories.ContainsKey(categoryName))
                                {
                                    _availableBlockCategories[categoryName] = _availableBlockCategories[categoryName].Union(parts).ToHashSet();
                                }
                                else
                                {
                                    // And this is where we add the newly-generated categoryName:partCategoriesSet pair I referenced earlier!
                                    _availableBlockCategories.Add(categoryName, parts);
                                }
                                parts = new HashSet<string>();
                                appendedSet = new HashSet<string>();
                            }
                        }

                        // clear the group and groups nodes
                        blockVariantGroupsNode = null;
                        blockVariantGroupNode = null;


                    }
                }

            }
        }

        private void exitButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void autoTechButton_Click(object sender, RoutedEventArgs e)
        {
            AutoTechWindow autoTechWindow = new AutoTechWindow(_partswapper, transactionsListBox);
            autoTechWindow.Show();
        }

        private void removePartsButton_Click(object sender, RoutedEventArgs e)
        {
            RemoveAllExceptBlocksDialogue removeBlocksDialogue = new RemoveAllExceptBlocksDialogue(_partswapper, transactionsListBox);
            removeBlocksDialogue.ShowDialog();
        }

        private void removeSpecificButton_Click(object sender, RoutedEventArgs e)
        {
            RemoveSpecificBlocksDialogue removeBlocksDialogue = new RemoveSpecificBlocksDialogue(_partswapper, transactionsListBox);
            removeBlocksDialogue.ShowDialog();
        }

        private void armorSwapButton_Click(object sender, RoutedEventArgs e)
        {
            List<string> transactionLog;

            string messageBoxText;
            string caption = "Executing Armor Operation!";

            MessageBoxButton button = MessageBoxButton.YesNo;
            MessageBoxImage icon = MessageBoxImage.Warning;
            MessageBoxResult messageBoxResult;

            // Convert light armor to heavy armor
            if (convertLightArmorToHeavyBttn.IsChecked.HasValue && convertLightArmorToHeavyBttn.IsChecked.Value)
            {
                messageBoxText = $"Warning: You are about to convert all light armor blocks on this grid to heavy armor!\nAre you sure you want to do this?";

                messageBoxResult = MessageBox.Show(messageBoxText, caption, button, icon);

                if (messageBoxResult == MessageBoxResult.Yes)
                {
                    transactionLog = _partswapper.AutoArmor_LightToHeavy();

                    foreach (string transactionLogItem in transactionLog)
                    {
                        transactionsListBox.Items.Add(transactionLogItem);
                    }
                    MessageBox.Show($"Replaced {transactionLog.Count} blocks of armor!");
                }
                return;
            }


            // Convert heavy armor to light armor
            if (convertHeavyArmorToLightBttn.IsChecked.HasValue && convertHeavyArmorToLightBttn.IsChecked.Value)
            {
                messageBoxText = $"Warning: You are about to convert all heavy armor blocks on this grid to light armor!\nAre you sure you want to do this?";

                messageBoxResult = MessageBox.Show(messageBoxText, caption, button, icon);

                if (messageBoxResult == MessageBoxResult.Yes)
                {
                    transactionLog = _partswapper.AutoArmor_HeavyToLight();

                    foreach (string transactionLogItem in transactionLog)
                    {
                        transactionsListBox.Items.Add(transactionLogItem);
                    }
                    MessageBox.Show($"Replaced {transactionLog.Count} blocks of armor!");
                }
                return;
            }

            // Delete light armor only
            if (removeLightArmorOnlyRadioRadBttn.IsChecked.HasValue && removeLightArmorOnlyRadioRadBttn.IsChecked.Value)
            {
                messageBoxText = $"Warning: You are about to remove all light armor blocks from this grid!\nAre you sure you want to do this?";

                messageBoxResult = MessageBox.Show(messageBoxText, caption, button, icon);

                if (messageBoxResult == MessageBoxResult.Yes)
                {
                    transactionLog = _partswapper.Remove_Armor(false,true);

                    foreach (string transactionLogItem in transactionLog)
                    {
                        transactionsListBox.Items.Add(transactionLogItem);
                    }
                    MessageBox.Show($"Removed {transactionLog.Count} blocks of armor!");
                }
                return;
            }

            // Delete heavy armor only
            if (removeHeavyArmorOnlyRadioRadBttn.IsChecked.HasValue && removeHeavyArmorOnlyRadioRadBttn.IsChecked.Value)
            {
                messageBoxText = $"Warning: You are about to remove all heavy armor blocks from this grid!\nAre you sure you want to do this?";

                messageBoxResult = MessageBox.Show(messageBoxText, caption, button, icon);

                if (messageBoxResult == MessageBoxResult.Yes)
                {
                    transactionLog = _partswapper.Remove_Armor(true, false);

                    foreach (string transactionLogItem in transactionLog)
                    {
                        transactionsListBox.Items.Add(transactionLogItem);
                    }
                    MessageBox.Show($"Removed {transactionLog.Count} blocks of armor!");
                }
                return;
            }

            // Delete all armor
            if (removeAllArmorRadioRadBttn.IsChecked.HasValue && removeAllArmorRadioRadBttn.IsChecked.Value)
            {
                messageBoxText = $"Warning: You are about to remove all armor blocks from this grid!\nAre you sure you want to do this?";

                messageBoxResult = MessageBox.Show(messageBoxText, caption, button, icon);

                if (messageBoxResult == MessageBoxResult.Yes)
                {
                    transactionLog = _partswapper.Remove_Armor(false, false);

                    foreach (string transactionLogItem in transactionLog)
                    {
                        transactionsListBox.Items.Add(transactionLogItem);
                    }
                    MessageBox.Show($"Removed {transactionLog.Count} blocks of armor!");
                }
                return;
            }

            // Delete all EXCEPT armor
            if (removeAllButArmorRadBttn.IsChecked.HasValue && removeAllButArmorRadBttn.IsChecked.Value)
            {
                messageBoxText = $"Warning: You are about to remove all non-armor blocks from this grid!\nAre you sure you want to do this?";

                messageBoxResult = MessageBox.Show(messageBoxText, caption, button, icon);

                if (messageBoxResult == MessageBoxResult.Yes)
                {
                    transactionLog = _partswapper.RemoveAllExceptArmor();

                    foreach (string transactionLogItem in transactionLog)
                    {
                        transactionsListBox.Items.Add(transactionLogItem);
                    }
                    MessageBox.Show($"Removed {transactionLog.Count} blocks of armor!");
                }
                return;
            }


            // Delete Vanilla Armor, Convert to Tritanium armor
            if (stcAutoTritArmorRadBttn.IsChecked.HasValue && stcAutoTritArmorRadBttn.IsChecked.Value)
            {
                messageBoxText = $"Warning: You are about to convert all armor to tritanium!\nAre you sure you want to do this?";

                messageBoxResult = MessageBox.Show(messageBoxText, caption, button, icon);

                if (messageBoxResult == MessageBoxResult.Yes)
                {
                    transactionLog = _partswapper.STC_AutoArmor_To_Tritanium();

                    foreach (string transactionLogItem in transactionLog)
                    {
                        transactionsListBox.Items.Add(transactionLogItem);
                    }
                    MessageBox.Show($"Swapped {transactionLog.Count} blocks of armor!");
                }
                return;
            }

            // Delete Tritanium armor, Convert to Heavy/Light
            if (stcAutoTritToVanillaArmorRadBttn.IsChecked.HasValue && stcAutoTritToVanillaArmorRadBttn.IsChecked.Value)
            {
                messageBoxText = $"Warning: You are about to convert all armor from tritanium to light armor!\nAre you sure you want to do this?";

                messageBoxResult = MessageBox.Show(messageBoxText, caption, button, icon);

                if (messageBoxResult == MessageBoxResult.Yes)
                {
                    transactionLog = _partswapper.STC_AutoArmor_From_Tritanium();

                    foreach (string transactionLogItem in transactionLog)
                    {
                        transactionsListBox.Items.Add(transactionLogItem);
                    }
                    MessageBox.Show($"Removed {transactionLog.Count} blocks of armor!");
                }
                return;
            }
        }

    }
}
