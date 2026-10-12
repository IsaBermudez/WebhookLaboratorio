-- Base de datos compartida del laboratorio de webhooks.
-- Los IDs los genera exclusivamente SQL Server (IDENTITY(1,1)).
IF DB_ID('WebhookLaboratorio') IS NULL
    CREATE DATABASE WebhookLaboratorio;
GO

USE WebhookLaboratorio;
GO

IF OBJECT_ID('dbo.Usuarios', 'U') IS NULL
CREATE TABLE Usuarios
(
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Nombre VARCHAR(100) NOT NULL,
    Email VARCHAR(150) NOT NULL,
    FechaCreacion DATETIME NOT NULL
);
GO

IF OBJECT_ID('dbo.Eventos', 'U') IS NULL
CREATE TABLE Eventos
(
    Id INT IDENTITY(1,1) PRIMARY KEY,
    EventType VARCHAR(100) NOT NULL,
    Estado VARCHAR(20) NOT NULL,
    FechaRecepcion DATETIME NOT NULL
);
GO
