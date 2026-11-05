using System.Net;
using System.Net.Mail;
using System.Text;
using Microsoft.Extensions.Options;
using QLKSan.Notification.Common;

namespace QLKSan.Notification.Business
{
    public interface IEmailSender
    {
        Task SendAsync(EmailMessage email, CancellationToken ct);
    }

    // Có Smtp:Host thì gửi qua SMTP; không có thì ghi file .eml vào ThuMucLuu (mở được bằng Outlook / trình đọc email)
    public class EmailSender : IEmailSender
    {
        private readonly EmailOptions _options;
        private readonly string _thuMuc;

        public EmailSender(IOptions<EmailOptions> options, IHostEnvironment environment)
        {
            _options = options.Value;
            _thuMuc = Path.GetFullPath(Path.Combine(environment.ContentRootPath, _options.ThuMucLuu));
        }

        public async Task SendAsync(EmailMessage email, CancellationToken ct)
        {
            using var message = new MailMessage
            {
                From = new MailAddress(_options.NguoiGui, _options.TenKhachSan),
                Subject = email.TieuDe,
                SubjectEncoding = Encoding.UTF8,
                Body = email.NoiDungHtml,
                BodyEncoding = Encoding.UTF8,
                IsBodyHtml = true
            };
            message.To.Add(new MailAddress(email.NguoiNhan, email.TenNguoiNhan));

            using var smtp = new SmtpClient();
            if (string.IsNullOrWhiteSpace(_options.Smtp?.Host))
            {
                Directory.CreateDirectory(_thuMuc);
                smtp.DeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory;
                smtp.PickupDirectoryLocation = _thuMuc;
            }
            else
            {
                smtp.Host = _options.Smtp.Host;
                smtp.Port = _options.Smtp.Port;
                smtp.EnableSsl = _options.Smtp.DungSsl;
                if (!string.IsNullOrWhiteSpace(_options.Smtp.TenDangNhap))
                    smtp.Credentials = new NetworkCredential(_options.Smtp.TenDangNhap, _options.Smtp.MatKhau);
            }
            await smtp.SendMailAsync(message, ct);
        }
    }
}
