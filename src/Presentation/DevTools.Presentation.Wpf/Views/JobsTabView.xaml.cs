using WpfControls = System.Windows.Controls;

namespace DevTools.Presentation.Wpf.Views;

public partial class JobsTabView : WpfControls.UserControl
{
    public WpfControls.DataGrid JobsGrid => JobsDataGrid;

    public JobsTabView()
    {
        InitializeComponent();
    }
}
