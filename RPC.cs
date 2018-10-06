using System.Net.Http;
using System.Threading.Tasks;
using Utf8Json;
using Utf8Json.Resolvers;

namespace NKN.Client
{
    public static class RPC
    {
        public interface IParams<R>
        {
        }

        public class GetWSAddrParams : IParams<string>
        {
            private string address;

            public GetWSAddrParams(string address)
            {
                this.address = address;
            }
        }

        public class SendRawTxParams : IParams<string>
        {
            private string tx;

            public SendRawTxParams(string tx)
            {
                this.tx = tx;
            }
        }

        public class TransactionInfo
        {
            public readonly string Hash;
        }

        public class GetTxParams : IParams<TransactionInfo>
        {
            private string hash;

            public GetTxParams(string hash)
            {
                this.hash = hash;
            }
        }

        private class Request<T, R> where T : IParams<R>
        {
            private string jsonrpc = "2.0";
            private string method;
            private T parameters;

            public Request(string method, T parameters)
            {
                this.method = method;
                this.parameters = parameters;
            }
        }

        public class Response<R>
        {
            public class Error
            {
                public enum Code
                {
                    SUCCESS = 0,
                    SESSION_EXPIRED = -41001,
                    SERVICE_CEILING = -41002,
                    ILLEGAL_DATAFORMAT = -41003,
                    INVALID_METHOD = -42001,
                    INVALID_PARAMS = -42002,
                    INVALID_TOKEN = -42003,
                    INVALID_TRANSACTION = -43001,
                    INVALID_ASSET = -43002,
                    INVALID_BLOCK = -43003,
                    INVALID_HASH = -43004,
                    INVALID_VERSION = -43005,
                    UNKNOWN_TRANSACTION = -44001,
                    UNKNOWN_ASSET = -44002,
                    UNKNOWN_BLOCK = -44003,
                    UNKNOWN_HASH = -44004,
                    INTERNAL_ERROR = -45001,
                    SMARTCODE_ERROR = -47001,
                    WRONG_NODE = -48001
                }

                public Code code;
                public string message;
            }

            public R result;
            public Error error;
        }

        private static readonly HttpClient Client = new HttpClient();

        static RPC()
        {
            JsonSerializer.SetDefaultResolver(StandardResolver.AllowPrivateExcludeNull);
        }

        public static async Task<Response<R>> Call<T, R>(string addr, string method, T parameters) where T : IParams<R> {
            var request = new Request<T, R>(method, parameters);
            var content = new ByteArrayContent(JsonSerializer.Serialize(request));
            var response = await Client.PostAsync(addr, content);
            var responseString = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<Response<R>>(responseString);
        }
    }
}