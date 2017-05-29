using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace dsdiff_cross_ui_wpf
{
    class MyAnimations
    {
        public static void AnimateProcessingWindowLoad(UIElement backGrid, UIElement surfGrid, 
            double actualHeight)
        {
            // Animate back opacity
            var animation = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = new Duration(TimeSpan.FromMilliseconds(200))
            };

            backGrid.BeginAnimation(UIElement.OpacityProperty, animation);

            // Animate scale
            animation = new DoubleAnimation
            {
                From = 0.2,
                To = 1,
                Duration = new Duration(TimeSpan.FromMilliseconds(200)),
                BeginTime = TimeSpan.FromMilliseconds(200)
            };

            backGrid.RenderTransform = new ScaleTransform(1, 0.2, 0, actualHeight / 2);
            backGrid.RenderTransform.BeginAnimation(ScaleTransform.ScaleYProperty, animation);

            // Animate grid opacity
            surfGrid.Opacity = 0;

            surfGrid.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = new Duration(TimeSpan.FromMilliseconds(200)),
                BeginTime = TimeSpan.FromMilliseconds(400)
            });
        }

        public static void AnimateOpacity(UIElement element, double from, double to, double time,
            double beginTime = 0.0, EventHandler animationCompleted = null)
        {
            var animation = new DoubleAnimation(from, to, new Duration(TimeSpan.FromMilliseconds(time)))
                                {
                                    BeginTime = TimeSpan.FromMilliseconds(beginTime)
                                };

            if (animationCompleted != null) 
                animation.Completed += animationCompleted;

            element.BeginAnimation(UIElement.OpacityProperty, animation);
        }

        public static void AnimateRenderScale(UIElement element, double from, double to, double cx,
            double cy, double time, EventHandler animationCompleted = null, 
            DependencyProperty property = null, bool autoreverse = false)
        {
            element.RenderTransform = new ScaleTransform(1, 1, cx, cy);
            var animationX = new DoubleAnimation(from, to, new Duration(TimeSpan.FromMilliseconds(time)))
                                {AutoReverse = autoreverse};
            var animationY = new DoubleAnimation(from, to, new Duration(TimeSpan.FromMilliseconds(time))) { AutoReverse = autoreverse };

            if (animationCompleted != null)
                animationX.Completed += animationCompleted;

            if (property == null)
            {
                element.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty, animationX);
                element.RenderTransform.BeginAnimation(ScaleTransform.ScaleYProperty, animationY);
            } else
                element.RenderTransform.BeginAnimation(property, animationX);
        }

        public static void AnimateLayoutScale(FrameworkElement element, double from, double to, double cx,
            double cy, double time, EventHandler animationCompleted = null,
            DependencyProperty property = null)
        {
            element.LayoutTransform = new ScaleTransform(1, 1, cx, cy);
            var animation = new DoubleAnimation(from, to, new Duration(TimeSpan.FromMilliseconds(time)));

            if (animationCompleted != null)
                animation.Completed += animationCompleted;

            if (property == null)
            {
                element.LayoutTransform.BeginAnimation(ScaleTransform.ScaleXProperty, animation);
                element.LayoutTransform.BeginAnimation(ScaleTransform.ScaleYProperty, animation);
            } else
                element.LayoutTransform.BeginAnimation(property, animation);
        }

        public static void AnimateRenderTranslation(UIElement element, DependencyProperty property,
            double from, double to, double time, double beginTime = 0, EventHandler animationCompleted = null)
        {
            var animation = new DoubleAnimation
            {
                From = from,
                To = to,
                Duration = new Duration(TimeSpan.FromMilliseconds(time)),
                BeginTime = TimeSpan.FromMilliseconds(beginTime),
                AutoReverse = false
            };

            if (animationCompleted != null)
                animation.Completed += animationCompleted;

            element.RenderTransform = new TranslateTransform();
            element.RenderTransform.BeginAnimation(property, animation);
        }
    }
}
