using System.Net;
using Microsoft.AspNetCore.HttpOverrides;

namespace QLKSan.Common
{
    // Chỉ nhận X-Forwarded-For / X-Forwarded-Proto từ proxy tin cậy (Gateway): loopback và các mạng khai báo dạng CIDR
    public static class TrustedProxies
    {
        public static void Configure(ForwardedHeadersOptions options, IEnumerable<string> mangTinCay)
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            foreach (var mang in mangTinCay ?? Enumerable.Empty<string>())
            {
                if (!string.IsNullOrWhiteSpace(mang))
                    options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(mang.Trim()));
            }
        }
    }
}
