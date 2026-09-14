@{
    Environment = @{
        ConnectionStrings__AuthDb = 'Server=localhost;Database=concertable-auth;Trusted_Connection=True;TrustServerCertificate=True'
    }
    Migrations = @(
        @{ Context = 'PersistedGrantDbContext'; Project = 'api/src/Concertable.Auth'; StartupProject = 'api/src/Concertable.Auth'; OutputDir = 'Data/Migrations/Duende' }
        @{ Context = 'AuthDbContext'; Project = 'api/src/Concertable.Auth'; StartupProject = 'api/src/Concertable.Auth'; OutputDir = 'Data/Migrations/Auth' }
    )
}
