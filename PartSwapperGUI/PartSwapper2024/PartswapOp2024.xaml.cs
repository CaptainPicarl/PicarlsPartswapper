using PartSwapperXMLSE;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace PartSwapperGUI.PartSwapper2024
{
    /// <summary>
    /// Interaction logic for PartswapOp2024Window.xaml
    /// </summary>
    public partial class PartswapOp2024Window : Window
    {
        private PartSwapper2024 _PartswapperRef;

        BlueprintSBC_CubeGrid? CurrentCubeGrid;
        BlueprintSBC_CubeBlock? CurrentCubeBlock;

        // All blocks detected in the base game and modblocks
        Dictionary<string, List<CubeBlockDefinitionSBC_CubeBlockDefinition>> VanillaBlocks = new Dictionary<string, List<CubeBlockDefinitionSBC_CubeBlockDefinition>>();
        Dictionary<string, List<CubeBlockDefinitionSBC_CubeBlockDefinition>> ModBlocks = new Dictionary<string, List<CubeBlockDefinitionSBC_CubeBlockDefinition>>();
        Dictionary<string, List<CubeBlockDefinitionSBC_CubeBlockDefinition>> GridBlocks = new Dictionary<string, List<CubeBlockDefinitionSBC_CubeBlockDefinition>>();

        // Block search variables
        Dictionary<DefinitionSource, List<CubeBlockDefinitionSBC_CubeBlockDefinition>> BlockSearchResults = new Dictionary<DefinitionSource, List<CubeBlockDefinitionSBC_CubeBlockDefinition>>();
        bool BlockDefinitionFound = false;

        // GUI Elements to build
        ComboBox GridSourceComboBox = new ComboBox();
        ComboBox GridPartsComboBox = new ComboBox();

        ComboBox PossiblePartsComboBox = new ComboBox();

        ComboBox AvailableSourceComboBox = new ComboBox();
        ComboBox AvailablePartsComboBox = new ComboBox();

        TextBlock SelectedGridPartTextBlock = new TextBlock();
        TextBlock SelectGridTextBlock = new TextBlock();

        TextBlock SelectedAvailablePartTextBlock = new TextBlock();
        TextBlock SelectedAvailablePartSourceTextBlock = new TextBlock();

        TextBlock SearchAvailablePartsTextBlock = new TextBlock();
        TextBlock SearchGridPartsTextBlock = new TextBlock();

        TextBlock PossiblePartDefsTextBlock = new TextBlock();

        TextBox GridPartValueSearchBox = new TextBox();
        TextBox AvailablePartValueSearchBox = new TextBox();

        StackPanel LeftControlStackPanel = new StackPanel();
        StackPanel RightControlStackPanel = new StackPanel();

        ListBox TransactionsListBox = new ListBox();

        Button SwapButton = new Button();
        Button ViewAvailableDefinitionButton = new Button();
        Button ViewGridPartDefinitionButton = new Button();

        // Variables related to GUI Selections
        CubeBlockDefinitionSBC_CubeBlockDefinition? CurrentShipDefSelection;
        CubeBlockDefinitionSBC_CubeBlockDefinition? CurrentAvailableBlocksDefSelection;

        CubeBlockDefinitionSBC_CubeBlockDefinition? PossibleDefinitionsSelection;

        Dictionary<DefinitionSource, List<CubeBlockDefinitionSBC_CubeBlockDefinition>> BlockSearchResult;

        // SP = "StackPanel", AKA: Referring to everything in the left/right stack panels
        Thickness SPTextBlockMarginThickness = new Thickness(0);
        Thickness SPElementMarginThickness = new Thickness(0);
        Thickness SPMarginThickness = new Thickness(0);

        double SPElementWidth = 100;

        private TransactionLog _TransactionsLogRef;
        private TransactionWindow2024 _TransactionLogWindow;

        public PartswapOp2024Window(ref PartSwapper2024 PartSwapperRef, ref BlueprintSBC_CubeGrid cubeGrid, DataFolder_Model workshopModFolder, DataFolder_Model vanillaModFolder)
        {
            InitializeComponent();

            this.Loaded += OnPartswapOp2024Window_Loaded;

            this.ResizeMode = ResizeMode.NoResize;
            this.SizeToContent = SizeToContent.WidthAndHeight;

            this._PartswapperRef = PartSwapperRef;
            this._TransactionsLogRef = this._PartswapperRef.MasterLogRef;
            this.CurrentCubeGrid = cubeGrid;

            this.VanillaBlocks = vanillaModFolder.GetCubeBlocksDefDict();
            this.ModBlocks = workshopModFolder.GetCubeBlocksDefDict();

            // Add entries to the listbox for each Vanilla block
            foreach (KeyValuePair<string, List<CubeBlockDefinitionSBC_CubeBlockDefinition>> kvp in VanillaBlocks)
            {
                if (!this.AvailableSourceComboBox.Items.Contains(kvp.Key))
                {
                    this.AvailableSourceComboBox.Items.Add(kvp.Key);
                }
            }

            // Add entries to the listbox for each Mod block
            foreach (KeyValuePair<string, List<CubeBlockDefinitionSBC_CubeBlockDefinition>> kvp in ModBlocks)
            {
                if (!this.AvailableSourceComboBox.Items.Contains(kvp.Key))
                {
                    this.AvailableSourceComboBox.Items.Add(kvp.Key);
                }
            }

            this.GridSourceComboBox.Items.Add(this.CurrentCubeGrid.GetDisplayName());

            foreach (BlueprintSBC_CubeBlock cubeblock in LoadCurrentShipPartsCategories(this.CurrentCubeGrid))
            {
                this.BlockSearchResult = this._PartswapperRef.SearchCubeBlockDefinitions(cubeblock);

                if (!this.GridBlocks.ContainsKey(CurrentCubeGrid.GetDisplayName()))
                {
                    this.GridBlocks.Add(CurrentCubeGrid.GetDisplayName(), new List<CubeBlockDefinitionSBC_CubeBlockDefinition>());
                }

                foreach (List<CubeBlockDefinitionSBC_CubeBlockDefinition> BlockList in this.BlockSearchResult.Values)
                {
                    foreach (CubeBlockDefinitionSBC_CubeBlockDefinition block in BlockList)
                    {
                        if (!this.GridBlocks[CurrentCubeGrid.GetDisplayName()].Contains(block))
                        {
                            this.GridBlocks[CurrentCubeGrid.GetDisplayName()].Add(block);
                        }
                    }
                }
            }

            this.GridSourceComboBox.SelectionChanged += OnGridSourceComboBox_SelectionChanged;
            this.GridSourceComboBox.Margin = SPElementMarginThickness;
            this.GridSourceComboBox.Width = SPElementWidth;
            this.GridSourceComboBox.SelectedIndex = 0;

            this.GridPartsComboBox.SelectionChanged += OnGridParts_SelectionChanged;
            this.GridPartsComboBox.Margin = SPElementMarginThickness;
            this.GridPartsComboBox.Width = SPElementWidth;

            this.AvailablePartsComboBox.SelectionChanged += OnAvailableParts_SelectionChanged;
            this.AvailablePartsComboBox.Margin = SPElementMarginThickness;
            this.AvailablePartsComboBox.Width = SPElementWidth;

            this.AvailableSourceComboBox.SelectionChanged += OnAvailableSourceComboBox_SelectionChanged;
            this.AvailableSourceComboBox.Margin = SPElementMarginThickness;
            this.AvailableSourceComboBox.Width = SPElementWidth;

            this.SelectedAvailablePartTextBlock.Text = "Select a part from chosen source:";
            this.SelectedAvailablePartTextBlock.Margin = SPTextBlockMarginThickness;
            this.SelectedAvailablePartTextBlock.Width = SPElementWidth;
            this.SelectedAvailablePartTextBlock.TextWrapping = TextWrapping.WrapWithOverflow;

            this.SelectedAvailablePartSourceTextBlock.Text = "Select a part source SBC";
            this.SelectedAvailablePartSourceTextBlock.Margin = SPTextBlockMarginThickness;
            this.SelectedAvailablePartSourceTextBlock.Width = SPElementWidth;
            this.SelectedAvailablePartSourceTextBlock.TextWrapping = TextWrapping.WrapWithOverflow;

            this.AvailablePartsComboBox.Text = "Possible available part definitions";
            this.AvailablePartsComboBox.Margin = SPTextBlockMarginThickness;
            this.AvailablePartsComboBox.Width = SPElementWidth;

            this.SearchAvailablePartsTextBlock.Text = "Enter a term to filter sources by";
            this.SearchAvailablePartsTextBlock.Margin = SPTextBlockMarginThickness;
            this.SearchAvailablePartsTextBlock.Width = SPElementWidth;
            this.SearchAvailablePartsTextBlock.TextWrapping = TextWrapping.WrapWithOverflow;

            this.AvailablePartValueSearchBox.Text = "Search selected part source";
            this.AvailablePartValueSearchBox.GotFocus += OnAvailablePartValueSearchBox_GotFocus;
            this.AvailablePartValueSearchBox.TextChanged += OnAvailablePartValueSearchBox_TextChanged;
            this.AvailablePartValueSearchBox.Margin = SPElementMarginThickness;
            this.AvailablePartValueSearchBox.Width = SPElementWidth;

            this.ViewAvailableDefinitionButton.Content = "View Selected Available Part Definition";
            this.ViewAvailableDefinitionButton.Click += OnViewDefinitionButton_Click;
            this.ViewAvailableDefinitionButton.Margin = SPElementMarginThickness;
            this.ViewAvailableDefinitionButton.Width = SPElementWidth;

            this.RightControlStackPanel.Children.Add(SearchAvailablePartsTextBlock);
            this.RightControlStackPanel.Children.Add(AvailablePartValueSearchBox);
            this.RightControlStackPanel.Children.Add(SelectedAvailablePartSourceTextBlock);
            this.RightControlStackPanel.Children.Add(AvailableSourceComboBox);
            this.RightControlStackPanel.Children.Add(SelectedAvailablePartTextBlock);
            this.RightControlStackPanel.Children.Add(AvailablePartsComboBox);
            this.RightControlStackPanel.Children.Add(ViewAvailableDefinitionButton);
            this.RightControlStackPanel.Margin = SPMarginThickness;

            this.SelectedGridPartTextBlock.Text = "Select a part from selected grid";
            this.SelectedGridPartTextBlock.Margin = SPTextBlockMarginThickness;
            this.SelectedGridPartTextBlock.Width = SPElementWidth;
            this.SelectedGridPartTextBlock.TextWrapping = TextWrapping.WrapWithOverflow;

            this.SelectGridTextBlock.Text = "Select a grid to populate parts list from";
            this.SelectGridTextBlock.Margin = SPTextBlockMarginThickness;
            this.SelectGridTextBlock.Width = SPElementWidth;
            this.SelectGridTextBlock.TextWrapping = TextWrapping.WrapWithOverflow;

            this.GridPartsComboBox.Text = "Possible grid part definitions";
            this.GridPartsComboBox.Margin = SPTextBlockMarginThickness;
            this.GridPartsComboBox.Width = SPElementWidth;

            this.SearchGridPartsTextBlock.Text = "Filter the Current Grid Parts List by the following term";
            this.SearchGridPartsTextBlock.Margin = SPTextBlockMarginThickness;
            this.SearchGridPartsTextBlock.Width = SPElementWidth;
            this.SearchGridPartsTextBlock.TextWrapping = TextWrapping.WrapWithOverflow;

            this.GridPartValueSearchBox.Text = "Search grid parts";
            this.GridPartValueSearchBox.GotFocus += OnGridPartValueSearchBox_GotFocus;
            this.GridPartValueSearchBox.TextChanged += OnGridPartValueSearchBox_TextChanged;
            this.GridPartValueSearchBox.Margin = SPElementMarginThickness;
            this.GridPartValueSearchBox.Width = SPElementWidth;

            this.ViewGridPartDefinitionButton.Content = "View Selected Part Definition";
            this.ViewGridPartDefinitionButton.Click += OnViewShipPartDefinitionButton_Click;
            this.ViewGridPartDefinitionButton.Margin = SPElementMarginThickness;
            this.ViewGridPartDefinitionButton.Width = SPElementWidth;

            this.PossiblePartDefsTextBlock.Text = "Possible definitions for selected ship part";
            this.PossiblePartDefsTextBlock.Margin = SPTextBlockMarginThickness;
            this.PossiblePartDefsTextBlock.Width = SPElementWidth;
            this.PossiblePartDefsTextBlock.TextWrapping = TextWrapping.WrapWithOverflow;

            this.PossiblePartsComboBox.Width = SPElementWidth;

            this.LeftControlStackPanel.Children.Add(SelectGridTextBlock);
            this.LeftControlStackPanel.Children.Add(GridSourceComboBox);
            this.LeftControlStackPanel.Children.Add(SearchGridPartsTextBlock);
            this.LeftControlStackPanel.Children.Add(GridPartValueSearchBox);
            this.LeftControlStackPanel.Children.Add(SelectedGridPartTextBlock);
            this.LeftControlStackPanel.Children.Add(GridPartsComboBox);

            this.LeftControlStackPanel.Children.Add(PossiblePartDefsTextBlock);
            this.LeftControlStackPanel.Children.Add(PossiblePartsComboBox);
            this.LeftControlStackPanel.Children.Add(ViewGridPartDefinitionButton);
            this.LeftControlStackPanel.Margin = SPMarginThickness;

            this.SwapButton.Content = GenerateButtonOperationText();
            this.SwapButton.Style = Application.Current.Resources["ButtonStyle2"] as Style;
            this.SwapButton.Click += SwapButton_Click;
            this.SwapButton.Margin = SPMarginThickness;


            this.L1_VerticalStackPanel_1.Children.Add(SwapButton);

            this.L2_LeftUIStackPanel.Children.Add(this.LeftControlStackPanel);
            this.L2_RightUIStackPanel.Children.Add(this.RightControlStackPanel);
        }

        private void OnPartswapOp2024Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.SPElementWidth = (this.Width / 2) - 10;

            this.GridPartsComboBox.Width = SPElementWidth;
            this.GridSourceComboBox.Width = SPElementWidth;
            this.AvailablePartsComboBox.Width = SPElementWidth;
            this.AvailableSourceComboBox.Width = SPElementWidth;
            this.SelectedAvailablePartTextBlock.Width = SPElementWidth;
            this.SelectedAvailablePartSourceTextBlock.Width = SPElementWidth;
            this.AvailablePartsComboBox.Width = SPElementWidth;
            this.SearchAvailablePartsTextBlock.Width = SPElementWidth;
            this.AvailablePartValueSearchBox.Width = SPElementWidth;
            this.ViewAvailableDefinitionButton.Width = SPElementWidth;
            this.SelectedGridPartTextBlock.Width = SPElementWidth;
            this.SelectGridTextBlock.Width = SPElementWidth;
            this.GridPartsComboBox.Width = SPElementWidth;
            this.SearchGridPartsTextBlock.Width = SPElementWidth;
            this.GridPartValueSearchBox.Width = SPElementWidth;
            this.ViewGridPartDefinitionButton.Width = SPElementWidth;
            this.PossiblePartDefsTextBlock.Width = SPElementWidth;
            this.PossiblePartsComboBox.Width = SPElementWidth;

            this.InvalidateMeasure();
            this.InvalidateVisual();
            this.InvalidateArrange();
        }

        private void OnAvailablePartValueSearchBox_GotFocus(object sender, RoutedEventArgs e)
        {
            this.AvailablePartValueSearchBox.Text = "";
        }

        private void OnGridPartValueSearchBox_GotFocus(object sender, RoutedEventArgs e)
        {
            this.GridPartValueSearchBox.Text = "";
        }

        private void OnGridPartValueSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if(GridPartsComboBox == null ||
                GridPartsComboBox.Items.Count == 0)
            {
                GridPartsComboBox.Items.Filter = null;
            }
            else {
                if (AvailablePartValueSearchBox.Text.Equals(""))
                {
                    GridPartsComboBox.Items.Filter = null;
                    return;
                }
                else
                {
                    GridPartsComboBox.Items.Filter = new Predicate<object>(item => (item as string).Contains(GridPartValueSearchBox.Text));
                }
            }
            
        }

        private void OnAvailablePartValueSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {


            if (AvailablePartValueSearchBox.Text.Equals(""))
            {
                AvailablePartsComboBox.Items.Filter = null;
                AvailableSourceComboBox.Items.Filter = null;
                return;
            }
            else
            {
                AvailableSourceComboBox.Items.Filter = new Predicate<object>(source => (ModBlocks.ContainsKey(source as string) && ModBlocks[(source as string)].Any(item => item.GetSubTypeID().Contains(AvailablePartValueSearchBox.Text))) ||
                                                                                       (VanillaBlocks.ContainsKey(source as string) && VanillaBlocks[(source as string)].Any(item => item.GetSubTypeID().Contains(AvailablePartValueSearchBox.Text))));
                AvailablePartsComboBox.Items.Filter = new Predicate<object>(item => (item as string).Contains(AvailablePartValueSearchBox.Text));
            }
        }

        private void OnGridSourceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                this.GridPartsComboBox.Items.Clear();

                if (e.AddedItems[0] is string)
                {
                    if (GridBlocks.ContainsKey(e.AddedItems[0] as string))
                    {
                        foreach (CubeBlockDefinitionSBC_CubeBlockDefinition cubeblockDefinition in GridBlocks[e.AddedItems[0] as string])
                        {
                            if (!GridPartsComboBox.Items.Contains(cubeblockDefinition.GetSubTypeID()))
                            {
                                GridPartsComboBox.Items.Add(cubeblockDefinition.GetSubTypeID());
                            }
                        }
                    }
                }
            }
            else
            {
                if (e.RemovedItems.Count > 0)
                {
                }
            }
        }

        private void OnAvailableSourceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                this.AvailablePartsComboBox.Items.Clear();

                if (e.AddedItems[0] is string)
                {
                    if (ModBlocks.ContainsKey(e.AddedItems[0] as string))
                    {
                        foreach (CubeBlockDefinitionSBC_CubeBlockDefinition block in ModBlocks[e.AddedItems[0] as string])
                        {
                            if (!AvailablePartsComboBox.Items.Contains(block.GetSubTypeID()))
                            {
                                AvailablePartsComboBox.Items.Add(block.GetSubTypeID());
                            }
                        }
                    }

                    if (VanillaBlocks.ContainsKey(e.AddedItems[0] as string))
                    {
                        foreach (CubeBlockDefinitionSBC_CubeBlockDefinition block in VanillaBlocks[e.AddedItems[0] as string])
                        {
                            if (!AvailablePartsComboBox.Items.Contains(block.GetSubTypeID()))
                            {
                                AvailablePartsComboBox.Items.Add(block.GetSubTypeID());
                            }
                        }
                    }

                    this.AvailablePartsComboBox.InvalidateVisual();
                }
            }
            else
            {
                if (e.RemovedItems.Count > 0)
                {
                }
            }
        }

        private void OnAvailableParts_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                this.BlockSearchResults = this._PartswapperRef.SearchCubeBlockDefinitions((e.AddedItems[0] as string));

                if (this.BlockSearchResults[DefinitionSource.Vanilla].Count > 0)
                {
                    this.CurrentAvailableBlocksDefSelection = this.BlockSearchResults[DefinitionSource.Vanilla].Count > 0 ? BlockSearchResults[DefinitionSource.Vanilla].First() : null;
                }
                else
                {
                    this.CurrentAvailableBlocksDefSelection = this.BlockSearchResults[DefinitionSource.Mod].Count > 0 ? BlockSearchResults[DefinitionSource.Mod].First() : null;

                }
            }
            else
            {
                // If exclusively deselection occurs
                if (e.RemovedItems.Count > 0)
                {
                }
            }

            this.SwapButton.Content = GenerateButtonOperationText();
        }

        private void OnGridParts_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                this.BlockSearchResults = this._PartswapperRef.SearchCubeBlockDefinitions((e.AddedItems[0] as string));

                // Decide which block we will defer to in the case where we find a modblock and a vanilla block. We'll choose vanilla first, modded second.

                if(this.BlockSearchResults[DefinitionSource.Vanilla].Count > 0)
                {
                    this.CurrentShipDefSelection = this.BlockSearchResults[DefinitionSource.Vanilla].Count > 0 ? BlockSearchResults[DefinitionSource.Vanilla].First() : null;
                } else
                {
                    this.CurrentShipDefSelection = this.BlockSearchResults[DefinitionSource.Mod].Count > 0 ? BlockSearchResults[DefinitionSource.Mod].First() : null;

                }

                //this.GridPartValueSearchBox.Text = this.CurrentShipDefSelection.GetDisplayName();

                // Next: Start populating the list/combobox of potential definitions, since there can be more than one!
                this.PossiblePartsComboBox.Items.Clear();

                ComboBoxItem comboBoxItemIter;

                foreach (CubeBlockDefinitionSBC_CubeBlockDefinition blockDef in this.BlockSearchResults[DefinitionSource.Vanilla])
                {
                    comboBoxItemIter = new ComboBoxItem();
                    comboBoxItemIter.Content = $"{blockDef.GetDisplayName()} -> {DefinitionSource.Vanilla.ToString()}";

                    this.PossiblePartsComboBox.Items.Add(comboBoxItemIter);
                }

                foreach (CubeBlockDefinitionSBC_CubeBlockDefinition blockDef in this.BlockSearchResults[DefinitionSource.Mod])
                {
                    comboBoxItemIter = new ComboBoxItem();
                    comboBoxItemIter.Content = $"{blockDef.GetDisplayName()} -> {DefinitionSource.Mod.ToString()} {blockDef.GetDefinitionSourceString()}";

                    this.PossiblePartsComboBox.Items.Add(comboBoxItemIter);
                }

                this.SwapButton.Content = GenerateButtonOperationText();

            }
            else
            {
                // If exclusively deselection occurs
                if (e.RemovedItems.Count > 0)
                {
                    this.BlockSearchResults = null;
                    this.PossiblePartsComboBox.Items.Clear();
                }
            }

        }

        private void OnViewDefinitionButton_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentAvailableBlocksDefSelection != null)
            {
                CubeblockDefinitionViewer cubeViewer = new CubeblockDefinitionViewer(ref this._PartswapperRef, CurrentAvailableBlocksDefSelection);
                cubeViewer.Show();
            }
        }

        private void OnViewShipPartDefinitionButton_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentShipDefSelection != null)
            {
                CubeblockDefinitionViewer cubeViewer = new CubeblockDefinitionViewer(ref this._PartswapperRef, CurrentShipDefSelection);
                cubeViewer.Show();
            }
        }

        private string MakeStringWPFFrameworkElementValid(string input)
        {
            return Regex.Replace(input, @"[^a-zA-Z0-9]", "_");
        }

        private string GenerateButtonOperationText()
        {
            string ButtonTextDefault = "Choose a part from your grid and an available part to swap!";

            switch (this.CurrentShipDefSelection == null)
            {
                case true:
                    switch (this.CurrentAvailableBlocksDefSelection == null)
                    {
                        case true:
                            return ButtonTextDefault;
                        case false:
                            return $"Swap <?> with {this.CurrentAvailableBlocksDefSelection.GetSubTypeID()}!";
                    }
                case false:
                    switch (this.CurrentAvailableBlocksDefSelection == null)
                    {
                        case true:
                            return $"Swap:\n{this.CurrentShipDefSelection.GetSubTypeID()}\nwith:\n<?>!";
                        case false:
                            return $"Swap\n{this.CurrentShipDefSelection.GetSubTypeID()}\nwith\n{this.CurrentAvailableBlocksDefSelection.GetSubTypeID()}!";
                    }
            }
        }

        private void SwapButton_Click(object sender, RoutedEventArgs e)
        {
            if(this.CurrentShipDefSelection == null ||
                this.CurrentAvailableBlocksDefSelection == null)
            {
                MessageBox.Show("Please select two parts to swap!");
            }

            TransactionLog transactionLog;
            
            this._PartswapperRef.BackupShipXML();

            transactionLog = this._PartswapperRef.PerformOperation("PartSwap", new Dictionary<string, object> {
                { "Operation", "PartSwap" },
                { "OldPart", CurrentShipDefSelection.GetSubTypeID() },
                { "NewPart", CurrentAvailableBlocksDefSelection.GetSubTypeID() }
            });

            MessageBox.Show($"Replaced {transactionLog.GetLogEntryCount()} parts, please review swapped parts...");

            this._TransactionLogWindow = new TransactionWindow2024(ref transactionLog, $"Partswap Operation: Swap {CurrentShipDefSelection.GetSubTypeID()} for {CurrentAvailableBlocksDefSelection.GetSubTypeID()}");
            this._TransactionLogWindow.Show();

            // Add all the entries from the log to our master log
            this._TransactionsLogRef.Merge(transactionLog);

            this._PartswapperRef.GetBlueprintDefinition().SaveFile();
            this._PartswapperRef.DeleteSBC5File();
            this._PartswapperRef.GetGridRendererRef().RedrawSkia();
        }

        public HashSet<BlueprintSBC_CubeBlock> LoadCurrentShipPartsCategories(BlueprintSBC_CubeGrid cubeGrid)
        {
            return cubeGrid.GetUniqueBlocks();
        }
    }
}
