using Mirror;
using Mirror.SimpleWeb;
using System;
using System.Net;
using System.Net.Sockets;
using kcp2k;
using UnityEngine;

namespace Mirror.Transports.Edgegap.EdgegapRelay
{
    public enum MessageType : byte
    {
        Ping = 1,
        Data = 2
    }

    public enum ConnectionState
    {
        Disconnected,
        Checking,
        Connected
    }

    public class EdgegapKcpServer : KcpServer
    {
        // ping interval in seconds
        const float PingInterval = 1f;

        // need buffer larger than KcpClient.rawReceiveBuffer to add metadata
        readonly byte[] relayReceiveBuffer;

        // authentication
        public uint userId;
        public uint sessionId;
        public ConnectionState state = ConnectionState.Disconnected;

        // server is an UDP client talking to relay
        protected Socket relaySocket;
        public EndPoint remoteEndPoint;

        // ping
        double lastPingTime;

        // custom 'active'. while connected to relay
        bool relayActive;

        public EdgegapKcpServer(
            Action<int, IPEndPoint> OnConnected,
            Action<int, ArraySegment<byte>, KcpChannel> OnData,
            Action<int> OnDisconnected,
            Action<int, ErrorCode, string> OnError,
            KcpConfig config)
              : base(OnConnected, OnData, OnDisconnected, OnError, config)
        {
            relayReceiveBuffer = new byte[config.Mtu + Kcp.OVERHEAD];
        }

        public override bool IsActive() => relayActive;

        // custom start function with relay parameters; connects udp client.
        public void Start(string relayAddress, ushort relayPort, uint userId, uint sessionId)
        {
            // reset last state
            state = ConnectionState.Checking;
            this.userId = userId;
            this.sessionId = sessionId;

            // try resolve host name
            try
            {
                IPAddress[] addresses = Dns.GetHostAddresses(relayAddress);
                if (addresses.Length == 0)
                {
                    OnError(0, ErrorCode.DnsResolve, $"Failed to resolve host: {relayAddress}");
                    return;
                }

                // create socket
                remoteEndPoint = new IPEndPoint(addresses[0], relayPort);
                relaySocket = new Socket(remoteEndPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
                relaySocket.Blocking = false;

                // configure buffer sizes
                relaySocket.ReceiveBufferSize = config.RecvBufferSize;
                relaySocket.SendBufferSize = config.SendBufferSize;

                // bind to endpoint for Send/Receive instead of SendTo/ReceiveFrom
                relaySocket.Connect(remoteEndPoint);
                relayActive = true;
            }
            catch (Exception ex)
            {
                OnError(0, ErrorCode.DnsResolve, $"Failed to start server: {ex.Message}");
            }
        }

        public override void Stop()
        {
            relayActive = false;
        }

        protected override bool RawReceiveFrom(out ArraySegment<byte> segment, out int connectionId)
        {
            segment = default;
            connectionId = 0;

            if (relaySocket == null) return false;

            try
            {
                // TODO need separate buffer. don't write into result yet. only payload

                if (relaySocket.ReceiveNonBlocking(relayReceiveBuffer, out ArraySegment<byte> content))
                {
                    using (NetworkReaderPooled reader = NetworkReaderPool.Get(content))
                    {
                        // parse message type
                        if (reader.Remaining == 0)
                        {
                            Debug.LogWarning($"EdgegapServer: message of {content.Count} is too small to parse header.");
                            return false;
                        }
                        byte messageType = reader.ReadByte();

                        // handle message type
                        switch (messageType)
                        {
                            case (byte)MessageType.Ping:
                            {
                                // parse state
                                if (reader.Remaining < 1) return false;
                                ConnectionState last = state;
                                state = (ConnectionState)reader.ReadByte();

                                // log state changes for debugging.
                                if (state != last) Debug.Log($"EdgegapServer: state updated to: {state}");

                                // return true indicates Mirror to keep checking
                                // for further messages.
                                return true;
                            }
                            case (byte)MessageType.Data:
                            {
                                // parse connectionId and payload
                                if (reader.Remaining <= 4)
                                {
                                    Debug.LogWarning($"EdgegapServer: message of {content.Count} is too small to parse connId.");
                                    return false;
                                }

                                connectionId = reader.ReadInt();
                                segment = reader.ReadBytesSegment(reader.Remaining);
                                // Debug.Log($"EdgegapServer: receiving from connId={connectionId}: {segment.ToHexString()}");
                                return true;
                            }
                            // wrong message type. return false, don't throw.
                            default: return false;
                        }
                    }
                }
            }
            catch (SocketException e)
            {
                kcp2k.Log.Info($"EdgegapServer: looks like the other end has closed the connection. This is fine: {e}");
            }
            return false;
        }

        protected override void RawSend(int connectionId, ArraySegment<byte> data)
        {
            using (NetworkWriterPooled writer = NetworkWriterPool.Get())
            {
                // Debug.Log($"EdgegapServer: sending to connId={connectionId}: {data.ToHexString()}");
                writer.WriteUInt(userId);
                writer.WriteUInt(sessionId);
                writer.WriteByte((byte)MessageType.Data);
                writer.WriteInt(connectionId);
                writer.WriteBytes(data.Array, data.Offset, data.Count);
                ArraySegment<byte> message = writer;

                try
                {
                    relaySocket.SendNonBlocking(message);
                }
                catch (SocketException e)
                {
                    kcp2k.Log.Error($"KcpRleayServer: RawSend failed: {e}");
                }
            }
        }

        void SendPing()
        {
            using (NetworkWriterPooled writer = NetworkWriterPool.Get())
            {
                writer.WriteUInt(userId);
                writer.WriteUInt(sessionId);
                writer.WriteByte((byte)MessageType.Ping);
                ArraySegment<byte> message = writer;

                try
                {
                    relaySocket.SendNonBlocking(message);
                }
                catch (SocketException e)
                {
                    Debug.LogWarning($"EdgegapServer: failed to ping. perhaps the relay isn't running? {e}");
                }
            }
        }

        public override void TickOutgoing()
        {
            if (relayActive)
            {
                // ping every interval for keepalive & handshake
                if (NetworkTime.localTime >= lastPingTime + PingInterval)
                {
                    SendPing();
                    lastPingTime = NetworkTime.localTime;
                }
            }

            // base processing
            base.TickOutgoing();
        }
    }
}
