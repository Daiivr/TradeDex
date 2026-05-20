using PKHeX.Core;
using SysBot.Base;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SysBot.Pokemon;

public class PokeTradeLogNotifier<T> : IPokeTradeNotifier<T> where T : PKM, new()
{
    private int BatchTradeNumber { get; set; } = 1;
    private int TotalBatchTrades { get; set; } = 1;

    public Action<PokeRoutineExecutor<T>>? OnFinish { get; set; }

    public Task SendInitialQueueUpdate()
    {
        return Task.CompletedTask;
    }

    public void UpdateBatchProgress(int currentBatchNumber, T currentPokemon, int uniqueTradeID)
    {
        BatchTradeNumber = currentBatchNumber;
        // Podemos registrar esta actualización opcionalmente
        if (TotalBatchTrades > 1)
        {
            LogUtil.LogInfo("BatchTracker", $"Progreso del intercambio por lotes: {currentBatchNumber}/{TotalBatchTrades} - {GameInfo.GetStrings("en").Species[currentPokemon.Species]}");
        }
    }

    public void SendNotification(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info, string message)
    {
        // Agregar contexto de lote si aplica
        if (info.TotalBatchTrades > 1)
        {
            TotalBatchTrades = info.TotalBatchTrades;
            message = $"[Intercambio {BatchTradeNumber}/{TotalBatchTrades}] {message}";
        }
        LogUtil.LogInfo(routine.Connection.Label, message);
    }

    public void SendNotification(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info, PokeTradeSummary message)
    {
        var msg = message.Summary;
        if (message.Details.Count > 0)
            msg += ", " + string.Join(", ", message.Details.Select(z => $"{z.Heading}: {z.Detail}"));

        // Agregar contexto de lote si aplica
        if (info.TotalBatchTrades > 1)
        {
            TotalBatchTrades = info.TotalBatchTrades;
            msg = $"[Intercambio {BatchTradeNumber}/{TotalBatchTrades}] {msg}";
        }

        LogUtil.LogInfo(routine.Connection.Label, msg);
    }

    public void SendNotification(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info, T result, string message)
    {
        var batchInfo = info.TotalBatchTrades > 1 ? $"[Intercambio {BatchTradeNumber}/{info.TotalBatchTrades}] " : "";
        LogUtil.LogInfo(routine.Connection.Label, $"{batchInfo}Notificando a {info.Trainer.TrainerName} sobre su {GameInfo.GetStrings("en").Species[result.Species]}");
        LogUtil.LogInfo(routine.Connection.Label, $"{batchInfo}{message}");
    }

    public void TradeCanceled(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info, PokeTradeResult msg)
    {
        var batchInfo = info.TotalBatchTrades > 1 ? $"[Intercambio por lotes {BatchTradeNumber}/{info.TotalBatchTrades}] " : "";
        LogUtil.LogInfo(routine.Connection.Label, $"{batchInfo}Cancelando intercambio con {info.Trainer.TrainerName}, porque {msg}.");
        OnFinish?.Invoke(routine);
    }

    public void TradeFinished(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info, T result)
    {
        // Mostrar el mote en intercambios de distribución aleatoria para ver qué se solicitó.
        var ledyname = string.Empty;
        if (info.Trainer.TrainerName == "Random Distribution" && result.IsNicknamed)
            ledyname = $" ({result.Nickname})";

        var batchInfo = info.TotalBatchTrades > 1 ? $"[Intercambio {BatchTradeNumber}/{info.TotalBatchTrades}] " : "";
        LogUtil.LogInfo(
            routine.Connection.Label,
            $"{batchInfo}Intercambio finalizado: {info.Trainer.TrainerName} recibió {GameInfo.GetStrings("en").Species[result.Species]} a cambio de {GameInfo.GetStrings("en").Species[info.TradeData.Species]}{ledyname}"
        );

        // Solo invocar OnFinish para intercambios individuales o el último de un lote
        if (info.TotalBatchTrades <= 1 || BatchTradeNumber == info.TotalBatchTrades)
        {
            OnFinish?.Invoke(routine);
        }
    }

    public void TradeInitialize(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info)
    {
        var batchInfo = info.TotalBatchTrades > 1 ? $"[Iniciando intercambio por lotes - {info.TotalBatchTrades} en total] " : "";
        LogUtil.LogInfo(
            routine.Connection.Label,
            $"{batchInfo}Iniciando ciclo de intercambio para {info.Trainer.TrainerName}, enviando {GameInfo.GetStrings("en").Species[info.TradeData.Species]}"
        );
    }

    public void TradeSearching(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info)
    {
        var batchInfo = info.TotalBatchTrades > 1 ? $"[Intercambio {BatchTradeNumber}/{info.TotalBatchTrades}] " : "";
        LogUtil.LogInfo(
            routine.Connection.Label,
            $"{batchInfo}Buscando intercambio con {info.Trainer.TrainerName}, enviando {GameInfo.GetStrings("en").Species[info.TradeData.Species]}"
        );
    }
}
