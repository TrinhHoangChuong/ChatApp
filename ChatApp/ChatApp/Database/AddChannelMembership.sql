-- =============================================
-- Add ChannelMemberships Table
-- =============================================
-- Chạy script này để thêm bảng ChannelMemberships vào database hiện có
-- =============================================

USE ChatAppDB;
GO

-- Kiểm tra xem bảng đã tồn tại chưa
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ChannelMemberships')
BEGIN
    -- Tạo bảng ChannelMemberships
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
    
    PRINT '✓ Table ChannelMemberships created successfully.';
END
ELSE
BEGIN
    PRINT '✓ Table ChannelMemberships already exists.';
END
GO

-- Kiểm tra kết quả
SELECT 
    t.name AS TableName,
    (SELECT COUNT(*) FROM sys.columns c WHERE c.object_id = t.object_id) AS ColumnCount
FROM sys.tables t
WHERE t.name = 'ChannelMemberships';
GO

