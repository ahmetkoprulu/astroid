using Astroid.Core;
using Astroid.Entity;

namespace Astroid.Providers;

public class AMProviderResult
{
	public bool Success { get; set; }
	public string? Message { get; set; }
	public List<ADAudit> Audits { get; set; } = new();

	public AMProviderResult WithSuccess()
	{
		Success = true;

		return this;
	}

	public AMProviderResult WithMessage(string message)
	{
		Message = message;

		return this;
	}

	public AMProviderResult AddAudit(AuditType type, string description, string? correlationId = null, string? data = null)
	{
		var audit = new ADAudit
		{
			Id = Guid.NewGuid(),
			UserId = Guid.Empty,
			ActorId = Guid.Empty,
			Type = type,
			Description = description,
			CorrelationId = correlationId,
			Data = data,
			CreatedDate = DateTime.UtcNow,
		};
		Audits.Add(audit);

		return this;
	}
}