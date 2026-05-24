using FluentAssertions;
using Vk.Dbp.AdminWindow.ViewModels;
using Xunit;

namespace Vk.Dbp.Tests.Unit.ViewModels;

public class AdminShellViewModelTests
{
    [Fact]
    public void Constructor_selects_first_menu_item()
    {
        var viewModel = new AdminShellViewModel();

        viewModel.MenuItems.Should().NotBeEmpty();
        viewModel.SelectedMenuItem.Should().BeSameAs(viewModel.MenuItems[0]);
        viewModel.ContentTitle.Should().Be(viewModel.MenuItems[0].Title);
    }

    [Fact]
    public void SelectMenuItemCommand_updates_selected_menu_item_and_content_title()
    {
        var viewModel = new AdminShellViewModel();
        var targetMenu = viewModel.MenuItems[2];

        viewModel.SelectMenuItemCommand.Execute(targetMenu);

        viewModel.SelectedMenuItem.Should().BeSameAs(targetMenu);
        viewModel.ContentTitle.Should().Be(targetMenu.Title);
    }
}
