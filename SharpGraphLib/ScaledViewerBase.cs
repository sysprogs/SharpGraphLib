using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using System.Drawing.Design;

namespace SharpGraphLib
{
    public partial class ScaledViewerBase : UserControl
    {
        #region Appearance-related fields and constants
        public delegate void LabelFormatter(object Sender, double Value, ref string FormattedValue);
        public delegate void ValueTransformation(object Sender, bool forwardTransform, ref double Value);
        string _DefaultXFormat = "{0}", _DefaultYFormat = "{0}";
        Padding _AdditionalPadding;
        bool _AlwaysShowZeroX, _AlwaysShowZeroY, _CenterX, _CenterY;
        bool _AntiAlias = true;

        const int kDistanceForNonFittingPoints = 10000;

        GridSettings _XGrid = new GridSettings();
        GridSettings _YGrid = new GridSettings();

        const int SmallRulerDash = 5, BigRulerDash = 10, RulerTextSpacing = 4, MinDistanceBetweenLabels = 5;
        #endregion
        #region Designer properties
        [Category("Axes")]
        public event LabelFormatter FormatXValue, FormatYValue;
        [Category("Axes")]
        public event ValueTransformation TransformX, TransformY;


        [Category("Axes"), DefaultValue("{0}")]
        public string DefaultYFormat
        {
            get { return _DefaultYFormat; }
            set { _DefaultYFormat = value; UpdateScaling(); }
        }

        [Category("Axes"), DefaultValue("{0}")]
        public string DefaultXFormat
        {
            get { return _DefaultXFormat; }
            set { _DefaultXFormat = value; UpdateScaling(); }
        }

        [Category("Appearance"), DefaultValue(true)]
        public bool AntiAlias
        {
            get { return _AntiAlias; }
            set { _AntiAlias = value; Invalidate(); }
        }

        [Category("Axes"), DefaultValue(false)]
        public bool CenterY
        {
            get { return _CenterY; }
            set { _CenterY = value; UpdateScaling(); }
        }

        [Category("Axes"), DefaultValue(false)]
        public bool CenterX
        {
            get { return _CenterX; }
            set { _CenterX = value; UpdateScaling(); }
        }

        [Category("Axes"), DefaultValue(false)]
        public bool AlwaysShowZeroY
        {
            get { return _AlwaysShowZeroY; }
            set { _AlwaysShowZeroY = value; UpdateScaling(); }
        }

        [Category("Axes"), DefaultValue(false)]
        public bool AlwaysShowZeroX
        {
            get { return _AlwaysShowZeroX; }
            set { _AlwaysShowZeroX = value; UpdateScaling(); }
        }

        [Category("Appearance")]
        public System.Windows.Forms.Padding AdditionalPadding
        {
            get { return _AdditionalPadding; }
            set { _AdditionalPadding = value; UpdateScaling(); }
        }

        [Category("Axes")]
        public GridSettings XGrid
        {
            get { return _XGrid; }
            set { _XGrid = value; UpdateScaling(); }
        }

        [Category("Axes")]
        public GridSettings YGrid
        {
            get { return _YGrid; }
            set { _YGrid = value; }
        }
        #endregion
        #region Coordinate mapping functions
        public int MapX(double transformedValue)
        {
            int val = (int)Math.Round((transformedValue - TransformedBounds.MinX) * _DataRectangle.Width / TransformedBounds.DeltaX);
            FitValue(ref val, _DataRectangle.Left - kDistanceForNonFittingPoints, _DataRectangle.Right + kDistanceForNonFittingPoints);
            return _DataRectangle.Left + val;
        }

        public int MapWidth(double transformedValue)
        {
            int val = (int)Math.Round(((transformedValue) * _DataRectangle.Width) / TransformedBounds.DeltaX);
            if (val < 0)
                val = 0;
            if (val > _DataRectangle.Width)
                val = _DataRectangle.Width;
            return val;
        }

        public int MapY(double transformedValue)
        {
            int val = (int)Math.Round(((transformedValue - TransformedBounds.MinY) * _DataRectangle.Height) / TransformedBounds.DeltaY);
            FitValue(ref val, _DataRectangle.Top - kDistanceForNonFittingPoints, _DataRectangle.Bottom + kDistanceForNonFittingPoints);
            return _DataRectangle.Bottom - val;
        }

        protected struct GraphLocation
        {
            public double X, Y;

            public GraphLocation(double x, double y)
            {
                X = x;
                Y = y;
            }
        }

        protected struct ScreenLocation
        {
            public readonly int X, Y;

            public ScreenLocation(int x, int y)
            {
                X = x;
                Y = y;
            }
        }

        static void FitValue(ref int value, int min, int max)
        {
            if (value < min)
                value = min;
            if (value > max)
                value = max;
        }

        protected ScreenLocation[] MapCoordinates(GraphLocation[] transformedValues, GraphBounds fixedBounds, Rectangle fixedRect = default)
        {
            var bounds = fixedBounds.IsValid ? fixedBounds : TransformedBounds;
            var dataRect = fixedRect.Height > 0 ? fixedRect : _DataRectangle;

            int minX = dataRect.Left - kDistanceForNonFittingPoints, maxX = dataRect.Right + kDistanceForNonFittingPoints;
            int minY = dataRect.Top - kDistanceForNonFittingPoints, maxY = dataRect.Bottom + kDistanceForNonFittingPoints;

            ScreenLocation[] result = new ScreenLocation[transformedValues.Length];
            for (int i = 0; i < transformedValues.Length; i++)
            {
                int x = (int)Math.Round((transformedValues[i].X - bounds.MinX) * dataRect.Width / bounds.DeltaX);
                FitValue(ref x, minX, maxX);

                int y = (int)Math.Round(((transformedValues[i].Y - bounds.MinY) * dataRect.Height) / bounds.DeltaY);
                FitValue(ref y, minY, maxY);

                result[i] = new ScreenLocation(_DataRectangle.Left + x, dataRect.Bottom - y);
            }

            return result;
        }


        public int[] MapY(double[] transformedValues, GraphBounds fixedBounds, Rectangle fixedRect = default)
        {
            var bounds = fixedBounds.IsValid ? fixedBounds : TransformedBounds;
            var dataRect = fixedRect.Height > 0 ? fixedRect : _DataRectangle;

            int[] result = new int[transformedValues.Length];
            for (int i = 0; i < transformedValues.Length; i++)
            {
                int val = (int)Math.Round(((transformedValues[i] - bounds.MinY) * dataRect.Height) / bounds.DeltaY);
                if (val > dataRect.Bottom)
                    val = dataRect.Bottom + kDistanceForNonFittingPoints;
                if (val < 0)
                    val = -kDistanceForNonFittingPoints;
                result[i] = dataRect.Bottom - val;
            }
            return result;
        }

        public int MapY(double transformedValue, GraphBounds fixedBounds, Rectangle fixedRect = default)
        {
            if (!fixedBounds.IsValid)
                return MapY(transformedValue);

            var dataRect = fixedRect.Height > 0 ? fixedRect : _DataRectangle;

            int val = (int)Math.Round(((transformedValue - fixedBounds.MinY) * dataRect.Height) / fixedBounds.DeltaY);
            FitValue(ref val, _DataRectangle.Top - kDistanceForNonFittingPoints, _DataRectangle.Bottom + kDistanceForNonFittingPoints);
            return dataRect.Bottom - val;
        }

        public int MapHeight(double transformedValue)
        {
            int val = (int)Math.Round(((transformedValue) * _DataRectangle.Height) / TransformedBounds.DeltaY);
            if (val > _DataRectangle.Height)
                val = _DataRectangle.Height;
            if (val < 0)
                val = 0;
            return val;
        }

        public int MapX(double value, bool transform)
        {
            if (transform && (TransformX != null))
                TransformX(this, true, ref value);
            return MapX(value);
        }

        public int MapY(double value, bool transform)
        {
            if (transform && (TransformY != null))
                TransformY(this, true, ref value);
            return MapY(value);
        }

        protected ScreenLocation[] MapCoordinates(GraphLocation[] value, bool transform, GraphBounds fixedBounds, Rectangle fixedRect = default)
        {
            if (transform && (TransformX != null || TransformY != null))
            {
                if (TransformX != null)
                    for (int i = 0; i < value.Length; i++)
                        TransformX(this, true, ref value[i].X);
                if (TransformY != null)
                    for (int i = 0; i < value.Length; i++)
                        TransformY(this, true, ref value[i].Y);
            }

            return MapCoordinates(value, fixedBounds, fixedRect);
        }

        public int MapY(double value, bool transform, GraphBounds fixedBounds, Rectangle fixedRect = default)
        {
            if (transform && (TransformY != null))
                TransformY(this, true, ref value);
            return MapY(value, fixedBounds, fixedRect);
        }

        public double UnmapX(int value, bool reverseTransform)
        {
            double x = TransformedBounds.MinX + (value - _DataRectangle.Left) * TransformedBounds.DeltaX / _DataRectangle.Width;
            if (reverseTransform && (TransformX != null))
                TransformX(this, false, ref x);
            return x;
        }

        public double UnmapWidth(int value)
        {
            return (value) * TransformedBounds.DeltaX / _DataRectangle.Width;
        }

        public double UnmapY(int value, bool reverseTransform)
        {
            double y = TransformedBounds.MinY + (_DataRectangle.Bottom - value) * TransformedBounds.DeltaY / _DataRectangle.Height;
            if (reverseTransform && (TransformY != null))
                TransformY(this, false, ref y);
            return y;
        }

        public double UnmapHeight(int value)
        {
            return (value) * TransformedBounds.DeltaY / _DataRectangle.Height;
        }
        #endregion
        #region Grid-related objects
        [TypeConverter(typeof(ExpandableObjectConverter))]
        public class GridSettings
        {
            public enum GridSpacingKind
            {
                FixedSpacing,
                MaxLinesPerGraph,
                MinPixelsBetweenLines,
            }

            const double kDefaultSpacing = 20;

            GridSpacingKind _SpacingKind = GridSpacingKind.MinPixelsBetweenLines;
            double _SpacingParameter = kDefaultSpacing;
            double _SpacingDivider = 0;
            Color _LineColor = Color.DarkGray;
            float _LineWidth = 1;
            bool _ProportionalToTransformedScale = true;
            bool _ShowGridLines = true;
            bool _ShowLabels = true;
            bool _DistributeLabelsEvenly = true;
            bool _TransformLabelValues = true;
            #region Properties
            public bool ProportionalToTransformedScale
            {
                get { return _ProportionalToTransformedScale; }
                set { _ProportionalToTransformedScale = value; }
            }

            public bool TransformLabelValues
            {
                get { return _TransformLabelValues; }
                set { _TransformLabelValues = value; }
            }

            public bool ShowGridLines
            {
                get { return _ShowGridLines; }
                set { _ShowGridLines = value; }
            }

            public bool ShowLabels
            {
                get { return _ShowLabels; }
                set { _ShowLabels = value; }
            }

            [DefaultValue(true)]
            public bool DistributeLabelsEvenly
            {
                get { return _DistributeLabelsEvenly; }
                set { _DistributeLabelsEvenly = value; }
            }

            [DefaultValue(GridSpacingKind.MinPixelsBetweenLines)]
            public GridSpacingKind SpacingKind
            {
                get { return _SpacingKind; }
                set { _SpacingKind = value; }
            }
            [DefaultValue(kDefaultSpacing)]
            public double SpacingParameter
            {
                get { return _SpacingParameter; }
                set { _SpacingParameter = value; }
            }
            [DefaultValue(0.0)]
            public double SpacingDivider
            {
                get { return _SpacingDivider; }
                set { _SpacingDivider = value; }
            }
            public System.Drawing.Color LineColor
            {
                get { return _LineColor; }
                set { _LineColor = value; }
            }

            public bool ShouldSerializeLineColor()
            {
                return LineColor != Color.DarkGray;
            }

            [DefaultValue(1.0F)]
            public float LineWidth
            {
                get { return _LineWidth; }
                set { _LineWidth = value; }
            }
            #endregion
            public override string ToString()
            {
                switch (SpacingKind)
                {
                    case GridSpacingKind.FixedSpacing:
                        return string.Format("Every {0} unit(s)", SpacingParameter);
                    case GridSpacingKind.MaxLinesPerGraph:
                        return string.Format("Exactly {0} lines(s) per graph", (int)SpacingParameter);
                    case GridSpacingKind.MinPixelsBetweenLines:
                        return string.Format("Every {0} pixels", (int)SpacingParameter);
                    default:
                        return "(???)";
                }
            }

            internal GridDimension CreateDimensionObjectTemplate(double minTransformed, double maxTransformed, double minNonTransformed, double maxNonTransformed, int pixelRange)
            {
                double gridStart, gridSpacing, gridEnd;
                bool gridTransformed;

                gridStart = _ProportionalToTransformedScale ? minTransformed : minNonTransformed;
                gridEnd = _ProportionalToTransformedScale ? maxTransformed : maxNonTransformed;
                gridSpacing = 1;
                gridTransformed = _ProportionalToTransformedScale;
                switch (_SpacingKind)
                {
                    case GridSpacingKind.FixedSpacing:
                        gridSpacing = _SpacingParameter;
                        break;
                    case GridSpacingKind.MaxLinesPerGraph:
                        if (_ProportionalToTransformedScale)
                            gridSpacing = (maxTransformed - minTransformed) / ((_SpacingParameter == 0) ? 1 : _SpacingParameter);
                        else
                            gridSpacing = (maxNonTransformed - minNonTransformed) / ((_SpacingParameter == 0) ? 1 : _SpacingParameter);
                        break;
                    case GridSpacingKind.MinPixelsBetweenLines:
                        if (_ProportionalToTransformedScale)
                            gridSpacing = (_SpacingParameter * (maxTransformed - minTransformed)) / pixelRange;
                        else
                            gridSpacing = (_SpacingParameter * (minNonTransformed - minNonTransformed)) / pixelRange;
                        break;
                }

                if (_SpacingDivider != 0)
                {
                    gridStart = Math.Floor(gridStart / _SpacingDivider) * _SpacingDivider;
                    gridSpacing = Math.Ceiling(gridSpacing / _SpacingDivider) * _SpacingDivider;
                }

                if (gridSpacing <= 0)
                    gridSpacing = 1;

                List<GridLine> lines = new List<GridLine>();

                if (pixelRange > 0)
                {
                    for (double val = gridStart; val < gridEnd && lines.Count <= pixelRange; val += gridSpacing)
                        lines.Add(new GridLine { RawValue = val });
                }

                return new GridDimension { Transformed = gridTransformed, Data = lines.ToArray() };
            }

            internal Pen CreatePen()
            {
                return new Pen(_LineColor, _LineWidth);
            }
        }

        internal class GridLine
        {
            public double RawValue;
            public int ScreenCoordinate, TextCoordinate;
            public string Label;
            public int LabelSize;
            public bool LabelVisible;

            public override string ToString()
            {
                return RawValue.ToString();
            }
        }

        internal class GridDimension
        {
            public bool Transformed;
            public GridLine[] Data;

            internal void ComputeLabelVisibility(bool spaceEvenly, bool VerticalAxis)
            {
                int direction = VerticalAxis ? -1 : 1;
                if (Data.Length != 0)
                {
                    int minLabelPositionIncrement = 1;
                    for (; ; )
                    {
                        int nextAvailablePosition = Data[0].TextCoordinate + (Data[0].LabelSize + MinDistanceBetweenLabels) * direction;
                        int lastVisibleLabel = 0;

                        foreach (GridLine line in Data)
                            line.LabelVisible = false;

                        GridLine lastLine = Data[Data.Length - 1];
                        Data[0].LabelVisible = true;
                        lastLine.LabelVisible = true;

                        bool restart = false;
                        for (int i = minLabelPositionIncrement; i < (Data.Length - 1); i += minLabelPositionIncrement)
                        {
                            GridLine line = Data[i];
                            if (line.TextCoordinate * direction < nextAvailablePosition * direction)
                                continue;
                            if ((line.TextCoordinate + (line.LabelSize + MinDistanceBetweenLabels) * direction) * direction > lastLine.TextCoordinate * direction)
                                break;
                            line.LabelVisible = true;

                            if (spaceEvenly && (lastVisibleLabel != 0))
                            {
                                int thisIncrement = i - lastVisibleLabel;
                                if (thisIncrement > minLabelPositionIncrement)
                                {
                                    minLabelPositionIncrement = thisIncrement;
                                    restart = true;
                                    break;
                                }
                            }

                            nextAvailablePosition = line.TextCoordinate + (Data[i].LabelSize + MinDistanceBetweenLabels) * direction;
                            lastVisibleLabel = i;
                        }
                        if (!restart)
                            break;
                    }
                }
            }
        }

        GridDimension _XGridObj, _YGridObj;
        #endregion

        public ScaledViewerBase()
        {
            InitializeComponent();
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }

        Rectangle _DataRectangle;
        GraphBounds _NormalTransformedBounds;
        GraphBounds _ForcedBoundsAsRequested;   //Might contain NaN for the non-overridden directions. TransformedBounds contains the fully computed value.
        bool _ForceCustomBounds;

        public bool ForceCustomBounds
        {
            get { return _ForceCustomBounds; }
            set { _ForceCustomBounds = value; UpdateScaling(); }
        }

        public void ForceNewBounds(double minX, double maxX, double minY, double maxY)
        {
            if (TransformX != null)
            {
                TransformX(this, true, ref minX);
                TransformX(this, true, ref maxX);
            }
            if (TransformY != null)
            {
                TransformY(this, true, ref minY);
                TransformY(this, true, ref maxY);
            }

            _ForcedBoundsAsRequested = new GraphBounds(minX, maxX, minY, maxY);
            ForceCustomBounds = true;
        }


        protected GraphBounds NormalTransformedBounds => _NormalTransformedBounds;
        protected GraphBounds TransformedBounds
        {
            get; private set;
        }

        protected Rectangle DataRectangle
        {
            get { return _DataRectangle; }
        }

        public enum GridDimensionKind
        {
            Time,
            Value,
        }

        public struct GridLineDefinition
        {
            public double Value;
            public string ExplicitLabel;

            public override string ToString()
            {
                return Value.ToString();
            }
        }

        public class ComputeGridLinesEventArgs : EventArgs
        {
            public readonly GridDimensionKind Dimension;
            public readonly GridSettings GridSettings;
            public readonly int GraphAreaSizeInPixels;

            public double MinValue, MaxValue;       //Editable by the event handler
            public GridLineDefinition[] GridLines;  //Initially null. Should be set by the event handler.

            public ComputeGridLinesEventArgs(GridDimensionKind dimension, GridSettings gridSettings, double minValue, double maxValue, int graphAreaSizeInPixels)
            {
                Dimension = dimension;
                GridSettings = gridSettings;
                MinValue = minValue;
                MaxValue = maxValue;
                GraphAreaSizeInPixels = graphAreaSizeInPixels;
            }
        }

        [Category("Axes")]
        public event EventHandler<ComputeGridLinesEventArgs> ComputeGridLines;

        GraphBounds ComputeEffectiveForcedBounds()
        {
            GraphBounds forcedBounds = _ForcedBoundsAsRequested;
            if (double.IsNaN(forcedBounds.MinX))
                forcedBounds.MinX = _NormalTransformedBounds.MinX;
            if (double.IsNaN(forcedBounds.MaxX))
                forcedBounds.MaxX = _NormalTransformedBounds.MaxX;
            if (double.IsNaN(forcedBounds.MinY) && double.IsNaN(forcedBounds.MaxY))
            {
                double minX = forcedBounds.MinX, maxX = forcedBounds.MaxX;
                FillTransformedDynamicValueBounds(ref forcedBounds);
                forcedBounds.MinX = minX;
                forcedBounds.MaxX = maxX;
            }

            if (double.IsNaN(forcedBounds.MinY))
                forcedBounds.MinY = _NormalTransformedBounds.MinY;
            if (double.IsNaN(forcedBounds.MaxY))
                forcedBounds.MaxY = _NormalTransformedBounds.MaxY;

            return forcedBounds;
        }

        public virtual void UpdateScaling()
        {
            GraphBounds nonTransformedBounds;
            GraphBounds transformedBounds;
            GetRawDataBounds(out nonTransformedBounds, out transformedBounds);
            if (_AlwaysShowZeroX)
            {
                transformedBounds.MinX = Math.Min(0, transformedBounds.MinX);
                transformedBounds.MaxX = Math.Max(0, transformedBounds.MaxX);
            }
            if (_AlwaysShowZeroY)
            {
                transformedBounds.MinY = Math.Min(0, transformedBounds.MinY);
                transformedBounds.MaxY = Math.Max(0, transformedBounds.MaxY);
            }
            if (_CenterX)
            {
                double max = Math.Max(Math.Abs(transformedBounds.MinX), Math.Abs(transformedBounds.MaxX));
                transformedBounds.MinX = -max;
                transformedBounds.MaxX = max;
            }
            if (_CenterY)
            {
                double max = Math.Max(Math.Abs(transformedBounds.MinY), Math.Abs(transformedBounds.MaxY));
                transformedBounds.MinY = -max;
                transformedBounds.MaxY = max;
            }

            _NormalTransformedBounds = transformedBounds;
            TransformedBounds = _ForceCustomBounds ? ComputeEffectiveForcedBounds() : _NormalTransformedBounds;
            _DataRectangle = new Rectangle(_AdditionalPadding.Left, _AdditionalPadding.Top, Width - _AdditionalPadding.Horizontal - 1, Height - _AdditionalPadding.Vertical - 1);

            ComputeGridLinesEventArgs yGridArgs = null;
            if (ComputeGridLines != null)
            {
                var bounds = TransformedBounds;
                yGridArgs = new ComputeGridLinesEventArgs(GridDimensionKind.Value, _YGrid, bounds.MinY, bounds.MaxY, _DataRectangle.Height);
                ComputeGridLines(this, yGridArgs);

                if (yGridArgs.GridLines != null)
                {
                    _NormalTransformedBounds.MinY = yGridArgs.MinValue;
                    _NormalTransformedBounds.MaxY = yGridArgs.MaxValue;

                    bounds.MinY = yGridArgs.MinValue;
                    bounds.MaxY = yGridArgs.MaxValue;
                    TransformedBounds = bounds;
                }
            }

            if (_ForceCustomBounds)
            {
                transformedBounds = TransformedBounds;
                double minX = transformedBounds.MinX, minY = transformedBounds.MinY, maxX = transformedBounds.MaxX, maxY = transformedBounds.MaxY;
                if (TransformX != null)
                {
                    TransformX(this, false, ref minX);
                    TransformX(this, false, ref maxX);
                }
                if (TransformY != null)
                {
                    TransformY(this, false, ref minY);
                    TransformY(this, false, ref maxY);
                }
                nonTransformedBounds = new GraphBounds(minX, maxX, minY, maxY);
            }

            using (Graphics gr = Graphics.FromHwnd(Handle))
            {
                int yPadding = Size.Ceiling(gr.MeasureString("M", Font)).Height + BigRulerDash + RulerTextSpacing;
                if (!_XGrid.ShowLabels)
                    yPadding = 5;

                if (yGridArgs?.GridLines != null)
                {
                    _YGridObj = new GridDimension
                    {
                        Transformed = true,
                        Data = new GridLine[yGridArgs.GridLines.Length]
                    };

                    for (int i = 0; i < yGridArgs.GridLines.Length; i++)
                        _YGridObj.Data[i] = new GridLine { RawValue = yGridArgs.GridLines[i].Value, Label = yGridArgs.GridLines[i].ExplicitLabel };
                }
                else
                {
                    //Create list of grid points, screen coordinates will be assigned later
                    _YGridObj = _YGrid.CreateDimensionObjectTemplate(transformedBounds.MinY, transformedBounds.MaxY, nonTransformedBounds.MinY, nonTransformedBounds.MaxY, _DataRectangle.Height);
                }

                /* Grid/labels computation algorithm:
                *  1. Compute fixed Y padding (font height)
                *  2. Determine Y labels, place them, compute X padding (max. Y label width)
                *  3. Determine X labels, place them
                *  4. Compute label visibility
                */

                //Reflect Y padding only. Used by MapY()
                _DataRectangle = new Rectangle(_AdditionalPadding.Left, _AdditionalPadding.Top, Width - _AdditionalPadding.Horizontal - 1, Height - _AdditionalPadding.Vertical - 1 - yPadding);

                int maxLabelWidth = 0;
                for (int i = 0; i < _YGridObj.Data.Length; i++)
                {
                    GridLine line = _YGridObj.Data[i];
                    double rawVal = line.RawValue;
                    if (_YGrid.TransformLabelValues && !_YGrid.ProportionalToTransformedScale)
                        DoTransformY(ref rawVal);

                    if (line.Label == null)
                    {
                        string val = string.Format(_DefaultYFormat, rawVal);
                        FormatYValue?.Invoke(this, line.RawValue, ref val);
                        line.Label = val;
                    }

                    Size labelSize = Size.Ceiling(gr.MeasureString(line.Label, Font));
                    line.LabelSize = labelSize.Height;
                    maxLabelWidth = Math.Max(maxLabelWidth, labelSize.Width);

                    line.ScreenCoordinate = MapY(line.RawValue, !_YGridObj.Transformed);
                    if (i == 0)
                        line.TextCoordinate = Math.Min(line.ScreenCoordinate - line.LabelSize / 2, _DataRectangle.Bottom - line.LabelSize);
                    else if (i == (_YGridObj.Data.Length - 1))
                        line.TextCoordinate = Math.Max(line.ScreenCoordinate - line.LabelSize / 2, _DataRectangle.Top);
                    else
                        line.TextCoordinate = line.ScreenCoordinate - line.LabelSize / 2;
                }

                int xPadding = maxLabelWidth + BigRulerDash + RulerTextSpacing;
                if (!_YGrid.ShowLabels)
                    xPadding = 0;

                //Reflect both X and Y padding
                _DataRectangle = new Rectangle(_AdditionalPadding.Left + xPadding, _AdditionalPadding.Top, Width - _AdditionalPadding.Horizontal - 1 - xPadding, Height - _AdditionalPadding.Vertical - 1 - yPadding);

                _XGridObj = _XGrid.CreateDimensionObjectTemplate(transformedBounds.MinX, transformedBounds.MaxX, nonTransformedBounds.MinX, nonTransformedBounds.MaxX, _DataRectangle.Width);

                for (int i = 0; i < _XGridObj.Data.Length; i++)
                {
                    GridLine line = _XGridObj.Data[i];
                    double rawVal = line.RawValue;
                    if (_XGrid.TransformLabelValues && !_XGrid.ProportionalToTransformedScale)
                        DoTransformX(ref rawVal);

                    string val = string.Format(_DefaultXFormat, rawVal);
                    if (FormatXValue != null)
                        FormatXValue(this, line.RawValue, ref val);
                    line.Label = val;
                    line.LabelSize = Size.Ceiling(gr.MeasureString(val, Font)).Width;
                    line.LabelVisible = true;

                    line.ScreenCoordinate = MapX(line.RawValue, !_YGridObj.Transformed);
                    if (i == 0)
                        line.TextCoordinate = Math.Max(line.ScreenCoordinate - line.LabelSize / 2, _DataRectangle.Left);
                    else if (i == (_XGridObj.Data.Length - 1))
                        line.TextCoordinate = Math.Min(line.ScreenCoordinate - line.LabelSize / 2, _DataRectangle.Right - line.LabelSize);
                    else
                        line.TextCoordinate = line.ScreenCoordinate - line.LabelSize / 2;
                }

                _YGridObj.ComputeLabelVisibility(_YGrid.DistributeLabelsEvenly && _YGridObj.Transformed, true);
                _XGridObj.ComputeLabelVisibility(_XGrid.DistributeLabelsEvenly && _XGridObj.Transformed, false);
                Invalidate();
            }
        }

        protected virtual void GetRawDataBounds(out GraphBounds nonTransformedBounds, out GraphBounds transformedBounds)
        {
            nonTransformedBounds = new GraphBounds();
            transformedBounds = new GraphBounds();
        }

        protected virtual void FillTransformedDynamicValueBounds(ref GraphBounds bounds)
        {
        }

        protected override void OnResize(EventArgs e)
        {
            UpdateScaling();
            base.OnResize(e);
            Invalidate();
        }

        protected bool _SuppressYGridLabels;

        protected override void OnPaint(PaintEventArgs e)
        {
            if (_AntiAlias)
                e.Graphics.SmoothingMode = SmoothingMode.HighQuality;
            base.OnPaint(e);

            using (Pen borderPen = new Pen(ForeColor))
            using (Brush coordinateLabelBrush = new SolidBrush(this.ForeColor))
            using (Pen xGridPen = _XGrid.CreatePen())
            using (Pen yGridPen = _YGrid.CreatePen())
            {
                int textHeight = Size.Ceiling(e.Graphics.MeasureString("M", Font)).Height;
                if (_XGridObj != null)
                    foreach (GridLine line in _XGridObj.Data)
                    {
                        if (_XGrid.ShowGridLines)
                            e.Graphics.DrawLine(xGridPen, line.ScreenCoordinate, _DataRectangle.Top, line.ScreenCoordinate, _DataRectangle.Bottom);
                        if (_XGrid.ShowLabels)
                        {
                            if (line.LabelVisible)
                            {
                                e.Graphics.DrawString(line.Label, Font, coordinateLabelBrush, line.TextCoordinate, ClientRectangle.Bottom - _AdditionalPadding.Bottom - textHeight);
                                e.Graphics.DrawLine(borderPen, line.ScreenCoordinate, _DataRectangle.Bottom + 2, line.ScreenCoordinate, _DataRectangle.Bottom + BigRulerDash + 1);
                            }
                            else
                                e.Graphics.DrawLine(borderPen, line.ScreenCoordinate, _DataRectangle.Bottom + 2, line.ScreenCoordinate, _DataRectangle.Bottom + SmallRulerDash + 1);
                        }
                    }

                if (_YGridObj != null)
                    foreach (GridLine line in _YGridObj.Data)
                    {
                        if (_YGrid.ShowGridLines)
                            e.Graphics.DrawLine(yGridPen, _DataRectangle.Left, line.ScreenCoordinate, _DataRectangle.Right, line.ScreenCoordinate);
                        if (_YGrid.ShowLabels && !_SuppressYGridLabels)
                        {
                            if (line.LabelVisible)
                            {
                                e.Graphics.DrawString(line.Label, Font, coordinateLabelBrush, AdditionalPadding.Left, line.TextCoordinate);
                                e.Graphics.DrawLine(borderPen, _DataRectangle.Left - BigRulerDash, line.ScreenCoordinate, _DataRectangle.Left - 2, line.ScreenCoordinate);
                            }
                            else
                                e.Graphics.DrawLine(borderPen, _DataRectangle.Left - SmallRulerDash, line.ScreenCoordinate, _DataRectangle.Left - 2, line.ScreenCoordinate);
                        }
                    }

                e.Graphics.DrawRectangle(borderPen, _DataRectangle);
            }
        }

        #region Various helper methods
        protected void DoTransformX(ref double x)
        {
            if (TransformX != null)
                TransformX(this, true, ref x);
        }

        protected void DoTransformY(ref double y)
        {
            if (TransformY != null)
                TransformY(this, true, ref y);
        }

        #endregion

    }
}
