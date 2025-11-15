-- =============================================
-- Add GuildInvitations Table
-- =============================================
-- Chạy script này để thêm bảng GuildInvitations vào database hiện có
-- =============================================

USE ChatAppDB;
GO

-- Kiểm tra xem bảng đã tồn tại chưa
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'GuildInvitations')
BEGIN
    -- Tạo bảng GuildInvitations
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
    
    PRINT '✓ Table GuildInvitations created successfully.';
END
ELSE
BEGIN
    PRINT '✓ Table GuildInvitations already exists.';
END
GO

-- Kiểm tra kết quả
SELECT 
    t.name AS TableName,
    (SELECT COUNT(*) FROM sys.columns c WHERE c.object_id = t.object_id) AS ColumnCount
FROM sys.tables t
WHERE t.name = 'GuildInvitations';
GO

