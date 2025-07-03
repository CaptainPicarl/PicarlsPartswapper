using PartSwapperXMLSE;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using SkiaSharp.Views.WPF;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using static PartSwapperGUI.PartSwapper2024.BlueprintCellGridManager;
using Window = System.Windows.Window;

namespace PartSwapperGUI.PartSwapper2024
{
    using RendererEntry = Tuple<CubeGridRenderCellGrid, GridGeometry, GridCursor, SKElement>;

    interface StaticRenderLayer
    {

        ref GridGeometry GridGeometry { get; }
        ref SKElement SkiaElementRef { get; }
        ref Window WPFWindowRef { get; }

        public SKSurface RenderSurface();
        public void UpdateGridGeometry(ref GridGeometry GridGeometry);

    }

    public static class RenderLayers
    {
        public class ForegroundGridLayer : StaticRenderLayer
        {
            bool _DebugMode = false;

            private GridGeometry _GridGeometryRef;
            private SKElement _SKElementRef;
            private Window _WindowRef;
            private SKImageInfo _ImageInfo;

            private SKPaint _PaintIter;
            private SKPath _PathIter;

            private SKColor _ColorIter;
            private SKPoint _SKPointIter;

            private SKSurface _SurfaceIterator;

            ref GridGeometry StaticRenderLayer.GridGeometry => ref _GridGeometryRef;

            ref SKElement StaticRenderLayer.SkiaElementRef => ref _SKElementRef;

            ref Window StaticRenderLayer.WPFWindowRef => ref _WindowRef;


            public ForegroundGridLayer(ref GridGeometry gridGeometryRef, ref SKElement skElementRef, ref Window windowRef)
            {
                this._GridGeometryRef = gridGeometryRef;
                this._SKElementRef = skElementRef;
                this._WindowRef = windowRef;
                this._ImageInfo = new SKImageInfo(Convert.ToInt32(_GridGeometryRef.GetSkiaCanvasWidth()), Convert.ToInt32(_GridGeometryRef.GetSkiaCanvasHeight()));
                this._SurfaceIterator = SKSurface.Create(_ImageInfo);
            }

            SKSurface StaticRenderLayer.RenderSurface()
            {
                return this._SurfaceIterator;
            }

            void StaticRenderLayer.UpdateGridGeometry(ref GridGeometry GridGeometry)
            {
                this._GridGeometryRef = GridGeometry;
                _ImageInfo = new SKImageInfo((int)GridGeometry.GetSkiaCanvasWidth(), (int)GridGeometry.GetSkiaCanvasHeight());
                DrawGridBackgroundSurface();
            }

            public void DrawGridBackgroundSurface()
            {
                float pixelWidth = this._GridGeometryRef.GetPixelWidth();
                float pixelHeight = this._GridGeometryRef.GetPixelHeight();
                float canvasWidth = this._GridGeometryRef.GetSkiaCanvasWidth();
                float canvasHeight = this._GridGeometryRef.GetSkiaCanvasHeight();

                int UIGridArrayBoundaryX = this._GridGeometryRef.GetUIGridArrayBoundaryX();
                int UIGridArrayBoundaryY = this._GridGeometryRef.GetUIGridArrayBoundaryY();

                this._SurfaceIterator = SKSurface.Create(_ImageInfo);

                this._ColorIter = SKColors.DarkSlateGray.WithAlpha(100);

                this._PaintIter = new SKPaint();
                this._PaintIter.Color = _ColorIter;
                this._PaintIter.Style = SKPaintStyle.Stroke;
                this._PaintIter.StrokeWidth = 1;
                this._PathIter = new SKPath();


                // DEBUG DRAW: Draws alternating purple/green for a sanity check.
                if (UIGridArrayBoundaryX < UIGridArrayBoundaryY)
                {
                    for (float i = 0; i < canvasWidth; i += pixelWidth)
                    {
                        for (float j = 0; j < canvasHeight; j += pixelHeight)
                        {
                            _SKPointIter = new SKPoint(i, j);

                            // Draw path lines
                            if (i < canvasWidth)
                            {
                                this._PathIter.MoveTo(i, j);
                                this._PathIter.LineTo(i + pixelWidth, j);
                                this._PathIter.MoveTo(i, j);
                                this._PathIter.LineTo(i, j + pixelHeight);
                            }


                            this._SurfaceIterator.Canvas.DrawPoint(_SKPointIter, _PaintIter);
                            this._SurfaceIterator.Canvas.DrawPath(_PathIter, _PaintIter);

                        }
                    }

                    this._SurfaceIterator.Canvas.Save();
                    this._SurfaceIterator.Canvas.Flush();
                }
                else
                {
                    for (float i = 0; i < canvasWidth; i += pixelWidth)
                    {
                        for (float j = 0; j < canvasHeight; j += pixelHeight)
                        {
                            _SKPointIter = new SKPoint(i, j);

                            // Draw path lines
                            if (i < canvasWidth)
                            {
                                this._PathIter.MoveTo(i, j);
                                this._PathIter.LineTo(i + pixelWidth, j);
                                this._PathIter.MoveTo(i, j);
                                this._PathIter.LineTo(i, j + pixelHeight);
                            }

                            this._PaintIter.Color = _ColorIter;

                            this._SurfaceIterator.Canvas.DrawPoint(_SKPointIter, _PaintIter);
                            this._SurfaceIterator.Canvas.DrawPath(_PathIter, _PaintIter);


                        }
                    }

                    this._SurfaceIterator.Canvas.Save();
                    this._SurfaceIterator.Canvas.Flush();
                }
            }
        }
    }

    public static class Tools
    {

        public static Stopwatch PerfStopwatch = new Stopwatch();

        public static SKColor SEColorHSVToSKColor(Vector3 colorMaskHSV)
        {
            float SATURATION_DELTA = 0.8f;

            float VALUE_DELTA = 0.55f;

            float VALUE_COLORIZE_DELTA = 0.1f;

            float hFloat = colorMaskHSV.X;
            float sFloat = colorMaskHSV.Y;
            float vFloat = colorMaskHSV.Z;

            try
            {
                // Winner! It doesn't seem entirely 1:1, but I think it's as good as we'll get for now.
                return SKColor.FromHsv(Math.Clamp(hFloat * 360f, 0f, 360f), Math.Clamp((sFloat + SATURATION_DELTA) * 100f, 0f, 100f), Math.Clamp((vFloat + VALUE_DELTA - VALUE_COLORIZE_DELTA) * 100f, 0f, 100f));
            }
            catch (Exception e)
            {
                throw new Exception("Failure to parse HSV color!\nError was:" + e);
            }
        }

        public static BlueprintSBC_CubeBlock EmptyBlock; // <--- maybe needed?
    }


    public class GridRenderer2024
    {
        private bool _DebugMode = false;

        private PartSwapper2024 _partswapperReference;

        private BlueprintSBC_BlueprintDefinition _SBCBlueprintDefinitionRef;

        private BlueprintCellGridManager _SkiaCellGridManager;

        private SKElement _SkiaElementRef;

        private Window _WindowRef;

        private Size _InitialWindowSize;

        private int _ZAxis3DCursor = 0;

        public float PixelHeight
        {
            get => _SkiaCellGridManager.GetCurrentRenderGrid().GetGridGeometryRef().GetPixelHeight();
            set => _SkiaCellGridManager.GetCurrentRenderGrid().GetGridGeometryRef().SetPixelHeight(value);
        }

        public float PixelWidth
        {
            get => _SkiaCellGridManager.GetCurrentRenderGrid().GetGridGeometryRef().GetPixelWidth();
            set => _SkiaCellGridManager.GetCurrentRenderGrid().GetGridGeometryRef().SetPixelWidth(value);
        }

        public int ZCursor
        {
            get => _ZAxis3DCursor;
            set
            {
                this._ZAxis3DCursor = value;
                this._SkiaCellGridManager.SetZCursor(value);
                this._SkiaElementRef = this._SkiaCellGridManager.GetCurrentSKElement();
            }
        }

        public int MaxZCursorValue
        {
            get => _SkiaCellGridManager.GetMaxZCursorValue();
        }

        public GridRenderer2024(ref PartSwapper2024 partswapperReference, ref BlueprintSBC_BlueprintDefinition blueprintDefinition, ref SKElement skElem, ref Window window)
        {
            if (_DebugMode)
            {
                Trace.WriteLine($"Entering GridRenderer2024 Constructor: GridRenderer2024(ref BlueprintSBC_BlueprintDefinition blueprintDefinition, ref SKElement skElem, ref Window window)");
            }

            this._partswapperReference = partswapperReference;

            // Standard reference assignments
            this._SBCBlueprintDefinitionRef = blueprintDefinition;
            this._SkiaElementRef = skElem;
            this._WindowRef = window;

            // Once the window size is setup: Setup the BPCellGridManager
            this._SkiaCellGridManager = new BlueprintCellGridManager(ref this._partswapperReference, ref _SBCBlueprintDefinitionRef, ref _WindowRef);

            this._SkiaElementRef = this._SkiaCellGridManager.GetCurrentSKElement();
            this._SkiaElementRef.Height = _SkiaCellGridManager.GetGridGeometry().GetSkiaElementHeight();
            this._SkiaElementRef.Width = _SkiaCellGridManager.GetGridGeometry().GetSkiaElementWidth();

            this._SkiaElementRef.UpdateLayout();
            this._SkiaElementRef.InvalidateVisual();

            this._WindowRef.UseLayoutRounding = false;
            this._WindowRef.UpdateLayout();
            this._WindowRef.InvalidateVisual();

            skElem = this._SkiaElementRef;

            if (_DebugMode)
            {
                Trace.WriteLine($"GridRenderer2024 Constructor: Assigning this._SBCBlueprintDefinitionRef to SKElement with hash: {this._SBCBlueprintDefinitionRef.GetHashCode()}");
                Trace.WriteLine($"GridRenderer2024 Constructor: Assigning this._SkiaElementRef to SKElement with hash: {this._SkiaElementRef.GetHashCode()}");
                Trace.WriteLine($"GridRenderer2024 Constructor: Assigning this._SkiaCellGridManager to BlueprintCellGridManager with hash: {this._SkiaCellGridManager.GetHashCode()}");
                Trace.WriteLine($"Exiting GridRenderer2024 Constructor: GridRenderer2024(ref BlueprintSBC_BlueprintDefinition blueprintDefinition, ref SKElement skElem, ref Window window)");
            }

            this.ZCursor = 0;
        }

        public ref RendererEntry GetCurrentRenderEntry()
        {
            return ref this._SkiaCellGridManager.GetCurrentRenderEntry();
        }

        public void SubmitWindowSizeUpdate(Size windowSize)
        {
            this._SkiaCellGridManager.SubmitWindowSizeUpdate(windowSize);

        }

        public bool PopulateTabControl(ref TabControl tabControl)
        {
            try
            {
                if (_DebugMode)
                {
                    Trace.WriteLine($"GridRenderer2024: Entering PopulateTabControl...");
                }
                this._SkiaCellGridManager.PopulateTabControl(ref tabControl);
                this._SkiaElementRef = _SkiaCellGridManager.GetCurrentSKElement();
                this.ZCursor = this._SkiaCellGridManager.GetZCursor();

                if (_DebugMode)
                {
                    Trace.WriteLine($"GridRenderer2024: Assigning this._SkiaElementRef to SKElement with hash {this._SkiaElementRef.GetHashCode()}");
                    Trace.WriteLine($"GridRenderer2024: Exiting PopulateTabControl...");
                }
                return true;
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"PopulateTabControl Exception:\n{ex.ToString()}");
                return false;
            }
        }

        public void RedrawSkia()
        {
            _SkiaElementRef.InvalidateVisual();
        }

        internal ref GridGeometry GetGridGeometry()
        {
            return ref _SkiaCellGridManager.GetGridGeometry();
        }

        internal ref GridCursor GetGridCursor()
        {
            return ref _SkiaCellGridManager.GetGridCursor();
        }

        public ref SKElement GetCurrentSKElement()
        {
            if (this._DebugMode)
            {
                Trace.WriteLine($"GridRenderer2024: Entering GetCurrentSKElement");

            }
            this._SkiaElementRef = this._SkiaCellGridManager.GetCurrentSKElement();

            if (this._DebugMode)
            {
                Trace.WriteLine($"GridRenderer2024: Returning reference to SKElement with hash: {this._SkiaElementRef.GetHashCode()}");

            }
            return ref this._SkiaElementRef;
        }
        public DoubleCollection GetTicksDoubleCollection()
        {
            DoubleCollection doubles = new DoubleCollection();
            int zBoundary = _SkiaCellGridManager.GetCurrentRenderGrid().GetGridGeometryRef().GetBPArrayBoundaryZ();
            for (double i = 0; i < zBoundary; i++)
            {

                doubles.Add(i);
            }

            return doubles;
        }

        public bool SetDebugMode(bool debugValue)
        {
            this._DebugMode = debugValue;
            this._SkiaCellGridManager.SetDebugMode(debugValue);
            return debugValue;
        }

        public void TESTRotateImage()
        {
            _SkiaCellGridManager.TESTRotateImage();
        }
    }
    public class GridCursor : INotifyPropertyChanged, StaticRenderLayer
    {
        private bool _DebugMode = false;

        private PartSwapper2024 _psReference;

        private GridGeometry _GridGeometryRef;

        private CubeGridRenderCellGrid _BPCellGrid;

        //BEGIN: Hovered Cell Detection Variables
        private BlueprintCell? _HoveredBlueprintCell = null;

        private float _HoveredBlueprintCellXEstimate;
        private float _HoveredBlueprintCellYEstimate;

        private BlueprintCell? _DetectedCellIterator = null;

        private float _WindowHeight;
        private float _WindowWidth;

        private float _CanvasHeight;
        private float _CanvasWidth;

        private float _PixelHeight;
        private float _PixelWidth;

        //END: Hovered Cell Detection Variables

        private SKElement _SKElementRef;

        private SKPoint _CursorPoint;

        private SKPaint _CursorPaint;
        private SKRect _CursorRect;
        private SKColor _CursorColor;
        private SKSurface _SKSurface;
        private SKImageInfo _SKImageInfo;

        private SKClipOperation _SKClipOperation = SKClipOperation.Difference;

        private int _CursorPixelSize = 10;

        private Window _Window;

        private float _WindowMousePositionX;
        private float _WindowMousePositionY;

        private float _WindowSkiaCoordDifferenceX;
        private float _WindowSkiaCoordDifferenceY;

        private float _WindowSkiaRatioX;
        private float _WindowSkiaRatioY;

        public float SKIACursorX
        {
            get => _CursorPoint.X;
            set
            {
                _CursorPoint.X = value;

                NotifyPropertyChanged(nameof(_CursorPoint.X));
            }
        }

        public float SKIACursorY
        {
            get => _CursorPoint.Y;
            set
            {
                _CursorPoint.Y = value;
                NotifyPropertyChanged(nameof(_CursorPoint.Y));
            }
        }

        private int _ZCursor = 0;

        ref GridGeometry StaticRenderLayer.GridGeometry => ref this._GridGeometryRef;

        ref SKElement StaticRenderLayer.SkiaElementRef => ref this._SKElementRef;

        ref Window StaticRenderLayer.WPFWindowRef => ref _Window;

        public event PropertyChangedEventHandler? PropertyChanged;

        // This method is called by the Set accessor of each property.  
        // The CallerMemberName attribute that is applied to the optional propertyName  
        // parameter causes the property name of the caller to be substituted as an argument.  
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public GridCursor(ref PartSwapper2024 psReference,ref CubeGridRenderCellGrid bpCellGrid, ref SKElement skElement, ref GridGeometry gridGeometry, ref Window window)
        {
            this._psReference = psReference;

            this._CursorPoint = new SKPoint(0, 0);

            this._CursorColor = SKColors.LightGoldenrodYellow;
            this._CursorPaint = new SKPaint();
            this._CursorPaint.Color = _CursorColor;
            this._CursorRect = new SKRect(0, 0, 10, 10);

            this._SKElementRef = skElement;
            this._GridGeometryRef = gridGeometry;
            this._BPCellGrid = bpCellGrid;

            this._ZCursor = _BPCellGrid.GetZCursor();

            this._WindowHeight = _GridGeometryRef.GetWPFWindowHeight();
            this._WindowWidth = _GridGeometryRef.GetWPFWindowWidth();

            this._CanvasHeight = _GridGeometryRef.GetSkiaCanvasHeight();
            this._CanvasWidth = _GridGeometryRef.GetSkiaCanvasWidth();

            this._PixelHeight = _GridGeometryRef.GetPixelHeight();
            this._PixelWidth = _GridGeometryRef.GetPixelWidth();

            this._WindowSkiaCoordDifferenceX = _GridGeometryRef.GetActualSkiaToWPFWidthDifference();
            this._WindowSkiaCoordDifferenceY = _GridGeometryRef.GetActualSkiaToWPFHeightDifference();

            this._WindowSkiaRatioX = this._CanvasWidth / (float)_SKElementRef.Width;
            this._WindowSkiaRatioY = this._CanvasHeight / (float)_SKElementRef.Height;

            this._SKImageInfo = new SKImageInfo((int)_CanvasWidth, (int)_CanvasHeight);
            this._SKSurface = SKSurface.Create(_SKImageInfo);

            // Draw initial image
            this._SKSurface.Canvas.Clear(this._CursorColor);
            this._SKSurface.Canvas.DrawRect(this.SKIACursorX, this.SKIACursorY, this._CursorPixelSize, this._CursorPixelSize, this._CursorPaint);
            this._SKSurface.Canvas.Save();
            this._SKSurface.Canvas.Flush();

            this.SKIACursorX = 0;
            this.SKIACursorY = 0;

            this._Window = window;
        }

        #region EventHandlers
        public void DrawCursor(SKPaintSurfaceEventArgs e)
        {
            this._CursorRect.Left = SKIACursorX;
            this._CursorRect.Top = SKIACursorY;
            this._CursorRect.Right = SKIACursorX + _CursorPixelSize;
            this._CursorRect.Bottom = SKIACursorY + _CursorPixelSize;

            this._SKSurface.Canvas.Clear(this._CursorColor);
            this._SKSurface.Canvas.DrawRect(this.SKIACursorX, this.SKIACursorY, this._CursorPixelSize, this._CursorPixelSize, this._CursorPaint);
            this._SKSurface.Canvas.Save();
            this._SKSurface.Canvas.Flush();
        }

        internal void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            BlueprintCell? detectedCell = this._HoveredBlueprintCell;
            CubeblockDefinitionViewer cubeViewer;
            BlueprintSBC_CubeBlock cubeBlock;

            if (detectedCell != null)
            {
                cubeBlock = detectedCell.GetCubeblockDefinition();

                if(cubeBlock != null)
                {
                    cubeViewer = new CubeblockDefinitionViewer(ref _psReference, cubeBlock);
                    cubeViewer.Show();
                } else
                {
                    MessageBox.Show("Cell detected - but no cubeBlock definition found! Cancelling CubeViewer!");
                }
            } else
            {
                MessageBox.Show("No Cell Detected! Cancelling CubeViewer!");
            }
        }

        internal void OnMouseLeaveEventHandler(object sender, MouseEventArgs e)
        {
            this._SKElementRef.Cursor = Cursors.Cross;
        }

        internal void OnMouseEnterEventHandler(object sender, MouseEventArgs e)
        {
            if (_DebugMode)
            {
                Trace.WriteLine("GridCursor: Entering OnMouseEnterEventHandler...");
            }

            this._SKElementRef.Cursor = Cursors.None;

            this._WindowMousePositionX = (float)e.MouseDevice.GetPosition(_Window).X;
            this._WindowMousePositionY = (float)e.MouseDevice.GetPosition(_Window).Y;

            this._WindowSkiaCoordDifferenceX = _GridGeometryRef.GetActualSkiaToWPFWidthDifference();
            this._WindowSkiaCoordDifferenceY = _GridGeometryRef.GetActualSkiaToWPFHeightDifference();

            this._WindowSkiaRatioX = this._CanvasWidth / (float)_SKElementRef.Width;
            this._WindowSkiaRatioY = _CanvasHeight / (float)_SKElementRef.Height;

            this.SKIACursorX = (float)(e.MouseDevice.GetPosition(_SKElementRef)).X * _WindowSkiaRatioX;
            this.SKIACursorY = (float)(e.MouseDevice.GetPosition(_SKElementRef)).Y * _WindowSkiaRatioY;

            this._CursorPoint.X = SKIACursorX;
            this._CursorPoint.Y = SKIACursorY;

            this._HoveredBlueprintCell = this.DetermineHoveredBlueprintCell();

            this._CursorRect = new SKRect(_CursorPoint.X, _CursorPoint.Y, _CursorPoint.X + _CursorPixelSize, _CursorPoint.Y + _CursorPixelSize);

            this._SKSurface.Canvas.Clear(this._CursorColor);
            //this._SKSurface.Canvas.DrawRect(this.SKIACursorX, this.SKIACursorY, this._CursorPixelSize, this._CursorPixelSize, this._CursorPaint);
            this._SKSurface.Canvas.DrawPoint(this.SKIACursorX, this.SKIACursorY, this._CursorPaint);
            this._SKSurface.Canvas.Save();
            this._SKSurface.Canvas.Flush();

            NotifyPropertyChanged(nameof(_CursorPoint));
            NotifyPropertyChanged(nameof(_CursorPoint.X));
            NotifyPropertyChanged(nameof(_CursorPoint.Y));

            this._SKElementRef.InvalidateVisual();

            if (_DebugMode)
            {
                Trace.WriteLine("GridCursor: Exiting OnMouseEnterEventHandler...");
            }
        }

        internal void OnMouseMoveEventHandler(object sender, MouseEventArgs e)
        {
            if (_DebugMode)
            {
                Trace.WriteLine("GridCursor: Entering OnMouseMoveEventHandler...");
            }

            this._WindowMousePositionX = (float)e.MouseDevice.GetPosition(_Window).X;
            this._WindowMousePositionY = (float)e.MouseDevice.GetPosition(_Window).Y;

            this._WindowSkiaCoordDifferenceX = _GridGeometryRef.GetActualSkiaToWPFWidthDifference();
            this._WindowSkiaCoordDifferenceY = _GridGeometryRef.GetActualSkiaToWPFHeightDifference();

            this._WindowSkiaRatioX = this._CanvasWidth / (float)_SKElementRef.Width;
            this._WindowSkiaRatioY = _CanvasHeight / (float)_SKElementRef.Height;

            this.SKIACursorX = (float)(e.MouseDevice.GetPosition(_SKElementRef)).X * _WindowSkiaRatioX;
            this.SKIACursorY = (float)(e.MouseDevice.GetPosition(_SKElementRef)).Y * _WindowSkiaRatioY;

            this._CursorPoint.X = SKIACursorX;
            this._CursorPoint.Y = SKIACursorY;

            this._HoveredBlueprintCell = this.DetermineHoveredBlueprintCell();

            this._CursorRect = new SKRect(_CursorPoint.X, _CursorPoint.Y, _CursorPoint.X + _CursorPixelSize, _CursorPoint.Y + _CursorPixelSize);

            this._SKSurface.Canvas.Clear(this._CursorColor);
            //this._SKSurface.Canvas.DrawRect(this.SKIACursorX, this.SKIACursorY, this._CursorPixelSize, this._CursorPixelSize, this._CursorPaint);
            this._CursorPaint.Color = SKColors.Pink;
            this._SKSurface.Canvas.DrawPoint(this.SKIACursorX, this.SKIACursorY, this._CursorPaint);
            this._SKSurface.Canvas.Save();
            this._SKSurface.Canvas.Flush();

            NotifyPropertyChanged(nameof(_CursorPoint));
            NotifyPropertyChanged(nameof(_CursorPoint.X));
            NotifyPropertyChanged(nameof(_CursorPoint.Y));

            this._SKElementRef.InvalidateVisual();

            if (_DebugMode)
            {
                Trace.WriteLine("GridCursor: Exiting OnMouseMoveEventHandler...");
            }
        }

        #endregion

        public BlueprintCell? GetHoveredBlueprintCell()
        {
            return this._HoveredBlueprintCell;
        }

        private BlueprintCell? DetermineHoveredBlueprintCell()
        {
            this._ZCursor = this._BPCellGrid.GetZCursor();
            float SkiaPixelRatioFloorWidth = MathF.Floor((SKIACursorX) / (_PixelWidth));
            float SkiaPixelRatioFloorHeight = MathF.Floor((SKIACursorY) / (_PixelHeight));

            //TODO: Fix this.
            // If X is the 'long' dimension...
            if (this._GridGeometryRef.GetBPArrayDimensionLengthX() > this._GridGeometryRef.GetBPArrayDimensionLengthY())
            {
                if (_DebugMode)
                {
                    Trace.WriteLine($"GridCursor accessing the following array coords: X:{this._HoveredBlueprintCellXEstimate},Y:{this._HoveredBlueprintCellYEstimate}");
                }

                this._HoveredBlueprintCellXEstimate = SkiaPixelRatioFloorWidth;
                this._HoveredBlueprintCellYEstimate = SkiaPixelRatioFloorHeight;

                try
                {
                    this._DetectedCellIterator = this._BPCellGrid.GetCellRefAtCoord((int)_HoveredBlueprintCellXEstimate, (int)_HoveredBlueprintCellYEstimate, this._ZCursor);
                }
                catch (ArgumentException)
                {
                    if (_DebugMode)
                    {
                        Trace.WriteLine("GridCursor -> DetermineHoveredBlueprintCell: Invalid argument to GetCellRefAtCoord! ...Likely just an empty cell, though? Maybe?");
                    }
                }
            }
            else
            {
                if (_DebugMode)
                {
                    Trace.WriteLine($"GridCursor accessing the following array coords: X:{this._HoveredBlueprintCellXEstimate},Y:{this._HoveredBlueprintCellYEstimate}");
                }

                this._HoveredBlueprintCellXEstimate = SkiaPixelRatioFloorHeight;
                this._HoveredBlueprintCellYEstimate = SkiaPixelRatioFloorWidth;

                try
                {
                    this._DetectedCellIterator = this._BPCellGrid.GetCellRefAtCoord((int)_HoveredBlueprintCellXEstimate, (int)_HoveredBlueprintCellYEstimate, this._ZCursor);
                } catch(ArgumentException)
                {
                    if (_DebugMode)
                    {
                        Trace.WriteLine("GridCursor -> DetermineHoveredBlueprintCell: Invalid argument to GetCellRefAtCoord! ...Likely just an empty cell, though? Maybe?");
                    }
                }


            }
            return _DetectedCellIterator;
        }

        public void SetDebugMode(bool debugMode)
        {
            this._DebugMode = debugMode;
        }

        public void SetCursorPoint(SKPoint cursorPoint)
        {
            _CursorPoint.X = cursorPoint.X;
            _CursorPoint.Y = cursorPoint.Y;
        }

        public void SetCursorPoint(int x, int y)
        {
            _CursorPoint.X = x;
            _CursorPoint.Y = y;
        }

        public float GetHoveredGridX()
        {
            return this._HoveredBlueprintCellXEstimate;
        }

        public float GetHoveredGridY()
        {
            return this._HoveredBlueprintCellYEstimate;
        }

        public SKPaint GetCursorPaint()
        {
            return _CursorPaint;
        }

        public SKPoint GetCursorPoint()
        {
            return _CursorPoint;
        }

        public void SetCursorRect(SKRect cursorRect)
        {
            _CursorRect = cursorRect;
        }
        public SKRect GetCursorRect()
        {
            return _CursorRect;
        }

        void StaticRenderLayer.UpdateGridGeometry(ref GridGeometry GridGeometry)
        {
            if (_DebugMode)
            {
                Trace.WriteLine("StaticRenderLayer: Entering UpdateGridGeometry...");
            }
            this._GridGeometryRef = GridGeometry;

            this._SKImageInfo = new SKImageInfo((int)this._GridGeometryRef.GetSkiaCanvasWidth(), (int)this._GridGeometryRef.GetSkiaCanvasHeight());
            this._SKSurface = SKSurface.Create(_SKImageInfo);

            this._CanvasHeight = (int)_GridGeometryRef.GetSkiaCanvasHeight();
            this._CanvasWidth = (int)_GridGeometryRef.GetSkiaCanvasWidth();

            this._PixelHeight = (int)_GridGeometryRef.GetPixelHeight();
            this._PixelWidth = (int)_GridGeometryRef.GetPixelWidth();

            this._WindowSkiaCoordDifferenceX = _GridGeometryRef.GetActualSkiaToWPFWidthDifference();
            this._WindowSkiaCoordDifferenceY = _GridGeometryRef.GetActualSkiaToWPFHeightDifference();

            this._WindowSkiaRatioX = this._CanvasWidth / (float)_SKElementRef.Width;
            this._WindowSkiaRatioY = _CanvasHeight / (float)_SKElementRef.Height;

            this._SKSurface.Canvas.Clear();
            this._SKSurface.Canvas.DrawRect(this.SKIACursorX, this.SKIACursorY, this._CursorPixelSize, this._CursorPixelSize, this._CursorPaint);
            this._SKSurface.Canvas.Save();
            this._SKSurface.Canvas.Flush();

            if (_DebugMode)
            {
                Trace.WriteLine("StaticRenderLayer: Exiting UpdateGridGeometry...");
            }
        }

        SKSurface StaticRenderLayer.RenderSurface()
        {
            if (_DebugMode)
            {
                Trace.WriteLine("StaticRenderLayer: Entering RenderSurface...");
            }
            this._SKSurface.Canvas.Clear();
            this._SKSurface.Canvas.DrawRect(this.SKIACursorX, this.SKIACursorY, this._CursorPixelSize, this._CursorPixelSize, this._CursorPaint);
            this._SKSurface.Canvas.Save();
            this._SKSurface.Canvas.Flush();
            if (_DebugMode)
            {
                Trace.WriteLine("StaticRenderLayer: Exiting RenderSurface...");
            }
            return _SKSurface;
        }
    }

    public class BlueprintCell
    {
        private bool _DebugMode = false;

        BlueprintSBC_CubeBlock? CubeblockDefinitionInstance;
        GridGeometry GridGeometryRef;

        Vector3 Array3DCoordinate;

        // _BlueprintDefinitionRef Coordinates store where the block is in the grid BP. AKA: "Min".
        SKPoint BP2DCellCoordinate;
        SKPoint3 BP3DCellCoordinate;

        // UI Coordinates store where the block is in the image.
        SKPoint UI2DCellCoordinate;
        SKPoint3 UI3DCellCoordinate;
        SKRectI UIBaseRect;

        // Tuple explanation:
        // string -> LayerName
        // SKRectI -> Dimensions of Rectangle to draw
        // SKPaint -> Paint to draw
        Queue<Tuple<string, SKRectI, SKPaint>> UIRectangles = new Queue<Tuple<string, SKRectI, SKPaint>>();

        SKColor UIPaintColor;
        SKPaint UIPaint;

        int RectangleSize = 1;

        bool isDummyCell = true;

        // 'dummy' cell definition - this represents empty 'pixels' in our renderer!
        public BlueprintCell(Vector3 arrayCoordinate, ref GridGeometry gridGeometry)
        {
            Array3DCoordinate = arrayCoordinate;

            GridGeometryRef = gridGeometry;

            BP3DCellCoordinate = Load3DDummyCellCoordinate();
            BP2DCellCoordinate = Load2DDummyCellCoordinate();

            UI3DCellCoordinate = LoadUI3DCellCoordinate();
            UI2DCellCoordinate = LoadUI2DCellCoordinate();

            UIPaintColor = SKColors.DarkOrange;
            UIPaint = LoadPaint(UIPaintColor);

            UIBaseRect = LoadBaseUIRect();

            UIRectangles.Enqueue(new Tuple<string, SKRectI, SKPaint>("BaseRect",UIBaseRect,UIPaint));

            isDummyCell = true;
        }

        public BlueprintCell(BlueprintSBC_CubeBlock cubeblockDefinition, ref GridGeometry gridGeometry)
        {

            CubeblockDefinitionInstance = cubeblockDefinition;
            GridGeometryRef = gridGeometry;

            Array3DCoordinate = LoadArrayCoordinate();

            BP3DCellCoordinate = LoadBP3DCellCoordinate();
            BP2DCellCoordinate = LoadBP2DCellCoordinate();

            UI3DCellCoordinate = LoadUI3DCellCoordinate();
            UI2DCellCoordinate = LoadUI2DCellCoordinate();

            UIPaintColor = Tools.SEColorHSVToSKColor(CubeblockDefinitionInstance.GetColorHSVVector());
            UIPaint = LoadPaint(UIPaintColor);

            UIBaseRect = LoadBaseUIRect();

            UIRectangles.Enqueue(new Tuple<string, SKRectI, SKPaint>("BaseRect", UIBaseRect, UIPaint));

            isDummyCell = false;
        }

        public void UpdateGeometry(ref GridGeometry geometry)
        {
            this.GridGeometryRef = geometry;

            this.UIRectangles = new Queue<Tuple<string, SKRectI, SKPaint>>();

            if (isDummyCell)
            {
                this.BP3DCellCoordinate = Load3DDummyCellCoordinate();
                this.BP2DCellCoordinate = Load2DDummyCellCoordinate();

                this.UI3DCellCoordinate = LoadUI3DCellCoordinate();
                this.UI2DCellCoordinate = LoadUI2DCellCoordinate();

                this.UIPaintColor = SKColors.DarkOrange;
                this.UIPaint = LoadPaint(UIPaintColor);

                this.UIBaseRect = LoadBaseUIRect();

                this.UIRectangles.Enqueue(new Tuple<string, SKRectI, SKPaint>("BaseRect", UIBaseRect, UIPaint));

                this.isDummyCell = true;
            }
            else
            {
                this.Array3DCoordinate = LoadArrayCoordinate();

                this.BP3DCellCoordinate = LoadBP3DCellCoordinate();
                this.BP2DCellCoordinate = LoadBP2DCellCoordinate();

                this.UI3DCellCoordinate = LoadUI3DCellCoordinate();
                this.UI2DCellCoordinate = LoadUI2DCellCoordinate();

                this.UIPaintColor = Tools.SEColorHSVToSKColor(CubeblockDefinitionInstance.GetColorHSVVector());
                this.UIPaint = LoadPaint(UIPaintColor);

                this.UIBaseRect = LoadBaseUIRect();

                this.UIRectangles.Enqueue(new Tuple<string, SKRectI, SKPaint>("BaseRect", UIBaseRect, UIPaint));

                this.isDummyCell = false;
            }
        }

        public ref BlueprintSBC_CubeBlock? GetCubeblockDefinition()
        {
            return ref this.CubeblockDefinitionInstance;
        }
        public Queue<Tuple<string, SKRectI,SKPaint>> GetUIRectangles()
        {
            return UIRectangles;
        }

        public void AddUIRectangle(Tuple<string, SKRectI, SKPaint> UIRectangleEntry)
        {
            this.UIRectangles.Enqueue(UIRectangleEntry);
        }

        public void SetUIRectangles(Queue<Tuple<string, SKRectI, SKPaint>> rectangles)
        {
            this.UIRectangles = rectangles;
        }

        public void Delete()
        {
            this.CubeblockDefinitionInstance.Delete();
            this.isDummyCell = true;
            this.UIRectangles.Clear();
            this.UIBaseRect = this.LoadBaseUIRect();
            this.UIPaint = this.LoadPaint(UIPaintColor);
        }
        public SKPaint LoadPaint(SKColor paintColor)
        {
            SKPaint sKPaint = new SKPaint();
            sKPaint.Color = paintColor;
            return sKPaint;
        }


        public SKRectI LoadBaseUIRect()
        {
            SKRectI newRect;

            int recCoordinateLeft;
            int recCoordinateRight;
            int recCoordinateTop;
            int recCoordinateBottom;

            recCoordinateLeft = Convert.ToInt32(UI3DCellCoordinate.X + GridGeometryRef.GetPixelLeftOffset());
            recCoordinateRight = Convert.ToInt32(UI3DCellCoordinate.X + GridGeometryRef.GetPixelRightOffset());
            recCoordinateTop = Convert.ToInt32(UI3DCellCoordinate.Y + GridGeometryRef.GetPixelTopOffset());
            recCoordinateBottom = Convert.ToInt32(UI3DCellCoordinate.Y + GridGeometryRef.GetPixelBottomOffset());

            newRect = new SKRectI(recCoordinateLeft, recCoordinateTop, recCoordinateRight, recCoordinateBottom);

            return newRect;
        }

        public void RotateUICoordsSwapXY()
        {
            Vector3 v3Representation;

            v3Representation = new Vector3(UI3DCellCoordinate.X, UI3DCellCoordinate.Y, UI3DCellCoordinate.Z);

            this.BP3DCellCoordinate = new SKPoint3(v3Representation.Y, v3Representation.X, v3Representation.Z);
            LoadBaseUIRect();
        }

        public bool IsDummyCell()
        {
            return isDummyCell;
        }

        public void SetUIPaint(SKPaint paint)
        {
            this.UIPaintColor = paint.Color;
            this.UIPaint = paint;
        }

        public SKPaint GetUIPaint()
        {
            return UIPaint;
        }

        public SKRectI GetUIRect()
        {
            return UIBaseRect;
        }

        public SKPoint GetUI2DCellCoordinateCenter()
        {
            return UI2DCellCoordinate;
        }

        public SKPoint3 GetUI3DCellCoordinateCenter()
        {
            return UI3DCellCoordinate;
        }

        public SKPoint GetBP2DCoordinateCenter()
        {
            return BP2DCellCoordinate;
        }

        public SKPoint3 GetBP3DCoordinateCenter()
        {
            return BP3DCellCoordinate;
        }
        public Vector3 GetArrayCoordinate()
        {
            return Array3DCoordinate;
        }
        private Vector3 LoadArrayCoordinate()
        {

            Vector3 ArrayCoordinates;

            ArrayCoordinates = CubeblockDefinitionInstance.GetMinVector();
            ArrayCoordinates = new Vector3(ArrayCoordinates.X + GridGeometryRef.GetGridOffSetX(), ArrayCoordinates.Y + GridGeometryRef.GetGridOffSetY(), ArrayCoordinates.Z + GridGeometryRef.GetGridOffSetZ());

            return ArrayCoordinates;
        }
        private SKPoint3 LoadUI3DCellCoordinate()
        {
            SKPoint3 SKPoint3;

            float xCoord;
            float yCoord;
            float zCoord;

            SKPoint3 cubeblock3DCoords;

            cubeblock3DCoords = BP3DCellCoordinate;

            // TODO: Check if this is correct - possibly wrong!
            if (GridGeometryRef.GetBPArrayDimensionLengthX() > GridGeometryRef.GetBPArrayDimensionLengthY())
            {
                xCoord = cubeblock3DCoords.X * GridGeometryRef.GetPixelWidth() + GridGeometryRef.GetPixelWidth();
                yCoord = cubeblock3DCoords.Y * GridGeometryRef.GetPixelHeight() + GridGeometryRef.GetPixelHeight();
            }
            else
            {
                yCoord = cubeblock3DCoords.X * GridGeometryRef.GetPixelHeight() + GridGeometryRef.GetPixelHeight();
                xCoord = cubeblock3DCoords.Y * GridGeometryRef.GetPixelWidth() + GridGeometryRef.GetPixelWidth();
            }

            zCoord = cubeblock3DCoords.Z;

            SKPoint3 = new SKPoint3(xCoord, yCoord, zCoord);

            return SKPoint3;
        }

        private SKPoint LoadUI2DCellCoordinate()
        {
            SKPoint SKPoint;

            float xCoord;
            float yCoord;

            SKPoint cubeblock3DCoords;

            cubeblock3DCoords = BP2DCellCoordinate;

            if (GridGeometryRef.GetBPArrayDimensionLengthX() > GridGeometryRef.GetBPArrayDimensionLengthY())
            {
                xCoord = cubeblock3DCoords.X * GridGeometryRef.GetPixelWidth() + GridGeometryRef.GetPixelWidth();
                yCoord = cubeblock3DCoords.Y * GridGeometryRef.GetPixelHeight() + GridGeometryRef.GetPixelHeight();
            }
            else
            {
                yCoord = cubeblock3DCoords.X * GridGeometryRef.GetPixelHeight() + GridGeometryRef.GetPixelHeight();
                xCoord = cubeblock3DCoords.Y * GridGeometryRef.GetPixelWidth() + GridGeometryRef.GetPixelWidth();
            }

            SKPoint = new SKPoint(xCoord, yCoord);

            return SKPoint;
        }

        private SKPoint Load2DDummyCellCoordinate()
        {
            SKPoint SKPoint;

            float xCoord;
            float yCoord;

            Vector3 ArrayCoords;

            ArrayCoords = Array3DCoordinate;

            xCoord = ArrayCoords.X;
            yCoord = ArrayCoords.Y;

            SKPoint = new SKPoint(xCoord, yCoord);

            return SKPoint;
        }

        private SKPoint3 Load3DDummyCellCoordinate()
        {
            SKPoint3 SKPoint3;

            float xCoord;
            float yCoord;
            float zCoord;

            Vector3 ArrayCoords;

            ArrayCoords = Array3DCoordinate;

            xCoord = ArrayCoords.X;
            yCoord = ArrayCoords.Y;
            zCoord = ArrayCoords.Z;

            SKPoint3 = new SKPoint3(xCoord, yCoord, zCoord);

            return SKPoint3;
        }

        private SKPoint3 LoadBP3DCellCoordinate()
        {
            SKPoint3 SKPoint3;

            float xCoord;
            float yCoord;
            float zCoord;

            Vector3 cubeblockMin;

            cubeblockMin = CubeblockDefinitionInstance.GetMinVector();

            xCoord = cubeblockMin.X + GridGeometryRef.GetGridOffSetX();
            yCoord = cubeblockMin.Y + GridGeometryRef.GetGridOffSetY();
            zCoord = cubeblockMin.Z + GridGeometryRef.GetGridOffSetZ();

            SKPoint3 = new SKPoint3(xCoord, yCoord, zCoord);

            return SKPoint3;
        }

        private SKPoint LoadBP2DCellCoordinate()
        {
            SKPoint SKPoint;

            float xCoord;
            float yCoord;

            Vector3 cubeblockMin;

            cubeblockMin = CubeblockDefinitionInstance.GetMinVector();

            xCoord = cubeblockMin.X + GridGeometryRef.GetGridOffSetX();
            yCoord = cubeblockMin.Y + GridGeometryRef.GetGridOffSetY();

            SKPoint = new SKPoint(xCoord, yCoord);

            return SKPoint;
        }
    }

    public class BlueprintCellGridManager
    {
        private bool _DebugMode = false;

        private PartSwapper2024 _partswapperRef;

        // Default Colors
        private SKPaint hotPinkPaint = new SKPaint();
        private SKPaint redPaint = new SKPaint();
        private SKPaint bluePaint = new SKPaint();
        private SKPaint greenPaint = new SKPaint();
        private SKPaint yellowPaint = new SKPaint();

        private SKColor skRedColor = SKColors.Red;
        private SKColor skBlueColor = SKColors.Blue;
        private SKColor skGreenColor = SKColors.Green;
        private SKColor skYellowColor = SKColors.Yellow;
        private SKColor skHotPinkColor = SKColors.HotPink;

        private SKColor UIDefaultGridColor = SKColors.CadetBlue;

        private GridCursor GridCursorInstance;
        private GridGeometry GridGeometryMasterInstance;
        private CubeGridRenderCellGrid CubegridRenderCellGridInstance;
        private RendererEntry RenderEntryInstance;
        private List<RendererEntry> RenderEntryList;

        private BlueprintSBC_CubeGrid[] SBC_CubeGridsIterArr;

        private SKSurface[]? SkiaSurfaceArrayInstance;

        private SKElement SKElementRef;
        private BlueprintSBC_BlueprintDefinition SBCBlueprintDefinitionRef;
        private SKSurface SKSurfaceIterator;
        private SKCanvas SKCanvasIterator;

        // WPF References
        private Window _WindowRef;
        private TabControl _TabControl;

        private Dictionary<string, StaticRenderLayer> StaticLayersCollection = new Dictionary<string, StaticRenderLayer>();
        private Dictionary<string, SKSurface> RenderedStaticSurfaceCollection = new Dictionary<string, SKSurface>();

        private int XLayerCenterIndex = 0;
        private int YLayerCenterIndex = 0;
        private int ZLayerCenterIndex = 0;

        private int zCursor = 0;


        public BlueprintCellGridManager(ref PartSwapper2024 partswapperRef, ref BlueprintSBC_BlueprintDefinition blueprintDefinition, ref Window window)
        {

            if (_DebugMode)
            {
                Trace.WriteLine("BlueprintCellGridManager: Entering Constructor BlueprintCellGridManager(ref BlueprintSBC_BlueprintDefinition blueprintDefinition, ref Window window)...");
            }

            this._partswapperRef = partswapperRef;

            // Set this zCursor to zero
            this.zCursor = 0;

            this.SBCBlueprintDefinitionRef = blueprintDefinition;

            this._WindowRef = window;

            this.hotPinkPaint.Color = skHotPinkColor;
            this.redPaint.Color = skRedColor;
            this.bluePaint.Color = skBlueColor;
            this.greenPaint.Color = skGreenColor;
            this.yellowPaint.Color = skYellowColor;


            // Populate the RendererEntryList with RendererEntrys
            this.RenderEntryList = new List<RendererEntry>(blueprintDefinition.GetCubegrids().Count);
            //Gotta convert to array for referencing purposes
            this.SBC_CubeGridsIterArr = blueprintDefinition.GetCubegrids().ToArray();

            // Iterate through the cubegrids and create our tuples representing a particular rendered grid.

            for (int i = 0; i < SBC_CubeGridsIterArr.Count(); i++)
            {
                this.SKElementRef = new SKElement();

                this.SKElementRef.BeginInit();

                this.GridGeometryMasterInstance = new GridGeometry(SBC_CubeGridsIterArr[i], ref SKElementRef, ref _WindowRef);
                this.CubegridRenderCellGridInstance = new CubeGridRenderCellGrid(ref this.GridGeometryMasterInstance, ref SBC_CubeGridsIterArr[i]);
                this.GridCursorInstance = new GridCursor(ref this._partswapperRef, ref CubegridRenderCellGridInstance, ref SKElementRef, ref GridGeometryMasterInstance, ref _WindowRef);

                this.SKElementRef.Height = this.GridGeometryMasterInstance.GetSkiaElementHeight();
                this.SKElementRef.Width = this.GridGeometryMasterInstance.GetSkiaElementWidth();

                this.CubegridRenderCellGridInstance.RegenerateCells();

                this.SKElementRef.Name = this.SBC_CubeGridsIterArr[i].GetDisplayName().Replace(" ", "").Replace("-","") + "_SKElement";

                this.SKElementRef.SizeChanged -= OnSKElementSizeChangedEventHandler;
                this.SKElementRef.SizeChanged += OnSKElementSizeChangedEventHandler;


                //this.SKElementRef.Loaded -= OnLoadedEventHandler;
                //this.SKElementRef.Loaded += OnLoadedEventHandler;


                this.SKElementRef.PaintSurface -= OnPaintSurfaceEventHandler;
                this.SKElementRef.PaintSurface += OnPaintSurfaceEventHandler;

                this.SKElementRef.MouseEnter -= GridCursorInstance.OnMouseEnterEventHandler;
                this.SKElementRef.MouseEnter += GridCursorInstance.OnMouseEnterEventHandler;

                this.SKElementRef.MouseLeave -= GridCursorInstance.OnMouseLeaveEventHandler;
                this.SKElementRef.MouseLeave += GridCursorInstance.OnMouseLeaveEventHandler;

                this.SKElementRef.MouseMove -= GridCursorInstance.OnMouseMoveEventHandler;
                this.SKElementRef.MouseMove += GridCursorInstance.OnMouseMoveEventHandler;

                this.SKElementRef.MouseLeftButtonDown -= GridCursorInstance.OnMouseLeftButtonDown;
                this.SKElementRef.MouseLeftButtonDown += GridCursorInstance.OnMouseLeftButtonDown;

                this.SKElementRef.EndInit();

                this.RenderEntryInstance = new RendererEntry(CubegridRenderCellGridInstance, GridGeometryMasterInstance, GridCursorInstance, SKElementRef);

                this.RenderEntryList.Add(RenderEntryInstance);

                if (_DebugMode)
                {
                    Trace.WriteLine($"BlueprintCellGridManager: Created CubegridRenderCellGridInstance for blueprint: {this.CubegridRenderCellGridInstance.cubeGridDefinitionRef.GetDisplayName()}");
                    Trace.WriteLine($"BlueprintCellGridManager: Created SKElement {this.SKElementRef.Name} with details:\n" +
                        $"Height: {this.SKElementRef.Height}\n" +
                        $"Width: {this.SKElementRef.Width}");

                    Trace.WriteLine($"BlueprintCellGridManager: Created CubegridRenderCellGridInstance with hash: {this.CubegridRenderCellGridInstance.GetHashCode()}");
                    Trace.WriteLine($"BlueprintCellGridManager: Created SKElement with hash: {this.SKElementRef.GetHashCode()}");
                    Trace.WriteLine($"BlueprintCellGridManager: Created GridGeometryMasterInstance with hash: {this.GridGeometryMasterInstance.GetHashCode()}");
                    Trace.WriteLine($"BlueprintCellGridManager: Created GridCursorInstance with hash: {this.GridCursorInstance.GetHashCode()}");
                }
            }

            this.SKElementRef = this.RenderEntryInstance.Item4;
            this.GridGeometryMasterInstance = this.RenderEntryInstance.Item2;
            this.GridCursorInstance = this.RenderEntryInstance.Item3;
            this.CubegridRenderCellGridInstance = this.RenderEntryInstance.Item1;

            this.GridGeometryMasterInstance.UpdateWindowSizeInfo();

            this.CubegridRenderCellGridInstance.RegenerateCells();

            this.LoadAndSelectSKSurfaceArrays(null, null);

            // Add ForegroundGridLayer
            this.StaticLayersCollection.Add("BackgroundGrid", new RenderLayers.ForegroundGridLayer(ref this.GridGeometryMasterInstance, ref this.SKElementRef, ref this._WindowRef));

            // Add Cursor Layer
            this.StaticLayersCollection.Add("GridCursor", this.GridCursorInstance);

            // Update the geometry for the existing static layers
            this.UpdateStaticLayerGeometry();
            // Render the layers for draw
            this.RenderStaticLayers();

            this.XLayerCenterIndex = GridGeometryMasterInstance.GetBPArrayBoundaryX() / 2;
            this.YLayerCenterIndex = GridGeometryMasterInstance.GetBPArrayBoundaryY() / 2;
            this.ZLayerCenterIndex = GridGeometryMasterInstance.GetBPArrayBoundaryZ() / 2;

            this.SKElementRef.InvalidateMeasure();
            this.SKElementRef.InvalidateArrange();
            this.SKElementRef.InvalidateVisual();
            this.SKElementRef.UpdateLayout();
        }

        internal void SubmitWindowSizeUpdate(Size windowSize)
        {
            if (_DebugMode)
            {
                Trace.WriteLine("BlueprintCellGridManager: Entering SubmitWindowSizeUpdate...");
            }

            this.GridGeometryMasterInstance.UpdateWindowSizeInfo();

            foreach (RendererEntry entry in this.RenderEntryList)
            {
                if(this._TabControl != null)
                {
                    entry.Item4.Height = this._TabControl.Height - 10;
                    entry.Item4.Width = this._TabControl.Width - 10;

                    if (_DebugMode)
                    {
                        Trace.WriteLine($"BlueprintCellGridManager -> SubmitWindowSizeUpdate: Setting SKElement with name {entry.Item4.Name} to Height {entry.Item4.Height}");
                        Trace.WriteLine($"BlueprintCellGridManager -> SubmitWindowSizeUpdate: Setting SKElement with name {entry.Item4.Width} to Width {entry.Item4.Width}");
                    }

                } else
                {
                    entry.Item4.Height = this._WindowRef.Height;
                    entry.Item4.Width = this._WindowRef.Width;

                    if (_DebugMode)
                    {
                        Trace.WriteLine($"BlueprintCellGridManager -> SubmitWindowSizeUpdate: Setting SKElement with name {entry.Item4.Name} to Height {entry.Item4.Height}");
                        Trace.WriteLine($"BlueprintCellGridManager -> SubmitWindowSizeUpdate: Setting SKElement with name {entry.Item4.Width} to Width {entry.Item4.Width}");
                    }
                }
            }

            if (_DebugMode)
            {
                Trace.WriteLine("BlueprintCellGridManager: Entering SubmitWindowSizeUpdate...");
            }
        }
        private void OnLoadedEventHandler(object sender, RoutedEventArgs e)
        {
            if (_DebugMode)
            {
                Trace.WriteLine("BlueprintCellGridManager: Entering OnLoadedEventHandler...");
            }

            this.GridGeometryMasterInstance.UpdateWindowSizeInfo();

            SKElement skElement = sender as SKElement;

            //skElement.Height = this.GridGeometryMasterInstance.GetSkiaElementHeight();
            //skElement.Width = this.GridGeometryMasterInstance.GetSkiaElementWidth();
            //skElement = this.GetCurrentSKElement();

            // ...and if not: Simply regenerate and redraw
            this.UpdateStaticLayerGeometry();
            this.RenderStaticLayers();

            this.CubegridRenderCellGridInstance.RegenerateCells();
            this.LoadAndSelectSKSurfaceArrays(null, null);

            //skElement.InvalidateMeasure();
            //skElement.InvalidateArrange();
            //skElement.InvalidateVisual();
            //skElement.UpdateLayout();

            if (_DebugMode)
            {
                Trace.WriteLine("BlueprintCellGridManager: Exiting OnLoadedEventHandler...");
            }
        }

        public void OnPaintSurfaceEventHandler(object? sender, SKPaintSurfaceEventArgs e)
        {
            if (_DebugMode)
            {
                Trace.WriteLine("<--- PAINT --->");
                Trace.WriteLine("BlueprintCellGridManager: Entering OnPaintSurfaceEventHandler...");
                Trace.WriteLine($"BlueprintCellGridManager -> OnPaintSurfaceEventHandler: Drawing on SKElement with hash {sender.GetHashCode()}");
                Trace.WriteLine($"BlueprintCellGridManager -> OnPaintSurfaceEventHandler: Drawing on SKCanvas with hash {e.Surface.GetHashCode()}");
                Trace.WriteLine($"BlueprintCellGridManager -> OnPaintSurfaceEventHandler: Drawing on SKSurface with hash {e.Surface.Canvas}");

                Tools.PerfStopwatch.Reset();
                Tools.PerfStopwatch.Start();
            }

            this.SKSurfaceIterator = e.Surface;
            this.SKCanvasIterator = e.Surface.Canvas;

            // Draw the background
            e.Surface.Canvas.DrawColor(UIDefaultGridColor);

            if (_DebugMode)
            {
                Trace.WriteLine($"BlueprintCellGridManager -> OnPaintSurfaceEventHandler: Draw UIDefaultGridColor Time: {Tools.PerfStopwatch.ElapsedMilliseconds} MS");
            }

            // Draw the Grid Blueprint
            this.SKSurfaceIterator = this.GenerateBPSkiaSurface();
            e.Surface.Canvas.DrawSurface(this.SKSurfaceIterator, (0), (0));

            if (_DebugMode)
            {
                Trace.WriteLine($"BlueprintCellGridManager: Drew SKSurfaceIterator With Hash: {this.SKSurfaceIterator.GetHashCode()}");
                Trace.WriteLine($"BlueprintCellGridManager: Draw SKSurfaceIterator Time: {Tools.PerfStopwatch.ElapsedMilliseconds} MS");
            }

            this.RenderStaticLayers();

            if (_DebugMode)
            {
                Trace.WriteLine($"BlueprintCellGridManager: RenderStaticLayers Time: {Tools.PerfStopwatch.ElapsedMilliseconds} MS");
            }

            // Draw the additional overlays
            foreach (KeyValuePair<string, SKSurface> RenderedStaticLayer in this.RenderedStaticSurfaceCollection)
            {
                if (RenderedStaticLayer.Value != null)
                {
                    e.Surface.Canvas.DrawSurface(RenderedStaticLayer.Value, (0), (0));
                }
                else
                {
                    Trace.WriteLine("BlueprintCellGridManager -> OnPaintSurfaceEventHandler: RenderedStaticLayer is null in OnPaintSurfaceEventHandler!");
                }
            }

            if (_DebugMode)
            {
                Trace.WriteLine($"BlueprintCellGridManager: Draw Overlays Time: {Tools.PerfStopwatch.ElapsedMilliseconds} MS");
            }



            if (_DebugMode)
            {
                // Draw the Dimension Indicators (colored corners)
                this.DebugDrawDimensionIndicators(sender, e);
                Trace.WriteLine($"BlueprintCellGridManager: Draw DebugDrawDimensionIndicators Time: {Tools.PerfStopwatch.ElapsedMilliseconds} MS");
            }

            //e.Surface.Canvas.DrawSurface(SKSurfaceIterator, (float)(this._SkiaElementRef.Height / 2), (float)(this._SkiaElementRef.Width / 2));
            //this.GridCursorInstance.DrawCursor(e);
            e.Surface.Canvas.Flush();

            if (_DebugMode)
            {
                Tools.PerfStopwatch.Stop();
                Trace.WriteLine($"BlueprintCellGridManager -> OnPaintSurfaceEventHandler: Total Paint Time: {Tools.PerfStopwatch.ElapsedMilliseconds} MS");
                Trace.WriteLine("BlueprintCellGridManager -> OnPaintSurfaceEventHandler: Exiting OnPaintSurfaceEventHandler...");
                Trace.WriteLine("<--- ENDPAINT --->");
            }
        }

        private void OnSKElementSizeChangedEventHandler(object sender, SizeChangedEventArgs e)
        {
            if (_DebugMode)
            {
                Trace.WriteLine("BlueprintCellGridManager -> OnPaintSurfaceEventHandler: Entering OnSKElementSizeChangedEventHandler...");
            }

            this.GridGeometryMasterInstance.UpdateWindowSizeInfo();

            this.UpdateStaticLayerGeometry();
            this.RenderStaticLayers();

            this.CubegridRenderCellGridInstance.RegenerateCells();
            this.SKElementRef.InvalidateVisual();

            if (_DebugMode)
            {
                Trace.WriteLine("BlueprintCellGridManager -> OnPaintSurfaceEventHandler: Exiting OnSKElementSizeChangedEventHandler...");
            }
        }

        private SKSurface CreateInitialSKSurface()
        {
            if (_DebugMode)
            {
                Trace.WriteLine("BlueprintCellGridManager -> OnPaintSurfaceEventHandler: Entering CreateInitialSKSurface...");
            }
            SKSurface skiaSurface;
            SKImageInfo imageInfo = CreateInitialSKImageInfo();
            SKSurfaceProperties surfaceProps = CreateSKSurfaceProperties();

            skiaSurface = SKSurface.Create(imageInfo, surfaceProps);

            if (_DebugMode)
            {
                Trace.WriteLine("BlueprintCellGridManager -> OnPaintSurfaceEventHandler: Exiting CreateInitialSKSurface...");
            }
            return skiaSurface;
        }
        private void UpdateStaticLayerGeometry()
        {
            if (_DebugMode)
            {
                Trace.WriteLine("BlueprintCellGridManager -> UpdateStaticLayerGeometry: Entering UpdateStaticLayerGeometry...");
            }

            // Update the existing layers with the new size information
            foreach (KeyValuePair<string, StaticRenderLayer> StaticLayer in this.StaticLayersCollection)
            {
                if (_DebugMode)
                {
                    Trace.WriteLine($"BlueprintCellGridManager -> UpdateStaticLayerGeometry: Updating GridGeometry for layer: {StaticLayer.Key}");
                }
                StaticLayer.Value.UpdateGridGeometry(ref this.GridGeometryMasterInstance);
            }

            if (_DebugMode)
            {
                Trace.WriteLine("BlueprintCellGridManager: Exiting UpdateStaticLayerGeometry...");
            }
        }

        private void RenderStaticLayers()
        {
            if (_DebugMode)
            {
                Trace.WriteLine("BlueprintCellGridManager: Entering RenderStaticLayers...");
            }

            this.RenderedStaticSurfaceCollection.Clear();

            foreach (KeyValuePair<string, StaticRenderLayer> StaticLayer in this.StaticLayersCollection)
            {
                if (_DebugMode)
                {
                    Trace.WriteLine($"BlueprintCellGridManager -> RenderStaticLayers: Render {StaticLayer.Key} START: {Tools.PerfStopwatch.ElapsedMilliseconds} MS");
                }

                this.RenderedStaticSurfaceCollection.Add(StaticLayer.Key, StaticLayer.Value.RenderSurface());

                if (_DebugMode)
                {
                    Trace.WriteLine($"BlueprintCellGridManager -> RenderStaticLayers: Render {StaticLayer.Key} END: {Tools.PerfStopwatch.ElapsedMilliseconds} MS");
                }
            }

            if (_DebugMode)
            {
                Trace.WriteLine("BlueprintCellGridManager: Exiting RenderStaticLayers...");
            }
        }

        private SKImageInfo CreateInitialSKImageInfo()
        {
            if (_DebugMode)
            {
                Trace.WriteLine("BlueprintCellGridManager: Entering CreateInitialSKImageInfo...");
            }
            SKImageInfo SKImageInfo = new SKImageInfo((int)GridGeometryMasterInstance.GetSkiaCanvasWidth(),
                                                      (int)GridGeometryMasterInstance.GetSkiaCanvasHeight());
            if (_DebugMode)
            {
                Trace.WriteLine("BlueprintCellGridManager: Exiting CreateInitialSKImageInfo...");
            }

            return SKImageInfo;
        }

        private SKSurfaceProperties CreateSKSurfaceProperties()
        {
            if (_DebugMode)
            {
                Trace.WriteLine("BlueprintCellGridManager: Entering CreateSKSurfaceProperties...");
            }
            SKSurfacePropsFlags flags = new SKSurfacePropsFlags();
            SKPixelGeometry skPixelGeometry = new SKPixelGeometry();

            SKSurfaceProperties SKSurfaceProps = new SKSurfaceProperties(flags, skPixelGeometry);

            if (_DebugMode)
            {
                Trace.WriteLine("BlueprintCellGridManager: Exiting CreateSKSurfaceProperties...");
            }
            return SKSurfaceProps;
        }

        public void LoadAndSelectSKSurfaceArrays(object sender, RoutedEventArgs e)
        {
            if (_DebugMode)
            {
                Trace.WriteLine("BlueprintCellGridManager: Entering LoadAndSelectSKSurfaceArrays...");
            }
            SKSurface surfaceIter;
            int retainedCursor = GetZCursor();
            int UIGridBoundaryZ = GridGeometryMasterInstance.GetUIGridArrayDimensionZ();

            this.SkiaSurfaceArrayInstance = new SKSurface[GridGeometryMasterInstance.GetBPArrayDimensionLengthZ()];

            for (int i = 0; i < UIGridBoundaryZ; i++)
            {
                SetZCursor(i);
                surfaceIter = GenerateBPSkiaSurface();
                this.SkiaSurfaceArrayInstance[i] = surfaceIter;

                if (_DebugMode)
                {
                    Trace.WriteLine($"BlueprintCellGridManager -> LoadAndSelectSKSurfaceArrays: <CREATED SURFACE WITH HASH {surfaceIter.GetHashCode()}>");
                }
            }

            // Set the surface reference layer to be the layer at the previously-selected index.
            this.SKSurfaceIterator = SkiaSurfaceArrayInstance[retainedCursor];
            this.SKCanvasIterator = SKSurfaceIterator.Canvas;

            // Return the zCursor to its original value
            SetZCursor(retainedCursor);

            if (_DebugMode)
            {
                Trace.WriteLine($"BlueprintCellGridManager -> LoadAndSelectSKSurfaceArrays: !SURFACE SELECTION! this.SKSurfaceIterator assigned to SKSurface with hash: {this.SKSurfaceIterator.GetHashCode()}!");
                Trace.WriteLine($"BlueprintCellGridManager -> LoadAndSelectSKSurfaceArrays: !CANVAS SELECTION! this.SKCanvasIterator assigned to SKCanvas with hash: {this.SKCanvasIterator.GetHashCode()}!");
                Trace.WriteLine("BlueprintCellGridManager -> LoadAndSelectSKSurfaceArrays: Exiting LoadAndSelectSKSurfaceArrays...");
            }
        }


        public SKSurface GenerateBPSkiaSurface()
        {
            if (_DebugMode)
            {
                Trace.WriteLine("BlueprintCellGridManager: Entering GenerateBPSkiaSurface...");
                Trace.WriteLine("<--- SURFACEDRAW --->");
            }

            int SkiaCanvasWidth = Convert.ToInt32(GridGeometryMasterInstance.GetSkiaCanvasWidth());
            int SkiaCanvasHeight = Convert.ToInt32(GridGeometryMasterInstance.GetSkiaCanvasHeight());
            int WPFWindowWidth = Convert.ToInt32(GridGeometryMasterInstance.GetWPFWindowWidth());
            int WPFWindowHeight = Convert.ToInt32(GridGeometryMasterInstance.GetWPFWindowHeight());

            SKImageInfo info = new SKImageInfo(SkiaCanvasWidth, SkiaCanvasHeight);

            SKSurfacePropsFlags flags = SKSurfacePropsFlags.UseDeviceIndependentFonts;
            SKPixelGeometry pixelGeometry = SKPixelGeometry.Unknown;
            SKSurfaceProperties props = new SKSurfaceProperties(flags, pixelGeometry);
            SKSurface newSurface = SKSurface.Create(info, props);
            SKCanvas newCanvas = newSurface.Canvas;

            SKRect CellUIRectIterator;
            SKPaint CellUIPaintIterator;
            SKPaint CellDebugPaint = new SKPaint();

            CellDebugPaint.TextSize = 15;
            CellDebugPaint.FakeBoldText = false;
            CellDebugPaint.Color = SKColors.DarkSlateGray;
            CellDebugPaint.TextAlign = SKTextAlign.Center;

            SKPaintSurfaceEventArgs paintArgs = new SKPaintSurfaceEventArgs(newSurface, info);
            if (_DebugMode)
            {
                Trace.WriteLine($"GenerateBPSkiaSurface Created SKSurface with hash {newSurface.GetHashCode()}!");
                Trace.WriteLine($"GenerateBPSkiaSurface Created SKCanvas with hash {newCanvas.GetHashCode()}!");
            }
            foreach (BlueprintCell cell in CubegridRenderCellGridInstance)
            {
                foreach (Tuple<string, SKRectI, SKPaint> UIRectangleEntry in cell.GetUIRectangles()) {

                    if (_DebugMode)
                    {
                        Trace.WriteLine($"GenerateBPSkiaSurface -> DRAWING: Rectangle associated with layer: {UIRectangleEntry.Item1}!");
                        Trace.WriteLine($"...With ToString value of: {UIRectangleEntry.Item2.ToString()}!");
                        Trace.WriteLine($"...With SKPaint value of: {UIRectangleEntry.Item3.ToString()}!");

                    }

                    CellUIRectIterator = UIRectangleEntry.Item2;
                    CellUIPaintIterator = UIRectangleEntry.Item3;

                    newCanvas.DrawRect(CellUIRectIterator, CellUIPaintIterator);
                    newCanvas.Save();
                    newCanvas.Flush();
                }


                if (_DebugMode)
                {
                    newCanvas.DrawText($"(X:{cell.GetArrayCoordinate().X}/Y:{cell.GetArrayCoordinate().Y})", cell.GetUIRect().MidX, cell.GetUIRect().MidY, CellDebugPaint);
                    newCanvas.Save();
                    newCanvas.Flush();
                }
            }

            if (_DebugMode)
            {
                Trace.WriteLine($"GenerateBPSkiaSurface Drew on Surface with hash {newSurface.GetHashCode()}!");
                Trace.WriteLine($"GenerateBPSkiaSurface Drew on Canvas with hash {newCanvas.GetHashCode()}!");
                Trace.WriteLine("<--- ENDSURFACEDRAW --->");
                Trace.WriteLine("BlueprintCellGridManager: Exiting GenerateBPSkiaSurface...");
            }
            return newSurface;
        }


        public void RenderCanvasBlueprintGrid()
        {
            // Draw the cells
            foreach (BlueprintCell cell in CubegridRenderCellGridInstance)
            {
                if (cell != null)
                {
                    SKCanvasIterator.DrawRect(cell.GetUIRect(), cell.GetUIPaint());
                }
            }

            // Draw the cursor
            SKCanvasIterator.DrawRect(GridCursorInstance.GetCursorRect(), GridCursorInstance.GetCursorPaint());
            return;
        }

        public void UpdateGeometryWindowInformation()
        {
            if (_DebugMode)
            {
                Trace.WriteLine("BlueprintCellGridManager: Entering UpdateGeometryWindowInformation...");
            }
            this.GridGeometryMasterInstance.UpdateWindowSizeInfo();

            if (_DebugMode)
            {
                Trace.WriteLine("BlueprintCellGridManager: Exiting UpdateGeometryWindowInformation...");
            }
        }

        public ref RendererEntry GetCurrentRenderEntry()
        {
            return ref this.RenderEntryInstance;
        }
        public ref SKElement GetCurrentSKElement()
        {
            if (_DebugMode)
            {
                Trace.WriteLine("BlueprintCellGridManager: Entering GetCurrentSKElement...");
                Trace.WriteLine($"BlueprintCellGridManager -> GetCurrentSKElement: Returning current BlueprintCellGridManager-SKElement with hash:{this.SKElementRef.GetHashCode()}!");
            }
            return ref this.SKElementRef;

        }

        internal ref GridGeometry GetGridGeometry()
        {
            return ref GridGeometryMasterInstance;
        }

        internal ref GridCursor GetGridCursor()
        {
            return ref GridCursorInstance;
        }

        internal void SetZCursor(int z)
        {
            if (_DebugMode)
            {
                Trace.WriteLine($"BlueprintCellGridManager: Entering SetZCursor for value {z}...");
            }
            if (z >= 0 && z <= this.GridGeometryMasterInstance.GetBPArrayBoundaryZ())
            {
                zCursor = z;
                this.CubegridRenderCellGridInstance.SetZCursor(z);
            }
            if (_DebugMode)
            {
                Trace.WriteLine($"BlueprintCellGridManager: Exiting SetZCursor for value {z}...");
            }
        }
        internal int GetZCursor()
        {
            return zCursor;
        }

        public bool SetDebugMode(bool debugValue)
        {
            if (_DebugMode)
            {
                Trace.WriteLine("BlueprintCellGridManager: Entering SetDebugMode...");
            }
            this._DebugMode = debugValue;
            this.GridGeometryMasterInstance._DebugMode = debugValue;
            this.CubegridRenderCellGridInstance.SetDebugMode(debugValue);

            this.SKElementRef.InvalidateVisual();
            if (_DebugMode)
            {
                Trace.WriteLine("BlueprintCellGridManager: Exiting SetDebugMode...");
            }
            return debugValue;
        }

        internal int GetMaxZCursorValue()
        {
            return this.CubegridRenderCellGridInstance.GetMaxZCursor();
        }

        internal bool SetCurrentRenderCellGrid(int index)
        {
            if (index > this.RenderEntryList.Count || index < 0)
            {
                return false;
            }
            else
            {
                this.RenderEntryInstance = this.RenderEntryList[index];
                this.SetZCursor(0);
                return true;
            }
        }

        internal int GetRenderGridCount()
        {
            return this.RenderEntryList.Count;
        }

        internal RendererEntry? GetRenderTupleAtIndex(int index)
        {
            if (index > this.RenderEntryList.Count || index < 0)
            {
                return null;
            }
            else
            {
                return this.RenderEntryList.ElementAt(index);
            }
        }

        public CubeGridRenderCellGrid GetCurrentRenderGrid()
        {
            return CubegridRenderCellGridInstance;
        }

        public void RawDraw(object? sender, SKPaintSurfaceEventArgs e)
        {
            SKPaint paintIter;

            SKColor colorIter;
            SKRect rectIter;
            SKPoint3 skPoint3Iter;
            SKPoint skPointIter;

            foreach (BlueprintCell cell in CubegridRenderCellGridInstance)
            {
                if (cell != null)
                {
                    rectIter = cell.GetUIRect();
                    paintIter = cell.GetUIPaint();
                    skPoint3Iter = cell.GetUI3DCellCoordinateCenter();
                    skPointIter = cell.GetUI2DCellCoordinateCenter();
                    e.Surface.Canvas.DrawRect(rectIter, paintIter);

                    colorIter = SKColors.White;

                    paintIter = new SKPaint();
                    paintIter.Color = colorIter;

                    e.Surface.Canvas.DrawCircle(rectIter.MidX, rectIter.MidY, 10, paintIter);

                    colorIter = SKColors.GreenYellow;

                    paintIter = new SKPaint();
                    paintIter.Color = colorIter;
                    e.Surface.Canvas.DrawCircle(rectIter.Right, rectIter.Bottom, 2, paintIter);
                }
            }


            e.Surface.Canvas.DrawRect(GridCursorInstance.GetCursorRect(), GridCursorInstance.GetCursorPaint());

            e.Surface.Canvas.DrawCircle(0, 0, 10, redPaint);
            e.Surface.Canvas.DrawCircle(GridGeometryMasterInstance.GetSkiaCanvasWidth(), 0, 10, yellowPaint);
            e.Surface.Canvas.DrawCircle(0, GridGeometryMasterInstance.GetSkiaCanvasHeight(), 10, greenPaint);
            e.Surface.Canvas.DrawCircle(GridGeometryMasterInstance.GetSkiaCanvasWidth(), GridGeometryMasterInstance.GetSkiaCanvasHeight(), 10, bluePaint);
        }

        public void DebugDrawDimensionIndicators(object? sender, SKPaintSurfaceEventArgs e)
        {
            e.Surface.Canvas.DrawCircle(0, 0, 10, redPaint);
            e.Surface.Canvas.DrawCircle(GridGeometryMasterInstance.GetSkiaCanvasWidth(), 0, 10, yellowPaint);
            e.Surface.Canvas.DrawCircle(0, GridGeometryMasterInstance.GetSkiaCanvasHeight(), 10, greenPaint);
            e.Surface.Canvas.DrawCircle(GridGeometryMasterInstance.GetSkiaCanvasWidth(), GridGeometryMasterInstance.GetSkiaCanvasHeight(), 10, bluePaint);
        }

        public void DebugDrawGrid(object? sender, SKPaintSurfaceEventArgs e)
        {
            SKPaint paintIter;

            SKColor colorIter;
            SKPoint skPointIter;

            // DEBUG DRAW: Draws alternating purple/green for a sanity check.
            if (GridGeometryMasterInstance.GetUIGridArrayBoundaryX() < GridGeometryMasterInstance.GetUIGridArrayBoundaryY())
            {
                for (int i = 0; i < GridGeometryMasterInstance.GetSkiaCanvasWidth(); i++)
                {
                    for (int j = 0; j < GridGeometryMasterInstance.GetSkiaCanvasHeight(); j++)
                    {
                        skPointIter = new SKPoint(i, j);

                        if (i % Convert.ToInt32(GridGeometryMasterInstance.GetPixelWidth()) == 0)
                        {
                            colorIter = i % Convert.ToInt32(GridGeometryMasterInstance.GetPixelWidth()) == 0 ? SKColors.Red : SKColors.Blue;
                        }
                        else
                        {
                            colorIter = j % Convert.ToInt32(GridGeometryMasterInstance.GetPixelHeight()) == 0 ? SKColors.HotPink : SKColors.Green;
                        }

                        paintIter = new SKPaint();
                        paintIter.Color = colorIter;

                        e.Surface.Canvas.DrawPoint(skPointIter, paintIter);
                    }
                }
            }
            else
            {
                for (int i = 0; i < GridGeometryMasterInstance.GetSkiaCanvasWidth(); i++)
                {
                    for (int j = 0; j < GridGeometryMasterInstance.GetSkiaCanvasHeight(); j++)
                    {
                        skPointIter = new SKPoint(i, j);

                        if (j % Convert.ToInt32(GridGeometryMasterInstance.GetPixelWidth()) == 0)
                        {
                            colorIter = j % Convert.ToInt32(GridGeometryMasterInstance.GetPixelWidth()) == 0 ? SKColors.Red : SKColors.Blue;
                        }
                        else
                        {
                            colorIter = i % Convert.ToInt32(GridGeometryMasterInstance.GetPixelHeight()) == 0 ? SKColors.HotPink : SKColors.Green;
                        }

                        paintIter = new SKPaint();
                        paintIter.Color = colorIter;

                        e.Surface.Canvas.DrawPoint(skPointIter, paintIter);
                    }
                }
            }
        }

        public bool PopulateTabControl(ref TabControl tabControl)
        {
            void OnTabItemSelectionChanged(object sender, SelectionChangedEventArgs e)
            {
                if (_DebugMode)
                {
                    Trace.WriteLine("BlueprintCellGridManager: Entering OnTabItemSelectionChanged...");
                }

                if (e.RemovedItems.Count > 0)
                {
                    if (_DebugMode)
                    {
                        foreach (TabItem tabItem in e.RemovedItems) {
                            Trace.WriteLine($"OnTabItemSelectionChanged -> REMOVING TabItem {tabItem.Name} with header {tabItem.Header.ToString()}");
                        }
                    }
                }
                if (e.AddedItems.Count > 0)
                {
                    TabItem firstChangedTab = (e.AddedItems[0] as TabItem);

                    this.RenderEntryInstance = this.RenderEntryList.First(renderEntry => renderEntry.Item1.cubeGridDefinitionRef.GetDisplayName().Equals(firstChangedTab.Header.ToString()));

                    this.SKElementRef = this.RenderEntryInstance.Item4;
                    this.GridGeometryMasterInstance = this.RenderEntryInstance.Item2;
                    this.GridCursorInstance = this.RenderEntryInstance.Item3;
                    this.CubegridRenderCellGridInstance = this.RenderEntryInstance.Item1;

                    this.SetZCursor(this.CubegridRenderCellGridInstance.GetZCursor());

                    if (_DebugMode)
                    {
                        Trace.WriteLine($"BlueprintCellGridManager -> OnTabItemSelectionChanged: Detected Change object is:\n{e.AddedItems[0].ToString()}");
                        Trace.WriteLine($"BlueprintCellGridManager -> OnTabItemSelectionChanged: Setting RenderEntryInstance to entry associated with blueprint {this.RenderEntryInstance.Item1.cubeGridDefinitionRef.GetDisplayName()}");

                        Trace.WriteLine($"BlueprintCellGridManager -> OnTabItemSelectionChanged: Setting current SKElementRef (Name:{this.SKElementRef.Name}) to SKElement with hash {this.SKElementRef.GetHashCode()}");
                        Trace.WriteLine($"BlueprintCellGridManager -> OnTabItemSelectionChanged: Setting current GridGeometryMasterInstance to GridGeometry with hash {this.GridGeometryMasterInstance.GetHashCode()}");
                        Trace.WriteLine($"BlueprintCellGridManager -> OnTabItemSelectionChanged: Setting current GridCursorInstance to GridCursor with hash {this.GridCursorInstance.GetHashCode()}");
                        Trace.WriteLine($"BlueprintCellGridManager -> OnTabItemSelectionChanged: Setting current CubegridRenderCellGridInstance to CubegridRenderCellGrid with hash {this.CubegridRenderCellGridInstance.GetHashCode()}");
                    }

                    if (_DebugMode)
                    {
                        Trace.WriteLine($"BlueprintCellGridManager -> OnTabItemSelectionChanged: Setting current tab content to SKElementRef (Name:{this.SKElementRef.Name}) with hash {this.SKElementRef.GetHashCode()}");
                    }

                    this.UpdateGeometryWindowInformation();

                    //this.CubegridRenderCellGridInstance.RegenerateCells();
                    //this.LoadAndSelectSKSurfaceArrays(null, null);

                    // Update Static Layers Collection
                    this.StaticLayersCollection.Clear();

                    // Add ForegroundGridLayer
                    this.StaticLayersCollection.Add("BackgroundGrid", new RenderLayers.ForegroundGridLayer(ref this.GridGeometryMasterInstance, ref this.SKElementRef, ref this._WindowRef));

                    // Add Cursor Layer
                    this.StaticLayersCollection.Add("GridCursor", this.GridCursorInstance);

                    // Update the geometry for the existing static layers
                    this.UpdateStaticLayerGeometry();

                    // Render the layers for draw
                    this.RenderStaticLayers();

                }
                else
                {
                    Trace.WriteLine($"BlueprintCellGridManager: AssignSelectedElements: No TabItems to iterate through!");
                }

                if (_DebugMode)
                {
                    Trace.WriteLine("BlueprintCellGridManager: Exiting OnTabItemSelectionChanged...");
                }
            }

            if (_DebugMode)
            {
                Trace.WriteLine("BlueprintCellGridManager: Entering PopulateTabControl...");
            }

            try
            {
                TabItem renderTab = new TabItem();

                this._TabControl = tabControl;

                tabControl.Items.Clear();

                foreach (RendererEntry renderGrid in this.RenderEntryList)
                {
                    renderTab = new TabItem();

                    renderTab.Header = $"{renderGrid.Item1.cubeGridDefinitionRef.GetDisplayName()}";
                    renderTab.Content = renderGrid.Item4;

                    tabControl.Items.Add(renderTab);

                    if (_DebugMode)
                    {
                        Trace.WriteLine($"BlueprintCellGridManager -> PopulateTabControl: Created TabItem with header {renderTab.Header}");
                        Trace.WriteLine($"BlueprintCellGridManager -> PopulateTabControl: Created TabItem with hash {renderTab.GetHashCode()}");
                        Trace.WriteLine($"BlueprintCellGridManager -> PopulateTabControl: Created TabItem with content (SKElement -> Name:{this.SKElementRef.Name})  hash {renderTab.Content.GetHashCode()}");
                    }
                }


                tabControl.SelectionChanged += OnTabItemSelectionChanged;

                tabControl.SelectedIndex = 0;
                tabControl.InvalidateVisual();
                if (_DebugMode)
                {
                    Trace.WriteLine("BlueprintCellGridManager: Exiting PopulateTabControl...");
                }
                return true;
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"BlueprintCellGridManager: PopulateTabControl{ex}");

                if (_DebugMode)
                {
                    Trace.WriteLine("BlueprintCellGridManager: Exiting PopulateTabControl (EXCEPTION ABOVE!)...");
                }
                return false;
            }


        }



        public void TESTRotateImage()
        {
            CubegridRenderCellGridInstance.RotateGridSwapXY();
        }
        public class GridGeometry
        {
            public bool _DebugMode = false;

            private SKElement _SkiaElementRef;
            private Window _WindowRef;

            private int _GridMaxXStored = 0;
            private int _GridMaxYStored = 0;
            private int _GridMaxZStored = 0;

            private int _GridMinXStored = 0;
            private int _GridMinYStored = 0;
            private int _GridMinZStored = 0;

            private int _GridOffsetXCalced = 0;
            private int _GridOffsetYCalced = 0;
            private int _GridOffsetZCalced = 0;

            private int _BPArrayDimensionLengthXCalced = 0;
            private int _BPArrayDimensionLengthYCalced = 0;
            private int _BPArrayDimensionLengthZCalced = 0;

            private int _BPArrayBoundaryXCalced = 0;
            private int _BPArrayBoundaryYCalced = 0;
            private int _BPArrayBoundaryZCalced = 0;

            private int _UIGridArrayDimensionXCalced = 0;
            private int _UIGridArrayDimensionYCalced = 0;
            private int _UIGridArrayDimensionZCalced = 0;

            private int _UIGridArrayBoundaryXCalced = 0;
            private int _UIGridArrayBoundaryYCalced = 0;
            private int _UIGridArrayBoundaryZCalced = 0;

            private float _CanvasHeightStored = 0;
            private float _CanvasWidthStored = 0;

            private float _PixelWidth = 0;
            private float _PixelHeight = 0;

            private float _PixelLeftOffset = 0;
            private float _PixelRightOffset = 0;
            private float _PixelTopOffset = 0;
            private float _PixelBottomOffset = 0;

            private float _WPFWindowHeightStored = 0;
            private float _WPFWindowWidthStored = 0;

            private float _SKElementWidthStored = 0;
            private float _SKElementHeightStored = 0;

            private float _SkiaWindowWidthDifferenceCalced = 0;
            private float _SkiaWindowHeightDifferenceCalced = 0;

            private float _DesiredSKElemToWPFWindowDiffHeight = 100;
            private float _DesiredSKElemToWPFWindowDiffWidth = 100;

            internal GridGeometry(BlueprintSBC_CubeGrid cubegridDefinition, ref SKElement skiaElement, ref Window window)
            {
                Vector3 cubeblockMinIterator;

                this._SkiaElementRef = skiaElement;
                this._WindowRef = window;

                #region StoredVariables
                this._WPFWindowHeightStored = (_WindowRef != null || !double.IsNaN(_WindowRef.Height)) ? (float)_WindowRef.Height : (float)SystemParameters.PrimaryScreenHeight / 2;
                this._WPFWindowWidthStored = (_WindowRef != null || !double.IsNaN(_WindowRef.Width)) ? (float)_WindowRef.Width : (float)SystemParameters.PrimaryScreenWidth / 2;

                this._SKElementHeightStored = (float)skiaElement.Height;
                this._SKElementWidthStored = (float)skiaElement.Width;

                this._CanvasHeightStored = (_SkiaElementRef.CanvasSize.Height != 0 || !double.IsNaN(_SkiaElementRef.CanvasSize.Height)) ? _SkiaElementRef.CanvasSize.Height : (float)SystemParameters.PrimaryScreenHeight / 2;
                this._CanvasWidthStored = (_SkiaElementRef.CanvasSize.Width != 0 || !double.IsNaN(_SkiaElementRef.CanvasSize.Width)) ? _SkiaElementRef.CanvasSize.Width : (float)SystemParameters.PrimaryScreenWidth / 2;

                foreach (BlueprintSBC_CubeBlock cubeblock in cubegridDefinition.GetCubeBlocks())
                {
                    cubeblockMinIterator = cubeblock.GetMinVector();

                    #region GridMaximumChecks
                    if (cubeblockMinIterator.X > _GridMaxXStored)
                    {
                        this._GridMaxXStored = (int)cubeblockMinIterator.X;
                    }

                    if (cubeblockMinIterator.Y > _GridMaxYStored)
                    {
                        this._GridMaxYStored = (int)cubeblockMinIterator.Y;
                    }

                    if (cubeblockMinIterator.Z > _GridMaxZStored)
                    {
                        this._GridMaxZStored = (int)cubeblockMinIterator.Z;
                    }
                    #endregion

                    #region GridMinimumChecks
                    if (cubeblockMinIterator.X < _GridMinXStored)
                    {
                        this._GridMinXStored = (int)cubeblockMinIterator.X;
                    }

                    if (cubeblockMinIterator.Y < _GridMinYStored)
                    {
                        this._GridMinYStored = (int)cubeblockMinIterator.Y;
                    }

                    if (cubeblockMinIterator.Z < _GridMinZStored)
                    {
                        this._GridMinZStored = (int)cubeblockMinIterator.Z;
                    }
                    #endregion
                }

                #endregion

                #region CalculatedVariables
                this._GridOffsetXCalced = Math.Abs(_GridMinXStored);
                this._GridOffsetYCalced = Math.Abs(_GridMinYStored);
                this._GridOffsetZCalced = Math.Abs(_GridMinZStored);

                this._BPArrayDimensionLengthXCalced = _GridMaxXStored - _GridMinXStored + 1;
                this._BPArrayDimensionLengthYCalced = _GridMaxYStored - _GridMinYStored + 1;
                this._BPArrayDimensionLengthZCalced = _GridMaxZStored - _GridMinZStored + 1;

                this._BPArrayBoundaryXCalced = _BPArrayDimensionLengthXCalced - 1;
                this._BPArrayBoundaryYCalced = _BPArrayDimensionLengthYCalced - 1;
                this._BPArrayBoundaryZCalced = _BPArrayDimensionLengthZCalced - 1;

                this._UIGridArrayDimensionXCalced = _BPArrayDimensionLengthXCalced;
                this._UIGridArrayDimensionYCalced = _BPArrayDimensionLengthYCalced;
                this._UIGridArrayDimensionZCalced = _BPArrayDimensionLengthZCalced;

                this._UIGridArrayBoundaryXCalced = _BPArrayBoundaryXCalced;
                this._UIGridArrayBoundaryYCalced = _BPArrayBoundaryYCalced;
                this._UIGridArrayBoundaryZCalced = _BPArrayBoundaryZCalced;

                this._SkiaWindowWidthDifferenceCalced = Math.Abs((float)this._WindowRef.Width - (float)this._SkiaElementRef.Width);
                this._SkiaWindowHeightDifferenceCalced = Math.Abs((float)this._WindowRef.Height - (float)this._SkiaElementRef.Height);

                this.LoadUIPixelDimensions();

                if (this._DebugMode)
                {
                    Trace.WriteLine($"GridGeometry: Grid Geometry Constructed with the following values:");
                    Trace.WriteLine($"_GridMaxXStored: {_GridMaxXStored}");
                    Trace.WriteLine($"_GridMaxYStored: {_GridMaxYStored}");
                    Trace.WriteLine($"_GridMaxZStored: {_GridMaxZStored}");

                    Trace.WriteLine($"_GridMinXStored: {_GridMinXStored}");
                    Trace.WriteLine($"_GridMinYStored: {_GridMinYStored}");
                    Trace.WriteLine($"_GridMinZStored: {_GridMinZStored}"); ;

                    Trace.WriteLine($"_GridOffsetXCalced: {_GridOffsetXCalced}");
                    Trace.WriteLine($"_GridOffsetYCalced: {_GridOffsetYCalced}");
                    Trace.WriteLine($"_GridOffsetZCalced: {_GridOffsetZCalced}");

                    Trace.WriteLine($"_BPArrayDimensionLengthXCalced: {_BPArrayDimensionLengthXCalced}");
                    Trace.WriteLine($"_BPArrayDimensionLengthYCalced: {_BPArrayDimensionLengthYCalced}");
                    Trace.WriteLine($"_BPArrayDimensionLengthZCalced: {_BPArrayDimensionLengthZCalced}");

                    Trace.WriteLine($"_BPArrayBoundaryXCalced: {_BPArrayBoundaryXCalced}");
                    Trace.WriteLine($"_BPArrayBoundaryYCalced: {_BPArrayBoundaryYCalced}");
                    Trace.WriteLine($"_BPArrayBoundaryZCalced: {_BPArrayBoundaryZCalced}");

                    Trace.WriteLine($"_UIGridArrayDimensionXCalced: {_UIGridArrayDimensionXCalced}");
                    Trace.WriteLine($"_UIGridArrayDimensionYCalced: {_UIGridArrayDimensionYCalced}");
                    Trace.WriteLine($"_UIGridArrayDimensionZCalced: {_UIGridArrayDimensionZCalced}");

                    Trace.WriteLine($"_UIGridArrayBoundaryXCalced: {_UIGridArrayBoundaryXCalced}");
                    Trace.WriteLine($"_UIGridArrayBoundaryYCalced: {_UIGridArrayBoundaryYCalced}");
                    Trace.WriteLine($"_UIGridArrayBoundaryZCalced: {_UIGridArrayBoundaryZCalced}");

                    Trace.WriteLine($"_CanvasHeightStored: {_CanvasHeightStored}");
                    Trace.WriteLine($"_CanvasWidthStored: {_CanvasWidthStored}");

                    Trace.WriteLine($"_PixelWidth: {_PixelWidth}");
                    Trace.WriteLine($"_PixelHeight: {_PixelHeight}");

                    Trace.WriteLine($"_PixelLeftOffset: {_PixelLeftOffset}");
                    Trace.WriteLine($"_PixelRightOffset: {_PixelRightOffset}");
                    Trace.WriteLine($"_PixelTopOffset: {_PixelTopOffset}");
                    Trace.WriteLine($"_PixelBottomOffset: {_PixelBottomOffset}");

                    Trace.WriteLine($"_WPFWindowHeightStored: {_WPFWindowHeightStored}");
                    Trace.WriteLine($"_WPFWindowWidthStored: {_WPFWindowWidthStored}");

                    Trace.WriteLine($"_SKElementWidthStored: {_SKElementWidthStored}");
                    Trace.WriteLine($"_SKElementHeightStored: {_SKElementHeightStored}");

                    Trace.WriteLine($"_SkiaWindowWidthDifferenceCalced: {_SkiaWindowWidthDifferenceCalced}");
                    Trace.WriteLine($"_SkiaWindowHeightDifferenceCalced: {_SkiaWindowHeightDifferenceCalced}");

                    Trace.WriteLine($"_DesiredSKElemToWPFWindowDiffHeight: {_DesiredSKElemToWPFWindowDiffHeight}");
                    Trace.WriteLine($"_DesiredSKElemToWPFWindowDiffWidth: {_DesiredSKElemToWPFWindowDiffWidth}");
                }
                #endregion
            }

            [Obsolete]
            internal GridGeometry(BlueprintSBC_BlueprintDefinition blueprintDefinition, ref SKElement skiaElement, ref Window window)
            {
                Vector3 cubeblockMinIterator;

                this._SkiaElementRef = skiaElement;
                this._WindowRef = window;

                #region StoredVariables
                this._WPFWindowHeightStored = (_WindowRef != null) ? (float)_WindowRef.Height : 0;
                this._WPFWindowWidthStored = (_WindowRef != null) ? (float)_WindowRef.Width : 0;

                foreach (BlueprintSBC_CubeGrid cubegrid in blueprintDefinition.GetCubegrids())
                {
                    foreach (BlueprintSBC_CubeBlock cubeblock in cubegrid.GetCubeBlocks())
                    {
                        cubeblockMinIterator = cubeblock.GetMinVector();

                        #region GridMaximumChecks
                        if (cubeblockMinIterator.X > _GridMaxXStored)
                        {
                            this._GridMaxXStored = (int)cubeblockMinIterator.X;
                        }

                        if (cubeblockMinIterator.Y > _GridMaxYStored)
                        {
                            this._GridMaxYStored = (int)cubeblockMinIterator.Y;
                        }

                        if (cubeblockMinIterator.Z > _GridMaxZStored)
                        {
                            this._GridMaxZStored = (int)cubeblockMinIterator.Z;
                        }
                        #endregion

                        #region GridMinimumChecks
                        if (cubeblockMinIterator.X < _GridMinXStored)
                        {
                            this._GridMinXStored = (int)cubeblockMinIterator.X;
                        }

                        if (cubeblockMinIterator.Y < _GridMinYStored)
                        {
                            this._GridMinYStored = (int)cubeblockMinIterator.Y;
                        }

                        if (cubeblockMinIterator.Z < _GridMinZStored)
                        {
                            this._GridMinZStored = (int)cubeblockMinIterator.Z;
                        }
                        #endregion
                    }
                }

                #endregion

                #region CalculatedVariables
                this._GridOffsetXCalced = Math.Abs(_GridMinXStored);
                this._GridOffsetYCalced = Math.Abs(_GridMinYStored);
                this._GridOffsetZCalced = Math.Abs(_GridMinZStored);

                this._BPArrayDimensionLengthXCalced = _GridMaxXStored - _GridMinXStored + 1;
                this._BPArrayDimensionLengthYCalced = _GridMaxYStored - _GridMinYStored + 1;
                this._BPArrayDimensionLengthZCalced = _GridMaxZStored - _GridMinZStored + 1;

                this._BPArrayBoundaryXCalced = _BPArrayDimensionLengthXCalced - 1;
                this._BPArrayBoundaryYCalced = _BPArrayDimensionLengthYCalced - 1;
                this._BPArrayBoundaryZCalced = _BPArrayDimensionLengthZCalced - 1;

                this._UIGridArrayDimensionXCalced = _BPArrayDimensionLengthXCalced;
                this._UIGridArrayDimensionYCalced = _BPArrayDimensionLengthYCalced;
                this._UIGridArrayDimensionZCalced = _BPArrayDimensionLengthZCalced;

                this._UIGridArrayBoundaryXCalced = _BPArrayBoundaryXCalced;
                this._UIGridArrayBoundaryYCalced = _BPArrayBoundaryYCalced;
                this._UIGridArrayBoundaryZCalced = _BPArrayBoundaryZCalced;

                this._SkiaWindowWidthDifferenceCalced = (_WindowRef != null) ? Math.Abs((float)_WindowRef.Width - _CanvasWidthStored) : 0;
                this._SkiaWindowHeightDifferenceCalced = (_WindowRef != null) ? Math.Abs((float)_WindowRef.Height - _CanvasHeightStored) : 0;

                this.LoadUIPixelDimensions();


                if (this._DebugMode)
                {
                    Trace.WriteLine($"GridGeometry: Grid Geometry Constructed with the following values:");
                    Trace.WriteLine($"_GridMaxXStored: {_GridMaxXStored}");
                    Trace.WriteLine($"_GridMaxYStored: {_GridMaxYStored}");
                    Trace.WriteLine($"_GridMaxZStored: {_GridMaxZStored}");

                    Trace.WriteLine($"_GridMinXStored: {_GridMinXStored}");
                    Trace.WriteLine($"_GridMinYStored: {_GridMinYStored}");
                    Trace.WriteLine($"_GridMinZStored: {_GridMinZStored}"); ;

                    Trace.WriteLine($"_GridOffsetXCalced: {_GridOffsetXCalced}");
                    Trace.WriteLine($"_GridOffsetYCalced: {_GridOffsetYCalced}");
                    Trace.WriteLine($"_GridOffsetZCalced: {_GridOffsetZCalced}");

                    Trace.WriteLine($"_BPArrayDimensionLengthXCalced: {_BPArrayDimensionLengthXCalced}");
                    Trace.WriteLine($"_BPArrayDimensionLengthYCalced: {_BPArrayDimensionLengthYCalced}");
                    Trace.WriteLine($"_BPArrayDimensionLengthZCalced: {_BPArrayDimensionLengthZCalced}");

                    Trace.WriteLine($"_BPArrayBoundaryXCalced: {_BPArrayBoundaryXCalced}");
                    Trace.WriteLine($"_BPArrayBoundaryYCalced: {_BPArrayBoundaryYCalced}");
                    Trace.WriteLine($"_BPArrayBoundaryZCalced: {_BPArrayBoundaryZCalced}");

                    Trace.WriteLine($"_UIGridArrayDimensionXCalced: {_UIGridArrayDimensionXCalced}");
                    Trace.WriteLine($"_UIGridArrayDimensionYCalced: {_UIGridArrayDimensionYCalced}");
                    Trace.WriteLine($"_UIGridArrayDimensionZCalced: {_UIGridArrayDimensionZCalced}");

                    Trace.WriteLine($"_UIGridArrayBoundaryXCalced: {_UIGridArrayBoundaryXCalced}");
                    Trace.WriteLine($"_UIGridArrayBoundaryYCalced: {_UIGridArrayBoundaryYCalced}");
                    Trace.WriteLine($"_UIGridArrayBoundaryZCalced: {_UIGridArrayBoundaryZCalced}");

                    Trace.WriteLine($"_CanvasHeightStored: {_CanvasHeightStored}");
                    Trace.WriteLine($"_CanvasWidthStored: {_CanvasWidthStored}");

                    Trace.WriteLine($"_PixelWidth: {_PixelWidth}");
                    Trace.WriteLine($"_PixelHeight: {_PixelHeight}");

                    Trace.WriteLine($"_PixelLeftOffset: {_PixelLeftOffset}");
                    Trace.WriteLine($"_PixelRightOffset: {_PixelRightOffset}");
                    Trace.WriteLine($"_PixelTopOffset: {_PixelTopOffset}");
                    Trace.WriteLine($"_PixelBottomOffset: {_PixelBottomOffset}");

                    Trace.WriteLine($"_WPFWindowHeightStored: {_WPFWindowHeightStored}");
                    Trace.WriteLine($"_WPFWindowWidthStored: {_WPFWindowWidthStored}");

                    Trace.WriteLine($"_SKElementWidthStored: {_SKElementWidthStored}");
                    Trace.WriteLine($"_SKElementHeightStored: {_SKElementHeightStored}");

                    Trace.WriteLine($"_SkiaWindowWidthDifferenceCalced: {_SkiaWindowWidthDifferenceCalced}");
                    Trace.WriteLine($"_SkiaWindowHeightDifferenceCalced: {_SkiaWindowHeightDifferenceCalced}");

                    Trace.WriteLine($"_DesiredSKElemToWPFWindowDiffHeight: {_DesiredSKElemToWPFWindowDiffHeight}");
                    Trace.WriteLine($"_DesiredSKElemToWPFWindowDiffWidth: {_DesiredSKElemToWPFWindowDiffWidth}");
                }

                #endregion
            }
            private void LoadUIPixelDimensions()
            {
                if (this._DebugMode)
                {
                    Trace.WriteLine("GridGeometry: Entering LoadUIPixelDimensions...");
                }

                if (_UIGridArrayDimensionXCalced > _UIGridArrayDimensionYCalced)
                {
                    _PixelWidth = (_CanvasWidthStored / _UIGridArrayDimensionXCalced);
                    _PixelHeight = (_CanvasHeightStored / _UIGridArrayDimensionYCalced);
                }
                else
                {
                    _PixelWidth = _CanvasWidthStored / _UIGridArrayDimensionYCalced;
                    _PixelHeight = _CanvasHeightStored / _UIGridArrayDimensionXCalced;
                }

                LoadRectOffsets();

                if (this._DebugMode)
                {
                    Trace.WriteLine($"GridGeometry: LoadUIPixelDimensions -> _PixelWidth : {_PixelWidth}");
                    Trace.WriteLine($"GridGeometry: LoadUIPixelDimensions -> _PixelHeight : {_PixelHeight}");

                    Trace.WriteLine("GridGeometry: Exiting LoadUIPixelDimensions...");
                }
            }

            private void LoadRectOffsets()
            {
                if (this._DebugMode)
                {
                    Trace.WriteLine("GridGeometry: Entering LoadRectOffsets...");
                }
                _PixelLeftOffset = -_PixelWidth;
                _PixelRightOffset = 0;

                _PixelTopOffset = -_PixelHeight;
                _PixelBottomOffset = 0;

                if (this._DebugMode)
                {
                    Trace.WriteLine($"GridGeometry: LoadRectOffsets -> _PixelLeftOffset : {_PixelLeftOffset}");
                    Trace.WriteLine($"GridGeometry: LoadRectOffsets -> _PixelRightOffset : {_PixelRightOffset}");
                    Trace.WriteLine($"GridGeometry: LoadRectOffsets -> _PixelTopOffset : {_PixelTopOffset}");
                    Trace.WriteLine($"GridGeometry: LoadRectOffsets -> _PixelBottomOffset : {_PixelBottomOffset}");

                    Trace.WriteLine("GridGeometry: Exiting LoadRectOffsets...");
                }
            }

            public void UpdateWindowSizeInfo()
            {
                if (this._DebugMode)
                {
                    Trace.WriteLine("GridGeometry: Entering UpdateWindowSizeInfo...");
                }

                if (this._DebugMode)
                {
                    Trace.WriteLine("GridGeometry: UpdateWindowSizeInfo Setting WPF Window Height/Width...");
                }
                this.SetWPFWindowHeight();
                this.SetWPFWindowWidth();

                if (this._DebugMode)
                {
                    Trace.WriteLine("GridGeometry: UpdateWindowSizeInfo Setting SKElement Height/Width...");
                }
                this.SetSKElementHeight();
                this.SetSKElementWidth();

                if (this._DebugMode)
                {
                    Trace.WriteLine("GridGeometry: UpdateWindowSizeInfo Setting Canvas Height/Width...");
                }
                this.SetCanvasHeight();
                this.SetCanvasWidth();

                if (this._DebugMode)
                {
                    Trace.WriteLine("GridGeometry: UpdateWindowSizeInfo Setting CalculateWPFSkiaDiffs...");
                }
                this.CalculateWPFSkiaDiffs();

                if (this._DebugMode)
                {
                    Trace.WriteLine("GridGeometry: UpdateWindowSizeInfo performing LoadUIPixelDimensions...");
                }
                this.LoadUIPixelDimensions();

                if (this._DebugMode)
                {
                    Trace.WriteLine("GridGeometry: Exiting UpdateWindowSizeInfo...");
                }
            }


            public void UpdateGeometryInformation(GridGeometry geometry)
            {
                if (this._DebugMode)
                {
                    Trace.WriteLine("GridGeometry: Entering UpdateGeometryInformation...");
                }

                this._GridMaxXStored = geometry._GridMaxXStored;
                this._GridMaxYStored = geometry._GridMaxYStored;
                this._GridMaxZStored = geometry._GridMaxZStored;

                this._GridMinXStored = geometry._GridMinXStored;
                this._GridMinYStored = geometry._GridMinYStored;
                this._GridMinZStored = geometry._GridMinZStored;

                this._GridOffsetXCalced = geometry._GridOffsetXCalced;
                this._GridOffsetYCalced = geometry._GridOffsetYCalced;
                this._GridOffsetZCalced = geometry._GridOffsetZCalced;

                this._BPArrayDimensionLengthXCalced = geometry._BPArrayDimensionLengthXCalced;
                this._BPArrayDimensionLengthYCalced = geometry._BPArrayDimensionLengthYCalced;
                this._BPArrayDimensionLengthZCalced = geometry._BPArrayDimensionLengthZCalced;

                this._BPArrayBoundaryXCalced = geometry._BPArrayBoundaryXCalced;
                this._BPArrayBoundaryYCalced = geometry._BPArrayBoundaryYCalced;
                this._BPArrayBoundaryZCalced = geometry._BPArrayBoundaryZCalced;

                this._UIGridArrayDimensionXCalced = geometry._UIGridArrayDimensionXCalced;
                this._UIGridArrayDimensionYCalced = geometry._UIGridArrayDimensionYCalced;
                this._UIGridArrayDimensionZCalced = geometry._UIGridArrayDimensionZCalced;

                this._UIGridArrayBoundaryXCalced = geometry._UIGridArrayBoundaryXCalced;
                this._UIGridArrayBoundaryYCalced = geometry._UIGridArrayBoundaryYCalced;
                this._UIGridArrayBoundaryZCalced = geometry._UIGridArrayBoundaryZCalced;

                this._CanvasHeightStored = geometry._CanvasHeightStored;
                this._CanvasWidthStored = geometry._CanvasWidthStored;

                this._PixelWidth = geometry._PixelWidth;
                this._PixelHeight = geometry._PixelHeight;

                this._PixelLeftOffset = geometry._PixelLeftOffset;
                this._PixelRightOffset = geometry._PixelRightOffset;
                this._PixelTopOffset = geometry._PixelTopOffset;
                this._PixelBottomOffset = geometry._PixelBottomOffset;

                this._WPFWindowHeightStored = geometry._WPFWindowHeightStored;
                this._WPFWindowWidthStored = geometry._WPFWindowWidthStored;

                this._SkiaWindowWidthDifferenceCalced = geometry._SkiaWindowWidthDifferenceCalced;
                this._SkiaWindowHeightDifferenceCalced = geometry._SkiaWindowHeightDifferenceCalced;

                this.LoadUIPixelDimensions();

                if (this._DebugMode)
                {
                    Trace.WriteLine($"GridGeometry: Grid Geometry Updated via UpdateGeometryInformation with the following values:");
                    Trace.WriteLine($"_GridMaxXStored: {_GridMaxXStored}");
                    Trace.WriteLine($"_GridMaxYStored: {_GridMaxYStored}");
                    Trace.WriteLine($"_GridMaxZStored: {_GridMaxZStored}");

                    Trace.WriteLine($"_GridMinXStored: {_GridMinXStored}");
                    Trace.WriteLine($"_GridMinYStored: {_GridMinYStored}");
                    Trace.WriteLine($"_GridMinZStored: {_GridMinZStored}"); ;

                    Trace.WriteLine($"_GridOffsetXCalced: {_GridOffsetXCalced}");
                    Trace.WriteLine($"_GridOffsetYCalced: {_GridOffsetYCalced}");
                    Trace.WriteLine($"_GridOffsetZCalced: {_GridOffsetZCalced}");

                    Trace.WriteLine($"_BPArrayDimensionLengthXCalced: {_BPArrayDimensionLengthXCalced}");
                    Trace.WriteLine($"_BPArrayDimensionLengthYCalced: {_BPArrayDimensionLengthYCalced}");
                    Trace.WriteLine($"_BPArrayDimensionLengthZCalced: {_BPArrayDimensionLengthZCalced}");

                    Trace.WriteLine($"_BPArrayBoundaryXCalced: {_BPArrayBoundaryXCalced}");
                    Trace.WriteLine($"_BPArrayBoundaryYCalced: {_BPArrayBoundaryYCalced}");
                    Trace.WriteLine($"_BPArrayBoundaryZCalced: {_BPArrayBoundaryZCalced}");

                    Trace.WriteLine($"_UIGridArrayDimensionXCalced: {_UIGridArrayDimensionXCalced}");
                    Trace.WriteLine($"_UIGridArrayDimensionYCalced: {_UIGridArrayDimensionYCalced}");
                    Trace.WriteLine($"_UIGridArrayDimensionZCalced: {_UIGridArrayDimensionZCalced}");

                    Trace.WriteLine($"_UIGridArrayBoundaryXCalced: {_UIGridArrayBoundaryXCalced}");
                    Trace.WriteLine($"_UIGridArrayBoundaryYCalced: {_UIGridArrayBoundaryYCalced}");
                    Trace.WriteLine($"_UIGridArrayBoundaryZCalced: {_UIGridArrayBoundaryZCalced}");

                    Trace.WriteLine($"_CanvasHeightStored: {_CanvasHeightStored}");
                    Trace.WriteLine($"_CanvasWidthStored: {_CanvasWidthStored}");

                    Trace.WriteLine($"_PixelWidth: {_PixelWidth}");
                    Trace.WriteLine($"_PixelHeight: {_PixelHeight}");

                    Trace.WriteLine($"_PixelLeftOffset: {_PixelLeftOffset}");
                    Trace.WriteLine($"_PixelRightOffset: {_PixelRightOffset}");
                    Trace.WriteLine($"_PixelTopOffset: {_PixelTopOffset}");
                    Trace.WriteLine($"_PixelBottomOffset: {_PixelBottomOffset}");

                    Trace.WriteLine($"_WPFWindowHeightStored: {_WPFWindowHeightStored}");
                    Trace.WriteLine($"_WPFWindowWidthStored: {_WPFWindowWidthStored}");

                    Trace.WriteLine($"_SKElementWidthStored: {_SKElementWidthStored}");
                    Trace.WriteLine($"_SKElementHeightStored: {_SKElementHeightStored}");

                    Trace.WriteLine($"_SkiaWindowWidthDifferenceCalced: {_SkiaWindowWidthDifferenceCalced}");
                    Trace.WriteLine($"_SkiaWindowHeightDifferenceCalced: {_SkiaWindowHeightDifferenceCalced}");

                    Trace.WriteLine($"_DesiredSKElemToWPFWindowDiffHeight: {_DesiredSKElemToWPFWindowDiffHeight}");
                    Trace.WriteLine($"_DesiredSKElemToWPFWindowDiffWidth: {_DesiredSKElemToWPFWindowDiffWidth}");

                    Trace.WriteLine("GridGeometry: Exiting UpdateGeometryInformation...");
                }
            }


            // Translates a "min" coord (_BlueprintDefinitionRef.sbc coordinate) to something that matches our zero-indexed grid.
            public int TranslateXMinCoordToUI(int x)
            {
                return Math.Abs(x) * _GridOffsetXCalced;
            }

            // Translates a "min" coord (_BlueprintDefinitionRef.sbc coordinate) to something that matches our zero-indexed grid.
            public int TranslateYMinCoordToUI(int y)
            {
                return Math.Abs(y) * _GridOffsetYCalced;
            }

            // Translates a "min" coord (_BlueprintDefinitionRef.sbc coordinate) to something that matches our zero-indexed grid.
            public int TranslateZMinCoordToUI(int z)
            {
                return Math.Abs(z) * _GridOffsetZCalced;
            }

            public SKElement GetSelectedSKElement()
            {
                return this._SkiaElementRef;
            }

            private void CalculateWPFSkiaDiffs()
            {
                if (this._DebugMode)
                {
                    Trace.WriteLine("GridGeometry: Entering CalculateWPFSkiaDiffs...");
                }
                // Calculate window height diff
                if (_WindowRef == null
                    || float.IsNaN(_WPFWindowHeightStored)
                    || float.IsNaN(_SKElementHeightStored)
                    || !float.IsPositive(_WPFWindowHeightStored)
                    || !float.IsPositive(_SKElementHeightStored)
                    || _WindowRef == null
                    || _SkiaElementRef == null
                    || double.IsNaN(this._WindowRef.Height)
                    || double.IsNaN(this._SkiaElementRef.Height))
                {
                    this._SkiaWindowHeightDifferenceCalced = float.MaxValue;
                }
                else
                {
                    this._SkiaWindowHeightDifferenceCalced = Math.Abs((float)this._WindowRef.Height - (float)this._SkiaElementRef.Height);
                }

                // Calculate window width diff
                if (_WindowRef == null ||
                    float.IsNaN(_WPFWindowWidthStored)
                    || float.IsNaN(_SKElementWidthStored)
                    || !float.IsPositive(_WPFWindowWidthStored)
                    || !float.IsPositive(_SKElementWidthStored)
                    || _WindowRef == null
                    || _SkiaElementRef == null
                    || double.IsNaN(this._WindowRef.Width)
                    || double.IsNaN(this._SkiaElementRef.Width))
                {
                    this._SkiaWindowWidthDifferenceCalced = float.MaxValue;
                }
                else
                {
                    this._SkiaWindowWidthDifferenceCalced = Math.Abs((float)this._WindowRef.Width - (float)this._SkiaElementRef.Width);
                }

                if (this._DebugMode)
                {
                    Trace.WriteLine($"GridGeometry: CalculateWPFSkiaDiffs -> _SkiaWindowHeightDifferenceCalced : {_SkiaWindowHeightDifferenceCalced}");
                    Trace.WriteLine($"GridGeometry: CalculateWPFSkiaDiffs -> _SkiaWindowWidthDifferenceCalced : {_SkiaWindowWidthDifferenceCalced}");

                    Trace.WriteLine("GridGeometry: Exiting CalculateWPFSkiaDiffs...");
                }
            }

            private void SetWPFWindowWidth()
            {
                if (this._DebugMode)
                {
                    Trace.WriteLine("GridGeometry: Entering SetWPFWindowWidth...");
                }

                float width = ((float)this._WindowRef.Width);

                if (float.IsNaN(width) || width <= 0)
                {
                    _WPFWindowWidthStored = (float)SystemParameters.PrimaryScreenWidth / 2;
                }
                else
                {
                    _WPFWindowWidthStored = width;
                }

                LoadUIPixelDimensions();

                if (this._DebugMode)
                {
                    Trace.WriteLine($"GridGeometry: SetWPFWindowWidth -> _WPFWindowWidthStored : {_WPFWindowWidthStored}");
                    Trace.WriteLine("GridGeometry: Exiting SetWPFWindowWidth...");
                }
            }

            private void SetWPFWindowHeight()
            {
                if (this._DebugMode)
                {
                    Trace.WriteLine("GridGeometry: Entering SetWPFWindowHeight...");
                }

                float height = ((float)this._WindowRef.Height);

                if (float.IsNaN(height) || height <= 0)
                {
                    _WPFWindowHeightStored = (float)SystemParameters.PrimaryScreenHeight / 2;
                }
                else
                {
                    _WPFWindowHeightStored = height;
                }

                LoadUIPixelDimensions();

                if (this._DebugMode)
                {
                    Trace.WriteLine($"GridGeometry: SetWPFWindowHeight -> _WPFWindowHeightStored : {_WPFWindowHeightStored}");
                    Trace.WriteLine("GridGeometry: Exiting SetWPFWindowHeight...");
                }
            }

            private void SetSKElementWidth()
            {
                if (this._DebugMode)
                {
                    Trace.WriteLine("GridGeometry: Entering SetSKElementWidth...");
                }

                float width = (float)this._SkiaElementRef.Width;

                if (float.IsNaN(width) || width <= 0)
                {
                    _SKElementWidthStored = (float)SystemParameters.PrimaryScreenWidth / 2;
                }
                else
                {
                    _SKElementWidthStored = width;
                }

                LoadUIPixelDimensions();

                if (this._DebugMode)
                {
                    Trace.WriteLine($"GridGeometry: SetSKElementWidth -> _SKElementWidthStored : {_SKElementWidthStored}");
                    Trace.WriteLine("GridGeometry: Exiting SetSKElementWidth...");
                }
            }

            private void SetSKElementHeight()
            {

                if (this._DebugMode)
                {
                    Trace.WriteLine("GridGeometry: Entering SetSKElementHeight...");
                }

                float height = (float)this._SkiaElementRef.Height;

                if (float.IsNaN(height) || height <= 0)
                {
                    _SKElementHeightStored = (float)SystemParameters.PrimaryScreenHeight / 2;
                }
                else
                {
                    _SKElementHeightStored = height;
                }

                LoadUIPixelDimensions();

                if (this._DebugMode)
                {
                    Trace.WriteLine($"GridGeometry: SetSKElementHeight -> _SKElementHeightStored : {_SKElementHeightStored}");
                    Trace.WriteLine("GridGeometry: Exiting SetSKElementHeight...");
                }
            }

            private void SetCanvasWidth()
            {

                if (this._DebugMode)
                {
                    Trace.WriteLine("GridGeometry: Entering SetCanvasWidth...");
                }

                float width = (float)this._SkiaElementRef.CanvasSize.Width;

                if (float.IsNaN(width) || width <= 0)
                {
                    _CanvasWidthStored = (float)SystemParameters.PrimaryScreenWidth / 2;
                }
                else
                {
                    _CanvasWidthStored = width;
                }

                LoadUIPixelDimensions();

                if (this._DebugMode)
                {
                    Trace.WriteLine($"GridGeometry: SetCanvasWidth -> _CanvasWidthStored : {_CanvasWidthStored}");
                    Trace.WriteLine("GridGeometry: Exiting SetCanvasWidth...");
                }
            }

            private void SetCanvasHeight()
            {
                if (this._DebugMode)
                {
                    Trace.WriteLine("GridGeometry: Entering SetCanvasHeight...");
                }

                float height = (float)this._SkiaElementRef.CanvasSize.Height;

                if (float.IsNaN(height) || height <= 0)
                {
                    _CanvasHeightStored = (float)SystemParameters.PrimaryScreenHeight / 2;
                }
                else
                {
                    _CanvasHeightStored = height;
                }

                LoadUIPixelDimensions();

                if (this._DebugMode)
                {
                    Trace.WriteLine($"GridGeometry: SetCanvasHeight -> _CanvasHeightStored : {_CanvasHeightStored}");
                    Trace.WriteLine("GridGeometry: Exiting SetCanvasHeight...");
                }
            }

            public void SetPixelWidth(float width)
            {
                if (this._DebugMode)
                {
                    Trace.WriteLine("GridGeometry: Entering SetPixelWidth...");
                }

                if (float.IsNaN(width) || width <= 0)
                {
                    _PixelWidth = 1;
                }
                else
                {
                    _PixelWidth = width;
                }

                LoadRectOffsets();

                if (this._DebugMode)
                {
                    Trace.WriteLine($"GridGeometry: SetPixelWidth -> _PixelWidth : {_PixelWidth}");
                    Trace.WriteLine("GridGeometry: Exiting SetPixelWidth...");
                }
            }

            public void SetPixelHeight(float height)
            {
                if (this._DebugMode)
                {
                    Trace.WriteLine("GridGeometry: Entering SetPixelHeight...");
                }

                if (float.IsNaN(height) || height <= 0)
                {
                    _PixelHeight = 1;
                }
                else
                {
                    _PixelHeight = height;
                }

                LoadRectOffsets();

                if (this._DebugMode)
                {
                    Trace.WriteLine($"GridGeometry: SetPixelHeight -> _PixelHeight : {_PixelHeight}");
                    Trace.WriteLine("GridGeometry: Exiting SetPixelHeight...");
                }
            }

            public int GetGridOffSetX()
            {
                return _GridOffsetXCalced;
            }

            public int GetGridOffSetY()
            {
                return _GridOffsetYCalced;
            }

            public int GetGridOffSetZ()
            {
                return _GridOffsetZCalced;
            }

            public int GetBPArrayDimensionLengthX()
            {
                return _BPArrayDimensionLengthXCalced;
            }

            public int GetBPArrayDimensionLengthY()
            {
                return _BPArrayDimensionLengthYCalced;
            }

            public int GetBPArrayDimensionLengthZ()
            {
                return _BPArrayDimensionLengthZCalced;
            }
            public int GetUIGridArrayDimensionX()
            {
                return _UIGridArrayDimensionXCalced;
            }

            public int GetUIGridArrayDimensionY()
            {
                return _UIGridArrayDimensionYCalced;
            }

            public int GetUIGridArrayDimensionZ()
            {
                return _UIGridArrayDimensionZCalced;
            }

            public int GetUIGridArrayBoundaryX()
            {
                return _UIGridArrayBoundaryXCalced;
            }

            public int GetUIGridArrayBoundaryY()
            {
                return _UIGridArrayBoundaryYCalced;
            }

            public int GetUIGridArrayBoundaryZ()
            {
                return _UIGridArrayBoundaryZCalced;
            }

            public float GetPixelLeftOffset()
            {
                if (float.IsNaN(_PixelLeftOffset))
                {
                    return 0;
                }
                else
                {
                    return _PixelLeftOffset;
                }
            }

            public float GetPixelRightOffset()
            {
                if (float.IsNaN(_PixelRightOffset))
                {
                    return 0;
                }
                else
                {
                    return _PixelRightOffset;
                }
            }

            public float GetPixelTopOffset()
            {
                if (float.IsNaN(_PixelTopOffset))
                {
                    return 0;
                }
                else
                {
                    return _PixelTopOffset;
                }
            }

            public float GetPixelBottomOffset()
            {
                if (float.IsNaN(_PixelBottomOffset))
                {
                    return 0;
                }
                else
                {
                    return _PixelBottomOffset;
                }
            }

            public float GetWPFWindowHeight()
            {
                return this._WPFWindowHeightStored;
            }

            public float GetWPFWindowWidth()
            {
                return this._WPFWindowWidthStored;
            }

            public float GetSkiaCanvasWidth()
            {
                if (this._DebugMode)
                {
                    Trace.WriteLine("GridGeometry: Entering GetSkiaCanvasWidth...");
                }

                if (this._CanvasWidthStored <= 1 || float.IsNaN(this._CanvasWidthStored))
                {
                    if (this._DebugMode)
                    {
                        Trace.WriteLine($"GridGeometry: GetSkiaCanvasWidth -> Returning : {(float)SystemParameters.PrimaryScreenWidth / 2}");
                        Trace.WriteLine("GridGeometry: Exiting GetSkiaCanvasWidth...");
                    }

                    return (float)SystemParameters.PrimaryScreenWidth / 2;
                }
                else
                {
                    if (this._DebugMode)
                    {
                        Trace.WriteLine($"GridGeometry: GetSkiaCanvasWidth -> Returning : {_CanvasWidthStored}");
                        Trace.WriteLine("GridGeometry: Exiting GetSkiaCanvasWidth...");
                    }
                    return _CanvasWidthStored;
                }

            }

            public float GetSkiaCanvasHeight()
            {
                if (this._DebugMode)
                {
                    Trace.WriteLine("GridGeometry: Entering GetSkiaCanvasHeight...");
                }

                if (this._CanvasHeightStored <= 1 || float.IsNaN(this._CanvasHeightStored))
                {
                    if (this._DebugMode)
                    {
                        Trace.WriteLine($"GridGeometry: GetSkiaCanvasHeight -> Returning : {(float)SystemParameters.PrimaryScreenHeight / 2}");
                        Trace.WriteLine("GridGeometry: Exiting GetSkiaCanvasHeight...");
                    }
                    return (float)SystemParameters.PrimaryScreenHeight / 2;
                }
                else
                {
                    if (this._DebugMode)
                    {
                        Trace.WriteLine($"GridGeometry: GetSkiaCanvasHeight -> Returning : {_CanvasHeightStored}");
                        Trace.WriteLine("GridGeometry: Exiting GetSkiaCanvasHeight...");
                    }
                    return _CanvasHeightStored;
                }


            }

            public void SetSkiaElementHeight(float value)
            {
                this._SKElementHeightStored = value;
            }

            public void SetSkiaElementWidth(float value)
            {
                this._SKElementWidthStored = value;
            }

            public float GetSkiaElementHeight()
            {
                if (this._DebugMode)
                {
                    Trace.WriteLine("GridGeometry: Entering GetSkiaElementHeight...");
                }

                if (this._SKElementHeightStored < 0
                    || float.IsNaN(this._SKElementHeightStored))
                {
                    if (this._DebugMode)
                    {
                        Trace.WriteLine($"GridGeometry: GetSkiaElementHeight -> Returning : {(float)SystemParameters.PrimaryScreenHeight / 2}");
                        Trace.WriteLine("GridGeometry: Exiting GetSkiaElementHeight...");
                    }
                    return (float)SystemParameters.PrimaryScreenHeight / 2;
                }
                else
                {
                    if (this._DebugMode)
                    {
                        Trace.WriteLine($"GridGeometry: GetSkiaElementHeight -> Returning : {this._SKElementHeightStored}");
                        Trace.WriteLine("GridGeometry: Exiting GetSkiaElementHeight...");
                    }
                    return this._SKElementHeightStored;
                }
            }

            public float GetSkiaElementWidth()
            {
                if (this._DebugMode)
                {
                    Trace.WriteLine("GridGeometry: Entering GetSkiaElementWidth...");
                }

                if ((this._SKElementWidthStored < 0)
                    || float.IsNaN(this._SKElementWidthStored))
                {
                    if (this._DebugMode)
                    {
                        Trace.WriteLine($"GridGeometry: GetSkiaElementWidth -> Returning : {(float)SystemParameters.PrimaryScreenWidth / 2}");
                        Trace.WriteLine("GridGeometry: Exiting GetSkiaElementWidth...");
                    }
                    return (float)SystemParameters.PrimaryScreenWidth / 2;
                }
                else
                {
                    if (this._DebugMode)
                    {
                        Trace.WriteLine($"GridGeometry: GetSkiaElementWidth -> Returning : {_SKElementWidthStored}");
                        Trace.WriteLine("GridGeometry: Exiting GetSkiaElementWidth...");
                    }
                    return this._SKElementWidthStored;
                }
            }

            public int GetBPArrayBoundaryX()
            {
                return _BPArrayBoundaryXCalced;
            }

            public int GetBPArrayBoundaryY()
            {
                return _BPArrayBoundaryYCalced;
            }

            public int GetBPArrayBoundaryZ()
            {
                return _BPArrayBoundaryZCalced;
            }

            public int GetGridMaxX()
            {
                return _GridMaxXStored;
            }

            public int GetGridMaxY()
            {
                return _GridMaxYStored;
            }

            public int GetGridMaxZ()
            {
                return _GridMaxZStored;
            }

            public int GetGridMinX()
            {
                return _GridMinXStored;
            }

            public int GetGridMinY()
            {
                return _GridMinYStored;
            }
            public int GetGridMinZ()
            {
                return _GridMinZStored;
            }

            public float GetPixelWidth()
            {
                if (_PixelWidth < 1 || float.IsNaN(_PixelWidth))
                {
                    return 1;
                }
                else
                {
                    return _PixelWidth;
                }
            }

            public float GetPixelHeight()
            {
                if (_PixelHeight < 1 || float.IsNaN(_PixelHeight))
                {
                    return 1;
                }
                else
                {
                    return _PixelHeight;
                }
            }

            public float GetActualSkiaToWPFHeightDifference()
            {
                return _SkiaWindowHeightDifferenceCalced;
            }

            public float GetActualSkiaToWPFWidthDifference()
            {
                return _SkiaWindowWidthDifferenceCalced;
            }

            public float GetDesiredSkiaToWPFDifferenceHeight()
            {
                return this._DesiredSKElemToWPFWindowDiffHeight;
            }

            public float GetDesiredSkiaToWPFDifferenceWidth()
            {
                return this._DesiredSKElemToWPFWindowDiffWidth;
            }


        }
    }

    public class CubeGridRenderCellGrid : IEnumerable
    {
        private bool _DebugMode = false;

        internal BlueprintSBC_CubeGrid cubeGridDefinitionRef;
        internal GridGeometry GridGeometryRef;
        internal BlueprintCell[,,] RenderGrid;

        int zCursor = 0;

        int UIGridArrayDimensionX;
        int UIGridArrayDimensionY;
        int UIGridArrayDimensionZ;

        internal CubeGridRenderCellGrid(ref GridGeometry uiGridGeometry, ref BlueprintSBC_CubeGrid cubeGridDefinition)
        {
            this.cubeGridDefinitionRef = cubeGridDefinition;
            this.GridGeometryRef = uiGridGeometry;

            UIGridArrayDimensionX = uiGridGeometry.GetUIGridArrayDimensionX();
            UIGridArrayDimensionY = uiGridGeometry.GetUIGridArrayDimensionY();
            UIGridArrayDimensionZ = uiGridGeometry.GetUIGridArrayDimensionZ();

            this.RenderGrid = new BlueprintCell[UIGridArrayDimensionX, UIGridArrayDimensionY, UIGridArrayDimensionZ];

            this.RegenerateCells();
        }

        internal void RegenerateCells()
        {
            if (_DebugMode)
            {
                Trace.WriteLine("CubeGridRenderCellGrid: Entering RegenerateCells...");
            }

            BlueprintCell bpCellIterator;

            Vector3 ArrayCoordinateIterator;
            List<BlueprintSBC_CubeBlock> cubeblocksIter;

            // Populate the 3D array with dummy BlueprintCell instances
            for (int i = 0; i < RenderGrid.GetLength(0); i++)
            {
                for (int j = 0; j < RenderGrid.GetLength(1); j++)
                {
                    for (int k = 0; k < RenderGrid.GetLength(2); k++)
                    {
                        RenderGrid[i, j, k] = new BlueprintCell(new Vector3(i, j, k), ref GridGeometryRef);
                    }
                }
            }

            // Then replace the populated cells with real cells
            cubeblocksIter = cubeGridDefinitionRef.GetCubeBlocks();

            foreach (BlueprintSBC_CubeBlock cubeblock in cubeblocksIter)
            {
                bpCellIterator = new BlueprintCell(cubeblock, ref GridGeometryRef);

                ArrayCoordinateIterator = bpCellIterator.GetArrayCoordinate();

                RenderGrid[(int)ArrayCoordinateIterator.X, (int)ArrayCoordinateIterator.Y, (int)ArrayCoordinateIterator.Z] = bpCellIterator;
            }

            if (_DebugMode)
            {
                Trace.WriteLine("CubeGridRenderCellGrid: Exiting RegenerateCells...");
            }
        }

        internal void RotateGridSwapXY()
        {
            foreach (BlueprintCell bpCell in RenderGrid)
            {
                bpCell.RotateUICoordsSwapXY();
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new BlueprintCellGridIterator(zCursor, this);
        }

        internal ref BlueprintCell GetCellRefAtCoord(int x, int y, int z)
        {
            if (x < 0 || y < 0 || z < 0)
            {
                throw new ArgumentException($"BlueprintCellGrid GetCellRefAtCoord --> Invalid matrix coords provided!\nProvided:\nx:{x},y:{y},z:{z}\n");
            }

            if (x < UIGridArrayDimensionX && y < UIGridArrayDimensionY && z < UIGridArrayDimensionZ)
            {
                return ref this.RenderGrid[x, y, z];
            } else
            {
                throw new ArgumentException("Invalid coodinates!");
            }
        }

        internal void SetGridGeometry(ref GridGeometry geometry)
        {
            this.GridGeometryRef = geometry;

            foreach (BlueprintCell cell in RenderGrid)
            {
                cell.UpdateGeometry(ref GridGeometryRef);
            }

            this.RegenerateCells();
        }
        internal ref GridGeometry GetGridGeometryRef()
        {
            return ref GridGeometryRef;
        }

        internal void SetZCursor(int z)
        {
            if (z >= 0 && z <= this.GridGeometryRef.GetBPArrayBoundaryZ())
            {
                this.zCursor = z;
            }
            else
            {
                Trace.WriteLine($"CubeGridRenderCellGrid: CubeGridRenderCellGrid.SetZCursor failed! Zcursor out of bounds at value: {z}");
            }
        }
        internal int GetZCursor()
        {
            return zCursor;
        }

        internal int GetMaxZCursor()
        {
            return this.GridGeometryRef.GetBPArrayBoundaryZ();
        }

        internal ref BlueprintCell[,,] GetBlueprintCell3DArrayRef()
        {
            return ref RenderGrid;
        }
        internal ref BlueprintSBC_CubeGrid GetBPSBCCubeGridDefinitionRef()
        {
            return ref this.cubeGridDefinitionRef;
        }
        internal bool SetDebugMode(bool debugValue)
        {
            this._DebugMode = debugValue;
            return debugValue;
        }

    }

    public class BlueprintCellGridIterator : IEnumerator
    {
        private bool _DebugMode = false;

        int xCursor;
        int yCursor;
        int zCursor;

        int xBoundary;
        int yBoundary;
        int zBoundary;

        BlueprintCell currentCell;
        CubeGridRenderCellGrid ui3DGrid;

        public BlueprintCellGridIterator(int cursorValue, CubeGridRenderCellGrid ui3DGrid)
        {
            xCursor = 0;
            yCursor = 0;

            zCursor = cursorValue;

            xBoundary = ui3DGrid.GridGeometryRef.GetBPArrayBoundaryX();
            yBoundary = ui3DGrid.GridGeometryRef.GetBPArrayBoundaryY();
            zBoundary = ui3DGrid.GridGeometryRef.GetBPArrayBoundaryZ();

            this.ui3DGrid = ui3DGrid;
        }

        object IEnumerator.Current => currentCell;

        bool IEnumerator.MoveNext()
        {
            BlueprintCell[,,] bp3DCellArray = ui3DGrid.GetBlueprintCell3DArrayRef();

            if (ui3DGrid.GridGeometryRef.GetUIGridArrayBoundaryX() < ui3DGrid.GridGeometryRef.GetUIGridArrayBoundaryY())
            {
                if (yCursor <= yBoundary)
                {
                    currentCell = bp3DCellArray[xCursor, yCursor, zCursor];
                    yCursor++;

                    return xCursor <= xBoundary;
                }
                else
                {
                    if (xCursor <= xBoundary)
                    {
                        yCursor = 0;

                        currentCell = bp3DCellArray[xCursor, yCursor, zCursor];
                        xCursor++;

                        return xCursor <= xBoundary;
                    }
                    else
                    {
                        currentCell = bp3DCellArray[xCursor, yCursor, zCursor];
                        return false;
                    }
                }
            }
            else
            {
                if (xCursor <= xBoundary)
                {
                    currentCell = bp3DCellArray[xCursor, yCursor, zCursor];
                    xCursor++;

                    return yCursor <= yBoundary;
                }
                else
                {
                    if (yCursor <= yBoundary)
                    {
                        xCursor = 0;

                        currentCell = bp3DCellArray[xCursor, yCursor, zCursor];
                        yCursor++;

                        return yCursor <= yBoundary;
                    }
                    else
                    {
                        currentCell = bp3DCellArray[xCursor, yCursor, zCursor];
                        return false;
                    }
                }
            }

        }


        void IEnumerator.Reset()
        {
            xCursor = 0;
            yCursor = 0;
        }
    }



}
