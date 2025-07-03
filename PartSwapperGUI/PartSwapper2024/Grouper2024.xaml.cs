using PartSwapperXMLSE;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    /// Interaction logic for AutoTechWindow.xaml
    /// </summary>
    public partial class Grouper : Window
    {
        PartSwapper2024 _partswapperReference;
        Dictionary<string, List<Vector3>> BlockGroupsDictionary;
        List<BlueprintSBC_CubeBlock> shipParts;
        List<Vector3> selectedGroupBlocks;
        BlueprintSBC_CubeBlock locatedBlockIter;

        string locatorBlockDims;
        string selectedGroup;
        string groupName;

        string vectorIIter = "";
        string xIter = "";
        string yIter = "";
        string zIter = "";

        string cubeMinPositionIterString;
        BlueprintSBC_CubeBlock cubeBlockVectorQuery;

        public Grouper(PartSwapper2024 partswapperInstance)
        {
            InitializeComponent();

            this._partswapperReference = partswapperInstance;
            this.BlockGroupsDictionary = _partswapperReference.GetGridRendererRef().GetCurrentRenderEntry().Item1.cubeGridDefinitionRef.GetBlockGroups();
            
            // Clear listboxes
            CurrentGroupsListbox.Items.Clear();
            BlocksInCurrentGroupsListbox.Items.Clear();
            

            shipParts = _partswapperReference.GetGridRendererRef().GetCurrentRenderEntry().Item1.cubeGridDefinitionRef.GetCubeBlocks();
        }

        private void grouperExecuteButton_Click(object sender, RoutedEventArgs e)
        {
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void CurrentGroupsListbox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string? subtypeName;
            string? customName;
            BlocksInCurrentGroupsListbox.Items.Clear();

            selectedGroup = CurrentGroupsListbox.SelectedValue.ToString();
            
            foreach(KeyValuePair<string,List<Vector3>> blockGroup in BlockGroupsDictionary)
            {
                if (blockGroup.Key.Equals(selectedGroup))
                {
                    selectedGroupBlocks = blockGroup.Value;

                    foreach (Vector3 blockVector in selectedGroupBlocks)
                    {
                        vectorIIter = blockVector.ToString();
                        xIter = blockVector.X.ToString();
                        yIter = blockVector.Y.ToString();
                        zIter = blockVector.Z.ToString();

                        cubeMinPositionIterString = $"<Min x=\"{xIter}\" y=\"{yIter}\" z=\"{zIter}\" />".ToString();

                        cubeBlockVectorQuery = GetShipPartByDimQuery(xIter, yIter, zIter);

                        if (cubeBlockVectorQuery != null)
                        {
                            subtypeName = cubeBlockVectorQuery.GetSubtypeName();
                            customName = cubeBlockVectorQuery.GetCustomName();

                            if (customName == null || customName.Equals(""))
                            {
                                BlocksInCurrentGroupsListbox.Items.Add("Block Type: " + subtypeName + " at X:" + xIter + " Y: " + yIter + " Z: " + zIter);
                            }
                            else
                            {
                                BlocksInCurrentGroupsListbox.Items.Add(customName);
                            }
                        }
                        else
                        {
                            Trace.WriteLine($"Unable to find a blockVector at position:{cubeMinPositionIterString}\n");
                            BlocksInCurrentGroupsListbox.Items.Add($"Unknown blockVector at coords x:{xIter} y:{yIter} z:{zIter}");
                        }

                        // return subtypename and customname to null for each iteration.
                        subtypeName = null;
                        customName = null;
                    }
                }
            }
            GrouperTabControl.SelectedIndex = 2;
        }

        // Assumes shipParts exists
        // Iterates through all blocks and returns the first entry located at that particular 'Min' position
        // TODO: This will be terribly slow, as-converted. It's O(n)!
        private BlueprintSBC_CubeBlock? GetShipPartByDimQuery(string xDim, string yDim, string zDim)
        {
            BlueprintSBC_CubeBlock? result = null;

            foreach (BlueprintSBC_CubeBlock shipPart in this.shipParts) {
                if (shipPart.GetMinVector().X.ToString().Equals(xDim) &&
                    shipPart.GetMinVector().Y.ToString().Equals(yDim) &&
                    shipPart.GetMinVector().Z.ToString().Equals(zDim))
                {
                    // if we found a blockVector at that position: break and return result.
                    result = shipPart; 
                    break;
                }
            }
            
            return result;

        }


        private void BlocksInCurrentGroupsListbox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}
