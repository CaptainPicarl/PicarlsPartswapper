using PartSwapperXMLSE;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
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
using System.Xml.Linq;

namespace PartSwapperGUI.PartSwapper2024
{
    /// <summary>
    /// Interaction logic for CubeblockDefinitionViewer.xaml
    /// </summary>
    public partial class CubeblockDefinitionViewer : Window
    {
        private PartSwapper2024 PartSwapper2024Ref;

        Dictionary<DefinitionSource, List<CubeBlockDefinitionSBC_CubeBlockDefinition>> DefinitionSearchResults;

        #region Source Block Variables
        CubeBlockDefinitionSBC_CubeBlockDefinition CurrentCubeBlockDefinition;
        BlueprintSBC_CubeBlock CurrentGridSBCCubeblockDefinition;

        XElement CubeBlockDefinitionXElement;

        string CurrBlckDefinitionSource;

        string CurrBlckTypeId;
        string CurrBlckSubtypeID;
        string CurrBlckDisplayName;
        string CurrBlckIcon;
        string CurrBlckDescription;

        CubeSize CurrBlckCubeSize;

        bool CurrBlckGUIVisible;
        bool CurrBlckPlaceDecals;
        string CurrBlckBlockTopology;
        Vector3 CurrBlckSize;
        Vector3 CurrBlckModelOffset;
        string CurrBlckModel;
        CubeBlocksDefinitionSBC_CubeDefinition? CurrBlckCubeDefinition;
        bool CurrBlckUseModelIntersection;

        List<BluePrintSBC_Component> CurrBlckComponents = new List<BluePrintSBC_Component>();
        CubeBlockDefinitionSBC_CriticalComponent? CurrBlckCriticalComponent;
        List<CubeBlocksDefinitionSBC_MountPoint> CurrBlckMountPoints = new List<CubeBlocksDefinitionSBC_MountPoint>();
        List<CubeBlocksDefinitionSBC_BuildProgressModel> CurrBlckBuildProgressModels = new List<CubeBlocksDefinitionSBC_BuildProgressModel>();
        List<CubeBlocksDefinitionSBC_Bone> CurrBlckSkeleton = new List<CubeBlocksDefinitionSBC_Bone>();
        BlueprintSBC_CubeBlock? CurrBlckBlockPairName;

        #endregion

        #region UI Variables

        // Common variables between the Grid Blueprint SBC Definition, and the Cubeblocks Definitions
        string UIBlckSubtypeIDString = "";

        // Variables unique to the Grid Blueprint SBC Definition of a cubeblock
        // None yet

        // Variables unique to the Cubeblocks Definition of a cubeblock
        string UIBlckDefinitionSourceString = "";

        string UIBlckTypeIdString = "";
        string UIBlckDisplayNameString = "";
        string UIBlckIconString = "";
        string UIBlckDescriptionString = "";

        string UIBlckCubeSizeString = "";

        string UIBlckGUIVisibleString = "";
        string UIBlckPlaceDecalsString = "";
        string UIBlckBlockTopologyString = "";
        string UIBlckSizeString = "";
        string UIBlckModelOffsetString = "";
        string UIBlckModelString = "";
        string UIBlckCubeDefinitionString = "";
        string UIBlckUseModelIntersectionString = "";

        string UIBlckComponentsString = "";
        string UIBlckCriticalComponentString = "";
        string UIBlckMountPointsString = "";
        string UIBlckBuildProgressModelsString = "";
        string UIBlckSkeletonString = "";
        string UIBlckBlockPairNameString = "";

        Dictionary<string, string> CubeBlockDefLabelToValuDict;

        ScrollViewer CubeBlockDefinitionViewer = new ScrollViewer();
        ScrollViewer GridSBCBlockDefinitionViewer = new ScrollViewer();

        StackPanel CubeBlockDefinitionStackPanel = new StackPanel();
        StackPanel GridSBCBlockDefinitionStackPanel = new StackPanel();

        TabControl UITabControl = new TabControl();

        TabItem UICubeBlockDefinitionTabItem = new TabItem();
        TabItem UIGridSBCDefinitionTabItem = new TabItem();

        #endregion
        public CubeblockDefinitionViewer(ref PartSwapper2024 psInstance, CubeBlockDefinitionSBC_CubeBlockDefinition currentCubeBlockDefinition)
        {
            InitializeComponent();

            this.PartSwapper2024Ref = psInstance;

            CurrentCubeBlockDefinition = currentCubeBlockDefinition;

            CubeBlockDefinitionXElement = currentCubeBlockDefinition.GetCubeBlockDefinitionXElement();

            CurrBlckDefinitionSource = currentCubeBlockDefinition.GetDefinitionSourceString();

            CurrBlckTypeId = currentCubeBlockDefinition.GetTypeID();
            CurrBlckSubtypeID = currentCubeBlockDefinition.GetSubTypeID();
            CurrBlckDisplayName = currentCubeBlockDefinition.GetDisplayName();
            CurrBlckIcon = currentCubeBlockDefinition.GetIcon();
            CurrBlckDescription = currentCubeBlockDefinition.GetDescription();

            CurrBlckCubeSize = currentCubeBlockDefinition.GetCubeSize();

            CurrBlckGUIVisible = currentCubeBlockDefinition.GetGUIVisible();
            CurrBlckPlaceDecals = currentCubeBlockDefinition.GetPlaceDecals();
            CurrBlckBlockTopology = currentCubeBlockDefinition.GetBlockTopology();
            CurrBlckSize = currentCubeBlockDefinition.GetSize();
            CurrBlckModelOffset = currentCubeBlockDefinition.GetModelOffset();
            CurrBlckModel = currentCubeBlockDefinition.GetModel();
            CurrBlckCubeDefinition = currentCubeBlockDefinition.GetCubeDefinition();
            CurrBlckUseModelIntersection = currentCubeBlockDefinition.GetUseModelIntersection();

            CurrBlckComponents = currentCubeBlockDefinition.GetComponents().ToList();
            CurrBlckCriticalComponent = currentCubeBlockDefinition.GetCriticalComponent();
            CurrBlckMountPoints = currentCubeBlockDefinition.GetMountPoints();
            CurrBlckBuildProgressModels = currentCubeBlockDefinition.GetBuildProgressModels();
            CurrBlckSkeleton = currentCubeBlockDefinition.GetSkeleton();
            CurrBlckBlockPairName = currentCubeBlockDefinition.GetBlockPairName();

            UIBlckDefinitionSourceString = CurrBlckDefinitionSource;

            UIBlckTypeIdString = CurrBlckTypeId;
            UIBlckSubtypeIDString = CurrBlckSubtypeID;
            UIBlckDisplayNameString = CurrBlckDisplayName;
            UIBlckIconString = CurrBlckIcon;
            UIBlckDescriptionString = CurrBlckDescription;

            UIBlckCubeSizeString = CurrBlckCubeSize.ToString();

            UIBlckGUIVisibleString = CurrBlckGUIVisible.ToString();
            UIBlckPlaceDecalsString = CurrBlckPlaceDecals.ToString();
            UIBlckBlockTopologyString = CurrBlckBlockTopology.ToString();
            UIBlckSizeString = CurrBlckSize.ToString();
            UIBlckModelOffsetString = CurrBlckModelOffset.ToString();
            UIBlckModelString = CurrBlckModel.ToString();
            UIBlckCubeDefinitionString = CurrBlckCubeDefinition == null ? "" : CurrBlckCubeDefinition.ToString();
            UIBlckUseModelIntersectionString = CurrBlckUseModelIntersection.ToString();

            UIBlckComponentsString = CurrBlckComponents == null ? "" : CurrBlckComponents.ToString();
            UIBlckCriticalComponentString = CurrBlckCriticalComponent == null ? "" : CurrBlckCriticalComponent.ToString();
            UIBlckMountPointsString = CurrBlckMountPoints == null ? "" : CurrBlckMountPoints.ToString();
            UIBlckBuildProgressModelsString = CurrBlckBuildProgressModels == null ? "" : CurrBlckBuildProgressModels.ToString();
            UIBlckSkeletonString = CurrBlckSkeleton == null ? "" : CurrBlckSkeleton.ToString();
            UIBlckBlockPairNameString = CurrBlckBlockPairName == null ? "" : CurrBlckBlockPairName.ToString();

            CubeBlockDefLabelToValuDict = new Dictionary<string, string>{
            {"TypeId",UIBlckTypeIdString},
            {"SubtypeID", UIBlckSubtypeIDString},
            {"DisplayName", UIBlckDisplayNameString},
            {"Icon", UIBlckIconString},
            {"BlckDescription", UIBlckDescriptionString},

            {"BlckCubeSize", UIBlckCubeSizeString},

            {"GUIVisible", UIBlckGUIVisibleString},
            {"PlaceDecals", UIBlckPlaceDecalsString},
            {"BlockTopology", UIBlckBlockTopologyString},
            {"BlckSize", UIBlckSizeString},
            {"ModelOffset", UIBlckModelOffsetString},
            {"BlckModel", UIBlckModelString},
            {"BlckCubeDefinition", UIBlckCubeDefinitionString},
            {"UseModelIntersection", UIBlckUseModelIntersectionString},

            {"Components", UIBlckComponentsString},
            {"CriticalComponent", UIBlckCriticalComponentString},
            {"MountPoints", UIBlckMountPointsString},
            {"BuildProgressModels", UIBlckBuildProgressModelsString},
            {"Skeleton", UIBlckSkeletonString},
            {"BlockPairName", UIBlckBlockPairNameString},
            {"Definition Source",UIBlckDefinitionSourceString}
        };

            this.CubeViewerContentStackPanel.Children.Add(UITabControl);

            UITabControl.Items.Add(UIGridSBCDefinitionTabItem);
            UITabControl.Items.Add(UICubeBlockDefinitionTabItem);

            UITabControl.Width = this.Width;
            UITabControl.Height = this.Height;

            UICubeBlockDefinitionTabItem.Content = CubeBlockDefinitionViewer;
            UICubeBlockDefinitionTabItem.Header = $"Cubeblock Definition for: {UIBlckSubtypeIDString} from {UIBlckDefinitionSourceString}";

            UIGridSBCDefinitionTabItem.Content = GridSBCBlockDefinitionViewer;
            UIGridSBCDefinitionTabItem.Header = $"No Grid SBC Definition Selected!";

            CubeBlockDefinitionStackPanel.Width = UITabControl.Width;

            CubeBlockDefinitionViewer.Content = CubeBlockDefinitionStackPanel;
            CubeBlockDefinitionViewer.Width = UICubeBlockDefinitionTabItem.Width;

            foreach (string rawDetailKey in this.CubeBlockDefLabelToValuDict.Keys)
            {
                StackPanel DetailEntryPanel = new StackPanel();
                DetailEntryPanel.Orientation = Orientation.Horizontal;
                DetailEntryPanel.Width = CubeBlockDefinitionStackPanel.Width;
                DetailEntryPanel.Background = Brushes.Orange;

                TextBox KeyTextBox = new TextBox();
                TextBox ValueTextBox = new TextBox();

                KeyTextBox.Text = rawDetailKey;
                KeyTextBox.Width = DetailEntryPanel.Width / 2;

                ValueTextBox.Text = this.CubeBlockDefLabelToValuDict[rawDetailKey].Equals("") ? "// UNDEFINED //" : this.CubeBlockDefLabelToValuDict[rawDetailKey];
                ValueTextBox.Width = DetailEntryPanel.Width / 2;

                DetailEntryPanel.Children.Add(KeyTextBox);
                DetailEntryPanel.Children.Add(ValueTextBox);

                CubeBlockDefinitionStackPanel.Children.Add(DetailEntryPanel);
            }

            this.SizeChanged += CubeblockDefinitionViewer_SizeChanged;
        }
        public CubeblockDefinitionViewer(ref PartSwapper2024 psInstance, BlueprintSBC_CubeBlock currentSBCCubeBlockDefinition)
        {
            InitializeComponent();

            this.PartSwapper2024Ref = psInstance;

            CurrentGridSBCCubeblockDefinition = currentSBCCubeBlockDefinition;

            DefinitionSearchResults = psInstance.SearchCubeBlockDefinitions(CurrentGridSBCCubeblockDefinition);

            if (DefinitionSearchResults[DefinitionSource.Mod] != null &&
                DefinitionSearchResults[DefinitionSource.Mod].Count > 0)
            {
                CurrentCubeBlockDefinition = DefinitionSearchResults[DefinitionSource.Mod].First();
            }

            if (DefinitionSearchResults[DefinitionSource.Vanilla] != null &&
                DefinitionSearchResults[DefinitionSource.Vanilla].Count > 0)
            {
                CurrentCubeBlockDefinition = DefinitionSearchResults[DefinitionSource.Vanilla].First();
            }

            CubeBlockDefinitionXElement = CurrentCubeBlockDefinition.GetCubeBlockDefinitionXElement();

            CurrBlckDefinitionSource = CurrentCubeBlockDefinition.GetDefinitionSourceString();

            CurrBlckTypeId = CurrentCubeBlockDefinition.GetTypeID();
            CurrBlckSubtypeID = CurrentCubeBlockDefinition.GetSubTypeID();
            CurrBlckDisplayName = CurrentCubeBlockDefinition.GetDisplayName();
            CurrBlckIcon = CurrentCubeBlockDefinition.GetIcon();
            CurrBlckDescription = CurrentCubeBlockDefinition.GetDescription();

            CurrBlckCubeSize = CurrentCubeBlockDefinition.GetCubeSize();

            CurrBlckGUIVisible = CurrentCubeBlockDefinition.GetGUIVisible();
            CurrBlckPlaceDecals = CurrentCubeBlockDefinition.GetPlaceDecals();
            CurrBlckBlockTopology = CurrentCubeBlockDefinition.GetBlockTopology();
            CurrBlckSize = CurrentCubeBlockDefinition.GetSize();
            CurrBlckModelOffset = CurrentCubeBlockDefinition.GetModelOffset();
            CurrBlckModel = CurrentCubeBlockDefinition.GetModel();
            CurrBlckCubeDefinition = CurrentCubeBlockDefinition.GetCubeDefinition();
            CurrBlckUseModelIntersection = CurrentCubeBlockDefinition.GetUseModelIntersection();

            CurrBlckComponents = CurrentCubeBlockDefinition.GetComponents().ToList();
            CurrBlckCriticalComponent = CurrentCubeBlockDefinition.GetCriticalComponent();
            CurrBlckMountPoints = CurrentCubeBlockDefinition.GetMountPoints();
            CurrBlckBuildProgressModels = CurrentCubeBlockDefinition.GetBuildProgressModels();
            CurrBlckSkeleton = CurrentCubeBlockDefinition.GetSkeleton();
            CurrBlckBlockPairName = CurrentCubeBlockDefinition.GetBlockPairName();

            UIBlckDefinitionSourceString = CurrBlckDefinitionSource;

            UIBlckTypeIdString = CurrBlckTypeId;
            UIBlckSubtypeIDString = CurrBlckSubtypeID;
            UIBlckDisplayNameString = CurrBlckDisplayName;
            UIBlckIconString = CurrBlckIcon;
            UIBlckDescriptionString = CurrBlckDescription;

            UIBlckCubeSizeString = CurrBlckCubeSize.ToString();

            UIBlckGUIVisibleString = CurrBlckGUIVisible.ToString();
            UIBlckPlaceDecalsString = CurrBlckPlaceDecals.ToString();
            UIBlckBlockTopologyString = CurrBlckBlockTopology.ToString();
            UIBlckSizeString = CurrBlckSize.ToString();
            UIBlckModelOffsetString = CurrBlckModelOffset.ToString();
            UIBlckModelString = CurrBlckModel.ToString();
            UIBlckCubeDefinitionString = CurrBlckCubeDefinition == null ? "" : CurrBlckCubeDefinition.ToString();
            UIBlckUseModelIntersectionString = CurrBlckUseModelIntersection.ToString();

            UIBlckComponentsString = CurrBlckComponents == null ? "" : CurrBlckComponents.ToString();
            UIBlckCriticalComponentString = CurrBlckCriticalComponent == null ? "" : CurrBlckCriticalComponent.ToString();
            UIBlckMountPointsString = CurrBlckMountPoints == null ? "" : CurrBlckMountPoints.ToString();
            UIBlckBuildProgressModelsString = CurrBlckBuildProgressModels == null ? "" : CurrBlckBuildProgressModels.ToString();
            UIBlckSkeletonString = CurrBlckSkeleton == null ? "" : CurrBlckSkeleton.ToString();
            UIBlckBlockPairNameString = CurrBlckBlockPairName == null ? "" : CurrBlckBlockPairName.ToString();

            CubeBlockDefLabelToValuDict = new Dictionary<string, string>{
            {"TypeId",UIBlckTypeIdString},
            {"SubtypeID", UIBlckSubtypeIDString},
            {"DisplayName", UIBlckDisplayNameString},
            {"Icon", UIBlckIconString},
            {"BlckDescription", UIBlckDescriptionString},

            {"BlckCubeSize", UIBlckCubeSizeString},

            {"GUIVisible", UIBlckGUIVisibleString},
            {"PlaceDecals", UIBlckPlaceDecalsString},
            {"BlockTopology", UIBlckBlockTopologyString},
            {"BlckSize", UIBlckSizeString},
            {"ModelOffset", UIBlckModelOffsetString},
            {"BlckModel", UIBlckModelString},
            {"BlckCubeDefinition", UIBlckCubeDefinitionString},
            {"UseModelIntersection", UIBlckUseModelIntersectionString},

            {"Components", UIBlckComponentsString},
            {"CriticalComponent", UIBlckCriticalComponentString},
            {"MountPoints", UIBlckMountPointsString},
            {"BuildProgressModels", UIBlckBuildProgressModelsString},
            {"Skeleton", UIBlckSkeletonString},
            {"BlockPairName", UIBlckBlockPairNameString},
            {"Definition Source",UIBlckDefinitionSourceString}
        };
            this.CubeViewerContentStackPanel.Children.Add(UITabControl);

            UITabControl.Items.Add(UIGridSBCDefinitionTabItem);
            UITabControl.Items.Add(UICubeBlockDefinitionTabItem);

            UITabControl.Width = this.Width;
            UITabControl.Height = this.Height;

            UICubeBlockDefinitionTabItem.Content = CubeBlockDefinitionViewer;
            UICubeBlockDefinitionTabItem.Header = $"Cubeblock Definition for: {UIBlckSubtypeIDString} from {UIBlckDefinitionSourceString}";

            UIGridSBCDefinitionTabItem.Content = CurrentGridSBCCubeblockDefinition;
            UIGridSBCDefinitionTabItem.Header = $"Grid SBC Definition for: {CurrentGridSBCCubeblockDefinition.SubtypeName} from {PartSwapper2024Ref.GetBlueprintDefinition().GetBlueprintName()}";

            CubeBlockDefinitionStackPanel.Width = UITabControl.Width;

            CubeBlockDefinitionViewer.Content = CubeBlockDefinitionStackPanel;
            CubeBlockDefinitionViewer.Width = UICubeBlockDefinitionTabItem.Width;

            foreach (string rawDetailKey in this.CubeBlockDefLabelToValuDict.Keys)
            {
                StackPanel DetailEntryPanel = new StackPanel();
                DetailEntryPanel.Orientation = Orientation.Horizontal;
                DetailEntryPanel.Width = CubeBlockDefinitionStackPanel.Width;
                DetailEntryPanel.Background = Brushes.Orange;

                TextBox KeyTextBox = new TextBox();
                TextBox ValueTextBox = new TextBox();

                KeyTextBox.Text = rawDetailKey;
                KeyTextBox.Width = DetailEntryPanel.Width / 2;

                ValueTextBox.Text = this.CubeBlockDefLabelToValuDict[rawDetailKey].Equals("") ? "// UNDEFINED //" : this.CubeBlockDefLabelToValuDict[rawDetailKey];
                ValueTextBox.Width = DetailEntryPanel.Width / 2;

                DetailEntryPanel.Children.Add(KeyTextBox);
                DetailEntryPanel.Children.Add(ValueTextBox);

                CubeBlockDefinitionStackPanel.Children.Add(DetailEntryPanel);
            }

            this.SizeChanged += CubeblockDefinitionViewer_SizeChanged;
        }

        private void CubeblockDefinitionViewer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UITabControl.Width = e.NewSize.Width;
            UITabControl.Height = e.NewSize.Height;

            CubeBlockDefinitionStackPanel.Width = UITabControl.Width;

            CubeBlockDefinitionViewer.Width = UICubeBlockDefinitionTabItem.Width;

            foreach (StackPanel DetailEntryPanel in CubeBlockDefinitionStackPanel.Children)
            {

                DetailEntryPanel.Width = CubeBlockDefinitionStackPanel.Width;

                TextBox KeyTextBox = DetailEntryPanel.Children[0] as TextBox;
                TextBox ValueTextBox = DetailEntryPanel.Children[1] as TextBox;

                KeyTextBox.Width = DetailEntryPanel.Width / 2;

                ValueTextBox.Width = DetailEntryPanel.Width / 2;
            }
        }
    }
}
