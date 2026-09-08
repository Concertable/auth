@{
    Environment = @{
        ConnectionStrings__AuthDb = 'Server=localhost;Database=concertable-auth;Trusted_Connection=True;TrustServerCertificate=True'
    }
    Migrations = @(
        @{ Context = 'PersistedGrantDbContext'; Project = 'src/Concertable.Auth'; StartupProject = 'src/Concertable.Auth'; OutputDir = 'Data/Migrations/Duende' }
        @{ Context = 'AuthDbContext'; Project = 'src/Concertable.Auth'; StartupProject = 'src/Concertable.Auth'; OutputDir = 'Data/Migrations/Auth' }
    )
}
