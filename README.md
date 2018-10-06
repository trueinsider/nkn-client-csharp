# nkn-client-csharp

C# implementation of NKN client.

Send and receive data between any NKN clients without setting up a server.

Note: This is a **client** version of the NKN protocol, which can send and
receive data but **not** relay data (mining). For **node** implementation which
can mine NKN token by relaying data, please refer to
[nkn](https://github.com/nknorg/nkn/).

**Note: This repository is in the early development stage and may not have all
functions working properly. It should be used only for testing now.**

## Usage

Create a client using an existing public key:

```cs
var client = new Client("02dfe490008ddc627a317ed319218e513e253a08b8988df5f536c1474b4d63c37f");
```

Or with an identifier (used to distinguish different clients sharing the same
public key):

```cs
var client = new Client("02dfe490008ddc627a317ed319218e513e253a08b8988df5f536c1474b4d63c37f", "any string");
```

Get client public key pair:

```cs
Console.WriteLine(client.PublicKey);
```

By default the client will use bootstrap RPC server (for getting node address)
provided by us. Any NKN full node can serve as a bootstrap RPC server. To create
a client using customized bootstrap RPC server:

```cs
var client = new Client(
    "02dfe490008ddc627a317ed319218e513e253a08b8988df5f536c1474b4d63c37f",
    options: new Client.Options
    {
        RpcServerAddr = "https://ip:port"
    }
);
```

Private key should be kept **SECRET**! Never put it in version control system
like here.

Get client identifier:

```cs
Console.WriteLine(client.Identifier);
```

And client NKN address, which is used to receive data from other clients:

```cs
Console.WriteLine(client.Addr);
```

Listen for connection established:

```cs
client.OnConnect += () =>
{
    Console.WriteLine("Connection opened.");
};
```

Send byte array to other clients:

```cs
client.Send("another client address", new byte[] {1, 2, 3, 4, 5});
```

Receive data from other clients:

```cs
// can also be async (src, data) => {}
client.OnMessage += (src, data) =>
{
    Console.WriteLine("Receive binary message from {0}: {1}", src, data);
};
```

## Contributing

**Can I submit a bug, suggestion or feature request?**

Yes. Please open an issue for that.

**Can I contribute patches?**

Yes, we appreciate your help! To make contributions, please fork the repo, push
your changes to the forked repo with signed-off commits, and open a pull request
here.

Please sign off your commit. This means adding a line "Signed-off-by: Name
<email>" at the end of each commit, indicating that you wrote the code and have
the right to pass it on as an open source patch. This can be done automatically
by adding -s when committing:

```shell
git commit -s
```

## Community

* [Discord](https://discord.gg/c7mTynX)
* [Telegram](https://t.me/nknorg)
* [Reddit](https://www.reddit.com/r/nknblockchain/)
* [Twitter](https://twitter.com/NKN_ORG)
