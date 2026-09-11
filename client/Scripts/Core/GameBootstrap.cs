using Godot;
using NaxcivanCS.Shared.Net;

namespace NaxcivanCS.Client.Core;

/// <summary>PRD 152 - Prototype 0.1 giriş nöqtəsi.</summary>
public sealed partial class GameBootstrap : Node
{
    public override void _Ready()
    {
        GD.Print($"NaxcivanCS Client — {VersionGate.Describe()}");
    }
}
