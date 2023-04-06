﻿using System.Security.Cryptography;
using Common;
using Common.Protobuf;
using DNToolKit.Net;
using DNToolKit.Protocol;


namespace DNToolKit.PacketProcessors;

public class PacketProcessor
{
    private static readonly RSA ClientPrivate = RSA.Create();
    private MtKey? _key;
    private MtKey? _sessionKey;
    private bool _useSessionKey;
    
    private ulong? _tokenReqSendTime;
    private ulong? _tokenRspServerKey;

    private byte _timesBFed;
    private long _count;
    private DNToolKit _toolKit;
    
    
    
    

    public PacketProcessor(DNToolKit toolKit, string clientRsa)
    {
        ClientPrivate.FromXmlString(clientRsa);
        _toolKit = toolKit;
        _timesBFed = 0;
    }
    

    public void Reset()
    {
        _tokenRspServerKey = null;
        _tokenReqSendTime = null;
        _sessionKey = null;
        _useSessionKey = false;
        _key = null;
        _timesBFed = 0;
        _count = 0;
    }

    public void AddPacket(byte[] data, UdpHandler.Sender sender)
    {
        Work(new EncryptedPacket(data, sender));
    }

    public void Work(EncryptedPacket encryptedPacket)
    {
        var item = encryptedPacket.Data;
        {
            if (!_useSessionKey)
            {
                _key ??= KeyRecovery.FindKey(item);
                _key?.Crypt(item);
            }
            else
            {
                if (_sessionKey is null)
                {
                    _toolKit.LogAction(LogLevel.Debug, "Bruteforcing Key...");
                    //Program.TestBF((long)tokenReqSendTime, tokenRspServerKey, item);
                    _timesBFed++;
                    if (_tokenReqSendTime.HasValue && _tokenRspServerKey.HasValue)
                    {
                        _sessionKey = KeyBruteForcer.BruteForce(item, (long)_tokenReqSendTime.Value, _tokenRspServerKey.Value);
                    }

                }

                if (_sessionKey is null)
                {
                    _toolKit.LogAction(LogLevel.Warn, "something went wrong!");
                }
                _sessionKey?.Crypt(item);

                if (_timesBFed > 10)
                {
                    _toolKit.Close();
                }
            }

            if (item.GetUInt16(0, true) == 0x4567)
            {
                ParsePacketFromData(encryptedPacket);
            }
            else if (_sessionKey is null)
            {
                // Log.Warning("Encrypted Packet got through lol");
            }
            else
            {
                _toolKit.LogAction(LogLevel.Debug, "Invalidating old key... maybe a reconnect?");
                _sessionKey = null;
            }
        }
    }


    private void ParsePacketFromData(EncryptedPacket encryptedPacket)
    {
        //todo: i think i need to handle exceptions but i *did* just check packet magic earlier so idk
        try
        {
            //this is SO UGLY

            var packet = new Packet(encryptedPacket.Data) { Sender = encryptedPacket.Sender };

            var type = packet.PacketType;

            var str = packet.Sender == UdpHandler.Sender.Client ? "C2S" : "S2C";
            _toolKit.LogAction(LogLevel.Info, $"{_count++} | {str} | {type}");

            if (type == Opcode.GetPlayerTokenRsp)
            {
                //ideally we do it based on tokenreq but unless your ping is like 3000 we should be fine
                _tokenReqSendTime = (packet.Metadata.SentMs);

                var tokenRsp = packet.PacketData as GetPlayerTokenRsp;
                
                if (tokenRsp?.ServerRandKey is not null)
                {
                    var key = ClientPrivate.Decrypt(Convert.FromBase64String(tokenRsp.ServerRandKey),
                        RSAEncryptionPadding.Pkcs1);
                    _tokenRspServerKey = (key.GetUInt64(0, true));
                    _useSessionKey = true;
                }
                else
                {
                    _toolKit.LogAction(LogLevel.Warn, "failed to get serverSeed");
                }
            }

            //todo: send packet
            _toolKit.AddGamePacket(packet);
            
            
        }
        catch (Exception e)
        {
            _toolKit.LogAction(LogLevel.Error, e.ToString());
        }
        
    }
}