namespace Astroid.Core.Queue;

public class QueueManager<T> where T : class
{
	public event EventHandler<EventArgs<T>> DataEnqueued;

	public void EnqueueData(T data)
	{
		DataEnqueued?.Invoke(this, new EventArgs<T> { Data = data });
	}
}
