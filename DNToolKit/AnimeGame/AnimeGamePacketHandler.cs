﻿using DNToolKit.AnimeGame.Models;
using DNToolKit.Configuration;
using DNToolKit.Configuration.Models;
using DNToolKit.Extensions;
using DNToolKit.Net;
using DNToolKit.Protocol;
using DNToolKit.Protocol.Events;
using PacketDotNet;
using Serilog;

namespace DNToolKit.AnimeGame
{
    /// <summary>
    /// An <see cref="UdpHandler"/> to handle UDP packages from the anime game.
    /// </summary>
    class AnimeGamePacketHandler : UdpHandler
    {
        private readonly AnimeGamePacketProcessor _processor;
        private readonly Config _config;
        private KCP? _client;
        private KCP? _server;

        /// <summary>
        /// The event to pass on anime game packets.
        /// </summary>
        public event EventHandler<AnimeGamePacket>? PacketReceived;
        /// <summary>
        /// The event to signal, that the key was not recoverable.
        /// </summary>
        public event EventHandler? KeyNotRecovered;
        public event EventHandler<long>? KeyFound;
        public event EventHandler? Disconnected;

        /// <summary>
        /// Create a new instance of <see cref="AnimeGamePacketHandler"/>.
        /// </summary>
        /// <param name="sniffer">The <see cref="PCapSniffer"/> to receive raw packets to handle from.</param>
        /// <param name="config">The config to setup communication.</param>
        public AnimeGamePacketHandler(PCapSniffer sniffer, Config config) : base(sniffer)
        {
            _config = config;
            _processor = new AnimeGamePacketProcessor(config.SniffConfig);
            _processor.PacketProcessed += AnimeGamePacketProcessed;
            _processor.KeyFound += OnKeyFound;
        }

        /// <summary>
        /// Initializes this instance.
        /// </summary>
        public void Initialize()
        {
            _processor.Initialize();
        }

        /// <inheritdoc cref="UdpHandler.ProcessUdpPacket"/>
        protected override void ProcessUdpPacket(UdpPacket packet)
        {
            var sender = packet.DestinationPort is 22101 or 22102 ? Sender.Client : Sender.Server;

            if (packet.PayloadData.Length == 20)
            {
                var magic = packet.PayloadData.GetUInt32(0);
                var conv = packet.PayloadData.GetUInt32(4);
                var token = packet.PayloadData.GetUInt32(8);

                switch (magic)
                {
                    // Server Handshake
                    case 0x145:
                        if (sender != Sender.Server)
                            break;

                        Log.Debug("Server Handshake: {Conv}, {Token}", conv, token);

                        _processor.Reset();

                        _client = new KCP(conv, token, Sender.Client);
                        _server = new KCP(conv, token, Sender.Server);

                        _client.MessageReceived += KcpMessageReceived;
                        _server.MessageReceived += KcpMessageReceived;

                        break;

                    // Disconnect
                    case 0x194:
                        if (sender == Sender.Server)
                            break;

                        if (_client is null)
                            break;

                        Disconnected?.Invoke(this, EventArgs.Empty);
                        Log.Information("{Sender} disconnected.", sender);
                        Log.Warning("Relaunch your client to continue capturing packets!");

                        break;

                    case 0xFF:
                        // Ignore
                        break;

                    default:
                        Log.Error("Unhandled Handshake {MagicBytes}.", magic);
                        return;
                }
            }

            switch (sender)
            {
                case Sender.Client when _client is not null:
                    _client.Input(packet.PayloadData);
                    break;

                case Sender.Server when _server is not null:
                    _server.Input(packet.PayloadData);
                    break;
            }
        }

        /// <inheritdoc cref="UdpHandler.DisposeInternal"/>
        protected override void DisposeInternal()
        {
            if (_client != null)
            {
                _client.MessageReceived -= KcpMessageReceived;
                _client = null;
            }

            if (_server != null)
            {
                _server.MessageReceived -= KcpMessageReceived;
                _server = null;
            }

            _processor.PacketProcessed -= AnimeGamePacketProcessed;
        }

        /// <summary>
        /// Process raw messages received from KCP.
        /// </summary>
        /// <param name="sender">The sender object.</param>
        /// <param name="messageReceived">The raw message to process.</param>
        private void KcpMessageReceived(object? sender, MessageReceivedEventArgs messageReceived)
        {
            _processor.Process(messageReceived.Message, messageReceived.Sender, out var keyReceived);

            if (!keyReceived)
                OnKeyNotRecovered();
        }

        /// <summary>
        /// Pass on <see cref="AnimeGamePacket"/> to <see cref="PacketReceived"/>.
        /// </summary>
        /// <param name="sender">The sender object.</param>
        /// <param name="packet">The packet to pass on.</param>
        private void AnimeGamePacketProcessed(object? sender, AnimeGamePacket packet)
        {
            OnPacketReceived(packet);
        }

        /// <summary>
        /// Pass on <see cref="AnimeGamePacket"/> to <see cref="PacketReceived"/>.
        /// </summary>
        /// <param name="packet">The packet to pass on.</param>
        private void OnPacketReceived(AnimeGamePacket packet)
        {
            PacketReceived?.Invoke(this, packet);
        }

        /// <summary>
        /// Invoke event <see cref="KeyNotRecovered"/>.
        /// </summary>
        private void OnKeyNotRecovered()
        {
            KeyNotRecovered?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Save the sendTime of the successful bruteforce.
        /// </summary>
        /// <param name="sender">The sender object.</param>
        /// <param name="sendTime">The timestamp of the successful bruteforce.</param>
        private void OnKeyFound(object? sender, long sendTime)
        {
            _config.SniffConfig.LastValidReqSentTime = sendTime;
            ConfigurationProvider.SaveConfig(_config);
            KeyFound?.Invoke(sender, sendTime);
        }
    }
}
