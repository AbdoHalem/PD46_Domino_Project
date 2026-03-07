using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Client_UI.Network
{
    public class MessageReceivedEventArgs : EventArgs
    {
        public string Action  { get; }
        public JsonElement Payload { get; }
        public MessageReceivedEventArgs(string action, JsonElement payload)
        { Action = action; Payload = payload; }
    }

    public class DominoClient : IDisposable
    {
        private TcpClient         _tcp;
        private NetworkStream     _stream;
        private CancellationTokenSource _cts;

        private readonly Control _uiControl;

        public event EventHandler<MessageReceivedEventArgs> MessageReceived;
        public event EventHandler Disconnected;

        public bool IsConnected => _tcp?.Connected ?? false;

        public DominoClient(Control uiControl)
        {
            _uiControl = uiControl;
        }

        public async Task<bool> ConnectAsync(string host, int port)
        {
            try
            {
                _tcp  = new TcpClient();
                await _tcp.ConnectAsync(host, port);
                _stream = _tcp.GetStream();
                _cts    = new CancellationTokenSource();

                // Start background receive loop
                _ = ReceiveLoopAsync(_cts.Token);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Client] Connect failed: {ex.Message}");
                return false;
            }
        }

        public async Task SendAsync(string action, object payload = null)
        {
            if (!IsConnected) return;
            try
            {
                string json = JsonSerializer.Serialize(new { Action = action, Payload = payload });
                byte[] body   = Encoding.UTF8.GetBytes(json);
                byte[] prefix = BitConverter.GetBytes(body.Length);

                await _stream.WriteAsync(prefix, 0, 4);
                await _stream.WriteAsync(body,   0, body.Length);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Client] Send failed: {ex.Message}");
            }
        }

        private async Task ReceiveLoopAsync(CancellationToken ct)
        {
            byte[] lenBuf = new byte[4];
            try
            {
                while (!ct.IsCancellationRequested && _tcp.Connected)
                {
                    // 1. Read 4-byte length prefix
                    int read = await ReadExactAsync(lenBuf, 4, ct);
                    if (read == 0) break;

                    int msgLen = BitConverter.ToInt32(lenBuf, 0);
                    if (msgLen <= 0 || msgLen > 1024 * 1024) break;

                    // 2. Read the payload
                    byte[] buf = new byte[msgLen];
                    await ReadExactAsync(buf, msgLen, ct);

                    string json = Encoding.UTF8.GetString(buf);
                    ParseAndRaise(json);
                }
            }
            catch (OperationCanceledException) { }
            catch (IOException) { }
            catch (Exception ex)
            {
                Console.WriteLine($"[Client] Receive error: {ex.Message}");
            }
            finally
            {
                RaiseOnUI(() => Disconnected?.Invoke(this, EventArgs.Empty));
            }
        }

        private async Task<int> ReadExactAsync(byte[] buffer, int count, CancellationToken ct)
        {
            int total = 0;
            while (total < count)
            {
                int r = await _stream.ReadAsync(buffer, total, count - total, ct);
                if (r == 0) return 0;
                total += r;
            }
            return total;
        }

        private void ParseAndRaise(string json)
        {
            try
            {
                using var doc  = JsonDocument.Parse(json);
                var root       = doc.RootElement.Clone();   // clone so doc can be disposed
                string action  = root.GetProperty("Action").GetString();
                var payload    = root.TryGetProperty("Payload", out var p) ? p : default;

                RaiseOnUI(() => MessageReceived?.Invoke(this, new MessageReceivedEventArgs(action, payload)));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Client] Parse error: {ex.Message}");
            }
        }

        private void RaiseOnUI(Action a)
        {
            if (_uiControl == null || _uiControl.IsDisposed) { a(); return; }
            if (_uiControl.InvokeRequired) _uiControl.BeginInvoke(a);
            else a();
        }

        public void Disconnect()
        {
            _cts?.Cancel();
            _tcp?.Close();
        }

        public void Dispose() => Disconnect();
    }
}
