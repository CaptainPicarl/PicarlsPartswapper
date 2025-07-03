using System;
using System.Collections.Generic;
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
    /// Interaction logic for XMLViewer.xaml
    /// </summary>
    public partial class XMLViewer : Window
    {
        public XMLViewer(XElement shipRoot)
        {
            InitializeComponent();
            PopulateBPCTreeView(shipRoot, xmlTreeViewer);
        }

        // Recursive method to descend into the .BPC XML file and populate the treeview
        private void PopulateBPCTreeView(XElement Shiproot, TreeView xmlTreeViewer)
        {
            if (Shiproot == null)
            {
                MessageBox.Show("Shiproot was null! Failed to load!");
                return;
            }

            TreeViewItem newNode = new TreeViewItem();
            newNode.IsExpanded = true;
            newNode.Header = Shiproot.Name;

            PopulateBPCTreeViewHelper(Shiproot, newNode);

            xmlTreeViewer.Items.Add(newNode);
            xmlTreeViewer.Focus();
        }

        // Recursive Helper that does most of the actual recursion
        private void PopulateBPCTreeViewHelper(XElement Shiproot, TreeViewItem currNode)
        {
            foreach (XElement childXElement in Shiproot.Elements())
            {
                TreeViewItem childTreeViewNode = new TreeViewItem();
                childTreeViewNode.IsExpanded = true;

                // This switch will omit certain XElement values from being appended, since they may be kinda huge
                // Example: We don't want to append the "Cubegrids" value, because it's gonna contain the entire tree of values!
                switch (childXElement.Name.ToString())
                {
                    case "ShipBluePrints":
                        childTreeViewNode.Header = childXElement.Name;
                        break;
                    case "ComponentData":
                        childTreeViewNode.Header = childXElement.Name;
                        break;
                    case "CubeGrids":
                        childTreeViewNode.Header = childXElement.Name;
                        break;
                    case "Storage":
                        childTreeViewNode.Header = childXElement.Name;
                        break;
                    case "ComponentContainer":
                        childTreeViewNode.Header = childXElement.Name;
                        break;
                    case "Components":
                        childTreeViewNode.Header = childXElement.Name;
                        break;
                    case "Component":
                        childTreeViewNode.Header = childXElement.Name;
                        break;
                    case "ShipBlueprint":
                        childTreeViewNode.Header = childXElement.Name;
                        break;
                    case "BlueprintSBC_CubeGrid":
                        childTreeViewNode.Header = childXElement.Name;
                        break;

                    case "CubeBlocks":
                        childTreeViewNode.Header = childXElement.Name;
                        break;

                    case "MyObjectBuilder_CubeBlock":
                        childTreeViewNode.Header = childXElement.Name;
                        break;

                    default:
                        childTreeViewNode.Header = childXElement.Name + " - " + childXElement.Value;
                        break;
                }
                currNode.Items.Add(childTreeViewNode);
                PopulateBPCTreeViewHelper(childXElement, childTreeViewNode);
            }

        }
    }

}
