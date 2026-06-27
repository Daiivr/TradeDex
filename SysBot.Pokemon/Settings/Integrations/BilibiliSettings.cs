using System;
using System.ComponentModel;
using System.Linq;
using SysBot.Pokemon.Localization;

namespace SysBot.Pokemon
{
    public class BilibiliSettings
    {
        private const string Startup = nameof(Startup);

        public override string ToString() => "Configuracion de integracion de Bilibili";

        // Startup

        [Category(Startup), Description("Directorio de logs de Bilibili Danmakuji.")]
        public string LogUrl { get; set; } = string.Empty;

        [Category(Startup), Description("ID de la sala de directo.")]
        public int RoomId { get; set; } = 0;
    }
}
