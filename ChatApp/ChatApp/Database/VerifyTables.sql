-- =============================================
-- Script kiểm tra tất cả Tables
-- =============================================
-- Chạy script này để xem tất cả tables đã được tạo chưa
-- =============================================

USE ChatAppDB;
GO

PRINT '=============================================';
PRINT 'Checking all required tables...';
PRINT '=============================================';
GO

-- Kiểm tra từng table
DECLARE @TableCount INT = 0;
DECLARE @MissingTables NVARCHAR(MAX) = '';

-- 1. Users
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Users')
BEGIN
    SET @TableCount = @TableCount + 1;
    PRINT '✓ Table Users exists';
END
ELSE
BEGIN
    SET @MissingTables = @MissingTables + 'Users, ';
    PRINT '✗ Table Users MISSING';
END

-- 2. Messages
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Messages')
BEGIN
    SET @TableCount = @TableCount + 1;
    PRINT '✓ Table Messages exists';
END
ELSE
BEGIN
    SET @MissingTables = @MissingTables + 'Messages, ';
    PRINT '✗ Table Messages MISSING';
END

-- 3. Guilds
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Guilds')
BEGIN
    SET @TableCount = @TableCount + 1;
    PRINT '✓ Table Guilds exists';
END
ELSE
BEGIN
    SET @MissingTables = @MissingTables + 'Guilds, ';
    PRINT '✗ Table Guilds MISSING';
END

-- 4. Channels
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Channels')
BEGIN
    SET @TableCount = @TableCount + 1;
    PRINT '✓ Table Channels exists';
END
ELSE
BEGIN
    SET @MissingTables = @MissingTables + 'Channels, ';
    PRINT '✗ Table Channels MISSING';
END

-- 5. GuildMemberships
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'GuildMemberships')
BEGIN
    SET @TableCount = @TableCount + 1;
    PRINT '✓ Table GuildMemberships exists';
END
ELSE
BEGIN
    SET @MissingTables = @MissingTables + 'GuildMemberships, ';
    PRINT '✗ Table GuildMemberships MISSING';
END

-- 6. FriendRequests
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'FriendRequests')
BEGIN
    SET @TableCount = @TableCount + 1;
    PRINT '✓ Table FriendRequests exists';
END
ELSE
BEGIN
    SET @MissingTables = @MissingTables + 'FriendRequests, ';
    PRINT '✗ Table FriendRequests MISSING';
END

-- 7. Friendships
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Friendships')
BEGIN
    SET @TableCount = @TableCount + 1;
    PRINT '✓ Table Friendships exists';
END
ELSE
BEGIN
    SET @MissingTables = @MissingTables + 'Friendships, ';
    PRINT '✗ Table Friendships MISSING';
END

PRINT '';
PRINT '=============================================';
PRINT 'Summary:';
PRINT '=============================================';
PRINT 'Total tables found: ' + CAST(@TableCount AS NVARCHAR(10)) + ' / 7';

IF @TableCount = 7
BEGIN
    PRINT '✓ All tables are created successfully!';
END
ELSE
BEGIN
    PRINT '✗ Missing tables: ' + LEFT(@MissingTables, LEN(@MissingTables) - 2);
    PRINT 'Please run CreateMissingTables.sql to create missing tables.';
END

PRINT '=============================================';
GO

-- Hiển thị danh sách tất cả tables
PRINT '';
PRINT 'All tables in ChatAppDB:';
SELECT 
    t.name AS TableName,
    (SELECT COUNT(*) FROM sys.columns c WHERE c.object_id = t.object_id) AS ColumnCount
FROM sys.tables t
WHERE t.type = 'U' AND t.name NOT LIKE 'sys%'
ORDER BY t.name;
GO

