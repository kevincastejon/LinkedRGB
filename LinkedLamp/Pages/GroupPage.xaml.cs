using LinkedLamp.Models;
using System.Text.RegularExpressions;

namespace LinkedLamp.Pages;

public partial class GroupPage : ContentPage
{
    //private readonly ScanPage _scanPage;
    private ProvisioningContext? _ctx;

    public GroupPage(/*ScanPage scanPage*/)
    {
        InitializeComponent();
        //_scanPage = scanPage;
    }

    public void SetContext(ProvisioningContext ctx)
    {
        _ctx = ctx;
        var savedGroupName = Preferences.Get("GroupName", string.Empty);
        GroupNameEntry.Text = savedGroupName;
        NextButton.IsEnabled = _ctx.GroupName.Length > 1;
    }

    private async void OnGroupNameCompleted(object? sender, EventArgs e)
    {
        if (_ctx == null )
            return;
        if (string.IsNullOrEmpty(_ctx.GroupName) || _ctx.GroupName.Length < 1)
            return;
        //_scanPage.SetContext(_ctx);
        //await Navigation.PushAsync(_scanPage);
    }

    private void OnGroupNameChanged(object sender, TextChangedEventArgs e)
    {
        if (_ctx == null)
            return;

        var filtered = Regex.Replace(e.NewTextValue, "[^a-zA-Z0-9_-]", "");
        if (filtered != e.NewTextValue)
            ((Entry)sender).Text = filtered;
        _ctx.GroupName = filtered;
        NextButton.IsEnabled = _ctx.GroupName.Length > 1;
        Preferences.Set("GroupName", _ctx.GroupName);
    }

    private async void OnNextClicked(object sender, EventArgs e)
    {
        if (_ctx == null)
            return;
        //_scanPage.SetContext(_ctx);
        //await Navigation.PushAsync(_scanPage);
    }
}
