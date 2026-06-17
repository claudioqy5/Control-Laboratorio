BEGIN TRANSACTION;
GO

CREATE TABLE [Libros] (
    [LibroID] int NOT NULL IDENTITY,
    [NroRegistro] nvarchar(50) NOT NULL,
    [CodigoBarras] nvarchar(50) NOT NULL,
    [NroClasificacion] nvarchar(100) NOT NULL,
    [Titulo] nvarchar(250) NOT NULL,
    [Autor] nvarchar(150) NOT NULL,
    [Anio] nvarchar(10) NOT NULL,
    [Editorial] nvarchar(150) NOT NULL,
    [Edicion] nvarchar(50) NOT NULL,
    [Portada] nvarchar(max) NULL,
    [Categoria] nvarchar(100) NOT NULL,
    [Idioma] nvarchar(50) NOT NULL,
    [Estante] int NULL,
    [Cara] nvarchar(1) NULL,
    [Piso] int NULL,
    [Estado] nvarchar(30) NOT NULL,
    [Resumen] nvarchar(max) NULL,
    [Paginas] int NOT NULL,
    CONSTRAINT [PK_Libros] PRIMARY KEY ([LibroID])
);
GO

CREATE TABLE [Favoritos] (
    [FavoritoID] int NOT NULL IDENTITY,
    [AlumnoID] int NOT NULL,
    [LibroID] int NOT NULL,
    CONSTRAINT [PK_Favoritos] PRIMARY KEY ([FavoritoID]),
    CONSTRAINT [FK_Favoritos_Alumnos_AlumnoID] FOREIGN KEY ([AlumnoID]) REFERENCES [Alumnos] ([AlumnoID]) ON DELETE CASCADE,
    CONSTRAINT [FK_Favoritos_Libros_LibroID] FOREIGN KEY ([LibroID]) REFERENCES [Libros] ([LibroID]) ON DELETE CASCADE
);
GO

CREATE TABLE [Prestamos] (
    [PrestamoID] int NOT NULL IDENTITY,
    [AlumnoID] int NOT NULL,
    [LibroID] int NOT NULL,
    [FechaPrestamo] datetime2 NOT NULL,
    [FechaDevolucion] datetime2 NOT NULL,
    [FechaEntregado] datetime2 NULL,
    [Estado] nvarchar(30) NOT NULL,
    CONSTRAINT [PK_Prestamos] PRIMARY KEY ([PrestamoID]),
    CONSTRAINT [FK_Prestamos_Alumnos_AlumnoID] FOREIGN KEY ([AlumnoID]) REFERENCES [Alumnos] ([AlumnoID]) ON DELETE CASCADE,
    CONSTRAINT [FK_Prestamos_Libros_LibroID] FOREIGN KEY ([LibroID]) REFERENCES [Libros] ([LibroID]) ON DELETE CASCADE
);
GO

CREATE TABLE [Multas] (
    [MultaID] int NOT NULL IDENTITY,
    [AlumnoID] int NOT NULL,
    [PrestamoID] int NOT NULL,
    [Monto] decimal(18,2) NOT NULL,
    [Estado] nvarchar(30) NOT NULL,
    [FechaEmision] datetime2 NOT NULL,
    CONSTRAINT [PK_Multas] PRIMARY KEY ([MultaID]),
    CONSTRAINT [FK_Multas_Alumnos_AlumnoID] FOREIGN KEY ([AlumnoID]) REFERENCES [Alumnos] ([AlumnoID]) ON DELETE CASCADE,
    CONSTRAINT [FK_Multas_Prestamos_PrestamoID] FOREIGN KEY ([PrestamoID]) REFERENCES [Prestamos] ([PrestamoID]) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_Favoritos_AlumnoID] ON [Favoritos] ([AlumnoID]);
GO

CREATE INDEX [IX_Favoritos_LibroID] ON [Favoritos] ([LibroID]);
GO

CREATE UNIQUE INDEX [IX_Libros_CodigoBarras] ON [Libros] ([CodigoBarras]);
GO

CREATE UNIQUE INDEX [IX_Libros_NroRegistro] ON [Libros] ([NroRegistro]);
GO

CREATE INDEX [IX_Multas_AlumnoID] ON [Multas] ([AlumnoID]);
GO

CREATE INDEX [IX_Multas_PrestamoID] ON [Multas] ([PrestamoID]);
GO

CREATE INDEX [IX_Prestamos_AlumnoID] ON [Prestamos] ([AlumnoID]);
GO

CREATE INDEX [IX_Prestamos_LibroID] ON [Prestamos] ([LibroID]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260617152754_AddUbicacionLibro', N'8.0.0');
GO

COMMIT;
GO

