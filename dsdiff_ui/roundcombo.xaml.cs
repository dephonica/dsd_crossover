using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace dsdiff_cross_ui_wpf
{
    public partial class RoundCombo
    {
        private List<string> _items = null;
        
        private int _selectedItem = -1;
        private bool _increased = false;

        public delegate void DlgOnChanged(object sender, int selected);

        public event DlgOnChanged OnChanged = null;

        public List<string> Items
        {
            set 
            { 
                _items = value;
                OnListUpdated();
            }

            get { return _items; }
        }

        public int SelectedItem
        {
            set 
            {
                if (_selectedItem != value)
                {
                    if (OnChanged != null)
                        OnChanged(this, value);

                    _increased = value > _selectedItem;

                    _selectedItem = value;
                    OnListUpdated();
                }
            }

            get { return _selectedItem; }
        }

        public RoundCombo()
        {
            InitializeComponent();
        }

        private void OnListUpdated()
        {
            var text = "RoundCombo";

            if (_items != null)
            {
                if (_selectedItem < 0)
                {
                    _selectedItem = 0;
                    if (_items.Count > 0)
                        TextLine.Text = _items[0];
                }

                if (_selectedItem >= 0 && _selectedItem < _items.Count)
                    text = _items[_selectedItem];
            }
            else
                _selectedItem = -1;

            if (TextLine.Text != text)
                AnimateText(text);
        }

        private void AnimateText(string text)
        {
            MyAnimations.AnimateRenderTranslation(TextLine, TranslateTransform.XProperty,
                0, (_increased ? -1 : 1) * TextLine.ActualWidth, 200, 0,
                (s, args) =>
                    {
                        TextLine.Text = text;
                        DoFlyin();
                    });
        }

        private void DoFlyin()
        {
            MyAnimations.AnimateRenderTranslation(TextLine, TranslateTransform.XProperty,
                (_increased ? 1 : -1) * TextLine.ActualWidth, 0, 200);
        }

        private void PolyMouseOver(object sender, MouseEventArgs e)
        {
            ((Polygon) sender).Fill = new SolidColorBrush(Color.FromArgb(180,0xf0,0xf0,0xf0));
        }

        private void PolyMouseLeave(object sender, MouseEventArgs e)
        {
            ((Polygon)sender).Fill = new SolidColorBrush(Color.FromArgb(0x50, 0xf0, 0xf0, 0xf0));
        }

        public void OnLeft(object sender, MouseButtonEventArgs e)
        {
            if (_items != null)
            {
                if (SelectedItem > 0) SelectedItem--;
                else SelectedItem = _items.Count - 1;

                var polygon = (Polygon) sender;
                MyAnimations.AnimateRenderScale(polygon, 1, 0.9,
                                                polygon.ActualWidth/2, polygon.ActualHeight/2, 200, null, null, true);


            }
        }

        public void OnRight(object sender, MouseButtonEventArgs e)
        {
            if (_items != null)
            {
                if (SelectedItem < _items.Count - 1) SelectedItem++;
                else SelectedItem = 0;

                var polygon = (Polygon)sender;
                MyAnimations.AnimateRenderScale(polygon, 1, 0.9,
                                                polygon.ActualWidth / 2, polygon.ActualHeight / 2, 200, null, null, true);
            }
        }

        private void LineClick(object sender, MouseButtonEventArgs e)
        {
            if (_items != null)
            {
                if (SelectedItem < _items.Count - 1)
                    SelectedItem++;
                else
                {
                    SelectedItem = 0;
                    _increased = true;
                }
            }
        }

        private void SignMouseEnter(object sender, MouseEventArgs e)
        {
            var element = (TextBlock) sender;

            element.TextDecorations = TextDecorations.Underline;
        }

        private void SignMouseLeave(object sender, MouseEventArgs e)
        {
            var element = (TextBlock)sender;

            element.TextDecorations = null;
        }
    }
}
