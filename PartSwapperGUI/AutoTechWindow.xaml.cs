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
    public partial class AutoTechWindow : Window
    {
        PartSwapper _partswapperReference;

        ListBox transactionLogListboxRef;
        public AutoTechWindow(PartSwapper partswapperInstance, ListBox transactionLogListboxRef)
        {
            this._partswapperReference = partswapperInstance;
            this.transactionLogListboxRef = transactionLogListboxRef;
            InitializeComponent();
            
        }

        

        private void autoTechExecuteButton_Click(object sender, RoutedEventArgs e)
        {
            List<string> transactionLog = new List<string>();
           

            string checkboxName;

            int techlevelValue = -1;
            int transactionLogCounter = 0;

            Dictionary<string, List<XElement>> shipPartsRef = _partswapperReference.GetShipParts();

            // clear any pre-existing log
            transactionLogListboxRef.Items.Clear();

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
                    foreach (string variant in shipPartsRef.Keys)
                    {
                        if (variant.Contains(checkboxName))
                        {
                            try
                            {
                                transactionLog = _partswapperReference.AutoTech(variant, techlevelValue);

                                foreach (string item in transactionLog)
                                {
                                    transactionLogListboxRef.Items.Add(item);
                                    transactionLogCounter++;
                                }
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show("AutoTech failed to tech level:" + techlevelValue.ToString());
                            }
                        }

                    }
                }
            }
            
            MessageBox.Show($"AutoTech Complete!\nAdded {transactionLogCounter} entries to transaction log!");
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
