using System;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SteamTrader.Core.Configuration;
using SteamTrader.Core.Dtos;
using WebSocketSharp;

namespace SteamTrader.Core.Services.WebSocket
{
    public class LootFarmWebSocketClient : IDisposable
    {
        private const string BaseUrl = "wss://loot.farm/wss_new";
        private readonly WebSocketSharp.WebSocket _webSocket;
        private readonly Settings _settings;

        public LootFarmWebSocketClient(IOptions<Settings> settings)
        {
            _settings = settings.Value;
            _webSocket = new WebSocketSharp.WebSocket(BaseUrl);

            _webSocket.OnOpen += (_, _) => Authorize();
            _webSocket.OnMessage += WebSocketOnOnMessage;
            
            Authorize();
        }

        private void WebSocketOnOnMessage(object sender, MessageEventArgs e)
        {
            
        }

        public void Authorize()
        {
            var commandRequest = new LootFarmWebSocketCommand("auth", _settings.LootFarmSettings.AuthToken).ToString();
            _webSocket.Send(commandRequest);
        }

        public void SendBuyTrade(ApiLootFarmBuyItemDto tradeOffer)
        {
            var requestModel = new
            {
                bot = new [] {$"{tradeOffer.AppId}_{tradeOffer.SubjectId}"},
                botVal = tradeOffer.Price,
                botId = tradeOffer.BotId,
                csrf = "",
                left = - tradeOffer.Price,
                status = 0,
                userVal = 0
            };
            var requestItems = new[] {requestModel};

            var command = new LootFarmWebSocketCommand("trade", requestItems);
            _webSocket.Send(command.ToString());
        }

        public void Dispose()
        {
            _webSocket.Close();
        }
    }

    public class LootFarmWebSocketCommand
    {
        public LootFarmWebSocketCommand()
        { }

        public LootFarmWebSocketCommand(string command, object data)
        {
            Command = command;
            Data = JsonConvert.SerializeObject(data);
        }
        [JsonProperty("c")]
        public string Command { get; set; }
        [JsonProperty("d")]
        public string Data { get; set; }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}