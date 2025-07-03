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

namespace PartSwapperGUI.PartSwapper2024
{
    /// <summary>
    /// Interaction logic for AutoTechWindow.xaml
    /// </summary>
    public partial class AutoTechWindow : Window
    {
        private PartSwapper2024 _partswapperReference;

        public AutoTechWindow(PartSwapper2024 partswapperInstance)
        {
            this._partswapperReference = partswapperInstance;
            InitializeComponent();
            
        }

        private void OnAutoTechExecuteButton_Click(object sender, RoutedEventArgs e)
        {
            string checkboxName;

            int techlevelValue = -1;

            List<BlueprintSBC_CubeBlock> shipPartsRef = _partswapperReference.GetGridRendererRef().GetCurrentRenderEntry().Item1.cubeGridDefinitionRef.GetCubeBlocks();


            // Get user-selected techLevelValue
            try
            {
                techlevelValue = int.Parse(autotechLevelComboBox.Text);
            } catch(Exception ex)
            {
                Trace.WriteLine($"AutoTech error parsing techlevelValue. See below:\n" + ex.ToString() + "\n");
            }

            //Checkbox processing. Checks each checkbox for its value.
            foreach (CheckBox box in systemSelectorStackPanel.Children)
            {
                if (box.IsChecked.Value)
                {
                    checkboxName = box.Content.ToString();

                    try
                    {
                        TransactionLog ResultingLogs;
                        ResultingLogs = _partswapperReference.PerformOperation("AutoTech", new Dictionary<string, object> { {"categoryName", checkboxName }, { "desiredTechlevel", techlevelValue } });
                        this._partswapperReference.MasterLogRef.Merge(ResultingLogs);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("AutoTech failed to tech level:" + techlevelValue.ToString() + ex.ToString());
                    }
                }
            }
            

        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
