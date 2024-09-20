using Microsoft.AspNetCore.Mvc;

namespace UserInfoImitator.Controllers;

[ApiController]
[Route("[controller]")]
public class UserInfoController : Controller
{
    [HttpGet("{userId}")]
    public DiscordSponsorInfo GetUserInfo(Guid userId)
    {
        return new()
        {
            Tiers = [SponsorTier.GoldenShlopa]
        };
    }

    [HttpGet("sponsors")]
    public async Task<SponsorUsers> GetUsersTiers()
    {
        // Запрос довольно ёмкий и его выполнение занимает время.
        // Поэтому симулируем задержку.
        await Task.Delay(6000);

        return new SponsorUsers()
        {
            Shlopas = ["A", "B", "C"],
            BigShlopas = ["C", "D", "E"],
            HugeShlopas = ["F", "H", "I"],
            GoldenShlopas = ["J", "K", "L"],
            CriticalMassShlopas = ["M", "N"]
        };
    }
}

public class DiscordSponsorInfo
{
    public SponsorTier[] Tiers { get; set; } = [];
}

public enum SponsorTier
{
    None,
    Shlopa,
    BigShlopa,
    HugeShlopa,
    GoldenShlopa,
    CriticalMassShlopa
}

public class SponsorUsers
{
    public string[] Shlopas { get; set; } = [];

    public string[] BigShlopas { get; set; } = [];

    public string[] HugeShlopas { get; set; } = [];

    public string[] GoldenShlopas { get; set; } = [];

    public string[] CriticalMassShlopas { get; set; } = [];
}
