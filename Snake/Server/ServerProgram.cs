using NetworkUtil;
using GameModel;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using System.Xml;
using System.Runtime.Serialization;

namespace Server
{
    internal class GameServer
    {
        private Dictionary<long, SocketState> clients;
        private ServerWorld zeWorld;
        public GameSettings settings;
        private int MSPerFrame;
        static void Main(string[] args)
        {
            GameServer server = new();
            GameSettings? settings;
            // XML stuff

            // Set the reader settings.
            XmlReaderSettings xmlsettings = new XmlReaderSettings();
            xmlsettings.IgnoreComments = true;
            xmlsettings.IgnoreProcessingInstructions = true;
            xmlsettings.IgnoreWhitespace = true;

            try
            {
                using (XmlReader reader = XmlReader.Create("settings.xml", xmlsettings))
                {
                    DataContractSerializer ser = new DataContractSerializer(typeof(GameSettings));
                    settings = (GameSettings?)ser.ReadObject(reader);
                }
            }
            catch
            {
                using (XmlReader reader = XmlReader.Create("../../../settings.xml", xmlsettings))
                {
                    DataContractSerializer ser = new DataContractSerializer(typeof(GameSettings));
                    settings = (GameSettings?)ser.ReadObject(reader);
                }
            }



            // ensure settings are valid. stop program if not
            if (settings is null)
                return;

            // make settings instance accessible by server methods
            server.settings = settings;

            server.zeWorld = new ServerWorld(settings.UniverseSize, settings.Walls);
            server.MSPerFrame = settings.MSPerFrame;

            server.StartServer();

            // Sleep to prevent program from closing.
            // this thread is done, but other threads are still working.
            Console.Read();
            server.Run();     // main update loop
        }

        /// <summary>
        /// Initialized server's state
        /// </summary>
        public GameServer()
        {
            clients = new Dictionary<long, SocketState>();
            zeWorld = new ServerWorld();
            settings = new GameSettings();
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
            {
                Console.WriteLine("Error connecting client ID " + state.ID);
                RemoveClient(state.ID);
                return;
            }

            // delegate to be invoked whenever we receive some data from this client
            state.OnNetworkAction = ReceiveName;

            Console.WriteLine("Client " + state.ID + " has connected.");

            // data thread loop
            Networking.GetData(state);
        }

        /// <summary>
        /// initial handshake protocol for receiving player name
        /// </summary>
        /// <param name="state"></param>
        private void ReceiveName(SocketState state)
        {
            if (state.ErrorOccurred)
            {
                Console.WriteLine("Error finishing handshake with client ID " + state.ID);
                RemoveClient(state.ID);
                return;
            }

            string totalData = state.GetData();
            // Ignore empty strings added by the regex splitter
            if (totalData.Length == 0)
                return;

            // haven't received terminator character yet
            if (totalData[totalData.Length - 1] != '\n')
                return;

            // in this last case we have a full message ending with a terminator character
            // that we can use as a name

            // add client state to set of connections
            // locks for race conditions etc
            lock (clients)
            {
                clients.Add(state.ID, state);
            }

            // add client information to model
            lock (zeWorld)
            {
                //Checks if a snake can be added into the client
                if (!zeWorld.AddSnake(Regex.Replace(totalData, @"\t|\n|\r", ""), (int)state.ID))
                    RemoveClient(state.ID);

                // PS9 FAQ states to first send then add state to client set
                // is important because we need to ensure the client gets handshake info FIRST
                // and there are other threads broadcasting everything to all clients.
                Networking.Send(state.TheSocket,
                    state.ID.ToString() + "\n"      // send client ID
                + settings.UniverseSize + "\n");    // and then worldsize

                state.OnNetworkAction = ReceiveData;
                Console.WriteLine("Player " + Regex.Replace(totalData, @"\t|\n|\r", "") + " has joined the game.");


                // send Walls information to the client
                foreach (Wall wall in zeWorld.Walls)
                    SerializeAndSend(wall, state);
            }

            if (state.ErrorOccurred)
            {
                Console.WriteLine("Error finishing handshake with client ID " + state.ID);
                RemoveClient(state.ID);
                return;
            }

            // messaged was processed correctly so we can remove it
            state.RemoveData(0, totalData.Length);


            // resume client receive loop with whichever delegate is valid at this point
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
                Console.WriteLine("Error receiving data from client ID " + state.ID);
                RemoveClient(state.ID);
                return;
            }

            // apply movement request
            ProcessData(state);

            // resume thread loop
            Networking.GetData(state);
        }

        /// <summary>
        /// Gathers the JSON strings and seperates them into substrings interpreted as movement commands
        /// </summary>
        /// <param name="state"></param>
        private void ProcessData(SocketState state)
        {
            string totalData = state.GetData();
            // splits received strings into substrings that end in newline
            string[] parts = Regex.Split(totalData, @"(?<=[\n])");


            lock (zeWorld)
            {
                // loop until we process all messages.
                // We may have received more than one.
                foreach (string p in parts)
                {
                    // Ignore empty strings added by the regex splitter
                    if (p.Length == 0)
                        continue;

                    // terminator string wasn't included
                    if (p[p.Length - 1] != '\n')
                        continue;

                    ControlCommand? Movement = JsonConvert.DeserializeObject<ControlCommand>(p);

                    // then we are given some weird string with a terminator character at the end
                    // possibly change to disable or remove client
                    if (Movement is null)
                        continue;

                    zeWorld.MoveSnake((int)state.ID, Movement);  // still need to implement this ServerWorld method

                    state.RemoveData(0, p.Length);
                }
            }
        }

        /// <summary>
        /// Serializes some object to prepare it to be sent over the network.
        /// Uses should only be for walls, snakes, and powerups.
        /// </summary>
        /// <param name="obj"> a wall, snake, or powerup </param>
        private static void SerializeAndSend(object obj, SocketState socket)
        {
            // this method should only be sending a Snake, Wall, or Powerup instance
            if (obj is not Snake && obj is not Wall && obj is not Powerup)
                return;

            string toSendString = JsonConvert.SerializeObject(obj) + "\n";
            Networking.Send(socket.TheSocket, toSendString);
        }

        /// <summary>
        /// Removes client from client set
        /// </summary>
        private void RemoveClient(long id)
        {
            Console.WriteLine("Client " + id + " disconnected");
            lock (clients)
            {
                clients.Remove(id);
            }
        }

        /// <summary>
        /// runs the server. a majority of the processing time should be spent in here from our program
        /// and our 
        /// </summary>
        public void Run()
        {
            // Start a new timer to control the frame rate
            System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
            watch.Start();

            while (true)
            {
                // wait until the next frame
                while (watch.ElapsedMilliseconds < MSPerFrame)
                { /* empty loop body */ }

                watch.Restart();

                Update();
            }
        }

        /// <summary>
        /// method for communicating updates to the world
        /// </summary>
        private void Update()
        {
            IEnumerable<int> playersToRemove = zeWorld.Snakes.Values.Where(snake => (!snake.alive || snake.dc)).Select(snake => snake.ID);
     
        }
    }

    /// <summary>
    /// Used as a kind of helper class on Server construct to read provided settings.xml file
    /// </summary>
    [DataContract(Namespace = "")]
    internal class GameSettings
    {
        // note that this doesn't do anything.
        [DataMember(Name = "FramesPerShot")]
        internal int FramesPerShot;

        [DataMember(Name = "MSPerFrame")]
        internal int MSPerFrame;

        [DataMember(Name = "RespawnRate")]
        internal int RespawnRate;

        [DataMember(Name = "UniverseSize")]
        internal int UniverseSize;

        [DataMember(Name = "Walls")]
        internal List<Wall> Walls;

        public GameSettings()
        {
            Walls = new();
            FramesPerShot = 0;
            RespawnRate = 0;
            UniverseSize = 0;
            MSPerFrame = 0;
        }
    }


}