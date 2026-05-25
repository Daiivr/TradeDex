using PKHeX.Core;
using SysBot.Base;
using SysBot.Pokemon.Helpers;
using SysBot.Pokemon.Localization;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace SysBot.Pokemon.WinForms.WebApi;

public partial class BotServer
{
    private const string TradeSessionCookie = "tradedex_session";
    private static readonly HttpClient DiscordHttp = new();
    private static readonly ConcurrentDictionary<string, WebTradeSession> TradeSessions = new();
    private static readonly ConcurrentDictionary<string, DateTimeOffset> OAuthStates = new();
    private static int _webTradeId;

    private sealed class WebTradeSession
    {
        public ulong DiscordId { get; init; }
        public string Username { get; init; } = string.Empty;
        public string Avatar { get; init; } = string.Empty;
        public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
        public WebTradeStatus? ActiveTrade { get; set; }
    }

    private sealed class WebTradeStatus
    {
        public int UniqueTradeId { get; init; }
        public int Code { get; init; }
        public string Pokemon { get; init; } = string.Empty;
        public string SpriteUrl { get; init; } = string.Empty;
        public string Message { get; set; } = "Queued";
        public string State { get; set; } = "queued";
        public string BotName { get; set; } = string.Empty;
        public string PartnerTrainerName { get; set; } = string.Empty;
        public string PartnerTid { get; set; } = string.Empty;
        public string PartnerSid { get; set; } = string.Empty;
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    }

    private sealed class DiscordTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;
    }

    private sealed class DiscordUserResponse
    {
        public string Id { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string? Avatar { get; set; }
        [JsonPropertyName("global_name")]
        public string? GlobalName { get; set; }
    }

    private sealed class WebTradeSubmitRequest
    {
        public string ShowdownSet { get; set; } = string.Empty;
        public string TrainerName { get; set; } = string.Empty;
        public bool IgnoreAutoOT { get; set; }
        public string PkmFileName { get; set; } = string.Empty;
        public string PkmFileBase64 { get; set; } = string.Empty;
    }

    private sealed class WebTradeCodeRequest
    {
        public string TradeCode { get; set; } = string.Empty;
    }

    private sealed class PkmLegalityException(IReadOnlyList<string> issues) : Exception("PKHeX reports this Pokemon is not legal.")
    {
        public IReadOnlyList<string> Issues { get; } = issues;
    }

    private string GetTradeAuthConfig(HttpListenerRequest request)
    {
        var settings = Main.Config?.Hub.WebServer;
        var configured = !string.IsNullOrWhiteSpace(settings?.DiscordOAuthClientId) &&
            !string.IsNullOrWhiteSpace(settings.DiscordOAuthClientSecret);

        return JsonSerializer.Serialize(new
        {
            discordConfigured = configured,
            loginUrl = "/api/trade/auth/login",
            redirectUri = GetDiscordRedirectUri(request)
        }, JsonOptions);
    }

    private string StartDiscordLogin(HttpListenerRequest request, HttpListenerResponse response)
    {
        var settings = Main.Config?.Hub.WebServer;
        if (settings == null || string.IsNullOrWhiteSpace(settings.DiscordOAuthClientId) ||
            string.IsNullOrWhiteSpace(settings.DiscordOAuthClientSecret))
        {
            return CreateErrorResponse("Discord OAuth is not configured in WebServer settings.");
        }

        var state = CreateToken(24);
        OAuthStates[state] = DateTimeOffset.UtcNow.AddMinutes(10);
        CleanupExpiredOAuthStates();

        var redirect = GetDiscordRedirectUri(request);
        var url = "https://discord.com/oauth2/authorize" +
            $"?client_id={WebUtility.UrlEncode(settings.DiscordOAuthClientId)}" +
            "&response_type=code" +
            $"&redirect_uri={WebUtility.UrlEncode(redirect)}" +
            "&scope=identify" +
            $"&state={WebUtility.UrlEncode(state)}";

        return JsonSerializer.Serialize(new { success = true, url }, JsonOptions);
    }

    private async Task<string> CompleteDiscordLogin(HttpListenerRequest request, HttpListenerResponse response)
    {
        var query = ParseQuery(request.Url?.Query);
        if (!query.TryGetValue("code", out var code) || !query.TryGetValue("state", out var state))
            return BuildAuthCallbackHtml(false, "invalid-response");

        if (!OAuthStates.TryRemove(state, out var expiresAt) || expiresAt < DateTimeOffset.UtcNow)
            return BuildAuthCallbackHtml(false, "session-expired");

        var settings = Main.Config?.Hub.WebServer;
        if (settings == null)
            return BuildAuthCallbackHtml(false, "server-settings");

        try
        {
            using var tokenContent = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = settings.DiscordOAuthClientId,
                ["client_secret"] = settings.DiscordOAuthClientSecret,
                ["grant_type"] = "authorization_code",
                ["code"] = code,
                ["redirect_uri"] = GetDiscordRedirectUri(request)
            });

            using var tokenResponse = await DiscordHttp.PostAsync("https://discord.com/api/oauth2/token", tokenContent).ConfigureAwait(false);
            if (!tokenResponse.IsSuccessStatusCode)
                return BuildAuthCallbackHtml(false, "rejected-code");

            var token = await JsonSerializer.DeserializeAsync<DiscordTokenResponse>(
                await tokenResponse.Content.ReadAsStreamAsync().ConfigureAwait(false), JsonOptions).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(token?.AccessToken))
                return BuildAuthCallbackHtml(false, "no-token");

            using var userRequest = new HttpRequestMessage(HttpMethod.Get, "https://discord.com/api/users/@me");
            userRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.AccessToken);
            using var userResponse = await DiscordHttp.SendAsync(userRequest).ConfigureAwait(false);
            if (!userResponse.IsSuccessStatusCode)
                return BuildAuthCallbackHtml(false, "no-profile");

            var user = await JsonSerializer.DeserializeAsync<DiscordUserResponse>(
                await userResponse.Content.ReadAsStreamAsync().ConfigureAwait(false), JsonOptions).ConfigureAwait(false);

            if (user == null || !ulong.TryParse(user.Id, out var discordId))
                return BuildAuthCallbackHtml(false, "invalid-user");

            var sessionId = CreateToken(32);
            TradeSessions[sessionId] = new WebTradeSession
            {
                DiscordId = discordId,
                Username = string.IsNullOrWhiteSpace(user.GlobalName) ? user.Username : user.GlobalName,
                Avatar = string.IsNullOrWhiteSpace(user.Avatar)
                    ? string.Empty
                    : $"https://cdn.discordapp.com/avatars/{user.Id}/{user.Avatar}.png"
            };

            response.Headers.Add("Set-Cookie", $"{TradeSessionCookie}={sessionId}; Path=/; HttpOnly; SameSite=Lax; Max-Age=2592000");
            return BuildAuthCallbackHtml(true, "login-complete");
        }
        catch (Exception ex)
        {
            LogUtil.LogError($"Discord OAuth callback failed: {ex.Message}", "WebTrade");
            return BuildAuthCallbackHtml(false, "login-failed");
        }
    }

    private string GetCurrentTradeUser(HttpListenerRequest request)
    {
        if (!TryGetTradeSession(request, out var session))
            return JsonSerializer.Serialize(new { authenticated = false }, JsonOptions);

        return JsonSerializer.Serialize(new
        {
            authenticated = true,
            isAdmin = IsTradeWebAdmin(session.DiscordId),
            user = new
            {
                id = session.DiscordId.ToString(),
                username = session.Username,
                avatar = session.Avatar
            }
        }, JsonOptions);
    }

    private static bool IsTradeWebAdmin(ulong discordId)
    {
        var configured = Main.Config?.Hub.WebServer.AdminID;
        return ulong.TryParse(configured, out var adminId) && adminId == discordId;
    }

    private string LogoutTradeUser(HttpListenerResponse response)
    {
        response.Headers.Add("Set-Cookie", $"{TradeSessionCookie}=; Path=/; HttpOnly; SameSite=Lax; Max-Age=0");
        return JsonSerializer.Serialize(new { success = true }, JsonOptions);
    }

    private string GetTradeProfile(HttpListenerRequest request)
    {
        if (!TryGetTradeSession(request, out var session))
            return CreateErrorResponse("Please login with Discord first.");

        var storage = new TradeCodeStorage();
        var details = storage.GetTradeDetails(session.DiscordId);
        return JsonSerializer.Serialize(new
        {
            success = true,
            discordId = session.DiscordId.ToString(),
            username = session.Username,
            tradeCode = details?.Code,
            tradeCount = storage.GetTradeCount(session.DiscordId),
            ot = details?.OT,
            tid = details?.TID,
            sid = details?.SID,
            lastTrade = CleanPokemonDisplayName(details?.LastTrade),
            lastTradeAt = details?.LastTradeAt
        }, JsonOptions);
    }

    private string GetRecentTradeFiles(HttpListenerRequest request)
    {
        if (!TryGetTradeSession(request, out var session))
            return CreateErrorResponse("Please login with Discord first.");

        var files = new TradeCodeStorage().GetRecentTradeFiles(session.DiscordId)
            .Select(file => new
            {
                file.Id,
                file.FileName,
                file.Pokemon,
                file.ReceivedAt
            });

        return JsonSerializer.Serialize(new { success = true, files }, JsonOptions);
    }

    private (int statusCode, object? content, string contentType) DownloadRecentTradeFile(
        HttpListenerRequest request,
        HttpListenerResponse response,
        string path)
    {
        if (!TryGetTradeSession(request, out var session))
            return (401, CreateErrorResponse("Please login with Discord first."), "application/json");

        var id = path["/api/trade/files/".Length..].Trim('/');
        if (id.Length != 32 || id.Any(character => !Uri.IsHexDigit(character)))
            return (404, CreateErrorResponse("Trade file not found."), "application/json");

        var file = new TradeCodeStorage().GetRecentTradeFile(session.DiscordId, id);
        if (file == null || string.IsNullOrWhiteSpace(file.DataBase64))
            return (404, CreateErrorResponse("Trade file not found."), "application/json");

        byte[] data;
        try
        {
            data = Convert.FromBase64String(file.DataBase64);
        }
        catch (FormatException)
        {
            return (404, CreateErrorResponse("Trade file not found."), "application/json");
        }

        var fileName = new string(Path.GetFileName(file.FileName)
            .Where(character => character >= ' ' && character <= '~' && character != '"' && character != '\\')
            .ToArray());
        if (string.IsNullOrWhiteSpace(fileName))
            fileName = $"trade-{id}.pkm";

        response.Headers.Add("Content-Disposition", $"attachment; filename=\"{fileName}\"");
        response.Headers.Add("Cache-Control", "no-store");
        return (200, data, "application/octet-stream");
    }

    private async Task<string> SaveTradeCode(HttpListenerRequest request)
    {
        if (!TryGetTradeSession(request, out var session))
            return CreateErrorResponse("Please login with Discord first.");

        var payload = await DeserializeTradeCodeRequest(request).ConfigureAwait(false);
        var digits = new string((payload?.TradeCode ?? string.Empty).Where(char.IsDigit).ToArray());
        if (digits.Length != 8 || !int.TryParse(digits, out var code))
            return CreateErrorResponse("Trade code must be exactly eight digits.");

        var storage = new TradeCodeStorage();
        var saved = storage.GetTradeDetails(session.DiscordId) == null
            ? storage.SetTradeCode(session.DiscordId, code)
            : storage.UpdateTradeCode(session.DiscordId, code);

        return saved
            ? JsonSerializer.Serialize(new { success = true, tradeCode = digits }, JsonOptions)
            : CreateErrorResponse("Trade code could not be saved.");
    }

    private string DeleteTradeCode(HttpListenerRequest request)
    {
        if (!TryGetTradeSession(request, out var session))
            return CreateErrorResponse("Please login with Discord first.");

        var deleted = new TradeCodeStorage().DeleteTradeCode(session.DiscordId);
        return JsonSerializer.Serialize(new { success = true, deleted }, JsonOptions);
    }

    private string GetWebTradeQueueStatus(HttpListenerRequest request)
    {
        if (!TryGetTradeSession(request, out var session))
            return CreateErrorResponse("Please login with Discord first.");

        return Main.Config?.Mode switch
        {
            ProgramMode.SWSH => GetQueueStatusFor<PK8>(session),
            ProgramMode.BDSP => GetQueueStatusFor<PB8>(session),
            ProgramMode.LA => GetQueueStatusFor<PA8>(session),
            ProgramMode.SV => GetQueueStatusFor<PK9>(session),
            ProgramMode.LGPE => GetQueueStatusFor<PB7>(session),
            ProgramMode.PLZA => GetQueueStatusFor<PA9>(session),
            _ => CreateErrorResponse("Unsupported trade mode.")
        };
    }

    private async Task<string> SubmitWebTrade(HttpListenerRequest request)
    {
        if (!TryGetTradeSession(request, out var session))
            return CreateErrorResponse("Please login with Discord first.");

        var payload = await DeserializeWebTradeRequest(request).ConfigureAwait(false);
        if (payload == null)
            return CreateErrorResponse("Invalid trade request.");

        return Main.Config?.Mode switch
        {
            ProgramMode.SWSH => await SubmitWebTradeFor<PK8>(session, payload).ConfigureAwait(false),
            ProgramMode.BDSP => await SubmitWebTradeFor<PB8>(session, payload).ConfigureAwait(false),
            ProgramMode.LA => await SubmitWebTradeFor<PA8>(session, payload).ConfigureAwait(false),
            ProgramMode.SV => await SubmitWebTradeFor<PK9>(session, payload).ConfigureAwait(false),
            ProgramMode.LGPE => await SubmitWebTradeFor<PB7>(session, payload).ConfigureAwait(false),
            ProgramMode.PLZA => await SubmitWebTradeFor<PA9>(session, payload).ConfigureAwait(false),
            _ => CreateErrorResponse("Unsupported trade mode.")
        };
    }

    private string CancelWebTrade(HttpListenerRequest request)
    {
        if (!TryGetTradeSession(request, out var session))
            return CreateErrorResponse("Please login with Discord first.");

        return Main.Config?.Mode switch
        {
            ProgramMode.SWSH => CancelWebTradeFor<PK8>(session),
            ProgramMode.BDSP => CancelWebTradeFor<PB8>(session),
            ProgramMode.LA => CancelWebTradeFor<PA8>(session),
            ProgramMode.SV => CancelWebTradeFor<PK9>(session),
            ProgramMode.LGPE => CancelWebTradeFor<PB7>(session),
            ProgramMode.PLZA => CancelWebTradeFor<PA9>(session),
            _ => CreateErrorResponse("Unsupported trade mode.")
        };
    }

    private async Task<string> SubmitWebTradeFor<T>(WebTradeSession session, WebTradeSubmitRequest payload) where T : PKM, new()
    {
        if (!TryGetHub<T>(out var hub))
            return CreateErrorResponse("The trade runner is not ready yet.");

        if (!hub.Queues.Info.GetCanQueue())
            return CreateErrorResponse("The trade queue is currently closed or no trade bots are ready.");

        T pk;
        var sourceName = string.Empty;
        try
        {
            if (!string.IsNullOrWhiteSpace(payload.PkmFileBase64))
            {
                pk = ReadUploadedPkmFile<T>(payload.PkmFileBase64, payload.PkmFileName, out sourceName);
            }
            else
            {
                var showdown = payload.ShowdownSet.Trim();
                if (showdown.Length < 2 || showdown.Length > 20000)
                    return CreateErrorResponse("Please provide a valid Pokemon Showdown set or upload a PKHeX file.");

                var set = new ShowdownSet(showdown);
                if (set.Species == 0)
                    return CreateErrorResponse("Unable to parse the Pokemon species from the Showdown set.");

                var sav = AutoLegalityWrapper.GetTrainerInfo<T>();
                var generated = sav.GetLegal(AutoLegalityWrapper.GetTemplate(set), out var result);
                if (generated is not T typed)
                {
                    LogUtil.LogError($"Generated Pokemon type mismatch: {generated?.GetType().Name ?? "null"} ({result})", "WebTrade");
                    return CreateErrorResponse("The generated Pokemon does not match the current bot mode.");
                }

                pk = typed;
            }

            pk.RefreshChecksum();
        }
        catch (PkmLegalityException ex)
        {
            LogUtil.LogError($"Web trade legality failed: {string.Join("; ", ex.Issues)}", "WebTrade");
            return CreateLegalityErrorResponse(ex.Issues);
        }
        catch (Exception ex)
        {
            LogUtil.LogError($"Web trade generation failed: {ex.Message}", "WebTrade");
            return CreateErrorResponse($"Could not read that Pokemon: {ex.Message}");
        }

        var code = hub.Queues.Info.GetRandomTradeCode(session.DiscordId);
        var trainerName = string.IsNullOrWhiteSpace(payload.TrainerName) ? session.Username : payload.TrainerName.Trim();
        if (trainerName.Length > 32)
            trainerName = trainerName[..32];

        var trainer = new PokeTradeTrainerInfo(trainerName, session.DiscordId);
        var uniqueTradeId = Interlocked.Increment(ref _webTradeId);
        var pokemonName = CleanPokemonDisplayName(string.IsNullOrWhiteSpace(sourceName) ? GameInfo.Strings.Species[pk.Species] : sourceName);
        var spriteUrl = TradeExtensions<T>.PokeImg(pk, false, false, TradeSettings.ImageSize.Size128x128);
        var notifier = new WebTradeNotifier<T>(session, pokemonName, spriteUrl, code, uniqueTradeId);
        var detail = new PokeTradeDetail<T>(
            pk,
            trainer,
            notifier,
            PokeTradeType.Specific,
            code,
            false,
            null,
            1,
            1,
            false,
            false,
            uniqueTradeId,
            payload.IgnoreAutoOT);

        var trade = new TradeEntry<T>(detail, session.DiscordId, PokeRoutineType.LinkTrade, session.Username, uniqueTradeId);
        var added = hub.Queues.Info.AddToTradeQueue(trade, session.DiscordId);
        if (added != QueueResultAdd.Added)
            return CreateErrorResponse(added switch
            {
                QueueResultAdd.AlreadyInQueue => "You already have a trade in the queue.",
                QueueResultAdd.QueueFull => "The trade queue is full.",
                QueueResultAdd.NotAllowedItem => "That Pokemon is holding an item that cannot be traded.",
                _ => $"The trade could not be queued: {added}"
            });

        new TradeCodeStorage().RecordLastTrade(session.DiscordId, pokemonName);
        session.ActiveTrade = new WebTradeStatus
        {
            UniqueTradeId = uniqueTradeId,
            Code = code,
            Pokemon = pokemonName,
            SpriteUrl = spriteUrl,
            Message = "Added to the trade queue.",
            State = "queued"
        };

        await notifier.SendInitialQueueUpdate().ConfigureAwait(false);
        return GetQueueStatusFor<T>(session);
    }

    private static T ReadUploadedPkmFile<T>(string base64, string fileName, out string displayName) where T : PKM, new()
    {
        displayName = string.Empty;

        var safeName = Path.GetFileName(fileName);
        if (string.IsNullOrWhiteSpace(safeName))
            throw new InvalidDataException("Uploaded PKHeX file is missing a file name.");

        var extension = Path.GetExtension(safeName).ToLowerInvariant();
        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".pk3", ".pk4", ".pk5", ".pk6", ".pk7", ".pb7", ".pk8", ".pb8", ".pa8", ".pk9", ".pa9"
        };

        if (!allowed.Contains(extension))
            throw new InvalidDataException("Unsupported file type. Upload a PKHeX .pk*, .pb*, or .pa* file.");

        byte[] data;
        try
        {
            data = Convert.FromBase64String(base64);
        }
        catch (FormatException)
        {
            throw new InvalidDataException("Uploaded file data is not valid.");
        }

        if (data.Length is <= 0 or > 4096)
            throw new InvalidDataException("Uploaded PKHeX file size is invalid.");

        var context = EntityFileExtension.GetContextFromExtension(safeName, EntityContext.None);
        var raw = EntityFormat.GetFromBytes(data, context);
        if (raw == null || raw.Species <= 0)
            throw new InvalidDataException("Invalid or unreadable PKHeX file.");

        var legality = new LegalityAnalysis(raw);
        if (!legality.Valid)
            throw new PkmLegalityException(GetLegalityIssues(legality.Report()));

        PKM converted = raw;
        if (raw is not T)
        {
            converted = EntityConverter.ConvertToType(raw, typeof(T), out _) ??
                throw new InvalidDataException("This PKHeX file cannot be converted to the current bot mode.");
        }

        if (converted is not T typed)
            throw new InvalidDataException("This PKHeX file does not match the current bot mode.");

        displayName = GameInfo.Strings.Species[typed.Species];
        return typed;
    }

    private static string CreateLegalityErrorResponse(IReadOnlyList<string> issues)
    {
        return JsonSerializer.Serialize(new
        {
            success = false,
            errorCode = "pkm_legal",
            error = "PKHeX reports this Pokemon is not legal.",
            issues
        }, JsonOptions);
    }

    private static IReadOnlyList<string> GetLegalityIssues(string report)
    {
        var parts = report
            .Replace("\r", "\n", StringComparison.Ordinal)
            .Split(['\n', '.'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(CleanLegalityIssue)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(10)
            .ToArray();

        return parts.Length == 0 ? ["PKHeX reports this Pokemon is not legal."] : parts;
    }

    private static string CleanLegalityIssue(string issue)
    {
        var text = issue.Trim();
        foreach (var prefix in new[] { "Invalid:", "Invalido:", "Invalid", "Invalido" })
        {
            if (text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                text = text[prefix.Length..].Trim();
        }

        return text;
    }

    private static string CleanPokemonDisplayName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var text = value.Trim();
        var paren = text.IndexOf(" (", StringComparison.Ordinal);
        if (paren > 0)
            text = text[..paren];

        return text.Trim();
    }

    private string GetQueueStatusFor<T>(WebTradeSession session) where T : PKM, new()
    {
        if (!TryGetHub<T>(out var hub))
            return CreateErrorResponse("The trade runner is not ready yet.");

        var active = session.ActiveTrade;
        if (active == null)
        {
            return JsonSerializer.Serialize(new
            {
                success = true,
                queueOpen = hub.Queues.Info.GetCanQueue(),
                queueCount = hub.Queues.Info.Count,
                activeTrade = (object?)null
            }, JsonOptions);
        }

        var position = hub.Queues.Info.CheckPosition(session.DiscordId, active.UniqueTradeId, PokeRoutineType.LinkTrade);
        return JsonSerializer.Serialize(new
        {
            success = true,
            queueOpen = hub.Queues.Info.GetCanQueue(),
            queueCount = hub.Queues.Info.Count,
            activeTrade = new
            {
                active.UniqueTradeId,
                code = active.Code.ToString("D8"),
                active.Pokemon,
                active.SpriteUrl,
                active.Message,
                active.State,
                active.BotName,
                active.PartnerTrainerName,
                active.PartnerTid,
                active.PartnerSid,
                inQueue = position.InQueue,
                position = position.Position,
                total = position.QueueCount,
                active.UpdatedAt
            }
        }, JsonOptions);
    }

    private string CancelWebTradeFor<T>(WebTradeSession session) where T : PKM, new()
    {
        if (!TryGetHub<T>(out var hub))
            return CreateErrorResponse("The trade runner is not ready yet.");

        var result = hub.Queues.Info.ClearTrade(session.DiscordId);
        if (result is QueueResultRemove.Removed or QueueResultRemove.CurrentlyProcessingRemoved)
        {
            if (session.ActiveTrade != null)
            {
                session.ActiveTrade.State = "cancelled";
                session.ActiveTrade.Message = "Trade cancelled.";
                session.ActiveTrade.UpdatedAt = DateTimeOffset.UtcNow;
            }

            return JsonSerializer.Serialize(new { success = true, message = "Trade cancelled." }, JsonOptions);
        }

        return CreateErrorResponse(result == QueueResultRemove.CurrentlyProcessing
            ? "This trade is already processing and cannot be cancelled right now."
            : "You do not have a pending trade in the queue.");
    }

    private bool TryGetHub<T>(out PokeTradeHub<T> hub) where T : PKM, new()
    {
        hub = default!;
        var runner = GetRunningEnvironment();
        if (runner is PokeBotRunner<T> typed)
        {
            hub = typed.Hub;
            return true;
        }

        return false;
    }

    private IPokeBotRunner? GetRunningEnvironment()
    {
        const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public;
        return _mainForm.GetType().GetProperty("RunningEnvironment", flags)?.GetValue(_mainForm) as IPokeBotRunner;
    }

    private static async Task<WebTradeSubmitRequest?> DeserializeWebTradeRequest(HttpListenerRequest request)
    {
        try
        {
            if (request.ContentLength64 > 30000)
                return null;

            using var reader = new StreamReader(request.InputStream);
            var body = await reader.ReadToEndAsync().ConfigureAwait(false);
            return JsonSerializer.Deserialize<WebTradeSubmitRequest>(body, CachedJsonOptions.Secure);
        }
        catch
        {
            return null;
        }
    }

    private static async Task<WebTradeCodeRequest?> DeserializeTradeCodeRequest(HttpListenerRequest request)
    {
        try
        {
            if (request.ContentLength64 > 1024)
                return null;

            using var reader = new StreamReader(request.InputStream);
            var body = await reader.ReadToEndAsync().ConfigureAwait(false);
            return JsonSerializer.Deserialize<WebTradeCodeRequest>(body, CachedJsonOptions.Secure);
        }
        catch
        {
            return null;
        }
    }

    private bool TryGetTradeSession(HttpListenerRequest request, out WebTradeSession session)
    {
        session = default!;
        var cookie = request.Cookies[TradeSessionCookie]?.Value;
        if (string.IsNullOrWhiteSpace(cookie))
            return false;

        if (!TradeSessions.TryGetValue(cookie, out session!))
            return false;

        if (session.CreatedAt < DateTimeOffset.UtcNow.AddDays(-30))
        {
            TradeSessions.TryRemove(cookie, out _);
            return false;
        }

        return true;
    }

    private string GetDiscordRedirectUri(HttpListenerRequest request)
    {
        var configured = Main.Config?.Hub.WebServer.DiscordOAuthRedirectUri;
        if (!string.IsNullOrWhiteSpace(configured))
            return configured;

        var host = request.UserHostName;
        if (string.IsNullOrWhiteSpace(host))
            host = $"localhost:{_port}";

        return $"http://{host}/api/trade/auth/callback";
    }

    private static Dictionary<string, string> ParseQuery(string? query)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(query))
            return values;

        foreach (var pair in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = pair.Split('=', 2);
            var key = WebUtility.UrlDecode(parts[0]);
            var value = parts.Length > 1 ? WebUtility.UrlDecode(parts[1]) : string.Empty;
            if (!string.IsNullOrWhiteSpace(key))
                values[key] = value;
        }

        return values;
    }

    private static string CreateToken(int byteCount)
    {
        var bytes = RandomNumberGenerator.GetBytes(byteCount);
        return Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }

    private static void CleanupExpiredOAuthStates()
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var state in OAuthStates.Where(x => x.Value < now).Select(x => x.Key).ToList())
            OAuthStates.TryRemove(state, out _);
    }

    private static string BuildAuthCallbackHtml(bool success, string messageKey)
    {
        bool isSpanish = AppLocalization.Language == AppLanguage.Spanish;
        string lang = isSpanish ? "es" : "en";
        string title = isSpanish ? "TradeDex - Login" : "TradeDex Login";
        string statusLabel = success
            ? (isSpanish ? "Listo" : "Success")
            : "Error";
        string statusClass = success ? "ok" : "err";
        string closeText = isSpanish
            ? "Puedes cerrar esta pestaña y volver a la página de trades."
            : "You can close this tab and return to the trade page.";
        string safeMessage = WebUtility.HtmlEncode(LocalizeCallbackMessage(messageKey, isSpanish));
        string successLower = success ? "true" : "false";

        return $$"""
            <!doctype html>
            <html lang="{{lang}}">
            <head>
                <meta charset="utf-8">
                <meta name="viewport" content="width=device-width, initial-scale=1">
                <title>{{title}}</title>
                <link rel="icon" type="image/x-icon" href="/icon.ico">
                <link rel="preconnect" href="https://fonts.googleapis.com">
                <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
                <link rel="stylesheet" href="https://fonts.googleapis.com/css2?family=Bricolage+Grotesque:opsz,wght@12..96,600;12..96,700&family=Geist:wght@400;500;600&family=JetBrains+Mono:wght@500&display=swap">
                <style>
                    :root {
                        --bg: oklch(0.165 0.012 50);
                        --surface: oklch(0.215 0.014 55);
                        --ink: oklch(0.97 0.008 80);
                        --ink-soft: oklch(0.84 0.012 75);
                        --muted: oklch(0.66 0.014 70);
                        --hairline: oklch(0.32 0.014 55);
                        --hairline-soft: oklch(0.27 0.014 55);
                        --accent: oklch(0.62 0.198 27);
                        --accent-glow: oklch(0.62 0.198 27 / 0.32);
                        --ok: oklch(0.72 0.16 150);
                        --ok-soft: oklch(0.3 0.07 150);
                        --ok-border: oklch(0.45 0.1 150);
                        --ok-text: oklch(0.88 0.13 150);
                        --danger: oklch(0.68 0.18 25);
                        --danger-soft: oklch(0.3 0.09 25);
                        --danger-border: oklch(0.45 0.12 25);
                        --danger-text: oklch(0.88 0.13 25);
                        --ease-out: cubic-bezier(0.16, 1, 0.3, 1);
                    }
                    * { box-sizing: border-box; }
                    html, body {
                        margin: 0;
                        background: var(--bg);
                        color: var(--ink);
                        font-family: "Geist", system-ui, -apple-system, "Segoe UI", sans-serif;
                        min-height: 100dvh;
                        -webkit-font-smoothing: antialiased;
                    }
                    body {
                        display: grid;
                        place-items: center;
                        padding: 24px;
                        overflow: hidden;
                        position: relative;
                    }
                    .aurora {
                        position: fixed;
                        inset: 0;
                        z-index: -1;
                        overflow: hidden;
                        pointer-events: none;
                    }
                    .blob {
                        position: absolute;
                        border-radius: 50%;
                        filter: blur(90px);
                    }
                    .blob-1 {
                        width: 60vw; height: 60vw;
                        top: -25vw; right: -20vw;
                        background: oklch(0.42 0.2 27);
                        opacity: 0.5;
                        animation: drift1 26s ease-in-out infinite;
                    }
                    .blob-2 {
                        width: 50vw; height: 50vw;
                        bottom: -22vw; left: -18vw;
                        background: oklch(0.3 0.13 285);
                        opacity: 0.35;
                        animation: drift2 34s ease-in-out infinite;
                    }
                    main {
                        position: relative;
                        width: min(440px, calc(100vw - 32px));
                        padding: 38px 34px 30px;
                        background: var(--surface);
                        border: 1px solid var(--hairline);
                        border-radius: 24px;
                        box-shadow: 0 28px 70px oklch(0 0 0 / 0.5), inset 0 1px 0 oklch(1 0 0 / 0.05);
                        text-align: center;
                        animation: enter 600ms var(--ease-out) both;
                    }
                    main::before {
                        content: "";
                        position: absolute;
                        inset: 0;
                        border-radius: inherit;
                        background: linear-gradient(180deg, oklch(1 0 0 / 0.03), transparent 35%);
                        pointer-events: none;
                    }
                    main > * { position: relative; }
                    .mark {
                        width: 56px; height: 56px;
                        margin: 0 auto 22px;
                        display: block;
                        filter: drop-shadow(0 10px 26px var(--accent-glow));
                        animation: float 4.4s ease-in-out infinite;
                    }
                    .pb-shell { fill: var(--ink); }
                    .pb-top { fill: var(--accent); }
                    .pb-band { fill: var(--bg); }
                    .pb-button-outer { fill: var(--bg); stroke: var(--ink); stroke-width: 1.4; }
                    .pb-button-inner { fill: var(--ink); }
                    .status {
                        display: inline-flex;
                        align-items: center;
                        gap: 8px;
                        padding: 5px 12px 5px 10px;
                        border-radius: 999px;
                        font-family: "JetBrains Mono", ui-monospace, monospace;
                        font-size: 10.5px;
                        font-weight: 500;
                        text-transform: uppercase;
                        letter-spacing: 0.18em;
                        margin-bottom: 18px;
                    }
                    .status::before {
                        content: "";
                        width: 6px; height: 6px;
                        border-radius: 50%;
                    }
                    .status.ok {
                        background: var(--ok-soft);
                        border: 1px solid var(--ok-border);
                        color: var(--ok-text);
                    }
                    .status.ok::before {
                        background: var(--ok);
                        box-shadow: 0 0 12px var(--ok);
                    }
                    .status.err {
                        background: var(--danger-soft);
                        border: 1px solid var(--danger-border);
                        color: var(--danger-text);
                    }
                    .status.err::before {
                        background: var(--danger);
                        box-shadow: 0 0 12px var(--danger);
                    }
                    h1 {
                        margin: 0 0 14px;
                        font-family: "Bricolage Grotesque", "Geist", system-ui, sans-serif;
                        font-size: 36px;
                        font-weight: 700;
                        letter-spacing: -0.03em;
                        line-height: 1;
                        font-variation-settings: "opsz" 48, "wdth" 105;
                    }
                    p {
                        margin: 0 0 8px;
                        color: var(--ink-soft);
                        font-size: 15px;
                        line-height: 1.55;
                        text-wrap: pretty;
                    }
                    .closing {
                        margin-top: 20px;
                        padding-top: 16px;
                        border-top: 1px solid var(--hairline-soft);
                        color: var(--muted);
                        font-family: "JetBrains Mono", ui-monospace, monospace;
                        font-size: 10.5px;
                        text-transform: uppercase;
                        letter-spacing: 0.16em;
                        line-height: 1.5;
                    }
                    @keyframes enter {
                        from { opacity: 0; transform: translateY(10px) scale(0.985); }
                        to { opacity: 1; transform: translateY(0) scale(1); }
                    }
                    @keyframes float {
                        0%, 100% { transform: translateY(0); }
                        50% { transform: translateY(-5px); }
                    }
                    @keyframes drift1 {
                        0%, 100% { transform: translate3d(0, 0, 0) scale(1); }
                        50% { transform: translate3d(-3vw, 4vw, 0) scale(1.08); }
                    }
                    @keyframes drift2 {
                        0%, 100% { transform: translate3d(0, 0, 0) scale(1); }
                        50% { transform: translate3d(4vw, -3vw, 0) scale(0.94); }
                    }
                    @media (prefers-reduced-motion: reduce) {
                        *, *::before, *::after {
                            animation-duration: 0.01ms !important;
                            animation-delay: 0ms !important;
                            transition-duration: 0.01ms !important;
                        }
                    }
                </style>
            </head>
            <body>
                <div class="aurora" aria-hidden="true">
                    <span class="blob blob-1"></span>
                    <span class="blob blob-2"></span>
                </div>
                <main>
                    <svg class="mark" viewBox="0 0 32 32" aria-hidden="true">
                        <circle cx="16" cy="16" r="14.5" class="pb-shell"/>
                        <path d="M1.5 16 A14.5 14.5 0 0 1 30.5 16 Z" class="pb-top"/>
                        <rect x="1.5" y="14.5" width="29" height="3" class="pb-band"/>
                        <circle cx="16" cy="16" r="4.5" class="pb-button-outer"/>
                        <circle cx="16" cy="16" r="2.2" class="pb-button-inner"/>
                    </svg>
                    <div class="status {{statusClass}}">{{statusLabel}}</div>
                    <h1>TradeDex</h1>
                    <p>{{safeMessage}}</p>
                    <p class="closing">{{closeText}}</p>
                </main>
                <script>
                    if (window.opener) {
                        window.opener.postMessage({ type: 'tradedex-auth', success: {{successLower}} }, window.location.origin);
                        setTimeout(() => window.close(), 1100);
                    } else {
                        setTimeout(() => window.location.href = '/trade', 1400);
                    }
                </script>
            </body>
            </html>
            """;
    }

    private static string LocalizeCallbackMessage(string key, bool isSpanish) => (key, isSpanish) switch
    {
        ("login-complete", true) => "Sesión iniciada correctamente.",
        ("login-complete", false) => "Login complete.",
        ("invalid-response", true) => "Discord no devolvió una respuesta de login válida.",
        ("invalid-response", false) => "Discord did not return a valid login response.",
        ("session-expired", true) => "La sesión de login expiró. Inténtalo de nuevo.",
        ("session-expired", false) => "The login session expired. Please try again.",
        ("server-settings", true) => "Los ajustes del WebServer no están disponibles.",
        ("server-settings", false) => "WebServer settings are unavailable.",
        ("rejected-code", true) => "Discord rechazó el código de login.",
        ("rejected-code", false) => "Discord rejected the login code.",
        ("no-token", true) => "Discord no devolvió un token de acceso.",
        ("no-token", false) => "Discord did not return an access token.",
        ("no-profile", true) => "No se pudo leer el perfil de Discord.",
        ("no-profile", false) => "Could not read the Discord user profile.",
        ("invalid-user", true) => "Discord devolvió un ID de usuario inválido.",
        ("invalid-user", false) => "Discord returned an invalid user ID.",
        ("login-failed", true) => "Falló el login al conectar con Discord.",
        ("login-failed", false) => "Login failed while talking to Discord.",
        _ => key
    };

    private sealed class WebTradeNotifier<T>(WebTradeSession session, string pokemonName, string spriteUrl, int code, int uniqueTradeId) : IPokeTradeNotifier<T>
        where T : PKM, new()
    {
        public Action<PokeRoutineExecutor<T>>? OnFinish { private get; set; }

        public Task SendInitialQueueUpdate()
        {
            Update("queued", $"Your {pokemonName} trade is queued. Link code: {code:0000 0000}.");
            return Task.CompletedTask;
        }

        public void UpdateBatchProgress(int currentBatchNumber, T currentPokemon, int uniqueTradeID)
        {
            Update("processing", $"Processing batch trade {currentBatchNumber}.");
        }

        public void SendNotification(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info, string message) =>
            Update(GetNotificationState(message), message);

        public void SendNotification(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info, PokeTradeSummary message) =>
            Update(GetNotificationState(message.ToString()), message.ToString() ?? "Trade updated.");

        public void SendNotification(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info, T result, string message) =>
            Update(GetNotificationState(message), message);

        public void TradeCanceled(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info, PokeTradeResult msg)
        {
            Update("cancelled", $"Trade cancelled: {msg.GetDescription()}");
            OnFinish?.Invoke(routine);
        }

        public void TradeFinished(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info, T result)
        {
            if (result.Species > 0)
            {
                var partyBytes = new byte[result.SIZE_PARTY];
                result.WriteDecryptedDataParty(partyBytes);
                new TradeCodeStorage().RecordReceivedTradeFile(
                    session.DiscordId,
                    result.FileName,
                    CleanPokemonDisplayName(GameInfo.Strings.Species[result.Species]),
                    partyBytes);
            }

            Update("finished", $"Trade finished. Enjoy your {pokemonName}.");
            OnFinish?.Invoke(routine);
        }

        public void TradeInitialize(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info) =>
            Update("initializing", $"Loading trade menu for {pokemonName}. Use link code {code:0000 0000}.", routine.InGameName);

        public void TradeSearching(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info) =>
            Update("searching", $"Searching with link code {code:0000 0000}. Bot trainer: {routine.InGameName}.", routine.InGameName);

        private void Update(string state, string message, string botName = "")
        {
            session.ActiveTrade ??= new WebTradeStatus
            {
                UniqueTradeId = uniqueTradeId,
                Code = code,
                Pokemon = pokemonName,
                SpriteUrl = spriteUrl
            };

            if (session.ActiveTrade.State is "finished" or "cancelled")
                return;

            if (IsRegressiveState(session.ActiveTrade.State, state) && !TryParseTrainerFoundMessage(message, out _))
                state = session.ActiveTrade.State;

            session.ActiveTrade.State = state;
            session.ActiveTrade.Message = message;
            if (!string.IsNullOrWhiteSpace(botName))
                session.ActiveTrade.BotName = botName;
            if (TryParseTrainerFoundMessage(message, out var partner))
            {
                session.ActiveTrade.PartnerTrainerName = partner.Name;
                session.ActiveTrade.PartnerTid = partner.Tid;
                session.ActiveTrade.PartnerSid = partner.Sid;
            }
            session.ActiveTrade.UpdatedAt = DateTimeOffset.UtcNow;
        }

        private static bool IsRegressiveState(string current, string next)
        {
            return next == "queued" && current is ("initializing" or "searching" or "partner" or "processing");
        }

        private static string GetNotificationState(string? message)
        {
            var text = message ?? string.Empty;
            if (TryParseTrainerFoundMessage(text, out _))
                return "partner";

            return "processing";
        }

        private static bool TryParseTrainerFoundMessage(string? message, out (string Name, string Tid, string Sid) partner)
        {
            partner = default;
            if (string.IsNullOrWhiteSpace(message))
                return false;

            var text = message.Replace("*", string.Empty).Replace("_", string.Empty).Replace("`", string.Empty);
            var marker = "Entrenador encontrado:";
            var index = text.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (index < 0 && text.Contains("Entrenador", StringComparison.OrdinalIgnoreCase))
            {
                marker = "encontrado:";
                index = text.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            }
            if (index < 0)
            {
                marker = "Link trade trainer found:";
                index = text.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            }
            if (index < 0)
            {
                marker = "Trainer found:";
                index = text.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            }
            if (index < 0)
                return false;

            var after = text[(index + marker.Length)..].Trim();
            var nameEnd = after.IndexOfAny(['\r', '\n', '.', ',']);
            var name = (nameEnd >= 0 ? after[..nameEnd] : after).Trim();
            var tid = ExtractNumberAfter(text, "TID");
            var sid = ExtractNumberAfter(text, "SID");
            if (string.IsNullOrWhiteSpace(name))
                return false;

            partner = (name, tid, sid);
            return true;
        }

        private static string ExtractNumberAfter(string text, string key)
        {
            var index = text.IndexOf(key, StringComparison.OrdinalIgnoreCase);
            if (index < 0)
                return string.Empty;

            var digits = new string(text[(index + key.Length)..].SkipWhile(c => !char.IsDigit(c)).TakeWhile(char.IsDigit).ToArray());
            return digits;
        }
    }
}
