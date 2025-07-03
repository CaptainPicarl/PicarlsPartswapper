using PartSwapperXMLSE;
using System.Reflection;
using System.Xml.Linq;
using SkiaSharp;
using System.Windows.Threading;
using System.Windows.Navigation;
using SkiaSharp.Views.WPF;
using PartSwapperGUI;
using PartSwapperGUI.PartSwapper2024;
using System.Windows;
using OpenTK.Graphics.OpenGL;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PartSwapperUnitTests
{
    [TestClass]
    public class Partswapper2024Tests
    {
        public static string ShipBlueprintSBCPath = "BlueprintSBCYouWantToTestGoesHere";
        public static string CubeBlocksSBCPath = "E:\\SteamLibrary\\steamapps\\common\\SpaceEngineers\\Content\\Data\\CubeBlocks\\";
        public static string SEContentPath = "E:\\SteamLibrary\\steamapps\\common\\SpaceEngineers\\Content\\";
        public static string WorkshopDirPath = "E:\\SteamLibrary\\steamapps\\workshop\\content\\244850";


        public static DataFolder_Model TestModFolder = new DataFolder_Model(WorkshopDirPath);
        public static DataFolder_Model TestVanillaFolder = new DataFolder_Model(SEContentPath);
        public static BlueprintSBC_BlueprintDefinition TestBlueprintDefinition = new BlueprintSBC_BlueprintDefinition(ShipBlueprintSBCPath);
        public static BlueprintSBC_CubeGrid TestCubeGrid = TestBlueprintDefinition.GetCubegrids().First();
        public static BlueprintSBC_CubeBlock TestCubeBlock = TestCubeGrid.GetCubeBlocks().First();
        public static CubeBlockDefinitionSBC_CubeBlockDefinition TestCubeBlockDefinition = TestVanillaFolder.GetCubeBlocksDefDict().First().Value.First();

        public PartSwapperGUI.App TestPSGUIApplication = new PartSwapperGUI.App();

        public Window TestMainWindow;
        public SKElement TestSKElement;
        public PartSwapper2024 TestPS2024Instance;


        Thread UIThread;
        public Partswapper2024Tests()
        {
        }
        

        public void InitMainWindow()
        {
            UIThread = new Thread(() =>
            {

                this.TestMainWindow = new PartSwapperGUI.PartSwapper2024.MainWindow(ShipBlueprintSBCPath,true, true);
            });

            UIThread.SetApartmentState(ApartmentState.STA);
            UIThread.Start();

            while (this.TestMainWindow == null)
            {
                Thread.Sleep(100);
            }
        }

        public void InitPS2024Instance()
        {
            UIThread = new Thread(() =>
            {

                this.TestSKElement = new SKElement();
                this.TestMainWindow = new PartSwapperGUI.PartSwapper2024.MainWindow(ShipBlueprintSBCPath,true,true);
                this.TestPS2024Instance = new PartSwapper2024(ShipBlueprintSBCPath, ref TestMainWindow, ref TestSKElement, SEContentPath, WorkshopDirPath, true);
            });

            UIThread.SetApartmentState(ApartmentState.STA);
            UIThread.Start();

            while (TestPS2024Instance == null)
            {
                Thread.Sleep(100);
            }
        }

        [TestMethod]
        public void TestBlueprintLoad()
        {
            BlueprintSBC_BlueprintDefinition blueprint = new BlueprintSBC_BlueprintDefinition(ShipBlueprintSBCPath);

            Assert.IsNotNull(blueprint);
        }

        [TestMethod, STAThread]
        public void TestBlueprintSkiaGridLoad()
        {
            UIThread = new Thread(() => {
                this.TestMainWindow = new PartSwapperGUI.PartSwapper2024.MainWindow(ShipBlueprintSBCPath, true, true);
                TestMainWindow.ShowDialog();
            });

            UIThread.SetApartmentState(ApartmentState.STA);
            UIThread.Start();

            while (UIThread.IsAlive)
            {
                Thread.Sleep(100);
            }

            Assert.IsNotNull(null);
        }

        [TestMethod]
        public void TestSkiaGridEnumerator()
        {
            SKElement skElement = new SKElement();
            skElement.Width = 1000;
            skElement.Height = 1000;

            BlueprintSBC_BlueprintDefinition blueprint = new BlueprintSBC_BlueprintDefinition(ShipBlueprintSBCPath);
            BlueprintCellGridManager bpSkiaGrid = new BlueprintCellGridManager(ref this.TestPS2024Instance,ref blueprint, ref TestMainWindow);
            CubeGridRenderCellGrid UI3dGrid = bpSkiaGrid.GetCurrentRenderGrid();

            SKPoint3 cellCoordIterator;

            foreach (BlueprintCell cell in UI3dGrid)
            {
                if (cell == null)
                {
                    continue;
                }
                else
                {
                    cellCoordIterator = cell.GetBP3DCoordinateCenter();
                }
            }

            Assert.IsNotNull(blueprint);
            Assert.IsNotNull(UI3dGrid);

        }

        [TestMethod]
        public void TestSkiaGridRender()
        {

            while (UIThread.ThreadState == ThreadState.Running)
            {
                Thread.Sleep(100);
            }

            TestMainWindow.ShowDialog();

            Assert.IsTrue(false);
        }

        [TestMethod]
        public void TestTransactionsWindow()
        {
            TransactionWindow2024 TransactionWindow;

            UIThread = new Thread(() =>
            {
                TransactionWindow =  new TransactionWindow2024(ref this.TestPS2024Instance.MasterLogRef,"TestTransactionWindow");
                TransactionWindow.ShowDialog();
            });

            UIThread.SetApartmentState(ApartmentState.STA);
            UIThread.Start();

            while (UIThread.ThreadState == ThreadState.Running)
            {
                Thread.Sleep(100);
            }

        }

        [TestMethod]
        public void TestCubeBlocksLoad()
        {
            Dictionary<string, CubeBlockDefinitionSBC_CubeBlockDefinitionFile> CubeBlockDefinitions = new Dictionary<string, CubeBlockDefinitionSBC_CubeBlockDefinitionFile>();

            XElement cubeblockDefRootElementIterator;

            foreach (string path in Directory.GetFiles(CubeBlocksSBCPath))
            {
                try
                {
                    cubeblockDefRootElementIterator = XElement.Load(path);
                }
                catch
                {
                    continue;
                }

                if (cubeblockDefRootElementIterator.Element("CubeBlocks") != null)
                {
                    CubeBlockDefinitions.Add(path, new CubeBlockDefinitionSBC_CubeBlockDefinitionFile(cubeblockDefRootElementIterator, DefinitionSource.Unknown));
                }
            };

            Assert.IsTrue(CubeBlockDefinitions.Count > 0);
        }

        [TestMethod]
        public void TestModdedCubeblockLoad()
        {
            DataFolder_Model testModFolder = new DataFolder_Model(WorkshopDirPath);
            Assert.IsTrue(testModFolder.GetComponentsDefDict().Count > 0);


            DataFolder_Model testVanillaFolder = new DataFolder_Model(SEContentPath);
            Assert.IsTrue(testVanillaFolder.GetComponentsDefDict().Count > 0);
        }

        [TestMethod]
        public void TestPartswapOpWindow()
        {

            PartswapOp2024Window partswapOpWindow = null;

            TestPSGUIApplication.InitializeComponent();

            UIThread = new Thread(() =>
            {
                partswapOpWindow = new PartswapOp2024Window(ref TestPS2024Instance, ref TestCubeGrid, TestModFolder, TestVanillaFolder);
                partswapOpWindow.ShowDialog();
            });

            UIThread.SetApartmentState(ApartmentState.STA);
            UIThread.Start();

            while (UIThread.ThreadState == ThreadState.Running)
            {
                Thread.Sleep(100);
            }

            Assert.IsTrue(false);

        }

        [TestMethod]
        public void TestCubeViewerWindow()
        {
            //TestPSGUIApplication.InitializeComponent();

            CubeblockDefinitionViewer CubeViewer = null;

            UIThread = new Thread(() =>
            {
                CubeViewer = new CubeblockDefinitionViewer(ref this.TestPS2024Instance, TestCubeBlockDefinition);
                CubeViewer.ShowDialog();
            });

            UIThread.SetApartmentState(ApartmentState.STA);
            UIThread.Start();

            while (UIThread.ThreadState == ThreadState.Running)
            {
                Thread.Sleep(100);
            }

            Assert.IsTrue(false);

        }

        [TestMethod]
        public void TestGridStatsWindow()
        {
            //TestPSGUIApplication.InitializeComponent();
            Thread PSThread;
            PSThread = new Thread(() =>
            {
                this.InitPS2024Instance();
            });

            PSThread.SetApartmentState(ApartmentState.STA);
            PSThread.Start();

            while (PSThread.ThreadState != ThreadState.Stopped)
            {
                Thread.Sleep(100);
            }

            UIThread = new Thread(() =>
            {
                GridStats gridStatsWindow = new GridStats(TestPS2024Instance, TestCubeGrid);
                gridStatsWindow.ShowDialog();
            });

            UIThread.SetApartmentState(ApartmentState.STA);
            UIThread.Start();

            while (UIThread.ThreadState != ThreadState.Stopped)
            {
                Thread.Sleep(100);
            }

            Assert.IsTrue(false);
        }
    }
}