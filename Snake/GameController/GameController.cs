using NetworkUtil;
using ClientModel;
using Newtonsoft.Json;
using System.Runtime.CompilerServices;

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
        private World modelWorld;
        /// <summary>
        /// represents the connection the client has to the server.
        /// </summary>
        private SocketState connection;
        public GameController()
        {
            modelWorld = new World();
        }

        public World GetWorld()
        {
            return modelWorld;
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
            string toSendString = JsonConvert.SerializeObject(toSend);
            while (!Networking.Send(/* FIGURE OUT CONNECTION SOCKETSTATE */
                connection.TheSocket, toSendString))
            { /* mine for bitcoin */ }
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