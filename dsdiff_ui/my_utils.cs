using System;
using System.Windows;
using System.Windows.Controls;

namespace dsdiff_cross_ui_wpf
{
    public class MyUtils
    {
        public static void ShowCommonDialog(Window dlg, Grid backGrid, double actualWidth, double actualHeight)
        {
            backGrid.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));
            backGrid.Arrange(new Rect(backGrid.DesiredSize));

            dlg.Left = (actualWidth - dlg.Width) / 2;
            dlg.Top = 0;
            dlg.Height = actualHeight;

            dlg.Show();
        }
    }
}
