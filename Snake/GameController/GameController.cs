using NetworkUtil;
using ClientModel;

namespace GameController
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
    }
}