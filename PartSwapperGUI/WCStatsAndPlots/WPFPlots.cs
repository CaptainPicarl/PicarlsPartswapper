using PartSwapperXMLSE;
using ScottPlot.WPF;
using ScottPlot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace PartSwapperGUI.WCStatsAndPlots
{
    class WCStatsAndPlots
    {
        public static WpfPlot generateWeaponRangesScttPltWPF(List<I_WCDefinition> wcDefinitions, Color color1, Color color2, Color color3)
        {
            WpfPlot wepPlot = new WpfPlot();

            Tick[] ticks = new Tick[wcDefinitions.Count];

            ScottPlot.Bar currBar;
            ScottPlot.Plottables.Marker currMaxDistMarker;
            ScottPlot.Palettes.Penumbra spPenumbraPalette = new();

            PixelSize labelSize;

            float yAxisMax = 0f;
            float xAxisMax = 0f;

            float largestLabelWidth = 0f;

            float maxDistanceData = 0f;
            float minDistanceData = 0f;
            float stopTrackingSpeedData = 0f;

            List<double> xs = new List<double>();
            List<double> minDoubles = new List<double>();
            List<double> maxDoubles = new List<double>();

            wepPlot.Plot.Axes.Title.Label.Text = "Weapon Ranges";
            wepPlot.Plot.Axes.Title.Label.ForeColor = color1;

            wepPlot.Plot.Axes.Bottom.Label.Text = "Weapon Definition Name";
            wepPlot.Plot.Axes.Bottom.Label.ForeColor = color1;

            wepPlot.Plot.Axes.Bottom.MajorTickStyle.Color = color1;

            wepPlot.Plot.Axes.Left.Label.Text = "Distance (Meters)";
            wepPlot.Plot.Axes.Left.Label.ForeColor = color1;

            wepPlot.Plot.Axes.Left.MajorTickStyle.Color = color1;

            wepPlot.Plot.Axes.Margins(bottom: 0);

            wepPlot.Plot.DataBackground.Color = color2;
            wepPlot.Plot.FigureBackground.Color = color3;

            // Y-Axis Major LineStyle
            wepPlot.Plot.Grid.YAxisStyle.MajorLineStyle.Color = color1;
            wepPlot.Plot.Grid.YAxisStyle.MajorLineStyle.Width = 1.0f;
            wepPlot.Plot.Grid.YAxisStyle.MajorLineStyle.Pattern = LinePattern.Solid;

            wepPlot.Plot.Axes.Left.TickLabelStyle.ForeColor = color1;

            // Y-Axis Minor LineStyle
            wepPlot.Plot.Grid.YAxisStyle.MinorLineStyle.IsVisible = false;

            // X-Axis Minor LineStyle
            wepPlot.Plot.Grid.XAxisStyle.MajorLineStyle.Color = color2;
            wepPlot.Plot.Grid.XAxisStyle.MajorLineStyle.Width = 0.2f;
            wepPlot.Plot.Grid.XAxisStyle.MajorLineStyle.Pattern = LinePattern.DenselyDashed;

            // X-Axis Minor LineStyle
            wepPlot.Plot.Grid.XAxisStyle.MinorLineStyle.IsVisible = false;

            //Bottom Axes Tick Label Style
            wepPlot.Plot.Axes.Bottom.TickLabelStyle.Rotation = 90;
            wepPlot.Plot.Axes.Bottom.TickLabelStyle.Alignment = Alignment.MiddleLeft;
            wepPlot.Plot.Axes.Bottom.TickLabelStyle.FontSize = 16;
            wepPlot.Plot.Axes.Bottom.TickLabelStyle.BackgroundColor = color3;
            wepPlot.Plot.Axes.Bottom.TickLabelStyle.LineSpacing = 15f;
            wepPlot.Plot.Axes.Bottom.TickLabelStyle.BorderColor = color3;
            wepPlot.Plot.Axes.Bottom.TickLabelStyle.BorderWidth = 1;
            wepPlot.Plot.Axes.Bottom.TickLabelStyle.ForeColor = color1;
            wepPlot.Plot.Axes.Bottom.TickLabelStyle.PointColor = color1;
            wepPlot.Plot.Axes.Bottom.TickLabelStyle.PointSize = 2;

            // Legend stuff
            wepPlot.Plot.Legend.OutlineStyle.Color = color1;
            wepPlot.Plot.Legend.BackgroundColor = color3;
            wepPlot.Plot.Legend.FontColor = color1;
            wepPlot.Plot.Legend.FontSize = 12;

            wepPlot.Plot.Legend.Alignment = Alignment.MiddleRight;

            wepPlot.Plot.Legend.Orientation = Orientation.Vertical;
            wepPlot.Plot.Legend.IsVisible = false;

            foreach (I_WCDefinition definition in wcDefinitions)
            {
                // One more entry on the x-axis, so we increment the counter
                xAxisMax++;

                // Based off the type of definition (Weapon/Ammo/Armor)...
                switch (definition)
                {
                    case WCWeaponDefinition wcWepDef:


                        // Check if we have a value that exceeds our current yAxis boundary
                        if (yAxisMax < maxDistanceData)
                        {
                            yAxisMax = maxDistanceData;
                        }

                        // null-barrier.
                        // If our weapondefinition isn't null, and the targetingDefinition that is a member of the ammoDefinition isn't null...
                        if (wcWepDef != null && wcWepDef.targetingDefinition != null)
                        {

                            // Then assign min/max/stop data to the plot.

                            // Order matters here! Data needs assigned first!
                            maxDistanceData = WeaponcoreStats.assignDefaultOrValue<float>(wcWepDef.targetingDefinition.maxTargetDistance);
                            minDistanceData = WeaponcoreStats.assignDefaultOrValue<float>(wcWepDef.targetingDefinition.minTargetDistance);
                            stopTrackingSpeedData = WeaponcoreStats.assignDefaultOrValue<float>(wcWepDef.targetingDefinition.stopTrackingSpeed);

                        }

                        // Add a new tick in the tick array
                        ticks[(int)xAxisMax - 1] = new Tick(xAxisMax, wcWepDef.definitionName);

                        // Use the new tick to determine whether or not it has the largest label size
                        labelSize = wepPlot.Plot.Axes.Bottom.TickLabelStyle.Measure(ticks[(int)xAxisMax - 1].Label).Size;
                        largestLabelWidth = Math.Max(largestLabelWidth, labelSize.Width);

                        //Bar Plot
                        currBar = new Bar
                        {
                            ValueBase = minDistanceData,
                            Value = maxDistanceData,
                            FillColor = color1,
                            LineColor = color3,
                            Size = 0.6,
                            Position = xAxisMax,
                            LineWidth = 2,
                            Orientation = Orientation.Vertical
                        };

                        wepPlot.Plot.Add.Bar(currBar);

                        //Scatter Plot
                        //currMaxDistMarker = maxDistMarker(xAxisMax,maxDistanceData,wcWepDef.definitionName,);
                        //wepPlot.Plot.Add.Plottable(currMaxDistMarker);

                        //FillY's Plot
                        //xs.Add(xAxisMax);
                        //minDoubles.Add(minDistanceData);
                        //maxDoubles.Add(maxDistanceData);

                        //ScottPlot.Plottables.Scatter thisPlot = wepPlot.Plot.Add.Scatter(xAxisMax, maxDistanceData);
                        //thisPlot.Label = wcWepDef.definitionName;


                        break;
                    default: break;
                }

            }
            wepPlot.Plot.Axes.Bottom.MinimumSize = largestLabelWidth;
            wepPlot.Plot.Axes.Right.MinimumSize = largestLabelWidth;

            //Fill Y's Plot
            //ScottPlot.Plottables.FillY result = wepPlot.Plot.Add.FillY(xs.ToArray(),minDoubles.ToArray(),maxDoubles.ToArray());
            //result.FillStyle.Color = color1;



            CoordinateRange xRange = new CoordinateRange(0, xAxisMax);
            CoordinateRange yRange = new CoordinateRange(0, yAxisMax);

            ScottPlot.AxisLimits axisLimit = new AxisLimits(xRange, yRange);
            ScottPlot.AxisRules.MaximumBoundary maxBoundaryRule = new ScottPlot.AxisRules.MaximumBoundary(wepPlot.Plot.Axes.Bottom, wepPlot.Plot.Axes.Left, axisLimit);

            wepPlot.Plot.Axes.SetLimits(axisLimit);
            wepPlot.Plot.Axes.Rules.Add(maxBoundaryRule);

            //wepPlot.Refresh(); <-- Refresh() apparently doesnt exist in .NET 8


            // ticks has to be populated before this assignment. This is how tick labels are made.
            wepPlot.Plot.Axes.Bottom.TickGenerator = new ScottPlot.TickGenerators.NumericManual(ticks);

            return wepPlot;
        }

    }
}
