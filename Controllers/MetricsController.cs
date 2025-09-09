using DELTAAPI.Data;
using DELTAAPI.DTOs;
using DELTAAPI.DTOs.DELTAAPI.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace DELTAAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MetricsController : ControllerBase
{
    private readonly DeltaContext _ctx;
    private readonly ILogger<MetricsController> _logger;

    public MetricsController(DeltaContext ctx, ILogger<MetricsController> logger)
    {
        _ctx = ctx; _logger = logger;
    }

    // -------- helpers --------
    private static TimeZoneInfo GetBrazilTz()
    {
        // Windows: "E. South America Standard Time" | Linux: "America/Sao_Paulo"
        try { return TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time"); }
        catch { return TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo"); }
    }
    private static DateTime BrazilNow() =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, GetBrazilTz());

    private static bool LooksLikeBot(string? ua)
    {
        if (string.IsNullOrWhiteSpace(ua)) return false;
        ua = ua.ToLowerInvariant();
        return ua.Contains("bot") || ua.Contains("spider") || ua.Contains("crawl")
            || ua.Contains("headless") || ua.Contains("uptime") || ua.Contains("pingdom")
            || ua.Contains("statuscake") || ua.Contains("monitor");
    }

    // -------- VISITAS (acesso ao site) --------
    [HttpPost("visit")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> Visit([FromBody] VisitPingDto dto)
    {
        try
        {
            var ua = Request.Headers.UserAgent.ToString();
            if (LooksLikeBot(ua)) return Ok(new { ignored = true });

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var nowBrt = BrazilNow();
            var diaBrt = nowBrt.Date;

            // saneamento
            string? url = dto?.Url?.Trim();
            if (!string.IsNullOrEmpty(url) && url.Length > 800) url = url[..800];
            string? referrer = dto?.Referrer?.Trim();
            if (!string.IsNullOrEmpty(referrer) && referrer.Length > 800) referrer = referrer[..800];

            Guid? anon = null;
            if (!string.IsNullOrWhiteSpace(dto?.AnonId) && Guid.TryParse(dto.AnonId, out var g)) anon = g;

            var p = new[]
            {
                new SqlParameter("@CriadoEmBrt", nowBrt),
                new SqlParameter("@DiaBrt",      diaBrt),
                new SqlParameter("@AnonId",      (object?)anon       ?? DBNull.Value),
                new SqlParameter("@Url",         (object?)url        ?? DBNull.Value),
                new SqlParameter("@Referrer",    (object?)referrer   ?? DBNull.Value),
                new SqlParameter("@UtmSource",   (object?)dto?.UtmSource   ?? DBNull.Value),
                new SqlParameter("@UtmMedium",   (object?)dto?.UtmMedium   ?? DBNull.Value),
                new SqlParameter("@UtmCampaign", (object?)dto?.UtmCampaign ?? DBNull.Value),
                new SqlParameter("@Ip",          (object?)ip         ?? DBNull.Value),
                new SqlParameter("@Ua",          (object?)ua         ?? DBNull.Value)
            };

            await _ctx.Database.ExecuteSqlRawAsync(@"
                INSERT INTO dbo.Visita
                  (CriadoEmBrt, -- explícito
                   AnonId, Url, Referrer, UtmSource, UtmMedium, UtmCampaign, Ip, UserAgent)
                VALUES
                  (@CriadoEmBrt,
                   @AnonId, @Url, @Referrer, @UtmSource, @UtmMedium, @UtmCampaign, @Ip, @Ua);
            ", p);

            return Ok(new { ok = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao registrar visita.");
            return Ok(new { ok = false });
        }
    }

    // -------- PAGEVIEWS (trocas de rota da SPA) --------
    [HttpPost("pageview")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> PageView([FromBody] PageViewDto dto)
    {
        try
        {
            var ua = Request.Headers.UserAgent.ToString();
            if (LooksLikeBot(ua)) return Ok(new { ignored = true });

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var nowBrt = BrazilNow();

            // saneamento
            var route = (dto?.Route ?? "").Trim();
            if (route.Length > 200) route = route[..200];

            string? url = dto?.Url?.Trim();
            if (!string.IsNullOrEmpty(url) && url.Length > 800) url = url[..800];

            Guid? anon = null;
            if (!string.IsNullOrWhiteSpace(dto?.AnonId) && Guid.TryParse(dto.AnonId, out var g)) anon = g;

            var p = new[]
            {
                new SqlParameter("@CriadoEmBrt", nowBrt),
                new SqlParameter("@Route",       route),
                new SqlParameter("@Url",         (object?)url  ?? DBNull.Value),
                new SqlParameter("@AnonId",      (object?)anon ?? DBNull.Value),
                new SqlParameter("@Ip",          (object?)ip   ?? DBNull.Value),
                new SqlParameter("@Ua",          (object?)ua   ?? DBNull.Value)
            };

            await _ctx.Database.ExecuteSqlRawAsync(@"
                INSERT INTO dbo.PageView
                  (CriadoEmBrt, Route, Url, AnonId, Ip, UserAgent)
                VALUES
                  (@CriadoEmBrt, @Route, @Url, @AnonId, @Ip, @Ua);
            ", p);

            return Ok(new { ok = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao registrar pageview.");
            return Ok(new { ok = false });
        }
    }
}
