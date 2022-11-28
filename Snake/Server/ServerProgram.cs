using NetworkUtil;
using GameModel;
using System.Text.RegularExpressions;

namespace Server
{
    internal class GameServer
    {
        private Dictionary<long, SocketState> clients;
        static void Main(string[] args)
        {
            GameServer server = new();
            server.StartServer();
            
            // Sleep to prevent program from closing.
            // this thread is done, but other threads are still working.
            Console.Read();
        }

        /// <summary>
        /// Initialized server's state
        /// </summary>
        public GameServer()
        {
            clients = new Dictionary<long, SocketState>();
        }

        /// <summary>
        /// Start accepting socket connections from clients
        /// </summary>
        public void StartServer()
        {
            // begin event loop
            Networking.StartServer(NewClientConnected, 11000);

            Console.WriteLine("Server is running. Accepting clients.");
        }

        /// <summary>
        /// Method invoked by networking library on new connection (StartServer)
        /// </summary>
        private void NewClientConnected(SocketState state)
        {
            if (state.ErrorOccurred)
                return;

            // add client state
            // locks for race conditions etc
            lock (clients)
            {
                clients[state.ID] = state;
            }

            // delegate to be invoked whenever we receive some data from this client
            state.OnNetworkAction = ReceiveData;
            
            // data thread loop
            Networking.GetData(state);
        }

        /// <summary>
        /// Method invoked by networking library on data receive
        /// </summary>
        /// <param name="state"></param>
        private void ReceiveData(SocketState state)
        {
            if (state.ErrorOccurred)
            {
                RemoveClient(state.ID);
                return;
            }

            // apply movement request
            ProcessData(state);

            // resume thread loop
            Networking.GetData(state);
        }

        private void ProcessData(SocketState state)
        {
            string totalData = state.GetData();
            // splits received strings into substrings that end in newline
            string[] parts = Regex.Split(totalData, @"(?<=[\n])");

            // loop until we process all messages.
            // We may have received more than one.
            foreach (string p in parts)
            {
                // Ignore empty strings added by the regex splitter
                if (p.Length == 0)
                    continue;

                // terminator string wasn't included, so this message is corrupt
                if (p[p.Length - 1] != '\n')
                    break;

                // may add message received or the message received from the client
                // but this clutters up the console so we'll leave it out.
                // otherwise, uncomment the following line:
                //Console.WriteLine("received data from client " + state.ID + ": \"" + p.Substring(0, p.Length - 1) + "\"");

                state.RemoveData(0, p.Length);

                // TODO: Process data in model, make sure that this actually is valid
                // also find out how to send information every frame
            }
        }

        private void RemoveClient(long id)
        {
            Console.WriteLine("Client " + id + " disconnected");
            lock (clients)
            {
                clients.Remove(id);
            }
        }
    }
}