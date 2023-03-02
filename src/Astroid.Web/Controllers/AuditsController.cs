using Microsoft.AspNetCore.Mvc;
using Astroid.Entity;
using Astroid.Web.Models;
using Astroid.Core;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Astroid.Providers;
using Astroid.Web.Helpers;
using Astroid.Core.Cache;

namespace Astroid.Web;

public class AuditsController : SecureController
{
	public AuditsController(AstroidDb db, ICacheService cache) : base(db, cache) { }

	[HttpPost("list")]
	public async Task<IActionResult> List([FromQuery(Name = "bot")] Guid botId, [FromBody] MPViewDataList<ADAudit> model)
	{
		model ??= new MPViewDataList<ADAudit>();

		model = await Db.Audits
			.Where(x => x.UserId == CurrentUser.Id)
			.Where(x => x.ActorId == botId)
			.AsNoTracking()
			.OrderByDescending(x => x.CreatedDate)
			.ViewDataListAsync<ADAudit>(model);

		return Success(model);
	}
}

public class AMAudit
{
	public string CorrelationId { get; set; }
	public Guid BotId { get; set; }
	public List<ADAudit> Audits { get; set; }
}