using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    /// Interaction logic for TransactionWindow2024.xaml
    /// </summary>
    public partial class TransactionWindow2024 : Window
    {
        public TransactionLog UITransactionLog { get => _UITransactionLog; }

        private TransactionLog _UITransactionLog;
        private ObservableCollection<TransactionLog.TransactionLogEntry> _ObservableTransactions = new ObservableCollection<TransactionLog.TransactionLogEntry>();
        private string _OperationString;

        public TransactionWindow2024(ref TransactionLog transactionLog, string operationString)
        {
            InitializeComponent();

            this._OperationString = operationString;

            this.PopulateLabelStackPanel(this._OperationString);
            this._UITransactionLog = transactionLog;
            this._ObservableTransactions = LoadObservableTransactions(transactionLog);
            this.TransactionsListBox.ItemsSource = this._ObservableTransactions;
        }

        public ObservableCollection<TransactionLog.TransactionLogEntry> LoadObservableTransactions(TransactionLog transactionLog)
        {
            ObservableCollection<TransactionLog.TransactionLogEntry> NewObservableTransactions = new ObservableCollection<TransactionLog.TransactionLogEntry>();

            if (transactionLog == null)
            {
                return NewObservableTransactions;
            }

            if(transactionLog.GetOrderedLogList() == null)
            {
                NewObservableTransactions = new ObservableCollection<TransactionLog.TransactionLogEntry>();
            }

            if (transactionLog.GetOrderedLogList().Count > 0)
            {
                foreach (TransactionLog.TransactionLogEntry logEntry in transactionLog.GetOrderedLogList())
                {
                    NewObservableTransactions.Add(logEntry);
                }
            }

            return NewObservableTransactions;
        }

        private void PopulateLabelStackPanel(string OperationString)
        {
            TextBlock TransactionsTextBlock = new TextBlock();
            TransactionsTextBlock.Text = $"Transactions for operation: {OperationString}";
            this.TransactionsLabelStackPanel.Children.Add(TransactionsTextBlock);
        }
    }
}
