using System.ComponentModel;

namespace SysBot.Pokemon;

/// <summary>
/// Configuracion para la recuperacion automatica de bots despues de cierres o cancelaciones.
/// </summary>
public class RecoverySettings
{
    private const string Recovery = nameof(Recovery);

    [Category(Recovery), Description("Habilita intentos de recuperacion automatica para bots bloqueados o detenidos.")]
    public bool EnableRecovery { get; set; } = true;

    [Category(Recovery), Description("Numero maximo de intentos consecutivos de recuperacion antes de abandonar un bot.")]
    public int MaxRecoveryAttempts { get; set; } = 3;

    [Category(Recovery), Description("Retraso inicial en segundos antes de intentar reiniciar un bot bloqueado.")]
    public int InitialRecoveryDelaySeconds { get; set; } = 5;

    [Category(Recovery), Description("Retraso maximo en segundos entre intentos de recuperacion (para espera exponencial).")]
    public int MaxRecoveryDelaySeconds { get; set; } = 300; // 5 minutes

    [Category(Recovery), Description("Multiplicador para la espera exponencial (por ejemplo, 2.0 duplica el retraso cada vez).")]
    public double BackoffMultiplier { get; set; } = 2.0;

    [Category(Recovery), Description("Ventana de tiempo en minutos para rastrear el historial de fallos. Los fallos fuera de esta ventana no cuentan.")]
    public int CrashHistoryWindowMinutes { get; set; } = 60; // 1 hour

    [Category(Recovery), Description("Numero maximo de fallos permitidos dentro de la ventana de historial antes de apagar permanentemente.")]
    public int MaxCrashesInWindow { get; set; } = 5;

    [Category(Recovery), Description("Habilita recuperacion para bots detenidos intencionalmente (util para desconexiones de red).")]
    public bool RecoverIntentionalStops { get; set; } = false;

    [Category(Recovery), Description("Retraso en segundos despues de una recuperacion exitosa antes de reiniciar el contador de intentos.")]
    public int SuccessfulRecoveryResetDelaySeconds { get; set; } = 300; // 5 minutes

    [Category(Recovery), Description("Enviar notificaciones cuando un bot falla y se intenta recuperarlo.")]
    public bool NotifyOnRecoveryAttempt { get; set; } = true;

    [Category(Recovery), Description("Enviar notificaciones cuando un bot no logra recuperarse despues de todos los intentos.")]
    public bool NotifyOnRecoveryFailure { get; set; } = true;

    [Category(Recovery), Description("Tiempo minimo activo en segundos antes de considerar estable a un bot (reinicia los intentos de recuperacion).")]
    public int MinimumStableUptimeSeconds { get; set; } = 600; // 10 minutes

    public override string ToString() => "Configuracion de recuperacion de bots";
}
