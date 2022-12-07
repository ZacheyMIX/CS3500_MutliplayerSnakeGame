// WRITTEN BY ZACH BLOMQUIST AND ASHTON HUNT AS PART OF PS7, CS3500 FALL 2022
//Guidelines:
// Do not modify any namespaces or provided helper classes.
// In the networking class, you may add any PRIVATE STATIC helper methods you see fit.
// Do not add any PUBLIC methods.
// Do not add any members or fields of any kind.
// Do not modify SocketState.
// None of the public methods in your networking library should ever throw an exception
// (directly or indirectly by not catching one thrown from another method)
// See the handout documentation and Errors section in PS7 instructions for more information on handling errors.
// Fill in the implementations of the methods in the Networking class according to the provided comments in the handout code.

// for now, I need to add pseudocode comments for all other methods aside from the ConnectToServer method

using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace NetworkUtil;

public static class Networking
{
    /////////////////////////////////////////////////////////////////////////////////////////
    // Server-Side Code
    /////////////////////////////////////////////////////////////////////////////////////////

    // public methods: StartServer, StopServer, ConnectToServer, GetData, Send, SendAndClose
    // private methods: AcceptNewClient, ConnectedCallback, ReceiveCallback, SendCallback, SendAndCloseCallback
    // recall from slideshow:
    //GetData(state)
    //{
    //    BeginReceive(…, callback, state);
    //}
    //callback(state)
    //{
    //    EndReceive(…);
    //    state.Action()  // delegate
    //}
    // consider how to apply this design to each public method and its corresponding private method.
    // public method -> private callback
    // ConnectToServer -> ConnectedCallback
    // Send -> SendCallback
    // SendAndClose -> SendAndCloseCallback
    // unsure of StartServer, StopServer, ReceiveCallback

    // accessing TcpListener comes down to understanding nomadic states. Check Hint #4 for more details.
    // Remember to keep these notes up to date with new findings.

    /// <summary>
    /// Starts a TcpListener on the specified port and starts an event-loop to accept new clients.
    /// The event-loop is started with BeginAcceptSocket and uses AcceptNewClient as the callback.
    /// AcceptNewClient will continue the event-loop.
    /// </summary>
    /// <param name="toCall">The method to call when a new connection is made</param>
    /// <param name="port">The the port to listen on</param>
    public static TcpListener StartServer(Action<SocketState> toCall, int port)
    {
        TcpListener listener = new(IPAddress.Any, port);
        // listener is nomadic so this is only returned between things
        // not to be treated as a field ever.
        try
        {
            // start listener
            listener.Start();
            Tuple<TcpListener, Action<SocketState>> passed = new(listener, toCall);
            // begin accepting clients
            listener.BeginAcceptSocket(AcceptNewClient, passed);  // calls AcceptNewClient and starts accept loop

            // begin receiving data
        }
        catch
        {
            // nothing to do as nothing has happened
        }

        return listener;
    }

    /// <summary>
    /// To be used as the callback for accepting a new client that was initiated by StartServer, and 
    /// continues an event-loop to accept additional clients.
    ///
    /// Uses EndAcceptSocket to finalize the connection and create a new SocketState. The SocketState's
    /// OnNetworkAction should be set to the delegate that was passed to StartServer.
    /// Then invokes the OnNetworkAction delegate with the new SocketState so the user can take action. 
    /// 
    /// If anything goes wrong during the connection process (such as the server being stopped externally), 
    /// the OnNetworkAction delegate should be invoked with a new SocketState with its ErrorOccurred flag set to true 
    /// and an appropriate message placed in its ErrorMessage field. The event-loop should not continue if
    /// an error occurs.
    ///
    /// If an error does not occur, after invoking OnNetworkAction with the new SocketState, an event-loop to accept 
    /// new clients should be continued by calling BeginAcceptSocket again with this method as the callback.
    /// </summary>
    /// <param name="ar">The object asynchronously passed via BeginAcceptSocket. It must contain a tuple with 
    /// 1) a delegate so the user can take action (a SocketState Action), and 2) the TcpListener</param>
    private static void AcceptNewClient(IAsyncResult ar)
    {
        TcpListener listener;
        Action<SocketState> toCall;
        Tuple<TcpListener, Action<SocketState>> passed = (Tuple<TcpListener, Action<SocketState>>)ar.AsyncState!;
        listener = passed.Item1;
        toCall = passed.Item2;
        SocketState state;
        Socket socket;
        try
        {
            socket = listener.EndAcceptSocket(ar);                              // creates a new socket from where the listener is 
            socket.NoDelay = true;                                              // disables Nagle algorithm for ease of use in our game
            state = new(toCall, socket);                                        // current socket works with the socketstate
            state.OnNetworkAction(state);                                       // acts on socketstate after connection
            listener.BeginAcceptSocket(AcceptNewClient, passed);                // resume loop
        }
        catch (Exception e)
        {
            NetworkErrorOccurred(toCall,
                "Something happened in the client acceptance loop\n" + e.ToString(), null);
            return; // end loop
        }
    }

    /// <summary>
    /// Stops the given TcpListener.
    /// </summary>
    public static void StopServer(TcpListener listener)
    {
        try
        {
            listener.Stop();
        }
        catch
        {
            // means server crashes and there is nothing to do
        }
    }

    /////////////////////////////////////////////////////////////////////////////////////////
    // Client-Side Code
    /////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Begins the asynchronous process of connecting to a server via BeginConnect, 
    /// and using ConnectedCallback as the method to finalize the connection once it's made.
    /// 
    /// If anything goes wrong during the connection process, toCall should be invoked 
    /// with a new SocketState with its ErrorOccurred flag set to true and an appropriate message 
    /// placed in its ErrorMessage field. Depending on when the error occurs, this should happen either
    /// in this method or in ConnectedCallback.
    ///
    /// This connection process should timeout and produce an error (as discussed above) 
    /// if a connection can't be established within 3 seconds of starting BeginConnect.
    /// 
    /// </summary>
    /// <param name="toCall">The action to take once the connection is open or an error occurs</param>
    /// <param name="hostName">The server to connect to</param>
    /// <param name="port">The port on which the server is listening</param>
    public static void ConnectToServer(Action<SocketState> toCall, string hostName, int port)
    {

        // Establish the remote endpoint for the socket.
        IPHostEntry ipHostInfo;
        IPAddress ipAddress = IPAddress.None;

        // Determine if the server address is a URL or an IP
        try
        {
            ipHostInfo = Dns.GetHostEntry(hostName);
            bool foundIPV4 = false;
            foreach (IPAddress addr in ipHostInfo.AddressList)
                if (addr.AddressFamily != AddressFamily.InterNetworkV6)
                {
                    foundIPV4 = true;
                    ipAddress = addr;
                    break;
                }
            // Didn't find any IPV4 addresses
            if (!foundIPV4)
            {
                NetworkErrorOccurred(toCall,
                    "Could not find applicable IPV4 address.", null);
                return; // end loop
            }
        }
        catch (Exception)
        {
            // see if host name is a valid ipaddress
            try
            {
                ipAddress = IPAddress.Parse(hostName);
            }
            catch (Exception e)
            {
                NetworkErrorOccurred(toCall,
                    "Host name is not a valid IP address.\n" + e.ToString(), null);
                return; // end loop
            }
        }

        // Create a TCP/IP socket.
        Socket socket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

        // This disables Nagle's algorithm (google if curious!)
        // Nagle's algorithm can cause problems for a latency-sensitive 
        // game like ours will be
        socket.NoDelay = true;
        // Finish the remainder of the connection process as specified.
        SocketState connectingState = new(toCall, socket);
        try
        {
            // connection starts from the client. need to consider IP address and port
            IAsyncResult waiter = connectingState.TheSocket.BeginConnect(ipAddress, port, ConnectedCallback, connectingState);
            // servers and waiters. It's official, we're in a diner.

            if (!waiter.AsyncWaitHandle.WaitOne(3000)) // returns true if async method takes less than 3 s to finish
                // this is kind of confusing so refer to IAsyncResult.AsyncWaitHandle documentation if need be
                throw new TimeoutException("Timed out while trying to connect to the server.");
        }
        catch (Exception e)
        {
            // need to catch for timeouts.
            // timeout will most likely throw an error in the try statement, so this should work
            NetworkErrorOccurred(toCall,
                "Error occurred when connecting:\n" + e.ToString(), connectingState);
            return; // end loop
        }
    }

    /// <summary>
    /// To be used as the callback for finalizing a connection process that was initiated by ConnectToServer.
    ///
    /// Uses EndConnect to finalize the connection.
    /// 
    /// As stated in the ConnectToServer documentation, if an error occurs during the connection process,
    /// either this method or ConnectToServer should indicate the error appropriately.
    /// 
    /// If a connection is successfully established, invokes the toCall Action that was provided to ConnectToServer (above)
    /// with a new SocketState representing the new connection.
    /// 
    /// </summary>
    /// <param name="ar">
    /// The object asynchronously passed via BeginConnect.
    /// This should always have an AsyncState field of a valid SocketState.
    /// </param>
    private static void ConnectedCallback(IAsyncResult ar)
    {
        SocketState state;
        state = (SocketState)ar.AsyncState!;
        try
        {
            state.TheSocket.EndConnect(ar); // finalizes the connection
            state.TheSocket.NoDelay = true; // disables Nagle algorithm for ease of use in our game
            state.OnNetworkAction(state);   // invokes the toCall Action for a new connection
        }
        catch (Exception e)
        {
            NetworkErrorOccurred(state.OnNetworkAction,
                "Something happened in the client acceptance loop\n" + e.ToString(), state);
            return; // end loop
        }
    }


    /////////////////////////////////////////////////////////////////////////////////////////
    // Server and Client Common Code
    /////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Begins the asynchronous process of receiving data via BeginReceive, using ReceiveCallback 
    /// as the callback to finalize the receive and store data once it has arrived.
    /// The object passed to ReceiveCallback via the AsyncResult should be the SocketState.
    /// 
    /// If anything goes wrong during the receive process, the SocketState's ErrorOccurred flag should 
    /// be set to true, and an appropriate message placed in ErrorMessage, then the SocketState's
    /// OnNetworkAction should be invoked. Depending on when the error occurs, this should happen either
    /// in this method or in ReceiveCallback.
    /// </summary>
    /// <param name="state">The SocketState to begin receiving</param>
    public static void GetData(SocketState state)
    {
        try
        {
            // BeginReceive parameters:
            // buffer, preallocated memory for information over network, included in SocketState class;
            // offset: index to start storing data in buffer, we start at zero as we can use the whole buffer;
            // size: number of bytes to receive at a time, size of SocketState object buffers (4096 after looking at SocketState);
            // socketFlags: send and receive behaviors, never specified so we use none;
            // callback: async method to be called, with parameter object that should be a state, ReceiveCallback in this method;
            // state: object passed into callback when called asynchronously, "state" in this method;
            state.TheSocket.BeginReceive(state.buffer, 0, SocketState.BufferSize, SocketFlags.None, ReceiveCallback, state);
        }
        catch (Exception e)
        {
            NetworkErrorOccurred
                (state.OnNetworkAction, "Error when receiving data from the other socket:\n" + e.ToString(), state);
            return; // end loop
        }
    }

    /// <summary>
    /// To be used as the callback for finalizing a receive operation that was initiated by GetData.
    /// 
    /// Uses EndReceive to finalize the receive.
    ///
    /// As stated in the GetData documentation, if an error occurs during the receive process,
    /// either this method or GetData should indicate the error appropriately.
    /// 
    /// If data is successfully received:
    ///  (1) Read the characters as UTF8 and put them in the SocketState's unprocessed data buffer (its string builder).
    ///      This must be done in a thread-safe manner with respect to the SocketState methods that access or modify its 
    ///      string builder.
    ///  (2) Call the saved delegate (OnNetworkAction) allowing the user to deal with this data.
    /// </summary>
    /// <param name="ar"> 
    /// This contains the SocketState that is stored with the callback when the initial BeginReceive is called.
    /// </param>
    private static void ReceiveCallback(IAsyncResult ar)
    {
        SocketState state = (SocketState)ar.AsyncState!;
        try
        {
            // determine if anything went through
            int bytesReceived = state.TheSocket.EndReceive(ar);
            if (bytesReceived <= 0)         // finalizes the connection
                // EndReceive returns the number of bytes received.
                // if this is 0 or less an error occurred.
                // 0 if nothing went through,
                // negative stuff if something seriously weird happened.
                throw new Exception("Socket was closed during receiving.");

            // put new data in buffer
            lock (state.data)
                // compiler didn't give me an error when trying to access the data field. happy little surprises!
                state.data.Append(Encoding.UTF8.GetString(state.buffer, 0, bytesReceived));

            state.OnNetworkAction(state);
        }
        catch (Exception e)
        {
            NetworkErrorOccurred(state.OnNetworkAction,
                "Something happened in the client acceptance loop\n" + e.ToString(), state);
            return; // end loop
        }
    }

    /// <summary>
    /// Begin the asynchronous process of sending data via BeginSend, using SendCallback to finalize the send process.
    /// 
    /// If the socket is closed, does not attempt to send.
    /// 
    /// If a send fails for any reason, this method ensures that the Socket is closed before returning.
    /// </summary>
    /// <param name="socket">The socket on which to send the data</param>
    /// <param name="data">The string to send</param>
    /// <returns>True if the send process was started, false if an error occurs or the socket is already closed</returns>
    public static bool Send(Socket socket, string data)
    {
        try
        {
            if (!socket.Connected)   // if socket is closed, Send operation is not attempted
                return false;
            // create a new state to start BeginSocket
            // according to SendCallback, the state parameter should be the socket parameter specified here.
            // change string data to a byte array
            byte[] toSend = Encoding.UTF8.GetBytes(data);   // we are using UTF8 and not ASCII in this project
            socket.BeginSend(toSend, 0, toSend.Length, SocketFlags.None, SendCallback, socket);
            // should be at the end of try block and executed if no other issues arise
            return true;    // SendCallback successfully began
        }
        catch
        {
            try
            {   // closing the socket is itself a network action and can be weird with errors
                socket.Shutdown(SocketShutdown.Both);   //option .Both was recommended by autocomplete. subject to change.
                socket.Close(); // ensure socket is closed if send fails for some reason
            }
            catch
            {
            }
            return false;
        }
    }

    /// <summary>
    /// To be used as the callback for finalizing a send operation that was initiated by Send.
    ///
    /// Uses EndSend to finalize the send.
    /// 
    /// This method must not throw, even if an error occurred during the Send operation.
    /// </summary>
    /// <param name="ar">
    /// This is the Socket (not SocketState) that is stored with the callback when
    /// the initial BeginSend is called.
    /// </param>
    private static void SendCallback(IAsyncResult ar)
    {
        try
        {
            Socket socket = (Socket)ar.AsyncState!;
            if (socket.Connected) //If socket is connected, finalizes the connection
                socket.EndSend(ar); // finalizes the connection
        }
        catch
        {
        }

    }


    /// <summary>
    /// Begin the asynchronous process of sending data via BeginSend, using SendAndCloseCallback to finalize the send process.
    /// This variant closes the socket in the callback once complete. This is useful for HTTP servers.
    /// 
    /// If the socket is closed, does not attempt to send.
    /// 
    /// If a send fails for any reason, this method ensures that the Socket is closed before returning.
    /// </summary>
    /// <param name="socket">The socket on which to send the data</param>
    /// <param name="data">The string to send</param>
    /// <returns>True if the send process was started, false if an error occurs or the socket is already closed</returns>
    public static bool SendAndClose(Socket socket, string data)
    {
        try
        {
            if (!socket.Connected)   // if socket is closed, Send operation is not attempted
                return false;
            // create a new state to start BeginSocket
            // according to SendCallback, the state parameter should be the socket parameter specified here.
            // change string data to a byte array
            byte[] toSend = Encoding.UTF8.GetBytes(data);   // may need to switch between UTF8 and ASCII
            IAsyncResult ar = socket.BeginSend(toSend, 0, toSend.Length, SocketFlags.None, SendAndCloseCallback, socket);
            // should be at the end of try block and executed if no other issues arise
            return true;    // SendCallback successfully began
        }
        catch
        {
            try
            {   // closing the socket is itself a network action and can be weird with errors
                socket.Shutdown(SocketShutdown.Both);   //option .Both was recommended by autocomplete. subject to change.
                socket.Close(); // ensure socket is closed if send fails for some reason
            }
            catch
            {
            }
            return false;
        }
    }

    /// <summary>
    /// To be used as the callback for finalizing a send operation that was initiated by SendAndClose.
    ///
    /// Uses EndSend to finalize the send, then closes the socket.
    /// 
    /// This method must not throw, even if an error occurred during the Send operation.
    /// 
    /// This method ensures that the socket is closed before returning.
    /// </summary>
    /// <param name="ar">
    /// This is the Socket (not SocketState) that is stored with the callback when
    /// the initial BeginSend is called.
    /// </param>
    private static void SendAndCloseCallback(IAsyncResult ar)
    {
        try
        {
            Socket socket = (Socket)ar.AsyncState!;
            if (socket.Connected) //If socket is connected finalizes the connection
            {
                socket.EndSend(ar); // finalizes the connection
                socket.Close(); //Closes connection
            }
        }
        catch
        {
        }
    }


    /////////////////////////////////////////////////////////////////////////////////////////
    // private static helper methods by Ash and Zach
    /////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Method to be invoked whenever an error occurs over the network
    /// and the SocketState in question needs to be altered to its error form.
    /// </summary>
    private static void NetworkErrorOccurred(Action<SocketState> toCall, string errorMsg, SocketState? state)
    {
        if (state is null)
        {
            state = new(toCall, errorMsg);
        }
        else
        {
            state.ErrorMessage = errorMsg;
        }
        state.ErrorOccurred = true;
        state.OnNetworkAction(state);
    }
}