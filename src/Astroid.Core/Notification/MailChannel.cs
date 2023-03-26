using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.Extensions.Logging;
using Astroid.Core.Models;

namespace Astroid.Core.Notification;

public class MailChannel : INotificationChannel
{
	private const string FromName = "Astroid";
	private const string From = "noreply@astroid.com";
	private readonly string Host = string.Empty;
	private readonly int Port = 587;
	private readonly string UserName = string.Empty;
	private readonly string Password = string.Empty;

	public MailChannel()
	{
		Host = Environment.GetEnvironmentVariable("ASTROID_SMTP_HOST") ?? throw new Exception("ASTROID_SMTP_HOST");
		Port = int.Parse(Environment.GetEnvironmentVariable("ASTROID_SMTP_PORT") ?? throw new Exception("ASTROID_SMTP_PORT"));
		UserName = Environment.GetEnvironmentVariable("ASTROID_SMTP_USERNAME") ?? throw new Exception("ASTROID_SMTP_USERNAME");
		Password = Environment.GetEnvironmentVariable("ASTROID_SMTP_PASSWORD") ?? throw new Exception("ASTROID_SMTP_PASSWORD");
	}

	public ServiceData Send(string subject, string content, string to, params NotificationAttachment[] attachments)
	{
		var message = new MimeMessage();
		message.From.Add(new MailboxAddress(FromName, From));

		// multiple reciever in TO
		to = $"{to}".Replace(";", ",");
		_ = InternetAddressList.TryParse(to, out var list);
		if (list != null)
			message.To.AddRange(list);

		message.Subject = subject;
		var bodyBuilder = new BodyBuilder
		{
			HtmlBody = content
		};

		if (attachments != null && attachments.Any())
		{
			foreach (var attachment in attachments)
				bodyBuilder.Attachments.Add(attachment.Name, attachment.Data, ContentType.Parse(attachment.ContentType));
		}

		message.Body = bodyBuilder.ToMessageBody();
		try
		{
			using var client = new SmtpClient();
			client.AuthenticationMechanisms.Remove("XOAUTH2");

			client.ServerCertificateValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
			client.Connect(Host, Port, false);

			if (!string.IsNullOrEmpty(UserName) || !string.IsNullOrEmpty(Password))
				client.Authenticate(UserName, Password);

			client.Send(message);

			client.Disconnect(true);
			return new ServiceData
			{
				Success = true,
				Message = "Mail sent successfully."
			};
		}
		catch (Exception ex)
		{
			return new ServiceData
			{
				Success = false,
				Message = $"Error while sending mail.\nError: {ex.Message} \nMessage: {ex.StackTrace}"
			};
		}
	}
}