using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using PartSwapperXMLSE;

namespace PartSwapperGUI
{
    /// <summary>
    /// Interaction logic for Options.xaml
    /// </summary>
    public partial class Options : Window
    {
       
        public Options(ConfigOptions config)
        {
            InitializeComponent();

            optionsGrid.IsReadOnly = false;

            optionsGrid.ItemsSource = config.OptsDict;

        }

        private void optionsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            KeyValuePair<string, string> kvp = (KeyValuePair<string, string>)e.AddedItems[0];
            
            oldValueTextbox.Text = "Editing Value for " +  kvp.Key;
            newValueTextbox.Text = kvp.Value;
        }
    }
}
