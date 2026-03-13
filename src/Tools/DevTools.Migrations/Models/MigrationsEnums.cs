namespace DevTools.Migrations.Models;

public enum MigrationsAction
{
    AddMigration  = 0,
    UpdateDatabase = 1
}

public enum DatabaseProvider
{
    SqlServer = 0,
    Sqlite    = 1
}
