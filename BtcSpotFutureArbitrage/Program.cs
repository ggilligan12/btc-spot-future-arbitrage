using BtcSpotFutureArbitrage;

string binanceURL = "wss://stream.binance.com:9443/ws/btcusdt@depth5";
WSClientSingleton client = new WSClientSingleton();

Console.WriteLine("Making WS connection...");
await client.ConnectAsync(binanceURL);
Console.WriteLine("Made connection!");

Console.WriteLine("Subscribing to stream...");
var garbage = @"{
  ""method"": ""SUBSCRIBE"",
  ""params"": [
    ""btcusdt@aggTrade"",
    ""btcusdt@depth""
  ],
  ""id"": 1
}";
await client.SendMessageAsync(garbage);



System.Threading.Thread.Sleep(1000);

Console.WriteLine("Closing connection...");
await client.DisconnectAsync();
Console.WriteLine("Connection closed. Done.");
