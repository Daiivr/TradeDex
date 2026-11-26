using SysBot.Base;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.IO;
using System.Text.Json.Serialization;

namespace SysBot.Pokemon;

public class TradeCodeStorage
{
    private const string FileName = "tradecodes.json";
    private const int MaxRecentTradeFiles = 5;
    private Dictionary<ulong, TradeCodeDetails> _tradeCodeDetails;

    public class TradeCodeDetails
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        [JsonConverter(typeof(TradeCodeStringConverter))]
        public string Code { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public string? OT { get; set; }
        public int TID { get; set; }
        public int SID { get; set; }
        public int? Language { get; set; }
        public byte? Gender { get; set; }
        public string? Quote { get; set; }
        public string? LastTrade { get; set; }
        public DateTimeOffset? LastTradeAt { get; set; }
        public int TradeCount { get; set; }
        public List<ReceivedTradeFile> RecentTradeFiles { get; set; } = new();
    }

    public sealed class ReceivedTradeFile
    {
        public string Id { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string Pokemon { get; set; } = string.Empty;
        public string DataBase64 { get; set; } = string.Empty;
        public DateTimeOffset ReceivedAt { get; set; }
    }

    private sealed class TradeCodeStringConverter : JsonConverter<string>
    {
        public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.TokenType switch
            {
                JsonTokenType.String => NormalizeCode(reader.GetString()),
                JsonTokenType.Number => reader.TryGetInt32(out var value) ? value.ToString("D8") : NormalizeCode(reader.GetDouble().ToString()),
                JsonTokenType.Null => string.Empty,
                _ => throw new JsonException($"Unsupported trade code token: {reader.TokenType}")
            };
        }

        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(NormalizeCode(value));
        }
    }

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public TradeCodeStorage()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    {
        LoadFromFile();
    }

    public int GetTradeCode(ulong trainerID)
    {
        LoadFromFile();

        if (_tradeCodeDetails.TryGetValue(trainerID, out var details))
        {
            if (!int.TryParse(NormalizeCode(details.Code), out var code))
                code = GenerateRandomTradeCode();

            details.Code = code.ToString("D8");
            details.TradeCount++;
            SaveToFile();
            return code;
        }
        else
        {
            var code = GenerateRandomTradeCode();
            _tradeCodeDetails[trainerID] = new TradeCodeDetails { Code = code.ToString("D8"), TradeCount = 1 };
            SaveToFile();
            return code;
        }
    }

    public bool SetTradeCode(ulong trainerID, int tradeCode)
    {
        // Convierte el entero a string aquí
        string tradeCodeStr = tradeCode.ToString("D8"); // Formatea como un número de 8 dígitos

        if (_tradeCodeDetails.ContainsKey(trainerID))
        {
            return false;
        }

        _tradeCodeDetails[trainerID] = new TradeCodeDetails { Code = tradeCodeStr, TradeCount = 1 };
        SaveToFile();
        return true;
    }

    public bool UpdateTradeCode(ulong trainerID, int newTradeCode)
    {
        if (!_tradeCodeDetails.ContainsKey(trainerID))
        {
            return false;
        }

        // Convierte el entero a string aquí también
        string newTradeCodeStr = newTradeCode.ToString("D8"); // Asegura que tenga 8 dígitos

        _tradeCodeDetails[trainerID].Code = newTradeCodeStr;
        SaveToFile();
        return true;
    }

    private static int GenerateRandomTradeCode()
    {
        var settings = new TradeSettings();
        return settings.GetRandomTradeCode();
    }

    private static string NormalizeCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return string.Empty;

        var digits = code.Trim();
        return int.TryParse(digits, out var value) ? value.ToString("D8") : digits;
    }

    private void LoadFromFile()
    {
        if (File.Exists(FileName))
        {
            try
            {
                string json = File.ReadAllText(FileName);
                _tradeCodeDetails = JsonSerializer.Deserialize<Dictionary<ulong, TradeCodeDetails>>(json, SerializerOptions)
                    ?? new Dictionary<ulong, TradeCodeDetails>();
            }
            catch (JsonException ex)
            {
                LogUtil.LogError($"Failed to load {FileName}: {ex.Message}", "TradeCodeStorage");
                _tradeCodeDetails = new Dictionary<ulong, TradeCodeDetails>();
            }
        }
        else
        {
            _tradeCodeDetails = new Dictionary<ulong, TradeCodeDetails>();
        }
    }

    public bool DeleteTradeCode(ulong trainerID)
    {
        LoadFromFile();

        if (_tradeCodeDetails.Remove(trainerID))
        {
            SaveToFile();
            return true;
        }
        return false;
    }

    private void SaveToFile()
    {
        try
        {
            string json = JsonSerializer.Serialize(_tradeCodeDetails, SerializerOptions);
            File.WriteAllText(FileName, json);
        }
        catch (IOException ex)
        {
            LogUtil.LogInfo("TradeCodeStorage", $"Error al guardar códigos comerciales para archivar: {ex.Message}");
        }
        catch (UnauthorizedAccessException ex)
        {
            LogUtil.LogInfo("TradeCodeStorage", $"Acceso denegado al guardar códigos comerciales en el archivo: {ex.Message}");
        }
        catch (Exception ex)
        {
            LogUtil.LogInfo("TradeCodeStorage", $"Se produjo un error al guardar códigos comerciales para archivar: {ex.Message}");
        }
    }

    public int GetTradeCount(ulong trainerID)
    {
        LoadFromFile();

        if (_tradeCodeDetails.TryGetValue(trainerID, out var details))
        {
            return details.TradeCount;
        }
        return 0;
    }

    public TradeCodeDetails? GetTradeDetails(ulong trainerID)
    {
        LoadFromFile();

        if (_tradeCodeDetails.TryGetValue(trainerID, out var details))
        {
            return details;
        }
        return null;
    }

    public IReadOnlyDictionary<ulong, TradeCodeDetails> GetAllTradeDetails()
    {
        LoadFromFile();
        return new Dictionary<ulong, TradeCodeDetails>(_tradeCodeDetails);
    }

    public void UpdateQuote(ulong trainerID, string? quote)
    {
        LoadFromFile();

        if (!_tradeCodeDetails.TryGetValue(trainerID, out var details))
        {
            details = new TradeCodeDetails { Code = GenerateRandomTradeCode().ToString("D8") };
            _tradeCodeDetails[trainerID] = details;
        }

        details.Quote = string.IsNullOrWhiteSpace(quote) ? null : quote.Trim();
        SaveToFile();
    }

    public void UpdateTradeDetails(ulong trainerID, string ot, int tid, int sid, string? quote = null, int? language = null, byte? gender = null)
    {
        LoadFromFile();

        if (_tradeCodeDetails.TryGetValue(trainerID, out var details))
        {
            details.OT = ot;
            details.TID = tid;
            details.SID = sid;
            if (quote is not null)
                details.Quote = quote;
            if (language.HasValue)
                details.Language = language;
            if (gender.HasValue)
                details.Gender = gender;
            SaveToFile();
        }
    }

    public void RecordLastTrade(ulong trainerID, string lastTrade)
    {
        LoadFromFile();

        if (!_tradeCodeDetails.TryGetValue(trainerID, out var details))
        {
            details = new TradeCodeDetails { Code = GenerateRandomTradeCode().ToString("D8") };
            _tradeCodeDetails[trainerID] = details;
        }

        details.LastTrade = lastTrade;
        details.LastTradeAt = DateTimeOffset.UtcNow;
        SaveToFile();
    }

    public void RecordReceivedTradeFile(ulong trainerID, string fileName, string pokemon, byte[] data)
    {
        if (data.Length == 0)
            return;

        LoadFromFile();

        if (!_tradeCodeDetails.TryGetValue(trainerID, out var details))
        {
            details = new TradeCodeDetails { Code = GenerateRandomTradeCode().ToString("D8") };
            _tradeCodeDetails[trainerID] = details;
        }

        details.RecentTradeFiles ??= new List<ReceivedTradeFile>();
        details.RecentTradeFiles.Insert(0, new ReceivedTradeFile
        {
            Id = Guid.NewGuid().ToString("N"),
            FileName = Path.GetFileName(fileName),
            Pokemon = pokemon,
            DataBase64 = Convert.ToBase64String(data),
            ReceivedAt = DateTimeOffset.UtcNow
        });

        if (details.RecentTradeFiles.Count > MaxRecentTradeFiles)
            details.RecentTradeFiles.RemoveRange(MaxRecentTradeFiles, details.RecentTradeFiles.Count - MaxRecentTradeFiles);

        SaveToFile();
    }

    public IReadOnlyList<ReceivedTradeFile> GetRecentTradeFiles(ulong trainerID)
    {
        LoadFromFile();

        if (!_tradeCodeDetails.TryGetValue(trainerID, out var details) || details.RecentTradeFiles == null)
            return Array.Empty<ReceivedTradeFile>();

        return details.RecentTradeFiles.GetRange(0, Math.Min(MaxRecentTradeFiles, details.RecentTradeFiles.Count));
    }

    public ReceivedTradeFile? GetRecentTradeFile(ulong trainerID, string id)
    {
        LoadFromFile();

        if (!_tradeCodeDetails.TryGetValue(trainerID, out var details) || details.RecentTradeFiles == null)
            return null;

        return details.RecentTradeFiles.Find(file => string.Equals(file.Id, id, StringComparison.Ordinal));
    }
}
