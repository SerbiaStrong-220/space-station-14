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