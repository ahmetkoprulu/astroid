namespace Astroid.Core.Models
{
	public class MailAttachment
	{
		public string Name { get; set; }
		public byte[] Data { get; set; }
		public string ContentType { get; set; }
	}
}