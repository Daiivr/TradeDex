using PKHeX.Core;
using PKHeX.Core.Searching;
using SysBot.Base;
using SysBot.Base.Util;
using SysBot.Pokemon.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using static SysBot.Base.SwitchButton;
using static SysBot.Pokemon.BasePokeDataOffsetsBS;
using static SysBot.Pokemon.TradeHub.SpecialRequests;

namespace SysBot.Pokemon;

// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
public class PokeTradeBotBS : PokeRoutineExecutor8BS, ICountBot, ITradeBot, IDisposable
{
    private readonly PokeTradeHub<PB8> Hub;
    private readonly TradeAbuseSettings AbuseSettings;
    private readonly FolderSettings DumpSetting;
    private readonly TradeSettings TradeSettings;

    // Cached offsets that stay the same per session.
    private ulong BoxStartOffset;

    // Track the last Pokémon we were offered since it persists between trades.
    private byte[] lastOffered = new byte[8];

    private ulong LinkTradePokemonOffset;

    private ulong SoftBanOffset;

    private ulong UnionGamingOffset;

    private ulong UnionTalkingOffset;

    public event EventHandler<Exception>? ConnectionError;
    public event EventHandler? ConnectionSuccess;
    public event Action<int>? TradeProgressChanged;


    public ICountSettings Counts => TradeSettings;

    /// <summary>
    /// Tracks failed synchronized starts to attempt to re-sync.
    /// </summary>
    public int FailedBarrier { get; private set; }

    /// <summary>
    /// Synchronized start for multiple bots.
    /// </summary>
    public bool ShouldWaitAtBarrier { get; private set; }

    private bool _disposed = false;

    public PokeTradeBotBS(PokeTradeHub<PB8> hub, PokeBotState config) : base(config)
    {
        Hub = hub;
        AbuseSettings = Hub.Config.TradeAbuse;
        DumpSetting = Hub.Config.Folder;
        TradeSettings = Hub.Config.Trade;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // Dispose managed resources
                // Unsubscribe event handlers
                ConnectionError = null;
                ConnectionSuccess = null;
            }

            // Dispose unmanaged resources if any

            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~PokeTradeBotBS()
    {
        Dispose(false);
    }

    public override async Task HardStop()
    {
        UpdateBarrier(false);
        await CleanExit(CancellationToken.None).ConfigureAwait(false);
        Dispose();
    }

    public override async Task MainLoop(CancellationToken token)
    {
        try
        {
            await InitializeHardware(Hub.Config.Trade, token).ConfigureAwait(false);

            Log("Identificando los datos del entrenador de la consola host.");
            var sav = await IdentifyTrainer(token).ConfigureAwait(false);
            RecentTrainerCache.SetRecentTrainer(sav);

            await RestartGameIfCantLeaveUnionRoom(token).ConfigureAwait(false);
            await InitializeSessionOffsets(token).ConfigureAwait(false);
            OnConnectionSuccess();
            Log($"Iniciando el bucle principal {nameof(PokeTradeBotBS)}.");
            await InnerLoop(sav, token).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            OnConnectionError(e);
            throw;
        }

        Log($"Finalizando el bucle {nameof(PokeTradeBotBS)}.");

        await HardStop().ConfigureAwait(false);
    }

    public override async Task RebootAndStop(CancellationToken t)
    {
        Hub.Queues.Info.CleanStuckTrades();
        await Task.Delay(2_000, t).ConfigureAwait(false);
        await ReOpenGame(Hub.Config, t).ConfigureAwait(false);
        await HardStop().ConfigureAwait(false);
        await Task.Delay(2_000, t).ConfigureAwait(false);
        if (!t.IsCancellationRequested)
        {
            Log("Reiniciando el bucle principal.");

            await MainLoop(t).ConfigureAwait(false);
        }
    }

    protected virtual async Task<(PB8 toSend, PokeTradeResult check)> GetEntityToSend(SAV8BS sav, PokeTradeDetail<PB8> poke, PB8 offered, PB8 toSend, PartnerDataHolder partnerID, SpecialTradeType? stt, CancellationToken token)
    {
        if (token.IsCancellationRequested)
            return (toSend, PokeTradeResult.RoutineCancel);

        return poke.Type switch
        {
            PokeTradeType.Random => await HandleRandomLedy(sav, poke, offered, toSend, partnerID, token).ConfigureAwait(false),
            PokeTradeType.FixOT => await HandleFixOT(sav, poke, offered, partnerID, token).ConfigureAwait(false),
            PokeTradeType.Clone => await HandleClone(sav, poke, offered, token).ConfigureAwait(false),
            PokeTradeType.Seed when stt is not SpecialTradeType.WonderCard => await HandleClone(sav, poke, offered, token).ConfigureAwait(false),
            PokeTradeType.Seed when stt is SpecialTradeType.WonderCard => await JustInject(sav, offered, token).ConfigureAwait(false),
            _ => (toSend, PokeTradeResult.Success),
        };
    }

    private async Task<(PB8 toSend, PokeTradeResult check)> JustInject(SAV8BS sav, PB8 offered, CancellationToken token)
    {
        await Click(A, 0_800, token).ConfigureAwait(false);
        await SetBoxPokemonAbsolute(BoxStartOffset, offered, token, sav).ConfigureAwait(false);

        for (int i = 0; i < 5; i++)
            await Click(A, 0_500, token).ConfigureAwait(false);

        return (offered, PokeTradeResult.Success);
    }

    protected virtual (PokeTradeDetail<PB8>? detail, uint priority) GetTradeData(PokeRoutineType type)
    {
        string botName = Connection.Name;

        // First check the specific type's queue
        if (Hub.Queues.TryDequeue(type, out var detail, out var priority, botName))
        {
            return (detail, priority);
        }

        // If we're doing FlexTrade, also check the Batch queue
        if (type == PokeRoutineType.FlexTrade)
        {
            if (Hub.Queues.TryDequeue(PokeRoutineType.Batch, out detail, out priority, botName))
            {
                return (detail, priority);
            }
        }

        if (Hub.Queues.TryDequeueLedy(out detail))
        {
            return (detail, PokeTradePriorities.TierFree);
        }
        return (null, PokeTradePriorities.TierFree);
    }

    protected virtual async Task<bool> IsUserBeingShifty(PokeTradeDetail<PB8> detail, CancellationToken token)
    {
        await Task.CompletedTask.ConfigureAwait(false);
        return false;
    }

    private static ulong GetFakeNID(string trainerName, uint trainerID)
    {
        var nameHash = trainerName.GetHashCode();
        return ((ulong)trainerID << 32) | (uint)nameHash;
    }

    private async Task<PB8> ApplyAutoOT(PB8 toSend, SAV8BS sav, string tradePartner, uint trainerTID7, uint trainerSID7, CancellationToken token)
    {
        if (token.IsCancellationRequested) return toSend;

        // Special handling for Pokémon GO
        if (toSend.Version == GameVersion.GO)
        {
            var goClone = toSend.Clone();
            goClone.OriginalTrainerName = tradePartner;

            // Actualizar el OT trash para que coincida con el nuevo nombre de OT
            ClearOTTrash(goClone, tradePartner);

            if (!toSend.ChecksumValid)
                goClone.RefreshChecksum();

            Log("Se aplicó solo el nombre de OT al Pokémon proveniente de GO.");
            await SetBoxPokemonAbsolute(BoxStartOffset, goClone, token, sav).ConfigureAwait(false);
            return goClone;
        }

        if (toSend is IHomeTrack pk && pk.HasTracker)
        {
            Log("Rastreador HOME detectado. No se puede aplicar Auto OT.");
            return toSend;
        }

        // Current handler cannot be past gen OT
        if (toSend.Generation != toSend.Format)
        {
            Log("No se pueden aplicar los detalles del entrenador: el dueño actual no puede ser de diferente generación OT.");
            return toSend;
        }

        // Check if the Pokémon is from a Mystery Gift
        bool isMysteryGift = toSend.FatefulEncounter;

        // Check if Mystery Gift has legitimate preset OT/TID/SID (not configured defaults or ALM's defaults)
        // Use the actual configured values from LegalitySettings, not hardcoded defaults
        var legalitySettings = Hub.Config.Legality;
        bool hasConfiguredDefaults = toSend.OriginalTrainerName.Equals(legalitySettings.GenerateOT, StringComparison.OrdinalIgnoreCase) &&
                                     toSend.TID16 == legalitySettings.GenerateTID16 &&
                                     toSend.SID16 == legalitySettings.GenerateSID16;

        // ALM's NET10 defaults can be identified by the OT name alone
        bool hasALMDefaults = toSend.OriginalTrainerName.Equals("ALM", StringComparison.OrdinalIgnoreCase);

        bool hasDefaultTrainerInfo = hasConfiguredDefaults || hasALMDefaults;

        if (isMysteryGift && !hasDefaultTrainerInfo)
        {
            Log("Se detectó un Regalo Misterioso con OT/TID/SID preestablecidos. Omitiendo AutoOT por completo.");
            return toSend;
        }

        var cln = toSend.Clone();

        if (isMysteryGift)
        {
            Log("Se detectó un Regalo Misterioso. Solo se aplicará la información de OT, preservando el idioma.");
            // Solo establecer la información relacionada al OT para Regalos Misteriosos sin OT/TID/SID preestablecidos
            cln.TrainerTID7 = trainerTID7;
            cln.TrainerSID7 = trainerSID7;
            cln.OriginalTrainerName = tradePartner;
        }
        else
        {
            // Apply all trade partner details for non-Mystery Gift Pokémon
            cln.TrainerTID7 = trainerTID7;
            cln.TrainerSID7 = trainerSID7;
            cln.OriginalTrainerName = tradePartner;
            // Any additional properties that would normally be set for BDSP
        }

        ClearOTTrash(cln, tradePartner);

        if (!toSend.IsNicknamed)
            cln.ClearNickname();

        if (toSend.IsShiny)
            cln.PID = (uint)((cln.TID16 ^ cln.SID16 ^ (cln.PID & 0xFFFF) ^ toSend.ShinyXor) << 16) | (cln.PID & 0xFFFF);

        if (!toSend.ChecksumValid)
            cln.RefreshChecksum();

        var tradeBS = new LegalityAnalysis(cln);
        if (tradeBS.Valid)
        {
            Log("El Pokémon es válido con la información del compañero de intercambio aplicada. Intercambiando detalles.");
            await SetBoxPokemonAbsolute(BoxStartOffset, cln, token, sav).ConfigureAwait(false);
            return cln;
        }
        else
        {
            // Partner info made the Pokemon illegal. Restore the original in the box
            // so the box always matches the value returned to the confirm path.
            Log("El Pokémon no es válido después de usar la información del compañero de intercambio.");
            await SetBoxPokemonAbsolute(BoxStartOffset, toSend, token, sav).ConfigureAwait(false);
            return toSend;
        }
    }

    private static void ClearOTTrash(PB8 pokemon, string trainerName)
    {
        Span<byte> trash = pokemon.OriginalTrainerTrash;
        trash.Clear();
        int maxLength = trash.Length / 2;
        int actualLength = Math.Min(trainerName.Length, maxLength);
        for (int i = 0; i < actualLength; i++)
        {
            char value = trainerName[i];
            trash[i * 2] = (byte)value;
            trash[(i * 2) + 1] = (byte)(value >> 8);
        }
        if (actualLength < maxLength)
        {
            trash[actualLength * 2] = 0x00;
            trash[(actualLength * 2) + 1] = 0x00;
        }
    }

    private async Task<PokeTradeResult> ConfirmAndStartTrading(PokeTradeDetail<PB8> detail, CancellationToken token)
    {
        if (token.IsCancellationRequested) return PokeTradeResult.RoutineCancel;

        // We'll keep watching B1S1 for a change to indicate a trade started -> should try quitting at that point.
        var oldEC = await SwitchConnection.ReadBytesAbsoluteAsync(BoxStartOffset, 8, token).ConfigureAwait(false);

        await Click(A, 3_000, token).ConfigureAwait(false);
        for (int i = 0; i < Hub.Config.Trade.TradeConfiguration.MaxTradeConfirmTime; i++)
        {
            if (token.IsCancellationRequested) return PokeTradeResult.RoutineCancel;

            if (await IsUserBeingShifty(detail, token).ConfigureAwait(false))
                return PokeTradeResult.SuspiciousActivity;

            // We're no longer talking, so they probably quit on us.
            if (!await IsUnionWork(UnionTalkingOffset, token).ConfigureAwait(false))
                return PokeTradeResult.TrainerTooSlow;
            await Click(A, 1_000, token).ConfigureAwait(false);

            // EC is detectable at the start of the animation.
            var newEC = await SwitchConnection.ReadBytesAbsoluteAsync(BoxStartOffset, 8, token).ConfigureAwait(false);
            if (!newEC.SequenceEqual(oldEC))
            {
                // Check if partner offered a Pokemon that will evolve
                if (Hub.Config.Trade.TradeConfiguration.DisallowTradeEvolve)
                {
                    var offered = await ReadPokemon(LinkTradePokemonOffset, BoxFormatSlotSize, token).ConfigureAwait(false);
                    if (offered != null && TradeEvolutions.WillTradeEvolve(offered.Species, offered.Form, offered.HeldItem, detail.TradeData.Species))
                    {
                        Log("Intercambio cancelado porque el entrenador ofreció un Pokémon que evolucionaría al intercambiarlo.");
                        detail.SendNotification(this, "Intercambio cancelado. No puedes intercambiar un Pokémon que vaya a evolucionar. Para evitar esto, dale a tu Pokémon una Piedra Eterna o intercambia un Pokémon diferente.");
                        return PokeTradeResult.TradeEvolveNotAllowed;
                    }
                }

                await Task.Delay(25_000, token).ConfigureAwait(false);
                return PokeTradeResult.Success;
            }
        }

        // If we don't detect a B1S1 change, the trade didn't go through in that time.
        return PokeTradeResult.TrainerTooSlow;
    }

    private async Task DoNothing(CancellationToken token)
    {
        int waitCounter = 0;
        while (!token.IsCancellationRequested && Config.NextRoutineType == PokeRoutineType.Idle)
        {
            if (waitCounter == 0)
                Log("No hay tarea asignada. Esperando una nueva asignación de tarea.");
            waitCounter++;
            if (waitCounter % 10 == 0 && Hub.Config.AntiIdle)
                await Click(B, 1_000, token).ConfigureAwait(false);
            else
                await Task.Delay(1_000, token).ConfigureAwait(false);
        }
    }

    private async Task DoTrades(SAV8BS sav, CancellationToken token)
    {
        var type = Config.CurrentRoutineType;
        int waitCounter = 0;
        while (!token.IsCancellationRequested && Config.NextRoutineType == type)
        {
            var (detail, priority) = GetTradeData(type);
            if (detail is null)
            {
                await WaitForQueueStep(waitCounter++, token).ConfigureAwait(false);
                continue;
            }
            waitCounter = 0;

            if (detail.Type != PokeTradeType.Random || !Hub.Config.Distribution.RemainInUnionRoomBDSP)
                await RestartGameIfCantLeaveUnionRoom(token).ConfigureAwait(false);

            string tradetype = $" ({detail.Type})";
            Log($"Iniciando el siguiente {type}{tradetype} intercambio del Bot. Obteniendo datos...");
            TradeProgressChanged?.Invoke(7);

            await Task.Delay(500, token).ConfigureAwait(false);
            Hub.Config.Stream.StartTrade(this, detail, Hub);
            Hub.Queues.StartTrade(this, detail);

            await PerformTrade(sav, detail, type, priority, token).ConfigureAwait(false);
        }
    }

    private async Task<bool> EnsureOutsideOfUnionRoom(CancellationToken token)
    {
        if (token.IsCancellationRequested) return false;

        if (!await IsUnionWork(UnionGamingOffset, token).ConfigureAwait(false))
            return true;

        if (!await ExitBoxToUnionRoom(token).ConfigureAwait(false))
            return false;
        if (!await ExitUnionRoomToOverworld(token).ConfigureAwait(false))
            return false;
        return true;
    }

    private async Task<bool> EnterUnionRoomWithCode(PokeTradeType tradeType, int tradeCode, CancellationToken token)
    {
        if (token.IsCancellationRequested) return false;

        // Already in Union Room.
        if (await IsUnionWork(UnionGamingOffset, token).ConfigureAwait(false))
            return true;

        // Open y-comm and select global room
        await Click(Y, 1_000 + Hub.Config.Timings.ExtraTimeOpenYMenu, token).ConfigureAwait(false);
        await Click(DRIGHT, 0_400, token).ConfigureAwait(false);

        // French has one less menu
        if (GameLang is not LanguageID.French)
        {
            await Click(A, 0_050, token).ConfigureAwait(false);
            await PressAndHold(A, 1_000, 0, token).ConfigureAwait(false);
        }

        await Click(A, 0_050, token).ConfigureAwait(false);
        await PressAndHold(A, 1_500, 0, token).ConfigureAwait(false);

        // Japanese has one extra menu
        if (GameLang is LanguageID.Japanese)
        {
            await Click(A, 0_050, token).ConfigureAwait(false);
            await PressAndHold(A, 1_000, 0, token).ConfigureAwait(false);
        }

        await Click(A, 1_000, token).ConfigureAwait(false); // Would you like to enter? Screen

        Log("Seleccionando la sala de Código de Enlace.");
        TradeProgressChanged?.Invoke(14);


        // Índice de selección del código de enlace
        await Click(DDOWN, 0_200, token).ConfigureAwait(false);
        await Click(DDOWN, 0_200, token).ConfigureAwait(false);

        Log("Conectando a internet.");
        TradeProgressChanged?.Invoke(21);

        await Click(A, 0_050, token).ConfigureAwait(false);
        await PressAndHold(A, 2_000, 0, token).ConfigureAwait(false);

        // Extra menus.
        if (GameLang is LanguageID.German or LanguageID.Italian or LanguageID.Korean)
        {
            await Click(A, 0_050, token).ConfigureAwait(false);
            await PressAndHold(A, 0_750, 0, token).ConfigureAwait(false);
        }

        await Click(A, 0_050, token).ConfigureAwait(false);
        await PressAndHold(A, 1_000, 0, token).ConfigureAwait(false);
        await Click(A, 0_050, token).ConfigureAwait(false);
        await PressAndHold(A, 1_500, 0, token).ConfigureAwait(false);
        await Click(A, 0_050, token).ConfigureAwait(false);
        await PressAndHold(A, 1_500, 0, token).ConfigureAwait(false);

        // Would you like to save your adventure so far?
        await Click(A, 0_500, token).ConfigureAwait(false);
        await Click(A, 0_500, token).ConfigureAwait(false);

        Log("Guardando la partida.");
        TradeProgressChanged?.Invoke(28);


        // Aceptar y guardar la partida.
        await Click(A, 0_050, token).ConfigureAwait(false);
        await PressAndHold(A, 6_500, 0, token).ConfigureAwait(false);

        if (tradeType != PokeTradeType.Random)
            Hub.Config.Stream.StartEnterCode(this);
        Log($"Ingresando el código de Intercambio por Enlace: {tradeCode:0000 0000}...");
        TradeProgressChanged?.Invoke(35);

        await EnterLinkCode(tradeCode, Hub.Config, token).ConfigureAwait(false);

        // Wait for Barrier to trigger all bots simultaneously.
        WaitAtBarrierIfApplicable(token);
        if (token.IsCancellationRequested) return false;

        await Click(PLUS, 0_600, token).ConfigureAwait(false);
        Hub.Config.Stream.EndEnterCode(this);
        Log("Entrando a la Sala Unión.");
        TradeProgressChanged?.Invoke(42);


        // Wait until we're past the communication message.
        int tries = 100;
        while (!await IsUnionWork(UnionGamingOffset, token).ConfigureAwait(false))
        {
            if (token.IsCancellationRequested) return false;

            await Click(A, 0_300, token).ConfigureAwait(false);

            if (--tries < 1)
                return false;
        }

        await Task.Delay(1_300 + Hub.Config.Timings.ExtraTimeJoinUnionRoom, token).ConfigureAwait(false);

        return true; // We've made it into the room and are ready to request.
    }

    private async Task<bool> ExitBoxToUnionRoom(CancellationToken token)
    {
        if (token.IsCancellationRequested) return false;

        if (await IsUnionWork(UnionTalkingOffset, token).ConfigureAwait(false))
        {
            Log("Saliendo de las cajas...");

            int tries = 30;
            while (await IsUnionWork(UnionTalkingOffset, token).ConfigureAwait(false))
            {
                if (token.IsCancellationRequested) return false;

                await Click(B, 0_500, token).ConfigureAwait(false);
                if (!await IsUnionWork(UnionTalkingOffset, token).ConfigureAwait(false))
                    break;
                await Click(DUP, 0_200, token).ConfigureAwait(false);
                await Click(A, 0_500, token).ConfigureAwait(false);

                // Keeps regular quitting a little faster, only need this for trade evolutions + moves.
                if (tries < 10)
                    await Click(B, 0_500, token).ConfigureAwait(false);
                await Click(B, 0_500, token).ConfigureAwait(false);
                tries--;
                if (tries < 0)
                    return false;
            }
        }
        await Task.Delay(2_000, token).ConfigureAwait(false);
        return true;
    }

    private async Task<bool> ExitUnionRoomToOverworld(CancellationToken token)
    {
        if (token.IsCancellationRequested) return false;

        if (await IsUnionWork(UnionGamingOffset, token).ConfigureAwait(false))
        {
            Log("Saliendo de la Sala Unión...");

            for (int i = 0; i < 3; ++i)
                await Click(B, 0_200, token).ConfigureAwait(false);

            await Click(Y, 1_000, token).ConfigureAwait(false);
            await Click(DDOWN, 0_200, token).ConfigureAwait(false);
            for (int i = 0; i < 3; ++i)
                await Click(A, 0_400, token).ConfigureAwait(false);

            int tries = 10;
            while (await IsUnionWork(UnionGamingOffset, token).ConfigureAwait(false))
            {
                if (token.IsCancellationRequested) return false;

                await Task.Delay(0_400, token).ConfigureAwait(false);
                tries--;
                if (tries < 0)
                    return false;
            }
            await Task.Delay(3_000 + Hub.Config.Timings.ExtraTimeLeaveUnionRoom, token).ConfigureAwait(false);
        }
        return true;
    }

    private async Task<TradePartnerBS?> GetTradePartnerInfo(CancellationToken token)
    {
        if (token.IsCancellationRequested) return null;

        var id = await SwitchConnection.PointerPeek(4, Offsets.LinkTradePartnerIDPointer, token).ConfigureAwait(false);
        var name = await SwitchConnection.PointerPeek(TradePartnerBS.MaxByteLengthStringObject, Offsets.LinkTradePartnerNamePointer, token).ConfigureAwait(false);
        // Read gender from first byte of param data
        var genderByte = await SwitchConnection.PointerPeek(1, Offsets.LinkTradePartnerParamPointer, token).ConfigureAwait(false);

        // Extract gender from bit 6 (0x40) - still needs testing, some tests i've done are accurate, some are not
        // Bit 6 set = female, Bit 6 clear = male
        bool isFemale = (genderByte[0] & 0x40) != 0;
        byte gender = isFemale ? (byte)1 : (byte)0;

        return new TradePartnerBS(id, name, gender);
    }

    private void HandleAbortedTrade(PokeTradeDetail<PB8> detail, PokeRoutineType type, uint priority, PokeTradeResult result)
    {
        detail.IsProcessing = false;
        if (result.ShouldAttemptRetry() && detail.Type != PokeTradeType.Random && !detail.IsRetry)
        {
            detail.IsRetry = true;
            Hub.Queues.Enqueue(type, detail, Math.Min(priority, PokeTradePriorities.Tier2));
            detail.SendNotification(this, "¡Ups! Algo ocurrió. Te volveré a poner en la cola para otro intento.");

        }
        else
        {
            detail.SendNotification(this, $"¡Ups! Algo ocurrió. Cancelando el intercambio: {result.GetDescription()}.");
            TradeProgressChanged?.Invoke(0);

            detail.TradeCanceled(this, result);
        }
    }

    private async Task<(PB8 toSend, PokeTradeResult check)> HandleClone(SAV8BS sav, PokeTradeDetail<PB8> poke, PB8 offered, CancellationToken token)
    {
        if (Hub.Config.Discord.ReturnPKMs)
            poke.SendNotification(this, offered, $"Aquí tienes lo que me mostraste - {GameInfo.GetStrings("en").Species[offered.Species]}");

        var la = new LegalityAnalysis(offered);
        if (!la.Valid)
        {
            Log($"La solicitud de clonación (de {poke.Trainer.TrainerName}) ha detectado un Pokémon inválido: {GameInfo.GetStrings("en").Species[offered.Species]}.");

            if (DumpSetting.Dump)
                DumpPokemon(DumpSetting.DumpFolder, "hacked", offered);

            var report = la.Report();
            Log(report);
            poke.SendNotification(this, "⚠️ Este Pokémon no es legal según las verificaciones de legalidad de PKHeX. Tengo prohibido clonarlo. Saliendo del intercambio.");
            TradeProgressChanged?.Invoke(0);

            poke.SendNotification(this, report);

            return (offered, PokeTradeResult.IllegalTrade);
        }

        var clone = offered.Clone();
        if (Hub.Config.Legality.ResetHOMETracker)
            clone.Tracker = 0;

        poke.SendNotification(this, $"**✅ ¡Cloné tu {GameInfo.GetStrings("en").Species[clone.Species]}!**\nAhora presiona B para cancelar tu oferta y dame un Pokémon que no quieras.");
        Log($"Se clonó un {GameInfo.GetStrings("en").Species[clone.Species]}. Esperando a que el usuario cambie su Pokémon...");
        TradeProgressChanged?.Invoke(84);


        // For BDSP, we need to read from LinkTradePokemonOffset instead of TradePartnerOfferedOffset
        var partnerFound = await ReadUntilChanged(LinkTradePokemonOffset, await SwitchConnection.ReadBytesAbsoluteAsync(LinkTradePokemonOffset, 8, token).ConfigureAwait(false), 15_000, 0_200, false, true, token).ConfigureAwait(false);
        if (!partnerFound)
        {
            poke.SendNotification(this, "**¡HEY, CÁMBIALO AHORA O ME VOY!**");

            // They get one more chance
            partnerFound = await ReadUntilChanged(LinkTradePokemonOffset, await SwitchConnection.ReadBytesAbsoluteAsync(LinkTradePokemonOffset, 8, token).ConfigureAwait(false), 15_000, 0_200, false, true, token).ConfigureAwait(false);
        }

        // In BDSP we check if we're still in the Union Room
        if (!await IsUnionWork(UnionGamingOffset, token).ConfigureAwait(false))
        {
            Log("El usuario canceló el intercambio. Saliendo...");
            TradeProgressChanged?.Invoke(0);

            await EnsureOutsideOfUnionRoom(token).ConfigureAwait(false);
            return (offered, PokeTradeResult.TrainerTooSlow);
        }

        var pk2 = await ReadPokemon(LinkTradePokemonOffset, BoxFormatSlotSize, token).ConfigureAwait(false);
        if (!partnerFound || pk2 is null || SearchUtil.HashByDetails(pk2) == SearchUtil.HashByDetails(offered))
        {
            Log("El compañero de intercambio no cambió su Pokémon.");
            TradeProgressChanged?.Invoke(0);

            await EnsureOutsideOfUnionRoom(token).ConfigureAwait(false);
            return (offered, PokeTradeResult.TrainerTooSlow);
        }

        await Click(A, 0_800, token).ConfigureAwait(false);
        await SetBoxPokemonAbsolute(BoxStartOffset, clone, token, sav).ConfigureAwait(false);

        return (clone, PokeTradeResult.Success);
    }

    private async Task<(PB8 toSend, PokeTradeResult check)> HandleFixOT(SAV8BS sav, PokeTradeDetail<PB8> poke, PB8 offered, PartnerDataHolder partner, CancellationToken token)
    {
        if (token.IsCancellationRequested) return (offered, PokeTradeResult.RoutineCancel);

        if (Hub.Config.Discord.ReturnPKMs)
            poke.SendNotification(this, offered, $"Aquí tienes lo que me mostraste - {GameInfo.GetStrings("en").Species[offered.Species]}");

        var adOT = TradeExtensions<PB8>.HasAdName(offered, out _);
        var laInit = new LegalityAnalysis(offered);
        if (!adOT && laInit.Valid)
        {
            poke.SendNotification(this, "✅ No se detectó anuncio en el Apodo o en el OT, y el Pokémon es legal. Saliendo del intercambio.");
            TradeProgressChanged?.Invoke(0);

            return (offered, PokeTradeResult.TrainerRequestBad);
        }


        var clone = (PB8)offered.Clone();
        if (Hub.Config.Legality.ResetHOMETracker)
            clone.Tracker = 0;

        string shiny = string.Empty;
        if (!TradeExtensions<PB8>.ShinyLockCheck(offered.Species, TradeExtensions<PB8>.FormOutput(offered.Species, offered.Form, out _), $"{(Ball)offered.Ball}"))
            shiny = $"\nShiny: {(offered.ShinyXor == 0 ? "Square" : offered.IsShiny ? "Star" : "No")}";
        else shiny = "\nShiny: No";

        var name = partner.TrainerName;
        var ball = $"\n{(Ball)offered.Ball}";
        var extraInfo = $"OT: {name}{ball}{shiny}";
        var set = ShowdownParsing.GetShowdownText(offered).Split('\n').ToList();
        var shinyRes = set.Find(x => x.Contains("Shiny"));
        if (shinyRes != null)
            set.Remove(shinyRes);
        set.InsertRange(1, extraInfo.Split('\n'));

        if (!laInit.Valid)
        {
            Log($"La solicitud de FixOT ha detectado un Pokémon ilegal de {name}: {GameInfo.GetStrings("en").Species[offered.Species]}");
            var report = laInit.Report();
            Log(laInit.Report());
            poke.SendNotification(this, $"⚠️ **Los Pokémon mostrados no son legales. Intentando regenerar...**\n\n```{report}```");
            if (DumpSetting.Dump)
                DumpPokemon(DumpSetting.DumpFolder, "hacked", offered);
        }

        if (clone.FatefulEncounter)
        {
            clone.SetDefaultNickname(laInit);
            var info = new SimpleTrainerInfo { Gender = clone.OriginalTrainerGender, Language = clone.Language, OT = name, TID16 = clone.TID16, SID16 = clone.SID16, Generation = 8 };
            var mg = EncounterEvent.GetAllEvents().Where(x => x.Species == clone.Species && x.Form == clone.Form && x.IsShiny == clone.IsShiny && x.OriginalTrainerName == clone.OriginalTrainerName).ToList();
            if (mg.Count > 0)
                clone = TradeExtensions<PB8>.CherishHandler(mg.First(), info);
            else clone = (PB8)sav.GetLegal(AutoLegalityWrapper.GetTemplate(new ShowdownSet(string.Join("\n", set))), out _);
        }
        else
        {
            clone = (PB8)sav.GetLegal(AutoLegalityWrapper.GetTemplate(new ShowdownSet(string.Join("\n", set))), out _);
        }

        var la = new LegalityAnalysis(clone);
        clone = (PB8)TradeExtensions<PB8>.TrashBytes(clone, la);
        clone.ResetPartyStats();
        la = new LegalityAnalysis(clone);
        if (!la.Valid)
        {
            poke.SendNotification(this, $"⚠️ Este Pokémon no es __**legal**__ según los controles de legalidad de __PKHeX__. No pude arreglar esto. Cancelando trade...");
            TradeProgressChanged?.Invoke(0);

            return (clone, PokeTradeResult.IllegalTrade);
        }

        TradeExtensions<PB8>.HasAdName(offered, out string detectedAd);
        poke.SendNotification(this, $"{(!laInit.Valid ? "**Legalizado" : "**Apodo/OT corregido para")} {(Species)clone.Species}** (anuncio encontrado: {detectedAd})! ¡Ahora confirma el intercambio!");
        Log($"{(!laInit.Valid ? "Legalizado" : "Apodo/OT corregido para")} {GameInfo.GetStrings("en").Species[clone.Species]}!");
        TradeProgressChanged?.Invoke(84);

        await SetBoxPokemonAbsolute(BoxStartOffset, clone, token, sav).ConfigureAwait(false);
        poke.SendNotification(this, "¡Ahora confirma el intercambio!");
        await Click(A, 0_800, token).ConfigureAwait(false);
        await Click(A, 6_000, token).ConfigureAwait(false);

        var pk2 = await ReadPokemon(LinkTradePokemonOffset, token).ConfigureAwait(false);
        var comp = await SwitchConnection.ReadBytesAbsoluteAsync(LinkTradePokemonOffset, 8, token).ConfigureAwait(false);
        bool changed = pk2 == null || !comp.SequenceEqual(lastOffered) || clone.Species != pk2.Species || offered.OriginalTrainerName != pk2.OriginalTrainerName;
        if (changed)
        {
            Log($"{name} cambió el Pokémon mostrado ({GameInfo.GetStrings("en").Species[clone.Species]}){(pk2 != null ? $" a {GameInfo.GetStrings("en").Species[pk2.Species]}" : "")}");
            poke.SendNotification(this, "**¡Por favor envía el Pokémon que mostraste originalmente!**");

            bool verify = await ReadUntilChanged(LinkTradePokemonOffset, comp, 10_000, 0_200, false, true, token).ConfigureAwait(false);
            if (verify)
                verify = await ReadUntilChanged(LinkTradePokemonOffset, lastOffered, 5_000, 0_200, true, true, token).ConfigureAwait(false);
            changed = !verify && (pk2 == null || clone.Species != pk2.Species || offered.OriginalTrainerName != pk2.OriginalTrainerName);
        }

        // Update the last Pokémon they showed us.
        lastOffered = await SwitchConnection.ReadBytesAbsoluteAsync(LinkTradePokemonOffset, 8, token).ConfigureAwait(false);

        if (changed)
        {
            poke.SendNotification(this, "El Pokémon fue cambiado y no se revirtió. Saliendo del intercambio.");
            Log("El compañero de intercambio no quiso enviar su Pokémon con anuncio.");
            TradeProgressChanged?.Invoke(0);

            return (offered, PokeTradeResult.TrainerTooSlow);
        }

        await Click(A, 0_500, token).ConfigureAwait(false);
        for (int i = 0; i < 5; i++)
            await Click(A, 0_500, token).ConfigureAwait(false);

        return (clone, PokeTradeResult.Success);
    }

    private async Task<(PB8 toSend, PokeTradeResult check)> HandleRandomLedy(SAV8BS sav, PokeTradeDetail<PB8> poke, PB8 offered, PB8 toSend, PartnerDataHolder partner, CancellationToken token)
    {
        if (token.IsCancellationRequested) return (toSend, PokeTradeResult.RoutineCancel);

        // Allow the trade partner to do a Ledy swap.
        var config = Hub.Config.Distribution;
        var trade = Hub.Ledy.GetLedyTrade(offered, partner.TrainerOnlineID, config.LedySpecies);
        if (trade != null)
        {
            if (trade.Type == LedyResponseType.AbuseDetected)
            {
                var msg = $"⚠️ Se ha detectado que {partner.TrainerName} está abusando de los intercambios de Ledy.";
                EchoUtil.Echo(msg);

                return (toSend, PokeTradeResult.SuspiciousActivity);
            }

            toSend = trade.Receive;
            poke.TradeData = toSend;

            poke.SendNotification(this, "Injecting the requested Pokémon.");
            await Click(A, 0_800, token).ConfigureAwait(false);
            await SetBoxPokemonAbsolute(BoxStartOffset, toSend, token, sav).ConfigureAwait(false);
            await Task.Delay(2_500, token).ConfigureAwait(false);
        }
        else if (config.LedyQuitIfNoMatch)
        {
            var nickname = offered.IsNicknamed ? $" (Apodo: \"{offered.Nickname}\")" : string.Empty;
            poke.SendNotification(this, $"⚠️ No se encontró coincidencia para el {GameInfo.GetStrings("en").Species[offered.Species]}{nickname} ofrecido.");
            return (toSend, PokeTradeResult.TrainerRequestBad);
        }

        for (int i = 0; i < 5; i++)
        {
            await Click(A, 0_500, token).ConfigureAwait(false);
        }

        return (toSend, PokeTradeResult.Success);
    }

    // These don't change per session, and we access them frequently, so set these each time we start.
    private async Task InitializeSessionOffsets(CancellationToken token)
    {
        if (token.IsCancellationRequested) return;

        Log("Caching session offsets...");
        BoxStartOffset = await SwitchConnection.PointerAll(Offsets.BoxStartPokemonPointer, token).ConfigureAwait(false);
        UnionGamingOffset = await SwitchConnection.PointerAll(Offsets.UnionWorkIsGamingPointer, token).ConfigureAwait(false);
        UnionTalkingOffset = await SwitchConnection.PointerAll(Offsets.UnionWorkIsTalkingPointer, token).ConfigureAwait(false);
        SoftBanOffset = await SwitchConnection.PointerAll(Offsets.UnionWorkPenaltyPointer, token).ConfigureAwait(false);
    }

    private async Task InnerLoop(SAV8BS sav, CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            Config.IterateNextRoutine();
            var task = Config.CurrentRoutineType switch
            {
                PokeRoutineType.Idle => DoNothing(token),
                _ => DoTrades(sav, token),
            };
            try
            {
                await task.ConfigureAwait(false);
            }
            catch (SocketException e)
            {
                if (e.StackTrace != null)
                    Connection.LogError(e.StackTrace);
                var attempts = Hub.Config.Timings.ReconnectAttempts;
                var delay = Hub.Config.Timings.ExtraReconnectDelay;
                var protocol = Config.Connection.Protocol;
                if (!await TryReconnect(attempts, delay, protocol, token).ConfigureAwait(false))
                    return;
            }
        }
    }

    private void OnConnectionError(Exception ex)
    {
        ConnectionError?.Invoke(this, ex);
    }

    private void OnConnectionSuccess()
    {
        ConnectionSuccess?.Invoke(this, EventArgs.Empty);
    }

    private async Task<PokeTradeResult> PerformLinkCodeTrade(SAV8BS sav, PokeTradeDetail<PB8> poke, CancellationToken token)
    {
        if (token.IsCancellationRequested) return PokeTradeResult.RoutineCancel;

        // Update Barrier Settings
        UpdateBarrier(poke.IsSynchronized);
        poke.TradeInitialize(this);
        Hub.Config.Stream.EndEnterCode(this);

        var distroRemainInRoom = poke.Type == PokeTradeType.Random && Hub.Config.Distribution.RemainInUnionRoomBDSP;

        // If we weren't supposed to remain and started out in the Union Room, ensure we're out of the box.
        if (!distroRemainInRoom && await IsUnionWork(UnionGamingOffset, token).ConfigureAwait(false))
        {
            if (!await ExitBoxToUnionRoom(token).ConfigureAwait(false))
                return PokeTradeResult.RecoverReturnOverworld;
        }

        if (await CheckIfSoftBanned(SoftBanOffset, token).ConfigureAwait(false))
            await UnSoftBan(token).ConfigureAwait(false);

        var toSend = poke.TradeData;
        if (toSend.Species != 0)
        {
            await SetBoxPokemonAbsolute(BoxStartOffset, toSend, token, sav).ConfigureAwait(false);
        }

        // Enter Union Room. Shouldn't do anything if we're already there.
        if (!await EnterUnionRoomWithCode(poke.Type, poke.Code, token).ConfigureAwait(false))
        {
            // We don't know how far we made it in, so restart the game to be safe.
            await RestartGameBDSP(token).ConfigureAwait(false);
            return PokeTradeResult.RecoverEnterUnionRoom;
        }
        await RequestUnionRoomTrade(token).ConfigureAwait(false);
        poke.TradeSearching(this);
        var waitPartner = Hub.Config.Trade.TradeConfiguration.TradeWaitTime;

        // Keep pressing A until we detect someone talking to us.
        while (!await IsUnionWork(UnionTalkingOffset, token).ConfigureAwait(false) && waitPartner > 0)
        {
            if (token.IsCancellationRequested) return PokeTradeResult.RoutineCancel;

            for (int i = 0; i < 2; ++i)
                await Click(A, 0_450, token).ConfigureAwait(false);

            if (--waitPartner <= 0)
            {
                // Ensure we exit the union room when no trainer is found.
                await EnsureOutsideOfUnionRoom(token).ConfigureAwait(false);
                return PokeTradeResult.NoTrainerFound;
            }
        }
        Log("Found a user talking to us!");
        TradeProgressChanged?.Invoke(56);

        // Keep pressing A until TargetTranerParam (sic) is loaded (when we hit the box).
        while (!await IsPartnerParamLoaded(token).ConfigureAwait(false) && waitPartner > 0)
        {
            if (token.IsCancellationRequested) return PokeTradeResult.RoutineCancel;

            for (int i = 0; i < 2; ++i)
                await Click(A, 0_450, token).ConfigureAwait(false);

            // Can be false if they talked and quit.
            if (!await IsUnionWork(UnionTalkingOffset, token).ConfigureAwait(false))
                break;
            if (--waitPartner <= 0)
            {
                // Ensure we exit the union room if the partner is too slow.
                await EnsureOutsideOfUnionRoom(token).ConfigureAwait(false);
                return PokeTradeResult.TrainerTooSlow;
            }
        }
        Log("Entering the box...");
        TradeProgressChanged?.Invoke(76);


        // Still going through dialog and box opening.
        await Task.Delay(3_000, token).ConfigureAwait(false);

        // Can happen if they quit out of talking to us.
        if (!await IsPartnerParamLoaded(token).ConfigureAwait(false))
        {
            // Ensure we exit the union room if the partner is too slow.
            await EnsureOutsideOfUnionRoom(token).ConfigureAwait(false);
            return PokeTradeResult.TrainerTooSlow;
        }

        var tradePartner = await GetTradePartnerInfo(token).ConfigureAwait(false);
        var trainerNID = GetFakeNID(tradePartner?.TrainerName ?? string.Empty, tradePartner?.TrainerID ?? 0);
        RecordUtil<PokeTradeBotBS>.Record($"Iniciando\t{trainerNID:X16}\t{tradePartner?.TrainerName ?? "Desconocido"}\t{poke.Trainer.TrainerName}\t{poke.Trainer.ID}\t{poke.ID}\t{toSend.EncryptionConstant:X8}");
        Log($"Se encontró compañero de Intercambio por Enlace: {tradePartner?.TrainerName ?? "Desconocido"}-{tradePartner?.TID7 ?? "0"} (ID: {trainerNID})");
        TradeProgressChanged?.Invoke(63);

        poke.SendNotification(this, $"Entrenador encontrado: **{tradePartner?.TrainerName ?? "Desconocido"}**.\n\n▼\n Aquí está tu Información\n **TID**: __{tradePartner?.TID7 ?? "0"}__\n **SID**: __{tradePartner?.SID7 ?? "0"}__\n▲\n\n Esperando por un __Pokémon__...");

        var tradeCodeStorage = new TradeCodeStorage();
        var existingTradeDetails = tradeCodeStorage.GetTradeDetails(poke.Trainer.ID);

        string ot = tradePartner?.TrainerName ?? string.Empty;
        int tid = int.Parse(tradePartner?.TID7 ?? "0");
        int sid = int.Parse(tradePartner?.SID7 ?? "0");

        if (existingTradeDetails != null)
        {
            bool shouldUpdateOT = existingTradeDetails.OT != tradePartner?.TrainerName;
            bool shouldUpdateTID = existingTradeDetails.TID != tid;
            bool shouldUpdateSID = existingTradeDetails.SID != sid;

            ot = shouldUpdateOT ? (tradePartner?.TrainerName ?? string.Empty) : (existingTradeDetails.OT ?? (tradePartner?.TrainerName ?? string.Empty));
            tid = shouldUpdateTID ? tid : existingTradeDetails.TID;
            sid = shouldUpdateSID ? sid : existingTradeDetails.SID;
        }

        if (ot != null)
        {
            tradeCodeStorage.UpdateTradeDetails(poke.Trainer.ID, ot, tid, sid);
        }

        var partnerCheck = CheckPartnerReputation(this, poke, trainerNID, tradePartner?.TrainerName ?? string.Empty, AbuseSettings, token);
        if (partnerCheck != PokeTradeResult.Success)
            return PokeTradeResult.SuspiciousActivity;

        if (Hub.Config.Legality.UseTradePartnerInfo && !poke.IgnoreAutoOT)
        {
            toSend = await ApplyAutoOT(toSend, sav, tradePartner?.TrainerName ?? string.Empty, (uint)tid, (uint)sid, token);
        }

        await Task.Delay(2_000, token).ConfigureAwait(false);

        // Confirm Box 1 Slot 1
        if (poke.Type == PokeTradeType.Specific)
        {
            for (int i = 0; i < 5; i++)
                await Click(A, 0_500, token).ConfigureAwait(false);
        }

        // Requires at least one trade for this pointer to make sense, so cache it here.
        LinkTradePokemonOffset = await SwitchConnection.PointerAll(Offsets.LinkTradePartnerPokemonPointer, token).ConfigureAwait(false);

        if (poke.Type == PokeTradeType.Dump)
            return await ProcessDumpTradeAsync(poke, token).ConfigureAwait(false);

        // Wait for user input... Needs to be different from the previously offered Pokémon.
        var tradeOffered = await ReadUntilChanged(LinkTradePokemonOffset, lastOffered, 25_000, 1_000, false, true, token).ConfigureAwait(false);
        if (!tradeOffered)
            return PokeTradeResult.TrainerTooSlow;

        // If we detected a change, they offered something.
        var offered = await ReadPokemon(LinkTradePokemonOffset, BoxFormatSlotSize, token).ConfigureAwait(false);
        if (offered.Species == 0 || !offered.ChecksumValid)
            return PokeTradeResult.TrainerTooSlow;

        // Add Special Request handling here
        SpecialTradeType itemReq = SpecialTradeType.None;
        if (poke.Type == PokeTradeType.Seed)
            itemReq = CheckItemRequest(ref offered, this, poke, tradePartner?.TrainerName ?? string.Empty, sav);
        if (itemReq == SpecialTradeType.FailReturn)
            return PokeTradeResult.IllegalTrade;

        if (poke.Type == PokeTradeType.Seed && itemReq == SpecialTradeType.None)
        {
            // Salir inmediatamente, no estamos intercambiando nada.
            poke.SendNotification(this, "⚠️ ¡Sin objeto equipado o solicitud válida! Cancelando este intercambio.");
            TradeProgressChanged?.Invoke(0);

            await EnsureOutsideOfUnionRoom(token).ConfigureAwait(false);
            return PokeTradeResult.TrainerRequestBad;
        }

        var trainer = new PartnerDataHolder(0, tradePartner?.TrainerName ?? string.Empty, tradePartner?.TID7 ?? "0");
        PokeTradeResult update;
        (toSend, update) = await GetEntityToSend(sav, poke, offered, toSend, trainer, poke.Type == PokeTradeType.Seed ? itemReq : null, token).ConfigureAwait(false);
        if (update != PokeTradeResult.Success)
        {
            if (itemReq != SpecialTradeType.None)
            {
                poke.SendNotification(this, "⚠️ Tu solicitud no es legal. Por favor intenta con un Pokémon o solicitud diferente.");
            }
            return update;
        }

        if (itemReq == SpecialTradeType.WonderCard)
            poke.SendNotification(this, "✅ ¡Distribución exitosa!");
        else if (itemReq != SpecialTradeType.None && itemReq != SpecialTradeType.Shinify)
            poke.SendNotification(this, "✅ ¡Solicitud especial completada con éxito!");
        else if (itemReq == SpecialTradeType.Shinify)
            poke.SendNotification(this, "✅ ¡Shinify completado! ¡Gracias por ser parte de la comunidad!");

        // Check if the offered Pokemon will evolve upon trade BEFORE confirming
        if (Hub.Config.Trade.TradeConfiguration.DisallowTradeEvolve && TradeEvolutions.WillTradeEvolve(offered.Species, offered.Form, offered.HeldItem, toSend.Species))
        {
            Log("Intercambio cancelado porque el entrenador ofreció un Pokémon que evolucionaría al intercambiarlo.");
            poke.SendNotification(this, "⚠️ Intercambio cancelado. No puedes intercambiar un **Pokémon que vaya a evolucionar**. Para evitar esto, dale a tu Pokémon una Piedra Eterna o intercambia un Pokémon diferente.");
            await EnsureOutsideOfUnionRoom(token).ConfigureAwait(false);
            return PokeTradeResult.TradeEvolveNotAllowed;
        }

        Log(PokeTradeBotUtil.FormatFinalTrainerInfo(toSend));
        var tradeResult = await ConfirmAndStartTrading(poke, token).ConfigureAwait(false);
        if (tradeResult != PokeTradeResult.Success)
            return tradeResult;

        if (token.IsCancellationRequested)
            return PokeTradeResult.RoutineCancel;

        // Trade was Successful!
        var received = await ReadPokemon(BoxStartOffset, BoxFormatSlotSize, token).ConfigureAwait(false);
        // Pokémon in b1s1 is same as the one they were supposed to receive (was never sent).
        if (SearchUtil.HashByDetails(received) == SearchUtil.HashByDetails(toSend) && received.Checksum == toSend.Checksum)
        {
            Log("El usuario no completó el intercambio.");
            TradeProgressChanged?.Invoke(0);

            return PokeTradeResult.TrainerTooSlow;
        }

        // As long as we got rid of our inject in b1s1, assume the trade went through.
        Log("El usuario completó el intercambio.");
        poke.TradeFinished(this, received);
        TradeProgressChanged?.Invoke(100);

        // Solo registrar si completamos el intercambio.
        UpdateCountsAndExport(poke, received, toSend);

        // Aún necesitamos esperar la animación del intercambio.
        await Task.Delay(12_000, token).ConfigureAwait(false);

        Log("Intentando salir de la Sala Unión.");
        TradeProgressChanged?.Invoke(100);
        // Ahora salir de la Sala Unión.
        if (!await EnsureOutsideOfUnionRoom(token).ConfigureAwait(false))
            return PokeTradeResult.RecoverReturnOverworld;

        // A veces ofrecieron otro Pokémon, así que almacenarlo inmediatamente al salir de la Sala Unión.
        lastOffered = await SwitchConnection.ReadBytesAbsoluteAsync(LinkTradePokemonOffset, 8, token).ConfigureAwait(false);

        return PokeTradeResult.Success;
    }

    private async Task PerformTrade(SAV8BS sav, PokeTradeDetail<PB8> detail, PokeRoutineType type, uint priority, CancellationToken token)
    {
        PokeTradeResult result;
        try
        {
            detail.IsProcessing = true;

            if (detail.Type == PokeTradeType.Batch)
                result = await PerformBatchTrade(sav, detail, token).ConfigureAwait(false);
            else
                result = await PerformLinkCodeTrade(sav, detail, token).ConfigureAwait(false);

            if (result != PokeTradeResult.Success)
            {
                if (detail.Type == PokeTradeType.Batch)
                    await HandleAbortedBatchTrade(detail, type, priority, result, token).ConfigureAwait(false);
                else
                    HandleAbortedTrade(detail, type, priority, result);
            }
        }
        catch (SocketException socket)
        {
            Log(socket.Message);
            result = PokeTradeResult.ExceptionConnection;
            if (detail.Type == PokeTradeType.Batch)
                await HandleAbortedBatchTrade(detail, type, priority, result, token).ConfigureAwait(false);
            else
                HandleAbortedTrade(detail, type, priority, result);
            throw;
        }
        catch (Exception e)
        {
            Log(e.Message);
            result = PokeTradeResult.ExceptionInternal;
            if (detail.Type == PokeTradeType.Batch)
                await HandleAbortedBatchTrade(detail, type, priority, result, token).ConfigureAwait(false);
            else
                HandleAbortedTrade(detail, type, priority, result);
        }
        finally
        {
            // Ensure processing flag is reset
            detail.IsProcessing = false;
        }
    }

    private async Task<PokeTradeResult> PerformBatchTrade(SAV8BS sav, PokeTradeDetail<PB8> poke, CancellationToken token)
    {
        int completedTrades = 0;
        var startingDetail = poke;
        var originalTrainerID = startingDetail.Trainer.ID;

        var tradesToProcess = poke.BatchTrades ?? [poke.TradeData];
        var totalBatchTrades = tradesToProcess.Count;

        void SendCollectedPokemonAndCleanup()
        {
            var allReceived = BatchTracker.GetReceivedPokemon(originalTrainerID);
            if (allReceived.Count > 0)
            {
                poke.SendNotification(this, $"Enviándote los {allReceived.Count} Pokémon que me intercambiaste antes de la interrupción.");

                Log($"Devolviendo {allReceived.Count} Pokémon al entrenador {originalTrainerID}.");

                // Enviar cada Pokémon directamente en lugar de llamar a TradeFinished
                for (int j = 0; j < allReceived.Count; j++)
                {
                    var pokemon = allReceived[j];
                    var speciesName = SpeciesName.GetSpeciesName(pokemon.Species, 2);
                    Log($"  - Devolviendo: {speciesName} (Checksum: {pokemon.Checksum:X8})");

                    // Enviar el Pokémon directamente al notificador
                    poke.SendNotification(this, pokemon, $"Pokémon que me intercambiaste: {speciesName}");
                    Thread.Sleep(500);
                }
            }
            else
            {
                Log($"No se encontraron Pokémon para devolver al entrenador {originalTrainerID}.");
            }

            BatchTracker.ClearReceivedPokemon(originalTrainerID);
            BatchTracker.ReleaseBatch(originalTrainerID, startingDetail.UniqueTradeID);
            poke.IsProcessing = false;
            Hub.Queues.Info.Remove(new TradeEntry<PB8>(poke, originalTrainerID, PokeRoutineType.Batch, poke.Trainer.TrainerName, poke.UniqueTradeID));
        }

        if (token.IsCancellationRequested)
        {
            SendCollectedPokemonAndCleanup();
            return PokeTradeResult.RoutineCancel;
        }

        UpdateBarrier(poke.IsSynchronized);
        poke.TradeInitialize(this);
        Hub.Config.Stream.EndEnterCode(this);

        if (await CheckIfSoftBanned(SoftBanOffset, token).ConfigureAwait(false))
            await UnSoftBan(token).ConfigureAwait(false);

        if (!await EnterUnionRoomWithCode(poke.Type, poke.Code, token).ConfigureAwait(false))
        {
            SendCollectedPokemonAndCleanup();
            await RestartGameBDSP(token).ConfigureAwait(false);
            return PokeTradeResult.RecoverEnterUnionRoom;
        }

        // Cache trade partner info after first successful connection
        TradePartnerBS? cachedTradePartner = null;
        uint cachedTID = 0;
        uint cachedSID = 0;

        for (int i = 0; i < totalBatchTrades; i++)
        {
            var currentTradeIndex = i;
            var toSend = tradesToProcess[currentTradeIndex];

            poke.TradeData = toSend;
            poke.Notifier.UpdateBatchProgress(currentTradeIndex + 1, toSend, poke.UniqueTradeID);

            if (currentTradeIndex == 0)
            {
                // First trade - prepare Pokemon before searching for partner
                if (toSend.Species != 0)
                    await SetBoxPokemonAbsolute(BoxStartOffset, toSend, token, sav).ConfigureAwait(false);

                await RequestUnionRoomTrade(token).ConfigureAwait(false);
            }
            else
            {
                // Intercambios subsecuentes: ya estamos en la pantalla de intercambio
                // PRIMERO: Preparar el Pokémon ANTES de permitir que el usuario ofrezca
                poke.SendNotification(this, $"✅ ¡Intercambio {completedTrades} completado! **NO OFREZCAS TODAVÍA** - Preparando tu próximo Pokémon ({completedTrades + 1}/{totalBatchTrades})...");


                // Esperar a que la animación del intercambio termine por completo
                await Task.Delay(5_000, token).ConfigureAwait(false);

                // Preparar el siguiente Pokémon con AutoOT si es necesario
                if (toSend.Species != 0)
                {
                    if (Hub.Config.Legality.UseTradePartnerInfo && !poke.IgnoreAutoOT && cachedTradePartner != null)
                    {
                        toSend = await ApplyAutoOT(toSend, sav, cachedTradePartner.TrainerName, cachedTID, cachedSID, token);
                        tradesToProcess[currentTradeIndex] = toSend; // Actualizar la lista
                    }
                    await SetBoxPokemonAbsolute(BoxStartOffset, toSend, token, sav).ConfigureAwait(false);
                }

                // Give time for the Pokemon to be properly set
                await Task.Delay(1_000, token).ConfigureAwait(false);

                // AHORA dile al usuario que puede ofrecer
                poke.SendNotification(this, $"✅ **¡Listo!** Ahora puedes ofrecer tu Pokémon para el intercambio {currentTradeIndex + 1}/{totalBatchTrades}.");

                // Additional delay to ensure we're ready to detect offers
                await Task.Delay(2_000, token).ConfigureAwait(false);
            }

            poke.TradeSearching(this);
            var waitPartner = Hub.Config.Trade.TradeConfiguration.TradeWaitTime;

            while (!await IsUnionWork(UnionTalkingOffset, token).ConfigureAwait(false) && waitPartner > 0)
            {
                for (int j = 0; j < 2; ++j)
                    await Click(A, 0_450, token).ConfigureAwait(false);

                if (--waitPartner <= 0)
                {
                    poke.SendNotification(this, $"⚠️ No se encontró ningún entrenador después del intercambio {completedTrades + 1}/{totalBatchTrades}. Cancelando los intercambios restantes.");
                    TradeProgressChanged?.Invoke(0);

                    SendCollectedPokemonAndCleanup();
                    await EnsureOutsideOfUnionRoom(token).ConfigureAwait(false);
                    return PokeTradeResult.NoTrainerFound;
                }
            }
            Log("¡Encontramos un usuario hablando con nosotros!");


            while (!await IsPartnerParamLoaded(token).ConfigureAwait(false) && waitPartner > 0)
            {
                if (token.IsCancellationRequested)
                {
                    SendCollectedPokemonAndCleanup();
                    return PokeTradeResult.RoutineCancel;
                }

                for (int j = 0; j < 2; ++j)
                    await Click(A, 0_450, token).ConfigureAwait(false);

                if (!await IsUnionWork(UnionTalkingOffset, token).ConfigureAwait(false))
                    break;
                if (--waitPartner <= 0)
                {
                    poke.SendNotification(this, $"⚠️ El compañero de intercambio fue demasiado lento después del intercambio {completedTrades + 1}/{totalBatchTrades}. Cancelando los intercambios restantes.");
                    TradeProgressChanged?.Invoke(0);

                    SendCollectedPokemonAndCleanup();
                    await EnsureOutsideOfUnionRoom(token).ConfigureAwait(false);
                    return PokeTradeResult.TrainerTooSlow;
                }
            }

            Log("Entrando a las cajas...");

            await Task.Delay(3_000, token).ConfigureAwait(false);

            if (!await IsPartnerParamLoaded(token).ConfigureAwait(false))
            {
                poke.SendNotification(this, $"⚠️ El compañero de intercambio fue demasiado lento después del intercambio {completedTrades + 1}/{totalBatchTrades}. Cancelando los intercambios restantes.");
                TradeProgressChanged?.Invoke(0);

                SendCollectedPokemonAndCleanup();
                await EnsureOutsideOfUnionRoom(token).ConfigureAwait(false);
                return PokeTradeResult.TrainerTooSlow;
            }

            var tradePartner = await GetTradePartnerInfo(token).ConfigureAwait(false);
            var trainerNID = GetFakeNID(tradePartner?.TrainerName ?? string.Empty, tradePartner?.TrainerID ?? 0);

            // Cache trade partner info from first trade
            if (currentTradeIndex == 0)
            {
                cachedTradePartner = tradePartner;
                cachedTID = (uint)int.Parse(tradePartner?.TID7 ?? "0");
                cachedSID = (uint)int.Parse(tradePartner?.SID7 ?? "0");
            }

            var tradeCodeStorage = new TradeCodeStorage();
            var existingTradeDetails = tradeCodeStorage.GetTradeDetails(poke.Trainer.ID);

            string ot = tradePartner?.TrainerName ?? string.Empty;
            int tid = int.Parse(tradePartner?.TID7 ?? "0");
            int sid = int.Parse(tradePartner?.SID7 ?? "0");

            if (existingTradeDetails != null)
            {
                bool shouldUpdateOT = existingTradeDetails.OT != tradePartner?.TrainerName;
                bool shouldUpdateTID = existingTradeDetails.TID != tid;
                bool shouldUpdateSID = existingTradeDetails.SID != sid;

                ot = shouldUpdateOT ? (tradePartner?.TrainerName ?? string.Empty) : (existingTradeDetails.OT ?? (tradePartner?.TrainerName ?? string.Empty));
                tid = shouldUpdateTID ? tid : existingTradeDetails.TID;
                sid = shouldUpdateSID ? sid : existingTradeDetails.SID;
            }

            if (ot != null)
            {
                tradeCodeStorage.UpdateTradeDetails(poke.Trainer.ID, ot, tid, sid);
            }

            var partnerCheck = CheckPartnerReputation(this, poke, trainerNID, tradePartner?.TrainerName ?? string.Empty, AbuseSettings, token);
            if (partnerCheck != PokeTradeResult.Success)
            {
                poke.SendNotification(this, $"⚠️ Actividad sospechosa detectada después del intercambio {completedTrades + 1}/{totalBatchTrades}. Cancelando los intercambios restantes.");
                TradeProgressChanged?.Invoke(0);

                SendCollectedPokemonAndCleanup();
                await EnsureOutsideOfUnionRoom(token).ConfigureAwait(false);
                return PokeTradeResult.SuspiciousActivity;
            }

            Log($"Se encontró un compañero de Intercambio por Enlace: {tradePartner?.TrainerName ?? "Desconocido"}-{tradePartner?.TID7 ?? "0"} (ID: {trainerNID})");
            TradeProgressChanged?.Invoke(63);


            // First trade only - send partner found notification
            if (currentTradeIndex == 0)
            {
                poke.SendNotification(this, $"Entrenador encontrado: **{tradePartner?.TrainerName ?? "Desconocido"}**.\n\n▼\n Aquí está tu Información\n **TID**: __{tradePartner?.TID7 ?? "0"}__\n **SID**: __{tradePartner?.SID7 ?? "0"}__\n▲\n\n Esperando por un __Pokémon__...");
            }

            // Esperar a que el usuario ofrezca un Pokémon
            if (currentTradeIndex == 0)
            {
                poke.SendNotification(this, $"Por favor ofrece tu Pokémon para el intercambio 1/{totalBatchTrades}.");
            }

            // Apply AutoOT for first trade if needed (already done for subsequent trades above)
            if (currentTradeIndex == 0 && Hub.Config.Legality.UseTradePartnerInfo && !poke.IgnoreAutoOT)
            {
                toSend = await ApplyAutoOT(toSend, sav, tradePartner?.TrainerName ?? string.Empty, (uint)tid, (uint)sid, token);
                poke.TradeData = toSend;
                if (toSend.Species != 0)
                    await SetBoxPokemonAbsolute(BoxStartOffset, toSend, token, sav).ConfigureAwait(false);
            }

            await Task.Delay(2_000, token).ConfigureAwait(false);

            LinkTradePokemonOffset = await SwitchConnection.PointerAll(Offsets.LinkTradePartnerPokemonPointer, token).ConfigureAwait(false);

            var offered = await ReadUntilPresent(LinkTradePokemonOffset, 25_000, 1_000, BoxFormatSlotSize, token).ConfigureAwait(false);
            if (offered == null || offered.Species == 0 || !offered.ChecksumValid)
            {
                poke.SendNotification(this, $"⚠️ Se ofreció un Pokémon no válido después del intercambio {completedTrades + 1}/{totalBatchTrades}. Cancelando los intercambios restantes.");
                TradeProgressChanged?.Invoke(0);

                SendCollectedPokemonAndCleanup();
                await EnsureOutsideOfUnionRoom(token).ConfigureAwait(false);
                return PokeTradeResult.TrainerTooSlow;
            }

            var trainer = new PartnerDataHolder(0, tradePartner?.TrainerName ?? string.Empty, tradePartner?.TID7 ?? "0");
            PokeTradeResult update;
            (toSend, update) = await GetEntityToSend(sav, poke, offered, toSend, trainer, null, token).ConfigureAwait(false);
            if (update != PokeTradeResult.Success)
            {
                poke.SendNotification(this, $"⚠️ La verificación de actualización falló después del intercambio {completedTrades + 1}/{totalBatchTrades}. Cancelando los intercambios restantes.");
                TradeProgressChanged?.Invoke(0);

                SendCollectedPokemonAndCleanup();
                await EnsureOutsideOfUnionRoom(token).ConfigureAwait(false);
                return update;
            }

            Log($"Confirming trade {currentTradeIndex + 1}/{totalBatchTrades}.");
            Log(PokeTradeBotUtil.FormatFinalTrainerInfo(toSend));
            TradeProgressChanged?.Invoke(92);

            var tradeResult = await ConfirmAndStartTrading(poke, token).ConfigureAwait(false);
            if (tradeResult != PokeTradeResult.Success)
            {
                poke.SendNotification(this, $"⚠️ La confirmación del intercambio falló después del intercambio {completedTrades + 1}/{totalBatchTrades}. Cancelando los intercambios restantes.");
                TradeProgressChanged?.Invoke(0);

                SendCollectedPokemonAndCleanup();
                await EnsureOutsideOfUnionRoom(token).ConfigureAwait(false);
                return tradeResult;
            }

            var received = await ReadPokemon(BoxStartOffset, BoxFormatSlotSize, token).ConfigureAwait(false);
            if (SearchUtil.HashByDetails(received) == SearchUtil.HashByDetails(toSend) && received.Checksum == toSend.Checksum)
            {
                poke.SendNotification(this, $"⚠️ El compañero no completó el intercambio {completedTrades + 1}/{totalBatchTrades}. Cancelando los intercambios restantes.");
                TradeProgressChanged?.Invoke(0);

                SendCollectedPokemonAndCleanup();
                await EnsureOutsideOfUnionRoom(token).ConfigureAwait(false);
                return PokeTradeResult.TrainerTooSlow;
            }


            Log("El usuario completó el intercambio.");

            UpdateCountsAndExport(poke, received, toSend);
            LogSuccessfulTrades(poke, trainerNID, tradePartner?.TrainerName ?? "Desconocido");

            BatchTracker.AddReceivedPokemon(originalTrainerID, received);
            completedTrades = currentTradeIndex + 1;
            Log($"Se añadió el Pokémon recibido {received.Species} (Checksum: {received.Checksum:X8}) al registro por lotes para el entrenador {originalTrainerID} (Intercambio {completedTrades}/{totalBatchTrades})");

            if (completedTrades == totalBatchTrades)
            {
                // Obtener todos los Pokémon recibidos antes de limpiar cualquier cosa
                var allReceived = BatchTracker.GetReceivedPokemon(originalTrainerID);
                Log($"Intercambios por lote completados. Se encontraron {allReceived.Count} Pokémon almacenados para el entrenador {originalTrainerID}");
                TradeProgressChanged?.Invoke(100);


                // Primero enviar notificación de que los intercambios han terminado
                poke.SendNotification(this, "✅ ¡Todos los intercambios por lote han sido completados! ¡Gracias por intercambiar!");

                // Enviar de vuelta todos los Pokémon recibidos si ReturnPKMs está habilitado
                if (Hub.Config.Discord.ReturnPKMs && allReceived.Count > 0)
                {
                    poke.SendNotification(this, $"✅ Aquí tienes los {allReceived.Count} Pokémon que me intercambiaste:");

                    // Enviar cada Pokémon directamente en lugar de llamar a TradeFinished
                    for (int j = 0; j < allReceived.Count; j++)
                    {
                        var pokemon = allReceived[j];
                        var speciesName = SpeciesName.GetSpeciesName(pokemon.Species, 2);
                        Log($"  - Devolviendo: {speciesName} (Checksum: {pokemon.Checksum:X8})");

                        // Enviar el Pokémon directamente al notificador
                        poke.SendNotification(this, pokemon, $"Pokémon que me intercambiaste: {speciesName}");
                        await Task.Delay(500, token).ConfigureAwait(false);
                    }
                }

                // Now call TradeFinished ONCE for the entire batch with the last received Pokemon
                // This signals that the entire batch trade transaction is complete
                if (allReceived.Count > 0)
                {
                    poke.TradeFinished(this, allReceived[^1]);
                }
                else
                {
                    poke.TradeFinished(this, received);
                }

                // Mark the batch as fully completed and clean up
                Hub.Queues.CompleteTrade(this, startingDetail);
                BatchTracker.ClearReceivedPokemon(originalTrainerID);

                // Exit the trade state to prevent further searching
                await EnsureOutsideOfUnionRoom(token).ConfigureAwait(false);
                poke.IsProcessing = false;
                break;
            }

            // Store last offered for next iteration
            lastOffered = await SwitchConnection.ReadBytesAbsoluteAsync(LinkTradePokemonOffset, 8, token).ConfigureAwait(false);
        }

        // Ensure we exit properly even if the loop breaks unexpectedly
        await EnsureOutsideOfUnionRoom(token).ConfigureAwait(false);
        poke.IsProcessing = false;
        return PokeTradeResult.Success;
    }

    private async Task HandleAbortedBatchTrade(PokeTradeDetail<PB8> detail, PokeRoutineType type, uint priority, PokeTradeResult result, CancellationToken token)
    {
        detail.IsProcessing = false;

        // Always remove from UsersInQueue on abort
        Hub.Queues.Info.Remove(new TradeEntry<PB8>(detail, detail.Trainer.ID, type, detail.Trainer.TrainerName, detail.UniqueTradeID));

        if (detail.TotalBatchTrades > 1)
        {
            // Release the batch claim on failure
            BatchTracker.ReleaseBatch(detail.Trainer.ID, detail.UniqueTradeID);

            if (result.ShouldAttemptRetry() && detail.Type != PokeTradeType.Random && !detail.IsRetry)
            {
                detail.IsRetry = true;
                Hub.Queues.Enqueue(type, detail, Math.Min(priority, PokeTradePriorities.Tier2));
                detail.SendNotification(this, "⚠️ ¡Ups! Algo ocurrió durante tu intercambio por lotes. Te volveré a poner en la cola para otro intento.");
            }
            else
            {
                detail.SendNotification(this, $"⚠️ El intercambio por lotes falló: {result}");
                TradeProgressChanged?.Invoke(0);

                detail.TradeCanceled(this, result);
                await EnsureOutsideOfUnionRoom(token).ConfigureAwait(false);
            }
        }
        else
        {
            HandleAbortedTrade(detail, type, priority, result);
        }
    }

    private async Task<PokeTradeResult> ProcessDumpTradeAsync(PokeTradeDetail<PB8> detail, CancellationToken token)
    {
        if (token.IsCancellationRequested) return PokeTradeResult.RoutineCancel;

        int ctr = 0;
        var time = TimeSpan.FromSeconds(Hub.Config.Trade.TradeConfiguration.MaxDumpTradeTime);
        var start = DateTime.Now;

        var bctr = 0;
        while (ctr < Hub.Config.Trade.TradeConfiguration.MaxDumpsPerTrade && DateTime.Now - start < time)
        {
            if (token.IsCancellationRequested) return PokeTradeResult.RoutineCancel;

            // We're no longer talking, so they probably quit on us.
            if (!await IsUnionWork(UnionTalkingOffset, token).ConfigureAwait(false))
                break;
            if (bctr++ % 3 == 0)
                await Click(B, 0_100, token).ConfigureAwait(false);

            // Wait for user input... Needs to be different from the previously offered Pokémon.
            var tradeOffered = await ReadUntilChanged(LinkTradePokemonOffset, lastOffered, 3_000, 1_000, false, true, token).ConfigureAwait(false);
            if (!tradeOffered)
                continue;

            // If we detected a change, they offered something.
            var pk = await ReadPokemon(LinkTradePokemonOffset, BoxFormatSlotSize, token).ConfigureAwait(false);
            var newECchk = await SwitchConnection.ReadBytesAbsoluteAsync(LinkTradePokemonOffset, 8, token).ConfigureAwait(false);
            if (pk.Species == 0 || !pk.ChecksumValid || lastOffered.SequenceEqual(newECchk))
                continue;
            lastOffered = newECchk;

            // Send results from separate thread; the bot doesn't need to wait for things to be calculated.
            if (DumpSetting.Dump)
            {
                var subfolder = detail.Type.ToString().ToLower();
                DumpPokemon(DumpSetting.DumpFolder, subfolder, pk); // received
            }

            var la = new LegalityAnalysis(pk);
            var verbose = $"```{la.Report(true)}```";
            Log($"El Pokémon mostrado es: {(la.Valid ? "Válido" : "Inválido")}.");

            ctr++;
            var msg = Hub.Config.Trade.TradeConfiguration.DumpTradeLegalityCheck ? verbose : $"File {ctr}";

            // Extra information about trainer data for people requesting with their own trainer data.
            var ot = pk.OriginalTrainerName;
            var ot_gender = pk.OriginalTrainerGender == 0 ? "Masculino" : "Femenino";
            var tid = pk.GetDisplayTID().ToString(pk.GetTrainerIDFormat().GetTrainerIDFormatStringTID());
            var sid = pk.GetDisplaySID().ToString(pk.GetTrainerIDFormat().GetTrainerIDFormatStringSID());
            msg += $"\n**Datos del Entrenador**\n```OT: {ot}\nOTGender: {ot_gender}\nTID: {tid}\nSID: {sid}```";

            // Extra information for shiny eggs, because of people dumping to skip hatching.
            var eggstring = pk.IsEgg ? "Huevo " : string.Empty;
            msg += pk.IsShiny ? $"\n**¡Este Pokémon {eggstring}es variocolor!**" : string.Empty;
            detail.SendNotification(this, pk, msg);
        }

        Log($"El ciclo de volcado terminó después de procesar {ctr} Pokémon.");
        TradeProgressChanged?.Invoke(100);

        if (ctr == 0)
            return PokeTradeResult.TrainerTooSlow;

        TradeSettings.CountStatsSettings.AddCompletedDumps();
        detail.Notifier.SendNotification(this, detail, $"✅ Se volcaron {ctr} Pokémon.");
        detail.Notifier.TradeFinished(this, detail, detail.TradeData); // blank pk8
        return PokeTradeResult.Success;
    }

    private async Task RequestUnionRoomTrade(CancellationToken token)
    {
        if (token.IsCancellationRequested) return;

        // Move to middle of room
        await PressAndHold(DUP, 2_000, 0_250, token).ConfigureAwait(false);
        // Y-button trades always put us in a place where we can open the call menu without having to move.
        Log("Intentando abrir el menú Y.");
        TradeProgressChanged?.Invoke(49);

        await Click(Y, 1_000, token).ConfigureAwait(false);
        await Click(A, 0_400, token).ConfigureAwait(false);
        await Click(DDOWN, 0_400, token).ConfigureAwait(false);
        await Click(DDOWN, 0_400, token).ConfigureAwait(false);
        await Click(A, 0_100, token).ConfigureAwait(false);
    }

    private async Task RestartGameBDSP(CancellationToken token)
    {
        await ReOpenGame(Hub.Config, token).ConfigureAwait(false);
        await InitializeSessionOffsets(token).ConfigureAwait(false);
    }

    private async Task RestartGameIfCantLeaveUnionRoom(CancellationToken token)
    {
        if (token.IsCancellationRequested) return;

        if (!await EnsureOutsideOfUnionRoom(token).ConfigureAwait(false))
            await RestartGameBDSP(token).ConfigureAwait(false);
    }

    /// <summary>
    /// Checks if the barrier needs to get updated to consider this bot.
    /// If it should be considered, it adds it to the barrier if it is not already added.
    /// If it should not be considered, it removes it from the barrier if not already removed.
    /// </summary>
    private void UpdateBarrier(bool shouldWait)
    {
        if (ShouldWaitAtBarrier == shouldWait)
            return; // no change required

        ShouldWaitAtBarrier = shouldWait;
        if (shouldWait)
        {
            Hub.BotSync.Barrier.AddParticipant();
            Log($"Se unió a la Barrera. Conteo: {Hub.BotSync.Barrier.ParticipantCount}");
        }
        else
        {
            Hub.BotSync.Barrier.RemoveParticipant();
            Log($"Salió de la Barrera. Conteo: {Hub.BotSync.Barrier.ParticipantCount}");
        }
    }

    private void UpdateCountsAndExport(PokeTradeDetail<PB8> poke, PB8 received, PB8 toSend)
    {
        var counts = TradeSettings;
        if (poke.Type == PokeTradeType.Random)
            counts.CountStatsSettings.AddCompletedDistribution();
        else if (poke.Type == PokeTradeType.FixOT)
            counts.CountStatsSettings.AddCompletedFixOTs();
        else
            counts.CountStatsSettings.AddCompletedTrade();

        if (DumpSetting.Dump && !string.IsNullOrEmpty(DumpSetting.DumpFolder))
        {
            var subfolder = poke.Type.ToString().ToLower();
            var service = poke.Notifier.GetType().ToString().ToLower();
            var tradedFolder = service.Contains("twitch") ? Path.Combine("traded", "twitch") : service.Contains("discord") ? Path.Combine("traded", "discord") : "traded";
            DumpPokemon(DumpSetting.DumpFolder, subfolder, received); // received by bot
            if (poke.Type is PokeTradeType.Specific or PokeTradeType.FixOT)
                DumpPokemon(DumpSetting.DumpFolder, tradedFolder, toSend); // sent to partner
        }
    }

    private void WaitAtBarrierIfApplicable(CancellationToken token)
    {
        if (!ShouldWaitAtBarrier)
            return;
        var opt = Hub.Config.Distribution.SynchronizeBots;
        if (opt == BotSyncOption.NoSync)
            return;

        var timeoutAfter = Hub.Config.Distribution.SynchronizeTimeout;
        if (FailedBarrier == 1) // failed last iteration
            timeoutAfter *= 2; // try to re-sync in the event things are too slow.

        var result = Hub.BotSync.Barrier.SignalAndWait(TimeSpan.FromSeconds(timeoutAfter), token);

        if (result)
        {
            FailedBarrier = 0;
            return;
        }

        FailedBarrier++;
        Log($"La sincronización de la Barrera agotó el tiempo después de {timeoutAfter} segundos. Continuando.");
    }

    private Task WaitForQueueStep(int waitCounter, CancellationToken token)
    {
        if (waitCounter == 0)
        {
            // Updates the assets.
            Hub.Config.Stream.IdleAssets(this);
            Log("Nada que revisar, esperando nuevos usuarios...");
            TradeProgressChanged?.Invoke(0);

        }

        const int interval = 10;
        if (waitCounter % interval == interval - 1 && Hub.Config.AntiIdle)
            return Click(B, 1_000, token);
        return Task.Delay(1_000, token);
    }
}
