﻿using Common;
using Common.Protobuf;
using DNToolKit.AnimeGame.Models;
using DNToolKit.Extensions;
using Google.Protobuf;

namespace DNToolKit.AnimeGame
{
    /// <summary>
    /// A packet send between the client and server of the anime game.
    /// </summary>
    public class AnimeGamePacket
    {
        /// <summary>
        /// The magic at the start of a decrypted anime game packet.
        /// </summary>
        public const int Magic = 0x4567;
        /// <summary>
        /// The magic at the end of a decrypted anime game packet.
        /// </summary>
        public const int Magic2 = 0x89AB;

        /// <summary>
        /// The raw metadata buffer.
        /// </summary>
        public byte[]? MetadataBytes { get; }
        /// <summary>
        /// The parsed metadata.
        /// </summary>
        public PacketHead? Metadata { get; }

        /// <summary>
        /// The raw protobuf buffer.
        /// </summary>
        public byte[] ProtoBufBytes { get; }
        /// <summary>
        /// The parsed protobuf message.
        /// </summary>
        public IMessage? ProtoBuf { get; }

        /// <summary>
        /// The CmdId for the protobuf message.
        /// </summary>
        public Opcode PacketType { get; }

        /// <summary>
        /// The sender of the message.
        /// </summary>
        public Sender Sender { get; }
        
        /// <summary>
        /// The parent of the packet, if it was nested
        /// </summary>
        public AnimeGamePacket? ParentPacket { get; set; }
        /// <summary>
        /// The children of the packet, if it contained nested data
        /// </summary>
        public List<AnimeGamePacket> ChildPackets { get; } = new();

        private AnimeGamePacket(Opcode packetType, byte[]? metaDataBytes, byte[] protoBufBytes, Sender sender)
        {
            PacketType = packetType;
            Sender = sender;
            MetadataBytes = metaDataBytes;
            ProtoBufBytes = protoBufBytes;

            // Parse metadata
            if (metaDataBytes != null)
                Metadata = PacketHead.Parser.ParseFrom(metaDataBytes);

            // Parse ProtoBuf data
            var parser = ProtobufFactory.GetPacketTypeParser(packetType);
            ProtoBuf = parser?.ParseFrom(protoBufBytes);
        }
        
        private AnimeGamePacket(MessageParser parser, ByteString protoBufBytes, Sender sender)
        {
            PacketType = Opcode.None;
            Sender = sender;
            ProtoBufBytes = protoBufBytes.ToByteArray();
            ProtoBuf = parser.ParseFrom(protoBufBytes);
        }

        /// <summary>
        /// Parse a decrypted packet received from the client-server connection.
        /// </summary>
        /// <param name="data">The decrypted packet to parse.</param>
        /// <param name="sender">The sender of the packet.</param>
        /// <returns>The parsed <see cref="AnimeGamePacket"/>.</returns>
        /// <exception cref="ArgumentException"></exception>
        public static AnimeGamePacket Parse(byte[] data, Sender sender)
        {
            // Check packet magics
            var magic = data.GetUInt16(0);
            if (magic != Magic)
                throw new ArgumentException($"Invalid Packet Magic at start. (Read: {magic}, Expected: {Magic})");

            var magic2 = data.GetUInt16(data.Length - 2);
            if (magic2 != Magic2)
                throw new ArgumentException($"Invalid Packet Magic at end. (Read: {magic2}, Expected: {Magic2})");

            // Extract data lengths
            var metaDataLength = (int)data.GetUInt16(4);
            var protoDataLength = (int)data.GetUInt32(6);

            var expectedLength = metaDataLength + protoDataLength + 12;
            if (expectedLength != data.Length)
                throw new ArgumentException($"Packet does not contain the correct amount of bytes. (MetaDataLength: {metaDataLength}, ProtoDataLength: {protoDataLength}, ExpectedLength: {expectedLength})");

            // Extract data buffers
            var packetType = (Opcode)data.GetUInt16(2);

            var metaDataBytes = data.AsSpan(10, metaDataLength).ToArray();
            var protoBufBytes = data.AsSpan(10 + metaDataLength, protoDataLength).ToArray();

            // Create packet model
            return new AnimeGamePacket(packetType, metaDataBytes, protoBufBytes, sender);
        }

        /// <summary>
        /// Parse a decrypted protobuf message.
        /// </summary>
        /// <param name="data">The decrypted protobuf message.</param>
        /// <param name="cmdId">The packet type of the protobuf message.</param>
        /// <param name="sender">The sender of the protobuf message.</param>
        /// <returns>The parsed <see cref="AnimeGamePacket"/> with no metadata.</returns>
        public static AnimeGamePacket ParseRaw(byte[] data, uint cmdId, Sender sender)
        {
            return ParseRaw(data, null, cmdId, sender);
        }

        /// <summary>
        /// Parse a decrypted protobuf message.
        /// </summary>
        /// <param name="data">The decrypted protobuf message.</param>
        /// <param name="meta">The potential metadata for the packet.</param>
        /// <param name="cmdId">The packet type of the protobuf message.</param>
        /// <param name="sender">The sender of the protobuf message.</param>
        /// <returns>The parsed <see cref="AnimeGamePacket"/>.</returns>
        public static AnimeGamePacket ParseRaw(byte[] data, byte[]? meta, uint cmdId, Sender sender)
        {
            return new AnimeGamePacket((Opcode)cmdId, meta, data, sender);
        }

        /// <summary>
        /// Parse a raw <see cref="ByteString"/> message, given a parser.
        /// </summary>
        /// <param name="data">The raw <see cref="ByteString"/>.</param>
        /// <param name="parser">The parser to use.</param>
        /// <param name="sender">The sender of the parent packet.</param>
        /// <returns>The parsed <see cref="AnimeGamePacket"/>.</returns>
        public static AnimeGamePacket ParseRaw(ByteString data, MessageParser parser, Sender sender)
        {
            return new AnimeGamePacket(parser, data, sender);
        }
    }
}
