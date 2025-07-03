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
    public partial class RemoveSpecificBlocksDialogue : Window
    {
        private PartSwapper _partSwapperRef;
        private ListBox _transactionLogListbox;

        public RemoveSpecificBlocksDialogue(PartSwapper ps, ListBox transactionLogListbox)
        {
            _partSwapperRef = ps;
            _transactionLogListbox = transactionLogListbox;
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
            List<string> logEntries;

            string deleteString = removeSpecificTermTextBox.Text;
            removeSpecificButton.Content = $"Removing blocks containing the word: {deleteString}";
            logEntries = _partSwapperRef.RemoveAllExcept(deleteString);

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

        private void removeSpecificButton_Click(object sender, RoutedEventArgs e)
        {
            List<string> logEntries;

            string DeleteString = removeSpecificTermTextBox.Text;
            removeSpecificButton.Content = $"Removing blocks specifically named: {DeleteString}...";
            logEntries = _partSwapperRef.RemoveSpecific(DeleteString);

            foreach (string logEntry in logEntries)
            {
                _transactionLogListbox.Items.Add(logEntry);
            }

            if (logEntries.Count > 0)
            {
                MessageBox.Show($"Removed {logEntries.Count} blocks!\nCheck transaction log for details!");

            }
            else
            {
                MessageBox.Show($"Removed no blocks!\nEither your search term was not found,\n...or something went wrong.");
            }

            this.Close();
        }
    }
}
