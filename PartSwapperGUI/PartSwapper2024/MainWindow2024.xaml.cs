using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Win32;
using PartSwapperXMLSE;
using SkiaSharp;
using SkiaSharp.Views.WPF;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
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
using SkiaSharp.Views.WPF;

namespace PartSwapperGUI.PartSwapper2024
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static string _AppDataPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PicarlsPartswapper2024", "settings.json");
        private static string _SteamWorkshopModDirOptKey = "SteamWorkshopModsDirectory";
        private static string _SteamSEGameContentDirKey = "SEBaseGameContentDirectory";

        private bool _DebugMode = false;
        private bool _AdSkip = false;

        private string _BlueprintFilePath = "";
        private PartSwapper2024 _PartswapperInstance;
        private BlueprintSBC_BlueprintDefinition? _BlueprintDefinitionRef;
        private GridRenderer2024? _GridRenderer2024Ref;
        private GridCursor? _GridCursorRef;
        private BlueprintCell? _HoveredBPCell;
        private BlueprintSBC_CubeBlock? _HoveredCubeblockDefinition;
        private ConfigOptions _ConfigOptions;

        private int _zCursor = 0;
        private int zCursorMax = 0;

        private Window WindowSelfRef;

        FoolFee foolFeeInstance;

        TransactionLog TransactionLogTemp;

        public MainWindow(string sbcPath,bool AdSkip, bool debugMode)
        {
            this._DebugMode = debugMode;
            this.foolFeeInstance = new FoolFee();
            this._AdSkip = AdSkip;

            if (!_AdSkip)
            {
                this.foolFeeInstance.ShowDialog();
            }


            _ConfigOptions = new ConfigOptions(_AppDataPath);
            _ConfigOptions.LoadOrCreateConfig();
            _ConfigOptions.SetOption("Debug", _DebugMode.ToString());

            if (_ConfigOptions.GetOption(_SteamSEGameContentDirKey) == null)
            {
                _ConfigOptions.ConfigOptionsPromptSetDirectory("Please locate your SE Installation Folder for Space Engineers", "Locate Space Engineers Data Directory...",
                    "\\steamapps\\common\\SpaceEngineers\\Content",
                    _SteamSEGameContentDirKey);
            }


            if (_ConfigOptions.GetOption(_SteamWorkshopModDirOptKey) == null)
            {
                _ConfigOptions.ConfigOptionsPromptSetDirectory("Please locate your Workshop Mods folder for Space Engineers", "Locate Space Engineers Workshop Mod Directory...",
                    "\\steamapps\\workshop\\content\\244850",
                    _SteamWorkshopModDirOptKey);
            }

            InitializeComponent();

            if (sbcPath.Equals(""))
            {
                //Do nothing, set nothing
                //throw new ArgumentException("No Blueprint Path provided!");
            }
            else
            {
                this._BlueprintFilePath = sbcPath;
                this.Loaded -= this.SetupWindow;
                this.Loaded += this.SetupWindow;

                this.Loaded -= this.InitializePartSwapper;
                this.Loaded += this.InitializePartSwapper;

                this.MouseMove -= this.OnStatusCursorWindowPositionValue_MouseMove;
                this.MouseMove += this.OnStatusCursorWindowPositionValue_MouseMove;
            }

        }

        // Actual constructor used in app
        public MainWindow()
        {
            this.foolFeeInstance = new FoolFee();
            this._AdSkip = false;

            if (!_AdSkip)
            {
                this.foolFeeInstance.ShowDialog();
            }

            _ConfigOptions = new ConfigOptions(_AppDataPath);
            _ConfigOptions.LoadOrCreateConfig();
            _ConfigOptions.SetOption("Debug", _DebugMode.ToString());

            if (_ConfigOptions.GetOption(_SteamSEGameContentDirKey) == null)
            {
                _ConfigOptions.ConfigOptionsPromptSetDirectory("Please locate your SE Installation Folder for Space Engineers","Locate Space Engineers Data Directory...", 
                    "\\steamapps\\common\\SpaceEngineers\\Content", 
                    _SteamSEGameContentDirKey);
            }


            if (_ConfigOptions.GetOption(_SteamWorkshopModDirOptKey) == null)
            {
                _ConfigOptions.ConfigOptionsPromptSetDirectory("Please locate your Workshop Mods folder for Space Engineers", "Locate Space Engineers Workshop Mod Directory...", 
                    "\\steamapps\\workshop\\content\\244850", 
                    _SteamWorkshopModDirOptKey);
            }


            InitializeComponent();

            if (_BlueprintFilePath.Equals(""))
            {
                this.Loaded += this.SetupWindow;
                //Do nothing, set nothing. There's no file to load!
            }
            else
            {
                // In case the _BlueprintFilePath was set externally...
                this.Loaded -= this.SetupWindow;
                this.Loaded += this.SetupWindow;

                this.Loaded -= this.InitializePartSwapper;
                this.Loaded += this.InitializePartSwapper;

                this.MouseMove -= OnStatusCursorWindowPositionValue_MouseMove;
                this.MouseMove += OnStatusCursorWindowPositionValue_MouseMove;

            }
        }

        private void OnSliderValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_DebugMode)
            {
                Trace.WriteLine("Entering OnSliderValueChanged...");
            }
            this._GridRenderer2024Ref.ZCursor = this._zCursor;

            this.StatusZLayerIndValue.Text = this._zCursor.ToString();

            this.zAxisSlider.Value = this._zCursor;

            this._GridRenderer2024Ref.RedrawSkia();

            if (_DebugMode)
            {
                Trace.WriteLine("Exiting OnSliderValueChanged...");
            }

        }

        private void OnZAxisSlider_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (_DebugMode)
            {
                Trace.WriteLine("Entering OnZAxisSlider_MouseWheel...");
            }

            this.zCursorMax = this._GridRenderer2024Ref.MaxZCursorValue;

            if (this._zCursor <= this.zCursorMax)
            {
                if (e.Delta > 0)
                {
                    if (this._zCursor + 1 > this.zCursorMax)
                    {
                        this._zCursor = this.zCursorMax;
                    }
                    else
                    {
                        this._zCursor += 1;
                    }
                }
                else
                {
                    if (this._zCursor - 1 < 0)
                    {
                        this._zCursor = 0;
                    }
                    else
                    {
                        this._zCursor -= 1;
                    }
                }

                OnSliderValueChanged(null, null);
            }

            if (_DebugMode)
            {
                Trace.WriteLine("Exiting OnZAxisSlider_MouseWheel...");
            }
        }

        private void OnMenuItem_Exit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OnMenuItem_LoadBPC_Click(object sender, RoutedEventArgs e)
        {
            if (_DebugMode)
            {
                Trace.WriteLine("Entering OnMenuItem_LoadBPC_Click...");
            }

            OpenFileDialog fileDialogue = new OpenFileDialog();
            fileDialogue.FileName = "Spaceship"; // Default file name
            fileDialogue.DefaultExt = ".sbc"; // Default file extension
            fileDialogue.Filter = "SE Definition Files (.sbc)|*.sbc"; // Filter files by extension
            fileDialogue.Title = "Select a grid definition...";
            fileDialogue.InitialDirectory = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SpaceEngineers\\Blueprints");

            // Show open file dialog box
            bool? filenameBool = fileDialogue.ShowDialog();

            // Process open file dialog box results
            if (filenameBool == true)
            {
                // Open document
                _BlueprintFilePath = fileDialogue.FileName;

            }
            else
            {
                this.StatusBlueprintPathValue.Text = "NO BLUEPRINT LOADED!";
            }

            // Now that the file has been selected, and if the _SEInstallFolderPath is selected - init a new PartSwapper instance!
            if (_BlueprintFilePath.Length > 0)

            {
                //this.OnInitializeGridRenderer(null, null); <-- old
                //this.InitializePartSwapper(_BlueprintFilePath);

                this.InitializePartSwapper(null,null);

                // Update currentBlueprintLabel information
                this.StatusBlueprintPathValue.Text = this._BlueprintDefinitionRef.GetBlueprintName();

                this.MouseMove -= OnStatusCursorWindowPositionValue_MouseMove;
                this.MouseMove += OnStatusCursorWindowPositionValue_MouseMove;

                this.CalculateSetTabControlDimensions(this.RenderSize);
                this._PartswapperInstance.SubmitWindowSizeUpdate(this.RenderSize);
            }
            else
            {
                MessageBox.Show("Invalid file selected!");
            }

            if (_DebugMode)
            {
                Trace.WriteLine("Exiting OnMenuItem_LoadBPC_Click...");
            }
        }

        private void OnMainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_DebugMode)
            {
                Trace.WriteLine("MainWindow: Entering OnMainWindow_SizeChanged...");
            }

            this.CalculateSetTabControlDimensions(e.NewSize);
            this._PartswapperInstance.SubmitWindowSizeUpdate(e.NewSize);

            if (_DebugMode)
            {
                Trace.WriteLine("MainWindow: Exiting OnMainWindow_SizeChanged...");
            }
        }

        private void SetupWindow(object sender, RoutedEventArgs e)
        {
            this.WindowSelfRef = this;

            this.WindowSelfRef.WindowState = WindowState.Normal;
            this.WindowSelfRef.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            this.WindowSelfRef.WindowStyle = WindowStyle.ThreeDBorderWindow;
            this.WindowSelfRef.SizeToContent = SizeToContent.Manual;
            this.WindowSelfRef.Visibility = Visibility.Visible;
            this.WindowSelfRef.Height = SystemParameters.FullPrimaryScreenHeight / 2;
            this.WindowSelfRef.Width = SystemParameters.FullPrimaryScreenWidth / 2;

            this.CalculateSetTabControlDimensions(this.RenderSize);

            this.CalculateSetSliderHeight();
        }

        public void SetDebugMode(bool value) {
            this._DebugMode = value;
            return;
        }

        private void InitializePartSwapper(object sender, RoutedEventArgs e)
        {
            if (_DebugMode)
            {
                Trace.WriteLine("MainWindow: Entering InitializePartSwapper...");
            }


            this._PartswapperInstance = new PartSwapper2024(this._BlueprintFilePath,ref this.WindowSelfRef, ref this.CubeGridRendererTabControl, _ConfigOptions.GetOption(_SteamSEGameContentDirKey), _ConfigOptions.GetOption(_SteamWorkshopModDirOptKey),this._DebugMode);

            this._PartswapperInstance.SetDebugMode(_DebugMode);

            this._BlueprintDefinitionRef = this._PartswapperInstance.GetBlueprintDefinition();

            this._GridRenderer2024Ref = this._PartswapperInstance.GetGridRendererRef();

            this._GridCursorRef = this._PartswapperInstance.GetGridCursorRef();

            this._PartswapperInstance.PopulateTabControl(ref this.CubeGridRendererTabControl);

            this.SetupBindings();

            this.WindowSelfRef.SizeChanged -= OnMainWindow_SizeChanged;
            this.WindowSelfRef.SizeChanged += OnMainWindow_SizeChanged;

            this.zAxisSlider.MouseWheel -= OnZAxisSlider_MouseWheel;
            this.zAxisSlider.MouseWheel += OnZAxisSlider_MouseWheel;

            this.CubeGridRendererTabControl.MouseWheel -= OnZAxisSlider_MouseWheel;
            this.CubeGridRendererTabControl.MouseWheel += OnZAxisSlider_MouseWheel;

            this.CubeGridRendererTabControl.SelectionChanged -= OnCubeGridRendererTabControl_SelectionChanged;
            this.CubeGridRendererTabControl.SelectionChanged += OnCubeGridRendererTabControl_SelectionChanged;

            if (_DebugMode)
            {
                Trace.WriteLine("MainWindow: Exiting InitializePartSwapper...");
            }
        }

        private void OnInitializeGridRenderer(object sender, RoutedEventArgs e)
        {
            if (_DebugMode)
            {
                Trace.WriteLine("MainWindow: Entering OnInitializeGridRenderer...");
            }

            try
            {
                WindowSelfRef = this;

                if (this._BlueprintFilePath.Equals(""))
                {
                    this.WindowSelfRef.WindowState = WindowState.Normal;
                    this.WindowSelfRef.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                    this.WindowSelfRef.WindowStyle = WindowStyle.ThreeDBorderWindow;
                    this.WindowSelfRef.SizeToContent = SizeToContent.Manual;
                    this.WindowSelfRef.Visibility = Visibility.Visible;
                    this.WindowSelfRef.Height = SystemParameters.FullPrimaryScreenHeight / 2;
                    this.WindowSelfRef.Width = SystemParameters.FullPrimaryScreenWidth / 2;

                    this.SetupBindings();

                    this.StatusBlueprintPathValue.Text = "No Blueprint Loaded!";

                    this.zCursorMax = 0;

                    this._zCursor = 0;

                    this.CalculateSetTabControlDimensions(this.DesiredSize);
                    this.CalculateSetSliderHeight();

                    this.WindowSelfRef.InvalidateArrange();
                    this.WindowSelfRef.InvalidateVisual();
                    this.WindowSelfRef.UpdateLayout();

                    return;
                }
                else
                {
                    this.WindowSelfRef.WindowState = WindowState.Normal;
                    this.WindowSelfRef.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                    this.WindowSelfRef.WindowStyle = WindowStyle.ThreeDBorderWindow;
                    this.WindowSelfRef.SizeToContent = SizeToContent.Manual;
                    this.WindowSelfRef.Visibility = Visibility.Visible;
                    this.WindowSelfRef.Height = SystemParameters.FullPrimaryScreenHeight / 2;
                    this.WindowSelfRef.Width = SystemParameters.FullPrimaryScreenWidth / 2;
                   
                    this._PartswapperInstance = new PartSwapper2024(_BlueprintFilePath, ref WindowSelfRef, ref this.CubeGridRendererTabControl, _ConfigOptions.GetOption(_SteamSEGameContentDirKey), _ConfigOptions.GetOption(_SteamWorkshopModDirOptKey), this._DebugMode);

                    this._BlueprintDefinitionRef = this._PartswapperInstance.GetBlueprintDefinition();

                    this._GridRenderer2024Ref = this._PartswapperInstance.GetGridRendererRef();

                    this._GridCursorRef = this._PartswapperInstance.GetGridCursorRef();

                    this._PartswapperInstance.PopulateTabControl(ref CubeGridRendererTabControl);

                    this.CubeGridRendererTabControl.SelectionChanged -= OnCubeGridRendererTabControl_SelectionChanged;
                    this.CubeGridRendererTabControl.SelectionChanged += OnCubeGridRendererTabControl_SelectionChanged;

                    this._HoveredBPCell = null;

                    this._HoveredCubeblockDefinition = null;

                    this.SetupBindings();

                    this.StatusBlueprintPathValue.Text = this._BlueprintDefinitionRef.GetBlueprintName();

                    this.CalculateSetTabControlDimensions(this.DesiredSize);
                    this.SetupSlider();

                    this.StatusZLayerIndValue.Text = this._zCursor.ToString();
                }
                if (_DebugMode)
                {
                    Trace.WriteLine("MainWindow: Exiting OnInitializeGridRenderer...");
                }
            }
            catch (Exception ex)
            {
                if (_DebugMode)
                {
                    Trace.WriteLine("MainWindow: Exiting OnInitializeGridRenderer (Exception!)...");
                }
                this.StatusBlueprintPathValue.Text = "INITIALIZE LOAD FAILURE!";
                Trace.WriteLine(ex);
            }
        }

        private void OnGridRendererSkiaElement_DefaultPaintSurface(object? sender, SkiaSharp.Views.Desktop.SKPaintSurfaceEventArgs e)
        {
            
            SKPaint defaultPaint = new SKPaint();
            defaultPaint.Color = SKColors.DarkGoldenrod;
            
            e.Surface.Canvas.DrawText("Welcome to Picarl's Partswapper!", (float)this.Height, (float)this.Width, defaultPaint);
        }

        private void OnCubeGridRendererTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_DebugMode)
            {
                Trace.WriteLine("MainWindow: Entering OnCubeGridRendererTabControl_SelectionChanged...");
            }

            this._HoveredBPCell = null;

            this._HoveredCubeblockDefinition = null;

            this._BlueprintDefinitionRef = this._PartswapperInstance.GetBlueprintDefinition();

            this._GridRenderer2024Ref = this._PartswapperInstance.GetGridRendererRef();

            this._GridCursorRef = this._PartswapperInstance.GetGridCursorRef();
            
            this.StatusBlueprintPathValue.Text = this._BlueprintDefinitionRef.GetBlueprintName();

            this._zCursor = 0;

            this._GridRenderer2024Ref.ZCursor = this._zCursor;

            this.zCursorMax = this._GridRenderer2024Ref.MaxZCursorValue;

            this.StatusZLayerIndValue.Text = this._zCursor.ToString();

            this.SetupBindings();
            this.SetupSlider();

            //this.CalculateSetTabControlDimensions();

            if (_DebugMode)
            {
                Trace.WriteLine("MainWindow: Exiting OnCubeGridRendererTabControl_SelectionChanged...");
            }
        }

        private void OnStatusCursorWindowPositionValue_MouseMove(object sender, MouseEventArgs e)
        {
            if (_DebugMode)
            {
                Trace.WriteLine("MainWindow: Entering OnStatusCursorWindowPositionValue_MouseMove...");
            }
            try
            {
                if (this._PartswapperInstance == null)
                {
                    // StatusBar _WindowRef-Cursor Position
                    if (e == null)
                    {
                        this.StatusCursorWindowXPositionValue.Text = "NA";
                        this.StatusCursorWindowYPositionValue.Text = "NA";
                    }
                    else
                    {
                        this.StatusCursorWindowXPositionValue.Text = Convert.ToInt32(e.MouseDevice.GetPosition(this).X).ToString();
                        this.StatusCursorWindowYPositionValue.Text = Convert.ToInt32(e.MouseDevice.GetPosition(this).Y).ToString();
                    }


                    // StatusBar Skia-Cursor Position
                    this.StatusCursorSkiaXPositionValue.Text = "NA";
                    this.StatusCursorSkiaYPositionValue.Text = "NA";

                    // StatusBar HoveredBlock Information
                    this.StatusHoveredBlockValue.Text = "NA";
                }
                else
                {

                    // StatusBar _WindowRef-Cursor Position
                    if (e == null)
                    {
                        this.StatusCursorWindowXPositionValue.Text = "NA";
                        this.StatusCursorWindowYPositionValue.Text = "NA";
                    }
                    else
                    {
                        this.StatusCursorWindowXPositionValue.Text = Convert.ToInt32(e.MouseDevice.GetPosition(this).X).ToString();
                        this.StatusCursorWindowYPositionValue.Text = Convert.ToInt32(e.MouseDevice.GetPosition(this).Y).ToString();
                    }

                    // StatusBar Skia-Cursor Position
                    this.StatusCursorSkiaXPositionValue.Text = this._GridRenderer2024Ref.GetGridCursor().SKIACursorX.ToString();
                    this.StatusCursorSkiaYPositionValue.Text = this._GridRenderer2024Ref.GetGridCursor().SKIACursorY.ToString();

                    // StatusBar HoveredBlock Information

                    if (this._GridCursorRef != null)
                    {

                        this.StatusHoveredBlockLabel.Text = $"Hovered Grid X:{this._GridCursorRef.GetHoveredGridX()} Y:{this._GridCursorRef.GetHoveredGridY()}";

                        this._HoveredBPCell = this._GridCursorRef.GetHoveredBlueprintCell();

                        if (this._HoveredBPCell != null)
                        {
                            this._HoveredCubeblockDefinition = this._HoveredBPCell.GetCubeblockDefinition();

                            if (this._HoveredCubeblockDefinition != null)
                            {
                                this.StatusHoveredBlockValue.Text = _HoveredCubeblockDefinition.GetSubtypeName();
                            }
                            else
                            {
                                this.StatusHoveredBlockValue.Text = "Empty!";
                            }
                        }
                        else
                        {
                            this.StatusHoveredBlockValue.Text = "Out of blueprint bounds!";
                        }

                    }
                    else
                    {
                        this.StatusHoveredBlockValue.Text = "No Grid Cursor!";
                    }
                }
                if (_DebugMode)
                {
                    Trace.WriteLine("MainWindow: Exiting OnStatusCursorWindowPositionValue_MouseMove...");
                }
            }
            catch (Exception ex)
            {
                if (_DebugMode)
                {
                    Trace.WriteLine("MainWindow: Exiting OnStatusCursorWindowPositionValue_MouseMove (EXCEPTION!)...");
                }
                Trace.WriteLine(ex.ToString());
            }
        }

        private void OnSetDebugModeON(object sender, RoutedEventArgs e)
        {
            Trace.WriteLine($"MainWindow: Setting DebugMode ON!");
            this._DebugMode = true;

            if (this._PartswapperInstance != null)
            {
                this._PartswapperInstance.SetDebugMode(true);
                this._GridCursorRef.SetDebugMode(true);
            }
            else
            {
                Trace.WriteLine($"MainWindow: SetDebugModeON failed to enable DebugMode on PS Instance: PartSwapperInstance null!");
            }
        }

        private void OnSetDebugModeOFF(object sender, RoutedEventArgs e)
        {
            Trace.WriteLine($"MainWindow: Setting DebugMode OFF!");
            this._DebugMode = false;

            if (this._PartswapperInstance != null)
            {
                this._PartswapperInstance.SetDebugMode(false);
                this._GridCursorRef.SetDebugMode(false);
            }
            else
            {
                Trace.WriteLine($"MainWindow: SetDebugModeOFF failed to disable DebugMode on PS Instance: PartSwapperInstance null!");
            }
        }

        private void SetupBindings()
        {
            if (_DebugMode)
            {
                Trace.WriteLine("MainWindow: Entering SetupBindings...");
            }

            if (this._GridRenderer2024Ref != null)
            {
                #region SliderBindings
                Binding SliderBinding = new Binding("ToString");
                SliderBinding.Source = this._GridRenderer2024Ref.ZCursor;

                this.zAxisSlider.SetBinding(System.Windows.Controls.Primitives.RangeBase.ValueProperty, SliderBinding);
                #endregion

                #region CursorStatusbarBindings
                Binding CursorSkiaStatusBarXBinding = new Binding("X");
                CursorSkiaStatusBarXBinding.Source = this._GridRenderer2024Ref.GetGridCursor().GetCursorPoint();
                CursorSkiaStatusBarXBinding.NotifyOnSourceUpdated = true;
                CursorSkiaStatusBarXBinding.NotifyOnTargetUpdated = true;
                this.StatusCursorSkiaXPositionValue.SetBinding(TextBlock.TextProperty, CursorSkiaStatusBarXBinding);
                #endregion

                #region CursorStatusbarBindings
                Binding CursorSkiaStatusBarYBinding = new Binding("Y");
                CursorSkiaStatusBarYBinding.Source = this._GridRenderer2024Ref.GetGridCursor().GetCursorPoint();
                CursorSkiaStatusBarYBinding.NotifyOnSourceUpdated = true;
                CursorSkiaStatusBarYBinding.NotifyOnTargetUpdated = true;
                this.StatusCursorSkiaYPositionValue.SetBinding(TextBlock.TextProperty, CursorSkiaStatusBarYBinding);
                #endregion
            } else { Trace.WriteLine("SetupBindings: Null GridRenderer Detected! Unable to set bindings!"); }
            
            if (_DebugMode)
            {
                Trace.WriteLine("MainWindow: Exiting SetupBindings...");
            }
        }

        public PartSwapper2024 GetCurrentPartswapper2024Instance()
        {
            return this._PartswapperInstance;
        }

        public SKElement GetCurrentSKElement()
        {
            return this._PartswapperInstance.GetCurrentSKElement();
        }

        private void CalculateSetSliderHeight()
        {
            if (_DebugMode)
            {
                Trace.WriteLine("MainWindow: Entering CalculateSetSliderHeight...");
            }
            // Set height of slider
            this.zAxisSlider.Height = this.CubeGridRendererTabControl.Height;

            if (_DebugMode)
            {
                Trace.WriteLine($"MainWindow: CalculateSetSliderHeight -> this.zAxisSlider.Height: {this.zAxisSlider.Height}");
                Trace.WriteLine("MainWindow: Exiting CalculateSetSliderHeight...");
            }
        }

        private void CalculateSetTabControlDimensions(Size windowSize)
        {
            if (_DebugMode)
            {
                Trace.WriteLine("MainWindow: Entering CalculateSetTabControlDimensions...");
            }

            //this.CubeGridRendererTabControl.Height = windowSize.Height - this.MainWindow_Menu.ActualHeight - this.StatusBar.ActualHeight;
            //this.CubeGridRendererTabControl.Width = windowSize.Width - this.zAxisSlider.ActualWidth;
            StatusBar.UpdateLayout();
            MainWindow_Menu.UpdateLayout();

            double HeightCalc = windowSize.Height - (StatusBar.ActualHeight + MainWindow_Menu.ActualHeight + SystemParameters.WindowResizeBorderThickness.Top + SystemParameters.WindowNonClientFrameThickness.Top + SystemParameters.WindowResizeBorderThickness.Bottom + SystemParameters.WindowNonClientFrameThickness.Bottom);
            double WidthCalc = windowSize.Width * 0.98 - (zAxisSlider.ActualWidth);

            if (HeightCalc <= 0)
            {
                this.CubeGridRendererTabControl.Height = SystemParameters.FullPrimaryScreenHeight / 3;
            }
            else
            {
                this.CubeGridRendererTabControl.Height = HeightCalc;
            }

            if (WidthCalc <= 0)
            {
                this.CubeGridRendererTabControl.Width = SystemParameters.FullPrimaryScreenWidth / 3;
            }
            else
            {
                this.CubeGridRendererTabControl.Width = WidthCalc;
            }

            if (_DebugMode)
            {
                Trace.WriteLine($"MainWindow: CalculateSetTabControlDimensions ->\nCubeGridRendererTabControl.Height: {CubeGridRendererTabControl.Height}\nCubeGridRendererTabControl.Width {CubeGridRendererTabControl.Width}\n");
                Trace.WriteLine("MainWindow: Exiting CalculateSetTabControlDimensions...");
            }
        }

        public void SetupSlider()
        {
            if (_DebugMode)
            {
                Trace.WriteLine("Entering SetupSlider...");
            }
            // Setup ZCursor

            this.zCursorMax = this._GridRenderer2024Ref.MaxZCursorValue;

            this._zCursor = 0;
            if (this._PartswapperInstance != null)
            {
                this._PartswapperInstance.GetGridRendererRef().ZCursor = 0;
            }

            // Setup Slider
            this.zAxisSlider.Value = 0;
            this.zAxisSlider.Ticks = this._GridRenderer2024Ref.GetTicksDoubleCollection();
            this.zAxisSlider.TickFrequency = 1;
            this.zAxisSlider.IsSnapToTickEnabled = true;
            this.zAxisSlider.ToolTip = this._GridRenderer2024Ref.ZCursor;
            this.zAxisSlider.Maximum = this._GridRenderer2024Ref.MaxZCursorValue;

            this.CalculateSetSliderHeight();

            this.zAxisSlider.MouseWheel -= OnZAxisSlider_MouseWheel;
            this.zAxisSlider.MouseWheel += OnZAxisSlider_MouseWheel;

            this.CubeGridRendererTabControl.MouseWheel -= OnZAxisSlider_MouseWheel;
            this.CubeGridRendererTabControl.MouseWheel += OnZAxisSlider_MouseWheel;

            this.StatusZLayerIndValue.Text = this._zCursor.ToString();

            if (_DebugMode)
            {
                Trace.WriteLine("Exiting SetupSlider...");
            }
        }

        private void HeavyToLightConversion_Click(object sender, RoutedEventArgs e)
        {
            if (_PartswapperInstance == null)
            {
                MessageBox.Show("Partswapper not initialized! Please load a file first!");
                return;
            }

            this._PartswapperInstance.BackupShipXML();

            this.TransactionLogTemp = this._PartswapperInstance.PerformOperation("AutoArmor", new Dictionary<string, object> { { "Operation", "HeavyToLight" } });
            this._PartswapperInstance.MasterLogRef.Merge(TransactionLogTemp);
            
            this._PartswapperInstance.GetBlueprintDefinition().SaveFile(this._BlueprintFilePath);
            this._PartswapperInstance.DeleteSBC5File();
            this._PartswapperInstance.GetGridRendererRef().RedrawSkia();
            
        }

        private void LightToHeavyConversion_Click(object sender, RoutedEventArgs e)
        {
            if (_PartswapperInstance == null)
            {
                MessageBox.Show("Partswapper not initialized! Please load a file first!");
                return;
            }

            this._PartswapperInstance.BackupShipXML();

            this.TransactionLogTemp = this._PartswapperInstance.PerformOperation("AutoArmor", new Dictionary<string, object> { { "Operation", "LightToHeavy" } });
            this._PartswapperInstance.MasterLogRef.Merge(TransactionLogTemp);

            this._PartswapperInstance.GetBlueprintDefinition().SaveFile(this._BlueprintFilePath);
            this._PartswapperInstance.DeleteSBC5File();
            this._PartswapperInstance.GetGridRendererRef().RedrawSkia();
        }

        private void TritaniumToLight_Click(object sender, RoutedEventArgs e)
        {
            if (_PartswapperInstance == null)
            {
                MessageBox.Show("Partswapper not initialized! Please load a file first!");
                return;
            }

            this._PartswapperInstance.BackupShipXML();
            this.TransactionLogTemp = this._PartswapperInstance.PerformOperation("AutoArmor", new Dictionary<string, object> { { "Operation", "STC_From_Tritanium" } });
            this._PartswapperInstance.MasterLogRef.Merge(TransactionLogTemp);

            this._PartswapperInstance.GetBlueprintDefinition().SaveFile(this._BlueprintFilePath);
            this._PartswapperInstance.DeleteSBC5File();
            this._PartswapperInstance.GetGridRendererRef().RedrawSkia();
        }

        private void RemoveAllArmor_Click(object sender, RoutedEventArgs e)
        {
            if (_PartswapperInstance == null)
            {
                MessageBox.Show("Partswapper not initialized! Please load a file first!");
                return;
            }

            this._PartswapperInstance.BackupShipXML();

            this.TransactionLogTemp = this._PartswapperInstance.PerformOperation("RemoveTool", new Dictionary<string, object> { { "Operation", "Remove_Armor" }, { "Remove_Armor", "All" } });
            this._PartswapperInstance.MasterLogRef.Merge(TransactionLogTemp);

            this._PartswapperInstance.GetBlueprintDefinition().SaveFile(this._BlueprintFilePath);
            this._PartswapperInstance.DeleteSBC5File();
            this._PartswapperInstance.GetGridRendererRef().RedrawSkia();
        }

        private void RemoveAllHeavyArmor_Click(object sender, RoutedEventArgs e)
        {
            if (_PartswapperInstance == null)
            {
                MessageBox.Show("Partswapper not initialized! Please load a file first!");
                return;
            }

            this._PartswapperInstance.BackupShipXML();

            this.TransactionLogTemp = this._PartswapperInstance.PerformOperation("RemoveTool", new Dictionary<string, object> { { "Operation", "Remove_Armor" }, { "Remove_Armor", "Heavy" } });
            this._PartswapperInstance.MasterLogRef.Merge(TransactionLogTemp);

            this._PartswapperInstance.GetBlueprintDefinition().SaveFile(this._BlueprintFilePath);
            this._PartswapperInstance.DeleteSBC5File();
            this._PartswapperInstance.GetGridRendererRef().RedrawSkia();
        }

        private void RemoveAllLightArmor_Click(object sender, RoutedEventArgs e)
        {
            if (_PartswapperInstance == null)
            {
                MessageBox.Show("Partswapper not initialized! Please load a file first!");
                return;
            }

            this._PartswapperInstance.BackupShipXML();

            this.TransactionLogTemp = this._PartswapperInstance.PerformOperation("RemoveTool", new Dictionary<string, object> { { "Operation", "Remove_Armor" }, { "Remove_Armor", "Light" } });
            this._PartswapperInstance.MasterLogRef.Merge(TransactionLogTemp);

            this._PartswapperInstance.GetBlueprintDefinition().SaveFile(this._BlueprintFilePath);
            this._PartswapperInstance.DeleteSBC5File();
            this._PartswapperInstance.GetGridRendererRef().RedrawSkia();
        }

        private void RemoveSpecific_Click(object sender, RoutedEventArgs e)
        {
            if (_PartswapperInstance == null)
            {
                MessageBox.Show("Partswapper not initialized! Please load a file first!");
                return;
            }

            RemoveSpecificBlocksDialogue2024 remSpecificDialog = new RemoveSpecificBlocksDialogue2024(this._PartswapperInstance);
            remSpecificDialog.ShowDialog();
        }

        private void RemoveAllExcept_Click(object sender, RoutedEventArgs e)
        {
            if (_PartswapperInstance == null)
            {
                MessageBox.Show("Partswapper not initialized! Please load a file first!");
                return;
            }

            RemoveAllExceptBlocksDialogue remAllExceptDialog = new RemoveAllExceptBlocksDialogue(this._PartswapperInstance);
            remAllExceptDialog.ShowDialog();
        }

        private void RemoveAllExceptArmor_Click(object sender, RoutedEventArgs e)
        {
            if (_PartswapperInstance == null)
            {
                MessageBox.Show("Partswapper not initialized! Please load a file first!");
                return;
            }

            this._PartswapperInstance.BackupShipXML();
            this._PartswapperInstance.PerformOperation("RemoveTool", new Dictionary<string, object> { { "Operation", "RemoveAllExceptArmor" } });
            this._PartswapperInstance.GetBlueprintDefinition().SaveFile(this._BlueprintFilePath);
            this._PartswapperInstance.DeleteSBC5File();
            this._PartswapperInstance.GetGridRendererRef().RedrawSkia();
        }

        private void AutoTechMenu_Click(object sender, RoutedEventArgs e)
        {
            if (_PartswapperInstance == null)
            {
                MessageBox.Show("Partswapper not initialized! Please load a file first!");
                return;
            }

            AutoTechWindow ATWindow = new AutoTechWindow(this._PartswapperInstance);
            ATWindow.ShowDialog();
        }

        private void PartSwapViaCategory_Click(object sender, RoutedEventArgs e)
        {
            if (_PartswapperInstance == null)
            {
                MessageBox.Show("Partswapper not initialized! Please load a file first!");
                return;
            }

            PartswapOp2024Window partswapWindow = new PartswapOp2024Window(ref this._PartswapperInstance,ref this._PartswapperInstance.GetGridRendererRef().GetCurrentRenderEntry().Item1.GetBPSBCCubeGridDefinitionRef(), 
                this._PartswapperInstance.GetVanillaDataFolder(), 
                this._PartswapperInstance.GetWorkshopModFolder());
            partswapWindow.ShowDialog();
        }

        private void OnStatsBlocksAndComponents_Click(object sender, RoutedEventArgs e)
        {
            if(this._PartswapperInstance != null)
            {
                GridStats GridStatsWindow = new GridStats(this._PartswapperInstance, this._PartswapperInstance.GetGridRendererRef().GetCurrentRenderEntry().Item1.GetBPSBCCubeGridDefinitionRef());
                GridStatsWindow.Show();
            } else
            {
                MessageBox.Show("Error: Partswapper not initialized! Please load a grid before attempting to use the GridStats Window!");
            }

        }

        private void OnViewTransactionsLog_Click(object sender, RoutedEventArgs e)
        {
            if(this._PartswapperInstance != null)
            {
                TransactionWindow2024 TransactionLogWindow = new TransactionWindow2024(ref _PartswapperInstance.MasterLogRef,"PS Master Log");
                TransactionLogWindow.Show();
            } else
            {
                MessageBox.Show("Unable to load transaction log! No grid loaded!");
            }
        }

        private void UITransparentRendererCheckBox_ValueChanged(object sender, RoutedEventArgs e)
        {
            
        }
    }
}
