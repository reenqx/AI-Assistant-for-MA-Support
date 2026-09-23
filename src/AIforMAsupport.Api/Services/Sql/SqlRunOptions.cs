namespace AIforMAsupport.Api.Services.Sql;

public sealed class SqlRunOptions
{
    public const string SectionName = "SqlRun";

    // Defaults to off on purpose - the demo/prod baseline (appsettings.json) stays disabled,
    // and only appsettings.Development.json (this developer's local run) turns it on. Flipping
    // this off is also the fastest way to fall back to copy-only SQL Studio if the live feature
    // misbehaves right before a demo.
    public bool Enabled { get; set; }

    // Separate kill switch from Enabled, specifically for DELETE/UPDATE/INSERT - the user
    // explicitly asked for real writes to be runnable (not just SELECT), understanding the risk,
    // but this stays its own flag so it can be turned off independently without losing read-only
    // SQL Studio entirely if something goes wrong. Defaults to off for the same reason as Enabled.
    public bool AllowWrites { get; set; }

    public string ConnectionStringName { get; set; } = "MoCS";

    public int MaxRowsPerTable { get; set; } = 500;

    public int CommandTimeoutSeconds { get; set; } = 30;
}
