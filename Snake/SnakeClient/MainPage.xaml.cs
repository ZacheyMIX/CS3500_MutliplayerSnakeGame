using GC;
using Plugin.Maui.Audio;

namespace SnakeGame;

public partial class MainPage : ContentPage
{
    GameController controller;
    IAudioManager audioManager;
    List<IAudioPlayer> audioPlayers; // possible death sounds
    Random random;  // used in playing a random death sound, or eventually doing a random effect
    public MainPage(IAudioManager audioManager)
    {
        InitializeComponent();
        this.audioManager = audioManager;

        controller = new GameController();
        audioPlayers = new List<IAudioPlayer>();
        random = new Random();

        controller.Error += NetworkErrorHandler;
        controller.Update += DisplayChanges;
        controller.Connected += SuccessfulConnect;
        controller.PlayerDied += PlayDeathSound;

        AddDeathSound("crash.wav"); // car crash sound effect
        AddDeathSound("bonk2.wav"); // bonk sound effect #2

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
    /// Plays a death sound on player's death.
    /// Accessed from WorldPanel
    /// </summary>
    public void PlayDeathSound()
    {
        int index = random.Next(0, 2);  // excludes 2, so goes from 0 to 1. Change this appropriately with new death sounds.
        audioPlayers[index].Play();
    }

    /// <summary>
    /// Adds sound from specified path to audioPlayers for playing a death sound.
    /// </summary>
    /// <param name="path"> string representation of the name of the file to add from Raw subdirectory </param>
    private void AddDeathSound(String path)
    {
        Dispatcher.Dispatch(async () =>
        {
            var player = audioManager.CreatePlayer
                (await FileSystem.OpenAppPackageFileAsync(path));
            audioPlayers.Add(player);
        });
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
        // show the error then give the user the option to reconnect or to not
        Dispatcher.Dispatch(async () =>
        {
            if (await DisplayAlert("Error", errorMsg, "Retry Connection", "Disconnect"))
            {
                controller.Connect(serverText.Text);
            }
            else
            {
                connectButton.IsEnabled = true;
                serverText.IsEnabled = true;
                nameText.IsEnabled = true;
            }
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

    /// <summary>
    /// Handler for what the view should do on a successful connect.
    /// Disables connectButton and serverText.
    /// Event notified by controller.
    /// </summary>
    private void SuccessfulConnect()
    {
        Dispatcher.Dispatch(() =>
        {
            connectButton.IsEnabled = false;
            serverText.IsEnabled = false;
            nameText.IsEnabled = false;
        });
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