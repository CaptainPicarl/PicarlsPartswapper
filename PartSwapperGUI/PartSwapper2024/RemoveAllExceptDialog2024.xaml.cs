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
    public partial class RemoveAllExceptBlocksDialogue : Window
    {
        private PartSwapper2024 _partSwapperRef;

        public RemoveAllExceptBlocksDialogue(PartSwapper2024 ps)
        {
            _partSwapperRef = ps;
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

            string doNotDeleteString = deletionSpareWildcardTextBox.Text;
            removeAllExceptButton.Content = $"Removing all blocks except {doNotDeleteString}";
            this._partSwapperRef.BackupShipXML();
            this._partSwapperRef.PerformOperation("RemoveTool", new Dictionary<string, object> { { "Operation", "RemoveAllExcept" }, { "RemoveAllExcept", doNotDeleteString } });
            this._partSwapperRef.GetBlueprintDefinition().SaveFile();
            this._partSwapperRef.DeleteSBC5File();
            this._partSwapperRef.GetGridRendererRef().RedrawSkia();

        }
    }
}
