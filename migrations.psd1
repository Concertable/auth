@{
    Environment = @{
        ConnectionStrings__AuthDb = 'Host=localhost;Database=concertable-auth;Username=postgres;Password=postgres'
    }
    Migrations = @(
        @{ Context = 'PersistedGrantDbContext'; Project = 'api/src/Concertable.Auth'; StartupProject = 'api/src/Concertable.Auth'; OutputDir = 'Data/Migrations/Duende' }
        @{ Context = 'AuthDbContext'; Project = 'api/src/Concertable.Auth'; StartupProject = 'api/src/Concertable.Auth'; OutputDir = 'Data/Migrations/Auth' }
    )
}
