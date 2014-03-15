DECLARE @TARGET_SCHEMA_VERSION INT;
SET @TARGET_SCHEMA_VERSION = 2;

PRINT 'Installing HangFire SQL objects...';

SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;
BEGIN TRANSACTION;

-- Create the database schema if it doesn't exists
IF NOT EXISTS (SELECT [schema_id] FROM [sys].[schemas] WHERE [name] = 'HangFire')
BEGIN
    EXEC (N'CREATE SCHEMA [HangFire]');
    PRINT 'Created database schema [HangFire]';
END
ELSE
    PRINT 'Database schema [HangFire] already exists';
    
DECLARE @SCHEMA_ID int;
SELECT @SCHEMA_ID = [schema_id] FROM [sys].[schemas] WHERE [name] = 'HangFire';

-- Create the HangFire.Schema table if not exists
IF NOT EXISTS(SELECT [object_id] FROM [sys].[tables] 
    WHERE [name] = 'Schema' AND [schema_id] = @SCHEMA_ID)
BEGIN
    CREATE TABLE [HangFire].[Schema](
        [Version] [int] NOT NULL,
        CONSTRAINT [PK_HangFire_Schema] PRIMARY KEY CLUSTERED ([Version] ASC)
    );
    PRINT 'Created table [HangFire].[Schema]';
END
ELSE
    PRINT 'Table [HangFire].[Schema] already exists';
    
DECLARE @CURRENT_SCHEMA_VERSION int;
SELECT @CURRENT_SCHEMA_VERSION = [Version] FROM [HangFire].[Schema];

PRINT 'Current HangFire schema version: ' + CASE @CURRENT_SCHEMA_VERSION WHEN NULL THEN 'none' ELSE CONVERT(nvarchar, @CURRENT_SCHEMA_VERSION) END;

IF @CURRENT_SCHEMA_VERSION IS NOT NULL AND @CURRENT_SCHEMA_VERSION > @TARGET_SCHEMA_VERSION
BEGIN
    ROLLBACK TRANSACTION;
    RAISERROR(N'HangFire current database schema version %d is newer than the configured SqlServerStorage schema version %d. Please update to the latest HangFire.SqlServer NuGet package.', 11, 1,
        @CURRENT_SCHEMA_VERSION, @TARGET_SCHEMA_VERSION);
END
ELSE
BEGIN
    -- Install HangFire schema objects
    IF @CURRENT_SCHEMA_VERSION IS NULL
    BEGIN
        PRINT 'Installing schema version 1';
        
        -- Create job tables
        CREATE TABLE [HangFire].[Job](
            [Id] [int] IDENTITY(1,1) NOT NULL,
            [State] [nvarchar](20) NOT NULL,
            [InvocationData] [nvarchar](max) NOT NULL,
            [Arguments] [nvarchar](max) NOT NULL,
            [StateData] [nvarchar](max) NULL,
            [CreatedAt] [datetime] NOT NULL,
            [ExpireAt] [datetime] NULL,

            CONSTRAINT [PK_HangFire_Job] PRIMARY KEY CLUSTERED ([Id] ASC)
        );
        PRINT 'Created table [HangFire].[Job]';
        
        -- Job history table
        
        CREATE TABLE [HangFire].[JobHistory](
            [Id] [int] IDENTITY(1,1) NOT NULL,
            [JobId] [int] NOT NULL,
			[StateName] nvarchar(20) NOT NULL,
			[Reason] nvarchar(100) NULL,
            [CreatedAt] datetime2(7) NOT NULL,
            [Data] [nvarchar](max) NULL,
            
            CONSTRAINT [PK_HangFire_JobHistory] PRIMARY KEY CLUSTERED ([Id] ASC)
        );
        PRINT 'Created table [HangFire].[JobHistory]';

        ALTER TABLE [HangFire].[JobHistory] ADD CONSTRAINT [FK_HangFire_JobHistory_Job] FOREIGN KEY([JobId])
            REFERENCES [HangFire].[Job] ([Id])
            ON UPDATE CASCADE
            ON DELETE CASCADE;
        PRINT 'Created constraint [FK_HangFire_JobHistory_Job]';
        
        CREATE NONCLUSTERED INDEX [IX_HangFire_JobHistory_JobId] ON [HangFire].[JobHistory] ([JobId] ASC);
        PRINT 'Created index [IX_HangFire_JobHistory_JobId]';
        
        -- Job parameters table
        
        CREATE TABLE [HangFire].[JobParameter](
            [Id] [int] IDENTITY(1,1) NOT NULL,
            [JobId] [int] NOT NULL,
            [Name] [nvarchar](100) NOT NULL,
            [Value] [nvarchar](max) NULL,
            
            CONSTRAINT [PK_HangFire_JobParameter] PRIMARY KEY CLUSTERED ([Id] ASC)
        );
        PRINT 'Created table [HangFire].[JobParameter]';

        ALTER TABLE [HangFire].[JobParameter] ADD CONSTRAINT [FK_HangFire_JobParameter_Job] FOREIGN KEY([JobId])
            REFERENCES [HangFire].[Job] ([Id])
            ON UPDATE CASCADE
            ON DELETE CASCADE;
        PRINT 'Created constraint [FK_HangFire_JobParameter_Job]';
        
        CREATE NONCLUSTERED INDEX [IX_HangFire_JobParameter_JobIdAndName] ON [HangFire].[JobParameter] (
            [JobId] ASC,
            [Name] ASC
        );
        PRINT 'Created index [IX_HangFire_JobParameter_JobIdAndName]';
        
        -- Job queue table
        
        CREATE TABLE [HangFire].[JobQueue](
            [Id] [int] IDENTITY(1,1) NOT NULL,
            [JobId] [int] NOT NULL,
            [Queue] [nvarchar](100) NOT NULL,
            [FetchedAt] [datetime] NULL,
            
            CONSTRAINT [PK_HangFire_JobQueue] PRIMARY KEY CLUSTERED ([Id] ASC)
        );
        PRINT 'Created table [HangFire].[JobQueue]';
        
        CREATE NONCLUSTERED INDEX [IX_HangFire_JobQueue_JobIdAndQueue] ON [HangFire].[JobQueue] (
            [JobId] ASC,
            [Queue] ASC
        );
        PRINT 'Created index [IX_HangFire_JobQueue_JobIdAndQueue]';
        
        CREATE NONCLUSTERED INDEX [IX_HangFire_JobQueue_QueueAndFetchedAt] ON [HangFire].[JobQueue] (
            [Queue] ASC,
            [FetchedAt] ASC
        );
        PRINT 'Created index [IX_HangFire_JobQueue_QueueAndFetchedAt]';
        
        -- Servers table
        
        CREATE TABLE [HangFire].[Server](
            [Id] [nvarchar](50) NOT NULL,
            [Data] [nvarchar](max) NULL,
            [LastHeartbeat] [datetime] NULL,
            
            CONSTRAINT [PK_HangFire_Server] PRIMARY KEY CLUSTERED ([Id] ASC)
        );
        PRINT 'Created table [HangFire].[Server]';
        
        -- Extension tables
        
        CREATE TABLE [HangFire].[Hash](
            [Id] [int] IDENTITY(1,1) NOT NULL,
            [Key] [nvarchar](100) NOT NULL,
            [Name] [nvarchar](100) NOT NULL,
            [StringValue] [nvarchar](max) NULL,
            [IntValue] [int] NULL,
            [ExpireAt] [datetime] NULL,
            
            CONSTRAINT [PK_HangFire_Hash] PRIMARY KEY CLUSTERED ([Id] ASC)
        );
        PRINT 'Created table [HangFire].[Hash]';
        
        CREATE UNIQUE NONCLUSTERED INDEX [UX_HangFire_Hash_KeyAndName] ON [HangFire].[Hash] (
            [Key] ASC,
            [Name] ASC
        );
        PRINT 'Created index [UX_HangFire_Hash_KeyAndName]';
        
        CREATE TABLE [HangFire].[List](
            [Id] [int] IDENTITY(1,1) NOT NULL,
            [Key] [nvarchar](100) NOT NULL,
            [Value] [nvarchar](max) NULL,
            [ExpireAt] [datetime] NULL,
            
            CONSTRAINT [PK_HangFire_List] PRIMARY KEY CLUSTERED ([Id] ASC)
        );
        PRINT 'Created table [HangFire].[List]';
        
        CREATE TABLE [HangFire].[Set](
            [Id] [int] IDENTITY(1,1) NOT NULL,
            [Key] [nvarchar](100) NOT NULL,
            [Score] [float] NOT NULL,
            [Value] [nvarchar](256) NOT NULL,
            [ExpireAt] [datetime] NULL,
            
            CONSTRAINT [PK_HangFire_Set] PRIMARY KEY CLUSTERED ([Id] ASC)
        );
        PRINT 'Created table [HangFire].[Set]';
        
        CREATE UNIQUE NONCLUSTERED INDEX [UX_HangFire_Set_KeyAndValue] ON [HangFire].[Set] (
            [Key] ASC,
            [Value] ASC
        );
        PRINT 'Created index [UX_HangFire_Set_KeyAndValue]';
        
        CREATE TABLE [HangFire].[Value](
            [Id] [int] IDENTITY(1,1) NOT NULL,
            [Key] [nvarchar](100) NOT NULL,
            [StringValue] [nvarchar](max) NULL,
            [IntValue] [int] NULL,
            [ExpireAt] [datetime] NULL,
            
            CONSTRAINT [PK_HangFire_Value] PRIMARY KEY CLUSTERED (
                [Id] ASC
            )
        );
        PRINT 'Created table [HangFire].[Value]';
        
        CREATE UNIQUE NONCLUSTERED INDEX [UX_HangFire_Value_Key] ON [HangFire].[Value] (
            [Key] ASC
        );
        PRINT 'Created index [UX_HangFire_Value_Key]';

		SET @CURRENT_SCHEMA_VERSION = 1;
    END

	IF @CURRENT_SCHEMA_VERSION = 1
	BEGIN
		PRINT 'Installing schema version 2';

		CREATE TABLE [HangFire].[Counter](
			[Id] [int] IDENTITY(1,1) NOT NULL,
			[Key] [nvarchar](100) NOT NULL,
			[Value] [tinyint] NOT NULL,
			[ExpireAt] [datetime] NULL,

			CONSTRAINT [PK_HangFire_Counter] PRIMARY KEY CLUSTERED ([Id] ASC)
		);
		PRINT 'Created table [HangFire].[Counter]';

		CREATE NONCLUSTERED INDEX [IX_HangFire_Counter_Key] ON [HangFire].[Counter] ([Key] ASC)
		INCLUDE ([Value]);
		PRINT 'Created index [IX_HangFire_Counter_Key]';

		SET @CURRENT_SCHEMA_VERSION = 2;
	END

	UPDATE [HangFire].[Schema] SET [Version] = @CURRENT_SCHEMA_VERSION
	IF @@ROWCOUNT = 0 
		INSERT INTO [HangFire].[Schema] ([Version]) VALUES (@CURRENT_SCHEMA_VERSION)        

    PRINT CHAR(13) + 'HangFire database schema installed';

    COMMIT TRANSACTION;
    PRINT 'HangFire SQL objects installed';
END
