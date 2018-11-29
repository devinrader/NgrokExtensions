namespace NgrokExtensions.TunnelInspector
{
    using System.Diagnostics.CodeAnalysis;
    using System.Windows;
    using System.Windows.Controls;

    /// <summary>
    /// Interaction logic for TunnelInspectorControl.
    /// </summary>
    public partial class TunnelInspectorControl : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TunnelInspectorControl"/> class.
        /// </summary>
        public TunnelInspectorControl()
        {
            this.InitializeComponent();
        }

        private void CommandBinding_CanExecute(object sender, System.Windows.Input.CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = false;
        }
    }
}