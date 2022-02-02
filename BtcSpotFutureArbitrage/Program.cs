using BtcSpotFutureArbitrage;

string binanceURL = "wss://stream.binance.com:9443/ws/btcusdt@depth5";


Console.WriteLine("Attempting to use our Web Socket class!");

WSClientSingleton client = new WSClientSingleton();

await client.ConnectAsync(binanceURL);



await client.DisconnectAsync();
