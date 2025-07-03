using Microsoft.Windows.Themes;
using PartSwapperXMLSE;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using static PartSwapperXMLSE.WCAmmoDefinition.ejectionDef;

namespace PartSwapperGUI.PartSwapper2024
{
    /// <summary>
    /// Interaction logic for GridStats.xaml
    /// </summary>
    public partial class GridStats : Window
    {
        private PartSwapper2024 _PartswapperRef;
        private BlueprintSBC_CubeGrid _Cubegrid;

        private Dictionary<string, List<CubeBlockDefinitionSBC_CubeBlockDefinition>> _VanillaBlocksDict;
        private Dictionary<string, List<CubeBlockDefinitionSBC_CubeBlockDefinition>> _ModBlocksDict;

        private GridStatsDataEntry _GridStatsDE;

        private List<CubeBlockDataEntry> _CubeBlockDataEntries = new List<CubeBlockDataEntry>();

        private Dictionary<BlueprintSBC_CubeBlock, float> _GridBlockCounterDict;

        private TabControl _UITabControl = new TabControl();

        private TabItem _BlocksToComponentsTabItem;
        private TabItem _GridStatsTabItem;

        public GridStats(PartSwapper2024 partswapperRef, BlueprintSBC_CubeGrid cubegrid)
        {
            InitializeComponent();

            this._PartswapperRef = partswapperRef;
            this._Cubegrid = cubegrid;

            this._VanillaBlocksDict = partswapperRef.GetVanillaDataFolder().GetCubeBlocksDefDict();
            this._ModBlocksDict = partswapperRef.GetWorkshopModFolder().GetCubeBlocksDefDict();

            this._GridBlockCounterDict = this._Cubegrid.GetBlockCounterDict();

            this._CubeBlockDataEntries = this.PopulateCubeBlockDataEntriesList(this._GridBlockCounterDict);

            this.PopulateTabItems();

            this.Content = this._UITabControl;

            this.Title = $"GridStats for Grid {_Cubegrid.GetDisplayName()}";

        }

        public DataGrid GenerateCubeBlockDEDataGrid(ref List<CubeBlockDataEntry> cubeBlockDataEntries)
        {
            DataGrid CubeBlockDataGrid = new DataGrid();

            DataGridTemplateColumn ComboboxColumn = this.FindResource("ComponentsDataGridTemplateColumn") as DataGridTemplateColumn;

            CubeBlockDataGrid.AutoGenerateColumns = false;

            CubeBlockDataGrid.Columns.Add(ComboboxColumn);

            CubeBlockDataGrid.ItemsSource = cubeBlockDataEntries;

            return CubeBlockDataGrid;
        }

        public void PopulateTabItems()
        {
            this._UITabControl.Items.Clear();
            this._BlocksToComponentsTabItem = GenerateCubeblockDefSettingsTabItem(this._CubeBlockDataEntries);
            this._GridStatsTabItem = GenerateGridStatsTabItem();
            this._UITabControl.Items.Add(_BlocksToComponentsTabItem);
            this._UITabControl.Items.Add(_GridStatsTabItem);
        }

        private TabItem GenerateCubeblockDefSettingsTabItem(List<CubeBlockDataEntry> cubeblockDataEntries)
        {
            TabItem CubeblockDefSettingsTabItem = new TabItem();
            ScrollViewer CubeblockDefSettingsScrollViewer = new ScrollViewer();

            CubeblockDefSettingsTabItem.Header = "CubeBlock Definition Settings";
            CubeblockDefSettingsTabItem.Content = CubeblockDefSettingsScrollViewer;
            CubeblockDefSettingsScrollViewer.Content = this.GenerateCubeBlockEntryTreeView(cubeblockDataEntries);

            return CubeblockDefSettingsTabItem;
        }

        private TabItem GenerateGridStatsTabItem()
        {
            this._GridStatsDE = new GridStatsDataEntry(this._CubeBlockDataEntries);

            TabItem GridStatsTabItem = new TabItem();
            ScrollViewer GridStatsScrollViewer = new ScrollViewer();
            GridStatsScrollViewer.Content = this._GridStatsDE.GetGridStatsTreeView();
            GridStatsTabItem.Header = "Grid Stats";
            GridStatsTabItem.Content = GridStatsScrollViewer;

            return GridStatsTabItem;
        }

        public TreeView GenerateCubeBlockEntryTreeView(List<CubeBlockDataEntry> cubeblockDataEntries)
        {
            TreeView CubeBlockDETreeView = new TreeView();

            // Populate _CubeblockDefSettingsTreeView
            foreach (CubeBlockDataEntry cubeblockEntry in cubeblockDataEntries)
            {
                CubeBlockDETreeView.Items.Add(cubeblockEntry.CubeBlockDefinitionTreeViewItem);
            }

            return CubeBlockDETreeView;
        }

        public List<CubeBlockDataEntry> PopulateCubeBlockDataEntriesList(Dictionary<BlueprintSBC_CubeBlock, float> gridBlocksCounterDict)
        {
            List<CubeBlockDataEntry> CubeBlockDataEntryListResult = new List<CubeBlockDataEntry>();

            foreach (KeyValuePair<BlueprintSBC_CubeBlock, float> gridblockDictKVP in gridBlocksCounterDict)
            {
                CubeBlockDataEntry newEntry = new CubeBlockDataEntry(gridblockDictKVP.Key, gridblockDictKVP.Value, ref this._PartswapperRef);
                newEntry.AlternateDefinitions.SelectionChanged += AlternateDefinitions_SelectionChanged;
                CubeBlockDataEntryListResult.Add(newEntry);
            }
            return CubeBlockDataEntryListResult;
        }

        private void AlternateDefinitions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this._GridStatsDE.UpdateCubeBlockDataEntries(this._CubeBlockDataEntries);
            this._GridStatsTabItem.Content = this._GridStatsDE.GetGridStatsTreeView();
            this.InvalidateVisual();
        }

        public class ComponentDataEntryNameEqualityComparer : IEqualityComparer<ComponentDataEntry>
        {

            public ComponentDataEntryNameEqualityComparer()
            {

            }

            bool IEqualityComparer<ComponentDataEntry>.Equals(ComponentDataEntry? x, ComponentDataEntry? y)
            {
                if (x == null && y == null)
                {
                    return true;
                }

                if (x == null && y != null ||
                    x != null && y == null)
                {
                    return false;
                }

                if (x.BPComponentDefinition.GetSubtype().Equals(y.BPComponentDefinition.GetSubtype()))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            int IEqualityComparer<ComponentDataEntry>.GetHashCode(ComponentDataEntry obj)
            {
                return obj.BPComponentDefinition.ToString().GetHashCode();
            }
        }

        public class GridStatsDataEntry
        {
            public float GridPCU { get => _CalculatedPCU; }

            private float _CalculatedPCU = 0;

            private TreeView _GridStatsTreeView = new TreeView();

            private DataGrid _GridStatsDataGrid = new DataGrid();

            private List<CubeBlockDataEntry> _CubeBlockDataEntries;

            private List<TreeViewItem> BlockStatTVIList = new List<TreeViewItem>();

            private TreeViewItem _PCUStatsTVI = new TreeViewItem();
            private TreeViewItem _GridStatsDataGridTVI = new TreeViewItem();

            private Dictionary<string,float> _ComponentSubtypeToCountDict = new Dictionary<string, float>();

            public GridStatsDataEntry(List<CubeBlockDataEntry> cubeBlockDataEntries)
            {
                this._CubeBlockDataEntries = cubeBlockDataEntries;

                this._CalculatedPCU = CalculatePCU(ref this._CubeBlockDataEntries);

                this._PCUStatsTVI = this.GeneratePCUStats(ref this._CubeBlockDataEntries, this._CalculatedPCU);

                this._GridStatsDataGrid = this.GenerateGridStatsDataGrid();
                this._GridStatsDataGridTVI = this.GenerateStatsDataGridTVI(ref this._GridStatsDataGrid);

                this._ComponentSubtypeToCountDict = this.GenerateComponentCountStatsDict(ref this._CubeBlockDataEntries);

                this._GridStatsTreeView = GenerateGridStatsTreeView(ref this._CubeBlockDataEntries,this._CalculatedPCU);

                this._GridStatsTreeView.Items.Add(this._PCUStatsTVI);
                this._GridStatsTreeView.Items.Add(this._GridStatsDataGridTVI);
            }

            private TreeViewItem GenerateStatsDataGridTVI(ref DataGrid gridStatsDataGrid)
            {
                TreeViewItem gridstatsDGTVI = new TreeViewItem();
                gridstatsDGTVI.Header = "GridStats DataGrid";
                gridstatsDGTVI.Items.Add(gridStatsDataGrid);

                return gridstatsDGTVI;
            }

            public float CalculatePCU(ref List<CubeBlockDataEntry> cubeblockDEList)
            {
                // Calculate PCU
                float _CalculatedPCU = 0;

                foreach (CubeBlockDataEntry cubeblockDE in cubeblockDEList)
                {
                    _CalculatedPCU += cubeblockDE.TotalPCU;
                }

                return _CalculatedPCU;
            }

            public void UpdateCubeBlockDataEntries(List<CubeBlockDataEntry> cubeBlockDataEntries)
            {
                this._CubeBlockDataEntries = cubeBlockDataEntries;

                this._CalculatedPCU = CalculatePCU(ref this._CubeBlockDataEntries);

                this._PCUStatsTVI = this.GeneratePCUStats(ref this._CubeBlockDataEntries, this._CalculatedPCU);

                this._GridStatsDataGrid = this.GenerateGridStatsDataGrid();
                this._GridStatsDataGridTVI = this.GenerateStatsDataGridTVI(ref this._GridStatsDataGrid);

                this._ComponentSubtypeToCountDict = this.GenerateComponentCountStatsDict(ref this._CubeBlockDataEntries);

                this._GridStatsTreeView = GenerateGridStatsTreeView(ref this._CubeBlockDataEntries, this._CalculatedPCU);

                this._GridStatsTreeView.Items.Add(this._PCUStatsTVI);
                this._GridStatsTreeView.Items.Add(this._GridStatsDataGridTVI);
            }


            private Dictionary<string, float> GenerateComponentCountStatsDict(ref List<CubeBlockDataEntry> cubeBlockDataEntries)
            {
                Dictionary<string, float> ComponentSubtypeToCountDict = new Dictionary<string, float>();

                // For every cubeblock...
                foreach (CubeBlockDataEntry cubeblockDE in cubeBlockDataEntries)
                {
                    // For every component...
                    foreach (ComponentDataEntry componentDE in cubeblockDE.GetComponentDataEntryList())
                    {
                        // Side-effect: Add subtype-to-count data to dict
                        if (ComponentSubtypeToCountDict.ContainsKey(componentDE.CurrentDefinition.SubtypeID))
                        {
                            ComponentSubtypeToCountDict[componentDE.CurrentDefinition.SubtypeID] += componentDE.ComponentCount * cubeblockDE.BlockCount;
                        }
                        else
                        {
                            ComponentSubtypeToCountDict[componentDE.CurrentDefinition.SubtypeID] = componentDE.ComponentCount * cubeblockDE.BlockCount;
                        }
                    }
                }

                return ComponentSubtypeToCountDict;
            }

            private TreeViewItem GeneratePCUStats(ref List<CubeBlockDataEntry> cubeBlockDataEntries,float TotalPCU)
            {
                TreeViewItem PCUStats = new TreeViewItem();
                PCUStats.Header = "PCU Statistics";

                TreeViewItem CubeBlockStats = new TreeViewItem();
                TreeViewItem ComponentStats = new TreeViewItem();
                float MassPerBlock = 0;
                float MassTotalBlocks = 0;

                // For every cubeblock...
                foreach (CubeBlockDataEntry cubeblockDE in cubeBlockDataEntries)
                {
                    // Reset WeightPerBlock
                    MassPerBlock = 0;

                    // Init a new CubeBlockStats
                    CubeBlockStats = new TreeViewItem();
                    CubeBlockStats.Header = $"CubeBlock: {cubeblockDE}, PCU Total: {cubeblockDE.TotalPCU},PCU Total Percentage:{100 * (cubeblockDE.TotalPCU / TotalPCU)}";

                    // Add initial data
                    CubeBlockStats.Items.Add($"CubeBlock SubtypeName:{cubeblockDE.SubtypeName}");
                    CubeBlockStats.Items.Add($"CubeBlock BlockCount:{cubeblockDE.BlockCount}");
                    CubeBlockStats.Items.Add($"CubeBlock PCU:{cubeblockDE.PCU}");
                    CubeBlockStats.Items.Add($"CubeBlock TotalPCU:{cubeblockDE.TotalPCU}");
                    CubeBlockStats.Items.Add($"DefinitionSource:{cubeblockDE.DefinitionSource}");
                    CubeBlockStats.Items.Add($"Percentage of total PCU count:{100 * (cubeblockDE.PCU / TotalPCU)}%");

                    // For every component...
                    foreach (ComponentDataEntry componentDE in cubeblockDE.GetComponentDataEntryList())
                    {
                        ComponentStats = new TreeViewItem();
                        ComponentStats.Header = $"Component: {componentDE}";

                        ComponentStats.Items.Add($"SubtypeID:");
                        ComponentStats.Items.Add(componentDE.CurrentDefinition.SubtypeID);
                        ComponentStats.Items.Add($"DisplayName:");
                        ComponentStats.Items.Add(componentDE.CurrentDefinition.DisplayName);
                        ComponentStats.Items.Add($"Mass:");
                        ComponentStats.Items.Add(componentDE.CurrentDefinition.Mass);
                        ComponentStats.Items.Add($"Count:");
                        ComponentStats.Items.Add(componentDE.ComponentCount);
                        // Add to mass sum
                        MassPerBlock += componentDE.CurrentDefinition.Mass;

                        ComponentStats.Margin = new Thickness(10);

                        CubeBlockStats.Items.Add(ComponentStats);
                    }

                    CubeBlockStats.Items.Add($"Weight per-{cubeblockDE.SubtypeName}:");
                    CubeBlockStats.Items.Add(MassPerBlock);

                    CubeBlockStats.Items.Add($"Weight of all {cubeblockDE.SubtypeName}:");
                    CubeBlockStats.Items.Add(MassPerBlock * cubeblockDE.BlockCount);

                    // Add to weight total sum
                    MassTotalBlocks += MassPerBlock;

                    CubeBlockStats.Header += $", Mass Per Block: {MassPerBlock}";

                    PCUStats.Items.Add(CubeBlockStats);
                }

                return PCUStats;
            }

            private TreeViewItem GeneratePartCountTreeView()
            {
                TreeViewItem PartCountTreeView = new TreeViewItem();
                PartCountTreeView.Header = "Part Counter";

                foreach (string componentSubtypeID in this._ComponentSubtypeToCountDict.Keys)
                {
                    PartCountTreeView.Items.Add($"Total number of {componentSubtypeID}: {this._ComponentSubtypeToCountDict[componentSubtypeID]}");
                }

                return PartCountTreeView;
            }

            private TreeView GenerateGridStatsTreeView(ref List<CubeBlockDataEntry> cubeBlockDataEntries, float GridPCU)
            {
                float gridWeight = 0;

                TreeView GridStatsTreeView = new TreeView();

                TreeViewItem CubeBlocksSummaryTreeViewItem = new TreeViewItem();
                TreeViewItem BlockStatTreeViewItem = new TreeViewItem(); ;
                TreeViewItem PartCounterTreeViewItem = this.GeneratePartCountTreeView();

                CubeBlocksSummaryTreeViewItem.Header = "Grid Statistics";

                GridStatsTreeView.Items.Add($"Grid PCU: {GridPCU}");

                foreach (CubeBlockDataEntry cubeblockDE in cubeBlockDataEntries)
                {
                    BlockStatTreeViewItem = new TreeViewItem();
                    BlockStatTreeViewItem.Header = cubeblockDE;
                    BlockStatTreeViewItem.IsExpanded = false;
                    
                    BlockStatTreeViewItem.Items.Add($"Block SubtypeName: {cubeblockDE.SubtypeName}");
                    BlockStatTreeViewItem.Items.Add(cubeblockDE.SubtypeName);

                    BlockStatTreeViewItem.Items.Add($"PCU per-{cubeblockDE.SubtypeName}: {cubeblockDE.PCU}");
                    BlockStatTreeViewItem.Items.Add(cubeblockDE.PCU);

                    BlockStatTreeViewItem.Items.Add($"Total PCU from all {cubeblockDE.SubtypeName}:");
                    BlockStatTreeViewItem.Items.Add(cubeblockDE.TotalPCU);

                    BlockStatTVIList.Add(BlockStatTreeViewItem);

                    foreach(ComponentDataEntry componentDE in cubeblockDE.GetComponentDataEntryList())
                    {
                        gridWeight += componentDE.CurrentDefinition.Mass * cubeblockDE.BlockCount;
                    }
                }

                GridStatsTreeView.Items.Add($"Grid Weight: {gridWeight}");

                // Side-effect: Setting BlockStatTVIList to ItemSource of returned TreeViewItem
                CubeBlocksSummaryTreeViewItem.ItemsSource = BlockStatTVIList;

                GridStatsTreeView.Items.Add(PartCounterTreeViewItem);
                GridStatsTreeView.Items.Add(CubeBlocksSummaryTreeViewItem);

                return GridStatsTreeView;
            }

            private DataGrid GenerateGridStatsDataGrid()
            {
                DataGrid GridStatsDataGrid = new DataGrid();

                #region DataGridTemplating
                GridStatsDataGrid.AutoGenerateColumns = true;
                /*
                DataGridComboBoxColumn AlternateDefinitionsColumn = new DataGridComboBoxColumn();
                Binding AlternateDefinitionsBinding = new Binding("AlternateDefinitions.Items");
                AlternateDefinitionsBinding.Mode = BindingMode.OneWay;

                AlternateDefinitionsColumn.Header = "Alternate Definitions";
                AlternateDefinitionsColumn.SelectedItemBinding = AlternateDefinitionsBinding;
                this._GridStatsDataGrid.Columns.Add(AlternateDefinitionsColumn);
                */
                #endregion

                GridStatsDataGrid.ItemsSource = this._CubeBlockDataEntries;

                return GridStatsDataGrid;
            }

            // TODO: WIP - rewrite to combine PopulateCurrentGridComponentCounterDict and PopulateGridStatsTreeView...maybe?
            private void Load(List<CubeBlockDataEntry> cubeBlockDataEntries)
            {
                TreeViewItem CubeBlockTreeViewItemIter;
                TreeViewItem ComponentTreeViewItemIter;

                this._CalculatedPCU = 0;

                foreach (CubeBlockDataEntry cubeblockDE in cubeBlockDataEntries)
                {
                    CubeBlockTreeViewItemIter = new TreeViewItem();
                    CubeBlockTreeViewItemIter.Header = cubeblockDE.SubtypeName;
                    CubeBlockTreeViewItemIter.Items.Add(cubeblockDE);
                    CubeBlockTreeViewItemIter.Items.Add(cubeblockDE.AlternateDefinitions);

                    // Add this block category's PCU to the total
                    _CalculatedPCU += cubeblockDE.TotalPCU;

                    foreach (ComponentDataEntry componentDE in cubeblockDE.GetComponentDataEntryList())
                    {
                        ComponentTreeViewItemIter = new TreeViewItem();
                        ComponentTreeViewItemIter.Header = componentDE.BPComponentDefinition.Subtype;
                        ComponentTreeViewItemIter.Items.Add(componentDE);
                        ComponentTreeViewItemIter.Items.Add(componentDE.AlternateDefinitions);
                    }
                }
            }

            public TreeView GetGridStatsTreeView()
            {
                return this._GridStatsTreeView;
            }
        }

        public class ComponentDataEntry : INotifyPropertyChanged
        {
            // Public properties, used for rendering
            public BluePrintSBC_Component BPComponentDefinition { get => this._BPComponent; }
            public ComponentsSBC_ComponentDefinition CurrentDefinition { get => _ComponentDefinition; }
            public HashSet<ComponentsSBC_ComponentDefinition> AlternateDefinitions { get => _PossibleDefinitions; }
            public float ComponentCount { get => this._ComponentCount; }

            public ComboBox AlternateDefinitionSelection { get => _AlternateDefinitionSelect; }

            private BluePrintSBC_Component _BPComponent;
            private ComponentsSBC_ComponentDefinition _ComponentDefinition;

            private Dictionary<DefinitionSource, List<ComponentsSBC_ComponentDefinition>> _AvailableComponentDefinitionsRef;
            private HashSet<ComponentsSBC_ComponentDefinition> _PossibleDefinitions = new HashSet<ComponentsSBC_ComponentDefinition>();
            private float _ComponentCount;

            private ComboBox _AlternateDefinitionSelect = new ComboBox();

            private Guid _guid = new Guid();

            public ComponentDataEntry(float componentCount, BluePrintSBC_Component component, Dictionary<DefinitionSource, List<ComponentsSBC_ComponentDefinition>> componentDefinitionOptions)
            {
                this._AvailableComponentDefinitionsRef = componentDefinitionOptions;
                this._ComponentCount = componentCount;
                this._BPComponent = component;
                PopulateComponentDefinitions(ref componentDefinitionOptions);

            }

            #region PropertyChangedEventHandler
            public event PropertyChangedEventHandler PropertyChanged;

            // Create the OnPropertyChanged method to raise the event
            // The calling member's name will be used as the parameter.
            protected void OnPropertyChanged([CallerMemberName] string name = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            }

            #endregion

            // Populates possible component definitions and selects one to make 'current'
            public void PopulateComponentDefinitions(ref Dictionary<DefinitionSource, List<ComponentsSBC_ComponentDefinition>> componentDefinitionOptions)
            {
                //Populate the possible definitions HashSet and Set the current ComponentDefinition to the first definition we find (alternatively: left null)
                if (componentDefinitionOptions != null)
                {
                    this._PossibleDefinitions.Clear();

                    if (componentDefinitionOptions.Count > 0)
                    {
                        if (componentDefinitionOptions[DefinitionSource.Mod] != null && componentDefinitionOptions[DefinitionSource.Mod].Count > 0)
                        {
                            foreach (ComponentsSBC_ComponentDefinition Component in componentDefinitionOptions[DefinitionSource.Mod])
                            {
                                if (Component.SubtypeID.Equals(this._BPComponent.Subtype))
                                {
                                    this._PossibleDefinitions.Add(Component);
                                }
                            }

                            this._ComponentDefinition = componentDefinitionOptions[DefinitionSource.Mod].First();
                        }

                        if (componentDefinitionOptions[DefinitionSource.Vanilla] != null && componentDefinitionOptions[DefinitionSource.Vanilla].Count > 0)
                        {
                            foreach (ComponentsSBC_ComponentDefinition Component in componentDefinitionOptions[DefinitionSource.Vanilla])
                            {
                                this._PossibleDefinitions.Add(Component);
                            }

                            this._ComponentDefinition = componentDefinitionOptions[DefinitionSource.Vanilla].First();
                        }

                        this._AlternateDefinitionSelect.ItemsSource = this._PossibleDefinitions;
                    }
                }
            }

            public override int GetHashCode()
            {
                return this.ToString().GetHashCode();
            }

            public override bool Equals(object? obj)
            {
                if (obj == null) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != GetType()) return false;

                if (obj is ComponentDataEntry)
                {
                    ComponentDataEntry compared = obj as ComponentDataEntry;

                    if (compared._guid.Equals(this._guid)) { return true; }
                    else
                    {
                        return false;
                    }
                }

                return base.Equals(obj);
            }

            public override string ToString()
            {
                return this.BPComponentDefinition.ToString() + "_" + (this.CurrentDefinition == null ? "CURRDEFNULL" : this.CurrentDefinition.ToString());
            }
        }

        public class CubeBlockDataEntry : INotifyPropertyChanged
        {
            #region UI-Presented Vars
            public string SubtypeName { get => this._sbcCubeblockDef.SubtypeName; }
            public string DefinitionSource { get => this._CubeblockDef.GetDefinitionSourceString(); }

            public float BlockCount { get => this._BlockCount; }
            public int PCU { get => this._PCU; }
            public int TotalPCU { get => this._PCU * (int)this._BlockCount; }

            public TreeViewItem CubeBlockDefinitionTreeViewItem { get => this._CubeblockSettingsTreeViewItem; }
            public ComboBox AlternateDefinitions { get => this._AlternateBlockComboBox; }
            public DataGrid ComponentsGrid { get => this._ComponentsDatagrid; }

            #endregion

            //PS Ref
            private PartSwapper2024 _PartswapperRef;

            //Cubeblock vars
            private BlueprintSBC_CubeBlock _sbcCubeblockDef;
            private CubeBlockDefinitionSBC_CubeBlockDefinition _CubeblockDef;

            private Dictionary<DefinitionSource, List<CubeBlockDefinitionSBC_CubeBlockDefinition>> _CubeBlockSearchResults;
            private Dictionary<DefinitionSource, List<ComponentsSBC_ComponentDefinition>> _ComponentSearchResults;

            // UI Vars
            private TreeViewItem _CubeblockSettingsTreeViewItem;

            private ComboBox _AlternateBlockComboBox = new ComboBox();

            private DataGrid _ComponentsDatagrid = new DataGrid();

            //Component vars
            private int _PCU = -1;

            private float _BlockCount;

            private List<ComponentDataEntry> _ComponentUIEntriesList = new List<ComponentDataEntry>();

            public CubeBlockDataEntry(BlueprintSBC_CubeBlock sbcCubeblock, float blockCount, ref PartSwapper2024 partswapperRef)
            {
                this._PartswapperRef = partswapperRef;

                this._sbcCubeblockDef = sbcCubeblock;
                this._BlockCount = blockCount;

                this._AlternateBlockComboBox = this.GenerateAlternateDefinitionsCombobox(ref this._PartswapperRef);
                this._CubeblockDef = this.SelectFirstCubeDef(this._AlternateBlockComboBox, sbcCubeblock);
                this._PCU = this.GeneratePCU();
                this._ComponentUIEntriesList = this.GenerateComponentsDEList(ref this._PartswapperRef, this._CubeblockDef);
                this._ComponentsDatagrid.ItemsSource = this._ComponentUIEntriesList;
                this._CubeblockSettingsTreeViewItem = this.GenerateTreeViewItem();
            }

            #region PropertyChangedEventHandler
            public event PropertyChangedEventHandler PropertyChanged;

            // Create the OnPropertyChanged method to raise the event
            // The calling member's name will be used as the parameter.
            protected void OnPropertyChanged([CallerMemberName] string name = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            }

            #endregion

            public int GeneratePCU()
            {

                if (this._CubeblockDef != null)
                {
                    return this._CubeblockDef.GetPCU();
                }
                else
                {
                    return -1;
                }
            }


            private void AddTreeViewItemsEntries(ref TreeViewItem CubeblockSettingTreeviewItem)
            {
                CubeblockSettingTreeviewItem.Items.Clear();

                CubeblockSettingTreeviewItem.Items.Add("Total PCU:");
                CubeblockSettingTreeviewItem.Items.Add(this.TotalPCU);
                CubeblockSettingTreeviewItem.Items.Add("Block PCU:");
                CubeblockSettingTreeviewItem.Items.Add(this.PCU);
                CubeblockSettingTreeviewItem.Items.Add($"DisplayName:");
                CubeblockSettingTreeviewItem.Items.Add(this._CubeblockDef.DisplayName);
                CubeblockSettingTreeviewItem.Items.Add($"SubtypeID:");
                CubeblockSettingTreeviewItem.Items.Add(this._CubeblockDef.SubtypeID);
                CubeblockSettingTreeviewItem.Items.Add($"Components:");
                CubeblockSettingTreeviewItem.Items.Add(this._ComponentsDatagrid);
                CubeblockSettingTreeviewItem.Items.Add($"Definition:");
                CubeblockSettingTreeviewItem.Items.Add(this._CubeblockDef);
                CubeblockSettingTreeviewItem.Items.Add($"Alternate Definitions:");
                CubeblockSettingTreeviewItem.Items.Add(this.AlternateDefinitions);
            }

            public TreeViewItem GenerateTreeViewItem()
            {
                TreeViewItem newTreeViewItem = new TreeViewItem();

                // Have to update the AlternateBlockList first
                //this._AlternateBlockComboBox = this.GenerateAlternateDefinitionsCombobox(ref this._PartswapperRef); <-- or do we?

                this._ComponentsDatagrid = this.GenerateComponentsDatagrid(ref this._PartswapperRef);

                newTreeViewItem.IsExpanded = false;
                newTreeViewItem.Header = $"CubeBlockSource: {this._CubeblockDef.GetDefinitionSourceString()}, SubTypeID:{this._CubeblockDef.GetSubTypeID()}";
                this.AddTreeViewItemsEntries(ref newTreeViewItem);
                newTreeViewItem.BorderThickness = new Thickness(0, 0, 0, 5);
                newTreeViewItem.BorderBrush = Brushes.OrangeRed;

                return newTreeViewItem;
            }

            public DataGrid GenerateComponentsDatagrid(ref PartSwapper2024 psRef)
            {
                DataGrid ComponentsDatagrid = new DataGrid();
                List<ComponentDataEntry> ComponentUIEntriesList = new List<ComponentDataEntry>();
                Dictionary<DefinitionSource, List<ComponentsSBC_ComponentDefinition>> SearchResults;

                foreach (BluePrintSBC_Component component in this._CubeblockDef.GetComponents())
                {
                    SearchResults = psRef.SearchComponentsDefinitions(component);
                    ComponentUIEntriesList.Add(new ComponentDataEntry(component.GetCount(), component, this._ComponentSearchResults));
                }

                ComponentsDatagrid.ItemsSource = ComponentUIEntriesList;

                return ComponentsDatagrid;
            }

            public override string ToString()
            {
                return this._CubeblockDef.GetSubTypeID();
            }

            private void _AlternateBlockComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
            {
                if (e.AddedItems.Count > 0)
                {
                    //this._CubeblockDef = (e.AddedItems[0] as ComboBoxItem).Content as CubeBlockDefinitionSBC_CubeBlockDefinition;
                    //this._ComponentsDatagrid.ItemsSource = GenerateComponentsDatagrid(ref this._PartswapperRef).Items; // <-- This works, but I want something more elegant.
                    //this._ComponentsDatagrid = GenerateComponentsDatagrid(ref this._PartswapperRef);

                    //this._AlternateBlockComboBox = this.GenerateAlternateDefinitionsCombobox(ref this._PartswapperRef);
                    this._CubeblockDef = (e.AddedItems[0] as ComboBoxItem).Content as CubeBlockDefinitionSBC_CubeBlockDefinition;
                    this._PCU = this.GeneratePCU();
                    this._ComponentUIEntriesList = this.GenerateComponentsDEList(ref this._PartswapperRef, this._CubeblockDef);
                    this._ComponentsDatagrid.ItemsSource = this._ComponentUIEntriesList;

                    this.AddTreeViewItemsEntries(ref this._CubeblockSettingsTreeViewItem);

                    this.OnPropertyChanged("PCU");
                    this.OnPropertyChanged("ComponentsGrid");
                    this.OnPropertyChanged("CubeBlockDefinitionTreeViewItem");

                    this._ComponentsDatagrid.InvalidateVisual();
                    this._CubeblockSettingsTreeViewItem.InvalidateVisual();
                    this.CubeBlockDefinitionTreeViewItem.InvalidateVisual();
                }

            }


            public List<ComponentDataEntry> GenerateComponentsDEList(ref PartSwapper2024 ps2024Ref, CubeBlockDefinitionSBC_CubeBlockDefinition cubeblockDef)
            {
                List<ComponentDataEntry> ComponentDEList = new List<ComponentDataEntry>();
                Dictionary<DefinitionSource, List<ComponentsSBC_ComponentDefinition>> SearchResults;

                foreach (BluePrintSBC_Component component in cubeblockDef.GetComponents())
                {
                    SearchResults = ps2024Ref.SearchComponentsDefinitions(component);
                    ComponentDEList.Add(new ComponentDataEntry(component.GetCount(), component, SearchResults));
                }

                return ComponentDEList;
            }

            public ComboBox GenerateAlternateDefinitionsCombobox(ref PartSwapper2024 partswapperRef)
            {
                ComboBox AlternateBlockComboBoxResult = new ComboBox();
                Dictionary<DefinitionSource, List<CubeBlockDefinitionSBC_CubeBlockDefinition>> SearchResults;

                SearchResults = partswapperRef.SearchCubeBlockDefinitions(this.SubtypeName);

                if (SearchResults.Count == 2)
                {
                    ComboBoxItem ComboBoxItemIterator;

                    if (SearchResults[PartSwapperXMLSE.DefinitionSource.Vanilla].Count > 0)
                    {
                        foreach (CubeBlockDefinitionSBC_CubeBlockDefinition cubeBlockDef in SearchResults.ElementAt(0).Value)
                        {
                            ComboBoxItemIterator = new ComboBoxItem();
                            ComboBoxItemIterator.Content = cubeBlockDef;

                            AlternateBlockComboBoxResult.Items.Add(ComboBoxItemIterator);
                        }
                    }

                    if (SearchResults[PartSwapperXMLSE.DefinitionSource.Mod].Count > 0)
                    {
                        foreach (CubeBlockDefinitionSBC_CubeBlockDefinition cubeBlockDef in SearchResults.ElementAt(1).Value)
                        {
                            ComboBoxItemIterator = new ComboBoxItem();
                            ComboBoxItemIterator.Content = cubeBlockDef;

                            AlternateBlockComboBoxResult.Items.Add(ComboBoxItemIterator);
                        }
                    }

                    if (AlternateBlockComboBoxResult.Items.Count > 0)
                    {
                        AlternateBlockComboBoxResult.SelectedIndex = 0;
                        AlternateBlockComboBoxResult.SelectionChanged += _AlternateBlockComboBox_SelectionChanged;
                    }

                }

                return AlternateBlockComboBoxResult;
            }

            public CubeBlockDefinitionSBC_CubeBlockDefinition? SelectFirstCubeDef(ComboBox AlternateDefinitionsComboBox, BlueprintSBC_CubeBlock SBCCubeblockDef)
            {
                CubeBlockDefinitionSBC_CubeBlockDefinition selectedCubeDef;

                if (AlternateDefinitionsComboBox != null && AlternateDefinitionsComboBox.Items.Count > 0)
                {

                    ComboBoxItem FirstComboBoxItem = (this._AlternateBlockComboBox.Items[this._AlternateBlockComboBox.SelectedIndex] as ComboBoxItem);

                    selectedCubeDef = (FirstComboBoxItem.Content as CubeBlockDefinitionSBC_CubeBlockDefinition);

                }
                else
                {
                    // Use the default constructor
                    selectedCubeDef = new CubeBlockDefinitionSBC_CubeBlockDefinition(SBCCubeblockDef);
                }

                return selectedCubeDef;
            }

            public List<ComponentDataEntry> GetComponentDataEntryList()
            {
                return this._ComponentUIEntriesList;
            }
        }
    }
}
