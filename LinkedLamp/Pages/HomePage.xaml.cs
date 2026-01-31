namespace LinkedLamp.Pages;

public partial class HomePage : ContentPage
{
    //private readonly WifiSsidPage _wifiPage;
    private readonly PermissionsPage _permissionsPage;

    public HomePage(PermissionsPage permissionsPage)
    {
        InitializeComponent();
        _permissionsPage = permissionsPage;
    }

    private async void OnStartClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(_permissionsPage);
    }
}
