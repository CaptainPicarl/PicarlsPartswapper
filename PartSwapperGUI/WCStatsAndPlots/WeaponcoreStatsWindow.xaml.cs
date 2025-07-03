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
using PartSwapperXMLSE;
using ScottPlot.WPF;
namespace PartSwapperGUI
{
    /// <summary>
    /// Interaction logic for WeaponcoreStatsWindow.xaml
    /// </summary>
    public partial class WeaponcoreStatsWindow : Window
    {
        ConfigOptions _config;
        WeaponcoreStats _weaponcoreStats;
        List<WpfPlot> _plots = new List<WpfPlot>();
        TabItem tabItemIter = new TabItem();
        WpfPlot wpfPlotIter = new WpfPlot();

        public WeaponcoreStatsWindow(ConfigOptions config)
        {
            InitializeComponent();

            _config = config;

            _weaponcoreStats = new WeaponcoreStats(_config);


            TabItem maxTargetsItem = new TabItem();

            maxTargetsItem.Name = "MaxTargetsTabItem";
            maxTargetsItem.Content = WCStatsAndPlots.WCStatsAndPlots.generateWeaponRangesScttPltWPF(WeaponcoreStats.wcDefinitions, WeaponcoreStats.color1, WeaponcoreStats.color2, WeaponcoreStats.color3);

            StatsTabControl.Items.Add(maxTargetsItem);
        }
    }
}
