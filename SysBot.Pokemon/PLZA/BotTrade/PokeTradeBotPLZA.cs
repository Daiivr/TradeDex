using PKHeX.Core;
using PKHeX.Core.Searching;
using SysBot.Base;
using SysBot.Base.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using static SysBot.Base.SwitchButton;
using static SysBot.Pokemon.PokeDataOffsetsPLZA;
using static SysBot.Pokemon.TradeHub.SpecialRequests;

namespace SysBot.Pokemon;

// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
public class PokeTradeBotPLZA(PokeTradeHub<PA9> Hub, PokeBotState Config) : PokeRoutineExecutor9PLZA(Config), ICountBot, ITradeBot
{
    public readonly TradeAbuseSettings AbuseSettings = Hub.Config.TradeAbuse;

    /// <summary>
    /// Folder to dump received trade data to.
    /// </summary>
    /// <remarks>If null, will skip dumping.</remarks>
    private readonly FolderSettings DumpSetting = Hub.Config.Folder;

    private readonly TradeSettings TradeSettings = Hub.Config.Trade;
    public event Action<int>? TradeProgressChanged;
    private uint DisplaySID;
    private uint DisplayTID;

    private string OT = string.Empty;
    private bool StartFromOverworld = true;
    private ulong? _cachedBoxOffset;
    private ulong TradePartnerStatusOffset;
    private bool _wasConnectedToPartner = false;
    private int _consecutiveConnectionFailures = 0; // Track consecutive online connection failures for soft ban detection

    public event EventHandler<Exception>? ConnectionError;

    public event EventHandler? ConnectionSuccess;

    // Progress bar states
    private enum TradeState
    {
        Idle,               // No trade active
        Starting,           // Command received
        EnteringCode,       // Link code input
        WaitingForPartner,  // Searching
        PartnerFound,       // Partner detected
        Confirming,         // Confirming trade
        Trading,            // Trade animation running
        Completed,          // Trade done successfully
        Failed              // Trade aborted / error
    }

    private TradeState _tradeState = TradeState.Idle;
    private int _lastProgress = -1;

    private void SetTradeState(TradeState newState)
    {
        if (_tradeState == newState)
            return;

        _tradeState = newState;

        int progress = newState switch
        {
            TradeState.Idle => 0,
            TradeState.Starting => 5,
            TradeState.EnteringCode => 15,
            TradeState.WaitingForPartner => 30,
            TradeState.PartnerFound => 45,
            TradeState.Confirming => 65,
            TradeState.Trading => 85,
            TradeState.Completed => 100,
            TradeState.Failed => 0,
            _ => _lastProgress
        };

        // never regress unless explicitly resetting to Idle
        if (progress < _lastProgress && newState != TradeState.Idle)
            return;

        _lastProgress = progress;
        TradeProgressChanged?.Invoke(progress);
    }

    public ICountSettings Counts => TradeSettings;

    /// <summary>
    /// Tracks failed synchronized starts to attempt to re-sync.
    /// </summary>
    public int FailedBarrier { get; private set; }

    /// <summary>
    /// Synchronized start for multiple bots.
    /// </summary>
    public bool ShouldWaitAtBarrier { get; private set; }

    #region Lifecycle & Main Loop

    public override Task HardStop()
    {
        UpdateBarrier(false);
        return CleanExit(CancellationToken.None);
    }

    public override async Task MainLoop(CancellationToken token)
    {
        try
        {
            // Ensure cache is clean on startup
            _cachedBoxOffset = null;
            _wasConnectedToPartner = false;
            _consecutiveConnectionFailures = 0;

            Hub.Queues.Info.CleanStuckTrades();
            await InitializeHardware(Hub.Config.Trade, token).ConfigureAwait(false);

            Log("Conectando a la consola...");
            var sav = await IdentifyTrainer(token).ConfigureAwait(false);
            OT = sav.OT;
            DisplaySID = sav.DisplaySID;
            DisplayTID = sav.DisplayTID;
            RecentTrainerCache.SetRecentTrainer(sav);
            OnConnectionSuccess();

            StartFromOverworld = true;

            Log("Inicializando el bot...");
            if (!await CheckIfOnOverworld(token).ConfigureAwait(false))
            {
                if (!await RecoverToOverworld(token).ConfigureAwait(false))
                {
                    Log("Reiniciando el juego...");

                    await RestartGamePLZA(token).ConfigureAwait(false);
                    await Task.Delay(5_000, token).ConfigureAwait(false);

                    if (!await CheckIfOnOverworld(token).ConfigureAwait(false))
                    {
                        Log("Error al iniciar. Por favor reinicia el bot.");
                        throw new Exception("No se pudo llegar al overworld. El bot no puede iniciar los intercambios.");
                    }
                }
            }

            Log("Bot listo. Esperando intercambios...");
            await InnerLoop(sav, token).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            OnConnectionError(e);
            throw;
        }

        Log($"Finalizando el bucle de {nameof(PokeTradeBotPLZA)}.");
        await HardStop().ConfigureAwait(false);
    }

    public override async Task RebootAndStop(CancellationToken t)
    {
        Hub.Queues.Info.CleanStuckTrades();
        await Task.Delay(2_000, t).ConfigureAwait(false);
        await ReOpenGame(Hub.Config, t).ConfigureAwait(false);
        _cachedBoxOffset = null; // Invalidate box offset cache after reboot
        await HardStop().ConfigureAwait(false);
        await Task.Delay(2_000, t).ConfigureAwait(false);
        if (!t.IsCancellationRequested)
        {
            Log("Reiniciando el bucle principal.");
            await MainLoop(t).ConfigureAwait(false);
        }
    }

    #endregion

    #region Enums

    protected enum TradePartnerWaitResult
    {
        Success,
        Timeout,
        KickedToMenu
    }

    protected enum LinkCodeEntryResult
    {
        Success,
        VerificationFailedMismatch
    }

    #endregion

    #region Trade Queue Management

    protected virtual (PokeTradeDetail<PA9>? detail, uint priority) GetTradeData(PokeRoutineType type)
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

    #endregion

    #region Trade Partner Detection

    // Upon connecting, their Nintendo ID will instantly update.
    protected virtual async Task<TradePartnerWaitResult> WaitForTradePartner(CancellationToken token)
    {
        Log("Esperando conectarse con el usuario antes de iniciar el proceso de intercambio...");
        SetTradeState(TradeState.WaitingForPartner);

        // Initial delay to let the game populate NID pointer in memory
        await Task.Delay(2_000, token).ConfigureAwait(false);

        int maxWaitMs = Hub.Config.Trade.TradeConfiguration.TradeWaitTime * 1_000;
        int elapsed = 2_000; // Already waited 3 seconds above

        while (elapsed < maxWaitMs)
        {
            // Check if we've entered the trade box - this confirms a partner is connected
            if (!await IsOnMenu(MenuState.InBox, token).ConfigureAwait(false))
            {
                // Check if we got kicked back to overworld/menu
                var menuState = await GetMenuState(token).ConfigureAwait(false);
                if (menuState == MenuState.Overworld || menuState == MenuState.XMenu)
                {
                    Log("Conexión interrumpida. Reiniciando...");
                    return TradePartnerWaitResult.KickedToMenu;
                }

                await Task.Delay(100, token).ConfigureAwait(false);
                elapsed += 100;
                continue;
            }

            // We're in the box - wait a moment then validate the status pointer
            await Task.Delay(500, token).ConfigureAwait(false);
            elapsed += 500;

            // Set the offset for trade partner status monitoring
            var (valid, statusOffset) = await ValidatePointerAll(Offsets.TradePartnerStatusPointer, token).ConfigureAwait(false);
            if (!valid)
                continue; // Keep trying until pointer is valid

            Log("¡Compañero de intercambio detectado!");
            SetTradeState(TradeState.PartnerFound);
            _wasConnectedToPartner = true;
            TradePartnerStatusOffset = statusOffset;
            return TradePartnerWaitResult.Success;
        }

        Log("Tiempo agotado esperando al compañero de intercambio.");
        SetTradeState(TradeState.Failed);
        return TradePartnerWaitResult.Timeout;
    }

    #endregion

    #region AutoOT Features

    private static void ApplyTrainerInfo(PA9 pokemon, TradePartnerStatusPLZA partner)
    {
        pokemon.OriginalTrainerGender = (byte)partner.Gender;
        pokemon.TrainerTID7 = (uint)Math.Abs(partner.DisplayTID);
        pokemon.TrainerSID7 = (uint)Math.Abs(partner.DisplaySID);
        pokemon.OriginalTrainerName = partner.OT;
    }

    private async Task<PA9> ApplyAutoOT(PA9 toSend, TradePartnerStatusPLZA tradePartner, SAV9ZA sav, CancellationToken token)
    {
        // Sanity check: if trade partner OT is empty, skip AutoOT
        if (string.IsNullOrWhiteSpace(tradePartner.OT))
        {
            return toSend;
        }

        if (toSend.Version == GameVersion.GO)
        {
            var goClone = toSend.Clone();
            goClone.OriginalTrainerName = tradePartner.OT;

            ClearOTTrash(goClone, tradePartner);

            if (!toSend.ChecksumValid)
                goClone.RefreshChecksum();

            var boxOffset = await GetBoxStartOffset(token).ConfigureAwait(false);
            await SetBoxPokemonAbsolute(boxOffset, goClone, token, sav).ConfigureAwait(false);
            return goClone;
        }

        if (toSend is IHomeTrack pk && pk.HasTracker)
        {
            return toSend;
        }

        if (toSend.Generation != toSend.Format)
        {
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
            return toSend;
        }

        var cln = toSend.Clone();

        // Apply trainer info (OT, TID, SID, Gender)
        ApplyTrainerInfo(cln, tradePartner);

        if (!isMysteryGift)
        {
            // Only override language if Pokemon has default/config language
            // If user explicitly requested a different language, preserve it
            var configLanguage = (int)legalitySettings.GenerateLanguage;

            // If Pokemon already has a non-default language (user explicitly requested it), keep it
            if (toSend.Language != configLanguage && toSend.Language >= 1 && toSend.Language <= 12)
            {
                cln.Language = toSend.Language; // Preserve explicitly requested language
            }
            else
            {
                // Otherwise, use trade partner's language
                int language = tradePartner.Language;
                if (language < 1 || language > 12) // Valid language IDs are 1-12
                    language = 2; // English
                cln.Language = language;
            }
        }

        ClearOTTrash(cln, tradePartner);

        // Hard-code version to ZA since PLZA only has one game version
        cln.Version = GameVersion.ZA;

        // Set nickname to species name in the Pokemon's language using PKHeX's method
        // This properly handles generation-specific formatting and language-specific names
        if (!toSend.IsNicknamed)
            cln.ClearNickname();

        // Clear handler info - make it look like trade partner is OT and never traded it
        cln.CurrentHandler = 0; // 0 = OT is current handler

        if (toSend.IsShiny)
            cln.PID = (uint)((cln.TID16 ^ cln.SID16 ^ (cln.PID & 0xFFFF) ^ toSend.ShinyXor) << 16) | (cln.PID & 0xFFFF);

        cln.RefreshChecksum();

        var tradeSV = new LegalityAnalysis(cln);

        if (tradeSV.Valid)
        {
            // Don't pass sav - we've already set handler info and don't want UpdateHandler to overwrite it
            var boxOffset = await GetBoxStartOffset(token).ConfigureAwait(false);
            await SetBoxPokemonAbsolute(boxOffset, cln, token, null).ConfigureAwait(false);
            return cln;
        }
        else
        {
            if (toSend.Species != 0)
            {
                var boxOffset = await GetBoxStartOffset(token).ConfigureAwait(false);
                await SetBoxPokemonAbsolute(boxOffset, toSend, token, sav).ConfigureAwait(false);
            }
            return toSend;
        }
    }

    private static void ClearOTTrash(PA9 pokemon, TradePartnerStatusPLZA tradePartner)
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

    #endregion

    #region Trade Confirmation

    private async Task<PokeTradeResult> ConfirmAndStartTrading(PokeTradeDetail<PA9> detail, uint checksumBeforeTrade, CancellationToken token)
    {
        var boxOffset = await GetBoxStartOffset(token).ConfigureAwait(false);
        var oldEC = await SwitchConnection.ReadBytesAbsoluteAsync(boxOffset, 8, token).ConfigureAwait(false);

        await Click(A, 3_000, token).ConfigureAwait(false);

        bool warningSent = false;
        int maxTime = Hub.Config.Trade.TradeConfiguration.MaxTradeConfirmTime;

        for (int i = 0; i < maxTime; i++)
        {
            // Check if we're still in trade box (partner disconnected if not in InBox menu state)
            if (!await IsOnMenu(MenuState.InBox, token).ConfigureAwait(false))
            {
                Log("Ya no estamos en la caja de intercambio: el compañero rechazó o salió durante la etapa de oferta.");
                SetTradeState(TradeState.Failed);
                detail.SendNotification(this, "El compañero de intercambio rechazó o se desconectó.");
                return PokeTradeResult.NoTrainerFound;
            }

            await Click(A, 1_000, token).ConfigureAwait(false);

            // Send warning 10 seconds before timeout
            if (!warningSent && i == maxTime - 10 && maxTime >= 10)
            {
                detail.SendNotification(this, "¡Oye! ¡Elige un Pokémon para intercambiar o me iré!");
                warningSent = true;
            }

            var newEC = await SwitchConnection.ReadBytesAbsoluteAsync(boxOffset, 8, token).ConfigureAwait(false);
            if (!newEC.SequenceEqual(oldEC))
            {
                Log("¡Intercambio iniciado!");
                SetTradeState(TradeState.Trading);
                return PokeTradeResult.Success;
            }
        }

        return PokeTradeResult.TrainerTooSlow;
    }

    #endregion

    #region Online Connection & Portal

    private async Task<bool> ConnectAndEnterPortal(CancellationToken token)
    {
        if (!await CheckIfOnOverworld(token).ConfigureAwait(false))
            await RecoverToOverworld(token).ConfigureAwait(false);

        await Click(X, 3_000, token).ConfigureAwait(false); // Load Menu

        await Click(DUP, 1_000, token).ConfigureAwait(false);
        await Click(A, 2_000, token).ConfigureAwait(false);
        await Click(DRIGHT, 1_000, token).ConfigureAwait(false);
        await Click(DRIGHT, 1_000, token).ConfigureAwait(false);
        await Click(A, 1_000, token).ConfigureAwait(false);
        await Click(DRIGHT, 1_000, token).ConfigureAwait(false);

        bool wasAlreadyConnected = await CheckIfConnectedOnline(token).ConfigureAwait(false);

        if (wasAlreadyConnected)
        {
            await Click(A, 1_000, token).ConfigureAwait(false);
            await Click(A, 1_000, token).ConfigureAwait(false);
            await Task.Delay(1_000, token).ConfigureAwait(false);
            _consecutiveConnectionFailures = 0;
        }
        else
        {
            await Click(A, 1_000, token).ConfigureAwait(false);

            int attempts = 0;
            int delayMs = 1_000; // Start with 1 second delay
            while (!await CheckIfConnectedOnline(token).ConfigureAwait(false))
            {
                // Use exponential backoff for retries to handle degraded connection conditions
                await Task.Delay(delayMs, token).ConfigureAwait(false);

                if (++attempts > 30)
                {
                    _consecutiveConnectionFailures++;
                    Log($"No se pudo conectar en línea. Fallos consecutivos: {_consecutiveConnectionFailures}");

                    if (_consecutiveConnectionFailures >= 3)
                    {
                        Log("Softban detectado (3 fallos consecutivos de conexión). Esperando 30 minutos...");
                        await Task.Delay(30 * 60 * 1000, token).ConfigureAwait(false);
                        Log("Espera de 30 minutos completada. Reanudando operaciones.");
                        _consecutiveConnectionFailures = 0;
                    }

                    return false;
                }

                // Exponential backoff: increase delay after every 5 failed attempts
                // This helps with degraded network conditions after multiple trades
                if (attempts % 5 == 0 && delayMs < 3_000)
                {
                    delayMs += 500; // Increment by 500ms every 5 attempts
                    Log($"Intento de conexión {attempts}, aumentando el retraso de reintento a {delayMs}ms");
                }
            }
            await Task.Delay(8_000 + Hub.Config.Timings.ExtraTimeConnectOnline, token).ConfigureAwait(false);
            Log("Conectado en línea.");
            _consecutiveConnectionFailures = 0;

            await Click(A, 1_000, token).ConfigureAwait(false);
            await Click(A, 1_000, token).ConfigureAwait(false);
            await Task.Delay(3_000, token).ConfigureAwait(false);
        }

        return true;
    }

    #endregion

    #region Trade Queue Processing

    private async Task DoNothing(CancellationToken token)
    {
        Log("Esperando a que un usuario comience a comerciar...");
        SetTradeState(TradeState.Idle);

        while (!token.IsCancellationRequested && Config.NextRoutineType == PokeRoutineType.Idle)
            await Task.Delay(1_000, token).ConfigureAwait(false);
    }

    private async Task DoTrades(SAV9ZA sav, CancellationToken token)
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

            detail.IsProcessing = true;
            Log($"Entrando al X-Menu y seleccionando Intercambio por Enlace...");
            SetTradeState(TradeState.Idle);
            SetTradeState(TradeState.Starting);
            Hub.Config.Stream.StartTrade(this, detail, Hub);
            Hub.Queues.StartTrade(this, detail);

            await PerformTrade(sav, detail, type, priority, token).ConfigureAwait(false);
        }
    }

    #endregion

    #region Navigation and Recovery

    private async Task DisconnectFromTrade(CancellationToken token)
    {
        Log("Desconectando del intercambio...");
        SetTradeState(TradeState.Failed);

        // Verificar si aún estamos en la caja de intercambio (conectados) o si ya nos devolvió al menú
        var menuState = await GetMenuState(token).ConfigureAwait(false);

        if (menuState == MenuState.InBox)
        {
            // Aún en la caja de intercambio: presionar B + A para desconectarse
            await Click(B, 0_500, token).ConfigureAwait(false);
            await Click(A, 1_000, token).ConfigureAwait(false);
        }
        else
        {
            // Ya estamos en el menú: solo presionar B para volver atrás
            await Click(B, 0_500, token).ConfigureAwait(false);
        }
    }

    private async Task ExitTradeToOverworld(bool unexpected, CancellationToken token)
    {
        if (unexpected)
            Log("Comportamiento inesperado, recuperando al overworld.");
        SetTradeState(TradeState.Failed);

        if (await CheckIfOnOverworld(token).ConfigureAwait(false))
        {
            StartFromOverworld = true;
            _wasConnectedToPartner = false; // Restablecer bandera cuando volvemos correctamente al overworld
            return;
        }

        // Si estamos en las Cajas o buscando un Intercambio por Enlace, necesitamos usar el método BAB; de lo contrario, solo podemos presionar B repetidamente.
        var remainMs = 120_000;
        while (await GetMenuState(token).ConfigureAwait(false) >= MenuState.LinkTrade)
        {
            if (remainMs < 0)
            {
                StartFromOverworld = true;
                _wasConnectedToPartner = false; // Restablecer bandera cuando volvemos correctamente al overworld
                return;
            }

            await Click(B, 1_000, token).ConfigureAwait(false);
            if (await GetMenuState(token).ConfigureAwait(false) < MenuState.LinkTrade)
                break;

            var box = await IsOnMenu(MenuState.InBox, token).ConfigureAwait(false);
            await Click(box ? A : B, 1_000, token).ConfigureAwait(false);
            if (await GetMenuState(token).ConfigureAwait(false) < MenuState.LinkTrade)
                break;

            await Click(B, 1_000, token).ConfigureAwait(false);
            if (await GetMenuState(token).ConfigureAwait(false) < MenuState.LinkTrade)
                break;
            remainMs -= 3_000;
        }

        // Desde aquí, deberíamos poder presionar B para volver al overworld.
        while (!await CheckIfOnOverworld(token).ConfigureAwait(false))
            await Click(B, 0_200, token).ConfigureAwait(false);

        Log("Regresó al overworld.");
        SetTradeState(TradeState.Failed);
        StartFromOverworld = true;
        _wasConnectedToPartner = false;
    }

    #endregion

    #region Game State & Data Access

    private async Task<TradePartnerStatusPLZA> GetTradePartnerFullInfo(CancellationToken token)
    {
        var baseAddr = await SwitchConnection.PointerAll(Offsets.LinkTradePartnerDataPointer, token).ConfigureAwait(false);
        var nidAddr = baseAddr + TradePartnerNIDShift;
        var tidAddr = baseAddr + TradePartnerTIDShift;

        // Read chunk starting from NID location - includes NID, TID at +0x44, and OT at +0x4C
        var chunk = await SwitchConnection.ReadBytesAbsoluteAsync(nidAddr, 0x69, token).ConfigureAwait(false);
        var nid = BitConverter.ToUInt64(chunk.AsSpan(0, 8));
        var dataIsLoaded = chunk[0x68] != 0;

        var trader_info = new TradePartnerStatusPLZA();

        if (dataIsLoaded)
        {
            var tid = chunk.AsSpan(0x44, 4).ToArray();
            var ot = chunk.AsSpan(0x4C, TradePartnerPLZA.MaxByteLengthStringObject).ToArray();
            tid.CopyTo(trader_info.Data, 0x00);
            ot.CopyTo(trader_info.Data, 0x08);

            // Read gender and language from TID location offset
            var genderLang = await SwitchConnection.ReadBytesAbsoluteAsync(tidAddr, 0x08, token).ConfigureAwait(false);
            trader_info.Data[0x04] = genderLang[0x04]; // Gender at TID base + 0x04
            trader_info.Data[0x05] = genderLang[0x05]; // Language at TID base + 0x05
        }
        else
        {
            // Data not at primary location, use fallback
            var fallbackTidAddr = tidAddr + FallBackTradePartnerDataShift;
            var fallbackChunk = await SwitchConnection.ReadBytesAbsoluteAsync(fallbackTidAddr, 34, token).ConfigureAwait(false);

            var tid = fallbackChunk.AsSpan(0, 4).ToArray();
            var ot = fallbackChunk.AsSpan(0x08, TradePartnerPLZA.MaxByteLengthStringObject).ToArray();
            tid.CopyTo(trader_info.Data, 0x00);
            ot.CopyTo(trader_info.Data, 0x08);

            // Read gender and language from fallback TID location
            var genderLang = await SwitchConnection.ReadBytesAbsoluteAsync(fallbackTidAddr, 0x08, token).ConfigureAwait(false);
            trader_info.Data[0x04] = genderLang[0x04]; // Gender at fallback TID + 0x04
            trader_info.Data[0x05] = genderLang[0x05]; // Language at fallback TID + 0x05
        }

        return trader_info;
    }

    private async Task<ulong> GetBoxStartOffset(CancellationToken token)
    {
        if (_cachedBoxOffset.HasValue)
            return _cachedBoxOffset.Value;

        // Get Box 1 Slot 1 address
        var finalOffset = await ResolvePointer(Offsets.BoxStartPokemonPointer, token).ConfigureAwait(false);
        _cachedBoxOffset = finalOffset;
        return finalOffset;
    }

    private async Task<bool> CheckIfOnOverworld(CancellationToken token)
    {
        return await IsOnMenu(MenuState.Overworld, token).ConfigureAwait(false);
    }

    private async Task<bool> CheckIfConnectedOnline(CancellationToken token)
    {
        // Primero verificar si el socket sigue activo para evitar SocketException
        if (!SwitchConnection.Connected)
        {
            Log("Conexión de socket perdida, forzando reconexión");
            return false;
        }

        // Usar el offset directo de memoria principal para comprobaciones de conexión más rápidas y fiables
        return await IsConnected(token).ConfigureAwait(false);
    }


    #endregion

    #region Trade Result Handling

    private void HandleAbortedTrade(PokeTradeDetail<PA9> detail, PokeRoutineType type, uint priority, PokeTradeResult result)
    {
        // Skip processing if we've already handled the notification (e.g., NoTrainerFound)
        if (result == PokeTradeResult.NoTrainerFound)
            return;

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
            detail.TradeCanceled(this, result);
        }
    }

    private async Task InnerLoop(SAV9ZA sav, CancellationToken token)
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

                // Invalidate cached pointers after reconnection - game state may have changed
                _cachedBoxOffset = null;
                Log("Reconectado - punteros en caché invalidados.");
            }
        }
    }

    #endregion

    #region Events

    private void OnConnectionError(Exception ex)
    {
        ConnectionError?.Invoke(this, ex);
    }

    private void OnConnectionSuccess()
    {
        ConnectionSuccess?.Invoke(this, EventArgs.Empty);
    }

    #endregion

    #region Specialized Trade Types

    private async Task<PokeTradeResult> PerformBatchTrade(
    SAV9ZA sav,
    PokeTradeDetail<PA9> poke,
    CancellationToken token)
    {
        int completedTrades = 0;
        var startingDetail = poke;
        var originalTrainerID = startingDetail.Trainer.ID;

        var tradesToProcess = poke.BatchTrades ?? new List<PA9> { poke.TradeData };
        int totalBatchTrades = tradesToProcess.Count;

        TradePartnerStatusPLZA? cachedTradePartnerInfo = null;

        void CleanupBatch(bool sendBackPokemon)
        {
            var allReceived = BatchTracker.GetReceivedPokemon(originalTrainerID);

            if (sendBackPokemon && allReceived.Count > 0)
            {
                poke.SendNotification(this,
                    $"✅ Enviándote los {allReceived.Count} Pokémon que me intercambiaste.");

                foreach (var mon in allReceived)
                {
                    var name = SpeciesName.GetSpeciesName(mon.Species, 2);
                    poke.SendNotification(this, mon, $"Pokémon que me intercambiaste: {name}");
                    Thread.Sleep(500);
                }
            }

            BatchTracker.ClearReceivedPokemon(originalTrainerID);
            BatchTracker.ReleaseBatch(originalTrainerID, startingDetail.UniqueTradeID);

            poke.IsProcessing = false;
            Hub.Queues.Info.Remove(
                new TradeEntry<PA9>(
                    poke,
                    originalTrainerID,
                    PokeRoutineType.Batch,
                    poke.Trainer.TrainerName,
                    poke.UniqueTradeID));
        }

        try
        {
            var retryCounts = new Dictionary<int, int>();
            for (int i = 0; i < totalBatchTrades; i++)
            {
                poke.TradeData = tradesToProcess[i];
                poke.Notifier.UpdateBatchProgress(i + 1, poke.TradeData, poke.UniqueTradeID);

                if (i > 0)
                {
                    poke.SendNotification(this,
                        $"**Ready!** Offer Pokémon {i + 1}/{totalBatchTrades}.");
                    await Task.Delay(2_000, token).ConfigureAwait(false);
                }

                // ================= FIRST TRADE ONLY =================
                if (i == 0)
                {
                    await Click(A, 500, token).ConfigureAwait(false);
                    await Click(A, 500, token).ConfigureAwait(false);

                    WaitAtBarrierIfApplicable(token);
                    await Click(A, 1_000, token).ConfigureAwait(false);

                    poke.TradeSearching(this);
                    var waitResult = await WaitForTradePartner(token).ConfigureAwait(false);

                    if (token.IsCancellationRequested)
                        return PokeTradeResult.RoutineCancel;

                    if (waitResult == TradePartnerWaitResult.Timeout)
                        return PokeTradeResult.NoTrainerFound;

                    if (waitResult == TradePartnerWaitResult.KickedToMenu)
                        return PokeTradeResult.RecoverStart;

                    Hub.Config.Stream.EndEnterCode(this);

                    int attempts = 0;
                    while (!await IsOnMenu(MenuState.InBox, token).ConfigureAwait(false))
                    {
                        if (++attempts > 30)
                            return PokeTradeResult.NoTrainerFound;

                        await Task.Delay(500, token).ConfigureAwait(false);
                    }

                    await Task.Delay(2_000, token).ConfigureAwait(false);

                    cachedTradePartnerInfo = await GetTradePartnerFullInfo(token).ConfigureAwait(false);
                    var partner = new TradePartnerPLZA(cachedTradePartnerInfo);
                    var trainerNID = await GetTradePartnerNID(token).ConfigureAwait(false);

                    Log($"[TradePartner] OT: {partner.TrainerName} | TID: {partner.TID7} | SID: {partner.SID7} | Género: {TrainerDisplayHelper.GetGenderString(partner.Gender)} | Idioma: {TrainerDisplayHelper.GetLanguageString(partner.Language)} | NID: {trainerNID}");

                    var partnerCheck = CheckPartnerReputation(
                    this,
                    poke,
                    trainerNID,
                    partner.TrainerName,
                    AbuseSettings,
                    token);

                    if (partnerCheck != PokeTradeResult.Success)
                    {
                        poke.SendNotification(this, "Compañero de intercambio bloqueado. Cancelando intercambios en lote.");
                        SetTradeState(TradeState.Failed);
                        return partnerCheck;
                    }

                    poke.SendNotification(this,
                        $"Compañero de intercambio encontrado: **{partner.TrainerName}** " +
                        $"\n\n▼\n Aquí está tu información\n **TID**: __{partner.TID7}__\n **SID**: __{partner.SID7}__\n▲");

                    if (Hub.Config.Legality.UseTradePartnerInfo && !poke.IgnoreAutoOT)
                    {
                        tradesToProcess[0] = await ApplyAutoOT(
                            tradesToProcess[0],
                            cachedTradePartnerInfo,
                            sav,
                            token).ConfigureAwait(false);

                        poke.TradeData = tradesToProcess[0];
                        await Task.Delay(3_000, token).ConfigureAwait(false);
                    }
                }

                poke.SendNotification(this,
                     $"Por favor ofrece el Pokémon {i + 1}/{totalBatchTrades}.");

                ulong boxOffset = await GetBoxStartOffset(token).ConfigureAwait(false);
                var beforeTrade = await ReadPokemon(
                    boxOffset,
                    BoxFormatSlotSize,
                    token).ConfigureAwait(false);

                var offered = await WaitForBatchOfferAsync(
                    i,
                    totalBatchTrades,
                    token).ConfigureAwait(false);

                if (offered == null)
                    return PokeTradeResult.TrainerTooSlow;

                if (Hub.Config.Trade.TradeConfiguration.DisallowTradeEvolve &&
                    TradeEvolutions.WillTradeEvolve(
                        offered.Species,
                        offered.Form,
                        offered.HeldItem,
                        poke.TradeData.Species))
                    return PokeTradeResult.TradeEvolveNotAllowed;

                SetTradeState(TradeState.Confirming);

                var tradeResult = await ConfirmAndStartTrading(
                    poke,
                    beforeTrade.Checksum,
                    token).ConfigureAwait(false);

                if (tradeResult == PokeTradeResult.TrainerTooSlow)
                {
                    if (!retryCounts.ContainsKey(i))
                        retryCounts[i] = 0;

                    retryCounts[i]++;

                    if (retryCounts[i] == 1)
                    {
                        Log($"Animación de intercambio detectada para el intercambio {i + 1}/{totalBatchTrades}. Esperando antes de continuar...");
                    }
                    else
                    {
                        Log($"El entrenador está tardando en entrar al intercambio {i + 1}/{totalBatchTrades}, reintentando...");
                    }

                    await Task.Delay(2_000, token).ConfigureAwait(false);
                    i--; // retry same trade index
                    continue;
                }

                if (tradeResult != PokeTradeResult.Success)
                    return tradeResult;

                boxOffset = await GetBoxStartOffset(token).ConfigureAwait(false);
                var received = await ReadPokemon(
                    boxOffset,
                    BoxFormatSlotSize,
                    token).ConfigureAwait(false);

                if (received == null || received.Species == 0)
                    return PokeTradeResult.TrainerTooSlow;

                BatchTracker.AddReceivedPokemon(originalTrainerID, received);
                UpdateCountsAndExport(poke, received, poke.TradeData);

                completedTrades++;

                // Log animation wait message after each successful trade except the last
                if (i + 1 < totalBatchTrades)
                {
                    Log($"Esperando a que la animación de intercambio termine antes de continuar con el intercambio {i + 2}...");
                }

                // Inject next Pokémon during animation
                if (i + 1 < totalBatchTrades)
                {
                    var next = tradesToProcess[i + 1];

                    if (Hub.Config.Legality.UseTradePartnerInfo &&
                        !poke.IgnoreAutoOT &&
                        cachedTradePartnerInfo != null)
                    {
                        next = await ApplyAutoOT(
                            next,
                            cachedTradePartnerInfo,
                            sav,
                            token).ConfigureAwait(false);

                        tradesToProcess[i + 1] = next;
                    }

                    await SetBoxPokemonAbsolute(
                        await GetBoxStartOffset(token).ConfigureAwait(false),
                        next,
                        token,
                        sav).ConfigureAwait(false);
                }
            }

            poke.SendNotification(this,
                "✅ ¡Todos los intercambios en lote han sido completados! ¡Gracias por intercambiar!");

            var allReceived = BatchTracker.GetReceivedPokemon(originalTrainerID);
            if (allReceived.Count > 0)
                poke.TradeFinished(this, allReceived[^1]);

            Hub.Queues.CompleteTrade(this, startingDetail);
            return PokeTradeResult.Success;
        }
        finally
        {
            CleanupBatch(Hub.Config.Discord.ReturnPKMs);
            await ExitTradeToOverworld(false, token).ConfigureAwait(false);
        }
    }

    private async Task<PA9?> WaitForBatchOfferAsync(
        int tradeIndex,
        int totalTrades,
        CancellationToken token)
    {
        var start = DateTime.UtcNow;
        var timeout = TimeSpan.FromSeconds(45);

        while (!token.IsCancellationRequested)
        {
            var mon = await ReadUntilPresentPointer(
                Offsets.LinkTradePartnerPokemonPointer,
                1_000,
                300,
                BoxFormatSlotSize,
                token).ConfigureAwait(false);

            if (mon?.Species > 0 && mon.ChecksumValid)
                return mon;

            if (!await IsOnMenu(MenuState.InBox, token).ConfigureAwait(false))
                return null;

            if (DateTime.UtcNow - start > timeout)
            {
                Log($"Trade {tradeIndex + 1}/{totalTrades} timed out.");
                return null;
            }

            await Task.Delay(250, token).ConfigureAwait(false);
        }

        return null;
    }


    #endregion

    #region Core Trade Logic

    private async Task PerformTrade(SAV9ZA sav, PokeTradeDetail<PA9> detail, PokeRoutineType type, uint priority, CancellationToken token)
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

    private async Task<PokeTradeResult> PerformLinkCodeTrade(SAV9ZA sav, PokeTradeDetail<PA9> poke, CancellationToken token)
    {
        // Check if trade was canceled by user
        if (poke.IsCanceled)
        {
            Log($"El intercambio para {poke.Trainer.TrainerName} fue cancelado por el usuario.");
            SetTradeState(TradeState.Failed);
            poke.TradeCanceled(this, PokeTradeResult.UserCanceled);
            return PokeTradeResult.UserCanceled;
        }

        // Update Barrier Settings
        UpdateBarrier(poke.IsSynchronized);
        poke.TradeInitialize(this);
        Hub.Config.Stream.EndEnterCode(this);

        // Handle connection and portal entry FIRST
        if (!await EnsureConnectedAndInPortal(token).ConfigureAwait(false))
        {
            return PokeTradeResult.RecoverStart;
        }

        // Enter Link Trade and code
        var result = await EnterLinkTradeAndCode(poke, poke.Code, token).ConfigureAwait(false);

        if (result == LinkCodeEntryResult.VerificationFailedMismatch)
        {
            // El código no coincidió - algo salió mal, reiniciando el juego
            Log("La verificación del código falló. Reiniciando el juego...");
            SetTradeState(TradeState.Failed);
            await RestartGamePLZA(token).ConfigureAwait(false);
            return PokeTradeResult.RecoverStart;
        }

        // Inject Pokemon AFTER code verification succeeds and BEFORE searching
        var toSend = poke.TradeData;
        if (toSend.Species != 0)
        {
            Log("Pokémon solicitado inyectado en B1S1.");
            SetTradeState(TradeState.EnteringCode);
            var offset = await GetBoxStartOffset(token).ConfigureAwait(false);
            await SetBoxPokemonAbsolute(offset, toSend, token, sav).ConfigureAwait(false);
        }

        StartFromOverworld = false;

        // Route to appropriate trade handling based on trade type
        if (poke.Type == PokeTradeType.Batch)
            return await PerformBatchTrade(sav, poke, token).ConfigureAwait(false);

        return await PerformNonBatchTrade(sav, poke, token).ConfigureAwait(false);
    }

    private async Task<bool> EnsureConnectedAndInPortal(CancellationToken token)
    {
        if (StartFromOverworld)
        {
            if (!await CheckIfOnOverworld(token).ConfigureAwait(false))
            {
                await RecoverToOverworld(token).ConfigureAwait(false);
            }

            if (!await ConnectAndEnterPortal(token).ConfigureAwait(false))
            {
                Log("Error de conexión. Reiniciando...");
                SetTradeState(TradeState.Failed);
                await RecoverToOverworld(token).ConfigureAwait(false);
                return false;
            }
        }
        else
        {
            // IMPORTANT: Check socket state first before trusting game memory
            // After multiple trades, the socket may be disconnected even if game state shows connected
            // This prevents the "assume connection persists" issue that causes socket read failures
            bool socketAlive = SwitchConnection.Connected;
            bool gameConnected = socketAlive && await CheckIfConnectedOnline(token).ConfigureAwait(false);

            if (!gameConnected)
            {
                if (!socketAlive)
                {
                    Log("Conexión del socket perdida entre intercambios, forzando reconexión...");
                }

                await RecoverToOverworld(token).ConfigureAwait(false);
                if (!await ConnectAndEnterPortal(token).ConfigureAwait(false))
                {
                    Log("La conexión falló. Reiniciando...");
                    SetTradeState(TradeState.Failed);
                    await RecoverToOverworld(token).ConfigureAwait(false);
                    return false;
                }
            }
        }

        return true;
    }

    private async Task<LinkCodeEntryResult> EnterLinkTradeAndCode(PokeTradeDetail<PA9> poke, int code, CancellationToken token)
    {
        // Loading code entry
        if (poke.Type != PokeTradeType.Random)
        {
            Hub.Config.Stream.StartEnterCode(this);
        }

        // PLZA saves the previous Link Code after the first trade.
        // If the pointer isn't valid, we haven't traded yet.
        var (valid, _) = await ValidatePointerAll(Offsets.LinkTradeCodePointer, token).ConfigureAwait(false);
        if (!valid)
        {
            // No previous trade, freely enter our code
            if (code != 0)
            {
                Log($"Ingresando el código de intercambio: {code:0000 0000}...");
                SetTradeState(TradeState.EnteringCode);
                await EnterLinkCode(code, Hub.Config, token).ConfigureAwait(false);
            }
        }
        else
        {
            var prevCode = await GetStoredLinkTradeCode(token).ConfigureAwait(false);
            if (prevCode != code)
            {
                // Only clear if the new code is different
                var codeLength = await GetStoredLinkTradeCodeLength(token).ConfigureAwait(false);
                if (codeLength > 0)
                {
                    for (int i = 0; i < codeLength; i++)
                        await Click(B, 0, token).ConfigureAwait(false);
                    await Task.Delay(0_500, token).ConfigureAwait(false);
                }

                if (code != 0)
                {
                    Log($"Ingresando el código de intercambio: {code:0000 0000}...");
                    SetTradeState(TradeState.EnteringCode);
                    await EnterLinkCode(code, Hub.Config, token).ConfigureAwait(false);
                }
            }
            else
            {
                Log($"Usando el código de intercambio anterior: {code:0000 0000}.");
                SetTradeState(TradeState.EnteringCode);
            }
        }

        await Click(PLUS, 2_000, token).ConfigureAwait(false);

        return LinkCodeEntryResult.Success;
    }

    private async Task<PokeTradeResult> PerformNonBatchTrade(SAV9ZA sav, PokeTradeDetail<PA9> poke, CancellationToken token)
    {
        var toSend = poke.TradeData;

        await Click(A, 0_500, token).ConfigureAwait(false);
        await Click(A, 0_500, token).ConfigureAwait(false);

        WaitAtBarrierIfApplicable(token);
        await Click(A, 1_000, token).ConfigureAwait(false);

        poke.TradeSearching(this);
        var partnerWaitResult = await WaitForTradePartner(token).ConfigureAwait(false);

        if (token.IsCancellationRequested)
        {
            StartFromOverworld = true;
            await ExitTradeToOverworld(false, token).ConfigureAwait(false);
            return PokeTradeResult.RoutineCancel;
        }

        if (partnerWaitResult == TradePartnerWaitResult.Timeout)
        {
            // El compañero nunca apareció - es su culpa, no volver a poner en cola
            poke.IsProcessing = false;
            poke.SendNotification(this, "No se encontró un compañero de intercambio. Cancelando el intercambio.");
            poke.TradeCanceled(this, PokeTradeResult.NoTrainerFound);

            await RecoverToOverworld(token).ConfigureAwait(false);
            return PokeTradeResult.NoTrainerFound;
        }

        if (partnerWaitResult == TradePartnerWaitResult.KickedToMenu)
        {
            // El bot fue expulsado al menú - es nuestra culpa, activar requeue
            Log("Error de conexión. Reintentando...");
            SetTradeState(TradeState.Failed);
            await RecoverToOverworld(token).ConfigureAwait(false);
            return PokeTradeResult.RecoverStart;
        }


        Hub.Config.Stream.EndEnterCode(this);

        // Wait until we're in the trade box
        Log("Seleccionando Pokémon en B1S1...");
        SetTradeState(TradeState.EnteringCode);
        int boxCheckAttempts = 0;
        while (!await IsOnMenu(MenuState.InBox, token).ConfigureAwait(false))
        {
            await Task.Delay(500, token).ConfigureAwait(false);
            if (++boxCheckAttempts > 30) // máximo 15 segundos
            {
                Log("No se encontró un compañero de intercambio.");
                SetTradeState(TradeState.Failed);
                return PokeTradeResult.NoTrainerFound;
            }
        }

        // Wait for trade UI and partner data to load
        await Task.Delay(5_000, token).ConfigureAwait(false);

        // Now that data has loaded, read partner info
        var tradePartnerFullInfo = await GetTradePartnerFullInfo(token).ConfigureAwait(false);
        var tradePartner = new TradePartnerPLZA(tradePartnerFullInfo);

        var trainerNID = await GetTradePartnerNID(token).ConfigureAwait(false);

        Log($"[TradePartner] OT: {tradePartner.TrainerName} | TID: {tradePartner.TID7} | SID: {tradePartner.SID7} | Gender: {TrainerDisplayHelper.GetGenderString(tradePartner.Gender)} | Language: {TrainerDisplayHelper.GetLanguageString(tradePartner.Language)} | NID: {trainerNID}");


        RecordUtil<PokeTradeBotPLZA>.Record($"Initiating\t{trainerNID:X16}\t{tradePartner.TrainerName}\t{poke.Trainer.TrainerName}\t{poke.Trainer.ID}\t{poke.ID}\t{toSend.EncryptionConstant:X8}");
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
        }

        var partnerCheck = CheckPartnerReputation(this, poke, trainerNID, tradePartner.TrainerName, AbuseSettings, token);
        if (partnerCheck != PokeTradeResult.Success)
        {
            await Click(A, 1_000, token).ConfigureAwait(false);
            await ExitTradeToOverworld(false, token).ConfigureAwait(false);
            return partnerCheck;
        }

        // Read the offered Pokemon for Clone/Dump trades
        PA9? offered = null;
        if (poke.Type == PokeTradeType.Clone || poke.Type == PokeTradeType.Dump)
        {
            offered = await ReadUntilPresentPointer(Offsets.LinkTradePartnerPokemonPointer, 3_000, 0_500, BoxFormatSlotSize, token).ConfigureAwait(false);
            if (offered == null || offered.Species == 0)
            {
                poke.SendNotification(this, "No se pudo leer el Pokémon ofrecido. Saliendo del intercambio.");
                await ExitTradeToOverworld(true, token).ConfigureAwait(false);
                return PokeTradeResult.TrainerRequestBad;
            }
        }

        if (poke.Type == PokeTradeType.Clone)
        {
            var (result, clone) = await ProcessCloneTradeAsync(poke, sav, offered!, token).ConfigureAwait(false);
            if (result != PokeTradeResult.Success)
            {
                await ExitTradeToOverworld(false, token).ConfigureAwait(false);
                return result;
            }

            // Trade them back their cloned Pokemon
            toSend = clone!;
        }

        if (poke.Type == PokeTradeType.Dump)
        {
            var result = await ProcessDumpTradeAsync(poke, token).ConfigureAwait(false);
            await ExitTradeToOverworld(false, token).ConfigureAwait(false);
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

            // Give game time to refresh trade offer display with AutoOT Pokemon
            await Task.Delay(3_000, token).ConfigureAwait(false);
        }

        SpecialTradeType itemReq = SpecialTradeType.None;
        if (poke.Type == PokeTradeType.Seed)
        {
            poke.SendNotification(this, "⚠️ Los intercambios de Seed están temporalmente no disponibles. Por favor solicita un Pokémon específico en su lugar.");
            await ExitTradeToOverworld(true, token).ConfigureAwait(false);
            return PokeTradeResult.TrainerRequestBad;
        }

        if (itemReq == SpecialTradeType.WonderCard)
            poke.SendNotification(this, "✅ ¡Distribución exitosa!");
        else if (itemReq != SpecialTradeType.None && itemReq != SpecialTradeType.Shinify)
            poke.SendNotification(this, "✅ ¡Solicitud especial completada!");
        else if (itemReq == SpecialTradeType.Shinify)
            poke.SendNotification(this, "✅ ¡Shinify completado! ¡Gracias por ser parte de la comunidad!");

        var offsetBefore = await GetBoxStartOffset(token).ConfigureAwait(false);
        var pokemonBeforeTrade = await ReadPokemon(offsetBefore, BoxFormatSlotSize, token).ConfigureAwait(false);
        var checksumBeforeTrade = pokemonBeforeTrade.Checksum;

        // Read the partner's offered Pokemon BEFORE we start pressing A to confirm
        // This way we can cancel with B+A if they're offering something that will evolve
        if (offered == null) // Only read if we haven't already (Clone/Dump read it earlier)
        {
            offered = await ReadUntilPresentPointer(Offsets.LinkTradePartnerPokemonPointer, 3_000, 0_500, BoxFormatSlotSize, token).ConfigureAwait(false);
        }

        if (offered == null || offered.Species == 0 || !offered.ChecksumValid)
        {
            Log("El intercambio terminó porque la oferta del entrenador fue retirada demasiado rápido.");
            SetTradeState(TradeState.Failed);
            poke.SendNotification(this, "El compañero de intercambio no ofreció un Pokémon válido.");
            await DisconnectFromTrade(token).ConfigureAwait(false);
            await ExitTradeToOverworld(false, token).ConfigureAwait(false);
            return PokeTradeResult.TrainerOfferCanceledQuick;
        }

        // Check if the offered Pokemon will evolve upon trade BEFORE confirming
        if (Hub.Config.Trade.TradeConfiguration.DisallowTradeEvolve && TradeEvolutions.WillTradeEvolve(offered.Species, offered.Form, offered.HeldItem, toSend.Species))
        {
            Log("El intercambio fue cancelado porque el entrenador ofreció un Pokémon que evolucionaría al intercambiarse.");
            SetTradeState(TradeState.Failed);
            poke.SendNotification(this, "⚠️ Intercambio cancelado. No puedes intercambiar un Pokémon que vaya a evolucionar. Para evitar esto, dale a tu Pokémon una Piedra Eterna o intercambia un Pokémon diferente.");
            await DisconnectFromTrade(token).ConfigureAwait(false);
            await ExitTradeToOverworld(false, token).ConfigureAwait(false);
            return PokeTradeResult.TradeEvolveNotAllowed;
        }

        Log("Seleccionando \"Intercambiarlo.\" Ahora esperando a que comience la animación del intercambio...");
        SetTradeState(TradeState.Confirming);
        var tradeResult = await ConfirmAndStartTrading(poke, checksumBeforeTrade, token).ConfigureAwait(false);
        if (tradeResult != PokeTradeResult.Success)
        {
            if (tradeResult == PokeTradeResult.TrainerTooSlow)
            {
                await DisconnectFromTrade(token).ConfigureAwait(false);
            }
            await ExitTradeToOverworld(false, token).ConfigureAwait(false);
            return tradeResult;
        }

        if (token.IsCancellationRequested)
        {
            StartFromOverworld = true;
            await ExitTradeToOverworld(false, token).ConfigureAwait(false);
            return PokeTradeResult.RoutineCancel;
        }

        var offset2 = await GetBoxStartOffset(token).ConfigureAwait(false);
        var received = await ReadPokemon(offset2, BoxFormatSlotSize, token).ConfigureAwait(false);
        var checksumAfterTrade = received.Checksum;

        if (checksumBeforeTrade == checksumAfterTrade)
        {
            Log("El intercambio fue cancelado.");
            SetTradeState(TradeState.Failed);
            poke.SendNotification(this, "⚠️ El intercambio fue cancelado. Por favor, inténtalo de nuevo.");
            await DisconnectFromTrade(token).ConfigureAwait(false);
            await ExitTradeToOverworld(false, token).ConfigureAwait(false);
            return PokeTradeResult.TrainerTooSlow;
        }

        Log($"¡Intercambio completo! Se recibió {(Species)received.Species}. Ahora esperando a que termine la animación del intercambio...");
        SetTradeState(TradeState.Completed);

        poke.TradeFinished(this, received);
        UpdateCountsAndExport(poke, received, toSend);
        LogSuccessfulTrades(poke, trainerNID, tradePartner.TrainerName);

        await ExitTradeToOverworld(false, token).ConfigureAwait(false);
        return PokeTradeResult.Success;
    }

    private async Task HandleAbortedBatchTrade(PokeTradeDetail<PA9> detail, PokeRoutineType type, uint priority, PokeTradeResult result, CancellationToken token)
    {
        detail.IsProcessing = false;

        // Always remove from UsersInQueue on abort
        Hub.Queues.Info.Remove(new TradeEntry<PA9>(detail, detail.Trainer.ID, type, detail.Trainer.TrainerName, detail.UniqueTradeID));

        if (detail.TotalBatchTrades > 1)
        {
            // Release the batch claim on failure
            BatchTracker.ReleaseBatch(detail.Trainer.ID, detail.UniqueTradeID);

            if (result.ShouldAttemptRetry() && detail.Type != PokeTradeType.Random && !detail.IsRetry)
            {
                detail.IsRetry = true;
                Hub.Queues.Enqueue(type, detail, Math.Min(priority, PokeTradePriorities.Tier2));
                detail.SendNotification(this, "⚠️ ¡Ups! Ocurrió algo durante tu intercambio múltiple. Te volveré a poner en la cola para otro intento.");
            }
            else
            {
                detail.SendNotification(this, $"⚠️ El intercambio múltiple falló: {result}");
                detail.TradeCanceled(this, result);
                await ExitTradeToOverworld(false, token).ConfigureAwait(false);
            }
        }
        else
        {
            HandleAbortedTrade(detail, type, priority, result);
        }
    }

    private async Task<bool> RecoverToOverworld(CancellationToken token)
    {
        if (await CheckIfOnOverworld(token).ConfigureAwait(false))
            return true;

        Log("Recuperando...");
        SetTradeState(TradeState.Failed);

        await Click(B, 1_500, token).ConfigureAwait(false);
        if (await CheckIfOnOverworld(token).ConfigureAwait(false))
            return true;

        await Click(A, 1_500, token).ConfigureAwait(false);
        if (await CheckIfOnOverworld(token).ConfigureAwait(false))
            return true;

        var attempts = 0;
        while (!await CheckIfOnOverworld(token).ConfigureAwait(false))
        {
            attempts++;
            if (attempts >= 30)
                break;

            await Click(B, 1_000, token).ConfigureAwait(false);
            if (await CheckIfOnOverworld(token).ConfigureAwait(false))
                break;

            await Click(B, 1_000, token).ConfigureAwait(false);
            if (await CheckIfOnOverworld(token).ConfigureAwait(false))
                break;
        }

        if (!await CheckIfOnOverworld(token).ConfigureAwait(false))
        {
            Log("Reiniciando el juego...");
            SetTradeState(TradeState.Failed);

            await RestartGamePLZA(token).ConfigureAwait(false);
        }
        await Task.Delay(1_000, token).ConfigureAwait(false);

        StartFromOverworld = true;
        return true;
    }

    private async Task RestartGamePLZA(CancellationToken token)
    {
        await ReOpenGame(Hub.Config, token).ConfigureAwait(false);
        _cachedBoxOffset = null; // Invalidate box offset cache after restart

        // If we were connected to a partner before restart, prevent soft ban
        if (_wasConnectedToPartner)
        {
            Log("Evitando el soft ban de intercambio: conectando con un compañero aleatorio para limpiar el estado del intercambio...");

            await PreventTradeSoftBan(token).ConfigureAwait(false);
            _wasConnectedToPartner = false; // Restablecer el indicador después de la recuperación
        }
    }

    /// <summary>
    /// Prevents trade soft ban after restarting during an active trade connection.
    ///
    /// When the bot restarts AFTER successfully connecting to a trade partner (verified via MenuState.InBox),
    /// the game may impose a soft ban if we attempt to trade again without clearing the previous connection state.
    ///
    /// This method connects to a random partner (no code) and immediately disconnects using B+A to signal
    /// to the game servers that the previous trade session has ended, preventing the soft ban.
    /// </summary>
    private async Task PreventTradeSoftBan(CancellationToken token)
    {
        await Task.Delay(5_000, token).ConfigureAwait(false);

        if (!await CheckIfOnOverworld(token).ConfigureAwait(false))
        {
            Log("No estás en el overworld después del reinicio, intentando recuperación...");

            await RecoverToOverworld(token).ConfigureAwait(false);
        }

        Log("Conectando en línea para evitar el soft ban de intercambio...");
        await Click(X, 3_000, token).ConfigureAwait(false);
        await Click(DUP, 1_000, token).ConfigureAwait(false);
        await Click(A, 2_000, token).ConfigureAwait(false);
        await Click(DRIGHT, 1_000, token).ConfigureAwait(false);
        await Click(DRIGHT, 1_000, token).ConfigureAwait(false);
        await Click(A, 1_000, token).ConfigureAwait(false);
        await Click(DRIGHT, 1_000, token).ConfigureAwait(false);
        await Click(A, 1_000, token).ConfigureAwait(false);

        int attempts = 0;
        while (!await CheckIfConnectedOnline(token).ConfigureAwait(false))
        {
            await Task.Delay(1_000, token).ConfigureAwait(false);
            if (++attempts > 30)
            {
                Log("No se pudo conectar en línea durante la prevención del soft ban.");
                await RecoverToOverworld(token).ConfigureAwait(false);
                return;
            }
        }
        await Task.Delay(8_000 + Hub.Config.Timings.ExtraTimeConnectOnline, token).ConfigureAwait(false);
        Log("Conectado en línea para la prevención del soft ban.");

        await Click(A, 1_000, token).ConfigureAwait(false);
        await Click(A, 1_000, token).ConfigureAwait(false);
        await Task.Delay(3_000, token).ConfigureAwait(false);

        Log("Conectando con un compañero aleatorio para limpiar la sesión de intercambio anterior...");
        await Click(PLUS, 2_000, token).ConfigureAwait(false);

        Log("Esperando a que se conecte un compañero aleatorio...");
        await Task.Delay(3_000, token).ConfigureAwait(false);

        int waitAttempts = 0;
        bool connected = false;
        while (waitAttempts < 30 && !connected)
        {
            var nid = await GetTradePartnerNID(token).ConfigureAwait(false);
            if (nid != 0)
            {
                Log("Un compañero aleatorio se conectó vía NID. Desconectando para completar la prevención del soft ban...");
                connected = true;
                break;
            }

            if (await IsOnMenu(MenuState.InBox, token).ConfigureAwait(false))
            {
                Log("Un compañero aleatorio se conectó vía TradeBox. Desconectando para completar la prevención del soft ban...");
                connected = true;
                break;
            }

            await Task.Delay(1_000, token).ConfigureAwait(false);
            waitAttempts++;
        }

        if (!connected)
        {
            Log("No se encontró un compañero aleatorio dentro del límite de 30 segundos. Es posible que el soft ban no se haya evitado completamente. Continuando...");
            await RecoverToOverworld(token).ConfigureAwait(false);
            return;
        }

        Log("Desconectando del compañero aleatorio (B para cancelar, A para confirmar)...");
        await Click(B, 1_000, token).ConfigureAwait(false);
        await Click(A, 1_000, token).ConfigureAwait(false);

        Log("Esperando la confirmación de desconexión del compañero...");
        int disconnectAttempts = 0;
        bool partnerDisconnected = false;
        while (disconnectAttempts < 10 && !partnerDisconnected)
        {
            await Task.Delay(500, token).ConfigureAwait(false);
            var currentNid = await GetTradePartnerNID(token).ConfigureAwait(false);
            if (currentNid == 0)
            {
                Log("Compañero desconectado (NID = 0). Saliendo al overworld...");

                partnerDisconnected = true;
                break;
            }
            disconnectAttempts++;
        }

        if (!partnerDisconnected)
        {
            Log("El compañero no se desconectó dentro del tiempo límite. Forzando salida...");

        }

        Log("Presionando B repetidamente para volver al overworld...");

        for (int i = 0; i < 15; i++)
        {
            await Click(B, 1_000, token).ConfigureAwait(false);

            if (await CheckIfOnOverworld(token).ConfigureAwait(false))
            {
                Log("Prevención del soft ban completada. Se volvió al overworld exitosamente.");

                StartFromOverworld = true;
                return;
            }
        }

        Log("No se pudo volver al overworld después de presionar B repetidamente. Realizando una recuperación completa...");

        await RecoverToOverworld(token).ConfigureAwait(false);
        StartFromOverworld = true;
    }

    #endregion

    #region Multi-Bot Synchronization

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

    private void UpdateCountsAndExport(PokeTradeDetail<PA9> poke, PA9 received, PA9 toSend)
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
            DumpPokemon(DumpSetting.DumpFolder, subfolder, received); // received by bot
            if (poke.Type is PokeTradeType.Specific or PokeTradeType.Clone)
                DumpPokemon(DumpSetting.DumpFolder, tradedFolder, toSend); // sent to partner
        }
    }

    #region Clone & Dump Features

    private async Task<bool> CheckCloneChangedOffer(CancellationToken token)
    {
        // Watch their status to indicate they canceled, then offered a new Pokémon.
        var hovering = await ReadUntilChanged(TradePartnerStatusOffset, [0x2], 25_000, 1_000, true, true, token).ConfigureAwait(false);
        if (!hovering)
        {
            Log("El compañero de intercambio no cambió su oferta inicial.");
            SetTradeState(TradeState.Failed);
            return false;
        }
        var offering = await ReadUntilChanged(TradePartnerStatusOffset, [0x3], 25_000, 1_000, true, true, token).ConfigureAwait(false);
        if (!offering)
        {
            return false;
        }
        return true;
    }

    private async Task<(PokeTradeResult Result, PA9? ClonedPokemon)> ProcessCloneTradeAsync(PokeTradeDetail<PA9> poke, SAV9ZA sav, PA9 offered, CancellationToken token)
    {
        if (Hub.Config.Discord.ReturnPKMs)
            poke.SendNotification(this, offered, "¡Aquí tienes lo que me mostraste!");

        var la = new LegalityAnalysis(offered);
        if (!la.Valid)
        {
            Log($"La solicitud de clonación (de {poke.Trainer.TrainerName}) detectó un Pokémon no válido: {GameInfo.GetStrings("en").Species[offered.Species]}.");
            SetTradeState(TradeState.Failed);
            if (DumpSetting.Dump)
                DumpPokemon(DumpSetting.DumpFolder, "hacked", offered);

            var report = la.Report();
            Log(report);
            poke.SendNotification(this, $"⚠️ Este Pokémon no es __**legal**__ según los controles de legalidad de __PKHeX__. Tengo prohibido clonar esto. Cancelando trade...");
            poke.SendNotification(this, report);

            return (PokeTradeResult.IllegalTrade, null);
        }

        var clone = offered.Clone();
        if (Hub.Config.Legality.ResetHOMETracker)
            clone.Tracker = 0;

        poke.SendNotification(this, $"**✅ ¡Cloné tu {GameInfo.GetStrings("en").Species[clone.Species]}!**\nAhora presiona B para cancelar tu oferta y dame un Pokémon que no quieras.");
        Log($"Se clonó un {(Species)clone.Species}. Esperando a que el usuario cambie su Pokémon...");
        SetTradeState(TradeState.Trading);


        if (!await CheckCloneChangedOffer(token).ConfigureAwait(false))
        {
            // They get one more chance.
            poke.SendNotification(this, "**¡HEY, CÁMBIALO AHORA O ME VOY!**");
            if (!await CheckCloneChangedOffer(token).ConfigureAwait(false))
            {
                Log("El compañero de intercambio no cambió su Pokémon.");
                SetTradeState(TradeState.Failed);
                return (PokeTradeResult.TrainerTooSlow, null);
            }
        }

        // If we got to here, we can read their offered Pokémon.
        var pk2 = await ReadUntilPresentPointer(Offsets.LinkTradePartnerPokemonPointer, 5_000, 1_000, BoxFormatSlotSize, token).ConfigureAwait(false);
        if (pk2 is null || SearchUtil.HashByDetails(pk2) == SearchUtil.HashByDetails(offered))
        {
            Log("El compañero de intercambio no cambió su Pokémon.");
            SetTradeState(TradeState.Failed);
            return (PokeTradeResult.TrainerTooSlow, null);
        }

        var boxOffset = await GetBoxStartOffset(token).ConfigureAwait(false);
        await SetBoxPokemonAbsolute(boxOffset, clone, token, sav).ConfigureAwait(false);

        return (PokeTradeResult.Success, clone);
    }

    private async Task<PokeTradeResult> ProcessDumpTradeAsync(PokeTradeDetail<PA9> detail, CancellationToken token)
    {
        int ctr = 0;
        var maxDumps = Hub.Config.Trade.TradeConfiguration.MaxDumpsPerTrade;
        var time = TimeSpan.FromSeconds(Hub.Config.Trade.TradeConfiguration.MaxDumpTradeTime);
        var start = DateTime.Now;

        // Tell the user what to do
        detail.SendNotification(this, $"¡Ahora mostrando tu Pokémon! Puedes mostrarme hasta {maxDumps} Pokémon. ¡Sigue cambiando de Pokémon para enviar más!");

        var pkprev = new PA9();
        var warnedAboutTime = false;
        var bctr = 0;

        while (ctr < maxDumps && DateTime.Now - start < time)
        {
            // Check if we're still in the trade box (user disconnected if not)
            if (!await IsOnMenu(MenuState.InBox, token).ConfigureAwait(false))
            {
                Log("El compañero de intercambio se desconectó (no está en la caja de intercambio).");
                SetTradeState(TradeState.Failed);
                break;
            }

            // Presionar periódicamente el botón B para mantener la conexión activa
            if (bctr++ % 3 == 0)
                await Click(B, 0_100, token).ConfigureAwait(false);

            // Avisar al usuario cuando le quede poco tiempo
            var elapsed = DateTime.Now - start;
            if (!warnedAboutTime && elapsed.TotalSeconds > time.TotalSeconds - 15)
            {
                detail.SendNotification(this, "⚠️ ¡Solo quedan 15 segundos! Muestra tu último Pokémon o presiona B para salir.");
                warnedAboutTime = true;
            }

            // Wait for the user to show us a Pokemon - needs to be different from the previous one
            var pk = await ReadUntilPresentPointer(Offsets.LinkTradePartnerPokemonPointer, 3_000, 0_050, BoxFormatSlotSize, token).ConfigureAwait(false);
            if (pk == null || pk.Species == 0 || !pk.ChecksumValid)
            {
                await Task.Delay(0_050, token).ConfigureAwait(false);
                continue;
            }

            // Check if this is the same Pokemon as before
            if (SearchUtil.HashByDetails(pk) == SearchUtil.HashByDetails(pkprev))
            {
                Log($"El usuario está mostrando el mismo Pokémon que antes. Esperando uno diferente...");
                await Task.Delay(0_500, token).ConfigureAwait(false);
                continue;
            }

            // Heal and refresh checksum to ensure valid data
            pk.Heal();
            pk.RefreshChecksum();

            // Save the new Pokemon for comparison next round
            pkprev = pk;

            // Dump the Pokemon to file if dumping is enabled
            if (DumpSetting.Dump)
            {
                var subfolder = detail.Type.ToString().ToLower();
                DumpPokemon(DumpSetting.DumpFolder, subfolder, pk);
            }

            var la = new LegalityAnalysis(pk);
            var verbose = $"```{la.Report(true)}```";
            Log($"El Pokémon mostrado es: {(la.Valid ? "Válido" : "Inválido")}.");
            SetTradeState(TradeState.Trading);
            ctr++;
            var msg = Hub.Config.Trade.TradeConfiguration.DumpTradeLegalityCheck ? verbose : $"File {ctr}";

            // Include trainer data for people requesting with their own trainer data
            var ot = pk.OriginalTrainerName;
            var ot_gender = pk.OriginalTrainerGender == 0 ? "Hombre" : "Mujer";
            var tid = pk.GetDisplayTID().ToString(pk.GetTrainerIDFormat().GetTrainerIDFormatStringTID());
            var sid = pk.GetDisplaySID().ToString(pk.GetTrainerIDFormat().GetTrainerIDFormatStringSID());
            msg += $"\n**Datos del Entrenador**\n```OT: {ot}\nGéneroOT: {ot_gender}\nTID: {tid}\nSID: {sid}```";

            // Extra information for shiny eggs
            var eggstring = pk.IsEgg ? "Huevo " : string.Empty;
            msg += pk.IsShiny ? $"\n**¡Este Pokémon {eggstring}es variocolor!**" : string.Empty;

            // Send the Pokemon file back to the user via Discord
            detail.SendNotification(this, pk, msg);

            // Tell user their progress
            var remaining = maxDumps - ctr;
            if (remaining > 0)
                detail.SendNotification(this, $"¡Recibido! Puedes mostrarme {remaining} más. Muestra un Pokémon diferente para continuar, o presiona B para salir.");
            else
                detail.SendNotification(this, "⚠️ ¡Ese es el máximo! Presiona B para salir del intercambio.");
        }

        var timeElapsed = DateTime.Now - start;
        Log($"Se terminó el ciclo de volcado después de procesar {ctr} Pokémon en {timeElapsed.TotalSeconds:F1} segundos.");

        if (ctr == 0)
            return PokeTradeResult.TrainerTooSlow;

        TradeSettings.CountStatsSettings.AddCompletedDumps();
        detail.Notifier.SendNotification(this, detail, $"Se enviaron {ctr} Pokémon.");
        detail.Notifier.TradeFinished(this, detail, pkprev); // Send last dumped Pokemon
        return PokeTradeResult.Success;
    }

    #endregion

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
            SetTradeState(TradeState.Idle);
        }

        return Task.Delay(1_000, token);
    }

    #endregion
}
