using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.IO;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Linq;
using PartSwapperXMLSE;
using Microsoft.Win32;
using PartSwapperGUI.WCStatsAndPlots;

namespace PartSwapperGUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static string appDataPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PicarlsPartswapper", "settings.json");
        private static string modsDirOptKey = "modsDir";
        private static string seBaseDirKey = "seBaseDir";

        private PartSwapper? _partswapperInstance;
        private string _cubegridFilePath;
        private string? _SEInstallFolderPath;
        private string? _SEWorkshopFolderPath;
        private string _selectedCubegridName;
        private ConfigOptions _config;
        private bool _debug = false;

        public MainWindow()
        {
            MainWindowInitStart:
            InitializeComponent();

            partswapperStatusTextBlock.Text = "Status: Initialized\nPlease Select a _BlueprintDefinitionRef (.sbc) file!";
            _cubegridFilePath = "";
            _selectedCubegridName = "";
            _config = new ConfigOptions(appDataPath);
            _config.LoadOrCreateConfig();
            _config.SetOption("debug", "false");

            if (_config.GetOption(seBaseDirKey) == null)
            {
                //MessageBox.Show("Please locate your SE Installation Folder for Space Engineers at the next prompt!\n" +
                //    "The folder path should end in: \\steamapps\\common\\SpaceEngineers", "SE Installation Folder Not Found!");
                PromptLocateSEbaseDir();

                _SEInstallFolderPath = _config.GetOption(seBaseDirKey);
            }
            else
            {
                _SEInstallFolderPath = _config.GetOption(seBaseDirKey);
            }

            if (_config.GetOption(modsDirOptKey) == null)
            {
                //MessageBox.Show("Please locate your Workshop Mods folder for Space Engineers at the next prompt!\n" +
                //    "The folder path should end in: \\steamapps\\workshop\\content\\244850\\", "SE Mods Folder Not Found!");
                PromptLocateModsDir();
                _SEWorkshopFolderPath = _config.GetOption(modsDirOptKey);
            } else
            {
                _SEWorkshopFolderPath = _config.GetOption(modsDirOptKey);
            }

            // Final check for null folder paths, restarting the instantiation process if necessary
            if(_SEInstallFolderPath == null)
            {
                goto MainWindowInitStart;
            }

            if(_SEWorkshopFolderPath == null)
            {
                goto MainWindowInitStart;
            }
        }

        // Prompts the user to locate their Mods Dir, then sets the _config object appropriately.
        private void PromptLocateModsDir()
        {

        PromptLoop:

            OpenFolderDialog folderDialogue = new OpenFolderDialog();
            string locateSEDiagCaption = "Locate your Space Engineers Workshop Directory in order to use Picarl's Partswapper!";
            MessageBoxButton locateSEDiagButton = MessageBoxButton.OKCancel;
            MessageBoxImage locateSEDiagImage = MessageBoxImage.Exclamation;

            // Show the dialog box
            MessageBoxResult locateDirDialogResult = MessageBox.Show("Please locate your SE Workshop Directory.\n(Typically ending in: ...steamapps\\workshop\\content\\244850)\n", locateSEDiagCaption, locateSEDiagButton, locateSEDiagImage, MessageBoxResult.Cancel);

            if (locateDirDialogResult == MessageBoxResult.Cancel)
            {
                MessageBox.Show("User did not select SE Workshop Directory! Quitting!");

                this.Close();
                Environment.Exit(0);
            }

            folderDialogue.Title = "Please locate the Space Engineers Workshop Directory! (Typically: ...steamapps\\workshop\\content\\244850)";
            folderDialogue.DefaultDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            folderDialogue.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            folderDialogue.ShowDialog();


            // If the dialogue returns the DefaultDirectory/InitialDirectory...we assume the user chose nothing.
            if (folderDialogue.FolderName.Equals(""))
            {
                // Force the user to select a valid folder
                goto PromptLoop;
            }
            else
            {
                // Update the _ConfigOptions instance to reflect the new 
                if (_config == null)
                {
                    // create a new ConfigOptions
                    _config = new ConfigOptions(modsDirOptKey, folderDialogue.FolderName, appDataPath);

                    // Save the new _configOptions immediately
                    _config.SaveConfig();
                }
                else
                {
                    _config.SetOption(modsDirOptKey, folderDialogue.FolderName);
                    _config.SaveConfig();
                }
            }
        }

        private void PromptLocateSEbaseDir()
        {
            PromptLoop:

            OpenFolderDialog folderDialogue = new OpenFolderDialog();
            string locateSEDiagCaption = "You must locate your Space Engineers Base Directory (steamapp folder) in order to use Picarl's Partswapper!";
            MessageBoxButton locateSEDiagButton = MessageBoxButton.OKCancel;
            MessageBoxImage locateSEDiagImage = MessageBoxImage.Exclamation;
            MessageBoxResult locateSEDiageResult = MessageBox.Show("Please locate your SE Directory!\n(Typically ending in: ...steamapps\\common\\SpaceEngineers)", locateSEDiagCaption, locateSEDiagButton,locateSEDiagImage,MessageBoxResult.Cancel);

            if (locateSEDiageResult == MessageBoxResult.Cancel)
            {
                MessageBox.Show("User did not select SE Directory! Quitting!");
                
                this.Close();
                Environment.Exit(0);
            }

            folderDialogue.Title = "Please choose the \"Space Engineers\" folder!";
            folderDialogue.DefaultDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            folderDialogue.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            folderDialogue.ShowDialog();

            // If the dialogue returns the DefaultDirectory/InitialDirectory...we assume the user chose nothing.
            if (folderDialogue.FolderName.Equals(""))
            {
                // Force the user to restart until they select something
                goto PromptLoop;
            }
            else
            {
                // Update the _ConfigOptions instance to reflect the new 
                if (_config == null)
                {
                    // create a new ConfigOptions
                    _config = new ConfigOptions(seBaseDirKey, folderDialogue.FolderName, appDataPath);

                    // Save the new _configOptions immediately
                    _config.SaveConfig();
                }
                else
                {
                    _config.SetOption(seBaseDirKey, folderDialogue.FolderName);
                    _config.SaveConfig();
                }
            }
        }


        private void ChooseFileButton_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog fileDialogue = new Microsoft.Win32.OpenFileDialog();
            fileDialogue.FileName = "Spaceship"; // Default file name
            fileDialogue.DefaultExt = ".sbc"; // Default file extension
            fileDialogue.Filter = "SE Definition Files (.sbc)|*.sbc"; // Filter files by extension
            fileDialogue.Title = "Select a grid definition...";
            fileDialogue.InitialDirectory = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SpaceEngineers\\Blueprints");

            // Show open file dialog box
            Nullable<bool> filenameBool = fileDialogue.ShowDialog();

            // Process open file dialog box results
            if (filenameBool == true)
            {
                // Open document
                _cubegridFilePath = fileDialogue.FileName;

                // Update currentBlueprintLabel information
                currentBlueprintLabel.Text = "Selected _BlueprintDefinitionRef:\n" + _cubegridFilePath + "\n";
            }

            // Now that the file has been selected, and if the _SEInstallFolderPath is selected - init a new PartSwapper instance!
            if (_cubegridFilePath.Length > 0 && _SEInstallFolderPath != null)
            {
                _partswapperInstance = new PartSwapper(_cubegridFilePath, _SEInstallFolderPath, _SEWorkshopFolderPath, _debug);

                PopulatePartswapperFields();

                // Enable the appropriate buttons and update status
                CubegridSelect.Visibility = Visibility.Visible;

                partswapperStatusTextBlock.Text = "Status: Ship loaded!\nSelect a BlueprintSBC_CubeGrid To Work On!";
                CubegridSelect.Visibility = Visibility.Visible;

                // Enable the xmlViewerButton
                xmlViewerButton.IsEnabled = true;

                // Enable the Grouper button
                grouperButton.IsEnabled = true;
            } else
            {
                MessageBox.Show("Invalid file selected!");
            }
        }

        private void PopulatePartswapperFields()
        {

            if (_partswapperInstance == null)
            {
                MessageBox.Show("No ship selected!");
                return;
            }
            if (_partswapperInstance.GetShipRoot() == null)
            {
                MessageBox.Show("Error getting shiproot!");
                return;
            }

            //PopulateBPCTreeView(_PartswapperInstance.GetShipRoot());
            PopulateCubegridListbox(_partswapperInstance.GetShipRoot());
        }



        private void PopulateCubegridListbox(XElement shiproot)
        {
            XElement cubegrids = shiproot.Element("ShipBlueprints").Element("ShipBlueprint").Element("CubeGrids");

            cubegridListBox.Items.Clear();

            foreach (XElement cubegrid in cubegrids.Elements())
            {
                cubegridListBox.Items.Add(cubegrid.Element("DisplayName").Value);
            }
        }

        private void RenderGridButton_Click(object sender, RoutedEventArgs e)
        {
            if (_partswapperInstance.GetCurrentCubegrid() == null)
            {
                MessageBox.Show("Error: GetCurrentCubegrid is null!");
                return;
            }
            GridRenderer renderWindow = new GridRenderer(_partswapperInstance.GetCurrentCubegrid());
            renderWindow.Show();
        }

        private void CubegridListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cubegridListBox.SelectedItem != null)
            {
                XElement cubegrids = _partswapperInstance.GetShipRoot().Element("ShipBlueprints").Element("ShipBlueprint").Element("CubeGrids");
                _selectedCubegridName = cubegridListBox.SelectedItem.ToString();

                //TODO: Replace this longwinded loop with something more efficient, when you figure out how to dive into XML better.
                // Should be able to do something like "Find in XElement where DisplayName.Value == _selectedCubegridName"
                // This lazy solution will work for now.
                foreach (XElement cubegrid in cubegrids.Elements())
                {
                    if (cubegrid.Element("DisplayName").Value == _selectedCubegridName)
                    {
                        _partswapperInstance.SetCurrentCubegrid(cubegrid);
                        _partswapperInstance.GenerateShipPartsList();

                        if (_partswapperInstance.GetShipParts() == null)
                        {
                            MessageBox.Show("Error populating ShipParts list!\nGetShipParts() is null!");
                        }
                        else
                        {
                            // Enable the renderGridButton
                            renderGridButton.IsEnabled = true;

                            // Enable the PartSwap button
                            partswapButton.IsEnabled = true;
                        }
                        return;
                    }
                }

            }
            else
            {
                // If the selected item is null, somehow, just return...for now.
                // Make sure the user can't press any buttons they shouldn't, given the invalid state
                renderGridButton.IsEnabled = false;
                partswapButton.IsEnabled = false;
                return;
            }
        }

        private void Partswap_Click(object sender, RoutedEventArgs e)
        {
            PartswapperGUI partswapWindow = new PartSwapperGUI.PartswapperGUI(_partswapperInstance,_selectedCubegridName);
            partswapWindow.Show();
        }

        private void OptionsButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: This needs a config object passed in
            Options optsWindow = new Options(_config);
            optsWindow.Show();
        }

        private void XMLViewerButton_Click(object sender, RoutedEventArgs e)
        {
            XMLViewer xmlView = new XMLViewer(_partswapperInstance.GetShipRoot());
            xmlView.Show();
        }

        private void WeaponcoreStatsButton_Click(object sender, RoutedEventArgs e)
        {
            WeaponcoreStatsWindow wcStatsWindow = new WeaponcoreStatsWindow(_config);
            wcStatsWindow.Show();
        }

        private void DebugToggleButton_Click(object sender, RoutedEventArgs e)
        {
            _debug = !_debug;

            if (_debug)
            {
                debugToggleButton.Content = "Debug ON";
            }
            else
            {
                debugToggleButton.Content = "Debug OFF";
            }
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void GrouperButton_Click(object sender, RoutedEventArgs e)
        {
            Grouper grouperWindows = new Grouper(_partswapperInstance);
            grouperWindows.Show();
        }
    }
}
