using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ClientDesktop.View.Navigation
{  
    public partial class ColorPickerWindow : UserControl
    {
        #region Variables
        public Brush SelectedBrush { get; set; }

        #endregion Variables

        #region Constructor
        public ColorPickerWindow()
        {
            InitializeComponent();
            LoadColors();
        }

        #endregion Constructor

        #region Method
        private void LoadColors()
        {           
            if (ColorPanel == null) return;

            foreach (var property in typeof(Colors).GetProperties())
            {
                var color = (Color)property.GetValue(null);
                var brush = new SolidColorBrush(color);

                Button btn = new Button();
                btn.Width = 20;
                btn.Height = 20;
                btn.Margin = new Thickness(5);
                btn.Background = brush;
               
                btn.Click += (s, e) =>
                {                   
                    Button clickedButton = s as Button;
                    if (clickedButton != null)
                    {
                        SelectedBrush = clickedButton.Background;
                       
                        Window parentWindow = Window.GetWindow(this);
                        if (parentWindow != null)
                        {
                            parentWindow.DialogResult = true;
                            parentWindow.Close();
                        }
                    }
                };

                ColorPanel.Children.Add(btn);
            }
        }

        #endregion Method
    }
}
