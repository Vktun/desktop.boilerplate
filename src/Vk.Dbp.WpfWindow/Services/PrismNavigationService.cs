using System;
using Prism.Navigation.Regions;
using Vk.Dbp.Contracts.Services;
using Vk.Dbp.Contracts.Constants;

namespace Dabp.WpfWindow.Services;

public class PrismNavigationService : INavigationService
{
    private readonly IRegionManager _regionManager;

    public PrismNavigationService(IRegionManager regionManager)
    {
        _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));
    }

    public event EventHandler<NavigationEventArgs>? NavigationCompleted;

    public void NavigateTo(string viewName, object? parameters = null)
    {
        try
        {
            if (parameters is NavigationParameters navigationParameters)
            {
                _regionManager.RequestNavigate(RegionNames.ContentRegion, viewName, navigationParameters);
            }
            else
            {
                _regionManager.RequestNavigate(RegionNames.ContentRegion, viewName);
            }

            NavigationCompleted?.Invoke(this, new NavigationEventArgs
            {
                ViewName = viewName,
                Parameters = parameters,
                Success = true
            });
        }
        catch (Exception ex)
        {
            NavigationCompleted?.Invoke(this, new NavigationEventArgs
            {
                ViewName = viewName,
                Parameters = parameters,
                Success = false,
                ErrorMessage = ex.Message
            });

            throw;
        }
    }

    public void NavigateTo<TView>(object? parameters = null)
    {
        NavigateTo(typeof(TView).Name, parameters);
    }

    public void GoBack()
    {
        throw new NotSupportedException("Prism region navigation back stack is not implemented in this service.");
    }
}

