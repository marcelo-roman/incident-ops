namespace IncidentOps.Infrastructure.Persistence;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";
    public const string ConnectionStringName = "IncidentOps";

    public bool ApplyMigrations { get; set; }

    public bool Seed { get; set; }
}
