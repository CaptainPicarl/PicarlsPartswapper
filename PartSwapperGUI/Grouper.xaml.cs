using PartSwapperXMLSE;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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

namespace PartSwapperGUI
{
    /// <summary>
    /// Interaction logic for AutoTechWindow.xaml
    /// </summary>
    public partial class Grouper : Window
    {
        PartSwapper _partswapperReference;
        Dictionary<string, List<XElement>> shipToShipGroupResolver;
        Dictionary<string, List<XElement>> selectedShipBlocks;
        List<XElement> shipPartsFlattened;
        List<XElement> selectedGroupBlocks;
        XElement locatedBlockIter;

        string locatorBlockDims;
        string selectedGrid;
        string selectedGroup;
        string groupName;

        string vectorIIter = "";
        string xIter = "";
        string yIter = "";
        string zIter = "";

        string cubeMinPositionIterString;
        XElement cubeBlockVectorQuery;

        public Grouper(PartSwapper partswapperInstance)
        {
            InitializeComponent();

            this._partswapperReference = partswapperInstance;
            this.shipToShipGroupResolver = _partswapperReference.GetShipGroups();
            
            // Clear listboxes
            CurrentGridsListbox.Items.Clear();
            CurrentGroupsListbox.Items.Clear();
            BlocksInCurrentGroupsListbox.Items.Clear();
            
            foreach (string shipGroup in shipToShipGroupResolver.Keys) {
                
                CurrentGridsListbox.Items.Add(shipGroup);
            }

            shipPartsFlattened = _partswapperReference.GetShipPartsFlattened();
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
            XElement? subtypeName;
            XElement? customName;
            BlocksInCurrentGroupsListbox.Items.Clear();

            selectedGroup = CurrentGroupsListbox.SelectedValue.ToString();
            
            foreach(XElement blockGroup in shipToShipGroupResolver[selectedGrid])
            {
                if (blockGroup.Element("Name").Value.Equals(selectedGroup))
                {
                    selectedGroupBlocks = blockGroup.Element("Blocks").Elements().ToList<XElement>();

                    foreach (XElement block in selectedGroupBlocks)
                    {
                        vectorIIter = block.Value.ToString();
                        xIter = block.Element("X").Value.ToString();
                        yIter = block.Element("Y").Value.ToString();
                        zIter = block.Element("Z").Value.ToString();

                        cubeMinPositionIterString = $"<Min x=\"{xIter}\" y=\"{yIter}\" z=\"{zIter}\" />".ToString();

                        cubeBlockVectorQuery = GetShipPartByDimQuery(xIter, yIter, zIter);

                        if (cubeBlockVectorQuery != null)
                        {
                            subtypeName = cubeBlockVectorQuery.Element("SubtypeName");
                            customName = cubeBlockVectorQuery.Element("CustomName");

                            if (customName == null || customName.Value.Equals(""))
                            {
                                BlocksInCurrentGroupsListbox.Items.Add("Block Type: " + subtypeName.Value + " at X:" + xIter + " Y: " + yIter + " Z: " + zIter);
                            }
                            else
                            {
                                BlocksInCurrentGroupsListbox.Items.Add(customName.Value);
                            }
                        }
                        else
                        {
                            Trace.WriteLine($"Unable to find a block at position:{cubeMinPositionIterString}\n");
                            BlocksInCurrentGroupsListbox.Items.Add($"Unknown block at coords x:{xIter} y:{yIter} z:{zIter}");
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
        private XElement? GetShipPartByDimQuery(string xDim, string yDim, string zDim)
        {
            XElement? result = null;

            foreach (XElement shipPart in this.shipPartsFlattened) {
                if (shipPart.Element("Min").Attribute("x").Value.Equals(xDim) &&
                    shipPart.Element("Min").Attribute("y").Value.Equals(yDim) &&
                    shipPart.Element("Min").Attribute("z").Value.Equals(zDim))
                {
                    // if we found a block at that position: break and return result.
                    result = shipPart; 
                    break;
                }
            }
            
            return result;

        }

        private void CurrentGridsListbox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectedGrid = CurrentGridsListbox.SelectedValue.ToString();

            foreach (XElement group in shipToShipGroupResolver[selectedGrid])
            {
                CurrentGroupsListbox.Items.Add(group.Element("Name").Value);
            }

            selectedShipBlocks = _partswapperReference.GetShipParts();

            GrouperTabControl.SelectedIndex = 1;

        }

        private void BlocksInCurrentGroupsListbox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            BlocksInCurrentGroupsListbox.Items.Clear();

            foreach (XElement block in shipToShipGroupResolver[selectedGrid].Elements(selectedGroup))
            {
                BlocksInCurrentGroupsListbox.Items.Add(block.Name);
            }
        }
    }
}
