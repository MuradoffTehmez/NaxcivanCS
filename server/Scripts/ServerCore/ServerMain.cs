using Godot;
using NaxcivanCS.Shared.Constants;

namespace NaxcivanCS.Server.ServerCore;

/// <summary>
/// PRD 87, 99 - Linux headless rejimində işləyən dedicated server giriş nöqtəsi.
/// İstifadə: <c>godot --headless --path server -- --port 27015 --map NC_Qala</c>
/// </summary>
public sealed partial class ServerMain : Node
{
    private Network.NetworkServer? _network;

    public override void _Ready()
    {
        int port = ParsePort(OS.GetCmdlineUserArgs()) ?? GameConstants.DefaultServerPort;

        _network = new Network.NetworkServer { Name = "NetworkServer" };
        AddChild(_network);

        if (_network.Listen(port) != Error.Ok)
        {
            GetTree().Quit(1);
        }
    }

    private static int? ParsePort(string[] args)
    {
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "--port" && int.TryParse(args[i + 1], out int port))
            {
                return port;
            }
        }

        return null;
    }
}
