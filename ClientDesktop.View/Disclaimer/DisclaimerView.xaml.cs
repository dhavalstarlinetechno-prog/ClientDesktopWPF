using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ClientDesktop.View.Disclaimer
{
    /// <summary>
    /// Interaction logic for DisclaimerView.xaml
    /// </summary>
    public partial class DisclaimerView : UserControl
    {
        public DisclaimerView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Handles the MouseLeftButtonDown event on the window header to initiate a window drag operation.
        /// </summary>
        /// <remarks>Call this method in response to a MouseLeftButtonDown event on a window's header to
        /// allow the user to move the window by dragging. The method checks that the left mouse button is pressed
        /// before starting the drag operation.</remarks>
        /// <param name="sender">The source of the event, typically the header element that received the mouse button down event.</param>
        /// <param name="e">The event data containing information about the mouse button state and position.</param>
        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Jab left mouse button daba ho, tab drag logic execute karo
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var window = Window.GetWindow(this);
                if (window != null)
                {
                    window.DragMove();
                }
            }
        }
    }
}