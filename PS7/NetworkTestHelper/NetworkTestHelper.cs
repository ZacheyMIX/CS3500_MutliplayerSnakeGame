// Author: Daniel Kopta, May 2019
// Unit testing helpers for CS 3500 networking library (part of final project)
// University of Utah

using System;
using System.Net.Sockets;
using System.Threading;

namespace NetworkUtil
{

    public class NetworkTestHelper
    {
        // 5 seconds should be more than enough time for any reasonable network operation
        public const int timeout = 5000;

        /// <summary>
        /// Waits for either the specified number of milliseconds, or until expr is true,
        /// whichever comes first.
        /// </summary>
        /// <param name="expr">The expression we expect to eventually become true</param>
        /// <param name="ms">The max wait time</param>
        public static void WaitForOrTimeout(Func<bool> expr, int ms)
        {
            int waited = 0;
            while (!expr() && waited < ms)
            {
                Thread.Sleep(15);
                // Note that Sleep is not accurate, so we didn't necessarily wait for 15ms (but probably close enough)
                waited += 15;
            }
        }


        public static void SetupSingleConnectionTest(out TcpListener listener, out SocketState? client, out SocketState? server)
        {
            SocketState? clientResult = null;
            SocketState? serverResult = null;

            void saveClientState(SocketState x)
            {
                clientResult = x;
            }

            void saveServerState(SocketState x)
            {
                serverResult = x;
            }

            listener = Networking.StartServer(saveServerState, 2112);
            Networking.ConnectToServer(saveClientState, "localhost", 2112);

            WaitForOrTimeout(() => (clientResult != null) && (serverResult != null), timeout);
            client = clientResult;
            server = serverResult;
        }


        public static void SetupTwoConnectionTest
            (out TcpListener listener, out SocketState? client1, out SocketState? client2, out SocketState? server)
        {
            SocketState? clientResult1 = null;
            SocketState? clientResult2 = null;
            SocketState? serverResult = null;

            void saveClient1State(SocketState x)
            {
                clientResult1 = x;
            }
            void saveClient2State(SocketState x)
            {
                clientResult2 = x;
            }
            void saveServerState(SocketState x)
            {
                serverResult = x;
            }

            listener = Networking.StartServer(saveServerState, 2112);
            Networking.ConnectToServer(saveClient1State, "localhost", 2112);
            Networking.ConnectToServer(saveClient2State, "localhost", 2112);


            WaitForOrTimeout(() => (clientResult1 != null) && (clientResult2 != null) && (serverResult != null), timeout);
            client1 = clientResult1;
            client2 = clientResult2;
            server = serverResult;
        }

    }
}




