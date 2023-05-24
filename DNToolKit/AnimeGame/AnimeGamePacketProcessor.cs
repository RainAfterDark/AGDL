using System.Security.Cryptography;
using Common;
using Common.Protobuf;
using DNToolKit.AnimeGame.Crypto;
using DNToolKit.AnimeGame.Models;
using DNToolKit.Configuration.Models;
using DNToolKit.Extensions;
using Google.Protobuf;
using Google.Protobuf.Collections;
using Serilog;

namespace DNToolKit.AnimeGame
{
    /// <summary>
    /// Handles the flow and cryptography of <see cref="AnimeGamePacket"/>s for raw messages.
    /// </summary>
    class AnimeGamePacketProcessor
    {
        private static readonly RSA ClientPrivate = RSA.Create();
        private readonly SniffConfig _config;
        private readonly AnimeGamePacketRecorder _packetRecorder;
        
        private static readonly Dictionary<AbilityInvokeArgument, string> AbilityInvokeParsers = new()
        {
            { AbilityInvokeArgument.MetaModifierChange, "AbilityMetaModifierChange" },
            // { AbilityInvokeArgument.MetaCommandModifierChangeRequest, "" },
            { AbilityInvokeArgument.MetaSpecialFloatArgument, "AbilityMetaSpecialFloatArgument" },
            { AbilityInvokeArgument.MetaOverrideParam, "AbilityScalarValueEntry" },
            { AbilityInvokeArgument.MetaClearOverrideParam, "AbilityScalarValueEntry" },
            { AbilityInvokeArgument.MetaReinitOverridemap, "AbilityMetaReInitOverrideMap" },
            { AbilityInvokeArgument.MetaGlobalFloatValue, "AbilityScalarValueEntry" },
            { AbilityInvokeArgument.MetaClearGlobalFloatValue, "AbilityScalarValueEntry" },
            // { AbilityInvokeArgument.MetaAbilityElementStrength, "" },
            { AbilityInvokeArgument.MetaAddOrGetAbilityAndTrigger, "AbilityMetaAddOrGetAbilityAndTrigger" },
            { AbilityInvokeArgument.MetaSetKilledSetate, "AbilityMetaSetKilledState" },
            { AbilityInvokeArgument.MetaSetAbilityTrigger, "AbilityMetaSetAbilityTrigger" },
            { AbilityInvokeArgument.MetaAddNewAbility, "AbilityMetaAddAbility" },
            // { AbilityInvokeArgument.MetaRemoveAbility, "" },
            { AbilityInvokeArgument.MetaSetModifierApplyEntity, "AbilityMetaSetModifierApplyEntityId" },
            { AbilityInvokeArgument.MetaModifierDurabilityChange, "AbilityMetaModifierDurabilityChange" },
            { AbilityInvokeArgument.MetaElementReactionVisual, "AbilityMetaElementReactionVisual" },
            { AbilityInvokeArgument.MetaSetPoseParameter, "AbilityMetaSetPoseParameter" },
            { AbilityInvokeArgument.MetaUpdateBaseReactionDamage, "AbilityMetaUpdateBaseReactionDamage" },
            { AbilityInvokeArgument.MetaTriggerElementReaction, "AbilityMetaTriggerElementReaction" },
            { AbilityInvokeArgument.MetaLoseHp, "AbilityMetaLoseHp" },
            { AbilityInvokeArgument.MetaDurabilityIsZero, "AbilityMetaDurabilityIsZero" },
            { AbilityInvokeArgument.ActionTriggerAbility, "AbilityActionTriggerAbility" },
            { AbilityInvokeArgument.ActionSetCrashDamage, "AbilityActionSetCrashDamage" },
            // { AbilityInvokeArgument.ActionEffect, "" },
            { AbilityInvokeArgument.ActionSummon, "AbilityActionSummon" },
            { AbilityInvokeArgument.ActionBlink, "AbilityActionBlink" },
            { AbilityInvokeArgument.ActionCreateGadget, "AbilityActionCreateGadget" },
            { AbilityInvokeArgument.ActionApplyLevelModifier, "AbilityApplyLevelModifier" },
            { AbilityInvokeArgument.ActionGenerateElemBall, "AbilityActionGenerateElemBall" },
            { AbilityInvokeArgument.ActionSetRandomOverrideMapValue, "AbilityActionSetRandomOverrideMapValue" },
            { AbilityInvokeArgument.ActionServerMonsterLog, "AbilityActionServerMonsterLog" },
            { AbilityInvokeArgument.ActionCreateTile, "AbilityActionCreateTile" },
            { AbilityInvokeArgument.ActionDestroyTile, "AbilityActionDestroyTile" },
            { AbilityInvokeArgument.ActionFireAfterImage, "AbilityActionFireAfterImage" },
            { AbilityInvokeArgument.ActionDeductStamina, "AbilityActionDeductStamina" },
            { AbilityInvokeArgument.ActionHitEffect, "AbilityActionHitEffect" },
            { AbilityInvokeArgument.ActionSetBulletTrackTarget, "AbilityActionSetBulletTrackTarget" },
            { AbilityInvokeArgument.MixinAvatarSteerByCamera, "AbilityMixinAvatarSteerByCamera" },
            // { AbilityInvokeArgument.MixinMonsterDefend, "" },
            { AbilityInvokeArgument.MixinWindZone, "AbilityMixinWindZone" },
            { AbilityInvokeArgument.MixinCostStamina, "AbilityMixinCostStamina" },
            { AbilityInvokeArgument.MixinEliteShield, "AbilityMixinEliteShield" },
            { AbilityInvokeArgument.MixinElementShield, "AbilityMixinElementShield" },
            { AbilityInvokeArgument.MixinGlobalShield, "AbilityMixinGlobalShield" },
            { AbilityInvokeArgument.MixinShieldBar, "AbilityMixinShieldBar" },
            { AbilityInvokeArgument.MixinWindSeedSpawner, "AbilityMixinWindSeedSpawner" },
            { AbilityInvokeArgument.MixinDoActionByElementReaction, "AbilityMixinDoActionByElementReaction" },
            { AbilityInvokeArgument.MixinFieldEntityCountChange, "AbilityMixinFieldEntityCountChange" },
            { AbilityInvokeArgument.MixinScenePropSync, "AbilityMixinScenePropSync" },
            { AbilityInvokeArgument.MixinWidgetMpSupport, "AbilityMixinWidgetMpSupport" },
            { AbilityInvokeArgument.MixinDoActionBySelfModifierElementDurabilityRatio, "AbilityMixinDoActionBySelfModifierElementDurabilityRatio" },
            { AbilityInvokeArgument.MixinFireworksLauncher, "AbilityMixinFireworksLauncher" },
            { AbilityInvokeArgument.MixinAttackResultCreateCount, "AttackResultCreateCount" },
            { AbilityInvokeArgument.MixinUgcTimeControl, "AbilityMixinUGCTimeControl" },
            { AbilityInvokeArgument.MixinAvatarCombat, "AbilityMixinAvatarCombat" },
            // { AbilityInvokeArgument.MixinDeathZoneRegionalPlayMixin, "" },
            { AbilityInvokeArgument.MixinUiInteract, "AbilityMixinUIInteract" },
            { AbilityInvokeArgument.MixinShootFromCamera, "AbilityMixinShootFromCamera" },
            { AbilityInvokeArgument.MixinEraseBrickActivity, "AbilityMixinEraseBrickActivity" },
            { AbilityInvokeArgument.MixinBreakout, "AbilityMixinBreakout" },
        };
        
        private static readonly Dictionary<CombatTypeArgument, string> CombatInvokeParsers = new()
        {
            { CombatTypeArgument.EvtBeingHit, "EvtBeingHitInfo" },
            { CombatTypeArgument.AnimatorStateChanged, "EvtAnimatorStateChangedInfo" },
            { CombatTypeArgument.FaceToDir, "EvtFaceToDirInfo" },
            { CombatTypeArgument.SetAttackTarget, "EvtSetAttackTargetInfo" },
            { CombatTypeArgument.RushMove, "EvtRushMoveInfo" },
            { CombatTypeArgument.AnimatorParameterChanged, "EvtAnimatorParameterInfo" },
            { CombatTypeArgument.EntityMove, "EntityMoveInfo" },
            { CombatTypeArgument.SyncEntityPosition, "EvtSyncEntityPositionInfo" },
            { CombatTypeArgument.SteerMotionInfo, "EvtCombatSteerMotionInfo" },
            { CombatTypeArgument.ForceSetPosInfo, "EvtCombatForceSetPosInfo" },
            { CombatTypeArgument.CompensatePosDiff, "EvtCompensatePosDiffInfo" },
            { CombatTypeArgument.MonsterDoBlink, "EvtMonsterDoBlink" },
            { CombatTypeArgument.FixedRushMove, "EvtFixedRushMove" },
            { CombatTypeArgument.SyncTransform, "EvtSyncTransform" },
            { CombatTypeArgument.LightCoreMove, "EvtLightCoreMove" },
            { CombatTypeArgument.BeingHealedNtf, "EvtBeingHealedNotify" },
            { CombatTypeArgument.SkillAnchorPositionNtf, "EvtSyncSkillAnchorPosition" },
            { CombatTypeArgument.GrapplingHookMove, "EvtGrapplingHookMove" },
        };

        private MtKey? _key;
        private MtKey? _sessionKey;
        private long? _sessionSeed;
        private bool _useSessionKey;

        private ulong? _tokenReqSendTime;
        private ulong? _tokenRspServerKey;

        private byte _countBruteForce;
        private long _count;

        /// <summary>
        /// The event to pass on anime game packets.
        /// </summary>
        public event EventHandler<AnimeGamePacket>? PacketProcessed;

        /// <summary>
        /// The event to pass on the timestamp used on a successful bruteforce.
        /// </summary>
        public event EventHandler<long>? KeyFound;

        /// <summary>
        /// Create a new instance of <see cref="AnimeGamePacketProcessor"/>.
        /// </summary>
        /// <param name="config">The config to setup communication.</param>
        public AnimeGamePacketProcessor(SniffConfig config)
        {
            _config = config;
            _packetRecorder = new AnimeGamePacketRecorder(config.PacketRecordPath);

            ClientPrivate.FromXmlString(config.ClientPrivateRsa);

            Reset();
        }

        /// <summary>
        /// Resets the state of the processor.
        /// </summary>
        public void Reset()
        {
            _tokenRspServerKey = null;
            _tokenReqSendTime = null;

            _key = null;
            _sessionKey = null;
            _sessionSeed = null;
            _useSessionKey = false;

            _countBruteForce = 0;
            _count = 0;
        }

        /// <summary>
        /// Initializes the processor and reads already recorded messages, if the config toggled it.
        /// </summary>
        public void Initialize()
        {
            if (!_config.LoadPackets) 
                return;

            foreach (var packet in _packetRecorder.ReadMessages())
                OnPacketReceived(packet);
        }

        /// <summary>
        /// Process a raw message and send out the parsed <see cref="AnimeGamePacket"/>.
        /// </summary>
        /// <param name="data">The raw message to process.</param>
        /// <param name="sender">The sender of the raw message.</param>
        /// <param name="keyReceived">If the key for decryption could be received or recovered. Is only <see langword="false"/>, if the <see cref="GetPlayerTokenRsp"/> was not sufficient to recover a key.</param>
        /// <remarks>This method starts the event invocation chain that exposes <see cref="AnimeGamePacket"/>s publicly.</remarks>
        public void Process(byte[] data, Sender sender, out bool keyReceived)
        {
            keyReceived = true;

            if (!_useSessionKey)
            {
                _key ??= DispatchKeyLookup.Find(data);
                _key?.ApplyTo(data);
            }
            else
            {
                if (_sessionKey is null)
                {
                    Log.Debug("Brute forcing Key...");

                    _countBruteForce++;

                    if (!_tokenReqSendTime.HasValue)
                        Log.Warning("Did not receive player token send time yet.");
                    else if (!_tokenRspServerKey.HasValue)
                        Log.Warning("Did not receive server key yet.");
                    else
                    {
                        (_sessionKey, _sessionSeed) = KeyBruteForcer.BruteForce(data, _tokenRspServerKey.Value, 
                            _config.LastValidReqSentTime, (long)_tokenReqSendTime.Value);
                        if (_sessionKey is null)
                            Log.Error("Cannot find seed: (@{Data} : {TokenReqSendTime} : {TokenRspServerKey}", 
                                data, _tokenReqSendTime.Value, _tokenRspServerKey.Value);
                        else
                            KeyFound?.Invoke(this, _sessionSeed!.Value);
                    }
                }

                _sessionKey?.ApplyTo(data);

                if (_countBruteForce > 10)
                {
                    Log.Error("Brute forcing has failed {Count} times, so make sure you login on a freshly launched client.", _countBruteForce);
                    keyReceived = false;

                    return;
                }
            }

            if (data.GetUInt16(0) == 0x4567)
            {
                var packet = ParsePacket(data, sender);
                if (packet != null) ProcessPacket(packet);
            }
            else if (_sessionKey is null)
            {
                Log.Warning("Encrypted Packet detected.");
            }
            else
            {
                Log.Warning("False positive has occurred. Restart the game client and app.");
                _sessionKey = null;
            }
        }

        /// <summary>
        /// Do additional processing for an already parsed <see cref="AnimeGamePacket"/>.
        /// </summary>
        /// <param name="packet">The packet to process.</param>
        private void ProcessPacket(AnimeGamePacket packet)
        {
            switch (packet.PacketType)
            {
                case Opcode.UnionCmdNotify:
                    ProcessUnionCmdNotify(packet);
                    break;
                case Opcode.ClientAbilityInitFinishNotify:
                    ProcessAbilityInvokes(packet, ((ClientAbilityInitFinishNotify?)packet.ProtoBuf)?.Invokes);
                    break;
                case Opcode.ClientAbilityChangeNotify:
                    ProcessAbilityInvokes(packet, ((ClientAbilityChangeNotify?)packet.ProtoBuf)?.Invokes);
                    break;
                case Opcode.AbilityInvocationsNotify:
                    ProcessAbilityInvokes(packet, ((AbilityInvocationsNotify?)packet.ProtoBuf)?.Invokes);
                    break;
                case Opcode.CombatInvocationsNotify:
                    ProcessCombatInvocationsNotify(packet);
                    break;
            }
            OnPacketReceived(packet);
        }

        /// <summary>
        /// Process a <see cref="UnionCmdNotify"/> type <see cref="AnimeGamePacket"/>.
        /// </summary>
        /// <param name="packet">The packet to process.</param>
        private void ProcessUnionCmdNotify(AnimeGamePacket packet)
        {
            var cmdList = ((UnionCmdNotify?)packet.ProtoBuf)?.CmdList;
            if (cmdList is null) return;
            foreach (var unionCmd in cmdList)
            {
                var cmdPacket = AnimeGamePacket.ParseRaw(unionCmd.Body.ToByteArray(), unionCmd.MessageId, packet.Sender);
                cmdPacket.ParentPacket = packet;
                packet.ChildPackets.Add(cmdPacket);
                ProcessPacket(cmdPacket);
            }
        }

        /// <summary>
        /// Process a <see cref="RepeatedField"/> of <see cref="AbilityInvokeEntry"/> from a <see cref="AnimeGamePacket"/>.
        /// </summary>
        /// <param name="parent">The parent packet.</param>
        /// <param name="invokes">The repeated field of invokes.</param>
        private void ProcessAbilityInvokes(AnimeGamePacket parent, RepeatedField<AbilityInvokeEntry>? invokes)
        {
            if (!_config.ParseInvocations || invokes is null) return;
            foreach (var entry in invokes)
            {
                if (!AbilityInvokeParsers.TryGetValue(entry.ArgumentType, out var typeName)) continue;
                var entryPacket = ParseEntryData(entry.AbilityData, typeName, parent.Sender);
                if (entryPacket is null) continue;
                entryPacket.ParentPacket = parent;
                parent.ChildPackets.Add(entryPacket);
                OnPacketReceived(entryPacket);
            }
        }

        /// <summary>
        /// Process a <see cref="CombatInvocationsNotify"/> type <see cref="AnimeGamePacket"/>.
        /// </summary>
        /// <param name="packet">The packet to process.</param>
        private void ProcessCombatInvocationsNotify(AnimeGamePacket packet)
        {
            if (!_config.ParseInvocations) return;
            var invokes = ((CombatInvocationsNotify?)packet.ProtoBuf)?.InvokeList;
            if (invokes is null) return;
            foreach (var entry in invokes)
            {
                if (!CombatInvokeParsers.TryGetValue(entry.ArgumentType, out var typeName)) continue;
                var entryPacket = ParseEntryData(entry.CombatData, typeName, packet.Sender);
                if (entryPacket is null) continue;
                entryPacket.ParentPacket = packet;
                packet.ChildPackets.Add(entryPacket);
                OnPacketReceived(entryPacket);
            }
        }

        /// <summary>
        /// Parses a raw invoke entry data to an <see cref="AnimeGamePacket"/>.
        /// </summary>
        /// <param name="data">The raw <see cref="ByteString"/> to parse.</param>
        /// <param name="typeName">The name of the parser to use.</param>
        /// <param name="sender">The sender of the parent packet.</param>
        /// <returns>The parsed <see cref="AnimeGamePacket"/> if a valid parser is available, otherwise <see langword="null"/>.</returns>
        private AnimeGamePacket? ParseEntryData(ByteString data, string typeName, Sender sender)
        {
            var parser = ProtobufFactory.GetPacketTypeParser(typeName);
            return parser is null ? null : AnimeGamePacket.ParseRaw(data, parser, sender);
        }

        /// <summary>
        /// Parses a raw message to an <see cref="AnimeGamePacket"/>.
        /// </summary>
        /// <param name="data">The raw message to parse.</param>
        /// <param name="sender">The sender of the raw message.</param>
        /// <returns>The parsed <see cref="AnimeGamePacket"/>. Otherwise <see langword="null"/>.</returns>
        private AnimeGamePacket? ParsePacket(byte[] data, Sender sender)
        {
            // Parse the packet
            AnimeGamePacket packet;
            try
            {
                packet = AnimeGamePacket.Parse(data, sender);
            }
            catch (Exception e)
            {
                Log.Fatal(e, "Could not parse anime game packet.");
                return null;
            }
            
            Log.Verbose("{Count} {Sender}: {PacketType}", _count++, sender, packet.PacketType);

            // Just return the packet, if it's not the player token response
            if (packet.PacketType != Opcode.GetPlayerTokenRsp)
                return packet;

            // Extract session key
            _tokenReqSendTime = packet.Metadata?.SentMs;

            var tokenRsp = packet.ProtoBuf as GetPlayerTokenRsp;
            Log.Verbose("GetPlayerTokenRsp: {@TokenRsp}", tokenRsp);

            if (tokenRsp?.ServerRandKey is null)
                Log.Warning("Failed to receive random server key.");
            else
            {
                var key = ClientPrivate.Decrypt(Convert.FromBase64String(tokenRsp.ServerRandKey), RSAEncryptionPadding.Pkcs1);

                _tokenRspServerKey = key.GetUInt64(0);
                _useSessionKey = true;
            }

            return packet;
        }

        /// <summary>
        /// Passes on the <see cref="AnimeGamePacket"/> to <see cref="PacketProcessed"/>.
        /// </summary>
        /// <param name="packet">The <see cref="AnimeGamePacket"/> to pass on.</param>
        private void OnPacketReceived(AnimeGamePacket packet)
        {
            if (_config.PersistPackets)
                _packetRecorder.PersistMessage(packet);
            
            PacketProcessed?.Invoke(this, packet);
        }
    }
}