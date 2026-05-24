namespace Vk.Dbp.AdminWindow.Models;

/// <summary>
/// Represents a navigation item in the admin shell sidebar.
/// </summary>
public sealed class AdminMenuItem
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AdminMenuItem"/> class.
    /// </summary>
    /// <param name="key">Stable key for the menu item.</param>
    /// <param name="title">Text displayed as the menu title.</param>
    /// <param name="description">Short description displayed below the title.</param>
    /// <param name="icon">Short icon text displayed in the sidebar.</param>
    public AdminMenuItem(string key, string title, string description, string icon)
    {
        Key = key;
        Title = title;
        Description = description;
        Icon = icon;
    }

    /// <summary>
    /// Gets the stable key for this menu item.
    /// </summary>
    public string Key { get; }

    /// <summary>
    /// Gets the menu title.
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// Gets the menu description.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Gets the compact icon text.
    /// </summary>
    public string Icon { get; }
}
