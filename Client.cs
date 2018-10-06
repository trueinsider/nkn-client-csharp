using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using NKN.Client.Protocol;
using Utf8Json;
using WebSocketSharp;

namespace NKN.Client
{
    public class Client
    {
        public class Options
        {
            public int ReconnectIntervalMin = Const.ReconnectIntervalMin;
            public int ReconnectIntervalMax = Const.ReconnectIntervalMax;
            public string RpcServerAddr = Const.SeedRpcServerAddr;
        }

        private class WebSocketRequest
        {
            public string Action;
            public string Addr;

            public WebSocketRequest(string action, string addr)
            {
                Action = action;
                Addr = addr;
            }
        }

        private class WebSocketResponse
        {
            public enum ErrCodes
            {
                Success
            }

            public string Action;
            public ErrCodes? Error;
        }

        private Options options;
        private bool shouldReconnect;
        private int reconnectInterval;
        private WebSocket ws;

        public string PublicKey;
        public string Identifier;
        public string Addr => (Identifier != null ? Identifier + "." : string.Empty) + PublicKey;

        public delegate void OnConnectHandler();
        public event OnConnectHandler OnConnect;

        public delegate void OnMessageHandler(string src, byte[] data);
        public event OnMessageHandler OnMessage;

        public Client(string publicKey, string identifier = null, Options options = null)
        {
            PublicKey = publicKey;
            Identifier = identifier;

            this.options = options ?? new Options();
            reconnectInterval = this.options.ReconnectIntervalMin;

            Connect();
        }

        public async void Connect()
        {
            string result;
            try
            {
                var response = await RPC.Call<RPC.GetWSAddrParams, string>(options.RpcServerAddr, "getwsaddr", new RPC.GetWSAddrParams(Addr));
                result = response.result;
            }
            catch (Exception e)
            {
                if (!shouldReconnect)
                {
                    return;
                }
                Debug.WriteLine("RPC call failed: " + e);
                Reconnect();
                return;
            }

            try
            {
                var url = "ws://" + result;
                ws = new WebSocket(url);

                ws.OnOpen += (sender, e) =>
                {
                    ws.Send(JsonSerializer.Serialize(new WebSocketRequest("setClient", Addr)));
                    shouldReconnect = true;
                    reconnectInterval = options.ReconnectIntervalMin;
                };

                ws.OnMessage += (sender, e) =>
                {
                    try
                    {
                        if (e.IsBinary)
                        {
                            HandleMsg(e.RawData);
                            return;
                        }

                        var msg = JsonSerializer.Deserialize<WebSocketResponse>(e.Data);
                        if (msg.Error != WebSocketResponse.ErrCodes.Success)
                        {
                            if (msg.Action.Equals("setClient"))
                            {
                                ws.Close();
                            }

                            return;
                        }

                        switch (msg.Action)
                        {
                            case "setClient":
                                OnConnect?.Invoke();
                                break;
                            case "updateSigChainBlockHash":
                                break;
                            default:
                                Debug.WriteLine("Unknown msg type: " + msg.Action);
                                break;
                        }
                    }
                    catch (Exception t)
                    {
                        Debug.WriteLine(t);
                    }
                };

                ws.OnClose += (sender, e) =>
                {
                    Debug.WriteLine("WebSocket unexpectedly closed: (" + e.Code + ") " + e.Reason);
                    if (!shouldReconnect)
                    {
                        return;
                    }
                    Reconnect();
                };

                ws.OnError += (sender, e) =>
                {
                    Debug.WriteLine(e.Message + ": " + e.Exception);
                };

                ws.Connect();
            }
            catch (Exception e)
            {
                Debug.WriteLine("Create WebSocket failed: " + e);
                if (!shouldReconnect)
                {
                    return;
                }
                Reconnect();
            }
        }

        private void Reconnect()
        {
            Task.Run(async () =>
            {
                Debug.WriteLine("Reconnecting in " + reconnectInterval / 1000 + "s...");
                await Task.Delay(reconnectInterval * 60000, CancellationToken.None);
                reconnectInterval *= 2;
                if (reconnectInterval > options.ReconnectIntervalMax)
                {
                    reconnectInterval = options.ReconnectIntervalMax;
                }
                Connect();
            }, CancellationToken.None).Wait(CancellationToken.None);
        }

        public void Close()
        {
            shouldReconnect = false;
            ws.Close();
        }

        public void Send(string dest, byte[] data, uint? maxHoldingSeconds = null)
        {
            Send(new HashSet<string> { dest }, data, maxHoldingSeconds);
        }

        public void Send(ISet<string> dests, byte[] data, uint? maxHoldingSeconds = null)
        {
            var msg = new OutboundMessage
            {
                Payload = ByteString.CopyFrom(data)
            };
            if (maxHoldingSeconds != null)
            {
                msg.MaxHoldingSeconds = maxHoldingSeconds.Value;
            }
            msg.Dests.AddRange(dests);
            ws.SendAsync(msg.ToByteArray(), _ => {});
        }
        
        private void HandleMsg(byte[] rawMsg)
        {
            var msg = InboundMessage.Parser.ParseFrom(rawMsg);
            var data = msg.Payload.ToByteArray();

            OnMessage?.Invoke(msg.Src, data);
        }
    }
}