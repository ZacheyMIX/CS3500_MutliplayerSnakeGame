using NetworkUtil;
using GameModel;

namespace Server
{
    internal class GameServer
    {
        private Dictionary<long, SocketState> clients;
        static void Main(string[] args)
        {
            GameServer server = new();
            server.StartServer();

            Console.Read();
        }


    }
}