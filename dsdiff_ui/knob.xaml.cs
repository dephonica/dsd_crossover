using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace dsdiff_cross_ui_wpf
{
    public partial class Knob : UserControl
    {
        [System.Runtime.InteropServices.DllImportAttribute("user32.dll", EntryPoint = "SetCursorPos")]
        [return: System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.Bool)]
        public static extern bool SetCursorPos(int x, int y);

        private const double MouseGain = 0.3;

        private double _minValue = 0, _maxValue = 1, _minStep = 0.05;
        private double _value = 0.0, _valueFormatted = 0.0;
        private Point _initCursor, _initMouse;
        private bool _mouseCaptured = false;

        private int _remouseCount = 0;

        public delegate void DlgOnChange(object sender, double value);

        public event DlgOnChange OnChange;

        public double Min
        {
            set { _minValue = value; InvalidateVisual(); }
            get { return _minValue; }
        }

        public double Max
        {
            set { _maxValue = value; InvalidateVisual(); }
            get { return _maxValue; }
        }

        public double Step
        {
            set { _minStep = value; InvalidateVisual(); }
            get { return _minStep; }
        }

        public double Value
        {
            set { _value = value; UpdateFormattedValue(); InvalidateVisual(); }
            get { return _valueFormatted; }
        }

        public Knob()
        {
            InitializeComponent();
        }

        private void UserControlLoaded(object sender, RoutedEventArgs e)
        {
            UpdateFormattedValue();
        }

        private double scale(double value, double src, double dst)
        {
            return value * dst / src;
        }

        public static string FormatValue(double v)
        {
            var s = v.ToString("F1").TrimEnd('0').TrimEnd(',');
            if (v >= 1000)
            {
                var div = v / 1000;
                s = div.ToString("F2").TrimEnd('0').TrimEnd(',') + "K";
            }

            return s;
        }

        public void Parse(string value)
        {
            string s = value.Trim(), t = "";
            bool k = false, wasPoint = false, minus = false;

            for (var n = 0; n < s.Length; n++)
            {
                var c = s[n];
                if (c >= '0' && c <= '9') t += c;
                else if (c == '-' && n == 0) minus = true;
                else if ((c == 'k' || c == 'K') && n == s.Length - 1) k = true;
                else if (!wasPoint && (c == '.' || c == ','))
                {
                    t += '.';
                    wasPoint = true;
                }
            }

            if (t == "") t = "0";

            var val = double.Parse(t, CultureInfo.InvariantCulture);
            if (k) val *= 1000;
            if (minus) val = -val;

            _value = val;
            UpdateFormattedValue();
        }

        private void UpdateFormattedValue()
        {
            if (_value > _maxValue) _value = _maxValue;
            if (_value < _minValue) _value = _minValue;

            var prevValue = _valueFormatted;

            _valueFormatted = _value;

            if (_minStep > 0)
                _valueFormatted = Math.Truncate(_value / _minStep) * _minStep;

            if (Math.Abs(_valueFormatted - prevValue) > 0.000001)
                if (OnChange != null) OnChange(this, _valueFormatted);

            DispValue.Content = FormatValue(_valueFormatted);

            // calc rotation angle
            var angle = scale(_valueFormatted - _minValue, _maxValue - _minValue, 270);
            Pointer.RenderTransform = new RotateTransform(angle, ellipse1.ActualWidth / 2,
                ellipse1.ActualHeight / 2);
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            // Draw signs
            var center = new Point(ActualWidth/2 - 1, ActualHeight/2 + 4);
            var targetRadius = (ActualWidth / 2) - 6;
            const double notchesOffset = Math.PI *0;
            const double notchesArc = 4;

            for (var n = 0; n < 7; n++)
            {
                var v = scale(n, 6, (float)(_maxValue - _minValue));
                var s = FormatValue(_maxValue - v);

                var x = center.X + (float)((targetRadius) * Math.Sin(notchesOffset + n / notchesArc * Math.PI));
                var y = center.Y + (float)((targetRadius) * Math.Cos(notchesOffset + n / notchesArc * Math.PI));

                var formattedText = new
                    FormattedText(s, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, 
                        new Typeface("Segoe UI"), 8, Brushes.White) {TextAlignment = TextAlignment.Center};

                drawingContext.DrawText(formattedText, new Point(x + 1, y - 10));
            }
        }

        private void Ellipse1MouseDown(object sender, MouseButtonEventArgs e)
        {
            _initMouse = Mouse.GetPosition(this);
            _initCursor = PointToScreen(_initMouse);

            _mouseCaptured = true;
            Mouse.Capture(ellipse1);
            Mouse.OverrideCursor = Cursors.None;

            ellipse1.RenderTransform = new ScaleTransform(1, 1, ellipse1.ActualWidth / 2, ellipse1.ActualHeight / 2);
            ellipse0.RenderTransform = ellipse1.RenderTransform;

            var animation = new DoubleAnimation(1, 0.9, new Duration(TimeSpan.FromMilliseconds(200)));

            ellipse1.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty, animation);
            ellipse1.RenderTransform.BeginAnimation(ScaleTransform.ScaleYProperty, animation);
        }

        private void Ellipse1MouseUp(object sender, MouseButtonEventArgs e)
        {
            // End mouse capture
            if (_mouseCaptured)
            {
                _mouseCaptured = false;
                Mouse.Capture(null);

                Mouse.OverrideCursor = Cursors.Arrow;
                SetCursorPos((int)_initCursor.X, (int)_initCursor.Y);
            }

            ellipse1.RenderTransform = new ScaleTransform(1, 1, ellipse1.ActualWidth / 2, ellipse1.ActualHeight / 2);
            ellipse0.RenderTransform = ellipse1.RenderTransform;

            var animation = new DoubleAnimation(0.9, 1, new Duration(TimeSpan.FromMilliseconds(200)));

            ellipse1.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty, animation);
            ellipse1.RenderTransform.BeginAnimation(ScaleTransform.ScaleYProperty, animation);
        }

        private void Ellipse1MouseMove(object sender, MouseEventArgs e)
        {
            if (_mouseCaptured)
            {
                var location = Mouse.GetPosition(this);

                var diff = (_initMouse.Y - location.Y) * MouseGain;
                if (diff > 1) diff = 1;
                if (diff < -1) diff = -1;

                _value += (diff) * ((_maxValue - _minValue) / 128);
                if (_value > _maxValue) _value = _maxValue;
                if (_value < _minValue) _value = _minValue;
                UpdateFormattedValue();

                _remouseCount++;
                if (_remouseCount > 5)
                {
                    _remouseCount = 0;
                    SetCursorPos((int)_initCursor.X, (int)_initCursor.Y);
                    _initMouse = PointFromScreen(_initCursor);
                }
                else _initMouse.Y = location.Y;
            }
        }

        private void Ellipse1MouseEnter(object sender, MouseEventArgs e)
        {
            if (ellipse1.Opacity < 1.0)
                ellipse1.BeginAnimation(OpacityProperty, 
                    new DoubleAnimation(0.6, 1, new Duration(TimeSpan.FromMilliseconds(100))));
        }

        private void Ellipse1MouseLeave(object sender, MouseEventArgs e)
        {
            if (ellipse1.Opacity > 0.6)
                ellipse1.BeginAnimation(OpacityProperty,
                    new DoubleAnimation(1, 0.6, new Duration(TimeSpan.FromMilliseconds(100))));
        }
    }
}
