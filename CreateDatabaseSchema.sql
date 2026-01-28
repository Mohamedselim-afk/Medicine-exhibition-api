-- =============================================
-- Medicine Exhibition API - Database Schema
-- Create all tables from scratch
-- Database: db39286
-- =============================================

USE db39286;
GO

-- =============================================
-- Drop existing tables if they exist (in reverse order of dependencies)
-- =============================================
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Notifications]') AND type in (N'U'))
    DROP TABLE [dbo].[Notifications];
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[InvoiceItems]') AND type in (N'U'))
    DROP TABLE [dbo].[InvoiceItems];
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Invoices]') AND type in (N'U'))
    DROP TABLE [dbo].[Invoices];
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Products]') AND type in (N'U'))
    DROP TABLE [dbo].[Products];
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Users]') AND type in (N'U'))
    DROP TABLE [dbo].[Users];
GO

PRINT 'Existing tables dropped (if any)';
GO

-- =============================================
-- Table: Users
-- =============================================
CREATE TABLE [dbo].[Users] (
    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [Username] NVARCHAR(100) NOT NULL,
    [Email] NVARCHAR(255) NOT NULL,
    [PasswordHash] NVARCHAR(MAX) NOT NULL,
    [Role] NVARCHAR(50) NOT NULL DEFAULT 'Employee',
    [FullName] NVARCHAR(100) NULL,
    [PhoneNumber] NVARCHAR(30) NULL,
    [FcmToken] NVARCHAR(500) NULL,
    [ResetToken] NVARCHAR(500) NULL,
    [ResetTokenExpiry] DATETIME2 NULL,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [IsActive] BIT NOT NULL DEFAULT 1
);
GO

-- Create indexes for Users
CREATE UNIQUE INDEX [IX_Users_Username] ON [dbo].[Users]([Username]);
CREATE UNIQUE INDEX [IX_Users_Email] ON [dbo].[Users]([Email]);
CREATE INDEX [IX_Users_Role] ON [dbo].[Users]([Role]);
CREATE INDEX [IX_Users_IsActive] ON [dbo].[Users]([IsActive]);
GO

PRINT 'Table Users created successfully';
GO

-- =============================================
-- Table: Products
-- =============================================
CREATE TABLE [dbo].[Products] (
    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [Name] NVARCHAR(200) NOT NULL,
    [Price] DECIMAL(18,2) NOT NULL,
    [Dose] NVARCHAR(100) NULL,
    [Notes] NVARCHAR(1000) NULL,
    [LocationInStore] NVARCHAR(200) NULL,
    [ImageData] VARBINARY(MAX) NULL,
    [ImageContentType] NVARCHAR(50) NULL,
    [StockQuantity] INT NOT NULL DEFAULT 0,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [IsActive] BIT NOT NULL DEFAULT 1
);
GO

-- Create indexes for Products
CREATE INDEX [IX_Products_Name] ON [dbo].[Products]([Name]);
CREATE INDEX [IX_Products_IsActive] ON [dbo].[Products]([IsActive]);
CREATE INDEX [IX_Products_StockQuantity] ON [dbo].[Products]([StockQuantity]);
GO

PRINT 'Table Products created successfully';
GO

-- =============================================
-- Table: Invoices
-- =============================================
CREATE TABLE [dbo].[Invoices] (
    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [CustomerName] NVARCHAR(100) NOT NULL,
    [TotalAmount] DECIMAL(18,2) NOT NULL,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [CreatedByUserId] INT NOT NULL,
    [IsConfirmed] BIT NOT NULL DEFAULT 0,
    [IsViewedByOwner] BIT NOT NULL DEFAULT 0,
    [IsDeletedByOwner] BIT NOT NULL DEFAULT 0,
    [DeletedByOwnerAt] DATETIME2 NULL,
    CONSTRAINT [FK_Invoices_Users] FOREIGN KEY ([CreatedByUserId]) 
        REFERENCES [dbo].[Users]([Id]) ON DELETE NO ACTION
);
GO

-- Create indexes for Invoices
CREATE INDEX [IX_Invoices_CreatedByUserId] ON [dbo].[Invoices]([CreatedByUserId]);
CREATE INDEX [IX_Invoices_CreatedAt] ON [dbo].[Invoices]([CreatedAt]);
CREATE INDEX [IX_Invoices_IsConfirmed] ON [dbo].[Invoices]([IsConfirmed]);
CREATE INDEX [IX_Invoices_IsViewedByOwner] ON [dbo].[Invoices]([IsViewedByOwner]);
CREATE INDEX [IX_Invoices_IsDeletedByOwner] ON [dbo].[Invoices]([IsDeletedByOwner]);
GO

PRINT 'Table Invoices created successfully';
GO

-- =============================================
-- Table: InvoiceItems
-- =============================================
CREATE TABLE [dbo].[InvoiceItems] (
    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [InvoiceId] INT NOT NULL,
    [ProductId] INT NOT NULL,
    [Quantity] INT NOT NULL,
    [UnitPrice] DECIMAL(18,2) NOT NULL,
    [TotalPrice] DECIMAL(18,2) NOT NULL,
    CONSTRAINT [FK_InvoiceItems_Invoices] FOREIGN KEY ([InvoiceId]) 
        REFERENCES [dbo].[Invoices]([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_InvoiceItems_Products] FOREIGN KEY ([ProductId]) 
        REFERENCES [dbo].[Products]([Id]) ON DELETE NO ACTION
);
GO

-- Create indexes for InvoiceItems
CREATE INDEX [IX_InvoiceItems_InvoiceId] ON [dbo].[InvoiceItems]([InvoiceId]);
CREATE INDEX [IX_InvoiceItems_ProductId] ON [dbo].[InvoiceItems]([ProductId]);
GO

PRINT 'Table InvoiceItems created successfully';
GO

-- =============================================
-- Table: Notifications
-- =============================================
CREATE TABLE [dbo].[Notifications] (
    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [UserId] INT NOT NULL,
    [Title] NVARCHAR(200) NOT NULL,
    [Message] NVARCHAR(1000) NOT NULL,
    [InvoiceId] INT NULL,
    [IsRead] BIT NOT NULL DEFAULT 0,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);
GO

-- Create indexes for Notifications
CREATE INDEX [IX_Notifications_UserId] ON [dbo].[Notifications]([UserId]);
CREATE INDEX [IX_Notifications_IsRead] ON [dbo].[Notifications]([IsRead]);
CREATE INDEX [IX_Notifications_CreatedAt] ON [dbo].[Notifications]([CreatedAt]);
GO

PRINT 'Table Notifications created successfully';
GO

-- =============================================
-- Summary
-- =============================================
PRINT '';
PRINT '=============================================';
PRINT 'Database schema created successfully!';
PRINT '=============================================';
PRINT 'Tables created:';
PRINT '  1. Users';
PRINT '  2. Products';
PRINT '  3. Invoices';
PRINT '  4. InvoiceItems';
PRINT '  5. Notifications';
PRINT '';
PRINT 'Note: Use /api/Auth/create-owner endpoint to create the first Owner account';
PRINT '=============================================';
GO

