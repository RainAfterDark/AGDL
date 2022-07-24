﻿using DNToolKit.Net;
using DNToolKit.PacketProcessors;
using PacketDotNet;
using Serilog;
using SharpPcap;

namespace DNToolKit.Sniffer;

public class UdpHandler
{
    private KCP? _client;
    private KCP? _server;
    private PacketProcessor _processor;
    
    public UdpHandler()
    {
        _processor = new PacketProcessor();
    }

    public void HandleRawCapture(RawCapture rawCapture)
    {
        IPv4Packet ipv4Packet = PacketDotNet.Packet.ParsePacket(LinkLayers.Ethernet, rawCapture.Data).Extract<IPv4Packet>();
        UdpPacket udpPacket = ipv4Packet.Extract<UdpPacket>();
        Destination destination = udpPacket.DestinationPort is 22101 or 22102 ? Destination.Server : Destination.Client;
        byte[] packetBytes = udpPacket.PayloadData;


        // Log.Information($"Recieved {packetBytes.Length} bytes");
        if(packetBytes.Length == 20)
        {
            try
            {
                var magic = packetBytes.GetUInt32(0, true);
                var conv = packetBytes.GetUInt32(4, true);
                var token = packetBytes.GetUInt32(8, true);
                
                switch (magic)
                {
                    case 0x145:

                        if (destination == Destination.Client)
                        {
                            Log.Information("Server Handshake : {Conv}, {Token}", conv, token);
                            _client = new KCP(conv, token,"Client",_processor);
                            _server = new KCP(conv, token,"Server",_processor);
                        }
                        //TODO: handle this
                        break;
                    case 0x194:
                        Log.Information("Disconnect Handshake");
                        _client?.Stop()                                                                                                             ;
                        _server?.Stop();
                        break;
                    case 0xFF:                                  
                        break;
                    default:
                        //unhandled handshake
                        Log.Error("Unhandled Handshake", magic);
                        break;
                }
            }
            catch (Exception e)
            {
                Log.Error("Handshake Error: {a}", e.ToString());
            }
            return;
        }
        
        if (_client is not null && _server is not null)
        {
            _ = Destination.Client == destination ? _client.Input(packetBytes) : _server.Input(packetBytes);
            return;
        } 
        
        //Log.Information($"Ignoring {packetBytes.Length} bytes going to {destination}");

    }
    enum Destination
    {
        Server,
        Client
    }

    public void Close()
    {
        _client?.Stop();
        _server?.Stop();
    }
}