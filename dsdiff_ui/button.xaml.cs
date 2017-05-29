using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace dsdiff_cross_ui_wpf
{
    public partial class MyButton
    {
        public delegate void DlgOnClick(object sender);

        public event DlgOnClick OnClick;

        public MyButton()
        {
            InitializeComponent();

            Background = new SolidColorBrush(Colors.White) {Opacity = 0.1};

            HorizontalContentAlignment = HorizontalAlignment.Center;
            VerticalContentAlignment = VerticalAlignment.Center;
        }

        protected override void OnMouseEnter(MouseEventArgs e)
        {
            base.OnMouseEnter(e);

            Background.BeginAnimation(Brush.OpacityProperty, new DoubleAnimation(Background.Opacity, 0.4, 
                new Duration(TimeSpan.FromMilliseconds(100))));
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);

            Background.BeginAnimation(Brush.OpacityProperty, new DoubleAnimation(Background.Opacity, 0.1,
                new Duration(TimeSpan.FromMilliseconds(100))));
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);

            MyAnimations.AnimateRenderScale(this, 1, 0.98, ActualWidth / 2, ActualHeight / 2, 100);
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            base.OnMouseUp(e);

            MyAnimations.AnimateRenderScale(this, 0.98, 1, ActualWidth / 2, ActualHeight / 2, 100);

            if (OnClick != null) OnClick(this);
        }
    }
}
