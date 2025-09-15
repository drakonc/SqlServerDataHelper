CREATE DATABASE SqlHelperTestDb;
GO
USE SqlHelperTestDb;
GO

-- Tabla principal
CREATE TABLE Medico (
    Id INT IDENTITY PRIMARY KEY,
    Nombre NVARCHAR(100) NOT NULL,
    Especialidad NVARCHAR(100) NULL
);

-- Insertar data de prueba
INSERT INTO Medico (Nombre, Especialidad)
VALUES ('Juan Perez', 'Cardiología'),
       ('Maria Lopez', 'Neurología'),
       ('Carlos Sanchez', 'Pediatría');
GO

-- Stored Procedure: obtener todos los médicos
CREATE OR ALTER PROCEDURE sp_GetMedicos
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, Nombre, Especialidad FROM Medico;
END
GO

-- Stored Procedure: obtener médico por Id
CREATE OR ALTER PROCEDURE sp_GetMedicoById
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, Nombre, Especialidad FROM Medico WHERE Id = @Id;
END
GO

-- Stored Procedure: insertar médico
CREATE OR ALTER PROCEDURE sp_InsertMedico
    @Nombre NVARCHAR(100),
    @Especialidad NVARCHAR(100)
AS
BEGIN
    INSERT INTO Medico (Nombre, Especialidad) VALUES (@Nombre, @Especialidad);
END
GO

-- Stored Procedure: paginado
CREATE OR ALTER PROCEDURE sp_GetMedicosPaged
    @PageNumber INT,
    @PageSize INT,
    @TotalRecords INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT @TotalRecords = COUNT(*) FROM Medico;

    SELECT Id, Nombre, Especialidad
    FROM Medico
    ORDER BY Id
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END
GO