using PKHeX.Core;
using PKHeX.Core.Searching;
using SysBot.Base;
using SysBot.Base.Util;
using SysBot.Pokemon.Helpers;
using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using static SysBot.Base.SwitchButton;
using static SysBot.Pokemon.PokeDataOffsetsSV;
using static SysBot.Pokemon.TradeHub.SpecialRequests;

namespace SysBot.Pokemon;

// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
public class PokeTradeBotSV(PokeTradeHub<PK9> Hub, PokeBotState Config) : PokeRoutineExecutor9SV(Config), ICountBot, ITradeBot
{

    public readonly TradeAbuseSettings AbuseSettings = Hub.Config.TradeAbuse;

    /// <summary>
    /// Folder to dump received trade data to.
    /// </summary>
    /// <remarks>If null, will skip dumping.</remarks>
    private readonly FolderSettings DumpSetting = Hub.Config.Folder;

    private readonly TradeSettings TradeSettings = Hub.Config.Trade;

    // Cached offsets that stay the same per session.
    private ulong BoxStartOffset;

    private ulong ConnectedOffset;

    private uint DisplaySID;

    private uint DisplayTID;

    // Track the last Pokémon we were offered since it persists between trades.
    private byte[] lastOffered = new byte[8];

    // Store the current save's OT and TID/SID for comparison.
    private string OT = string.Empty;

    private ulong OverworldOffset;

    private ulong PortalOffset;

    // Stores whether we returned all the way to the overworld, which repositions the cursor.
    private bool StartFromOverworld = true;

    private ulong TradePartnerNIDOffset;

    private ulong TradePartnerOfferedOffset;

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

    public override Task HardStop()
    {
        UpdateBarrier(false);
        return CleanExit(CancellationToken.None);
    }

    public override async Task MainLoop(CancellationToken token)
    {
        try
        {
            Hub.Queues.Info.CleanStuckTrades();
            await InitializeHardware(Hub.Config.Trade, token).ConfigureAwait(false);

            Log("Identificando los datos del entrenador de la consola anfitriona.");
            var sav = await IdentifyTrainer(token).ConfigureAwait(false);
            OT = sav.OT;
            DisplaySID = sav.DisplaySID;
            DisplayTID = sav.DisplayTID;
            RecentTrainerCache.SetRecentTrainer(sav);
            await InitializeSessionOffsets(token).ConfigureAwait(false);
            OnConnectionSuccess();

            // Obliga al bot a pasar por todos los pasos nuevamente en su primera ejecución.
            StartFromOverworld = true;

            Log($"Iniciando el bucle principal de {nameof(PokeTradeBotSV)}.");
            await InnerLoop(sav, token).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            OnConnectionError(e);
            throw;
        }

        Log($"Finalizando el bucle de {nameof(PokeTradeBotSV)}.");
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

    protected virtual async Task<(PK9 toSend, PokeTradeResult check)> GetEntityToSend(SAV9SV sav, PokeTradeDetail<PK9> poke, PK9 offered, byte[] oldEC, PK9 toSend, PartnerDataHolder partnerID, SpecialTradeType? stt, CancellationToken token)
    {
        return poke.Type switch
        {
            PokeTradeType.Random => await HandleRandomLedy(sav, poke, offered, toSend, partnerID, token).ConfigureAwait(false),
            PokeTradeType.Clone => await HandleClone(sav, poke, offered, oldEC, token).ConfigureAwait(false),
            PokeTradeType.FixOT => await HandleFixOT(sav, poke, offered, partnerID, token).ConfigureAwait(false),
            PokeTradeType.Seed when stt is not SpecialTradeType.WonderCard => await HandleClone(sav, poke, offered, oldEC, token).ConfigureAwait(false),
            PokeTradeType.Seed when stt is SpecialTradeType.WonderCard => await JustInject(sav, offered, token).ConfigureAwait(false),
            _ => (toSend, PokeTradeResult.Success),
        };
    }

    protected virtual (PokeTradeDetail<PK9>? detail, uint priority) GetTradeData(PokeRoutineType type)
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

    // Upon connecting, their Nintendo ID will instantly update.
    protected virtual async Task<bool> WaitForTradePartner(CancellationToken token)
    {
        Log("Esperando al entrenador...");
        TradeProgressChanged?.Invoke(48);

        int ctr = (Hub.Config.Trade.TradeConfiguration.TradeWaitTime * 1_000) - 2_000;
        await Task.Delay(2_000, token).ConfigureAwait(false);
        while (ctr > 0)
        {
            await Task.Delay(1_000, token).ConfigureAwait(false);
            ctr -= 1_000;
            var newNID = await GetTradePartnerNID(TradePartnerNIDOffset, token).ConfigureAwait(false);
            if (newNID != 0)
            {
                TradePartnerOfferedOffset = await SwitchConnection.PointerAll(Offsets.LinkTradePartnerPokemonPointer, token).ConfigureAwait(false);
                return true;
            }

            // Fully load into the box.
            await Task.Delay(1_000, token).ConfigureAwait(false);
        }
        return false;
    }

    private async Task<PK9> ApplyAutoOT(PK9 toSend, TradeMyStatus tradePartner, SAV9SV sav, CancellationToken token)
    {
        if (toSend.Version == GameVersion.GO)
        {
            var goClone = toSend.Clone();
            goClone.OriginalTrainerName = tradePartner.OT;

            ClearOTTrash(goClone, tradePartner);

            if (!toSend.ChecksumValid)
                goClone.RefreshChecksum();

            Log("Se aplicó solo el nombre del OT al Pokémon proveniente de GO.");
            await SetBoxPokemonAbsolute(BoxStartOffset, goClone, token, sav).ConfigureAwait(false);
            return goClone;
        }

        if (toSend is IHomeTrack pk && pk.HasTracker)
        {
            Log("Se detectó un rastreador de HOME. No se puede aplicar AutoOT.");
            return toSend;
        }

        if (toSend.Generation != toSend.Format)
        {
            Log("No se pueden aplicar los detalles del compañero: el manejador actual no puede ser un OT de una generación diferente.");
            return toSend;
        }

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
            Log("Regalo Misterioso con OT/TID/SID preestablecidos detectado. Omitiendo AutoOT por completo.");
            return toSend;
        }

        var cln = toSend.Clone();

        if (isMysteryGift)
        {
            Log("Regalo Misterioso detectado. Solo se aplicará la información del OT, preservando el idioma.");
            cln.OriginalTrainerGender = (byte)tradePartner.Gender;
            cln.TrainerTID7 = (uint)Math.Abs(tradePartner.DisplayTID);
            cln.TrainerSID7 = (uint)Math.Abs(tradePartner.DisplaySID);
            cln.OriginalTrainerName = tradePartner.OT;
        }
        else
        {
            cln.OriginalTrainerGender = (byte)tradePartner.Gender;
            cln.TrainerTID7 = (uint)Math.Abs(tradePartner.DisplayTID);
            cln.TrainerSID7 = (uint)Math.Abs(tradePartner.DisplaySID);

            // Only override language if Pokemon has default/config language
            // If user explicitly requested a different language, preserve it
            var configLanguage = (int)legalitySettings.GenerateLanguage;
            if (toSend.Language != configLanguage && toSend.Language >= 1 && toSend.Language <= 12)
            {
                cln.Language = toSend.Language; // Preserve explicitly requested language
            }
            else
            {
                cln.Language = tradePartner.Language; // Use trade partner's language
            }

            cln.OriginalTrainerName = tradePartner.OT;
        }

        ClearOTTrash(cln, tradePartner);

        ushort species = toSend.Species;
        GameVersion version;
        switch (species)
        {
            case (ushort)Species.Koraidon:
            case (ushort)Species.GougingFire:
            case (ushort)Species.RagingBolt:
                version = GameVersion.SL;
                Log("Pokémon exclusivo de la versión Scarlet, cambiando la versión a Scarlet.");
                break;

            case (ushort)Species.Miraidon:
            case (ushort)Species.IronCrown:
            case (ushort)Species.IronBoulder:
                version = GameVersion.VL;
                Log("Pokémon exclusivo de la versión Violet, cambiando la versión a Violet.");
                break;

            default:
                version = (GameVersion)tradePartner.Game;
                break;
        }
        cln.Version = version;

        if (!toSend.IsNicknamed)
            cln.ClearNickname();

        if (toSend.IsShiny)
            cln.PID = (uint)((cln.TID16 ^ cln.SID16 ^ (cln.PID & 0xFFFF) ^ toSend.ShinyXor) << 16) | (cln.PID & 0xFFFF);

        if (!toSend.ChecksumValid)
            cln.RefreshChecksum();

        var tradeSV = new LegalityAnalysis(cln);
        if (tradeSV.Valid)
        {
            Log("El Pokémon es válido, usando la información del compañero de intercambio (AutoOT).");
            await SetBoxPokemonAbsolute(BoxStartOffset, cln, token, sav).ConfigureAwait(false);
            return cln;
        }
        else
        {
            Log("No se puede aplicar AutoOT al Pokémon de intercambio.");
            return toSend;
        }
    }

    private static void ClearOTTrash(PK9 pokemon, TradeMyStatus tradePartner)
    {
        Span<byte> trash = pokemon.OriginalTrainerTrash;
        trash.Clear();
        string name = tradePartner.OT;
        int maxLength = trash.Length / 2;
        int actualLength = Math.Min(name.Length, maxLength);
        for (int i = 0; i < actualLength; i++)
        {
            char value = name[i];
            trash[i * 2] = (byte)value;
            trash[(i * 2) + 1] = (byte)(value >> 8);
        }
        if (actualLength < maxLength)
        {
            trash[actualLength * 2] = 0x00;
            trash[(actualLength * 2) + 1] = 0x00;
        }
    }

    private async Task<PokeTradeResult> ConfirmAndStartTrading(PokeTradeDetail<PK9> detail, CancellationToken token)
    {
        // We'll keep watching B1S1 for a change to indicate a trade started -> should try quitting at that point.
        var oldEC = await SwitchConnection.ReadBytesAbsoluteAsync(BoxStartOffset, 8, token).ConfigureAwait(false);

        await Click(A, 3_000, token).ConfigureAwait(false);
        for (int i = 0; i < Hub.Config.Trade.TradeConfiguration.MaxTradeConfirmTime; i++)
        {
            // We can fall out of the box if the user offers, then quits.
            if (!await IsInBox(PortalOffset, token).ConfigureAwait(false))
                return PokeTradeResult.TrainerLeft;

            await Click(A, 1_000, token).ConfigureAwait(false);

            // EC is detectable at the start of the animation.
            var newEC = await SwitchConnection.ReadBytesAbsoluteAsync(BoxStartOffset, 8, token).ConfigureAwait(false);
            if (!newEC.SequenceEqual(oldEC))
            {
                // Check if partner offered a Pokemon that will evolve
                if (Hub.Config.Trade.TradeConfiguration.DisallowTradeEvolve)
                {
                    var offered = await ReadUntilPresent(TradePartnerOfferedOffset, 2_000, 0_500, BoxFormatSlotSize, token).ConfigureAwait(false);
                    if (offered != null && TradeEvolutions.WillTradeEvolve(offered.Species, offered.Form, offered.HeldItem, detail.TradeData.Species))
                    {
                        Log("Intercambio cancelado porque el entrenador ofreció un Pokémon que evolucionaría al intercambiarlo.");
                        detail.SendNotification(this, "⚠️ Intercambio cancelado. No puedes intercambiar un Pokémon que vaya a evolucionar. Para evitar esto, dale a tu Pokémon una Piedra Eterna o intercambia un Pokémon diferente.");
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

    // Should be used from the overworld. Opens X menu, attempts to connect online, and enters the Portal.
    // The cursor should be positioned over Link Trade.
    private async Task<bool> ConnectAndEnterPortal(CancellationToken token)
    {
        if (!await IsOnOverworld(OverworldOffset, token).ConfigureAwait(false))
            await RecoverToOverworld(token).ConfigureAwait(false);

        Log("Abriendo el Poképortal.");

        // Abrir el menú X.
        await Click(X, 1_000, token).ConfigureAwait(false);

        // Manejar las noticias que aparecen.
        if (await SwitchConnection.IsProgramRunning(LibAppletWeID, token).ConfigureAwait(false))
        {
            Log("Noticias detectadas, se cerrarán una vez que carguen.");
            await Task.Delay(5_000, token).ConfigureAwait(false);
            await Click(B, 2_000, token).ConfigureAwait(false);
        }

        // Desplazarse al final del menú principal, así no importa si Picnic está desbloqueado.
        await Click(DRIGHT, 0_300, token).ConfigureAwait(false);
        await PressAndHold(DDOWN, 1_000, 1_000, token).ConfigureAwait(false);
        await Click(DUP, 0_200, token).ConfigureAwait(false);
        await Click(DUP, 0_200, token).ConfigureAwait(false);
        await Click(DUP, 0_200, token).ConfigureAwait(false);
        await Click(A, 1_000, token).ConfigureAwait(false);

        return await SetUpPortalCursor(token).ConfigureAwait(false);
    }

    private async Task<bool> ConnectToOnline(PokeTradeHubConfig config, CancellationToken token)
    {
        int attemptCount = 0;
        const int maxAttempt = 5;
        const int waitTime = 10; // time in minutes to wait after max attempts
        int recheckDelayMs = 5000; // Start with 5 seconds, will increase with exponential backoff

        while (true) // Loop until a successful connection is made or the task is canceled
        {
            if (token.IsCancellationRequested)
            {
                Log("Intento de conexión cancelado.");
                break;
            }
            try
            {
                if (await IsConnectedOnline(ConnectedOffset, token).ConfigureAwait(false))
                {
                    Log("Conexión establecida correctamente.");

                    break; // Exit the loop if connected successfully
                }

                if (attemptCount >= maxAttempt)
                {
                    Log($"No se pudo conectar después de {maxAttempt} intentos. Suponiendo un softban. Iniciando espera de {waitTime} minutos antes de reintentar.");

                    // Waiting process
                    await Click(B, 0_500, token).ConfigureAwait(false);
                    await Click(B, 0_500, token).ConfigureAwait(false);
                    Log($"Esperando {waitTime} minutos antes de intentar reconectar.");
                    await Task.Delay(TimeSpan.FromMinutes(waitTime), token).ConfigureAwait(false);
                    Log("Intentando reabrir el juego.");
                    await ReOpenGame(Hub.Config, token).ConfigureAwait(false);
                    attemptCount = 0; // Reset attempt count
                    recheckDelayMs = 5000; // Reset delay after softban wait
                }

                attemptCount++;
                Log($"Intento {attemptCount} de {maxAttempt}: Intentando conectar en línea...");

                // Connection attempt logic
                await Click(X, 3_000, token).ConfigureAwait(false);
                await Click(L, 5_000 + config.Timings.ExtraTimeConnectOnline, token).ConfigureAwait(false);

                // Use exponential backoff for retries to handle degraded connection conditions
                // Increases delay after multiple failed attempts to allow network recovery
                await Task.Delay(recheckDelayMs, token).ConfigureAwait(false);

                if (attemptCount < maxAttempt)
                {
                    Log("Revisando nuevamente el estado de la conexión en línea...");
                    // Wait and recheck logic
                    await Click(B, 0_500, token).ConfigureAwait(false);

                    // Exponential backoff: increase delay after each failed attempt
                    // Helps with degraded network conditions after multiple trades
                    if (recheckDelayMs < 10000) // Cap at 10 seconds
                    {
                        recheckDelayMs += 1000; // Increment by 1 second each attempt
                        Log($"Incrementando el retraso de reintento a {recheckDelayMs}ms para el siguiente intento");
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"Ocurrió una excepción durante el intento de conexión: {ex.Message}");

                if (attemptCount >= maxAttempt)
                {
                    Log($"No se pudo conectar después de {maxAttempt} intentos debido a una excepción. Esperando {waitTime} minutos antes de reintentar.");
                    await Task.Delay(TimeSpan.FromMinutes(waitTime), token).ConfigureAwait(false);
                    Log("Intentando reabrir el juego.");
                    await ReOpenGame(Hub.Config, token).ConfigureAwait(false);
                    attemptCount = 0;
                    recheckDelayMs = 5000; // Restablecer el retraso después de la recuperación por excepción
                }
            }
        }

        // Final steps after connection is established
        await Task.Delay(3_000 + config.Timings.ExtraTimeConnectOnline, token).ConfigureAwait(false);

        return true;
    }

    private async Task DoNothing(CancellationToken token)
    {
        Log("Ninguna tarea asignada. Esperando la asignación de una nueva tarea.");

        while (!token.IsCancellationRequested && Config.NextRoutineType == PokeRoutineType.Idle)
            await Task.Delay(1_000, token).ConfigureAwait(false);
    }

    private async Task DoTrades(SAV9SV sav, CancellationToken token)
    {
        var type = Config.CurrentRoutineType;
        int waitCounter = 0;
        await SetCurrentBox(0, token).ConfigureAwait(false);
        while (!token.IsCancellationRequested && Config.NextRoutineType == type)
        {
            var (detail, priority) = GetTradeData(type);
            if (detail is null)
            {
                await WaitForQueueStep(waitCounter++, token).ConfigureAwait(false);
                continue;
            }
            waitCounter = 0;

            detail.IsProcessing = true;
            string tradetype = $" ({detail.Type})";
            Log($"Iniciando el siguiente intercambio del Bot {type}{tradetype}. Obteniendo datos...");
            TradeProgressChanged?.Invoke(12);

            Hub.Config.Stream.StartTrade(this, detail, Hub);
            Hub.Queues.StartTrade(this, detail);

            await PerformTrade(sav, detail, type, priority, token).ConfigureAwait(false);
        }
    }

    private async Task ExitTradeToPortal(bool unexpected, CancellationToken token)
    {
        await Task.Delay(1_000, token).ConfigureAwait(false);
        if (await IsInPokePortal(PortalOffset, token).ConfigureAwait(false))
            return;

        if (unexpected)
            Log("Comportamiento inesperado, recuperando el acceso al Portal.");

        // Ensure we're not in the box first.
        // Takes a long time for the Portal to load up, so once we exit the box, wait 5 seconds.
        Log("Saliendo de la caja...");
        TradeProgressChanged?.Invoke(100);
        var attempts = 0;
        while (await IsInBox(PortalOffset, token).ConfigureAwait(false))
        {
            await Click(B, 1_000, token).ConfigureAwait(false);
            if (!await IsInBox(PortalOffset, token).ConfigureAwait(false))
            {
                await Task.Delay(1_000, token).ConfigureAwait(false);
                break;
            }

            await Click(A, 1_000, token).ConfigureAwait(false);
            if (!await IsInBox(PortalOffset, token).ConfigureAwait(false))
            {
                await Task.Delay(1_000, token).ConfigureAwait(false);
                break;
            }

            await Click(B, 1_000, token).ConfigureAwait(false);
            if (!await IsInBox(PortalOffset, token).ConfigureAwait(false))
            {
                await Task.Delay(1_000, token).ConfigureAwait(false);
                break;
            }

            // Didn't make it out of the box for some reason.
            if (++attempts > 20)
            {
                Log("No se pudo salir de la caja, reiniciando el juego.");

                if (!await RecoverToOverworld(token).ConfigureAwait(false))
                    await RestartGameSV(token).ConfigureAwait(false);
                await ConnectAndEnterPortal(token).ConfigureAwait(false);
                return;
            }
        }

        // Wait for the portal to load.
        Log("Esperando a que el portal cargue...");
        attempts = 0;
        while (!await IsInPokePortal(PortalOffset, token).ConfigureAwait(false))
        {
            await Task.Delay(1_000, token).ConfigureAwait(false);
            if (await IsInPokePortal(PortalOffset, token).ConfigureAwait(false))
                break;

            // Didn't make it into the portal for some reason.
            if (++attempts > 40)
            {
                Log("No se pudo cargar el portal, reiniciando el juego.");

                if (!await RecoverToOverworld(token).ConfigureAwait(false))
                    await RestartGameSV(token).ConfigureAwait(false);
                await ConnectAndEnterPortal(token).ConfigureAwait(false);
                return;
            }
        }
    }

    private async Task<TradeMyStatus> GetTradePartnerFullInfo(CancellationToken token)
    {
        // We're able to see both users' MyStatus, but one of them will be ourselves.
        var trader_info = await GetTradePartnerMyStatus(Offsets.Trader1MyStatusPointer, token).ConfigureAwait(false);
        if (trader_info.OT == OT && trader_info.DisplaySID == DisplaySID && trader_info.DisplayTID == DisplayTID) // This one matches ourselves.
            trader_info = await GetTradePartnerMyStatus(Offsets.Trader2MyStatusPointer, token).ConfigureAwait(false);
        return trader_info;
    }

    private void HandleAbortedTrade(PokeTradeDetail<PK9> detail, PokeRoutineType type, uint priority, PokeTradeResult result)
    {
        // Skip processing if we've already handled the notification (e.g., NoTrainerFound)
        if (result == PokeTradeResult.NoTrainerFound)
            return;

        detail.IsProcessing = false;
        if (result.ShouldAttemptRetry() && detail.Type != PokeTradeType.Random && !detail.IsRetry)
        {
            detail.IsRetry = true;
            Hub.Queues.Enqueue(type, detail, Math.Min(priority, PokeTradePriorities.Tier2));
            detail.SendNotification(this, "⚠️ ¡Ups! Algo ocurrió. Te volveré a poner en la cola para otro intento.");
        }
        else
        {
            detail.SendNotification(this, $"⚠️ ¡Ups! Algo ocurrió. Cancelando el intercambio: {result.GetDescription()}.");
            TradeProgressChanged?.Invoke(0);

            detail.TradeCanceled(this, result);
        }
    }

    private async Task<(PK9 toSend, PokeTradeResult check)> HandleClone(SAV9SV sav, PokeTradeDetail<PK9> poke, PK9 offered, byte[] oldEC, CancellationToken token)
    {

        if (Hub.Config.Discord.ReturnPKMs)
            poke.SendNotification(this, offered, $"Aquí tienes lo que me mostraste - {GameInfo.GetStrings("en").Species[offered.Species]}");

        var la = new LegalityAnalysis(offered);
        if (!la.Valid)
        {
            Log($"La solicitud de clonación (de {poke.Trainer.TrainerName}) ha detectado un Pokémon no válido: {GameInfo.GetStrings("en").Species[offered.Species]}.");

            if (DumpSetting.Dump)
                DumpPokemon(DumpSetting.DumpFolder, "hacked", offered);

            var report = la.Report();
            Log(report);
            poke.SendNotification(this, $"❌ Este Pokémon no es __**legal**__ según los controles de legalidad de __PKHeX__. Tengo prohibido clonar esto. Cancelando trade...");
            TradeProgressChanged?.Invoke(0);

            poke.SendNotification(this, report);

            return (offered, PokeTradeResult.IllegalTrade);
        }

        var clone = offered.Clone();
        if (Hub.Config.Legality.ResetHOMETracker)
            clone.Tracker = 0;

        poke.SendNotification(this, $"**✅ ¡Cloné tu {GameInfo.GetStrings("en").Species[clone.Species]}!**\nAhora presiona B para cancelar tu oferta e intercambiarme un Pokémon que no quieras.");
        Log($"Se clonó un {GameInfo.GetStrings("en").Species[clone.Species]}. Esperando a que el usuario cambie su Pokémon...");
        TradeProgressChanged?.Invoke(72);

        // Separate this out from WaitForPokemonChanged since we compare to old EC from original read.
        var partnerFound = await ReadUntilChanged(TradePartnerOfferedOffset, oldEC, 15_000, 0_200, false, true, token).ConfigureAwait(false);
        if (!partnerFound)
        {
            poke.SendNotification(this, "**¡HEY, CÁMBIALO AHORA O ME VOY!**");

            // They get one more chance.
            partnerFound = await ReadUntilChanged(TradePartnerOfferedOffset, oldEC, 15_000, 0_200, false, true, token).ConfigureAwait(false);
        }

        // Check if the user has cancelled the trade
        if (!await IsInBox(PortalOffset, token).ConfigureAwait(false))
        {
            Log("El usuario canceló el intercambio. Saliendo...");
            TradeProgressChanged?.Invoke(0);

            await ExitTradeToPortal(false, token).ConfigureAwait(false);
            return (offered, PokeTradeResult.TrainerTooSlow);
        }

        var pk2 = await ReadUntilPresent(TradePartnerOfferedOffset, 25_000, 1_000, BoxFormatSlotSize, token).ConfigureAwait(false);
        if (!partnerFound || pk2 is null || SearchUtil.HashByDetails(pk2) == SearchUtil.HashByDetails(offered))
        {
            Log("El compañero de intercambio no cambió su Pokémon.");
            TradeProgressChanged?.Invoke(0);

            await ExitTradeToPortal(false, token).ConfigureAwait(false);
            return (offered, PokeTradeResult.TrainerTooSlow);
        }

        await Click(A, 0_800, token).ConfigureAwait(false);
        await SetBoxPokemonAbsolute(BoxStartOffset, clone, token, sav).ConfigureAwait(false);

        return (clone, PokeTradeResult.Success);
    }


    private async Task<(PK9 toSend, PokeTradeResult check)> HandleFixOT(SAV9SV sav, PokeTradeDetail<PK9> poke, PK9 offered, PartnerDataHolder partnerID, CancellationToken token)
    {
        if (Hub.Config.Discord.ReturnPKMs)
            poke.SendNotification(this, offered, $"Aquí tienes lo que me mostraste - {GameInfo.GetStrings("en").Species[offered.Species]}");

        var adOT = TradeExtensions<PK9>.HasAdName(offered, out _);
        var laInit = new LegalityAnalysis(offered);
        if (!adOT && laInit.Valid)
        {
            poke.SendNotification(this, "✅ No se detectó anuncio en el Apodo o el OT, y el Pokémon es legal. Saliendo del intercambio.");
            TradeProgressChanged?.Invoke(0);

            return (offered, PokeTradeResult.TrainerRequestBad);
        }

        var clone = (PK9)offered.Clone();
        if (Hub.Config.Legality.ResetHOMETracker)
            clone.Tracker = 0;

        string shiny = string.Empty;
        if (!TradeExtensions<PK9>.ShinyLockCheck(offered.Species, TradeExtensions<PK9>.FormOutput(offered.Species, offered.Form, out _), $"{(Ball)offered.Ball}"))
            shiny = $"\nShiny: {(offered.ShinyXor == 0 ? "Square" : offered.IsShiny ? "Star" : "No")}";
        else shiny = "\nShiny: No";

        var name = partnerID.TrainerName;
        var ball = $"\n{(Ball)offered.Ball}";
        var extraInfo = $"OT: {name}{ball}{shiny}";
        var set = ShowdownParsing.GetShowdownText(offered).Split('\n').ToList();
        set.Remove(set.Find(x => x.Contains("Shiny")) ?? "");
        set.InsertRange(1, extraInfo.Split('\n'));

        if (!laInit.Valid)
        {
            Log($"La solicitud de FixOT ha detectado un Pokémon ilegal de {name}: {GameInfo.GetStrings("en").Species[offered.Species]}");
            var report = laInit.Report();
            Log(laInit.Report());
            poke.SendNotification(this, $"⚠️ **El Pokémon mostrado no es legal. Intentando regenerarlo...**\n\n```{report}```");
            if (DumpSetting.Dump)
                DumpPokemon(DumpSetting.DumpFolder, "hacked", offered);
        }

        if (clone.FatefulEncounter)
        {
            clone.SetDefaultNickname(laInit);
            var info = new SimpleTrainerInfo { Gender = clone.OriginalTrainerGender, Language = clone.Language, OT = name, TID16 = clone.TID16, SID16 = clone.SID16, Generation = 9 };
            var mg = EncounterEvent.GetAllEvents().Where(x => x.Species == clone.Species && x.Form == clone.Form && x.IsShiny == clone.IsShiny && x.OriginalTrainerName == clone.OriginalTrainerName).ToList();
            if (mg.Count > 0)
                clone = TradeExtensions<PK9>.CherishHandler(mg.First(), info);
            else clone = (PK9)sav.GetLegal(AutoLegalityWrapper.GetTemplate(new ShowdownSet(string.Join("\n", set))), out _);
        }
        else
        {
            clone = (PK9)sav.GetLegal(AutoLegalityWrapper.GetTemplate(new ShowdownSet(string.Join("\n", set))), out _);
        }

        var la = new LegalityAnalysis(clone);
        clone = (PK9)TradeExtensions<PK9>.TrashBytes(clone, la);
        clone.ResetPartyStats();

        la = new LegalityAnalysis(clone);
        if (!la.Valid)
        {
            poke.SendNotification(this, $"⚠️ Este Pokémon no es __**legal**__ según los controles de legalidad de __PKHeX__. No pude arreglar esto. Cancelando trade...");
            TradeProgressChanged?.Invoke(0);

            return (clone, PokeTradeResult.IllegalTrade);
        }

        TradeExtensions<PK9>.HasAdName(offered, out string detectedAd);
        poke.SendNotification(this, $"{(!laInit.Valid ? "**Legalizado" : "**Arreglado Apodo/OT para")} {(Species)clone.Species}** (anuncio detectado: {detectedAd})! ¡Ahora confirma el intercambio!");
        Log($"{(!laInit.Valid ? "Legalizado" : "Arreglado Apodo/OT para")} {GameInfo.GetStrings("en").Species[clone.Species]}!");
        TradeProgressChanged?.Invoke(72);

        // Wait for a bit in case trading partner tries to switch out.
        await Task.Delay(2_000, token).ConfigureAwait(false);

        var pk2 = await ReadUntilPresent(TradePartnerOfferedOffset, 15_000, 0_200, BoxFormatSlotSize, token).ConfigureAwait(false);
        bool changed = pk2 is null || pk2.Species != offered.Species || offered.OriginalTrainerName != pk2.OriginalTrainerName;
        if (changed)
        {
            // They get one more chance.
            poke.SendNotification(this, "**¡Ofrece el Pokémon que mostraste originalmente o me voy!**");

            var timer = 10_000;
            while (changed)
            {
                pk2 = await ReadUntilPresent(TradePartnerOfferedOffset, 2_000, 0_500, BoxFormatSlotSize, token).ConfigureAwait(false);
                changed = pk2 == null || clone.Species != pk2.Species || offered.OriginalTrainerName != pk2.OriginalTrainerName;
                await Task.Delay(1_000, token).ConfigureAwait(false);
                timer -= 1_000;

                if (timer <= 0)
                    break;
            }
        }

        if (changed)
        {
            poke.SendNotification(this, "⚠️ El Pokémon fue cambiado y no se volvió a poner el original. Saliendo del intercambio.");
            Log("El compañero de intercambio no quiso entregar su Pokémon con anuncio.");
            TradeProgressChanged?.Invoke(0);

            return (offered, PokeTradeResult.TrainerTooSlow);
        }

        await Click(A, 0_800, token).ConfigureAwait(false);
        await SetBoxPokemonAbsolute(BoxStartOffset, clone, token, sav).ConfigureAwait(false);

        return (clone, PokeTradeResult.Success);
    }

    private async Task<(PK9 toSend, PokeTradeResult check)> HandleRandomLedy(SAV9SV sav, PokeTradeDetail<PK9> poke, PK9 offered, PK9 toSend, PartnerDataHolder partner, CancellationToken token)
    {
        // Allow the trade partner to do a Ledy swap.
        var config = Hub.Config.Distribution;
        var trade = Hub.Ledy.GetLedyTrade(offered, partner.TrainerOnlineID, config.LedySpecies);
        if (trade != null)
        {
            if (trade.Type == LedyResponseType.AbuseDetected)
            {
                var msg = $"⚠️ Se detectó que {partner.TrainerName} ha abusado de los intercambios de Ledy.";
                if (AbuseSettings.EchoNintendoOnlineIDLedy)
                    msg += $"\nID: {partner.TrainerOnlineID}";
                if (!string.IsNullOrWhiteSpace(AbuseSettings.LedyAbuseEchoMention))
                    msg = $"{AbuseSettings.LedyAbuseEchoMention} {msg}";
                EchoUtil.Echo(msg);

                return (toSend, PokeTradeResult.SuspiciousActivity);
            }

            toSend = trade.Receive;
            poke.TradeData = toSend;

            poke.SendNotification(this, "✅ Inyectando el Pokémon solicitado.");
            await SetBoxPokemonAbsolute(BoxStartOffset, toSend, token, sav).ConfigureAwait(false);
        }
        else if (config.LedyQuitIfNoMatch)
        {
            var nickname = offered.IsNicknamed ? $" (Apodo: \"{offered.Nickname}\")" : string.Empty;
            poke.SendNotification(this, $"⚠️ No se encontró coincidencia para el {GameInfo.GetStrings("en").Species[offered.Species]} ofrecido{nickname}.");
            return (toSend, PokeTradeResult.TrainerRequestBad);
        }

        return (toSend, PokeTradeResult.Success);
    }

    // These don't change per session and we access them frequently, so set these each time we start.
    private async Task InitializeSessionOffsets(CancellationToken token)
    {
        Log("Guardando en caché los offsets de la sesión...");
        BoxStartOffset = await SwitchConnection.PointerAll(Offsets.BoxStartPokemonPointer, token).ConfigureAwait(false);
        OverworldOffset = await SwitchConnection.PointerAll(Offsets.OverworldPointer, token).ConfigureAwait(false);
        PortalOffset = await SwitchConnection.PointerAll(Offsets.PortalBoxStatusPointer, token).ConfigureAwait(false);
        ConnectedOffset = await SwitchConnection.PointerAll(Offsets.IsConnectedPointer, token).ConfigureAwait(false);
        TradePartnerNIDOffset = await SwitchConnection.PointerAll(Offsets.LinkTradePartnerNIDPointer, token).ConfigureAwait(false);
    }

    private async Task InnerLoop(SAV9SV sav, CancellationToken token)
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

    private async Task<(PK9 toSend, PokeTradeResult check)> JustInject(SAV9SV sav, PK9 offered, CancellationToken token)
    {
        await Click(A, 0_800, token).ConfigureAwait(false);
        await SetBoxPokemonAbsolute(BoxStartOffset, offered, token, sav).ConfigureAwait(false);

        for (int i = 0; i < 5; i++)
            await Click(A, 0_500, token).ConfigureAwait(false);

        return (offered, PokeTradeResult.Success);
    }

    private void OnConnectionError(Exception ex)
    {
        ConnectionError?.Invoke(this, ex);
    }

    private void OnConnectionSuccess()
    {
        ConnectionSuccess?.Invoke(this, EventArgs.Empty);
    }

    private async Task<PokeTradeResult> PerformBatchTrade(SAV9SV sav, PokeTradeDetail<PK9> poke, CancellationToken token)
    {
        int completedTrades = 0;
        var startingDetail = poke;
        var originalTrainerID = startingDetail.Trainer.ID;

        var tradesToProcess = poke.BatchTrades ?? [poke.TradeData];
        var totalBatchTrades = tradesToProcess.Count;

        // Cache trade partner info after first successful connection
        TradeMyStatus? cachedTradePartnerInfo = null;

        void SendCollectedPokemonAndCleanup()
        {
            var allReceived = BatchTracker.GetReceivedPokemon(originalTrainerID);
            if (allReceived.Count > 0)
            {
                poke.SendNotification(this, $"Te enviaré los {allReceived.Count} Pokémon que me intercambiaste antes de la interrupción.");

                Log($"Devolviendo {allReceived.Count} Pokémon al entrenador {originalTrainerID}.");

                // Send each Pokemon directly instead of calling TradeFinished
                for (int j = 0; j < allReceived.Count; j++)
                {
                    var pokemon = allReceived[j];
                    var speciesName = SpeciesName.GetSpeciesName(pokemon.Species, 2);
                    Log($"  - Devolviendo: {speciesName} (Checksum: {pokemon.Checksum:X8})");

                    // Send the Pokemon directly to the notifier
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
            Hub.Queues.Info.Remove(new TradeEntry<PK9>(poke, originalTrainerID, PokeRoutineType.Batch, poke.Trainer.TrainerName, poke.UniqueTradeID));
        }

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
            }
            else
            {
                // Subsequent trades - we're already in the trade screen
                // FIRST: Prepare the Pokemon BEFORE allowing user to offer
                poke.SendNotification(this, $"✅ Intercambio {completedTrades} completado. **NO OFREZCAS TODAVÍA** - Preparando tu próximo Pokémon ({completedTrades + 1}/{totalBatchTrades})...");

                // Wait for trade animation to fully complete
                await Task.Delay(5_000, token).ConfigureAwait(false);

                // Prepare the next Pokemon with AutoOT if needed
                if (toSend.Species != 0)
                {
                    if (Hub.Config.Legality.UseTradePartnerInfo && !poke.IgnoreAutoOT && cachedTradePartnerInfo != null)
                    {
                        // Preserve explicitly requested language through AutoOT
                        var originalLanguage = toSend.Language;
                        var configLanguage = (int)Hub.Config.Legality.GenerateLanguage;
                        bool hasExplicitLanguage = originalLanguage != configLanguage && originalLanguage >= 1 && originalLanguage <= 12;

                        toSend = await ApplyAutoOT(toSend, cachedTradePartnerInfo, sav, token);

                        // Restore explicitly requested language if it was changed by AutoOT
                        if (hasExplicitLanguage && toSend.Language != originalLanguage)
                        {
                            toSend.Language = originalLanguage;
                            toSend.RefreshChecksum();
                        }

                        tradesToProcess[currentTradeIndex] = toSend; // Update the list
                    }
                    await SetBoxPokemonAbsolute(BoxStartOffset, toSend, token, sav).ConfigureAwait(false);
                }

                // Give time for the Pokemon to be properly set
                await Task.Delay(1_000, token).ConfigureAwait(false);

                // NOW tell the user they can offer
                poke.SendNotification(this, $"**¡Listo!** Ya puedes ofrecer tu Pokémon para el intercambio {currentTradeIndex + 1}/{totalBatchTrades}.");

                // Store the last offered state before allowing new offers
                lastOffered = await SwitchConnection.ReadBytesAbsoluteAsync(TradePartnerOfferedOffset, 8, token).ConfigureAwait(false);

                // Additional delay to ensure we're ready to detect offers
                await Task.Delay(5_000, token).ConfigureAwait(false);
            }

            // For first trade only - search for partner
            if (currentTradeIndex == 0)
            {
                await Click(A, 0_500, token).ConfigureAwait(false);
                await Click(A, 0_500, token).ConfigureAwait(false);

                await ClearTradePartnerNID(TradePartnerNIDOffset, token).ConfigureAwait(false);

                WaitAtBarrierIfApplicable(token);
                await Click(A, 1_000, token).ConfigureAwait(false);

                poke.TradeSearching(this);
                var partnerFound = await WaitForTradePartner(token).ConfigureAwait(false);

                if (token.IsCancellationRequested)
                {
                    StartFromOverworld = true;
                    await ExitTradeToPortal(false, token).ConfigureAwait(false);
                    poke.SendNotification(this, "⚠️ Cancelando los intercambios en lote. La rutina ha sido interrumpida.");
                    TradeProgressChanged?.Invoke(0);

                    SendCollectedPokemonAndCleanup();
                    return PokeTradeResult.RoutineCancel;
                }

                if (!partnerFound)
                {
                    poke.IsProcessing = false;
                    poke.SendNotification(this, "⚠️ No se encontró compañero de intercambio. Cancelando los intercambios en lote.");
                    TradeProgressChanged?.Invoke(0);

                    poke.TradeCanceled(this, PokeTradeResult.NoTrainerFound);
                    SendCollectedPokemonAndCleanup();

                    if (!await RecoverToPortal(token).ConfigureAwait(false))
                    {
                        Log("No se pudo volver al portal.");

                        await RecoverToOverworld(token).ConfigureAwait(false);
                    }
                    return PokeTradeResult.NoTrainerFound;
                }

                var cnt = 0;
                while (!await IsInBox(PortalOffset, token).ConfigureAwait(false))
                {
                    await Task.Delay(0_500, token).ConfigureAwait(false);
                    if (++cnt > 20)
                    {
                        await Click(A, 1_000, token).ConfigureAwait(false);
                        SendCollectedPokemonAndCleanup();

                        if (!await RecoverToPortal(token).ConfigureAwait(false))
                        {
                            Log("No se pudo volver al portal.");

                            await RecoverToOverworld(token).ConfigureAwait(false);
                        }
                        poke.SendNotification(this, "⚠️ No se pudo entrar a la caja de intercambio. Cancelando los intercambios en lote.");
                        TradeProgressChanged?.Invoke(0);

                        return PokeTradeResult.RecoverOpenBox;
                    }
                }
                await Task.Delay(3_000 + Hub.Config.Timings.ExtraTimeOpenBox, token).ConfigureAwait(false);

                // Get trade partner info and verify
                var tradePartnerFullInfo = await GetTradePartnerFullInfo(token).ConfigureAwait(false);
                cachedTradePartnerInfo = tradePartnerFullInfo; // Cache for subsequent trades
                var tradePartner = new TradePartnerSV(tradePartnerFullInfo);
                var trainerNID = await GetTradePartnerNID(TradePartnerNIDOffset, token).ConfigureAwait(false);
                RecordUtil<PokeTradeBotSV>.Record($"Iniciando\t{trainerNID:X16}\t{tradePartner.TrainerName}\t{poke.Trainer.TrainerName}\t{poke.Trainer.ID}\t{poke.ID}\t{toSend.EncryptionConstant:X8}");

                var tradeCodeStorage = new TradeCodeStorage();
                var existingTradeDetails = tradeCodeStorage.GetTradeDetails(poke.Trainer.ID);

                bool shouldUpdateOT = existingTradeDetails?.OT != tradePartner.TrainerName;
                bool shouldUpdateTID = existingTradeDetails?.TID != int.Parse(tradePartner.TID7);
                bool shouldUpdateSID = existingTradeDetails?.SID != int.Parse(tradePartner.SID7);

                if (shouldUpdateOT || shouldUpdateTID || shouldUpdateSID)
                {
                    string? ot = shouldUpdateOT ? tradePartner.TrainerName : existingTradeDetails?.OT;
                    int? tid = shouldUpdateTID ? int.Parse(tradePartner.TID7) : existingTradeDetails?.TID;
                    int? sid = shouldUpdateSID ? int.Parse(tradePartner.SID7) : existingTradeDetails?.SID;

                    if (ot != null && tid.HasValue && sid.HasValue)
                    {
                        tradeCodeStorage.UpdateTradeDetails(poke.Trainer.ID, ot, tid.Value, sid.Value);
                    }
                    else
                    {
                        Log("El OT, TID o SID es nulo. Omitiendo UpdateTradeDetails.");
                    }
                }

                var partnerCheck = CheckPartnerReputation(this, poke, trainerNID, tradePartner.TrainerName, AbuseSettings, token);
                if (partnerCheck != PokeTradeResult.Success)
                {
                    poke.SendNotification(this, "La verificación del compañero de intercambio falló. Cancelando los intercambios en lote.");
                    TradeProgressChanged?.Invoke(0);

                    SendCollectedPokemonAndCleanup();
                    await Click(A, 1_000, token).ConfigureAwait(false);
                    await ExitTradeToPortal(false, token).ConfigureAwait(false);
                    return partnerCheck;
                }

                var tradeOffered = await ReadUntilChanged(TradePartnerOfferedOffset, lastOffered, 10_000, 0_500, false, true, token).ConfigureAwait(false);
                if (!tradeOffered)
                {
                    poke.SendNotification(this, "El compañero de intercambio tardó demasiado. Cancelando los intercambios en lote.");
                    TradeProgressChanged?.Invoke(0);

                    SendCollectedPokemonAndCleanup();
                    await ExitTradeToPortal(false, token).ConfigureAwait(false);
                    return PokeTradeResult.TrainerTooSlow;
                }

                Log($"Compañero de Intercambio encontrado: {tradePartner.TrainerName}-{tradePartner.TID7} (ID: {trainerNID})");
                TradeProgressChanged?.Invoke(64);

                poke.SendNotification(this, $"Entrenador encontrado: **{tradePartner.TrainerName}**.\n\n▼\n Aqui esta tu Informacion\n **TID**: __{tradePartner.TID7}__\n **SID**: __{tradePartner.SID7}__\n▲\n\n Esperando por un __Pokémon__...");

                // Apply AutoOT for first trade if needed
                if (Hub.Config.Legality.UseTradePartnerInfo && !poke.IgnoreAutoOT)
                {
                    // Preserve explicitly requested language through AutoOT
                    var originalLanguage = toSend.Language;
                    var configLanguage = (int)Hub.Config.Legality.GenerateLanguage;
                    bool hasExplicitLanguage = originalLanguage != configLanguage && originalLanguage >= 1 && originalLanguage <= 12;

                    toSend = await ApplyAutoOT(toSend, tradePartnerFullInfo, sav, token).ConfigureAwait(false);

                    // Restore explicitly requested language if it was changed by AutoOT
                    if (hasExplicitLanguage && toSend.Language != originalLanguage)
                    {
                        toSend.Language = originalLanguage;
                        toSend.RefreshChecksum();
                    }

                    poke.TradeData = toSend;
                    if (toSend.Species != 0)
                        await SetBoxPokemonAbsolute(BoxStartOffset, toSend, token, sav).ConfigureAwait(false);
                }
            }

            // Wait for user to offer a Pokemon
            if (currentTradeIndex == 0)
            {
                poke.SendNotification(this, $"Por favor ofrece tu Pokémon para el intercambio 1/{totalBatchTrades}.");
            }

            var offered = await ReadUntilPresent(TradePartnerOfferedOffset, 25_000, 1_000, BoxFormatSlotSize, token).ConfigureAwait(false);
            var oldEC = await SwitchConnection.ReadBytesAbsoluteAsync(TradePartnerOfferedOffset, 8, token).ConfigureAwait(false);
            if (offered == null || offered.Species == 0 || !offered.ChecksumValid)
            {
                Log("El intercambio terminó porque no se ofreció un Pokémon válido.");

                poke.SendNotification(this, $"⚠️ Pokémon inválido ofrecido para el intercambio {currentTradeIndex + 1}/{totalBatchTrades}. Cancelando los intercambios restantes.");
                TradeProgressChanged?.Invoke(0);

                SendCollectedPokemonAndCleanup();
                await ExitTradeToPortal(false, token).ConfigureAwait(false);
                return PokeTradeResult.TrainerTooSlow;
            }

            // Get trade partner info for subsequent trades
            var trainer = new PartnerDataHolder(0, "", "");
            if (cachedTradePartnerInfo != null)
            {
                var tradePartner = new TradePartnerSV(cachedTradePartnerInfo);
                trainer = new PartnerDataHolder(0, tradePartner.TrainerName, tradePartner.TID7);
            }

            PokeTradeResult update;
            (toSend, update) = await GetEntityToSend(sav, poke, offered, oldEC, toSend, trainer, null, token).ConfigureAwait(false);
            if (update != PokeTradeResult.Success)
            {
                poke.SendNotification(this, $"⚠️ La verificación de actualización falló para el intercambio {currentTradeIndex + 1}/{totalBatchTrades}. Cancelando los intercambios restantes.");
                TradeProgressChanged?.Invoke(0);

                SendCollectedPokemonAndCleanup();
                await ExitTradeToPortal(false, token).ConfigureAwait(false);
                return update;
            }

            Log($"Confirmando intercambio {currentTradeIndex + 1}/{totalBatchTrades}.");
            TradeProgressChanged?.Invoke(86);

            var tradeResult = await ConfirmAndStartTrading(poke, token).ConfigureAwait(false);
            if (tradeResult != PokeTradeResult.Success)
            {
                poke.SendNotification(this, $"⚠️ La confirmación del intercambio falló para el intercambio {currentTradeIndex + 1}/{totalBatchTrades}. Cancelando los intercambios restantes.");

                SendCollectedPokemonAndCleanup();
                await ExitTradeToPortal(false, token).ConfigureAwait(false);
                return tradeResult;
            }

            if (token.IsCancellationRequested)
            {
                StartFromOverworld = true;
                poke.SendNotification(this, "⚠️ Cancelando los intercambios en lote. La rutina ha sido interrumpida.");
                TradeProgressChanged?.Invoke(0);

                SendCollectedPokemonAndCleanup();
                await ExitTradeToPortal(false, token).ConfigureAwait(false);
                return PokeTradeResult.RoutineCancel;
            }

            var received = await ReadPokemon(BoxStartOffset, BoxFormatSlotSize, token).ConfigureAwait(false);
            if (SearchUtil.HashByDetails(received) == SearchUtil.HashByDetails(toSend) && received.Checksum == toSend.Checksum)
            {
                poke.SendNotification(this, $"⚠️ El compañero no completó el intercambio {currentTradeIndex + 1}/{totalBatchTrades}. Cancelando los intercambios restantes.");
                TradeProgressChanged?.Invoke(0);

                SendCollectedPokemonAndCleanup();
                await ExitTradeToPortal(false, token).ConfigureAwait(false);
                return PokeTradeResult.TrainerTooSlow;
            }

            UpdateCountsAndExport(poke, received, toSend);

            // Get the trainer NID for logging
            var logTrainerNID = currentTradeIndex == 0 ? await GetTradePartnerNID(TradePartnerNIDOffset, token).ConfigureAwait(false) : 0;
            LogSuccessfulTrades(poke, logTrainerNID, trainer.TrainerName);

            BatchTracker.AddReceivedPokemon(originalTrainerID, received);
            completedTrades = currentTradeIndex + 1;
            Log($"Añadido el Pokémon recibido {received.Species} (Checksum: {received.Checksum:X8}) al registro del lote para el entrenador {originalTrainerID} (Intercambio {completedTrades}/{totalBatchTrades})");

            if (completedTrades == totalBatchTrades)
            {
                // Get all collected Pokemon before cleaning anything up
                var allReceived = BatchTracker.GetReceivedPokemon(originalTrainerID);
                Log($"Intercambios en lote completados. Se encontraron {allReceived.Count} Pokémon almacenados para el entrenador {originalTrainerID}");
                TradeProgressChanged?.Invoke(100);

                // First send notification that trades are complete
                poke.SendNotification(this, "⚠️ ¡Todos los intercambios en lote han sido completados! ¡Gracias por intercambiar!");

                // Send back all received Pokemon if ReturnPKMs is enabled
                if (Hub.Config.Discord.ReturnPKMs && allReceived.Count > 0)
                {
                    poke.SendNotification(this, $"✅ Aquí tienes los {allReceived.Count} Pokémon que me intercambiaste:");

                    // Send each Pokemon directly instead of calling TradeFinished
                    for (int j = 0; j < allReceived.Count; j++)
                    {
                        var pokemon = allReceived[j];
                        Log($"  - Devolviendo: {GameInfo.GetStrings("en").Species[offered.Species]} (Checksum: {pokemon.Checksum:X8})");

                        // Send the Pokemon directly to the notifier
                        poke.SendNotification(this, pokemon, $"Pokémon que me intercambiaste: {GameInfo.GetStrings("en").Species[offered.Species]}");
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
                await ExitTradeToPortal(false, token).ConfigureAwait(false);
                poke.IsProcessing = false;
                break;
            }

            // Store the last offered Pokemon before moving to next trade
            lastOffered = await SwitchConnection.ReadBytesAbsoluteAsync(TradePartnerOfferedOffset, 8, token).ConfigureAwait(false);
        }

        // Ensure we exit properly even if the loop breaks unexpectedly
        await ExitTradeToPortal(false, token).ConfigureAwait(false);
        poke.IsProcessing = false;
        return PokeTradeResult.Success;
    }

    private async Task PerformTrade(SAV9SV sav, PokeTradeDetail<PK9> detail, PokeRoutineType type, uint priority, CancellationToken token)
    {
        PokeTradeResult result;
        try
        {
            // All trades go through PerformLinkCodeTrade which will handle both regular and batch trades
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
    }

    private async Task<PokeTradeResult> PerformLinkCodeTrade(SAV9SV sav, PokeTradeDetail<PK9> poke, CancellationToken token)
    {
        // Update Barrier Settings
        UpdateBarrier(poke.IsSynchronized);
        poke.TradeInitialize(this);

        // Handle connection and portal entry
        if (!await EnsureConnectedAndInPortal(token).ConfigureAwait(false))
        {
            return PokeTradeResult.RecoverStart;
        }

        // Enter Link Trade and code
        if (!await EnterLinkTradeAndCode(poke, poke.Code, token).ConfigureAwait(false))
        {
            return PokeTradeResult.RecoverStart;
        }

        StartFromOverworld = false;

        // Route to appropriate trade handling based on trade type
        if (poke.Type == PokeTradeType.Batch)
            return await PerformBatchTrade(sav, poke, token).ConfigureAwait(false);

        return await PerformNonBatchTrade(sav, poke, token).ConfigureAwait(false);
    }

    private async Task<bool> EnsureConnectedAndInPortal(CancellationToken token)
    {
        // StartFromOverworld can be true on first pass or if something went wrong last trade.
        if (StartFromOverworld && !await IsOnOverworld(OverworldOffset, token).ConfigureAwait(false))
            await RecoverToOverworld(token).ConfigureAwait(false);

        if (!StartFromOverworld)
        {
            // IMPORTANT: Check socket state first before trusting game memory
            // After multiple trades, the socket may be disconnected even if game state shows connected
            // This prevents the "assume connection persists" issue that causes socket read failures
            bool socketAlive = SwitchConnection.Connected;
            bool gameConnected = socketAlive && await IsConnectedOnline(ConnectedOffset, token).ConfigureAwait(false);

            if (!gameConnected)
            {
                if (!socketAlive)
                {
                    Log("Conexión de socket perdida entre intercambios, forzando reconexión...");
                }

                await RecoverToOverworld(token).ConfigureAwait(false);
                if (!await ConnectAndEnterPortal(token).ConfigureAwait(false))
                {
                    await RecoverToOverworld(token).ConfigureAwait(false);
                    return false;
                }
            }
        }
        else if (StartFromOverworld && !await ConnectAndEnterPortal(token).ConfigureAwait(false))
        {
            await RecoverToOverworld(token).ConfigureAwait(false);
            return false;
        }
        return true;
    }

    private async Task<bool> EnterLinkTradeAndCode(PokeTradeDetail<PK9> poke, int code, CancellationToken token)
    {
        // Assumes we're freshly in the Portal and the cursor is over Link Trade.
        Log("Seleccionando Intercambio por Enlace.");
        TradeProgressChanged?.Invoke(24);
        await Click(A, 1_500, token).ConfigureAwait(false);
        // Always clear Link Codes and enter a new one based on the current trade type
        await Click(X, 1_000, token).ConfigureAwait(false);
        await Click(PLUS, 1_000, token).ConfigureAwait(false);
        // Loading code entry
        bool startedEnterCode = false;
        try
        {
            if (poke.Type != PokeTradeType.Random)
            {
                Hub.Config.Stream.StartEnterCode(this);
                startedEnterCode = true;
            }
            await Task.Delay(Hub.Config.Timings.ExtraTimeOpenCodeEntry, token).ConfigureAwait(false);
            Log($"Ingresando código de Intercambio por Enlace: {code:0000 0000}...");
            TradeProgressChanged?.Invoke(36);
            await EnterLinkCode(code, Hub.Config, token).ConfigureAwait(false);
            await Click(PLUS, 3_000, token).ConfigureAwait(false);
            return true;
        }
        finally
        {
            if (startedEnterCode)
                Hub.Config.Stream.EndEnterCode(this);
        }
    }

    private async Task<PokeTradeResult> PerformNonBatchTrade(SAV9SV sav, PokeTradeDetail<PK9> poke, CancellationToken token)
    {
        var toSend = poke.TradeData;
        if (toSend.Species != 0)
            await SetBoxPokemonAbsolute(BoxStartOffset, toSend, token, sav).ConfigureAwait(false);

        // Search for a trade partner for a Link Trade.
        await Click(A, 0_500, token).ConfigureAwait(false);
        await Click(A, 0_500, token).ConfigureAwait(false);

        // Clear it so we can detect it loading.
        await ClearTradePartnerNID(TradePartnerNIDOffset, token).ConfigureAwait(false);

        // Wait for Barrier to trigger all bots simultaneously.
        WaitAtBarrierIfApplicable(token);
        await Click(A, 1_000, token).ConfigureAwait(false);

        // Wait for a Trainer...
        poke.TradeSearching(this);
        var partnerFound = await WaitForTradePartner(token).ConfigureAwait(false);

        if (token.IsCancellationRequested)
        {
            StartFromOverworld = true;
            await ExitTradeToPortal(false, token).ConfigureAwait(false);
            return PokeTradeResult.RoutineCancel;
        }
        if (!partnerFound)
        {
            // Fast-path for no trainer found - handle immediately
            poke.IsProcessing = false;
            poke.SendNotification(this, "⚠️ No se encontró compañero de intercambio. Cancelando el intercambio.");
            TradeProgressChanged?.Invoke(0);

            poke.TradeCanceled(this, PokeTradeResult.NoTrainerFound);

            if (!await RecoverToPortal(token).ConfigureAwait(false))
            {
                Log("No se pudo volver al portal.");

                await RecoverToOverworld(token).ConfigureAwait(false);
            }
            return PokeTradeResult.NoTrainerFound;
        }

        // Wait until we get into the box.
        var cnt = 0;
        while (!await IsInBox(PortalOffset, token).ConfigureAwait(false))
        {
            await Task.Delay(0_500, token).ConfigureAwait(false);
            if (++cnt > 20) // Didn't make it in after 10 seconds.
            {
                await Click(A, 1_000, token).ConfigureAwait(false); // Ensures we dismiss a popup.
                if (!await RecoverToPortal(token).ConfigureAwait(false))
                {
                    Log("No se pudo volver al portal.");
                    await RecoverToOverworld(token).ConfigureAwait(false);
                }
                return PokeTradeResult.RecoverOpenBox;
            }
        }
        await Task.Delay(3_000 + Hub.Config.Timings.ExtraTimeOpenBox, token).ConfigureAwait(false);

        var tradePartnerFullInfo = await GetTradePartnerFullInfo(token).ConfigureAwait(false);
        var tradePartner = new TradePartnerSV(tradePartnerFullInfo);
        var trainerNID = await GetTradePartnerNID(TradePartnerNIDOffset, token).ConfigureAwait(false);
        RecordUtil<PokeTradeBotSV>.Record($"Iniciando\t{trainerNID:X16}\t{tradePartner.TrainerName}\t{poke.Trainer.TrainerName}\t{poke.Trainer.ID}\t{poke.ID}\t{toSend.EncryptionConstant:X8}");
        Log($"Compañero de Intercambio por Enlace encontrado: {tradePartner.TrainerName}-{tradePartner.TID7} (ID: {trainerNID})");
        TradeProgressChanged?.Invoke(64);

        poke.SendNotification(this, $"Entrenador encontrado: **{tradePartner.TrainerName}**.\n\n▼\n Aqui esta tu Informacion\n **TID**: __{tradePartner.TID7}__\n **SID**: __{tradePartner.SID7}__\n▲\n\n Esperando por un __Pokémon__...");

        var tradeCodeStorage = new TradeCodeStorage();
        var existingTradeDetails = tradeCodeStorage.GetTradeDetails(poke.Trainer.ID);

        bool shouldUpdateOT = existingTradeDetails?.OT != tradePartner.TrainerName;
        bool shouldUpdateTID = existingTradeDetails?.TID != int.Parse(tradePartner.TID7);
        bool shouldUpdateSID = existingTradeDetails?.SID != int.Parse(tradePartner.SID7);

        if (shouldUpdateOT || shouldUpdateTID || shouldUpdateSID)
        {
            string? ot = shouldUpdateOT ? tradePartner.TrainerName : existingTradeDetails?.OT;
            int? tid = shouldUpdateTID ? int.Parse(tradePartner.TID7) : existingTradeDetails?.TID;
            int? sid = shouldUpdateSID ? int.Parse(tradePartner.SID7) : existingTradeDetails?.SID;

            if (ot != null && tid.HasValue && sid.HasValue)
            {
                tradeCodeStorage.UpdateTradeDetails(poke.Trainer.ID, ot, tid.Value, sid.Value);
            }
            else
            {
                Log("El OT, TID o SID es nulo. Omitiendo UpdateTradeDetails.");
            }
        }

        var partnerCheck = CheckPartnerReputation(this, poke, trainerNID, tradePartner.TrainerName, AbuseSettings, token);
        if (partnerCheck != PokeTradeResult.Success)
        {
            await Click(A, 1_000, token).ConfigureAwait(false); // Ensures we dismiss a popup.
            await ExitTradeToPortal(false, token).ConfigureAwait(false);
            return partnerCheck;
        }

        // Hard check to verify that the offset changed from the last thing offered from the previous trade.
        // This is because box opening times can vary per person, the offset persists between trades, and can also change offset between trades.
        var tradeOffered = await ReadUntilChanged(TradePartnerOfferedOffset, lastOffered, 10_000, 0_500, false, true, token).ConfigureAwait(false);
        if (!tradeOffered)
        {
            await ExitTradeToPortal(false, token).ConfigureAwait(false);
            return PokeTradeResult.TrainerTooSlow;
        }

        if (poke.Type == PokeTradeType.Dump)
        {
            var result = await ProcessDumpTradeAsync(poke, token).ConfigureAwait(false);
            await ExitTradeToPortal(false, token).ConfigureAwait(false);
            return result;
        }
        if (Hub.Config.Legality.UseTradePartnerInfo && !poke.IgnoreAutoOT)
        {
            // Preserve explicitly requested language through AutoOT
            var originalLanguage = toSend.Language;
            var configLanguage = (int)Hub.Config.Legality.GenerateLanguage;
            bool hasExplicitLanguage = originalLanguage != configLanguage && originalLanguage >= 1 && originalLanguage <= 12;

            toSend = await ApplyAutoOT(toSend, tradePartnerFullInfo, sav, token);

            // Restore explicitly requested language if it was changed by AutoOT
            if (hasExplicitLanguage && toSend.Language != originalLanguage)
            {
                toSend.Language = originalLanguage;
                toSend.RefreshChecksum();
            }
        }
        // Wait for user input...
        var offered = await ReadUntilPresent(TradePartnerOfferedOffset, 25_000, 1_000, BoxFormatSlotSize, token).ConfigureAwait(false);
        var oldEC = await SwitchConnection.ReadBytesAbsoluteAsync(TradePartnerOfferedOffset, 8, token).ConfigureAwait(false);
        if (offered == null || offered.Species == 0 || !offered.ChecksumValid)
        {
            Log("El intercambio terminó porque no se ofreció un Pokémon válido.");
            TradeProgressChanged?.Invoke(0);

            await ExitTradeToPortal(false, token).ConfigureAwait(false);
            return PokeTradeResult.TrainerTooSlow;
        }

        SpecialTradeType itemReq = SpecialTradeType.None;
        if (poke.Type == PokeTradeType.Seed)
            itemReq = CheckItemRequest(ref offered, this, poke, tradePartner.TrainerName, sav);
        if (itemReq == SpecialTradeType.FailReturn)
            return PokeTradeResult.IllegalTrade;

        if (poke.Type == PokeTradeType.Seed && itemReq == SpecialTradeType.None)
        {
            // Immediately exit, we aren't trading anything.
            poke.SendNotification(this, "⚠️ ¡Sin objeto equipado o solicitud válida! Cancelando este intercambio.");
            TradeProgressChanged?.Invoke(0);

            await ExitTradeToPortal(true, token).ConfigureAwait(false);
            return PokeTradeResult.TrainerRequestBad;
        }

        var trainer = new PartnerDataHolder(0, tradePartner.TrainerName, tradePartner.TID7);
        PokeTradeResult update;
        (toSend, update) = await GetEntityToSend(sav, poke, offered, oldEC, toSend, trainer, poke.Type == PokeTradeType.Seed ? itemReq : null, token).ConfigureAwait(false);
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
            Log("Intercambio cancelado porque el entrenador ofreció un Pokémon que evolucionaría al intercambiarse.");
            poke.SendNotification(this, "⚠️ Intercambio cancelado. No puedes intercambiar un Pokémon que vaya a evolucionar. Para evitar esto, dale a tu Pokémon una Piedra Eterna o intercambia un Pokémon diferente.");
            await ExitTradeToPortal(false, token).ConfigureAwait(false);
            return PokeTradeResult.TradeEvolveNotAllowed;
        }

        Log("Confirmando intercambio.");
        TradeProgressChanged?.Invoke(86);

        var tradeResult = await ConfirmAndStartTrading(poke, token).ConfigureAwait(false);
        if (tradeResult != PokeTradeResult.Success)
        {
            await ExitTradeToPortal(false, token).ConfigureAwait(false);
            return tradeResult;
        }

        if (token.IsCancellationRequested)
        {
            StartFromOverworld = true;
            await ExitTradeToPortal(false, token).ConfigureAwait(false);
            return PokeTradeResult.RoutineCancel;
        }

        // Trade was Successful!
        var received = await ReadPokemon(BoxStartOffset, BoxFormatSlotSize, token).ConfigureAwait(false);

        // Pokémon in b1s1 is same as the one they were supposed to receive (was never sent).
        if (SearchUtil.HashByDetails(received) == SearchUtil.HashByDetails(toSend) && received.Checksum == toSend.Checksum)
        {
            Log("El usuario no completó el intercambio.");
            TradeProgressChanged?.Invoke(0);

            await ExitTradeToPortal(false, token).ConfigureAwait(false);
            return PokeTradeResult.TrainerTooSlow;
        }

        // As long as we got rid of our inject in b1s1, assume the trade went through.
        Log("El usuario completó el intercambio.");
        TradeProgressChanged?.Invoke(100);


        poke.TradeFinished(this, received);

        // Only log if we completed the trade.
        UpdateCountsAndExport(poke, received, toSend);

        // Log for Trade Abuse tracking.
        LogSuccessfulTrades(poke, trainerNID, tradePartner.TrainerName);

        // Sometimes they offered another mon, so store that immediately upon leaving Union Room.
        lastOffered = await SwitchConnection.ReadBytesAbsoluteAsync(TradePartnerOfferedOffset, 8, token).ConfigureAwait(false);

        await ExitTradeToPortal(false, token).ConfigureAwait(false);
        return PokeTradeResult.Success;
    }

    private async Task HandleAbortedBatchTrade(PokeTradeDetail<PK9> detail, PokeRoutineType type, uint priority, PokeTradeResult result, CancellationToken token)
    {
        detail.IsProcessing = false;

        // Always remove from UsersInQueue on abort
        Hub.Queues.Info.Remove(new TradeEntry<PK9>(detail, detail.Trainer.ID, type, detail.Trainer.TrainerName, detail.UniqueTradeID));

        if (detail.TotalBatchTrades > 1)
        {
            // Release the batch claim on failure
            BatchTracker.ReleaseBatch(detail.Trainer.ID, detail.UniqueTradeID);

            if (result.ShouldAttemptRetry() && detail.Type != PokeTradeType.Random && !detail.IsRetry)
            {
                detail.IsRetry = true;
                Hub.Queues.Enqueue(type, detail, Math.Min(priority, PokeTradePriorities.Tier2));
                detail.SendNotification(this, "⚠️ ¡Ups! Algo ocurrió durante tu intercambio en lote. Te volveré a poner en la cola para otro intento.");
            }
            else
            {
                detail.SendNotification(this, $"⚠️ El intercambio en lote falló: {result}");
                TradeProgressChanged?.Invoke(0);

                detail.TradeCanceled(this, result);
                await ExitTradeToPortal(false, token).ConfigureAwait(false);
            }
        }
        else
        {
            HandleAbortedTrade(detail, type, priority, result);
        }
    }

    private async Task<PokeTradeResult> ProcessDumpTradeAsync(PokeTradeDetail<PK9> detail, CancellationToken token)
    {
        int ctr = 0;
        var time = TimeSpan.FromSeconds(Hub.Config.Trade.TradeConfiguration.MaxDumpTradeTime);
        var start = DateTime.Now;

        var pkprev = new PK9();
        var bctr = 0;
        while (ctr < Hub.Config.Trade.TradeConfiguration.MaxDumpsPerTrade && DateTime.Now - start < time)
        {
            if (!await IsInBox(PortalOffset, token).ConfigureAwait(false))
                break;
            if (bctr++ % 3 == 0)
                await Click(B, 0_100, token).ConfigureAwait(false);

            // Wait for user input... Needs to be different from the previously offered Pokémon.
            var pk = await ReadUntilPresent(TradePartnerOfferedOffset, 3_000, 0_050, BoxFormatSlotSize, token).ConfigureAwait(false);
            if (pk == null || pk.Species == 0 || !pk.ChecksumValid || SearchUtil.HashByDetails(pk) == SearchUtil.HashByDetails(pkprev))
                continue;

            // Save the new Pokémon for comparison next round.
            pkprev = pk;

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
            var msg = Hub.Config.Trade.TradeConfiguration.DumpTradeLegalityCheck ? verbose : $"Archivo {ctr}";

            // Extra information about trainer data for people requesting with their own trainer data.
            var ot = pk.OriginalTrainerName;
            var ot_gender = pk.OriginalTrainerGender == 0 ? "Masculino" : "Femenino";
            var tid = pk.GetDisplayTID().ToString(pk.GetTrainerIDFormat().GetTrainerIDFormatStringTID());
            var sid = pk.GetDisplaySID().ToString(pk.GetTrainerIDFormat().GetTrainerIDFormatStringSID());
            msg += $"\n**Datos del Entrenador**\n```OT: {ot}\nGéneroOT: {ot_gender}\nTID: {tid}\nSID: {sid}```";

            // Extra information for shiny eggs, because of people dumping to skip hatching.
            var eggstring = pk.IsEgg ? "Huevo " : string.Empty;
            msg += pk.IsShiny ? $"\n**¡Este Pokémon {eggstring}es shiny!**" : string.Empty;

            detail.SendNotification(this, pk, msg);
        }

        Log($"Bucle de volcado finalizado después de procesar {ctr} Pokémon.");
        TradeProgressChanged?.Invoke(100);

        if (ctr == 0)
            return PokeTradeResult.TrainerTooSlow;

        TradeSettings.CountStatsSettings.AddCompletedDumps();
        detail.Notifier.SendNotification(this, detail, $"Se volcaron {ctr} Pokémon.");
        detail.Notifier.TradeFinished(this, detail, detail.TradeData); // blank PK9
        return PokeTradeResult.Success;
    }

    // If we can't manually recover to overworld, reset the game.
    // Try to avoid pressing A which can put us back in the portal with the long load time.
    private async Task<bool> RecoverToOverworld(CancellationToken token)
    {
        if (await IsOnOverworld(OverworldOffset, token).ConfigureAwait(false))
            return true;

        Log("Intentando recuperar el mundo exterior.");

        var attempts = 0;
        while (!await IsOnOverworld(OverworldOffset, token).ConfigureAwait(false))
        {
            attempts++;
            if (attempts >= 30)
                break;

            await Click(B, 1_000, token).ConfigureAwait(false);
            if (await IsOnOverworld(OverworldOffset, token).ConfigureAwait(false))
                break;

            await Click(B, 1_000, token).ConfigureAwait(false);
            if (await IsOnOverworld(OverworldOffset, token).ConfigureAwait(false))
                break;

            if (await IsInBox(PortalOffset, token).ConfigureAwait(false))
                await Click(A, 1_000, token).ConfigureAwait(false);
        }

        // We didn't make it for some reason.
        if (!await IsOnOverworld(OverworldOffset, token).ConfigureAwait(false))
        {
            Log("No se pudo recuperar el mundo exterior, reiniciando el juego.");

            await RestartGameSV(token).ConfigureAwait(false);
        }
        await Task.Delay(1_000, token).ConfigureAwait(false);

        // Force the bot to go through all the motions again on its first pass.
        StartFromOverworld = true;
        return true;
    }

    // If we didn't find a trainer, we're still in the portal but there can be
    // different numbers of pop-ups we have to dismiss to get back to when we can trade.
    // Rather than resetting to overworld, try to reset out of portal and immediately go back in.
    private async Task<bool> RecoverToPortal(CancellationToken token)
    {
        Log("Reorientando al Poké Portal.");
        var attempts = 0;
        while (await IsInPokePortal(PortalOffset, token).ConfigureAwait(false))
        {
            await Click(B, 2_500, token).ConfigureAwait(false);
            if (++attempts >= 30)
            {
                Log("No se pudo recuperar el Poké Portal.");

                return false;
            }
        }

        // Should be in the X menu hovered over Poké Portal.
        await Click(A, 1_000, token).ConfigureAwait(false);

        return await SetUpPortalCursor(token).ConfigureAwait(false);
    }

    private async Task RestartGameSV(CancellationToken token)
    {
        await ReOpenGame(Hub.Config, token).ConfigureAwait(false);
        await InitializeSessionOffsets(token).ConfigureAwait(false);
    }

    // Waits for the Portal to load (slow) and then moves the cursor down to Link Trade.
    private async Task<bool> SetUpPortalCursor(CancellationToken token)
    {
        // Wait for the portal to load.
        var attempts = 0;
        while (!await IsInPokePortal(PortalOffset, token).ConfigureAwait(false))
        {
            await Task.Delay(0_500, token).ConfigureAwait(false);
            if (++attempts > 20)
            {
                Log("No se pudo cargar el Poké Portal.");

                return false;
            }
        }
        await Task.Delay(2_000 + Hub.Config.Timings.ExtraTimeLoadPortal, token).ConfigureAwait(false);

        // Connect online if not already.
        if (!await ConnectToOnline(Hub.Config, token).ConfigureAwait(false))
        {
            Log("No se pudo conectar en línea.");

            return false; // Failed, either due to connection or softban.
        }

        // Handle the news popping up.
        if (await SwitchConnection.IsProgramRunning(LibAppletWeID, token).ConfigureAwait(false))
        {
            Log("Noticias detectadas, se cerrarán una vez carguen.");
            await Task.Delay(5_000, token).ConfigureAwait(false);
            await Click(B, 2_000 + Hub.Config.Timings.ExtraTimeLoadPortal, token).ConfigureAwait(false);
        }

        Log("Ajustando el cursor en el Portal.");

        // Move down to Link Trade.
        await Click(DDOWN, 0_300, token).ConfigureAwait(false);
        await Click(DDOWN, 0_300, token).ConfigureAwait(false);
        return true;
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

    private void UpdateCountsAndExport(PokeTradeDetail<PK9> poke, PK9 received, PK9 toSend)
    {
        var counts = TradeSettings;
        if (poke.Type == PokeTradeType.Random)
            counts.CountStatsSettings.AddCompletedDistribution();
        else if (poke.Type == PokeTradeType.Clone)
            counts.CountStatsSettings.AddCompletedClones();
        else if (poke.Type == PokeTradeType.FixOT)
            counts.CountStatsSettings.AddCompletedFixOTs();
        else
            counts.CountStatsSettings.AddCompletedTrade();

        if (DumpSetting.Dump && !string.IsNullOrEmpty(DumpSetting.DumpFolder))
        {
            var subfolder = poke.Type.ToString().ToLower();
            var service = poke.Notifier.GetType().ToString().ToLower();
            var tradedFolder = service.Contains("twitch") ? Path.Combine("traded", "twitch") : service.Contains("discord") ? Path.Combine("traded", "discord") : "traded";
            DumpPokemon(DumpSetting.DumpFolder, subfolder, received); // recibido por el bot
            if (poke.Type is PokeTradeType.Specific or PokeTradeType.Clone)
                DumpPokemon(DumpSetting.DumpFolder, tradedFolder, toSend); // enviado al compañero
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
        Log($"La sincronización de la barrera agotó el tiempo después de {timeoutAfter} segundos. Continuando.");

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

        return Task.Delay(1_000, token);
    }
}
