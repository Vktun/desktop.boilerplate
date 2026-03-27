using System.Windows;
using System.Windows.Controls;
using Vk.Dbp.AccountModule.Models;
using Vk.Dbp.AccountModule.ViewModels;

namespace Vk.Dbp.AccountModule.Views
{
    public partial class OrganizationManagementView : UserControl
    {
        public OrganizationManagementView()
        {
            InitializeComponent();
        }

        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (DataContext is OrganizationManagementViewModel viewModel)
            {
                viewModel.SelectedOrganization = e.NewValue as OrganizationUnitModel;
            }
        }
    }
}
