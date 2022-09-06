using System;
using System.Windows;
using System.Windows.Controls;
using Dynamo.ViewModels;

namespace Dynamo.UI.Controls
{
    /// <summary>
    /// Interaction logic for InPortAutoJoin.xaml
    /// </summary>
    public partial class InPortAutoJoin : UserControl
    {
        internal event Action<ShowHideFlags, PortViewModel> RequestShowPortContextMenu;

        public InPortAutoJoin()
        {
            InitializeComponent();

            if (Application.Current != null)
            {
                Application.Current.Deactivated += CurrentApplicationDeactivated;
            }

            Unloaded += InPortAutoJoinControl_Unloaded;
        }

        /// <summary>
        /// Disposes of the event listener when the control is unloaded.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void InPortAutoJoinControl_Unloaded(object sender, RoutedEventArgs e)
        {
            if (Application.Current != null)
            {
                Application.Current.Deactivated -= CurrentApplicationDeactivated;
            }
            Unloaded -= InPortAutoJoinControl_Unloaded;
        }

        /// <summary>
        /// Closes the popup when Dynamo is not the active application.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CurrentApplicationDeactivated(object sender, EventArgs e)
        {
            OnRequestShowPortAutoJoin(ShowHideFlags.Hide);
        }

        /// <summary>
        /// Requests to open the InPortContextMenu popup.
        /// </summary>
        /// <param name="flags"></param>
        private void OnRequestShowPortAutoJoin(ShowHideFlags flags)
        {
            RequestShowPortContextMenu?.Invoke(flags, DataContext as PortViewModel);
        }
    }
}
