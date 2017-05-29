using System;
using System.Windows;
using System.Windows.Media;

namespace dsdiff_cross_ui_wpf
{
    public partial class Graph
    {
        private double _min, _max, _minv, _maxv;

        public delegate double DlgExpression(object sender, double t);

        private DlgExpression _expression = null;

        private readonly DrawingVisual _content = new DrawingVisual();

        public DlgExpression Expression
        {
            set { _expression = value; RecalcContent(); }
            get { return _expression; }
        }

        public bool AutoBounds { set; get; }

        public Pen GraphPen { set; get; }
        public Pen AxisPen { set; get; }

        public bool AxisXVisible { set; get; }
        public bool AxisYVisible { set; get; }

        public double Min
        {
            set 
            { 
                _min = value;
                if (AutoBounds) RecalcContent();
            }

            get { return _min; }
        }

        public double Max
        {
            set
            {
                _max = value;
                if (AutoBounds) RecalcContent();
            }

            get { return _max; }
        }

        public double MinValue
        {
            set
            {
                if (!AutoBounds)
                {
                    _minv = value;
                    RecalcContent();
                }
            }

            get { return _minv; }
        }

        public double MaxValue
        {
            set
            {
                if (!AutoBounds)
                {
                    _maxv = value;
                    RecalcContent();
                }
            }

            get { return _maxv; }
        }

        public Graph()
        {
            InitializeComponent();

            AxisPen = new Pen(Brushes.Black, 2);
            GraphPen = new Pen(Brushes.DimGray, 1);
        }

        private void UserControlLoaded(object sender, RoutedEventArgs e)
        {
            RecalcContent();
        }

        private double Scale(double value, double src, double dst)
        {
            return value * dst / src;
        }

        private void RecalcContent()
        {
            if (_expression == null) return;

            var previous = new Point(0,0);

            var rc = _content.RenderOpen();

            var margin = 3;

            var h = ActualHeight - margin * 2;
            var w = ActualWidth - margin * 2;

            if (AxisXVisible)
            {
                var yax = margin + h - Scale(0 - _minv, _maxv - _minv, h);
                rc.DrawLine(AxisPen, new Point(margin, yax), 
                    new Point(margin + w, yax));
            }

            if (AxisYVisible)
            {
                var yax = margin + Scale(0 - _min, _max - _min, w);
                rc.DrawLine(AxisPen, new Point(yax, margin),
                    new Point(yax, margin + h));
            }

            if (AutoBounds)
            {
                _minv = 99999999999;
                _maxv = -99999999999;
                for (var i = 0; i < (int)w; i++)
                {
                    var v = _expression(this, _min + Scale(i, w, _max - _min));
                    if (v < _minv) _minv = v;
                    if (v > _maxv) _maxv = v;
                }

                _minv -= 0.01*(_maxv - _minv);
                _maxv += 0.01*(_maxv - _minv);
            }

            for (var i = 0; i < (int)w + 10; i += 8)
            {
                if (i > w) i = (int)w;

                var v = _expression(this, _min + Scale(i, w, _max - _min));
                var r = margin + h - Scale(v - _minv, _maxv - _minv, h);

                var current = new Point(margin + i, r);

                if (i == 0) previous = current;

                if (r > margin && r < h) 
                    rc.DrawLine(GraphPen, previous, current);

                previous = current;

                if (i == (int)w) break;
            }

            rc.Close();

            InvalidateVisual();
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);

            RecalcContent();
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            drawingContext.DrawDrawing(_content.Drawing);
        }
    }
}
