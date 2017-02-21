using System;
using System.Text;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;


namespace UnitTest
{
    public sealed class SocketClient : IDisposable
    {
        /// <summary>
		/// Possible FTP return codes.
		/// </summary>
		private enum ReturnCodes
        {
            /// <summary>
            /// Data connection already open; transfer starting.
            /// </summary>
            DataConnectionOpen = 125,

            /// <summary>
            /// File status okay; about to open data connection.
            /// </summary>
            FileStatusOK = 150,

            /// <summary>
            /// Command okay.
            /// </summary>
            CommandOK = 200,

            /// <summary>
            /// Command not implemented, superfluous at this site
            /// </summary>
            CommandNotImplemented = 202,

            /// <summary>
            /// File status.
            /// </summary>
            FileStatus = 213,

            /// <summary>
            /// Service ready for new user.
            /// </summary>
            ServiceReady = 220,

            /// <summary>
            /// Closing data connection. Requested file action successful (for example, file transfer or file abort).
            /// </summary>
            ClosingDataConnection = 226,

            /// <summary>
            /// Entering Passive Mode (h1,h2,h3,h4,p1,p2).
            /// </summary>
            EnteringPassiveMode = 227,

            /// <summary>
            /// User logged in, proceed. Logged out if appropriate.
            /// </summary>
            UserLoggedIn = 230,

            /// <summary>
            /// Requested file action okay, completed.
            /// </summary>
            RequestedFileActionOK = 250,

            /// <summary>
            /// "PATHNAME" created.
            /// </summary>
            PathCreated = 257,

            /// <summary>
            /// User name okay, need password.
            /// </summary>
            UsernameOK = 331,

            /// <summary>
            /// Requested file action pending further information
            /// </summary>
            RequestedFileActionPending = 350
        }
        private IPEndPoint hostEndPoint;
        /// <summary>
		/// The size of the buffers for the Sockets.
		/// </summary>
		internal const int BLOCK_SIZE = 512;
        private ISocket _socket;
        private string _username;
        private string _password;
        /// <summary>
        /// Default constructor using System.Net.Sockets.Socket via the SocketAdapter.
        /// </summary>
        /// <param name="hostEndPoint">The TCP host.</param>
        public SocketClient(IPEndPoint hostEndPoint) : this(new SocketAdapter(), hostEndPoint)
        { }

        /// <summary>
        /// Constructor using custom (e.g. mocked) Socket.
        /// </summary>
        /// <param name="socket">The Socket to use.</param>
        /// <param name="hostEndPoint">The TCP host.</param>
        public SocketClient(ISocket socket, IPEndPoint hostEndPoint)
        {
            this._socket = socket;
            this.hostEndPoint = hostEndPoint;
            OpenSocket();
        }

        /// <summary>
        /// Opens the socket connection to the FTP server.
        /// </summary>
        private void OpenSocket()
        {
            _socket.Connect(hostEndPoint.Address.ToString(), hostEndPoint.Port);
            string[] response = ReadFromSocket();
            if (GetReturnCodeFromResponse(response) != ReturnCodes.ServiceReady)
            {
                throw new Exception("Error while connecting to FTP server. Server responded: " + String.Join(" ", response));
            }
        }
        /// <summary>
		/// Reads the response from the FTP server from the Socket.
		/// </summary>
		/// <returns>The server's response message.</returns>
		private string[] ReadFromSocket()
        {
            MemoryStream ms = new MemoryStream();
            byte[] buffer = new byte[BLOCK_SIZE];
            while (true)
            {
                int bytes = _socket.Receive(buffer, buffer.Length, SocketFlags.None);
                ms.Write(buffer, 0, bytes);
                if (bytes < buffer.Length)
                {
                    break;
                }
            }

            StreamReader sr = new StreamReader(ms);
            ms.Position = 0;
            string response = sr.ReadToEnd();
            sr.Close();
            ms.Close();
            string[] result = response.Split(new char[] { '\n' });
            Console.WriteLine("Response from Server: " + result[0]);
            return result;
        }
        /// <summary>
		/// Parses the server's response for the return code.
		/// </summary>
		/// <param name="response">The response message.</param>
		/// <returns>The return code.</returns>
		private ReturnCodes GetReturnCodeFromResponse(string[] response)
        {
            try
            {
                return (ReturnCodes)Enum.Parse(typeof(ReturnCodes), response[0].Substring(0, 3));
            }
            catch (Exception e)
            {
                throw new Exception("Error while parsing response from FTP server: " + e.Message);
            }
        }


        /// <summary>
        /// Sends an (FTP) command through the socket.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <returns>The return code from the server.</returns>
        private ReturnCodes SendCommand(String command)
        {
            Byte[] cmdBytes = Encoding.ASCII.GetBytes((command + "\r\n").ToCharArray());
            _socket.Send(cmdBytes, cmdBytes.Length, SocketFlags.None);
            return GetReturnCodeFromResponse(ReadFromSocket());
        }


        /// <summary>
        /// Closes the connection to the FTP server.
        /// </summary>
        public void Close()
        {
            SendCommand("QUIT");
        }

        #region IDisposable Member

        public void Dispose()
        {
            if (_socket != null)
            {
                _socket.Close();
            }
        }

        #endregion
    }
}
