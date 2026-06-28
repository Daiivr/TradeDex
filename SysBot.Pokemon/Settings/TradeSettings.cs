using Discord;
using PKHeX.Core;
using SysBot.Base;
using SysBot.Pokemon.Localization;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading;

namespace SysBot.Pokemon;

public class TradeSettings : IBotStateSettings, ICountSettings
{
    private const string CountStats = nameof(CountStats);

    private const string HOMELegality = nameof(HOMELegality);

    private const string TradeConfig = nameof(TradeConfig);

    private const string VGCPastesConfig = nameof(VGCPastesConfig);

    private const string Miscellaneous = nameof(Miscellaneous);

    private const string EmbedSettings = nameof(EmbedSettings);

    public override string ToString() => "Ajustes de configuración de Trade";

    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class EmojiInfo
    {
        [Description("La cadena completa para el emoji.")]
        public string EmojiString { get; set; } = string.Empty;

        public override string ToString()
        {
            return string.IsNullOrEmpty(EmojiString) ? "No establecido" : EmojiString;
        }
    }

    [Category(TradeConfig), Description("Ajustes relacionados con la configuración del trade."), DisplayName("Configuración del trade"), Browsable(true)]
    public TradeSettingsCategory TradeConfiguration { get; set; } = new();

    [Category(VGCPastesConfig), Description("Ajustes relacionados con la Configuración de VGCPastes."), DisplayName("Configuración de VGCPastes"), Browsable(true)]
    public VGCPastesCategory VGCPastesConfiguration { get; set; } = new();

    [Category(EmbedSettings), Description("Ajustes relacionados con el Trade Embed en Discord."), DisplayName("Configuración del Embed Trade"), Browsable(true)]
    public TradeEmbedSettingsCategory TradeEmbedSettings { get; set; } = new();

    [Category(CountStats), Description("Ajustes relacionados con las estadísticas de recuento de trades."), DisplayName("Configuración de las estadísticas de recuento de trades"), Browsable(true)]
    public CountStatsSettingsCategory CountStatsSettings { get; set; } = new();

    [Category(TradeConfig), TypeConverter(typeof(CategoryConverter<TradeSettingsCategory>))]
    public class TradeSettingsCategory
    {
        public override string ToString() => "Ajustes de configuración de trade";

        [Category(TradeConfig), Description("Código de enlace mínimo."), DisplayName("Código mínimo de enlace comercial")]
        public int MinTradeCode { get; set; } = 0;

        [Category(TradeConfig), Description("Código de enlace máximo."), DisplayName("Código de enlace comercial máximo")]
        public int MaxTradeCode { get; set; } = 9999_9999;

        [Category(TradeConfig), Description("Si se establece en True, el código de trade de los usuarios de Discord se almacenará y se utilizará repetidamente sin cambiar."), DisplayName("Almacenar y reutilizar códigos de Tradeo")]
        public bool StoreTradeCodes { get; set; } = true;

        [Category(TradeConfig), Description("Tiempo a esperar por un usuario en segundos."), DisplayName("Tiempo a esperar por un usuario (segundos)")]
        public int TradeWaitTime { get; set; } = 55;

        [Category(TradeConfig), Description("Cantidad máxima de tiempo en segundos pulsando A para esperar a que se procese una operación."), DisplayName("Tiempo máximo de confirmación de la operación (segundos)")]
        public int MaxTradeConfirmTime { get; set; } = 45;

        [Category(TradeConfig), Description("Seleccione la especie por defecto para \"ItemTrade\", si está configurado."), DisplayName("Especies por defecto para el Item Trade")]
        public Species ItemTradeSpecies { get; set; } = Species.None;

        [Category(TradeConfig), Description("Item por defecto que se enviará si no se especifica ninguno."), DisplayName("Item por defecto para trades")]
        public HeldItem DefaultHeldItem { get; set; } = HeldItem.None;

        [Category(TradeConfig), Description("Si se establece en True, cada Pokemon válido vendrá con todos los Movimientos Reaprendibles sugeridos sin necesidad de utilizar un batch command."), DisplayName("Sugerir movimientos reaprendibles por defecto")]
        public bool SuggestRelearnMoves { get; set; } = true;

        [Category(TradeConfig), Description("Activar o desactivar los trades por lotes."), DisplayName("Permitir trades por lotes")]
        public bool AllowBatchTrades { get; set; } = true;

        [Category(TradeConfig), Description("Verifica el apodo y el OT para detectar spam."), DisplayName("Habilitar verificación de spam")]
        public bool EnableSpamCheck { get; set; } = true;

        [Category(TradeConfig), Description("Máximo de pokemons de un solo comercio. El modo por lotes se cerrará si esta configuración es inferior a 1"), DisplayName("Máximo de Pokémon por trades")]
        public int MaxPkmsPerTrade { get; set; } = 6;

        [Category(TradeConfig), Description("Dump Trade: La rutina de dump se detendrá tras un número máximo de deumps de un mismo usuario."), DisplayName("Dumps máximos por operación")]
        public int MaxDumpsPerTrade { get; set; } = 25;

        [Category(TradeConfig), Description("Dump Trade: La rutina de dump se detendrá después de pasar x segundos en el trade."), DisplayName("Tiempo máximo de dump (segundos)")]
        public int MaxDumpTradeTime { get; set; } = 60;

        [Category(TradeConfig), Description("Dump Trade: Si está activada, la rutina de dump mostrará al usuario información sobre la comprobación de la legalidad."), DisplayName("Verificación de la legalidad del dumping")]
        public bool DumpTradeLegalityCheck { get; set; } = true;

        [Category(TradeConfig), Description("Cuando esta habilitado, el bot cancelara automaticamente un trade si le ofrecen un Pokemon que va a evolucionar."), DisplayName("No permitir ofertas que evolucionan")]
        public bool DisallowTradeEvolve { get; set; } = true;

        [Category(TradeConfig), Description("Ajustes LGPE.")]
        public int TradeAnimationMaxDelaySeconds = 25;

        public enum HeldItem
        {
            None = 0,

            MasterBall = 1,

            RareCandy = 50,

            ppUp = 51,

            ppMax = 53,

            BigPearl = 89,

            Nugget = 92,

            AbilityCapsule = 645,

            BottleCap = 795,

            GoldBottleCap = 796,

            expCandyL = 1127,

            expCandyXL = 1128,

            AbilityPatch = 1606,

            FreshStartMochi = 2479,
        }
    }

    [Category(EmbedSettings), TypeConverter(typeof(CategoryConverter<TradeEmbedSettingsCategory>))]
    public class TradeEmbedSettingsCategory
    {
        public override string ToString() => "Ajustes de configuración de Trade Embed";

        private bool _useEmbeds = true;

        [Category("General"), TypeConverter(typeof(ExpandableObjectConverter)), Description("Ajustes principales del embed de trade."), DisplayName("General")]
        [JsonIgnore]
        public EmbedGeneralSettings General => new(this);

        [Category("Emojis"), TypeConverter(typeof(ExpandableObjectConverter)), Description("Ajustes de emojis usados por el embed de trade."), DisplayName("Emojis")]
        [JsonIgnore]
        public EmbedEmojiSettings Emojis => new(this);

        [Category("Contenido"), TypeConverter(typeof(ExpandableObjectConverter)), Description("Campos que se muestran dentro del embed de trade."), DisplayName("Contenido mostrado")]
        [JsonIgnore]
        public EmbedContentSettings Content => new(this);

        public sealed class EmbedGeneralSettings(TradeEmbedSettingsCategory settings)
        {
            public override string ToString() => "Ajustes generales del embed";

            [Description("Si es verdadero, mostrará hermosos embeds en sus canales de trade de discord de lo que el usuario este tradeando. False mostrará el texto por defecto."), DisplayName("Usar embeds")]
            public bool UseEmbeds
            {
                get => settings.UseEmbeds;
                set => settings.UseEmbeds = value;
            }

            [Description("Tamaño preferido de la imagen de la especie para embeds."), DisplayName("Tamaño de la imagen del Pokémon")]
            public ImageSize PreferredImageSize
            {
                get => settings.PreferredImageSize;
                set => settings.PreferredImageSize = value;
            }

            [TypeConverter(typeof(ExpandableObjectConverter)), Description("Opciones Extras para el embed"), DisplayName("Opciones Extras")]
            public EmbedTxTOptions ExtraEmbedOptions
            {
                get => settings.ExtraEmbedOptions;
                set => settings.ExtraEmbedOptions = value ?? new();
            }
        }

        public sealed class EmbedEmojiSettings(TradeEmbedSettingsCategory settings)
        {
            public override string ToString() => "Ajustes de emojis del embed";

            [Description("Mostrará los iconos de tipo de movimiento junto a los movimientos en el Embed Trade (sólo Discord). Requiere que el usuario suba los emojis a su servidor."), DisplayName("¿Mostrar Emojis de Movimientos?")]
            public bool MoveTypeEmojis
            {
                get => settings.MoveTypeEmojis;
                set => settings.MoveTypeEmojis = value;
            }

            [Description("Mostrará los iconos de Tera Tipo junto a los movimientos en el Embed Trade (sólo SV y Discord). Requiere que el usuario suba los emojis a su servidor."), DisplayName("¿Mostrar Emojis de Tipo Tera?")]
            public bool UseTeraEmojis
            {
                get => settings.UseTeraEmojis;
                set => settings.UseTeraEmojis = value;
            }

            [Description("Si es verdadero, se mostrarán los emojis para las escalas XXXS y XXXL en el Embed Trade."), DisplayName("¿Usar Emojis de Tamaño?")]
            public bool UseScaleEmojis
            {
                get => settings.UseScaleEmojis;
                set => settings.UseScaleEmojis = value;
            }

            [Description("Información personalizada de Emoji para los tipos de movimiento."), DisplayName("Emojis de Movimientos")]
            public List<MoveTypeEmojiInfo> CustomTypeEmojis
            {
                get => settings.CustomTypeEmojis;
                set => settings.CustomTypeEmojis = value ?? [];
            }

            [Description("Configuración de emojis para todos los tipos Tera, incluyendo 'Stellar'."), DisplayName("Emojis de Tipo Tera")]
            public List<TeraTypeEmojiInfo> TeraTypeEmojis
            {
                get => settings.TeraTypeEmojis;
                set => settings.TeraTypeEmojis = value ?? [];
            }

            [Description("Informacion del emoji para mostrar movimientos Plus aplicables en el embed de Discord."), DisplayName("Emoji de movimiento Plus")]
            public EmojiInfo UsePlusMoveEmoji
            {
                get => settings.UsePlusMoveEmoji;
                set => settings.UsePlusMoveEmoji = value ?? new();
            }

            [TypeConverter(typeof(ExpandableObjectConverter)), Description("Configuración de emojis para las escalas XXXS y XXXL."), DisplayName("Emojis de tamaño")]
            public ScaleEmojisSettings ScaleEmojis
            {
                get => settings.ScaleEmojis;
                set => settings.ScaleEmojis = value ?? new();
            }

            [TypeConverter(typeof(ExpandableObjectConverter)), Description("Configuración de emojis para Pokémon Shiny."), DisplayName("Emojis Shiny")]
            public ShinyEmojisSettings ShinyEmojis
            {
                get => settings.ShinyEmojis;
                set => settings.ShinyEmojis = value ?? new();
            }

            [TypeConverter(typeof(ExpandableObjectConverter)), Description("Configuración de emojis para géneros."), DisplayName("Emojis de Género")]
            public GenderEmojisSettings GenderEmojis
            {
                get => settings.GenderEmojis;
                set => settings.GenderEmojis = value ?? new();
            }

            [TypeConverter(typeof(ExpandableObjectConverter)), Description("Configuración de emojis para marcas especiales y estados."), DisplayName("Emojis de Marcas y Estados Especiales")]
            public SpecialMarksEmojisSettings SpecialMarksEmojis
            {
                get => settings.SpecialMarksEmojis;
                set => settings.SpecialMarksEmojis = value ?? new();
            }
        }

        public sealed class EmbedContentSettings(TradeEmbedSettingsCategory settings)
        {
            public override string ToString() => "Campos visibles del embed";

            [Description("Se mostrará la Escala en el Embed Trade (SV y Discord solamente). Requiere que el usuario suba los emojis a su servidor."), DisplayName("Mostrar Tamaño")]
            public bool ShowScale { get => settings.ShowScale; set => settings.ShowScale = value; }

            [Description("Mostrará el Tera Tipo en el Embed Trade (sólo SV y Discord)."), DisplayName("Mostrar Tera Tipo")]
            public bool ShowTeraType { get => settings.ShowTeraType; set => settings.ShowTeraType = value; }

            [Description("Se mostrará el nivel en el Embed Trade (Discord solamente)."), DisplayName("Mostrar Nivel")]
            public bool ShowLevel { get => settings.ShowLevel; set => settings.ShowLevel = value; }

            [Description("Mostrara la Ball en el embed de trade (solo Discord)."), DisplayName("Mostrar Ball")]
            public bool ShowBall { get => settings.ShowBall; set => settings.ShowBall = value; }

            [Description("Mostrara el nivel de encuentro en el embed de trade (solo Discord)."), DisplayName("Mostrar nivel de encuentro")]
            public bool ShowMetLevel { get => settings.ShowMetLevel; set => settings.ShowMetLevel = value; }

            [Description("Mostrará MetDate en el Embed Trade (sólo Discord)."), DisplayName("Mostrar Fecha de Encuentro")]
            public bool ShowMetDate { get => settings.ShowMetDate; set => settings.ShowMetDate = value; }

            [Description("Mostrara la ubicacion de encuentro en el embed de trade (solo Discord)."), DisplayName("Mostrar ubicacion de encuentro")]
            public bool ShowMetLocation { get => settings.ShowMetLocation; set => settings.ShowMetLocation = value; }

            [Description("Se mostrará Habilidad en el Embed Trade (Discord solamente)."), DisplayName("Mostrar Habilidad")]
            public bool ShowAbility { get => settings.ShowAbility; set => settings.ShowAbility = value; }

            [Description("Se mostrará la naturaleza en el Embed Trade (Discord solamente)."), DisplayName("Mostrar Naturaleza")]
            public bool ShowNature { get => settings.ShowNature; set => settings.ShowNature = value; }

            [Description("Mostrará el idioma en el embed de intercambio (solo en Discord)."), DisplayName("Mostrar Idioma")]
            public bool ShowLanguage { get => settings.ShowLanguage; set => settings.ShowLanguage = value; }

            [Description("Mostrará IVs en el Embed Trade (Discord solamente)."), DisplayName("Mostrar IVs")]
            public bool ShowIVs { get => settings.ShowIVs; set => settings.ShowIVs = value; }

            [Description("Mostrará los EVs en el Embed Trade (sólo Discord)."), DisplayName("Mostrar EVs")]
            public bool ShowEVs { get => settings.ShowEVs; set => settings.ShowEVs = value; }

            [Description("Mostrar GVs en el embed de intercambio (solo Discord)."), DisplayName("Mostrar GVs para PLA")]
            public bool ShowGVs { get => settings.ShowGVs; set => settings.ShowGVs = value; }

            [Description("Mostrar AVs en el embed de intercambio (solo Discord)."), DisplayName("Mostrar AVs para LGPE")]
            public bool ShowAVs { get => settings.ShowAVs; set => settings.ShowAVs = value; }

            [Description("Mostrará el Rastreador HOME en el embed de intercambio (solo en Discord)."), DisplayName("Mostrar Rastreador")]
            public bool ShowTracker { get => settings.ShowTracker; set => settings.ShowTracker = value; }
        }

        [Browsable(false), Category(EmbedSettings), Description("Si es verdadero, mostrará hermosos embeds en sus canales de trade de discord de lo que el usuario este tradeando. False mostrará el texto por defecto."), DisplayName("Usar embeds")]
        public bool UseEmbeds
        {
            get => _useEmbeds;
            set
            {
                _useEmbeds = value;
                OnUseEmbedsChanged();
            }
        }

        private void OnUseEmbedsChanged()
        {
            if (!_useEmbeds)
            {
                PreferredImageSize = ImageSize.Size128x128;
                MoveTypeEmojis = false;
                ShowScale = false;
                ShowTeraType = false;
                ShowLevel = false;
                ShowBall = false;
                ShowMetLevel = false;
                ShowMetDate = false;
                ShowMetLocation = false;
                ShowAbility = false;
                ShowNature = false;
                ShowIVs = false;
            }
        }

        [Browsable(false), Category(EmbedSettings), Description("Tamaño preferido de la imagen de la especie para embeds."), DisplayName("Tamaño de la imagen del Pokémon")]
        public ImageSize PreferredImageSize { get; set; } = ImageSize.Size128x128;

        [Browsable(false), Category(EmbedSettings), TypeConverter(typeof(ExpandableObjectConverter)), Description("Opciones Extras para el embed"), DisplayName("Opciones Extras")]
        public EmbedTxTOptions ExtraEmbedOptions { get; set; } = new();

        public class EmbedTxTOptions
        {
            private const string LegacySpanishNonNativeText = "*Puede que no pueda ir a HOME y AutoOT no fue aplicado.*";
            private const string PreviousEnglishNonNativeText = "*Cannot enter HOME & AutoOT not applied.*";
            private const string PreviousSpanishNonNativeText = "*No puede entrar a HOME y AutoOT no fue aplicado.*";
            private const string DefaultEnglishNonNativeText = "*It may not be able to enter HOME and AutoOT was not applied.*";

            private string _nonNativeTexT = string.Empty;

            public override string ToString() => "(Collection)";

            [Category(EmbedSettings), Description("URL que aparece al hacer click en el titulo de embed."), DisplayName("URL del título del Embed")]
            public string TradingBotUrl { get; set; } = string.Empty;

            [Category(EmbedSettings), Description("Mensaje que aparece en el embed cuando el Pokémon solicitado no es nativo del juego actual."), DisplayName("Texto para Pokémon no nativo")]
            public string NonNativeTexT
            {
                get => IsDefaultNonNativeText(_nonNativeTexT)
                    ? AppLocalization.Get(LocalizationKeys.DiscordCannotEnterHomeAutoOt)
                    : _nonNativeTexT;
                set => _nonNativeTexT = IsDefaultNonNativeText(value) ? string.Empty : value;
            }

            private static bool IsDefaultNonNativeText(string? value)
            {
                if (string.IsNullOrWhiteSpace(value))
                    return true;

                var trimmed = value.Trim();
                return trimmed.Equals(LegacySpanishNonNativeText, StringComparison.Ordinal) ||
                       trimmed.Equals(PreviousEnglishNonNativeText, StringComparison.Ordinal) ||
                       trimmed.Equals(PreviousSpanishNonNativeText, StringComparison.Ordinal) ||
                       trimmed.Equals(DefaultEnglishNonNativeText, StringComparison.Ordinal);
            }
        }

        [Browsable(false), Category(EmbedSettings), Description("Mostrará los iconos de tipo de movimiento junto a los movimientos en el Embed Trade (sólo Discord). Requiere que el usuario suba los emojis a su servidor."), DisplayName("¿Mostrar Emojis de Movimientos?")]
        public bool MoveTypeEmojis { get; set; } = true;

        [Browsable(false), Category(EmbedSettings), Description("Mostrará los iconos de Tera Tipo junto a los movimientos en el Embed Trade (sólo Discord). Requiere que el usuario suba los emojis a su servidor."), DisplayName("¿Mostrar Emojis de Tipo Tera?")]
        public bool UseTeraEmojis { get; set; } = true;

        [Browsable(false), Category(EmbedSettings), Description("Si es verdadero, se mostrarán los emojis para las escalas XXXS y XXXL en el Embed Trade."), DisplayName("¿Usar Emojis de Tamaño?")]
        public bool UseScaleEmojis { get; set; } = true;

        [Browsable(false), Category(EmbedSettings), Description("Información personalizada de Emoji para los tipos de movimiento."), DisplayName("Emojis de Movimientos")]
        public List<MoveTypeEmojiInfo> CustomTypeEmojis { get; set; } =
        [
            new(MoveType.Bug),
            new(MoveType.Fire),
            new(MoveType.Flying),
            new(MoveType.Ground),
            new(MoveType.Water),
            new(MoveType.Grass),
            new(MoveType.Ice),
            new(MoveType.Rock),
            new(MoveType.Ghost),
            new(MoveType.Steel),
            new(MoveType.Fighting),
            new(MoveType.Electric),
            new(MoveType.Dragon),
            new(MoveType.Psychic),
            new(MoveType.Dark),
            new(MoveType.Normal),
            new(MoveType.Poison),
            new(MoveType.Fairy),
            new(MoveType.Stellar)
        ];

        [Browsable(false), Category(EmbedSettings), Description("Configuración de emojis para todos los tipos Tera, incluyendo 'Stellar'."), DisplayName("Emojis de Tipo Tera")]
        public List<TeraTypeEmojiInfo> TeraTypeEmojis { get; set; } =
        [
            new(MoveType.Bug),
            new(MoveType.Fire),
            new(MoveType.Flying),
            new(MoveType.Ground),
            new(MoveType.Water),
            new(MoveType.Grass),
            new(MoveType.Ice),
            new(MoveType.Rock),
            new(MoveType.Ghost),
            new(MoveType.Steel),
            new(MoveType.Fighting),
            new(MoveType.Electric),
            new(MoveType.Dragon),
            new(MoveType.Psychic),
            new(MoveType.Dark),
            new(MoveType.Normal),
            new(MoveType.Poison),
            new(MoveType.Fairy),
            new(MoveType.Stellar)
        ];

        [Browsable(false), Category(EmbedSettings), Description("Informacion del emoji para mostrar movimientos Plus aplicables en el embed de Discord."), DisplayName("Emoji de movimiento Plus")]
        public EmojiInfo UsePlusMoveEmoji { get; set; } = new EmojiInfo();

        [Browsable(false), Category(EmbedSettings), TypeConverter(typeof(ExpandableObjectConverter)), Description("Configuración de emojis para las escalas XXXS y XXXL."), DisplayName("Emojis de tamaño")]
        public ScaleEmojisSettings ScaleEmojis { get; set; } = new();

        public class ScaleEmojisSettings
        {
            public override string ToString() => "(Collection)";

            [Description("Emoji para la escala XXXS."), DisplayName("Emoji Escala XXXS")]
            public EmojiInfo ScaleXXXSEmoji { get; set; } = new();

            [Description("Emoji para la escala XXXL."), DisplayName("Emoji Escala XXXL")]
            public EmojiInfo ScaleXXXLEmoji { get; set; } = new();
        }

        [Browsable(false), Category(EmbedSettings), TypeConverter(typeof(ExpandableObjectConverter)), Description("Configuración de emojis para Pokémon Shiny."), DisplayName("Emojis Shiny")]
        public ShinyEmojisSettings ShinyEmojis { get; set; } = new();

        public class ShinyEmojisSettings
        {
            public override string ToString() => "(Collection)";

            [Description("Emoji para Pokémon con Shiny Square."), DisplayName("Emoji Shiny Square")]
            public EmojiInfo ShinySquareEmoji { get; set; } = new();

            [Description("Emoji para Pokémon Shiny normal."), DisplayName("Emoji Shiny Normal")]
            public EmojiInfo ShinyNormalEmoji { get; set; } = new();
        }

        [Browsable(false), Category(EmbedSettings), TypeConverter(typeof(ExpandableObjectConverter)), Description("Configuración de emojis para géneros."), DisplayName("Emojis de Género")]
        public GenderEmojisSettings GenderEmojis { get; set; } = new();

        public class GenderEmojisSettings
        {
            public override string ToString() => "(Collection)";

            [Description("La cadena completa para el emoji de género masculino."), DisplayName("Emoji Masculino")]
            public EmojiInfo MaleEmoji { get; set; } = new();

            [Description("La cadena completa para el emoji de género femenino."), DisplayName("Emoji Femenino")]
            public EmojiInfo FemaleEmoji { get; set; } = new();
        }

        [Browsable(false), Category(EmbedSettings), TypeConverter(typeof(ExpandableObjectConverter)), Description("Configuración de emojis para marcas especiales y estados."), DisplayName("Emojis de Marcas y Estados Especiales")]
        public SpecialMarksEmojisSettings SpecialMarksEmojis { get; set; } = new();

        public class SpecialMarksEmojisSettings
        {
            public override string ToString() => "(Collection)";

            [Description("Emoji utilizado para representar el estado de Regalo Misterioso."), DisplayName("Emoji de Regalo Misterioso")]
            public EmojiInfo MysteryGiftEmoji { get; set; } = new();

            [Description("Emoji utilizado para representar la Marca Alfa."), DisplayName("Emoji de la Marca Alfa")]
            public EmojiInfo AlphaMarkEmoji { get; set; } = new();

            [Description("Emoji utilizado para representar la Marca Imbatible."), DisplayName("Emoji de la Marca Imbatible")]
            public EmojiInfo MightiestMarkEmoji { get; set; } = new();

            [Description("Emoji utilizado para representar Pokémon Alfa en Legends: Arceus."), DisplayName("Emoji de Alfa PLA")]
            public EmojiInfo AlphaPLAEmoji { get; set; } = new();

            [Description("Emoji utilizado para representar Pokémon Gigantamax en Sword & Shield."), DisplayName("Emoji de Gigantamax")]
            public EmojiInfo GigantamaxEmoji { get; set; } = new();
        }

        [Browsable(false), Category(EmbedSettings), Description("Se mostrará la Escala en el Embed Trade (SV y Discord solamente). Requiere que el usuario suba los emojis a su servidor."), DisplayName("Mostrar Tamaño")]
        public bool ShowScale { get; set; } = true;

        [Browsable(false), Category(EmbedSettings), Description("Mostrará el Tera Tipo en el Embed Trade (sólo SV y Discord)."), DisplayName("Mostrar Tera Tipo")]
        public bool ShowTeraType { get; set; } = true;

        [Browsable(false), Category(EmbedSettings), Description("Se mostrará el nivel en el Embed Trade (Discord solamente)."), DisplayName("Mostrar Nivel")]
        public bool ShowLevel { get; set; } = true;

        [Browsable(false), Category(EmbedSettings), Description("Mostrara la Ball en el embed de trade (solo Discord)."), DisplayName("Mostrar Ball")]
        public bool ShowBall { get; set; } = false;

        [Browsable(false), Category(EmbedSettings), Description("Mostrara el nivel de encuentro en el embed de trade (solo Discord)."), DisplayName("Mostrar nivel de encuentro")]
        public bool ShowMetLevel { get; set; } = false;

        [Browsable(false), Category(EmbedSettings), Description("Mostrará MetDate en el Embed Trade (sólo Discord)."), DisplayName("Mostrar Fecha de Encuentro")]
        public bool ShowMetDate { get; set; } = true;

        [Browsable(false), Category(EmbedSettings), Description("Mostrara la ubicacion de encuentro en el embed de trade (solo Discord)."), DisplayName("Mostrar ubicacion de encuentro")]
        public bool ShowMetLocation { get; set; } = false;

        [Browsable(false), Category(EmbedSettings), Description("Se mostrará Habilidad en el Embed Trade (Discord solamente)."), DisplayName("Mostrar Habilidad")]
        public bool ShowAbility { get; set; } = true;

        [Browsable(false), Category(EmbedSettings), Description("Se mostrará la naturaleza en el Embed Trade (Discord solamente)."), DisplayName("Mostrar Naturaleza")]
        public bool ShowNature { get; set; } = true;

        [Browsable(false), Category(EmbedSettings), Description("Mostrará el idioma en el embed de intercambio (solo en Discord)."), DisplayName("Mostrar Idioma")]
        public bool ShowLanguage { get; set; } = true;

        [Browsable(false), Category(EmbedSettings), Description("Mostrará IVs en el Embed Trade (Discord solamente)."), DisplayName("Mostrar IVs")]
        public bool ShowIVs { get; set; } = true;

        [Browsable(false), Category(EmbedSettings), Description("Mostrará los EVs en el Embed Trade (sólo Discord)."), DisplayName("Mostrar EVs")]
        public bool ShowEVs { get; set; } = true;

        [Browsable(false), Category(EmbedSettings), Description("Mostrar GVs en el embed de intercambio (solo Discord)."), DisplayName("Mostrar GVs para PLA")]
        public bool ShowGVs { get; set; } = true;

        [Browsable(false), Category(EmbedSettings), Description("Mostrar AVs en el embed de intercambio (solo Discord)."), DisplayName("Mostrar AVs para LGPE")]
        public bool ShowAVs { get; set; } = true;

        [Browsable(false), Category(EmbedSettings), Description("Mostrará el Rastreador HOME en el embed de intercambio (solo en Discord)."), DisplayName("Mostrar Rastreador")]
        public bool ShowTracker { get; set; } = true;
    }

    [Category(VGCPastesConfig), TypeConverter(typeof(CategoryConverter<VGCPastesCategory>))]
    public class VGCPastesCategory
    {
        public override string ToString() => "Ajustes de configuración de VGCPastes";

        [Category(VGCPastesConfig), Description("Permitir a los usuarios solicitar y generar equipos utilizando la hoja de cálculo VGCPastes."), DisplayName("Permitir solicitudes VGCPaste")]
        public bool AllowRequests { get; set; } = true;

        [Category(VGCPastesConfig), Description("GID de la pestaña de la hoja de cálculo de la que desea extraer datos. Sugerencia: https://docs.google.com/spreadsheets/d/ID/gid=1837599752"), DisplayName("GID de la hoja de cálculo")]
        public int GID { get; set; } = 1837599752;
    }

    [Category(Miscellaneous), Description("Apaga la pantalla de la Switch durante las operaciones"), DisplayName("Apagar Pantalla")]
    public bool ScreenOff { get; set; } = false;

    /// <summary>
    /// Gets a random trade code based on the range settings.
    /// </summary>
    public int GetRandomTradeCode() => Util.Rand.Next(TradeConfiguration.MinTradeCode, TradeConfiguration.MaxTradeCode + 1);

    public static List<Pictocodes> GetRandomLGTradeCode(bool randomtrade = false)
    {
        var lgcode = new List<Pictocodes>();
        if (randomtrade)
        {
            for (int i = 0; i <= 2; i++)
            {
                // code.Add((pictocodes)Util.Rand.Next(10));
                lgcode.Add(Pictocodes.Pikachu);
            }
        }
        else
        {
            for (int i = 0; i <= 2; i++)
            {
                lgcode.Add((Pictocodes)Util.Rand.Next(10));

                // code.Add(pictocodes.Pikachu);
            }
        }
        return lgcode;
    }

    [Category(CountStats), TypeConverter(typeof(CategoryConverter<CountStatsSettingsCategory>))]
    public class CountStatsSettingsCategory
    {
        public override string ToString() => "Estadísticas del recuento de trades";

        private int _completedSurprise;

        private int _completedDistribution;

        private int _completedTrades;

        private int _completedSeedChecks;

        private int _completedClones;

        private int _completedDumps;

        private int _completedFixOTs;

        [Category(CountStats), Description("Trades sorpresas finalizados")]
        public int CompletedSurprise
        {
            get => _completedSurprise;
            set => _completedSurprise = value;
        }

        [Category(), Description("Trades de enlaces finalizados (distribución)")]
        public int CompletedDistribution
        {
            get => _completedDistribution;
            set => _completedDistribution = value;
        }

        [Category(CountStats), Description("Trades de enlace completados (usuario específico)")]
        public int CompletedTrades
        {
            get => _completedTrades;
            set => _completedTrades = value;
        }

        [Category(CountStats), Description("Trades FixOT completados (Usuario específico)")]
        public int CompletedFixOTs
        {
            get => _completedFixOTs;
            set => _completedFixOTs = value;
        }

        [Browsable(false)]
        [Category(CountStats), Description("Trades de control de semillas finalizadas")]
        public int CompletedSeedChecks
        {
            get => _completedSeedChecks;
            set => _completedSeedChecks = value;
        }

        [Category(CountStats), Description("Trades de clonacion completados (usuario específico)")]
        public int CompletedClones
        {
            get => _completedClones;
            set => _completedClones = value;
        }

        [Category(CountStats), Description("Trades de Dumps finalizados (usuario específico)")]
        public int CompletedDumps
        {
            get => _completedDumps;
            set => _completedDumps = value;
        }

    [Description("Controla si los recuentos de estadísticas de operaciones se emiten durante las comprobaciones de estado."), DisplayName("Emitir Recuentos al Comprobar Estado")]
        public bool EmitCountsOnStatusCheck { get; set; }

        public void AddCompletedTrade() => Interlocked.Increment(ref _completedTrades);

        public void AddCompletedSeedCheck() => Interlocked.Increment(ref _completedSeedChecks);

        public void AddCompletedSurprise() => Interlocked.Increment(ref _completedSurprise);

        public void AddCompletedDistribution() => Interlocked.Increment(ref _completedDistribution);

        public void AddCompletedDumps() => Interlocked.Increment(ref _completedDumps);

        public void AddCompletedClones() => Interlocked.Increment(ref _completedClones);

        public void AddCompletedFixOTs() => Interlocked.Increment(ref _completedFixOTs);

        public IEnumerable<string> GetNonZeroCounts()
        {
            if (!EmitCountsOnStatusCheck)
                yield break;
            if (CompletedSeedChecks != 0)
                yield return $"Seed Check Trades: {CompletedSeedChecks}";
            if (CompletedClones != 0)
                yield return $"Clone Trades: {CompletedClones}";
            if (CompletedDumps != 0)
                yield return $"Dump Trades: {CompletedDumps}";
            if (CompletedTrades != 0)
                yield return $"Link Trades: {CompletedTrades}";
            if (CompletedDistribution != 0)
                yield return $"Distribution Trades: {CompletedDistribution}";
            if (CompletedFixOTs != 0)
                yield return $"FixOT Trades: {CompletedFixOTs}";
            if (CompletedSurprise != 0)
                yield return $"Surprise Trades: {CompletedSurprise}";
        }
    }

    [Description("Controla si los recuentos de estadísticas de operaciones se emiten durante las comprobaciones de estado."), DisplayName("Emitir Recuentos al Comprobar Estado")]
    public bool EmitCountsOnStatusCheck
    {
        get => CountStatsSettings.EmitCountsOnStatusCheck;
        set => CountStatsSettings.EmitCountsOnStatusCheck = value;
    }

    public IEnumerable<string> GetNonZeroCounts()
    {
        // Delegating the call to CountStatsSettingsCategory
        return CountStatsSettings.GetNonZeroCounts();
    }

    public class CategoryConverter<T> : TypeConverter
    {
        public override bool GetPropertiesSupported(ITypeDescriptorContext? context) => true;

        public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext? context, object value, Attribute[]? attributes)
        {
            var properties = TypeDescriptor.GetProperties(typeof(T), attributes ?? []);
            var browsable = properties.Cast<PropertyDescriptor>().Where(p => p.IsBrowsable).ToArray();
            return new PropertyDescriptorCollection(browsable, true);
        }

        public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType) => destinationType != typeof(string) && base.CanConvertTo(context, destinationType);
    }

    public enum ImageSize
    {
        Size256x256,

        Size128x128
    }

    public enum MoveType
    {
        Normal,
        Fighting,
        Flying,
        Poison,
        Ground,
        Rock,
        Bug,
        Ghost,
        Steel,
        Fire,
        Water,
        Grass,
        Electric,
        Psychic,
        Ice,
        Dragon,
        Dark,
        Fairy,
        Stellar
    }

    public class MoveTypeEmojiInfo
    {
        [Description("El tipo Tera.")]
        public MoveType MoveType { get; set; }
        [Description("La cadena de emojis de Discord para este tipo de tera.")]
        public string EmojiCode { get; set; } = string.Empty;
        public MoveTypeEmojiInfo()
        { }
        public MoveTypeEmojiInfo(MoveType moveType)
        {
            MoveType = moveType;
            EmojiCode = string.Empty;
        }
        public override string ToString()
        {
            if (string.IsNullOrEmpty(EmojiCode))
                return MoveType.ToString();
            return $"{EmojiCode}";
        }
    }

    public class TeraTypeEmojiInfo
    {
        [Description("El tipo Tera.")]
        public MoveType MoveType { get; set; }
        [Description("La cadena de emojis de Discord para este tipo de tera.")]
        public string EmojiCode { get; set; }
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public TeraTypeEmojiInfo()
        { }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public TeraTypeEmojiInfo(MoveType teraType)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
            MoveType = teraType;
        }
        public override string ToString()
        {
            if (string.IsNullOrEmpty(EmojiCode))
                return MoveType.ToString();
            return $"{EmojiCode}";
        }
    }
}
