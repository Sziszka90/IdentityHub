-- 1. Create a SQL Server login
CREATE LOGIN IdentityHubDB_Login
WITH PASSWORD = 'Secret12345!', -- Change this to a secure password
     CHECK_EXPIRATION = OFF,
     CHECK_POLICY = ON;

-- 2. Use the target database
USE IdentityHubDB;


-- 3. Create a database user mapped to the login
CREATE USER IdentityHubDB_User
FOR LOGIN IdentityHubDB_Login
WITH DEFAULT_SCHEMA = dbo;

-- 4. Grant roles/permissions
-- Basic read/write access
ALTER ROLE db_datareader ADD MEMBER IdentityHubDB_User;
ALTER ROLE db_datawriter ADD MEMBER IdentityHubDB_User;

ALTER ROLE db_ddladmin ADD MEMBER IdentityHubDB_User;
ALTER ROLE db_owner ADD MEMBER IdentityHubDB_User;

-- Optionally grant additional permissions, e.g.:
-- EXEC sp_addrolemember 'db_ddladmin', 'IdentityHubDB_User'; -- for DDL (create tables, etc.)
-- EXEC sp_addrolemember 'db_owner', 'IdentityHubDB_User'; -- full control (use with caution)
