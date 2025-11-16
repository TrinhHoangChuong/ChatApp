-- =============================================
-- ChatApp Database Setup - Complete Script
-- =============================================
-- Server: localhost,1433
-- Database: ChatAppDB
-- Username: sa
-- Password: 123456
-- =============================================
-- Chạy script này để tạo database và tất cả tables
-- Bao gồm: Users, Messages, Guilds, Channels, Memberships, Invitations, Friends
-- =============================================

-- =============================================
-- BƯỚC 1: Tạo Database
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'ChatAppDB')
BEGIN
    CREATE DATABASE ChatAppDB;
    PRINT '✓ Database ChatAppDB created successfully.';
END
ELSE
BEGIN
    PRINT '✓ Database ChatAppDB already exists.';
END
GO

USE ChatAppDB;
GO

-- =============================================
-- BƯỚC 2: Xóa các tables cũ nếu có (để tạo lại)
-- =============================================
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'RoomMembers')
    DROP TABLE RoomMembers;
GO

IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Rooms')
    DROP TABLE Rooms;
GO

IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Friendships')
    DROP TABLE Friendships;
GO

IF EXISTS (SELECT * FROM sys.tables WHERE name = 'FriendRequests')
    DROP TABLE FriendRequests;
GO

IF EXISTS (SELECT * FROM sys.tables WHERE name = 'ChannelMemberships')
    DROP TABLE ChannelMemberships;
GO

IF EXISTS (SELECT * FROM sys.tables WHERE name = 'GuildInvitations')
    DROP TABLE GuildInvitations;
GO

IF EXISTS (SELECT * FROM sys.tables WHERE name = 'GuildMemberships')
    DROP TABLE GuildMemberships;
GO

IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Channels')
    DROP TABLE Channels;
GO

IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Guilds')
    DROP TABLE Guilds;
GO

IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Messages')
    DROP TABLE Messages;
GO

IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Users')
    DROP TABLE Users;
GO

PRINT '✓ Cleaned up old tables (if any).';
GO

-- =============================================
-- BƯỚC 3: Tạo tất cả Tables
-- =============================================

-- =============================================
-- 1. Bảng Users
-- =============================================
CREATE TABLE Users (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Username NVARCHAR(100) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(255) NOT NULL
);

CREATE INDEX IX_Users_Username ON Users(Username);
PRINT '✓ Table Users created.';
GO

-- =============================================
-- 2. Bảng Messages
-- =============================================
CREATE TABLE Messages (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Sender NVARCHAR(100) NOT NULL,
    Content NVARCHAR(MAX) NULL,
    MediaUrl NVARCHAR(500) NULL,
    Recipient NVARCHAR(100) NULL,
    ChannelId INT NULL,
    RoomId INT NULL,
    Type NVARCHAR(20) NOT NULL DEFAULT 'text',
    Timestamp DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

CREATE INDEX IX_Messages_Sender ON Messages(Sender);
CREATE INDEX IX_Messages_ChannelId ON Messages(ChannelId);
CREATE INDEX IX_Messages_RoomId ON Messages(RoomId);
CREATE INDEX IX_Messages_Recipient ON Messages(Recipient);
CREATE INDEX IX_Messages_Timestamp ON Messages(Timestamp);
PRINT '✓ Table Messages created.';
GO

-- =============================================
-- 3. Bảng Guilds (Servers)
-- =============================================
CREATE TABLE Guilds (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Name NVARCHAR(100) NOT NULL,
    Description NVARCHAR(255) NULL,
    OwnerId INT NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    FOREIGN KEY (OwnerId) REFERENCES Users(Id) ON DELETE CASCADE
);

CREATE INDEX IX_Guilds_OwnerId ON Guilds(OwnerId);
PRINT '✓ Table Guilds created.';
GO

-- =============================================
-- 4. Bảng Channels
-- =============================================
CREATE TABLE Channels (
    Id INT PRIMARY KEY IDENTITY(1,1),
    GuildId INT NOT NULL,
    Name NVARCHAR(100) NOT NULL,
    Topic NVARCHAR(255) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    FOREIGN KEY (GuildId) REFERENCES Guilds(Id) ON DELETE CASCADE
);

CREATE INDEX IX_Channels_GuildId ON Channels(GuildId);
PRINT '✓ Table Channels created.';
GO

-- =============================================
-- 5. Bảng GuildMemberships
-- =============================================
CREATE TABLE GuildMemberships (
    Id INT PRIMARY KEY IDENTITY(1,1),
    GuildId INT NOT NULL,
    UserId INT NOT NULL,
    Role NVARCHAR(50) NOT NULL DEFAULT 'member',
    JoinedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    FOREIGN KEY (GuildId) REFERENCES Guilds(Id) ON DELETE CASCADE,
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE NO ACTION,
    UNIQUE(GuildId, UserId)
);

CREATE INDEX IX_GuildMemberships_GuildId ON GuildMemberships(GuildId);
CREATE INDEX IX_GuildMemberships_UserId ON GuildMemberships(UserId);
PRINT '✓ Table GuildMemberships created.';
GO

-- =============================================
-- 5a. Bảng ChannelMemberships
-- =============================================
CREATE TABLE ChannelMemberships (
    Id INT PRIMARY KEY IDENTITY(1,1),
    ChannelId INT NOT NULL,
    UserId INT NOT NULL,
    JoinedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    FOREIGN KEY (ChannelId) REFERENCES Channels(Id) ON DELETE CASCADE,
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE NO ACTION,
    UNIQUE(ChannelId, UserId)
);

CREATE INDEX IX_ChannelMemberships_ChannelId ON ChannelMemberships(ChannelId);
CREATE INDEX IX_ChannelMemberships_UserId ON ChannelMemberships(UserId);
PRINT '✓ Table ChannelMemberships created.';
GO

-- =============================================
-- 5b. Bảng GuildInvitations
-- =============================================
CREATE TABLE GuildInvitations (
    Id INT PRIMARY KEY IDENTITY(1,1),
    GuildId INT NOT NULL,
    InviterId INT NOT NULL,
    InviteeId INT NOT NULL,
    Status NVARCHAR(20) NOT NULL DEFAULT 'Pending',
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    RespondedAt DATETIME2 NULL,
    FOREIGN KEY (GuildId) REFERENCES Guilds(Id) ON DELETE CASCADE,
    FOREIGN KEY (InviterId) REFERENCES Users(Id) ON DELETE NO ACTION,
    FOREIGN KEY (InviteeId) REFERENCES Users(Id) ON DELETE NO ACTION
);

CREATE INDEX IX_GuildInvitations_GuildId ON GuildInvitations(GuildId);
CREATE INDEX IX_GuildInvitations_InviteeId ON GuildInvitations(InviteeId);
CREATE INDEX IX_GuildInvitations_Status ON GuildInvitations(Status);

-- Tạo unique filtered index để đảm bảo mỗi user chỉ có 1 invitation pending cho mỗi guild
CREATE UNIQUE NONCLUSTERED INDEX IX_GuildInvitations_UniquePending 
    ON GuildInvitations(GuildId, InviteeId) 
    WHERE Status = 'Pending';

PRINT '✓ Table GuildInvitations created.';
GO

-- =============================================
-- 6. Bảng FriendRequests
-- =============================================
CREATE TABLE FriendRequests (
    Id INT PRIMARY KEY IDENTITY(1,1),
    SenderId INT NOT NULL,
    ReceiverId INT NOT NULL,
    Status NVARCHAR(20) NOT NULL DEFAULT 'Pending',
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    RespondedAt DATETIME2 NULL,
    FOREIGN KEY (SenderId) REFERENCES Users(Id) ON DELETE CASCADE,
    FOREIGN KEY (ReceiverId) REFERENCES Users(Id) ON DELETE NO ACTION,
    CHECK (SenderId != ReceiverId)
);

CREATE INDEX IX_FriendRequests_SenderId ON FriendRequests(SenderId);
CREATE INDEX IX_FriendRequests_ReceiverId ON FriendRequests(ReceiverId);
CREATE INDEX IX_FriendRequests_Status ON FriendRequests(Status);
PRINT '✓ Table FriendRequests created.';
GO

-- =============================================
-- 7. Bảng Friendships
-- =============================================
CREATE TABLE Friendships (
    Id INT PRIMARY KEY IDENTITY(1,1),
    UserId INT NOT NULL,
    FriendId INT NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
    FOREIGN KEY (FriendId) REFERENCES Users(Id) ON DELETE NO ACTION,
    CHECK (UserId != FriendId),
    UNIQUE(UserId, FriendId)
);

CREATE INDEX IX_Friendships_UserId ON Friendships(UserId);
CREATE INDEX IX_Friendships_FriendId ON Friendships(FriendId);
PRINT '✓ Table Friendships created.';
GO

-- =============================================
-- 8. Bảng Rooms (Optional - for future room feature)
-- =============================================
CREATE TABLE Rooms (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Name NVARCHAR(100) NOT NULL,
    Description NVARCHAR(255) NULL,
    IsPrivate BIT NOT NULL DEFAULT 0,
    CreatorId INT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

CREATE INDEX IX_Rooms_CreatorId ON Rooms(CreatorId);
PRINT '✓ Table Rooms created.';
GO

-- =============================================
-- 9. Bảng RoomMembers
-- =============================================
CREATE TABLE RoomMembers (
    Id INT PRIMARY KEY IDENTITY(1,1),
    RoomId INT NOT NULL,
    UserId INT NOT NULL,
    JoinedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    FOREIGN KEY (RoomId) REFERENCES Rooms(Id) ON DELETE CASCADE,
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
    UNIQUE(RoomId, UserId)
);

CREATE INDEX IX_RoomMembers_RoomId ON RoomMembers(RoomId);
CREATE INDEX IX_RoomMembers_UserId ON RoomMembers(UserId);
PRINT '✓ Table RoomMembers created.';
GO

-- =============================================
-- BƯỚC 4: Kiểm tra và báo cáo
-- =============================================
PRINT '';
PRINT '=============================================';
PRINT 'DATABASE SETUP COMPLETED!';
PRINT '=============================================';
PRINT 'Database: ChatAppDB';
PRINT 'Total tables created: 11';
PRINT '';
PRINT 'Tables:';
PRINT '  1. Users';
PRINT '  2. Messages';
PRINT '  3. Guilds';
PRINT '  4. Channels';
PRINT '  5. GuildMemberships';
PRINT '  6. ChannelMemberships';
PRINT '  7. GuildInvitations';
PRINT '  8. FriendRequests';
PRINT '  9. Friendships';
PRINT '  10. Rooms';
PRINT '  11. RoomMembers';
PRINT '';
PRINT 'You can now run your application!';
PRINT '=============================================';
GO

-- Hiển thị danh sách tables
SELECT 
    t.name AS TableName,
    (SELECT COUNT(*) FROM sys.columns c WHERE c.object_id = t.object_id) AS ColumnCount
FROM sys.tables t
WHERE t.type = 'U' AND t.name NOT LIKE 'sys%'
ORDER BY t.name;
GO
