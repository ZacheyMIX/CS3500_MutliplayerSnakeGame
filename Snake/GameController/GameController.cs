using NetworkUtil;
using ClientModel;
using Newtonsoft.Json;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

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

        public delegate void ErrorHandler(string errorMsg);
        public event ErrorHandler? Error;

        public delegate void UpdateHandler();
        public event UpdateHandler? Update;

        // other possible handlers go here

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
        /// </summary>
        /// <param name="address"> string representation of address to connect to </param>
        public void Connect(string address, string playername)
        {
            try
            {
                Networking.ConnectToServer(OnConnect, address, 11000);
            }
            catch
            {
                Error?.Invoke("Server connection timed out");
            }
        }

        /// <summary>
        /// Method to be invoked by Networking library when connecting to server.
        /// </summary>
        /// <param name="state">SocketState created by Networking library</param>
        private void OnConnect(SocketState state)
        {
            if (state.ErrorOccurred)
            {
                // communicate error to the view
                Error?.Invoke("Error connecting to server");
                return;
            }

            // send player name to server
            if (!Networking.Send(state.TheSocket, modelWorld.PlayerName))
            {
                Error?.Invoke("Error connecting to server");
            }

            theServer = state;

            // Start an event loop to receive data from the server
            state.OnNetworkAction = ReceiveData;
            Networking.GetData(state);
        }

        /// <summary>
        /// Delegate to be used by Networking library on network activity.
        /// </summary>
        /// <param name="state">SocketState used and created by Networking library</param>
        private void ReceiveData(SocketState state)
        {
            if (state.ErrorOccurred)
            {
                Error?.Invoke("Lost connection to server");
                return;
            }
            ProcessData(state);

            // resume loop.
            // Same procedure every frame, so the delegate hasn't changed.
            Networking.GetData(state);
        }

        private void ProcessData(SocketState state)
        {
            string totalData = state.GetData();
            string[] parts = Regex.Split(totalData, @"(?<=[\n])");

            lock (modelWorld)
            {
                foreach (string part in parts)
                {
                    modelWorld.Update(part);
                }
            }

            // inform the view that the world has new information
            Update?.Invoke();
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