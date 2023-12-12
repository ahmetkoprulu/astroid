public interface IRepository
{
	Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
