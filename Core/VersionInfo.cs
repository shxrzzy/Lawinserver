namespace LawinServer.Core;

public sealed class VersionInfo
{
    public int Season { get; set; }
    public double Build { get; set; }
    public string CL { get; set; } = "";
    public string Lobby { get; set; } = "";

    public override string ToString() => $"Season={Season} Build={Build} CL={CL} Lobby={Lobby}";
}
