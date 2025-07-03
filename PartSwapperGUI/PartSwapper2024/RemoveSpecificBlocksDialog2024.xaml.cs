using PartSwapperXMLSE;
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

namespace PartSwapperGUI.PartSwapper2024
{
    /// <summary>
    /// Interaction logic for RemoveSpecificBlocksDialogue.xaml
    /// </summary>
    public partial class RemoveSpecificBlocksDialogue2024 : Window
    {
        private PartSwapper2024 _partSwapperRef;

        public RemoveSpecificBlocksDialogue2024(PartSwapper2024 ps)
        {
            _partSwapperRef = ps;
            InitializeComponent();
        }

        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            removeSpecificTermTextBox.Text = "";
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void removeAllExceptButton_Click(object sender, RoutedEventArgs e)
        {
            string RetainedString = removeSpecificTermTextBox.Text;
            removeSpecificButton.Content = $"Removing blocks containing the word: {RetainedString}";
            _partSwapperRef.PerformOperation("RemoveTool",new Dictionary<string, object> { { "RemoveAllExcept", RetainedString } });
        }

        private void removeSpecificButton_Click(object sender, RoutedEventArgs e)
        {
            string DeleteString = removeSpecificTermTextBox.Text;
            removeSpecificButton.Content = $"Removing blocks specifically named: {DeleteString}...";
            _partSwapperRef.PerformOperation("RemoveTool", new Dictionary<string, object> { { "RemoveSpecific", DeleteString } });
        }
    }
}
