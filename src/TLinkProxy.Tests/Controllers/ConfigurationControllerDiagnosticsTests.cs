using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using System.Diagnostics.CodeAnalysis;
using TLinkProxy.Abstractions;
using TLinkProxy.Base;
using TLinkProxy.Configuration.Controllers;
using TLinkProxy.Configuration.Models;

namespace TLinkProxy.Tests.Controllers;

[TestClass]
[ExcludeFromCodeCoverage]
public class ConfigurationControllerDiagnosticsTests
{
    private ISettingsService _settings;
    private ConfigurationController _sut;

    [TestInitialize]
    public void Setup()
    {
        _settings = Substitute.For<ISettingsService>();
        _sut = new ConfigurationController(Substitute.For<IAiBackendRegistry>(), Substitute.For<IAiModelStore>(), _settings);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _sut.Dispose();
    }

    [TestMethod]
    public void Diagnostics_Get_NoStoredValue_UsesDefaultInterval()
    {
        _settings.GetSetting(Constants.DiagnosticsIntervalSecondsKey).Returns((string)null);

        ViewResult result = (ViewResult)_sut.Diagnostics();

        Assert.AreEqual(Constants.DefaultDiagnosticsIntervalSeconds, ((DiagnosticsSettingsViewModel)result.Model).IntervalSeconds);
    }

    [TestMethod]
    public void Diagnostics_Get_StoredValue_ReturnsStoredInterval()
    {
        _settings.GetSetting(Constants.DiagnosticsIntervalSecondsKey).Returns("30");

        ViewResult result = (ViewResult)_sut.Diagnostics();

        Assert.AreEqual(30, ((DiagnosticsSettingsViewModel)result.Model).IntervalSeconds);
    }

    [TestMethod]
    public void Diagnostics_Get_InvalidStoredValue_UsesDefaultInterval()
    {
        _settings.GetSetting(Constants.DiagnosticsIntervalSecondsKey).Returns("abc");

        ViewResult result = (ViewResult)_sut.Diagnostics();

        Assert.AreEqual(Constants.DefaultDiagnosticsIntervalSeconds, ((DiagnosticsSettingsViewModel)result.Model).IntervalSeconds);
    }

    [TestMethod]
    public void Diagnostics_Post_ValidModel_SavesSettingAndRedirects()
    {
        _sut.TempData = Substitute.For<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataDictionary>();

        IActionResult result = _sut.Diagnostics(new DiagnosticsSettingsViewModel { IntervalSeconds = 15 });

        _settings.Received(1).UpdateSetting(Constants.DiagnosticsIntervalSecondsKey, "15");
        Assert.IsInstanceOfType<RedirectToActionResult>(result);
    }

    [TestMethod]
    public void Diagnostics_Post_InvalidModel_DoesNotSaveAndReturnsView()
    {
        _sut.ModelState.AddModelError(nameof(DiagnosticsSettingsViewModel.IntervalSeconds), "invalid");

        IActionResult result = _sut.Diagnostics(new DiagnosticsSettingsViewModel { IntervalSeconds = 0 });

        _settings.DidNotReceiveWithAnyArgs().UpdateSetting(default, default);
        Assert.IsInstanceOfType<ViewResult>(result);
    }
}
