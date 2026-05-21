using PKHeX.Core;
using SysBot.Base;
using SysBot.Pokemon.Helpers;
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
            return BuildAuthCallbackHtml(false, "Discord did not return a valid login response.");

        if (!OAuthStates.TryRemove(state, out var expiresAt) || expiresAt < DateTimeOffset.UtcNow)
            return BuildAuthCallbackHtml(false, "The login session expired. Please try again.");

        var settings = Main.Config?.Hub.WebServer;
        if (settings == null)
            return BuildAuthCallbackHtml(false, "WebServer settings are unavailable.");

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
                return BuildAuthCallbackHtml(false, "Discord rejected the login code.");

            var token = await JsonSerializer.DeserializeAsync<DiscordTokenResponse>(
                await tokenResponse.Content.ReadAsStreamAsync().ConfigureAwait(false), JsonOptions).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(token?.AccessToken))
                return BuildAuthCallbackHtml(false, "Discord did not return an access token.");

            using var userRequest = new HttpRequestMessage(HttpMethod.Get, "https://discord.com/api/users/@me");
            userRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.AccessToken);
            using var userResponse = await DiscordHttp.SendAsync(userRequest).ConfigureAwait(false);
            if (!userResponse.IsSuccessStatusCode)
                return BuildAuthCallbackHtml(false, "Could not read the Discord user profile.");

            var user = await JsonSerializer.DeserializeAsync<DiscordUserResponse>(
                await userResponse.Content.ReadAsStreamAsync().ConfigureAwait(false), JsonOptions).ConfigureAwait(false);

            if (user == null || !ulong.TryParse(user.Id, out var discordId))
                return BuildAuthCallbackHtml(false, "Discord returned an invalid user ID.");

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
            return BuildAuthCallbackHtml(true, "Login complete.");
        }
        catch (Exception ex)
        {
            LogUtil.LogError($"Discord OAuth callback failed: {ex.Message}", "WebTrade");
            return BuildAuthCallbackHtml(false, "Login failed while talking to Discord.");
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

    private static string BuildAuthCallbackHtml(bool success, string message)
    {
        var safeMessage = WebUtility.HtmlEncode(message);
        var status = success ? "success" : "error";
        return $$"""
            <!doctype html>
            <html lang="en">
            <head>
                <meta charset="utf-8">
                <meta name="viewport" content="width=device-width, initial-scale=1">
                <title>TradeDex Login</title>
                <style>
                    body { margin: 0; min-height: 100vh; display: grid; place-items: center; font-family: Inter, Segoe UI, sans-serif; background: #0d1324; color: #eef2ff; }
                    main { width: min(440px, calc(100vw - 32px)); border: 1px solid rgba(255,255,255,.12); border-radius: 8px; padding: 28px; background: rgba(255,255,255,.06); }
                    .status { color: {{(success ? "#7ee787" : "#ff7b72")}}; font-weight: 700; text-transform: uppercase; font-size: 12px; letter-spacing: .08em; }
                    p { color: #b9c2d9; line-height: 1.6; }
                </style>
            </head>
            <body>
                <main>
                    <div class="status">{{status}}</div>
                    <h1>TradeDex</h1>
                    <p>{{safeMessage}}</p>
                    <p>You can close this tab and return to the trade page.</p>
                </main>
                <script>
                    if (window.opener) {
                        window.opener.postMessage({ type: 'tradedex-auth', success: {{success.ToString().ToLowerInvariant()}} }, window.location.origin);
                        setTimeout(() => window.close(), 700);
                    } else {
                        setTimeout(() => window.location.href = '/trade', 900);
                    }
                </script>
            </body>
            </html>
            """;
    }

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
