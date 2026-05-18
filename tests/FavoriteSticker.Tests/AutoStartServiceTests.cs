using FavoriteSticker.Services;
using Xunit;

namespace FavoriteSticker.Tests;

public class AutoStartServiceTests
{
    [Fact]
    public void Enable_WritesRegistryEntry()
    {
        var service = new AutoStartService();
        service.Enable();
        Assert.True(service.IsEnabled);
    }

    [Fact]
    public void Disable_RemovesRegistryEntry()
    {
        var service = new AutoStartService();
        service.Enable();
        Assert.True(service.IsEnabled);

        service.Disable();
        Assert.False(service.IsEnabled);
    }

    [Fact]
    public void SetEnabled_ToggleWorks()
    {
        var service = new AutoStartService();
        service.SetEnabled(true);
        Assert.True(service.IsEnabled);

        service.SetEnabled(false);
        Assert.False(service.IsEnabled);
    }

    [Fact]
    public void Disable_WhenNotEnabled_DoesNotThrow()
    {
        var service = new AutoStartService();
        service.Disable(); // ensure not enabled
        service.Disable(); // should not throw
        Assert.False(service.IsEnabled);
    }
}
