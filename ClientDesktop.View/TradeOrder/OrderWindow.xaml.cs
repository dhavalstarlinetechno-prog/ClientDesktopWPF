using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ClientDesktop.View.TradeOrder
{
    public partial class OrderWindow : Window
    {
        // Colors for Active/Inactive Tabs
        private readonly Brush _activeBackground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2196F3")); // Blue
        private readonly Brush _inactiveBackground = Brushes.White;
        private readonly Brush _activeForeground = Brushes.White;
        private readonly Brush _inactiveForeground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2196F3")); // Blue

        public OrderWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Default to Market View on Load
            SetMarketMode();
        }

        // --- BUTTON CLICKS ---

        private void BtnLimit_Click(object sender, RoutedEventArgs e)
        {
            SetLimitMode();
        }

        private void BtnMarket_Click(object sender, RoutedEventArgs e)
        {
            SetMarketMode();
        }

        private void BtnStopLimit_Click(object sender, RoutedEventArgs e)
        {
            SetStopLimitMode();
        }

        // --- MODES LOGIC ---

        private void SetLimitMode()
        {
            // 1. Update Visuals
            UpdateButtonVisuals(BtnLimit, BtnMarket, BtnStopLimit);
            lblRateLabel.Text = "Limit Rate :";
            btnSellOrder.Content = "SELL LIMIT";
            btnBuyOrder.Content = "BUY LIMIT";

            // 2. Show Expiry Controls
            if (Cmbexpiry != null) Cmbexpiry.Visibility = Visibility.Visible;
            // Restore DatePicker visibility based on current selection
            UpdateExpiryDateVisibility();
        }

        private void SetMarketMode()
        {
            // 1. Update Visuals
            UpdateButtonVisuals(BtnMarket, BtnLimit, BtnStopLimit);
            lblRateLabel.Text = "Rate :";
            btnSellOrder.Content = "SELL";
            btnBuyOrder.Content = "BUY";

            // 2. Hide Expiry Controls (As requested)
            if (Cmbexpiry != null) Cmbexpiry.Visibility = Visibility.Collapsed;
            if (ExpirydatemonthPicker != null) ExpirydatemonthPicker.Visibility = Visibility.Collapsed;
        }

        private void SetStopLimitMode()
        {
            // 1. Update Visuals
            UpdateButtonVisuals(BtnStopLimit, BtnLimit, BtnMarket);
            lblRateLabel.Text = "Limit Rate :";
            btnSellOrder.Content = "SELL STOPLIMIT";
            btnBuyOrder.Content = "BUY STOPLIMIT";

            // 2. Show Expiry Controls
            if (Cmbexpiry != null) Cmbexpiry.Visibility = Visibility.Visible;
            // Restore DatePicker visibility based on current selection
            UpdateExpiryDateVisibility();
        }

        // Helper to switch tab colors
        private void UpdateButtonVisuals(Button activeBtn, Button inactiveBtn1, Button inactiveBtn2)
        {
            // Set Active
            activeBtn.Background = _activeBackground;
            activeBtn.Foreground = _activeForeground;
            activeBtn.FontWeight = FontWeights.Bold;

            // Set Inactive 1
            inactiveBtn1.Background = _inactiveBackground;
            inactiveBtn1.Foreground = _inactiveForeground;
            inactiveBtn1.FontWeight = FontWeights.Normal;

            // Set Inactive 2
            inactiveBtn2.Background = _inactiveBackground;
            inactiveBtn2.Foreground = _inactiveForeground;
            inactiveBtn2.FontWeight = FontWeights.Normal;
        }

        // --- EXPIRY LOGIC ---

        private void Cmbexpiry_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // FIX: Check if controls are initialized. 
            if (ExpirydatemonthPicker == null) return;
            UpdateExpiryDateVisibility();
        }

        private void UpdateExpiryDateVisibility()
        {
            if (ExpirydatemonthPicker == null || Cmbexpiry == null) return;

            if (Cmbexpiry.SelectedItem is ComboBoxItem selectedItem)
            {
                if (selectedItem.Content.ToString() == "Specific Date")
                {
                    ExpirydatemonthPicker.Visibility = Visibility.Visible;
                }
                else
                {
                    ExpirydatemonthPicker.Visibility = Visibility.Collapsed;
                }
            }
        }
    }
}