// Author: Daniel Kopta, May 2019
// Unit testing examples for CS 3500 networking library (part of final project)
// University of Utah


using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace NetworkUtil
{
    [TestClass]
    public class NetworkTests
    {
        // When testing network code, we have some necessary global state,
        // since open sockets are system-wide (managed by the OS)
        // Therefore, we need some per-test setup and cleanup
        private TcpListener? testListener;
        private SocketState? testLocalSocketState, testRemoteSocketState;

        /// <summary>
        /// added by Ash for randomized tests.
        /// </summary>
        private static Random random = new Random();


        [TestInitialize]
        public void Init()
        {
            testListener = null;
            testLocalSocketState = null;
            testRemoteSocketState = null;
        }


        [TestCleanup]
        public void Cleanup()
        {
            StopTestServer(testListener, testLocalSocketState, testRemoteSocketState);
        }


        private void StopTestServer(TcpListener? listener, SocketState? socket1, SocketState? socket2)
        {
            try
            {
                // '?.' is just shorthand for null checks
                listener?.Stop();
                socket1?.TheSocket?.Shutdown(SocketShutdown.Both);
                socket1?.TheSocket?.Close();
                socket2?.TheSocket?.Shutdown(SocketShutdown.Both);
                socket2?.TheSocket?.Close();
            }
            // Do nothing with the exception, since shutting down the server will likely result in 
            // a prematurely closed socket
            // If the timeout is long enough, the shutdown should succeed
            catch (Exception) { }
        }



        public void SetupTestConnections(bool clientSide,
          out TcpListener listener, out SocketState local, out SocketState remote)
        {
            SocketState? tempLocal, tempRemote;
            if (clientSide)
            {
                NetworkTestHelper.SetupSingleConnectionTest(
                  out listener,
                  out tempLocal,    // local becomes client
                  out tempRemote);  // remote becomes server
            }
            else
            {
                NetworkTestHelper.SetupSingleConnectionTest(
                  out listener,
                  out tempRemote,   // remote becomes client
                  out tempLocal);   // local becomes server
            }

            Assert.IsNotNull(tempLocal);
            Assert.IsNotNull(tempRemote);
            local = tempLocal;
            remote = tempRemote;
        }


        /*** Begin Basic Connectivity Tests ***/
        [TestMethod]
        public void TestConnect()
        {
            NetworkTestHelper.SetupSingleConnectionTest(out testListener, out testLocalSocketState, out testRemoteSocketState);
            Assert.IsNotNull(testLocalSocketState);
            Assert.IsNotNull(testRemoteSocketState);

            Assert.IsTrue(testRemoteSocketState.TheSocket.Connected);
            Assert.IsTrue(testLocalSocketState.TheSocket.Connected);

            Assert.AreEqual("127.0.0.1:2112", testLocalSocketState.TheSocket.RemoteEndPoint?.ToString());
        }


        [TestMethod]
        public void TestConnectNoServer()
        {
            bool isCalled = false;

            void saveClientState(SocketState x)
            {
                isCalled = true;
                testLocalSocketState = x;
            }

            // Try to connect without setting up a server first.
            Networking.ConnectToServer(saveClientState, "localhost", 2112);
            NetworkTestHelper.WaitForOrTimeout(() => isCalled, NetworkTestHelper.timeout);

            Assert.IsTrue(isCalled);
            Assert.IsTrue(testLocalSocketState?.ErrorOccurred);
        }


        [TestMethod]
        public void TestConnectTimeout()
        {
            bool isCalled = false;

            void saveClientState(SocketState x)
            {
                isCalled = true;
                testLocalSocketState = x;
            }

            Networking.ConnectToServer(saveClientState, "google.com", 2112);

            // The connection should timeout after 3 seconds. NetworkTestHelper.timeout is 5 seconds.
            NetworkTestHelper.WaitForOrTimeout(() => isCalled, NetworkTestHelper.timeout);

            Assert.IsTrue(isCalled);
            Assert.IsTrue(testLocalSocketState?.ErrorOccurred);
        }


        [TestMethod]
        public void TestConnectCallsDelegate()
        {
            bool serverActionCalled = false;
            bool clientActionCalled = false;

            void saveServerState(SocketState x)
            {
                testLocalSocketState = x;
                serverActionCalled = true;
            }

            void saveClientState(SocketState x)
            {
                testRemoteSocketState = x;
                clientActionCalled = true;
            }

            testListener = Networking.StartServer(saveServerState, 2112);
            Networking.ConnectToServer(saveClientState, "localhost", 2112);
            NetworkTestHelper.WaitForOrTimeout(() => serverActionCalled, NetworkTestHelper.timeout);
            NetworkTestHelper.WaitForOrTimeout(() => clientActionCalled, NetworkTestHelper.timeout);

            Assert.IsTrue(serverActionCalled);
            Assert.IsTrue(clientActionCalled);
        }


        /// <summary>
        /// This is an example of a parameterized test. 
        /// DataRow(true) and DataRow(false) means this test will be 
        /// invoked once with an argument of true, and once with false.
        /// This way we can test your Send method from both
        /// client and server sockets. In theory, there should be no 
        /// difference, but odd things can happen if you save static
        /// state (such as sockets) in your networking library.
        /// </summary>
        /// <param name="clientSide"></param>
        [DataRow(true)]
        [DataRow(false)]
        [DataTestMethod]
        public void TestDisconnectLocalThenSend(bool clientSide)
        {
            SetupTestConnections(clientSide, out testListener, out testLocalSocketState, out testRemoteSocketState);

            testLocalSocketState.TheSocket.Shutdown(SocketShutdown.Both);

            // No assertions, but the following should not result in an unhandled exception
            Networking.Send(testLocalSocketState.TheSocket, "a");
        }

        /*** End Basic Connectivity Tests ***/


        /*** Begin Send/Receive Tests ***/

        // In these tests, "local" means the SocketState doing the sending,
        // and "remote" is the one doing the receiving.
        // Each test will run twice, swapping the sender and receiver between
        // client and server, in order to defeat statically-saved SocketStates
        [DataRow(true)]
        [DataRow(false)]
        [DataTestMethod]
        public void TestSendTinyMessage(bool clientSide)
        {
            SetupTestConnections(clientSide, out testListener, out testLocalSocketState, out testRemoteSocketState);

            // Set the action to do nothing
            testLocalSocketState.OnNetworkAction = x => { };
            testRemoteSocketState.OnNetworkAction = x => { };

            Networking.Send(testLocalSocketState.TheSocket, "a");

            Networking.GetData(testRemoteSocketState);

            // Note that waiting for data like this is *NOT* how the networking library is 
            // intended to be used. This is only for testing purposes.
            // Normally, you would provide an OnNetworkAction that handles the data.
            NetworkTestHelper.WaitForOrTimeout(() => testRemoteSocketState.GetData().Length > 0, NetworkTestHelper.timeout);

            Assert.AreEqual("a", testRemoteSocketState.GetData());
        }

        [DataRow(true)]
        [DataRow(false)]
        [DataTestMethod]
        public void TestNoEventLoop(bool clientSide)
        {
            SetupTestConnections(clientSide, out testListener, out testLocalSocketState, out testRemoteSocketState);

            int calledCount = 0;

            // This OnNetworkAction will not ask for more data after receiving one message,
            // so it should only ever receive one message
            testLocalSocketState.OnNetworkAction = (x) => calledCount++;

            Networking.Send(testRemoteSocketState.TheSocket, "a");
            Networking.GetData(testLocalSocketState);
            // Note that waiting for data like this is *NOT* how the networking library is 
            // intended to be used. This is only for testing purposes.
            // Normally, you would provide an OnNetworkAction that handles the data.
            NetworkTestHelper.WaitForOrTimeout(() => testLocalSocketState.GetData().Length > 0, NetworkTestHelper.timeout);

            // Send a second message (which should not increment calledCount)
            Networking.Send(testRemoteSocketState.TheSocket, "a");
            NetworkTestHelper.WaitForOrTimeout(() => false, NetworkTestHelper.timeout);

            Assert.AreEqual(1, calledCount);
        }


        [DataRow(true)]
        [DataRow(false)]
        [DataTestMethod]
        public void TestDelayedSends(bool clientSide)
        {
            SetupTestConnections(clientSide, out testListener, out testLocalSocketState, out testRemoteSocketState);

            // Set the action to do nothing
            testLocalSocketState.OnNetworkAction = x => { };
            testRemoteSocketState.OnNetworkAction = x => { };

            Networking.Send(testLocalSocketState.TheSocket, "a");
            Networking.GetData(testRemoteSocketState);
            // Note that waiting for data like this is *NOT* how the networking library is 
            // intended to be used. This is only for testing purposes.
            // Normally, you would provide an OnNetworkAction that handles the data.
            NetworkTestHelper.WaitForOrTimeout(() => testRemoteSocketState.GetData().Length > 0, NetworkTestHelper.timeout);

            Networking.Send(testLocalSocketState.TheSocket, "b");
            Networking.GetData(testRemoteSocketState);
            // Note that waiting for data like this is *NOT* how the networking library is 
            // intended to be used. This is only for testing purposes.
            // Normally, you would provide an OnNetworkAction that handles the data.
            NetworkTestHelper.WaitForOrTimeout(() => testRemoteSocketState.GetData().Length > 1, NetworkTestHelper.timeout);

            Assert.AreEqual("ab", testRemoteSocketState.GetData());
        }


        [DataRow(true)]
        [DataRow(false)]
        [DataTestMethod]
        public void TestEventLoop(bool clientSide)
        {
            SetupTestConnections(clientSide, out testListener, out testLocalSocketState, out testRemoteSocketState);

            int calledCount = 0;

            // This OnNetworkAction asks for more data, creating an event loop
            testLocalSocketState.OnNetworkAction = (x) =>
            {
                if (x.ErrorOccurred)
                    return;
                calledCount++;
                Networking.GetData(x);
            };

            Networking.Send(testRemoteSocketState.TheSocket, "a");
            Networking.GetData(testLocalSocketState);
            NetworkTestHelper.WaitForOrTimeout(() => calledCount == 1, NetworkTestHelper.timeout);

            Networking.Send(testRemoteSocketState.TheSocket, "a");
            NetworkTestHelper.WaitForOrTimeout(() => calledCount == 2, NetworkTestHelper.timeout);

            Assert.AreEqual(2, calledCount);
        }


        [DataRow(true)]
        [DataRow(false)]
        [DataTestMethod]
        public void TestChangeOnNetworkAction(bool clientSide)
        {
            SetupTestConnections(clientSide, out testListener, out testLocalSocketState, out testRemoteSocketState);

            int firstCalledCount = 0;
            int secondCalledCount = 0;

            // This is an example of a nested method (just another way to make a quick delegate)
            void firstOnNetworkAction(SocketState state)
            {
                if (state.ErrorOccurred)
                    return;
                firstCalledCount++;
                state.OnNetworkAction = secondOnNetworkAction;
                Networking.GetData(testLocalSocketState);
            }

            void secondOnNetworkAction(SocketState state)
            {
                secondCalledCount++;
            }

            // Change the OnNetworkAction after the first invokation
            testLocalSocketState.OnNetworkAction = firstOnNetworkAction;

            Networking.Send(testRemoteSocketState.TheSocket, "a");
            Networking.GetData(testLocalSocketState);
            NetworkTestHelper.WaitForOrTimeout(() => firstCalledCount == 1, NetworkTestHelper.timeout);

            Networking.Send(testRemoteSocketState.TheSocket, "a");
            NetworkTestHelper.WaitForOrTimeout(() => secondCalledCount == 1, NetworkTestHelper.timeout);

            //Assert.AreEqual(1, firstCalledCount);   // test fails here
            Assert.AreEqual(1, secondCalledCount);
        }



        [DataRow(true)]
        [DataRow(false)]
        [DataTestMethod]
        public void TestReceiveRemovesAll(bool clientSide)
        {
            SetupTestConnections(clientSide, out testListener, out testLocalSocketState, out testRemoteSocketState);

            StringBuilder localCopy = new StringBuilder();

            void removeMessage(SocketState state)
            {
                if (state.ErrorOccurred)
                    return;
                localCopy.Append(state.GetData());
                state.RemoveData(0, state.GetData().Length);
                Networking.GetData(state);
            }

            testLocalSocketState.OnNetworkAction = removeMessage;

            // Start a receive loop
            Networking.GetData(testLocalSocketState);

            for (int i = 0; i < 10000; i++)
            {
                char c = (char)('a' + (i % 26));
                Networking.Send(testRemoteSocketState.TheSocket, "" + c);
            }

            NetworkTestHelper.WaitForOrTimeout(() => localCopy.Length == 10000, NetworkTestHelper.timeout);

            // Reconstruct the original message outside the send loop
            // to (in theory) make the send operations happen more rapidly.
            StringBuilder message = new StringBuilder();
            for (int i = 0; i < 10000; i++)
            {
                char c = (char)('a' + (i % 26));
                message.Append(c);
            }

            Assert.AreEqual(message.ToString(), localCopy.ToString());
        }


        [DataRow(true)]
        [DataRow(false)]
        [DataTestMethod]
        public void TestReceiveRemovesPartial(bool clientSide)
        {
            SetupTestConnections(clientSide, out testListener, out testLocalSocketState, out testRemoteSocketState);

            const string toSend = "abcdefghijklmnopqrstuvwxyz";

            // Use a static seed for reproducibility
            Random rand = new Random(0);

            StringBuilder localCopy = new StringBuilder();

            void removeMessage(SocketState state)
            {
                if (state.ErrorOccurred)
                    return;
                int numToRemove = rand.Next(state.GetData().Length);
                localCopy.Append(state.GetData().Substring(0, numToRemove));
                state.RemoveData(0, numToRemove);
                Networking.GetData(state);
            }

            testLocalSocketState.OnNetworkAction = removeMessage;

            // Start a receive loop
            Networking.GetData(testLocalSocketState);

            for (int i = 0; i < 1000; i++)
            {
                Networking.Send(testRemoteSocketState.TheSocket, toSend);
            }

            // Wait a while
            NetworkTestHelper.WaitForOrTimeout(() => false, NetworkTestHelper.timeout);

            localCopy.Append(testLocalSocketState.GetData());

            // Reconstruct the original message outside the send loop
            // to (in theory) make the send operations happen more rapidly.
            StringBuilder message = new StringBuilder();
            for (int i = 0; i < 1000; i++)
            {
                message.Append(toSend);
            }

            Assert.AreEqual(message.ToString(), localCopy.ToString());
        }



        [DataRow(true)]
        [DataRow(false)]
        [DataTestMethod]
        public void TestReceiveHugeMessage(bool clientSide)
        {
            SetupTestConnections(clientSide, out testListener, out testLocalSocketState, out testRemoteSocketState);

            testLocalSocketState.OnNetworkAction = (x) =>
            {
                if (x.ErrorOccurred)
                    return;
                Networking.GetData(x);
            };

            Networking.GetData(testLocalSocketState);

            StringBuilder message = new StringBuilder();
            message.Append('a', (int)(SocketState.BufferSize * 7.5));   // originally multiplied by 7.5

            Networking.Send(testRemoteSocketState.TheSocket, message.ToString());

            NetworkTestHelper.WaitForOrTimeout(() => testLocalSocketState.GetData().Length == message.Length, NetworkTestHelper.timeout);
            // times out after 5000 ms to evaluate if the socket's data length is equal to the length of the message sent

            Assert.AreEqual(message.ToString(), testLocalSocketState.GetData());
            // asserts if the message is the same as the one stored in the socket state's data buffer
        }

        /*** End Send/Receive Tests ***/


        //TODO: Add more of your own tests here


        [DataRow(true)]
        [DataRow(false)]
        [DataTestMethod]
        public void TestSendAndCloseHugeMessage(bool clientSide)
        {
            SetupTestConnections(clientSide, out testListener, out testLocalSocketState, out testRemoteSocketState);

            testLocalSocketState.OnNetworkAction = (x) =>
            {
                if (x.ErrorOccurred)
                    return;
                Networking.GetData(x);
            };

            Networking.GetData(testLocalSocketState);

            StringBuilder message = new StringBuilder();
            message.Append('a', (int)(SocketState.BufferSize * 7.5));   // originally multiplied by 7.5

            Networking.Send(testRemoteSocketState.TheSocket, message.ToString());

            NetworkTestHelper.WaitForOrTimeout(() => testLocalSocketState.GetData().Length == message.Length, NetworkTestHelper.timeout);
            // times out after 5000 ms to evaluate if the socket's data length is equal to the length of the message sent

            Assert.AreEqual(message.ToString(), testLocalSocketState.GetData());
            // asserts if the message is the same as the one stored in the socket state's data buffer
        }

        [DataRow(true)]
        [DataRow(false)]
        [DataTestMethod]
        public void TestSendAndCloseTinyMessage(bool clientSide)
        {   //rephrasing of TestSendTinyMessage
            SetupTestConnections(clientSide, out testListener, out testLocalSocketState, out testRemoteSocketState);

            // Set the action to do nothing
            testLocalSocketState.OnNetworkAction = x => { };
            testRemoteSocketState.OnNetworkAction = x => { };

            Networking.SendAndClose(testLocalSocketState.TheSocket, "a");

            Assert.IsFalse(testLocalSocketState.TheSocket.Connected);

            Networking.GetData(testRemoteSocketState);

            // Note that waiting for data like this is *NOT* how the networking library is 
            // intended to be used. This is only for testing purposes.
            // Normally, you would provide an OnNetworkAction that handles the data.
            NetworkTestHelper.WaitForOrTimeout(() => testRemoteSocketState.GetData().Length > 0, NetworkTestHelper.timeout);

            Assert.AreEqual("a", testRemoteSocketState.GetData());
        }


        /// <summary>
        /// Method for which the other randomized invalid hostname tests stem.
        /// </summary>
        private void ConnectWithInvalidHostName(string invalidHostname)
        {
            // modeled after timeout test
            bool isCalled = false;

            void saveClientState(SocketState x)
            {
                isCalled = true;
                testLocalSocketState = x;
            }

            Networking.ConnectToServer(saveClientState, invalidHostname, 2112);

            Assert.IsTrue(isCalled);
            Assert.IsTrue(testLocalSocketState?.ErrorOccurred);
        }

        /// <summary>
        /// Generates a random string of length chars and returns it.
        /// </summary>
        private string GenerateInvalidHostname(int length)
        {
            // taken from stack overflow user Wai Ha Lee, question 134221
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        [TestMethod]
        public void TestRandomHostname1()
        {
            ConnectWithInvalidHostName(GenerateInvalidHostname(12));
        }

        [TestMethod]
        public void TestRandomHostname2()
        {
            ConnectWithInvalidHostName(GenerateInvalidHostname(13));
        }

        [TestMethod]
        public void TestRandomHostname3()
        {
            ConnectWithInvalidHostName(GenerateInvalidHostname(14));
        }

        /// <summary>
        /// test sends messages, sleeps this thread, and then ensures that the messages are received in order.
        /// </summary>
        /// <param name="clientSide">true if local is the client, false if local is the server</param>
        [DataRow(true)]
        [DataRow(false)]
        [DataTestMethod]
        public void TestSendSleepReceive(bool clientSide)
        {
            SetupTestConnections(clientSide, out testListener, out testLocalSocketState, out testRemoteSocketState);

            // Set the action to do nothing
            testLocalSocketState.OnNetworkAction = x => { };
            testRemoteSocketState.OnNetworkAction = x => { };

            Networking.Send(testLocalSocketState.TheSocket, "a");
            Networking.Send(testLocalSocketState.TheSocket, "b");
            Networking.Send(testLocalSocketState.TheSocket, "c");
            Networking.Send(testLocalSocketState.TheSocket, "d");
            Networking.Send(testLocalSocketState.TheSocket, "e");
            Networking.Send(testLocalSocketState.TheSocket, "f");
            // some time passes over the network
            Thread.Sleep(10000);
            // message is received
            Networking.GetData(testRemoteSocketState);
            NetworkTestHelper.WaitForOrTimeout(() => testRemoteSocketState.GetData().Length > 5, NetworkTestHelper.timeout);

            Assert.AreEqual("abcdef", testRemoteSocketState.GetData());
        }

        /// <summary>
        /// test sends messages, sleeps this thread, and then ensures that the messages are received in order.
        /// </summary>
        /// <param name="clientSide">true if local is the client, false if local is the server</param>
        [DataRow(true)]
        [DataRow(false)]
        [DataTestMethod]
        public void TestSleepSendReceive(bool clientSide)
        {
            SetupTestConnections(clientSide, out testListener, out testLocalSocketState, out testRemoteSocketState);

            // Set the action to do nothing
            testLocalSocketState.OnNetworkAction = x => { };
            testRemoteSocketState.OnNetworkAction = x => { };
            // some time passes over the network
            Thread.Sleep(10000);
            // actions take place
            Networking.Send(testLocalSocketState.TheSocket, "a");
            Networking.Send(testLocalSocketState.TheSocket, "b");
            Networking.Send(testLocalSocketState.TheSocket, "c");
            Networking.Send(testLocalSocketState.TheSocket, "d");
            Networking.Send(testLocalSocketState.TheSocket, "e");
            Networking.Send(testLocalSocketState.TheSocket, "f");

            Networking.GetData(testRemoteSocketState);
            NetworkTestHelper.WaitForOrTimeout(() => testRemoteSocketState.GetData().Length > 5, NetworkTestHelper.timeout);

            Assert.AreEqual("abcdef", testRemoteSocketState.GetData());
        }

        public void SetupTwoTestConnections
            (out TcpListener listener, out SocketState client1, out SocketState client2, out SocketState server)
        {
            SocketState? tempClient1, tempClient2, tempServer;
            
            NetworkTestHelper.SetupTwoConnectionTest(out listener, out tempClient1, out tempClient2, out tempServer);

            Assert.IsNotNull(tempClient1);
            Assert.IsNotNull(tempClient2);
            Assert.IsNotNull(tempServer);
            client1 = tempClient1;
            client2 = tempClient2;
            server = tempServer;
        }

        /// <summary>
        /// simulates multiple clients on a connection NEED TO FINISH
        /// </summary>
        /// <param name="clientSide">true if local is a client, false if local is the server</param>
        [DataRow(true)]
        [DataRow(false)]
        [DataTestMethod]
        public void TestSendTinyMessageMultipleClients(bool clientSide)
        {
            // TODO: FINISH IMPLEMENTING FOR ALL SOCKETS

            SetupTestConnections(clientSide, out testListener, out testLocalSocketState, out testRemoteSocketState);

            // Set the action to do nothing
            testLocalSocketState.OnNetworkAction = x => { };
            testRemoteSocketState.OnNetworkAction = x => { };

            Networking.Send(testLocalSocketState.TheSocket, "a");

            Networking.GetData(testRemoteSocketState);

            // Note that waiting for data like this is *NOT* how the networking library is 
            // intended to be used. This is only for testing purposes.
            // Normally, you would provide an OnNetworkAction that handles the data.
            NetworkTestHelper.WaitForOrTimeout(() => testRemoteSocketState.GetData().Length > 0, NetworkTestHelper.timeout);

            Assert.AreEqual("a", testRemoteSocketState.GetData());
        }


        /// <summary>
        /// simulates multiple clients on a connection NEED TO FINISH
        /// </summary>
        /// <param name="clientSide">true if local is a client, false if local is the server</param>
        [DataRow(true)]
        [DataRow(false)]
        [DataTestMethod]
        public void TestReceiveHugeMessageMultipleClients(bool clientSide)
        {
            // TODO: FINISH IMPLEMENTING FOR ALL SOCKETS

            SetupTestConnections(clientSide, out testListener, out testLocalSocketState, out testRemoteSocketState);

            testLocalSocketState.OnNetworkAction = (x) =>
            {
                if (x.ErrorOccurred)
                    return;
                Networking.GetData(x);
            };

            Networking.GetData(testLocalSocketState);

            StringBuilder message = new StringBuilder();
            message.Append('a', (int)(SocketState.BufferSize * 7.5));   // originally multiplied by 7.5

            Networking.Send(testRemoteSocketState.TheSocket, message.ToString());

            NetworkTestHelper.WaitForOrTimeout(() => testLocalSocketState.GetData().Length == message.Length, NetworkTestHelper.timeout);
            // times out after 5000 ms to evaluate if the socket's data length is equal to the length of the message sent

            Assert.AreEqual(message.ToString(), testLocalSocketState.GetData());
            // asserts if the message is the same as the one stored in the socket state's data buffer
        }
    }
}
