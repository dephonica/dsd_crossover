using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace dsdiff_cross_ui_wpf
{
    public partial class FilterEdit : Window
    {
        private bool _enterCatched = false;

        public delegate void DlgOnApplyClick(object sender);
        public DlgOnApplyClick ApplyClick;

        public delegate void DlgOnCancelClick(object sender);
        public DlgOnApplyClick CancelClick;

        public delegate void DlgOnDeleteClick(object sender);
        public DlgOnDeleteClick DeleteClick;

        public int _enterCounter = 0;

        public enum FilterType
        {
            LowPass = 0,
            HighPass,
            BandPass,
            BandStop
        };

        private FilterType _typeOfFilter;
        public FilterType TypeOfFilter
        {
            get { return _typeOfFilter; }
            set
            {
                _typeOfFilter = value; 
                Roundcombo2.SelectedItem = (int) value; 
            }
        }

        public bool EditMode { get; set; }

        private int _frequencyOne, _frequencyTwo;

        public int FrequencyOne
        {
            get { return _frequencyOne; }
            set
            {
                _frequencyOne = value;
                knobLowFreq.Value = value;
                knobCutOff.Value = value;
            }
        }

        public int FrequencyTwo
        {
            get { return _frequencyTwo; }
            set
            {
                _frequencyTwo = value;
                knobHiFreq.Value = value;
            }
        }

        public FilterEdit()
        {
            InitializeComponent();

            BackGrid.Opacity = 0;
            SurfGrid.Opacity = 0;

            Roundcombo2.Items = new List<string>(
                new[] { "Low-pass", "High-pass", "Band-pass", "Band-stop" });
        }

        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            DeleteFilter.Visibility = EditMode ? Visibility.Visible : Visibility.Hidden;

            MyAnimations.AnimateProcessingWindowLoad(BackGrid, SurfGrid, ActualHeight);

            edit_panel1.Opacity = 0.0;
            edit_panel1.FlowBox(0, false);
            edit_panel1.Value = Knob.FormatValue(knobCutOff.Value);
            edit_panel1.Active = knobCutOff;
            edit_panel1.OnChange += edit_panel_OnChange;
            edit_panel1.OnFlowing += edit_panel_OnFlowing;
            edit_panel1.textBox1.Focus();

            edit_panel2.Opacity = 0.0;
            edit_panel2.FlowBox(0, false);
            edit_panel2.Value = Knob.FormatValue(knobLowFreq.Value);
            edit_panel2.Active = knobLowFreq;
            edit_panel2.OnChange += edit_panel_OnChange;
            edit_panel2.OnFlowing += edit_panel_OnFlowing;
            edit_panel2.textBox1.Focus();

            //graph1.GraphPen = new Pen(new SolidColorBrush(Color.FromArgb(100, 0xa5, 0x2a, 0x2a)), 7);
            Graph1.GraphPen = new Pen(new SolidColorBrush(Color.FromArgb(25, 117, 152, 45)), 7);
            Graph1.AxisPen = new Pen(new SolidColorBrush(Color.FromArgb(200, 50, 50, 50)), 3);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Key == Key.Enter) _enterCounter++;
            else _enterCounter = 0;

            if (_enterCounter >= 2) OnApplyClick(this);
            if (e.Key == Key.Escape) OnCancelClick(this);
        }

        protected override void OnMouseEnter(MouseEventArgs e)
        {
            base.OnMouseEnter(e);

            _enterCatched = true;
            if (Opacity < 1.0)
                MyAnimations.AnimateOpacity(this, Opacity, 1.0, 200);
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);

            if (_enterCatched)
                MyAnimations.AnimateOpacity(this, 1.0, 0.4, 200);
        }

        private void CloseAnimation()
        {
            Topmost = true;

            MyAnimations.AnimateOpacity(SurfGrid, Opacity, 0, 150);
            MyAnimations.AnimateRenderScale(this, 1, 0.0001, ActualWidth / 2, ActualHeight / 2, 300,
                (sender, args) => Close(), ScaleTransform.ScaleYProperty);
        }

        protected override void OnDeactivated(EventArgs e)
        {
            base.OnDeactivated(e);

            CloseAnimation();
        }

        private void knobCutOff_OnChange(object sender, double value)
        {
            edit_panel1.Value = Knob.FormatValue(value);
        }

        private void knobLowFreq_OnChange(object sender, double value)
        {
            if (knobHiFreq.Value < value + 10)
                knobHiFreq.Value = value + 10;

            edit_panel2.Value = Knob.FormatValue(value);
        }

        private void knobHiFreq_OnChange(object sender, double value)
        {
            if (knobLowFreq.Value > value - 10)
                knobLowFreq.Value = value - 10;

            edit_panel2.Value = Knob.FormatValue(value);
        }

        private void KnobMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (CutPanel.Visibility == Visibility.Visible)
            {
                if (edit_panel1.Opacity < 1.0)
                    MyAnimations.AnimateOpacity(edit_panel1, 0, 1, 100);

                if (sender.Equals(knobCutOff)) edit_panel1.FlowBox(0);
            }
            else
            {
                if (edit_panel2.Opacity < 1.0)
                    MyAnimations.AnimateOpacity(edit_panel2, 0, 1, 100);

                if (sender.Equals(knobLowFreq)) edit_panel2.FlowBox(0);
                if (sender.Equals(knobHiFreq)) edit_panel2.FlowBox(1);
            }
        }

        object edit_panel_OnFlowing(object sender, int idx)
        {
            var panel = (edit_panel)sender;

            var obj = CutPanel.Visibility == Visibility.Visible ?
                                 new[] { knobCutOff } :
                                 new[] { knobLowFreq, knobHiFreq };

            var active = obj[idx];

            panel.Value = Knob.FormatValue(active.Value);

            return active;
        }

        void edit_panel_OnChange(object sender, object active, string value)
        {
            if (active == null) return;

            var s = (edit_panel)sender;
            var k = (Knob)active;
            k.Parse(s.Value);
            s.Value = Knob.FormatValue(k.Value);
        }

        double MyGraphLowPass(object sender, double i)
        {
            return 0.95 - (Math.Exp(-5 + i * 10) / 100);
        }

        double MyGraphHighPass(object sender, double i)
        {
            return 0.95 - (Math.Exp(5 - (i * 10)) / 100);
        }

        double MyGraphBandPass(object sender, double i)
        {
            if (i < 0.5)
                return 0.95 - (Math.Exp(5 - (i * 20)) / 100);

            return 0.95 - (Math.Exp(-5 + (i - 0.5) * 20) / 100);
        }

        double MyGraphBandStop(object sender, double i)
        {
            if (i < 0.5)
                return 0.05 + (Math.Exp(5 - (i * 20)) / 100);

            return 0.05 + (Math.Exp(-5 + (i - 0.5) * 20) / 100);
        }

        private void FilterTypeChanged(object sender, int selected)
        {
            // Change graph
            Graph.DlgExpression[] graphTable = {
                                                       MyGraphLowPass, MyGraphHighPass,
                                                       MyGraphBandPass, MyGraphBandStop
                                                   };

            MyAnimations.AnimateOpacity(Graph1, 1, 0, 200, 0,
                (s, args) =>
                {
                    Graph1.Expression =
                        graphTable[selected];
                    MyAnimations.AnimateOpacity(Graph1, 0, 1, 200);
                });

            // Change controls
            if (selected >= 2)
            {
                if (BandPanel.Visibility == Visibility.Hidden)
                {
                    MyAnimations.AnimateOpacity(CutPanel, 1, 0, 200, 0,
                        (s, args) =>
                        {
                            CutPanel.Visibility = Visibility.Hidden;
                            BandPanel.Visibility = Visibility.Visible;
                            MyAnimations.AnimateOpacity(BandPanel, 0, 1, 200);
                        });

                    edit_panel2.textBox1.Focus();
                }
            }
            else
            {
                if (CutPanel.Visibility == Visibility.Hidden)
                {
                    MyAnimations.AnimateOpacity(BandPanel, 1, 0, 200, 0,
                        (s, args) =>
                        {
                            CutPanel.Visibility = Visibility.Visible;
                            BandPanel.Visibility = Visibility.Hidden;
                            MyAnimations.AnimateOpacity(CutPanel, 0, 1, 200);
                        });

                    edit_panel1.textBox1.Focus();
                }
            }
        }

        private void OnApplyClick(object sender)
        {
            TypeOfFilter = (FilterType) Roundcombo2.SelectedItem;
            
            if (TypeOfFilter == FilterType.HighPass || TypeOfFilter == FilterType.LowPass)
            {
                FrequencyOne = (int)knobCutOff.Value;
                FrequencyTwo = 0;
            }
            else
            {
                FrequencyOne = (int) knobLowFreq.Value;
                FrequencyTwo = (int) knobHiFreq.Value;
            }

            if (ApplyClick != null) ApplyClick(this);

            CloseAnimation();
        }

        private void OnCancelClick(object sender)
        {
            if (CancelClick != null) CancelClick(this);
            CloseAnimation();
        }

        private void OnDeleteClick(object sender)
        {
            if (DeleteClick != null) DeleteClick(this);
            CloseAnimation();
        }
    }
}
