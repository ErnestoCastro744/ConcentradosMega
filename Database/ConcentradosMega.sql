IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122004825_Inicial'
)
BEGIN
    CREATE TABLE [AspNetRoles] (
        [Id] nvarchar(450) NOT NULL,
        [Name] nvarchar(256) NULL,
        [NormalizedName] nvarchar(256) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122004825_Inicial'
)
BEGIN
    CREATE TABLE [AspNetUsers] (
        [Id] nvarchar(450) NOT NULL,
        [NombreCompleto] nvarchar(100) NOT NULL,
        [Activo] bit NOT NULL,
        [UserName] nvarchar(256) NULL,
        [NormalizedUserName] nvarchar(256) NULL,
        [Email] nvarchar(256) NULL,
        [NormalizedEmail] nvarchar(256) NULL,
        [EmailConfirmed] bit NOT NULL,
        [PasswordHash] nvarchar(max) NULL,
        [SecurityStamp] nvarchar(max) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        [PhoneNumber] nvarchar(max) NULL,
        [PhoneNumberConfirmed] bit NOT NULL,
        [TwoFactorEnabled] bit NOT NULL,
        [LockoutEnd] datetimeoffset NULL,
        [LockoutEnabled] bit NOT NULL,
        [AccessFailedCount] int NOT NULL,
        CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122004825_Inicial'
)
BEGIN
    CREATE TABLE [Clientes] (
        [Id] int NOT NULL IDENTITY,
        [Cedula] nvarchar(20) NOT NULL,
        [Nombre] nvarchar(100) NOT NULL,
        [Telefono] nvarchar(20) NULL,
        [Correo] nvarchar(100) NULL,
        [Direccion] nvarchar(250) NULL,
        [Estado] bit NOT NULL,
        CONSTRAINT [PK_Clientes] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122004825_Inicial'
)
BEGIN
    CREATE TABLE [Productos] (
        [Id] int NOT NULL IDENTITY,
        [Codigo] nvarchar(20) NOT NULL,
        [Nombre] nvarchar(100) NOT NULL,
        [Descripcion] nvarchar(250) NULL,
        [Precio] decimal(12,2) NOT NULL,
        [StockMinimo] int NOT NULL,
        [Estado] bit NOT NULL,
        CONSTRAINT [PK_Productos] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122004825_Inicial'
)
BEGIN
    CREATE TABLE [AspNetRoleClaims] (
        [Id] int NOT NULL IDENTITY,
        [RoleId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122004825_Inicial'
)
BEGIN
    CREATE TABLE [AspNetUserClaims] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122004825_Inicial'
)
BEGIN
    CREATE TABLE [AspNetUserLogins] (
        [LoginProvider] nvarchar(450) NOT NULL,
        [ProviderKey] nvarchar(450) NOT NULL,
        [ProviderDisplayName] nvarchar(max) NULL,
        [UserId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
        CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122004825_Inicial'
)
BEGIN
    CREATE TABLE [AspNetUserRoles] (
        [UserId] nvarchar(450) NOT NULL,
        [RoleId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
        CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122004825_Inicial'
)
BEGIN
    CREATE TABLE [AspNetUserTokens] (
        [UserId] nvarchar(450) NOT NULL,
        [LoginProvider] nvarchar(450) NOT NULL,
        [Name] nvarchar(450) NOT NULL,
        [Value] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
        CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122004825_Inicial'
)
BEGIN
    CREATE TABLE [Rutas] (
        [Id] int NOT NULL IDENTITY,
        [Nombre] nvarchar(100) NOT NULL,
        [Fecha] datetime2 NOT NULL,
        [RepartidorId] nvarchar(450) NOT NULL,
        [Estado] nvarchar(20) NOT NULL,
        CONSTRAINT [PK_Rutas] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Rutas_AspNetUsers_RepartidorId] FOREIGN KEY ([RepartidorId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122004825_Inicial'
)
BEGIN
    CREATE TABLE [Pedidos] (
        [Id] int NOT NULL IDENTITY,
        [Guid] uniqueidentifier NOT NULL,
        [ClienteId] int NOT NULL,
        [VendedorId] nvarchar(450) NOT NULL,
        [Fecha] datetime2 NOT NULL,
        [Estado] nvarchar(20) NOT NULL,
        [Total] decimal(12,2) NOT NULL,
        [MotivoAnulacion] nvarchar(250) NULL,
        [OrigenLocal] bit NOT NULL,
        CONSTRAINT [PK_Pedidos] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Pedido_Estado] CHECK ([Estado] IN ('Pendiente','Confirmado','Anulado')),
        CONSTRAINT [FK_Pedidos_AspNetUsers_VendedorId] FOREIGN KEY ([VendedorId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Pedidos_Clientes_ClienteId] FOREIGN KEY ([ClienteId]) REFERENCES [Clientes] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122004825_Inicial'
)
BEGIN
    CREATE TABLE [DetallesPedido] (
        [Id] int NOT NULL IDENTITY,
        [PedidoId] int NOT NULL,
        [ProductoId] int NOT NULL,
        [Cantidad] int NOT NULL,
        [PrecioUnitario] decimal(12,2) NOT NULL,
        [Subtotal] decimal(12,2) NOT NULL,
        CONSTRAINT [PK_DetallesPedido] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Detalle_Cantidad] CHECK ([Cantidad] > 0),
        CONSTRAINT [FK_DetallesPedido_Pedidos_PedidoId] FOREIGN KEY ([PedidoId]) REFERENCES [Pedidos] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_DetallesPedido_Productos_ProductoId] FOREIGN KEY ([ProductoId]) REFERENCES [Productos] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122004825_Inicial'
)
BEGIN
    CREATE TABLE [MovimientosInventario] (
        [Id] int NOT NULL IDENTITY,
        [ProductoId] int NOT NULL,
        [Tipo] nvarchar(10) NOT NULL,
        [Cantidad] int NOT NULL,
        [Fecha] datetime2 NOT NULL,
        [PedidoId] int NULL,
        [Observacion] nvarchar(250) NULL,
        CONSTRAINT [PK_MovimientosInventario] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Mov_Cantidad] CHECK ([Cantidad] > 0),
        CONSTRAINT [CK_Mov_Tipo] CHECK ([Tipo] IN ('Entrada','Salida')),
        CONSTRAINT [FK_MovimientosInventario_Pedidos_PedidoId] FOREIGN KEY ([PedidoId]) REFERENCES [Pedidos] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_MovimientosInventario_Productos_ProductoId] FOREIGN KEY ([ProductoId]) REFERENCES [Productos] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122004825_Inicial'
)
BEGIN
    CREATE TABLE [RutaPedidos] (
        [Id] int NOT NULL IDENTITY,
        [RutaId] int NOT NULL,
        [PedidoId] int NOT NULL,
        [Orden] int NOT NULL,
        [EstadoEntrega] nvarchar(20) NOT NULL,
        [FechaEntrega] datetime2 NULL,
        [Observacion] nvarchar(250) NULL,
        CONSTRAINT [PK_RutaPedidos] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RutaPedidos_Pedidos_PedidoId] FOREIGN KEY ([PedidoId]) REFERENCES [Pedidos] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_RutaPedidos_Rutas_RutaId] FOREIGN KEY ([RutaId]) REFERENCES [Rutas] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122004825_Inicial'
)
BEGIN
    CREATE TABLE [Ventas] (
        [Id] int NOT NULL IDENTITY,
        [PedidoId] int NOT NULL,
        [Fecha] datetime2 NOT NULL,
        [Total] decimal(12,2) NOT NULL,
        CONSTRAINT [PK_Ventas] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Ventas_Pedidos_PedidoId] FOREIGN KEY ([PedidoId]) REFERENCES [Pedidos] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122004825_Inicial'
)
BEGIN
    CREATE TABLE [Facturas] (
        [Id] int NOT NULL IDENTITY,
        [VentaId] int NOT NULL,
        [Consecutivo] int NOT NULL,
        [FechaEmision] datetime2 NOT NULL,
        [ArchivoPdf] varbinary(max) NULL,
        CONSTRAINT [PK_Facturas] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Facturas_Ventas_VentaId] FOREIGN KEY ([VentaId]) REFERENCES [Ventas] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122004825_Inicial'
)
BEGIN
    CREATE TABLE [Pagos] (
        [Id] int NOT NULL IDENTITY,
        [VentaId] int NOT NULL,
        [Monto] decimal(12,2) NOT NULL,
        [Metodo] nvarchar(20) NOT NULL,
        [Estado] nvarchar(20) NOT NULL,
        [Referencia] nvarchar(50) NULL,
        [Fecha] datetime2 NOT NULL,
        CONSTRAINT [PK_Pagos] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Pago_Estado] CHECK ([Estado] IN ('Pendiente','Aprobado','Rechazado')),
        CONSTRAINT [CK_Pago_Monto] CHECK ([Monto] > 0),
        CONSTRAINT [FK_Pagos_Ventas_VentaId] FOREIGN KEY ([VentaId]) REFERENCES [Ventas] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122004825_Inicial'
)
BEGIN
    CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122004825_Inicial'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122004825_Inicial'
)
BEGIN
    CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122004825_Inicial'
)
BEGIN
    CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122004825_Inicial'
)
BEGIN
    CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122004825_Inicial'
)
BEGIN
    CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122004825_Inicial'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122004825_Inicial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Clientes_Cedula] ON [Clientes] ([Cedula]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122004825_Inicial'
)
BEGIN
    CREATE INDEX [IX_DetallesPedido_PedidoId] ON [DetallesPedido] ([PedidoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122004825_Inicial'
)
BEGIN
    CREATE INDEX [IX_DetallesPedido_ProductoId] ON [DetallesPedido] ([ProductoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122004825_Inicial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Facturas_Consecutivo] ON [Facturas] ([Consecutivo]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122004825_Inicial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Facturas_VentaId] ON [Facturas] ([VentaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122004825_Inicial'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_MovimientosInventario_PedidoId_ProductoId_Tipo] ON [MovimientosInventario] ([PedidoId], [ProductoId], [Tipo]) WHERE [PedidoId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122004825_Inicial'
)
BEGIN
    CREATE INDEX [IX_MovimientosInventario_ProductoId] ON [MovimientosInventario] ([ProductoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122004825_Inicial'
)
BEGIN
    CREATE INDEX [IX_Pagos_VentaId] ON [Pagos] ([VentaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122004825_Inicial'
)
BEGIN
    CREATE INDEX [IX_Pedidos_ClienteId] ON [Pedidos] ([ClienteId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122004825_Inicial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Pedidos_Guid] ON [Pedidos] ([Guid]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122004825_Inicial'
)
BEGIN
    CREATE INDEX [IX_Pedidos_VendedorId] ON [Pedidos] ([VendedorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122004825_Inicial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Productos_Codigo] ON [Productos] ([Codigo]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122004825_Inicial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_RutaPedidos_PedidoId] ON [RutaPedidos] ([PedidoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122004825_Inicial'
)
BEGIN
    CREATE INDEX [IX_RutaPedidos_RutaId] ON [RutaPedidos] ([RutaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122004825_Inicial'
)
BEGIN
    CREATE INDEX [IX_Rutas_RepartidorId] ON [Rutas] ([RepartidorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122004825_Inicial'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Ventas_PedidoId] ON [Ventas] ([PedidoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122004825_Inicial'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261122004825_Inicial', N'10.0.15');
END;

COMMIT;
GO

