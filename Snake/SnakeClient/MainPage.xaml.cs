using GC;
namespace SnakeGame;

public partial class MainPage : ContentPage
{
    GameController controller;
    public MainPage()
    {
        InitializeComponent();
        controller = new GameController();

        controller.Error += NetworkErrorHandler;
        controller.Update += DisplayChanges;

        worldPanel.SetWorld(controller.modelWorld);
    }
    /// <summary>
    /// Dispatches a request to invalidate graphicsView
    /// </summary>
    private void DisplayChanges()
    {
        Dispatcher.Dispatch(() => graphicsView.Invalidate());
    }

    /// <summary>
    /// input method
    /// </summary>
    void OnTapped(object sender, EventArgs args)
    {
        keyboardHack.Focus();
    }
    /// <summary>
    /// handles directional inputs w,a,s,d
    /// </summary>
    void OnTextChanged(object sender, TextChangedEventArgs args)
    {
        Entry entry = (Entry)sender;
        String text = entry.Text.ToLower();
        if (text == "w")
        {
            controller.MoveUp();
        }
        else if (text == "a")
        {
            controller.MoveLeft();
        }
        else if (text == "s")
        {
            controller.MoveDown();
        }
        else if (text == "d")
        {
            controller.MoveRight();
        }
        entry.Text = "";
    }
    /// <summary>
    /// event handler for network errors. Dispatches appropriate error displays.
    /// </summary>
    /// <param name="errorMsg"> error message to be displayed </param>
    private void NetworkErrorHandler(string errorMsg)
    {
        // show the error
        Dispatcher.Dispatch(async () =>
        {
            if (await DisplayAlert("Error", errorMsg, "Retry Connection", "Disconnect"))
            {
                controller.Connect(serverText.Text);
            }
        });

        // then re-enable controls so user can reconenct
        Dispatcher.Dispatch(
            () =>
            {
                connectButton.IsEnabled = true;
                serverText.IsEnabled = true;
            });
    }


    /// <summary>
    /// Event handler for the connect button
    /// We will put the connection attempt logic here in the view, instead of the controller,
    /// because it is closely tied with disabling/enabling buttons, and showing dialogs.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    private void ConnectClick(object sender, EventArgs args)
    {
        if (serverText.Text == "")
        {
            DisplayAlert("Error", "Please enter a server address", "OK");
            return;
        }
        if (nameText.Text == "")
        {
            DisplayAlert("Error", "Please enter a name", "OK");
            return;
        }
        if (nameText.Text.Length > 16)
        {
            DisplayAlert("Error", "Name must be less than 16 characters", "OK");
            return;
        }
        controller.modelWorld.PlayerName = nameText.Text;
        controller.Connect(serverText.Text);
        keyboardHack.Focus();
    }


    private void ControlsButton_Clicked(object sender, EventArgs e)
    {
        DisplayAlert("Controls",
                     "W:\t\t Move up\n" +
                     "A:\t\t Move left\n" +
                     "S:\t\t Move down\n" +
                     "D:\t\t Move right\n",
                     "OK");
    }

    private void AboutButton_Clicked(object sender, EventArgs e)
    {
        DisplayAlert("About",
      "SnakeGame solution\nArtwork by Jolie Uk and Alex Smith\nGame design by Daniel Kopta and Travis Martin\n" +
      "Implementation by Ashton Hunt and Zachery Blomquist\n" +
        "CS 3500 Fall 2022, University of Utah", "OK");
    }

    private void ContentPage_Focused(object sender, FocusEventArgs e)
    {
        if (!connectButton.IsEnabled)
            keyboardHack.Focus();
    }
}