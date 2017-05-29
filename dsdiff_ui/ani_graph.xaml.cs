using System;
using System.Collections.Generic;
using System.Drawing;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using Pen = System.Windows.Media.Pen;
using Point = System.Windows.Point;
using FontFamily = System.Windows.Media.FontFamily;

namespace dsdiff_cross_ui_wpf
{
    public partial class AniGraph : UserControl, IDisposable
    {
        private const int AniTimerInterval = 20;

        private readonly object _globalLock = new object();

        private Rect _graphRect;

        private readonly List<Tuple<PointF[], Color, string>> _points =
            new List<Tuple<PointF[], Color, string>>();

        private readonly double[] _aniScale = {1.0, 1.0, 1.0, 1.0};
        private readonly Timer _aniTimer = new Timer(AniTimerInterval);

        private readonly Tuple<int, Color>[] _freqAreas =
            {
                new Tuple<int, Color>(192000, Colors.Snow),
                new Tuple<int, Color>(96000, Colors.Linen),
                new Tuple<int, Color>(44100, Colors.Lavender),
            };

        // Edit mode variables
        private const int EdgeSize = 50;
        private double[] _edgeData = new double[EdgeSize];
        private const double EditScale = 0.8;

        private readonly List<Tuple<FilterEdit.FilterType, int, int, Color>> _filters = 
            new List<Tuple<FilterEdit.FilterType, int, int, Color>>();

        private double _mousePosition = -1;

        /////////////////////////////////////////////////////////////////
        // Properties

        public int AnimationInterval { get; set; }
        public int TopFrequency { get; set; }
        public int MouseFrequency { get; private set; }
        public int SelectedFilter { get; private set; }

        public enum GraphType
        {
            Display,
            Edit
        };

        public GraphType GraphRenderType { get; set; }

        public AniGraph()
        {
            InitializeComponent();

            GraphRenderType = GraphType.Display;
            TopFrequency = 200000;
            AnimationInterval = 300;

            lock (_globalLock)
            {
                FillEdgeData();

                var rnd = new Random();

                _points.Add(new Tuple<PointF[], Color, string>(new PointF[50], Colors.LightSteelBlue, "Test data"));

                for (var n = 0; n < 50; n++)
                    _points[0].Item1[n] = new PointF(n, (float) rnd.NextDouble());
            }

            _aniTimer.Elapsed += AniTimerElapsed;
            _aniTimer.Start();

            IsVisibleChanged += AniGraphIsVisibleChanged;
        }

        public void Dispose()
        {
            _aniTimer.Stop();
        }

        public void AddFilterDef(FilterEdit.FilterType filterType, int freq1, int freq2, Color color)
        {
            lock (_globalLock)
            {
                _filters.Add(new Tuple<FilterEdit.FilterType, int, int, Color>(filterType, freq1, freq2, color));
                InvalidateVisual();
            }
        }

        public void EditFilterDef(int index, FilterEdit.FilterType filterType, int freq1, int freq2, Color color)
        {
            lock (_globalLock)
            {
                _filters[index] =  new Tuple<FilterEdit.FilterType, int, int, Color>(filterType, freq1, freq2, color);
                InvalidateVisual();
            }
        }

        public void DeleteFilterDef(int index)
        {
            lock (_globalLock)
            {
                _filters.Remove(_filters[index]);
                InvalidateVisual();
            }
        }

        public int GetFiltersCount()
        {
            lock (_globalLock)
            {
                return _filters.Count;
            }
        }

        public Tuple<FilterEdit.FilterType, int, int, Color> GetFilterDef(int index)
        {
            lock (_globalLock)
            {
                return _filters[index];
            }
        }

        private void FillEdgeData()
        {
            for (var n = 0; n < EdgeSize; n++)
                _edgeData[n] = (1 + Math.Cos((double)n/EdgeSize*Math.PI)) / 2;
        }

        private void AniTimerElapsed(object sender, ElapsedEventArgs e)
        {
            var doUpdate = false;

            lock (_globalLock)
            {
                var scaleInc = 1.0/(AnimationInterval/_aniTimer.Interval);

                for (var n = 0; n < _aniScale.Length; n++)
                    if (_aniScale[n] < 1.0)
                    {
                        _aniScale[n] += scaleInc;
                        doUpdate = true;
                    }
            }

            if (doUpdate)
            {
                try
                {
                    Dispatcher.Invoke(InvalidateVisual, DispatcherPriority.Render);
                }
                catch
                {
                }
            }
        }

        public void SetPoints(int index, PointF[] points, Color color, string legendName)
        {
            lock (_globalLock)
            {
                while (_points.Count <= index) _points.Add(new Tuple<PointF[], Color, string>(null, Colors.Black, ""));

                _points[index] = new Tuple<PointF[], Color, string>(points, color, legendName);
                _aniScale[index] = 0.0;

                Dispatcher.Invoke(InvalidateVisual, DispatcherPriority.Render);
            }
        }

        public Tuple<PointF[], Color, string> GetPoints(int index)
        {
            lock (_globalLock)
            {
                return _points[index];
            }
        }

        private void AniGraphIsVisibleChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            for (var n = 0; n < 4; n++)
                _aniScale[n] = 0.0;
        }

        private void RenderCommonLayout(Rect rect, DrawingContext drawingContext)
        {
            drawingContext.DrawLine(new Pen(Brushes.Silver, 1.5), new Point(rect.Left, rect.Top),
                                    new Point(rect.Left, rect.Top + rect.Height));
            drawingContext.DrawLine(new Pen(Brushes.Silver, 1.5), new Point(rect.Left, rect.Top + rect.Height),
                                    new Point(rect.Left + rect.Width, rect.Top + rect.Height));

            var courier = new FontFamily("Segoe");
            var courierTypeface = new Typeface(courier, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
            var formattedText = new FormattedText("Frequency",
                                                  System.Globalization.CultureInfo.CurrentCulture,
                                                  FlowDirection.LeftToRight,
                                                  courierTypeface,
                                                  10.0,
                                                  Brushes.White);
            drawingContext.DrawText(formattedText,
                new Point(rect.Right - formattedText.Width,
                    rect.Bottom + 4));

            formattedText = new FormattedText("Level",
                                                  System.Globalization.CultureInfo.CurrentCulture,
                                                  FlowDirection.LeftToRight,
                                                  courierTypeface,
                                                  10.0,
                                                  Brushes.White);
            drawingContext.PushTransform(new RotateTransform(-90));
            drawingContext.DrawText(formattedText,
                new Point(-rect.Top - formattedText.Width, rect.Left - 14));
            drawingContext.Pop();
        }

        private void RenderDisplayGraph(Rect rect, DrawingContext drawingContext)
        {
            foreach (var freqArea in _freqAreas)
            {
                var width = (double) freqArea.Item1/TopFrequency*rect.Width;

                drawingContext.DrawRectangle(new SolidColorBrush(freqArea.Item2), null,
                                             new Rect(rect.Left, rect.Top, width, rect.Height));
            }

            var courier = new FontFamily("Segoe");
            var courierTypeface = new Typeface(courier, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);

            for (var n = 0; n < _points.Count && n < _aniScale.Length; n++)
            {
                if (_points[n].Item1.Length < 1) continue;

                var formattedText = new FormattedText(_points[n].Item3,
                                                  System.Globalization.CultureInfo.CurrentCulture,
                                                  FlowDirection.LeftToRight,
                                                  courierTypeface,
                                                  10.0,
                                                  Brushes.White);

                drawingContext.DrawText(formattedText,
                                        new Point(rect.Right - 50, rect.Bottom + (n - _points.Count - 1) * formattedText.Height));

                drawingContext.DrawRectangle(new SolidColorBrush(_points[n].Item2), null,
                                             new Rect(rect.Right - 70, rect.Bottom + (n - _points.Count - 1) * formattedText.Height,
                                                      formattedText.Height, formattedText.Height));

                double min = 99999999, max = -99999999;
                foreach (var p in _points[n].Item1)
                {
                    if (p.Y < min) min = p.Y;
                    if (p.Y > max) max = p.Y;
                }

                var biasX = _points[n].Item1[0].X;
                var scaleX = rect.Width/_points[n].Item1.Length;

                var biasY = min;
                var scaleY = (max - min)/(rect.Height - 5);

                var prevPoint = new Point(rect.Left,
                                          rect.Top + rect.Height - 1 -
                                          (_points[n].Item1[0].Y - biasY)/scaleY*_aniScale[n]);
                var startPoint = prevPoint;

                var points = new List<Point>();

                foreach (var p in _points[n].Item1)
                {
                    var curPoint = new Point(rect.Left + (p.X - biasX)*scaleX,
                                             rect.Top + rect.Height - 1 - (p.Y - biasY)/scaleY*_aniScale[n]);
                    points.Add(curPoint);
                    prevPoint = curPoint;
                }

                var figures = new PathSegmentCollection();
                var segment = new PolyQuadraticBezierSegment(points, true);
                figures.Add(segment);

                var figure = new PathFigure(startPoint, figures, false);
                var geometry = new PathGeometry();
                geometry.Figures.Add(figure);

                drawingContext.DrawGeometry(null, new Pen(new SolidColorBrush(_points[n].Item2), 2), geometry);
            }

            for (var n = 0; n < _freqAreas.Length; n++)
            {
                var formattedText = new FormattedText(_freqAreas[n].Item1.ToString("N0"),
                                                  System.Globalization.CultureInfo.CurrentCulture,
                                                  FlowDirection.LeftToRight,
                                                  courierTypeface,
                                                  10.0,
                                                  Brushes.White);

                drawingContext.DrawText(formattedText,
                                        new Point(rect.Right - 50, rect.Top + n*formattedText.Height));

                drawingContext.DrawRectangle(new SolidColorBrush(_freqAreas[n].Item2), null,
                                             new Rect(rect.Right - 70, rect.Top + n*formattedText.Height,
                                                      formattedText.Height, formattedText.Height));
            }
        }

        private void RenderEditGraph(Rect rect, DrawingContext drawingContext)
        {
            drawingContext.DrawRectangle(new SolidColorBrush(Color.FromArgb(1,0,0,0)), null, _graphRect);

            SelectedFilter = -1;
            var points = new List<Point>();

            // Draw defined filters
            for (var n = 0; n < 4; n++)
                DrawFilterInfo(drawingContext, n, null, true);

            var filterIndex = 0;
            foreach (var filter in _filters)
            {
                DrawFilterInfo(drawingContext, filterIndex, filter, false);

                points.Clear();

                if (filter.Item1 == FilterEdit.FilterType.LowPass)
                {
                    var x1 = filter.Item2 * rect.Width / TopFrequency;

                    if (_mousePosition < x1 + EdgeSize) SelectedFilter = filterIndex;

                    points.Add(new Point(rect.Left, rect.Bottom));
                    points.Add(new Point(rect.Left, rect.Top - 1 + rect.Height * (1 - EditScale)));

                    for (var n = 0; n < EdgeSize; n++)
                        points.Add(new Point(rect.Left + x1 + n,
                                                 rect.Bottom - 1 - _edgeData[n] * rect.Height * EditScale));
                } else if (filter.Item1 == FilterEdit.FilterType.HighPass)
                {
                    var x1 = filter.Item2 * rect.Width / TopFrequency;
                    if (x1 + EdgeSize > rect.Width)
                        x1 = rect.Width - EdgeSize;

                    if (_mousePosition > x1) SelectedFilter = filterIndex;

                    for (var n = 0; n < EdgeSize; n++)
                        points.Add(new Point(rect.Left + x1 + n,
                                                 rect.Bottom - 1 - _edgeData[EdgeSize - n - 1] * rect.Height * EditScale));

                    points.Add(new Point(rect.Right, rect.Top - 1 + rect.Height * (1 - EditScale)));
                    points.Add(new Point(rect.Right, rect.Bottom));
                } else if (filter.Item1 == FilterEdit.FilterType.BandPass)
                {
                    var x1 = filter.Item2*rect.Width/TopFrequency;
                    var x2 = filter.Item3*rect.Width/TopFrequency;

                    if (x1 + EdgeSize > x2)
                        x2 = x1 + EdgeSize;

                    if (_mousePosition > x1 && _mousePosition < x2 + EdgeSize)
                        SelectedFilter = filterIndex;

                    for (var n = 0; n < EdgeSize; n++)
                        points.Add(new Point(rect.Left + x1 + n,
                                                 rect.Top - 1 + rect.Height * (1 - EditScale) + _edgeData[n] * rect.Height * EditScale));

                    for (var n = 0; n < EdgeSize; n++)
                        points.Add(new Point(rect.Left + x2 + n,
                                                 rect.Top - 1 + rect.Height * (1 - EditScale) + _edgeData[EdgeSize - n - 1] * rect.Height * EditScale));
                } else if (filter.Item1 == FilterEdit.FilterType.BandStop)
                {
                    var x1 = filter.Item2 * rect.Width / TopFrequency;
                    var x2 = filter.Item3 * rect.Width / TopFrequency;

                    if (x1 + EdgeSize > x2)
                        x2 = x1 + EdgeSize;

                    if (_mousePosition < x1 || _mousePosition > x2 + EdgeSize)
                        SelectedFilter = filterIndex;

                    points.Add(new Point(rect.Left, rect.Bottom));
                    points.Add(new Point(rect.Left, rect.Top - 1 + rect.Height * (1 - EditScale)));

                    for (var n = 0; n < EdgeSize; n++)
                        points.Add(new Point(rect.Left + x1 + n,
                                                 rect.Bottom - 1 - _edgeData[n] * rect.Height * EditScale));

                    for (var n = 0; n < EdgeSize; n++)
                        points.Add(new Point(rect.Left + x2 + n,
                                                 rect.Bottom - 1 - _edgeData[EdgeSize - n - 1] * rect.Height * EditScale));

                    points.Add(new Point(rect.Right, rect.Top - 1 + rect.Height * (1 - EditScale)));
                    points.Add(new Point(rect.Right, rect.Bottom));
                }

                var drawColor = filter.Item4;
                if (filterIndex == SelectedFilter)
                    drawColor = Color.FromArgb(255, filter.Item4.R, filter.Item4.G, filter.Item4.B);

                if (points.Count > 0)
                    DrawPoints(drawingContext, points[0], points.ToArray(), drawColor, false);

                filterIndex++;
            }

            // Draw mouse pointer
            if (SelectedFilter == -1 && _filters.Count < 4)
            {
                points.Clear();

                for (var n = 0; n < EdgeSize; n++)
                    points.Add(new Point(rect.Left + _mousePosition - EdgeSize * 1.5 + n,
                                             rect.Top - 1 + rect.Height * (1 - EditScale) +
                                             _edgeData[n] * rect.Height * EditScale));

                for (var n = 0; n < EdgeSize; n++)
                    points.Add(new Point(rect.Left + _mousePosition + EdgeSize / 2 + n,
                                             rect.Top - 1 + rect.Height * (1 - EditScale) +
                                             _edgeData[EdgeSize - n - 1] * rect.Height * EditScale));

                DrawPoints(drawingContext, points[0], points.ToArray(), Color.FromArgb(200,192,192,192));
            }
        }

        private void DrawFilterInfo(DrawingContext drawingContext, int index, 
            Tuple<FilterEdit.FilterType, int, int, Color> filter, bool placeOnly)
        {
            var color = Color.FromArgb(30,192,192,192);
            if (!placeOnly) color = filter.Item4;

            var rect = new Rect(20 + (ActualWidth - 20) / 4 * index, _graphRect.Top - 35,
                (ActualWidth - 20) / 4 - 20, 30);
            drawingContext.DrawRectangle(new SolidColorBrush(color), null, rect);

            if (placeOnly) return;

            var text = filter.Item1.ToString() + ", " + filter.Item2 + "Hz";
            if (filter.Item1 == FilterEdit.FilterType.BandPass ||
                filter.Item1 == FilterEdit.FilterType.BandStop)
                text += " - " + filter.Item3 + "Hz";

            var courier = new FontFamily("Segoe");
            var courierTypeface = new Typeface(courier, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);

            var formattedText = new FormattedText(text,
                                              System.Globalization.CultureInfo.CurrentCulture,
                                              FlowDirection.LeftToRight,
                                              courierTypeface,
                                              14.0,
                                              Brushes.White);

            drawingContext.DrawText(formattedText,
                                    new Point(rect.Left + (rect.Width - formattedText.Width) / 2, 
                                        rect.Top + (rect.Height - formattedText.Height) / 2));

        }

        private void DrawPoints(DrawingContext drawingContext, Point startPoint, IEnumerable<Point> points, Color color, bool bezier = true)
        {
            var figures = new PathSegmentCollection();
            if (bezier)
            {
                var segment = new PolyQuadraticBezierSegment(points, true);
                figures.Add(segment);
            }
            else
            {
                var segment = new PolyLineSegment(points, true);
                figures.Add(segment);
            }

            var figure = new PathFigure(startPoint, figures, false);
            var geometry = new PathGeometry();
            geometry.Figures.Add(figure);

            drawingContext.DrawGeometry(new SolidColorBrush(color), new Pen(new SolidColorBrush(Colors.Snow), 2), geometry);
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            lock (_globalLock)
            {
                var rect = new Rect(50, 40, (float)ActualWidth - 100, (float)ActualHeight - 60);
                _graphRect = rect;

                if (GraphRenderType == GraphType.Display)
                    RenderDisplayGraph(rect, drawingContext);
                else if (GraphRenderType == GraphType.Edit)
                    RenderEditGraph(rect, drawingContext);

                RenderCommonLayout(rect, drawingContext);
            }
        }

        protected override void OnMouseMove(System.Windows.Input.MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (GraphRenderType != GraphType.Edit) return;

            var position = e.GetPosition(this);

            lock (_globalLock)
            {
                if (position.X < _graphRect.Left || position.X > _graphRect.Right ||
                    position.Y < _graphRect.Top || position.Y > _graphRect.Bottom)
                {
                    if (_mousePosition >= 0)
                    {
                        _mousePosition = -1;
                        Dispatcher.Invoke(InvalidateVisual, DispatcherPriority.Render);
                    }

                    return;
                }

                var currentMousePosition = position.X - _graphRect.Left;

                if (Math.Abs(currentMousePosition - _mousePosition) > 0.1)
                {
                    _mousePosition = currentMousePosition;
                    MouseFrequency = (int)(currentMousePosition*TopFrequency/_graphRect.Width);
                    Dispatcher.Invoke(InvalidateVisual, DispatcherPriority.Render);
                }
            }
        }
    }
}
