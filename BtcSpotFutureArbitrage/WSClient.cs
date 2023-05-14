using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace BtcSpotFutureArbitrage
{

	/*
	Significant credit due to the author of this SO answer:
	https://stackoverflow.com/questions/30523478/connecting-to-websocket-using-c-sharp-i-can-connect-using-javascript-but-c-sha
	*/
    public class WSClientSingleton : IDisposable
    {

    	private ClientWebSocket? WS;
        private CancellationTokenSource? CTS;

        public int ReceiveBufferSize { get; set; } = 8192;

        public async Task ConnectAsync(string url)
        {
            if (WS is not null)
            {
                // A connection is already open. Return.
                if (WS.State == WebSocketState.Open) return;
                // Out with the old.
                else WS.Dispose();
            }
            WS = new ClientWebSocket();
            if (CTS is not null) CTS.Dispose();
            CTS = new CancellationTokenSource();
            await WS.ConnectAsync(new Uri(url), CTS.Token);
            await Task.Factory.StartNew(ReceiveLoop, CTS.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        public async Task DisconnectAsync()
        {
            if (WS is null || CTS is null) return;
            // TODO: requests cleanup code, sub-protocol dependent.
            if (WS.State == WebSocketState.Open)
            {
                CTS.CancelAfter(TimeSpan.FromSeconds(2));
                await WS.CloseOutputAsync(WebSocketCloseStatus.Empty, "", CancellationToken.None);
                await WS.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
            }
            WS.Dispose();
            WS = null;
            if (CTS is not null) CTS.Dispose();
            CTS = null;
        }

        private async Task ReceiveLoop()
        {
            // TODO: Handle the event of the WS/CTS being null better
            if (WS is null || CTS is null) return;
            var loopToken = CTS.Token;
            MemoryStream? outputStream = null;
            WebSocketReceiveResult? receiveResult = null;
            var buffer = new byte[ReceiveBufferSize];
            try
            {
                while (!loopToken.IsCancellationRequested)
                {
                    outputStream = new MemoryStream(ReceiveBufferSize);
                    do
                    {
                        receiveResult = await WS.ReceiveAsync(buffer, CTS.Token);
                        if (receiveResult.MessageType != WebSocketMessageType.Close)
                            outputStream.Write(buffer, 0, receiveResult.Count);
                    }
                    while (!receiveResult.EndOfMessage);
                    if (receiveResult.MessageType == WebSocketMessageType.Close) break;
                    outputStream.Position = 0;
                    ResponseReceived(outputStream);
                }
            }
            catch (TaskCanceledException) { }
            finally
            {
                outputStream?.Dispose();
            }
        }

        public async Task SendMessageAsync(string message)
        {
            if (WS is null) return;
        	ArraySegment<byte> bytesToSend = new ArraySegment<byte>(Encoding.UTF8.GetBytes(message));
        	await WS.SendAsync(bytesToSend, WebSocketMessageType.Text, true, CancellationToken.None);
        }

        private void ResponseReceived(Stream inputStream)
        {
            // TODO: handle deserializing responses and matching them to the requests.
            // IMPORTANT: DON'T FORGET TO DISPOSE THE inputStream!
            var buffer = new byte[ReceiveBufferSize];
            inputStream.Read(buffer, 0, ReceiveBufferSize-1);
            var response = Encoding.UTF8.GetString(buffer);
            Console.WriteLine(response);
        }

        public void Dispose() => DisconnectAsync().Wait();
        
    }

}