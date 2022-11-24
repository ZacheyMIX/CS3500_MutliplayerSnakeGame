using NetworkUtil;
using ClientModel;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace GC
{
    /// <summary>
    /// Controller between the ClientModel and the view.
    /// Should contain all Networking functionality,
    /// and delegate appropriate information to and from appropriate things.
    /// </summary>
    public class GameController
    {
        /// <summary>
        /// World used an accessed on the Client end
        /// </summary>
        public World modelWorld { get; }
        /// <summary>
        /// represents the connection the client has to the server.
        /// </summary>
        private SocketState? theServer = null;
        private string lastString = ""; // used when incoming strings are cut off

        public delegate void ErrorHandler(string errorMsg);
        public event ErrorHandler? Error;

        public delegate void UpdateHandler();
        public event UpdateHandler? Update;

        public delegate void ConnectedHandler();
        public event ConnectedHandler? Connected;


        /// <summary>
        /// Default constructor.
        /// Should create a valid model and connection state,
        /// and work with view.
        /// </summary>
        public GameController()
        {
            modelWorld = new World();
        }


        //////////////////////
        // CONNECTION METHODS
        //////////////////////
        
        /// <summary>
        /// Connects to server with specified address
        /// Starts OnConnect method asynchronously
        /// </summary>
        /// <param name="address"> string representation of address to connect to </param>
        public void Connect(string address)
        {
            Networking.ConnectToServer(OnConnect, address, 11000);
        }

        /// <summary>
        /// Method to be invoked by Networking library when connecting to server.
        /// Starts a Send method asynchronously to send our client's playername to the server
        /// and then asynchronously starts a receive loop for the client using ReceiveData
        /// </summary>
        /// <param name="state">SocketState created by Networking library</param>
        private void OnConnect(SocketState state)
        {
            if (state.ErrorOccurred)
            {
                // communicate error to the view
                Error?.Invoke("Error connecting to server. Couldn't establish connection.\n"+state.ErrorMessage);
                Disconnect();
                return;
            }
            theServer = state;
            theServer.OnNetworkAction = ReceiveData;
            // send player name to server
            Networking.Send(theServer.TheSocket, modelWorld.PlayerName);

            if (state.ErrorOccurred)
            {
                Error?.Invoke("Error connecting to server. Couldn't send player name.\n"+state.ErrorMessage);
                Disconnect();
            }

            // notify the view that the connection went through
            Connected?.Invoke();

            // Start an event loop to receive data from the server
            Networking.GetData(state);
        }

        /// <summary>
        /// Delegate to be used by Networking library on network activity.
        /// Receives data from the network for use in our client model.
        /// Once the method finishes processing data, restarts the receive loop
        /// with GetData.
        /// </summary>
        /// <param name="state">SocketState used and created by Networking library</param>
        private void ReceiveData(SocketState state)
        {
            if (state.ErrorOccurred)
            {
                Error?.Invoke
                    ("Lost connection to server. Some receive operation went weird.\n"+state.ErrorMessage);
                Disconnect();
                return;
            }
            
            // Errors may occur over receives due to lost data etc
            // left in as a comment in case this was a mistake to remove
            ProcessData(state);

            // resume loop.
            // Same procedure every frame, so the delegate hasn't changed.
            Networking.GetData(state);
        }

        /// <summary>
        /// Deeper method to be used by networking library on network activity.
        /// Processes data received by the network and feeds to our client model.
        /// </summary>
        private void ProcessData(SocketState state)
        {
            // Receive new data and append it to previously cut off data
            string received = state.GetData();
            lastString += received;
            if (received.Length > 0)
                state.RemoveData(0, received.Length);
            string[] parts = Regex.Split(lastString, "(?<=[\n])");
            lastString = "";

            lock (modelWorld)
            {
                foreach (string part in parts)
                {
                    if (int.TryParse(part, out int value))
                    {   // means this is the first few messages received from the server
                        if (modelWorld.ID == -1)
                            modelWorld.ID = value;

                        else if (modelWorld.WorldSize == -1)
                            modelWorld.WorldSize = value;

                        continue;
                    }
                    if (part == "" || part == "\n")     // funky Regex junk
                        continue;

                    if (!Regex.IsMatch(part, "\n$"))
                    {   // means the message got cut off and we should receive again before parsing more
                        lastString = part;
                        return;
                    }
                    // string is an intact json string
                    JObject newObj = JObject.Parse(part.Trim());

                    // check what kind of json string
                    if (newObj.ContainsKey("snake"))    // "snake" token unique to snake objects
                    {
                        Snake? newSnake = newObj.ToObject<Snake>();

                        if (newSnake is null)
                            continue;

                        modelWorld.UpdateSnakes(newSnake);
                    }

                    else if (newObj.ContainsKey("wall"))
                        modelWorld.UpdateWalls(newObj.ToObject<Wall>());

                    else if (newObj.ContainsKey("power"))
                        modelWorld.UpdatePowerups(newObj.ToObject<Powerup>());
                }
            }

            // inform the view that the world has new information
            Update?.Invoke();
        }

        /// <summary>
        /// Closes connection with the server
        /// </summary>
        public void Disconnect()
        {
            theServer?.TheSocket?.Close();  // cleanly disconnects from server
            theServer = null;               // resets the socket state just in case
            modelWorld.Reset();
        }


        ////////////////////
        // MOVEMENT SENDS
        ////////////////////

        /// <summary>
        /// Helper method that makes other move sending operations more concise.
        /// Takes a valid string, direction, converts that into a ControlCommands object,
        /// and then sends that over the network.
        /// </summary>
        /// <param name="direction"> should only be "none", "up", "down", "left", or "right". </param>
        private void MoveDirection(string direction)
        {
            ControlCommand toSend = new(direction);
            string toSendString = JsonConvert.SerializeObject(toSend) + "\n";
            if (theServer is not null)
            {
                while (!Networking.Send(theServer.TheSocket, toSendString))
                { /* mine for bitcoin */ }

                // Check if an error occurred after sendin
                if (theServer.ErrorOccurred)
                    Error?.Invoke("lost connection to server\n"+theServer.ErrorMessage);
            }
        }
        /// <summary>
        /// Tells the server our snake wants to move up.
        /// </summary>
        public void MoveUp()
        {
            MoveDirection("up");
        }
        /// <summary>
        /// Tells the server our snake wants to move down.
        /// </summary>
        public void MoveDown()
        {
            MoveDirection("down");
        }
        /// <summary>
        /// Tells the server our snake wants to move right.
        /// </summary>
        public void MoveRight()
        {
            MoveDirection("right");
        }
        /// <summary>
        /// Tells the server our snake wants to move left.
        /// </summary>
        public void MoveLeft()
        {
            MoveDirection("left");
        }
        /// <summary>
        /// Tells the server our snake does not want to move.
        /// </summary>
        public void MoveNone()
        {
            MoveDirection("none");
        }
    }



    /// <summary>
    /// Helper class for sending move commands over the network
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class ControlCommand
    {
        /// <summary>
        /// the only possible strings are none, up, down, left, or right.
        /// </summary>
        [JsonProperty(PropertyName = "moving")]
        public string moving;
        /// <summary>
        /// Only necessary constructor for our helper class
        /// </summary>
        /// <param name="direction"> should only be "none", "up", "down", "left" or "right" </param>
        public ControlCommand(string direction)
        {
            moving = direction;
        }
    }
}