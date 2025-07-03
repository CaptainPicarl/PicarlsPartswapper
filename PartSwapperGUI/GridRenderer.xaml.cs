using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
using System.Xml.Linq;
using PartSwapperXMLSE;
using SkiaSharp;
using SkiaSharp.Views.WPF;
using SkiaSharp.Views.Desktop;
using System.Xml;
using System.Diagnostics;
using System.Runtime.InteropServices;
using ScottPlot.Colormaps;
using OpenTK.Graphics.OpenGL;

namespace PartSwapperGUI
{
    /// <summary>
    /// Interaction logic for _GridRenderer2024Ref.xaml
    /// </summary>
    public partial class GridRenderer : Window
    {
        XElement cubegrid;

        XElement[,,] grid3DArray;

        KeyValuePair<SKPoint, SKPaint>[,,] skiaGrid;

        KeyValuePair<SKPoint, SKPaint> skiaCursor;

        Point mousePosition;

        SKColor cursorColor;
        SKPaint cursorPaint;
        SKPoint cursorPosition;

        int skiaCursorX;
        int skiaCursorY;

        int gridMaxX;
        int gridMaxY;
        int gridMaxZ;

        int gridMinX;
        int gridMinY;
        int gridMinZ;

        int arrayBoundaryX;
        int arrayBoundaryY;
        int arrayBoundaryZ;

        int xCursor = 0;
        int xCursorPrevious = 0;

        //Size of each rendered 'rectangle' (effectively pixel in the image)
        int rectSizeValue = 5;

        Dictionary<int, XElement> idLocationResolver;

        public GridRenderer(XElement cubegrid)
        {

            this.cubegrid = cubegrid;
            this.cursorColor = SKColors.Yellow;
            this.cursorPaint = new SKPaint();
            this.cursorPaint.StrokeWidth = 5;
            this.cursorPaint.Color = cursorColor;

            grid3DArray = new XElement[arrayBoundaryX, arrayBoundaryY, arrayBoundaryZ];
            skiaGrid = new KeyValuePair<SKPoint, SKPaint>[arrayBoundaryX, arrayBoundaryY, arrayBoundaryZ];
            xCursor = 0;

            try
            {
                InitializeComponent();

                // Generate the initial dimensions and boundaries.
                initializeGridDimensions();

                // Start with a blank 'map' / 'screen' / whatever.
                resetSkiaGridCells();

                // populate3DGridArray will give us the ship slices
                populateRenderingArrays(arrayBoundaryX, arrayBoundaryY, arrayBoundaryZ, cubegrid.Element("CubeBlocks"));

                // Pay attention to scaling
                gridRendererSKCanvas.IgnorePixelScaling = false;

                // Debugging calls to renderGrid and renderGridSlice
                //renderGrid(cubegrid.Element("CubeBlocks"));

                //populateGridSliceShapes(cubegrid, xCursor);

                // Set GUI Variables
                xCursorScrollbar.Minimum = 0;
                xCursorScrollbar.Maximum = grid3DArray.GetLength(0) - 1;
                xCursorScrollbar.Value = xCursor;
                xCursorScrollbar.SmallChange = 1;
                xCursorScrollbar.LargeChange = 1;

                xCursorScrollbar.ValueChanged += (o, e) =>
                {
                    xCursor = ((int)xCursorScrollbar.Value);

                    xCursorIndicatorTextBlock.Text = xCursor.ToString();

                    //populateGridSliceShapes(cubegrid, xCursor);
                    gridRendererSKCanvas.InvalidateVisual();
                    gridRendererSKCanvas.UpdateLayout();
                };

                gridRotateButton.Click += (o, e) =>
                {
                    rotateArrays();

                    xCursorScrollbar.Maximum = grid3DArray.GetLength(0) - 1;
                };

                xCursorIndicatorTextBlock.Text = xCursor.ToString();

                xCursorIndicatorTextBlock.TextChanged += (o, e) =>
                {
                    try
                    {
                        xCursorPrevious = xCursor;
                        xCursor = (int)xCursorScrollbar.Value;
                    }
                    catch (Exception ex)
                    {
                        xCursor = xCursorPrevious;
                    }
                };

            }
            catch (Exception ex)
            {
                MessageBox.Show("Error occurred!\n" + ex.ToString());
            }

        }

        // Initializes the dimension information for this _GridRenderer2024Ref instance.
        // This includes values *and* arrays!
        // Used ONLY at initial map population, since it takes the dimensions directly from the grid definition!
        public void initializeGridDimensions()
        {
            // get min and max grid dimensions so we know how large to render the canvas-grid
            gridMaxX = getGridMaxDim("x", cubegrid.Element("CubeBlocks"));
            gridMaxY = getGridMaxDim("y", cubegrid.Element("CubeBlocks"));
            gridMaxZ = getGridMaxDim("z", cubegrid.Element("CubeBlocks"));

            gridMinX = getGridMinDim("x", cubegrid.Element("CubeBlocks"));
            gridMinY = getGridMinDim("y", cubegrid.Element("CubeBlocks"));
            gridMinZ = getGridMinDim("z", cubegrid.Element("CubeBlocks"));

            //arrayBoundary should represent the total length of a dimension
            // Thought with boundaries of +/-...Think about the range of -1 to +1
            // 1 - (-1) = 2, but there are three positions there! Add +1!
            arrayBoundaryX = (gridMaxX - gridMinX) + 1;
            arrayBoundaryY = (gridMaxY - gridMinY) + 1;
            arrayBoundaryZ = (gridMaxZ - gridMinZ) + 1;

            grid3DArray = new XElement[arrayBoundaryX, arrayBoundaryY, arrayBoundaryZ];
            skiaGrid = new KeyValuePair<SKPoint, SKPaint>[arrayBoundaryX, arrayBoundaryY, arrayBoundaryZ];

            // 18MAY2024 Experiment: Set canvas size based off array boundary dimensions?
            // Lesson learned: These calls *will* modify the canvas, but they aren't 'correct' right now.
            //gridRendererSKCanvas.Height = _BPArrayBoundaryYCalced;
            //gridRendererSKCanvas.Width = _BPArrayBoundaryXCalced;
        }


        // The intention is to rotate the image by 90 degrees
        public void rotateArrays()
        {
            checkArrayBoundaries();

            // assume the first dimension is already selected with xCursor
            /* Disabling while we try to develop a solution that relies on the cubegrid dimension, rather than the prior array dimension. */


            int lenX = grid3DArray.GetLength(0);
            int lenY = grid3DArray.GetLength(1);
            int lenZ = grid3DArray.GetLength(2);


            // Set new dimensions, globally. We are rotating, so Y and Z flip.
            // We make our assignments normally here...

            /* <-- I think this solution will be wrong, because it bases dimensions off of the cubeblocks def, and not the previous grid
            _GridMaxXStored = getGridMaxDim("x", cubegrid.Element("CubeBlocks"));
            _GridMaxZStored = getGridMaxDim("z", cubegrid.Element("CubeBlocks"));
            _GridMaxYStored = getGridMaxDim("y", cubegrid.Element("CubeBlocks"));

            _GridMinXStored = getGridMinDim("x", cubegrid.Element("CubeBlocks"));
            _GridMinZStored = getGridMinDim("z", cubegrid.Element("CubeBlocks"));
            _GridMinYStored = getGridMinDim("y", cubegrid.Element("CubeBlocks"));
            

            //arrayBoundary should represent the total length of a dimension
            // Thought with boundaries of +/-...Think about the range of -1 to +1
            // 1 - (-1) = 2, but there are three positions there! Add +1!
            // Since we are rotating the array here - we flip the assignments:
            // Notice that the X/Y/Z values here are now 'flipped' on a Z/Y axis. 
            // This *should* switch the dimensions on the arrays...
            
            _BPArrayBoundaryXCalced = (_GridMaxXStored - _GridMinXStored);
            _BPArrayBoundaryYCalced = (_GridMaxYStored - _GridMinYStored);
            _BPArrayBoundaryZCalced = (_GridMaxZStored - _GridMinZStored);
            */

            XElement[,,] rotatedCubeblockArray = new XElement[lenX, lenZ, lenY];
            KeyValuePair<SKPoint, SKPaint>[,,] rotatedPaintArray = new KeyValuePair<SKPoint, SKPaint>[lenX, lenZ, lenY];

            XElement gridEntryIter;
            KeyValuePair<SKPoint, SKPaint> skiaPaintGridEntryIter;

            // Let's think of it this way:
            // For every 'x', we can keep the 'x' layer where it is - but we need to rotate the rows within each 'slice' of x.
            for (int x = 0; x < lenX; x++)
            {
                for (int y = 0; y < lenY; y++)
                {
                    for (int z = 0; z < lenZ; z++)
                    {


                        // We rotate both arrays because we want reference addresses to be reliable at all times!
                        //TODO: Something *RIGHT HERE* is fucked up! Figure out how to 'rotate' a bitmap!
                        gridEntryIter = grid3DArray[x, y, z];
                        skiaPaintGridEntryIter = skiaGrid[x, y, z];

                        // Uncomment for debugging. Warning: Console output always SUPER slow!
                        //Trace.WriteLine($"Assigning value x/y/z:\nFrom:\n{x}/{y}/{z}\nTo:\n{x}/{z}/{y}");


                        rotatedCubeblockArray[x, z, y] = gridEntryIter;
                        rotatedPaintArray[x, z, y] = skiaPaintGridEntryIter;
                    }
                }
            }



            this.grid3DArray = rotatedCubeblockArray;
            this.skiaGrid = rotatedPaintArray;

            gridRendererSKCanvas.InvalidateArrange();
            gridRendererSKCanvas.InvalidateVisual();
        }

        public void checkArrayBoundaries()
        {
            if (skiaGrid.GetLength(0) != grid3DArray.GetLength(0) ||
                skiaGrid.GetLength(1) != grid3DArray.GetLength(1) ||
                skiaGrid.GetLength(2) != grid3DArray.GetLength(2))
            {
                throw new ArgumentException("Paint array and 3D array are different sizes!\nThis should never happen!\n");
            }
        }

        // populateRenderingArrays.
        //Note: maxX/maxY/maxZ need the maxX/Y/Z found via the *difference* of the max and min.
        //Such as: maxX = getGridMax(X...) - getGridMin(X...)
        public void populateRenderingArrays(int maxX, int maxY, int maxZ, XElement gridRoot)
        {

            SKPaint paintIterator = new SKPaint();
            SKPaint defaultPaint = new SKPaint();

            KeyValuePair<SKPoint, SKPaint> skiaPointPaintEntry;
            XElement[,,] gridArray = new XElement[maxX, maxY, maxZ];
            XElement referenceElement;
            SKPoint point = new SKPoint(0, 0);
            SKColor color = SKColors.BlueViolet;

            string blockX = "";
            string blockY = "";
            string blockZ = "";

            int gridX = 0;
            int gridY = 0;
            int gridZ = 0;

            int topBlockId;

            float SATURATION_DELTA = 0.8f;

            float VALUE_DELTA = 0.55f;

            float VALUE_COLORIZE_DELTA = 0.1f;

            // Set default paintIterator stroke and color.
            defaultPaint.StrokeWidth = 2;
            paintIterator.StrokeWidth = 2;

            // Set pain colors
            defaultPaint.Color = color;
            paintIterator.Color = color;


            checkArrayBoundaries();

            try
            {

                // In this block is where you can do cube processing, if you need to.
                foreach (XElement cubeblock in gridRoot.Elements())
                {
                    // Idea: Add the absolute value of 'gridMin' to the xCursorValue/y/z values in order to perform index-correction and allow the coordinates into the array
                    // NOTE: This will have the effect of making all values 0 through gridMin representing 'negative' values. True zero in the 3D grid array is the value of gridMin!
                    // NOTE: Subtracting one due to zero-indexing. Hopefully this doesn't mess up our actual 'zero'.

                    // Setting values

                    //Assign block x/y/z values in our gridArray
                    if (cubeblock.Element("Min") == null)
                    {

                        if (cubeblock.Element("EntityID") != null)
                        {
                            topBlockId = int.Parse(cubeblock.Element("TopBlockID").Value);
                            referenceElement = idLocationResolver[topBlockId];

                            blockX = referenceElement.Element("Min").Attribute("x").Value;
                            blockY = referenceElement.Element("Min").Attribute("y").Value;
                            blockZ = referenceElement.Element("Min").Attribute("z").Value;

                            // We have to do this to get all the values into a positive value
                            gridX = Int32.Parse(blockX) + Math.Abs(gridMinX);
                            gridY = Int32.Parse(blockY) + Math.Abs(gridMinY);
                            gridZ = Int32.Parse(blockZ) + Math.Abs(gridMinZ);

                            // Set the appropriate values in both arrays, using the same addressing.
                            gridArray[gridX, gridY, gridZ] = cubeblock;
                        }
                        else
                        {
                            // At this point - we have no way of getting the location.
                            throw new ArgumentException($"Unable to determine location for the following cubeblock:{cubeblock}");
                        }
                    }
                    else
                    {
                        // Min is not null! Use the coordinates provided!
                        blockX = cubeblock.Element("Min").Attribute("x").Value;
                        blockY = cubeblock.Element("Min").Attribute("y").Value;
                        blockZ = cubeblock.Element("Min").Attribute("z").Value;

                        // We have to do this to get all the values into a positive value
                        gridX = Int32.Parse(blockX) + Math.Abs(gridMinX);
                        gridY = Int32.Parse(blockY) + Math.Abs(gridMinY);
                        gridZ = Int32.Parse(blockZ) + Math.Abs(gridMinZ);

                        // Set the appropriate values in both arrays, using the same addressing.
                        gridArray[gridX, gridY, gridZ] = cubeblock;
                    }

                    // Populate the skiaGrid with the blocks' custom color, if we find it.
                    // Then overwrite the default entry we could have made for the paintIterator array. No need to do anything with the gridArray.
                    // NOTE: This is where we populate the PAINT ARRAY!
                    if (cubeblock.Element("ColorMaskHSV") != null)
                    {
                        XElement colorMaskHSVelement = cubeblock.Element("ColorMaskHSV");

                        // Give an actual-color paintIterator to the block representation
                        // Why the ColorMaskHSV uses x/y/z coords? I will never know...
                        string h = colorMaskHSVelement.Attribute("x").Value;
                        string s = colorMaskHSVelement.Attribute("y").Value;
                        string v = colorMaskHSVelement.Attribute("z").Value;

                        float hFloat = float.Parse(h);
                        float sFloat = float.Parse(s);
                        float vFloat = float.Parse(v);

                        point = new SKPoint(gridY, gridZ);

                        paintIterator = new SKPaint();


                        try
                        {
                            // Winner! It doesn't seem entirely 1:1, but I think it's as good as we'll get for now.
                            paintIterator.Color = SKColor.FromHsv(Math.Clamp((hFloat * 360f),0f,360f), Math.Clamp(((sFloat + SATURATION_DELTA)*100f),0f,100f), Math.Clamp(((vFloat + VALUE_DELTA - VALUE_COLORIZE_DELTA) * 100f),0f,100f));

                            //paintIterator.Color = SKColor.FromHsv((hFloat / 360f), ((sFloat * 0.1f) - SATURATION_DELTA), ((vFloat * 0.1f) - VALUE_DELTA + VALUE_COLORIZE_DELTA));

                            //paintIterator.Color = SKColor.FromHsv((hFloat / 360f), ((sFloat * 0.1f) - SATURATION_DELTA), ((vFloat * 0.1f) - VALUE_DELTA + VALUE_COLORIZE_DELTA));

                            //paintIterator.Color = SKColor.FromHsv((hFloat/360f), (sFloat*0.1f) - SATURATION_DELTA, ((vFloat*0.1f)/2f) - VALUE_DELTA + VALUE_COLORIZE_DELTA);

                            //paintIterator.Color = SKColor.FromHsv((hFloat), (sFloat + 100f)/2f, ((vFloat + 100f)/2f));

                            //paintIterator.Color = SKColor.FromHsv((hFloat), (Math.Clamp((sFloat + SATURATION_DELTA),0f,1f)), (Math.Clamp((vFloat + VALUE_DELTA - VALUE_COLORIZE_DELTA),0f,1f)));
                        }
                        catch (Exception e)
                        {
                            throw new Exception("Failure to parse HSV color!\nError was:" + e);
                        }

                        paintIterator.StrokeWidth = 2;

                        skiaPointPaintEntry = new KeyValuePair<SKPoint, SKPaint>(point, paintIterator);
                        skiaGrid[gridX, gridY, gridZ] = skiaPointPaintEntry;
                    }
                    else
                    {
                        // Give a generic blueViolet color paintIterator to the block representation in case we can't find a 'ColorMaskHSV' value
                        paintIterator = defaultPaint;
                        skiaPointPaintEntry = new KeyValuePair<SKPoint, SKPaint>(point, paintIterator);
                        skiaGrid[gridX, gridY, gridZ] = skiaPointPaintEntry;
                    }

                    // The below conditions check for either: 1) The coordinates of a block, explicitly as a tag. 2) Any indicators of a 'reference' to a parent block that we will use against the idResolver.
                    // NOTE: This is where we populate the PAINT ARRAY!





                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in populating 3D Grid Array!\n" + ex);
            }
        }

        public int getGridMaxDim(String dim, XElement cubeblocks)
        {
            int MaxDim = 0;
            int currDimVal = 0;

            if (cubeblocks == null)
            {
                MessageBox.Show("Invalid cubegridRoot provided!");
                return 0;
            }

            foreach (XElement cubeblock in cubeblocks.Descendants())
            {
                if (cubeblock.Element("Min") != null)
                {
                    currDimVal = Int32.Parse(cubeblock.Element("Min").Attribute(dim).Value);

                    if (currDimVal > MaxDim)
                    {
                        MaxDim = currDimVal;
                    }
                    else
                    {
                        continue;
                    }
                }
                else
                {
                    continue;
                }

            }
            return MaxDim;
        }

        public int getGridMinDim(String dim, XElement cubeblocks)
        {
            int MinDim = 0;
            int currDimVal;

            if (cubeblocks == null)
            {
                MessageBox.Show("Invalid cubegridRoot provided!");
                return 0;
            }

            foreach (XElement cubeblock in cubeblocks.Descendants())
            {
                if (cubeblock.Element("Min") != null)
                {
                    currDimVal = Int32.Parse(cubeblock.Element("Min").Attribute(dim).Value);
                    if (currDimVal < MinDim)
                    {
                        MinDim = currDimVal;
                    }
                    else
                    {
                        continue;
                    }
                }
                else
                {
                    continue;
                }
            }

            return MinDim;
        }

        // takes the globally-accessible skiaGrid and populates each cell with our 'default' value
        public void resetSkiaGridCells()
        {
            SKPoint point;
            SKColor color = SKColors.BlueViolet;
            SKPaint paint = new SKPaint();

            paint.StrokeWidth = 2;
            paint.Color = color;

            for (int x = 0; x < skiaGrid.GetLength(0); x++)
            {
                for (int y = 0; y < skiaGrid.GetLength(1); y++)
                {
                    for (int z = 0; z < skiaGrid.GetLength(2); z++)
                    {
                        point = new SKPoint(y, z);
                        skiaGrid[x, y, z] = new KeyValuePair<SKPoint, SKPaint>(point, paint);
                    }
                }
            }
        }

        public void rectValueChangedEvent(Object sender, TextChangedEventArgs e)
        {
            int retrievedValue = -1;

            if (rectangleSizeTextbox.Text == null || rectangleSizeTextbox.Text.Length == 0)
            {
                rectSizeValue = 1;
            }
            else
            {

                try
                {
                    retrievedValue = int.Parse(rectangleSizeTextbox.Text);

                    if (retrievedValue != -1 && retrievedValue > 0)
                    {
                        rectSizeValue = retrievedValue;
                    }
                    else
                    {
                        rectSizeValue = 1;
                    }
                }
                catch (Exception ex)
                {
                    rectSizeValue = 1;
                    MessageBox.Show("Invalid rectangle size selected!\nMust be 1 or greater!");
                }
            }

            //Force a redraw
            if (gridRendererSKCanvas != null)
            {
                gridRendererSKCanvas.InvalidateVisual();
            }
        }

        // This is where the gridrendering actually happens
        public void OnPaintSurface(object sender, SKPaintSurfaceEventArgs e)
        {
            SKCanvas canvas = e.Surface.Canvas;

            SKPaint hotPinkPaint = new SKPaint();
            SKPaint redPaint = new SKPaint();
            SKPaint bluePaint = new SKPaint();
            SKPaint greenPaint = new SKPaint();
            SKPaint yellowPaint = new SKPaint();

            SKColor skRed = SKColors.Red;
            SKColor skBlue = SKColors.Blue;
            SKColor skGreen = SKColors.Green;
            SKColor skYellow = SKColors.Yellow;
            SKColor skHotPink = SKColors.HotPink;

            SKRect rectangle;

            int skiaGridXLength = skiaGrid.GetLength(1);
            int skiaGridYLength = skiaGrid.GetLength(2);

            int canvasXWidth = (int)gridRendererSKCanvas.CanvasSize.Width;
            int canvasYHeight = (int)gridRendererSKCanvas.CanvasSize.Height;

            int xRatio = (canvasXWidth / skiaGridXLength);
            int yRatio = (canvasYHeight / skiaGridYLength);

            int xRatioHalf = xRatio / 2;
            int yRatioHalf = yRatio / 2;

            float rectCenterX = xRatioHalf;
            float rectCenterY = yRatioHalf;

            // iterating variables
            float leftCoord;
            float rightCoord;
            float topCoord;
            float bottomCoord;

            // These colors are used for debugging
            redPaint.Color = skRed;
            redPaint.StrokeWidth = 1;

            greenPaint.Color = skGreen;
            greenPaint.StrokeWidth = 1;

            yellowPaint.Color = skYellow;
            yellowPaint.StrokeWidth = 1;

            bluePaint.Color = skBlue;
            bluePaint.StrokeWidth = 1;

            // hotpink paintIterator means 'late stage draw failure'
            hotPinkPaint.Color = SKColors.HotPink;

            // Reset canvas color
            canvas.Clear(SKColors.DarkSlateGray);

            // Iterating key/value pair representing the current skiaGrid item we are iterating through.
            KeyValuePair<SKPoint, SKPaint> item = skiaGrid[xCursor, 0, 0];

            for (int x = 0; x < skiaGridXLength; x++)
            {
                for (int y = 0; y < skiaGridYLength; y++)
                {
                    try
                    {
                        item = skiaGrid[xCursor, x, y];

                        leftCoord = rectCenterX - rectSizeValue;
                        rightCoord = rectCenterX + rectSizeValue;
                        topCoord = rectCenterY - rectSizeValue;
                        bottomCoord = rectCenterY + rectSizeValue;

                        #region boundaryCorrections
                        if (leftCoord < 0)
                        {
                            leftCoord = 0;
                        }

                        if (rightCoord > canvasXWidth)
                        {
                            rightCoord = canvasXWidth;
                        }

                        if (topCoord < 0)
                        {
                            topCoord = 0;
                        }

                        if (bottomCoord > canvasYHeight)
                        {
                            bottomCoord = canvasYHeight;
                        }
                        #endregion

                        rectangle = new SKRect(leftCoord, topCoord, rightCoord, bottomCoord);

                        // if the value is null, draw pink (debug color). Else: the color assigned on discovery.

                        SKPaint currBlockPaint = item.Value;

                        e.Surface.Canvas.DrawRect(rectangle, currBlockPaint);

                        // rectCenterX/Y are our 'drawing cursors', while the iterating x/y are our 'matrix cursors'
                        rectCenterY += yRatio;
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine("Error in OnPaintSurfaceEventHandler!\n" + ex);
                    }
                }
                rectCenterY = yRatioHalf;
                rectCenterX += xRatio;
            }



            //canvas.DrawCircle(canvasCenterx, canvasCentery, 20, hotPinkPaint);
            canvas.DrawCircle(0, 0, 10, redPaint);
            canvas.DrawCircle((float)canvasXWidth, 0, 10, yellowPaint);
            canvas.DrawCircle(0, (float)canvasYHeight, 10, greenPaint);
            canvas.DrawCircle((float)canvasXWidth, (float)canvasYHeight, 10, bluePaint);

        }

        private void shipRenderScrollbar_ValueChanged(object sender, ScrollEventArgs e)
        {
            xCursor = (int)xCursorScrollbar.Value;
            gridRendererSKCanvas.InvalidateVisual();
        }
    }


}
