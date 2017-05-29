using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace dsdiff_cross_ui_wpf
{
    public partial class edit_panel : UserControl
    {
        private Grid _parent = null;
        private int _column, _span, _flowedTo;
        private readonly double []_columnWidths = new double[64];

        public object Active { set; get; }

        public delegate void DlgOnChange(object sender, object active, string value);
        public delegate object DlgOnFlowing(object sender, int idx);

        public event DlgOnChange OnChange;
        public event DlgOnFlowing OnFlowing;

        public string Value
        {
            set { textBox1.Text = value; }
            get { return textBox1.Text; }
        }

        public edit_panel()
        {
            InitializeComponent();

            textBox1.KeyDown += TextBox1KeyDown;
        }

        void TextBox1KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                if (OnChange != null) OnChange(this, Active, Value);

            if (e.Key == Key.Tab)
            {
                var next = _flowedTo;

                if (Opacity < 1.0)
                    MyAnimations.AnimateOpacity(this, 0, 1, 100);
                else
                {
                    next = _flowedTo + 1;
                    if (next >= _span) next = 0;
                }

                FlowBox(next);
            }
        }

        private void UserControlLoaded(object sender, RoutedEventArgs e)
        {
            if (Parent.GetType() != typeof (Grid)) return;

            _parent = (Grid) Parent;

            _column = Grid.GetColumn(this);
            _span = Grid.GetColumnSpan(this);

            for (var n = 0; n < _span; n++)
                _columnWidths[n] = _parent.ColumnDefinitions[_column + n].ActualWidth;

            FlowBox(0, false);
        }

        public void FlowBox(int flowTo, bool animated = true)
        {
            _flowedTo = flowTo;

            if (OnFlowing != null) Active = OnFlowing(this, flowTo);

            var current = Input.Margin.Left;
            var target = 0.0;

            for (var n = 0; n < flowTo; n++)
                target += _columnWidths[n];

            target += (_columnWidths[flowTo] - Input.ActualWidth)/2;

            if (animated)
                Input.BeginAnimation(MarginProperty,
                    new ThicknessAnimation(
                        new Thickness(current, 5, 0, 0),
                        new Thickness(target, 5, 0, 0),
                        new Duration(TimeSpan.FromMilliseconds(200))));
            else
                Input.Margin = new Thickness(target, 5, 0, 0);
        }
    }
}
