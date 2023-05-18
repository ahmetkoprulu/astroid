public class EventArgs<T> : EventArgs where T : class
{
	public T Data { get; set; }
}
