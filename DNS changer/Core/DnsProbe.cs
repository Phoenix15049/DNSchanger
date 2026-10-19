using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace DNS_changer.Core
{
    /// <summary>
    /// Measures real DNS response time by sending an A query over UDP/53.
    /// More meaningful than ICMP ping, which many resolvers block.
    /// </summary>
    public static class DnsProbe
    {
        const string TestDomain = "google.com";
        static readonly Random Random = new Random();

        /// <returns>Best response time in milliseconds, or -1 when the server did not answer.</returns>
        public static async Task<int> MeasureAsync(string server, int timeoutMs = 2000)
        {
            IPAddress address;
            if (!IPAddress.TryParse(server, out address))
                return -1;

            int best = -1;
            for (int attempt = 0; attempt < 2; attempt++)
            {
                int ms = await QueryOnceAsync(address, timeoutMs).ConfigureAwait(false);
                if (ms >= 0 && (best < 0 || ms < best))
                    best = ms;
            }
            return best;
        }

        static async Task<int> QueryOnceAsync(IPAddress address, int timeoutMs)
        {
            ushort id;
            lock (Random)
                id = (ushort)Random.Next(1, ushort.MaxValue);
            byte[] query = BuildQuery(id, TestDomain);

            try
            {
                using (var udp = new UdpClient(address.AddressFamily))
                {
                    udp.Connect(address, 53);
                    var watch = Stopwatch.StartNew();
                    await udp.SendAsync(query, query.Length).ConfigureAwait(false);

                    var receive = udp.ReceiveAsync();
                    var finished = await Task.WhenAny(receive, Task.Delay(timeoutMs)).ConfigureAwait(false);
                    if (finished != receive)
                    {
                        // Observe the fault caused by disposing the socket.
                        _ = receive.ContinueWith(t => t.Exception, TaskContinuationOptions.OnlyOnFaulted);
                        return -1;
                    }

                    byte[] response = receive.Result.Buffer;
                    watch.Stop();
                    if (!IsValidResponse(response, id))
                        return -1;
                    return (int)Math.Max(1, watch.ElapsedMilliseconds);
                }
            }
            catch
            {
                return -1;
            }
        }

        static bool IsValidResponse(byte[] r, ushort id)
        {
            if (r == null || r.Length < 12)
                return false;
            if (((r[0] << 8) | r[1]) != id)
                return false;
            if ((r[2] & 0x80) == 0) // QR bit: must be a response
                return false;
            int rcode = r[3] & 0x0F;
            return rcode == 0 || rcode == 3; // NOERROR or NXDOMAIN both mean the server works
        }

        static byte[] BuildQuery(ushort id, string domain)
        {
            var bytes = new System.Collections.Generic.List<byte>
            {
                (byte)(id >> 8), (byte)id,
                0x01, 0x00,             // standard query, recursion desired
                0x00, 0x01,             // QDCOUNT
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00
            };
            foreach (var label in domain.Split('.'))
            {
                bytes.Add((byte)label.Length);
                foreach (char c in label)
                    bytes.Add((byte)c);
            }
            bytes.AddRange(new byte[] { 0x00, 0x00, 0x01, 0x00, 0x01 }); // end, QTYPE=A, QCLASS=IN
            return bytes.ToArray();
        }
    }
}
