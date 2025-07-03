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

namespace PartSwapperGUI
{
    /// <summary>
    /// Interaction logic for RemoveSpecificBlocksDialogue.xaml
    /// </summary>
    public partial class RemoveAllExceptBlocksDialogue : Window
    {
        private PartSwapper _partSwapperRef;
        private ListBox _transactionLogListbox;

        public RemoveAllExceptBlocksDialogue(PartSwapper ps, ListBox transactionLogListbox)
        {
            _partSwapperRef = ps;
            _transactionLogListbox = transactionLogListbox;
            InitializeComponent();
        }

        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            deletionSpareWildcardTextBox.Text = "";
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void removeAllExceptButton_Click(object sender, RoutedEventArgs e)
        {
            List<string> logEntries;

            string doNotDeleteString = deletionSpareWildcardTextBox.Text;
            removeAllExceptButton.Content = $"Removing all blocks except {doNotDeleteString}";
            logEntries = _partSwapperRef.RemoveAllExcept(doNotDeleteString);

            foreach (string logEntry in logEntries)
            {
                _transactionLogListbox.Items.Add(logEntry);
            }

            if (logEntries.Count > 0)
            {
                MessageBox.Show($"Removed {logEntries.Count} blocks!\nCheck transaction log for details!");
            
            } else
            {
                MessageBox.Show($"Removed no blocks!\nEither your search term was not found,\n...or something went wrong.");
            }

            this.Close();
        }
    }
}
