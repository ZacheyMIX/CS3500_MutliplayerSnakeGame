using NetworkUtil;
using ClientModel;
using Newtonsoft.Json;
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
        /// Starts OnConnect method asynchronously
        /// </summary>
        /// <param name="address"> string representation of address to connect to </param>
        public void Connect(string address)
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
        /// Starts a Send method asynchronously to send our client's playername to the server
        /// and then asynchronously starts a receive loop for the client using ReceiveData
        /// </summary>
        /// <param name="state">SocketState created by Networking library</param>
        private void OnConnect(SocketState state)
        {
            if (state.ErrorOccurred)
            {
                // communicate error to the view
                Error?.Invoke("Error connecting to server");
                Disconnect();
                return;
            }

            // send player name to server
            while (modelWorld.PlayerName == "") { /* wait for model to have a name */ }
            if (!Networking.Send(state.TheSocket, modelWorld.PlayerName))
            {
                Error?.Invoke("Error connecting to server");
                Disconnect();
            }

            theServer = state;

            // Start an event loop to receive data from the server
            state.OnNetworkAction = ReceiveData;
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
                Error?.Invoke("Lost connection to server");
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
        /// Deeper delegate to be used by networking library on network activity.
        /// Processes data received by the network and feeds to our client model.
        /// </summary>
        private void ProcessData(SocketState state)
        {
            string totalData = state.GetData();
            state.RemoveData(0, totalData.Length);
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

        /// <summary>
        /// Closes connection with the server
        /// </summary>
        public void Disconnect()
        {
            theServer?.TheSocket?.Close();
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