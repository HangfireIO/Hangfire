-- This file is part of Hangfire.
-- Copyright © 2013-2014 Sergey Odinokov.
-- 
-- Hangfire is free software: you can redistribute it and/or modify
-- it under the terms of the GNU Lesser General Public License as 
-- published by the Free Software Foundation, either version 3 
-- of the License, or any later version.
-- 
-- Hangfire is distributed in the hope that it will be useful,
-- but WITHOUT ANY WARRANTY; without even the implied warranty of
-- MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
-- GNU Lesser General Public License for more details.
-- 
-- You should have received a copy of the GNU Lesser General Public 
-- License along with Hangfire. If not, see <http://www.gnu.org/licenses/>.

SET NOCOUNT ON
DECLARE @TARGET_SCHEMA_VERSION INT;
SET @TARGET_SCHEMA_VERSION = 5;

PRINT 'Installing Hangfire SQL objects...';

BEGIN TRANSACTION;

-- Create the database schema if it doesn't exists
IF NOT EXISTS (SELECT [schema_id] FROM [sys].[schemas] WHERE [name] = '$(HangFireSchema)')
BEGIN
    EXEC (N'CREATE SCHEMA [$(HangFireSchema)]');
    PRINT 'Created database schema [$(HangFireSchema)]';
END
ELSE
    PRINT 'Database schema [$(HangFireSchema)] already exists';
    
DECLARE @SCHEMA_ID int;
SELECT @SCHEMA_ID = [schema_id] FROM [sys].[schemas] WHERE [name] = '$(HangFireSchema)';

-- Create the [$(HangFireSchema)].Schema table if not exists
IF NOT EXISTS(SELECT [object_id] FROM [sys].[tables] 
    WHERE [name] = 'Schema' AND [schema_id] = @SCHEMA_ID)
BEGIN
    CREATE TABLE [$(HangFireSchema)].[Schema](
        [Version] [int] NOT NULL,
        CONSTRAINT [PK_HangFire_Schema] PRIMARY KEY CLUSTERED ([Version] ASC)
    );
    PRINT 'Created table [$(HangFireSchema)].[Schema]';
END
ELSE
    PRINT 'Table [$(HangFireSchema)].[Schema] already exists';
    
DECLARE @CURRENT_SCHEMA_VERSION int;
SELECT @CURRENT_SCHEMA_VERSION = [Version] FROM [$(HangFireSchema)].[Schema];

PRINT 'Current Hangfire schema version: ' + CASE @CURRENT_SCHEMA_VERSION WHEN NULL THEN 'none' ELSE CONVERT(nvarchar, @CURRENT_SCHEMA_VERSION) END;

IF @CURRENT_SCHEMA_VERSION IS NOT NULL AND @CURRENT_SCHEMA_VERSION > @TARGET_SCHEMA_VERSION
BEGIN
    ROLLBACK TRANSACTION;
    RAISERROR(N'HangFire current database schema version %d is newer than the configured SqlServerStorage schema version %d. Please update to the latest HangFire.SqlServer NuGet package.', 11, 1,
        @CURRENT_SCHEMA_VERSION, @TARGET_SCHEMA_VERSION);
END

-- Install [$(HangFireSchema)] schema objects
IF @CURRENT_SCHEMA_VERSION IS NULL
BEGIN
    PRINT 'Installing schema version 1';
        
    -- Create job tables
    CREATE TABLE [$(HangFireSchema)].[Job] (
        [Id] [int] IDENTITY(1,1) NOT NULL,
		[StateId] [int] NULL,
		[StateName] [nvarchar](20) NULL, -- To speed-up queries.
        [InvocationData] [nvarchar](max) NOT NULL,
        [Arguments] [nvarchar](max) NOT NULL,
        [CreatedAt] [datetime] NOT NULL,
        [ExpireAt] [datetime] NULL,

        CONSTRAINT [PK_HangFire_Job] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
    PRINT 'Created table [$(HangFireSchema)].[Job]';

	CREATE NONCLUSTERED INDEX [IX_HangFire_Job_StateName] ON [$(HangFireSchema)].[Job] ([StateName] ASC);
	PRINT 'Created index [IX_HangFire_Job_StateName]';
        
    -- Job history table
        
    CREATE TABLE [$(HangFireSchema)].[State] (
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [JobId] [int] NOT NULL,
		[Name] [nvarchar](20) NOT NULL,
		[Reason] [nvarchar](100) NULL,
        [CreatedAt] [datetime] NOT NULL,
        [Data] [nvarchar](max) NULL,
            
        CONSTRAINT [PK_HangFire_State] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
    PRINT 'Created table [$(HangFireSchema)].[State]';

    ALTER TABLE [$(HangFireSchema)].[State] ADD CONSTRAINT [FK_HangFire_State_Job] FOREIGN KEY([JobId])
        REFERENCES [$(HangFireSchema)].[Job] ([Id])
        ON UPDATE CASCADE
        ON DELETE CASCADE;
    PRINT 'Created constraint [FK_HangFire_State_Job]';
        
    CREATE NONCLUSTERED INDEX [IX_HangFire_State_JobId] ON [$(HangFireSchema)].[State] ([JobId] ASC);
    PRINT 'Created index [IX_HangFire_State_JobId]';
        
    -- Job parameters table
        
    CREATE TABLE [$(HangFireSchema)].[JobParameter](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [JobId] [int] NOT NULL,
        [Name] [nvarchar](40) NOT NULL,
        [Value] [nvarchar](max) NULL,
            
        CONSTRAINT [PK_HangFire_JobParameter] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
    PRINT 'Created table [$(HangFireSchema)].[JobParameter]';

    ALTER TABLE [$(HangFireSchema)].[JobParameter] ADD CONSTRAINT [FK_HangFire_JobParameter_Job] FOREIGN KEY([JobId])
        REFERENCES [$(HangFireSchema)].[Job] ([Id])
        ON UPDATE CASCADE
        ON DELETE CASCADE;
    PRINT 'Created constraint [FK_HangFire_JobParameter_Job]';
        
    CREATE NONCLUSTERED INDEX [IX_HangFire_JobParameter_JobIdAndName] ON [$(HangFireSchema)].[JobParameter] (
        [JobId] ASC,
        [Name] ASC
    );
    PRINT 'Created index [IX_HangFire_JobParameter_JobIdAndName]';
        
    -- Job queue table
        
    CREATE TABLE [$(HangFireSchema)].[JobQueue](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [JobId] [int] NOT NULL,
        [Queue] [nvarchar](20) NOT NULL,
        [FetchedAt] [datetime] NULL,
            
        CONSTRAINT [PK_HangFire_JobQueue] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
    PRINT 'Created table [$(HangFireSchema)].[JobQueue]';
        
    CREATE NONCLUSTERED INDEX [IX_HangFire_JobQueue_JobIdAndQueue] ON [$(HangFireSchema)].[JobQueue] (
        [JobId] ASC,
        [Queue] ASC
    );
    PRINT 'Created index [IX_HangFire_JobQueue_JobIdAndQueue]';
        
    CREATE NONCLUSTERED INDEX [IX_HangFire_JobQueue_QueueAndFetchedAt] ON [$(HangFireSchema)].[JobQueue] (
        [Queue] ASC,
        [FetchedAt] ASC
    );
    PRINT 'Created index [IX_HangFire_JobQueue_QueueAndFetchedAt]';
        
    -- Servers table
        
    CREATE TABLE [$(HangFireSchema)].[Server](
        [Id] [nvarchar](50) NOT NULL,
        [Data] [nvarchar](max) NULL,
        [LastHeartbeat] [datetime] NULL,
            
        CONSTRAINT [PK_HangFire_Server] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
    PRINT 'Created table [$(HangFireSchema)].[Server]';
        
    -- Extension tables
        
    CREATE TABLE [$(HangFireSchema)].[Hash](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [Key] [nvarchar](100) NOT NULL,
        [Name] [nvarchar](40) NOT NULL,
        [StringValue] [nvarchar](max) NULL,
        [IntValue] [int] NULL,
        [ExpireAt] [datetime] NULL,
            
        CONSTRAINT [PK_HangFire_Hash] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
    PRINT 'Created table [$(HangFireSchema)].[Hash]';
        
    CREATE UNIQUE NONCLUSTERED INDEX [UX_HangFire_Hash_KeyAndName] ON [$(HangFireSchema)].[Hash] (
        [Key] ASC,
        [Name] ASC
    );
    PRINT 'Created index [UX_HangFire_Hash_KeyAndName]';
        
    CREATE TABLE [$(HangFireSchema)].[List](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [Key] [nvarchar](100) NOT NULL,
        [Value] [nvarchar](max) NULL,
        [ExpireAt] [datetime] NULL,
            
        CONSTRAINT [PK_HangFire_List] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
    PRINT 'Created table [$(HangFireSchema)].[List]';
        
    CREATE TABLE [$(HangFireSchema)].[Set](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [Key] [nvarchar](100) NOT NULL,
        [Score] [float] NOT NULL,
        [Value] [nvarchar](256) NOT NULL,
        [ExpireAt] [datetime] NULL,
            
        CONSTRAINT [PK_HangFire_Set] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
    PRINT 'Created table [$(HangFireSchema)].[Set]';
        
    CREATE UNIQUE NONCLUSTERED INDEX [UX_HangFire_Set_KeyAndValue] ON [$(HangFireSchema)].[Set] (
        [Key] ASC,
        [Value] ASC
    );
    PRINT 'Created index [UX_HangFire_Set_KeyAndValue]';
        
    CREATE TABLE [$(HangFireSchema)].[Value](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [Key] [nvarchar](100) NOT NULL,
        [StringValue] [nvarchar](max) NULL,
        [IntValue] [int] NULL,
        [ExpireAt] [datetime] NULL,
            
        CONSTRAINT [PK_HangFire_Value] PRIMARY KEY CLUSTERED (
            [Id] ASC
        )
    );
    PRINT 'Created table [$(HangFireSchema)].[Value]';
        
    CREATE UNIQUE NONCLUSTERED INDEX [UX_HangFire_Value_Key] ON [$(HangFireSchema)].[Value] (
        [Key] ASC
    );
    PRINT 'Created index [UX_HangFire_Value_Key]';

	CREATE TABLE [$(HangFireSchema)].[Counter](
		[Id] [int] IDENTITY(1,1) NOT NULL,
		[Key] [nvarchar](100) NOT NULL,
		[Value] [tinyint] NOT NULL,
		[ExpireAt] [datetime] NULL,

		CONSTRAINT [PK_HangFire_Counter] PRIMARY KEY CLUSTERED ([Id] ASC)
	);
	PRINT 'Created table [$(HangFireSchema)].[Counter]';

	CREATE NONCLUSTERED INDEX [IX_HangFire_Counter_Key] ON [$(HangFireSchema)].[Counter] ([Key] ASC)
	INCLUDE ([Value]);
	PRINT 'Created index [IX_HangFire_Counter_Key]';

	SET @CURRENT_SCHEMA_VERSION = 1;
END

IF @CURRENT_SCHEMA_VERSION = 1
BEGIN
	PRINT 'Installing schema version 2';

	-- https://github.com/odinserj/HangFire/issues/83

	DROP INDEX [IX_HangFire_Counter_Key] ON [$(HangFireSchema)].[Counter];

	ALTER TABLE [$(HangFireSchema)].[Counter] ALTER COLUMN [Value] SMALLINT NOT NULL;

	CREATE NONCLUSTERED INDEX [IX_HangFire_Counter_Key] ON [$(HangFireSchema)].[Counter] ([Key] ASC)
	INCLUDE ([Value]);
	PRINT 'Index [IX_HangFire_Counter_Key] re-created';

	DROP TABLE [$(HangFireSchema)].[Value];
	DROP TABLE [$(HangFireSchema)].[Hash];
	PRINT 'Dropped tables [$(HangFireSchema)].[Value] and [$(HangFireSchema)].[Hash]'

	DELETE FROM [$(HangFireSchema)].[Server] WHERE [LastHeartbeat] IS NULL;
	ALTER TABLE [$(HangFireSchema)].[Server] ALTER COLUMN [LastHeartbeat] DATETIME NOT NULL;

	SET @CURRENT_SCHEMA_VERSION = 2;
END

IF @CURRENT_SCHEMA_VERSION = 2
BEGIN
	PRINT 'Installing schema version 3';

	DROP INDEX [IX_HangFire_JobQueue_JobIdAndQueue] ON [$(HangFireSchema)].[JobQueue];
	PRINT 'Dropped index [IX_HangFire_JobQueue_JobIdAndQueue]';

	CREATE TABLE [$(HangFireSchema)].[Hash](
		[Id] [int] IDENTITY(1,1) NOT NULL,
		[Key] [nvarchar](100) NOT NULL,
		[Field] [nvarchar](100) NOT NULL,
		[Value] [nvarchar](max) NULL,
		[ExpireAt] [datetime] NULL,
		
		CONSTRAINT [PK_HangFire_Hash] PRIMARY KEY CLUSTERED ([Id] ASC)
	);
	PRINT 'Created table [$(HangFireSchema)].[Hash]';

	CREATE UNIQUE NONCLUSTERED INDEX [UX_HangFire_Hash_Key_Field] ON [$(HangFireSchema)].[Hash] (
		[Key] ASC,
		[Field] ASC
	);
	PRINT 'Created index [UX_HangFire_Hash_Key_Field]';

	SET @CURRENT_SCHEMA_VERSION = 3;
END

IF @CURRENT_SCHEMA_VERSION = 3
BEGIN
	PRINT 'Installing schema version 4';

	CREATE TABLE [$(HangFireSchema)].[AggregatedCounter] (
		[Id] [int] IDENTITY(1,1) NOT NULL,
		[Key] [nvarchar](100) NOT NULL,
		[Value] [bigint] NOT NULL,
		[ExpireAt] [datetime] NULL,

		CONSTRAINT [PK_HangFire_CounterAggregated] PRIMARY KEY CLUSTERED ([Id] ASC)
	);
	PRINT 'Created table [$(HangFireSchema)].[AggregatedCounter]';

	CREATE UNIQUE NONCLUSTERED INDEX [UX_HangFire_CounterAggregated_Key] ON [$(HangFireSchema)].[AggregatedCounter] (
		[Key] ASC
	) INCLUDE ([Value]);
	PRINT 'Created index [UX_HangFire_CounterAggregated_Key]';

	CREATE NONCLUSTERED INDEX [IX_HangFire_Hash_ExpireAt] ON [$(HangFireSchema)].[Hash] ([ExpireAt])
	INCLUDE ([Id]);

	CREATE NONCLUSTERED INDEX [IX_HangFire_Job_ExpireAt] ON [$(HangFireSchema)].[Job] ([ExpireAt])
	INCLUDE ([Id]);

	CREATE NONCLUSTERED INDEX [IX_HangFire_List_ExpireAt] ON [$(HangFireSchema)].[List] ([ExpireAt])
	INCLUDE ([Id]);

	CREATE NONCLUSTERED INDEX [IX_HangFire_Set_ExpireAt] ON [$(HangFireSchema)].[Set] ([ExpireAt])
	INCLUDE ([Id]);

	PRINT 'Created indexes for [ExpireAt] columns';

	CREATE NONCLUSTERED INDEX [IX_HangFire_Hash_Key] ON [$(HangFireSchema)].[Hash] ([Key] ASC)
	INCLUDE ([ExpireAt]);
	PRINT 'Created index [IX_HangFire_Hash_Key]';

	CREATE NONCLUSTERED INDEX [IX_HangFire_List_Key] ON [$(HangFireSchema)].[List] ([Key] ASC)
	INCLUDE ([ExpireAt], [Value]);
	PRINT 'Created index [IX_HangFire_List_Key]';

	CREATE NONCLUSTERED INDEX [IX_HangFire_Set_Key] ON [$(HangFireSchema)].[Set] ([Key] ASC)
	INCLUDE ([ExpireAt], [Value]);
	PRINT 'Created index [IX_HangFire_Set_Key]';

	SET @CURRENT_SCHEMA_VERSION = 4;
END

IF @CURRENT_SCHEMA_VERSION = 4
BEGIN
	PRINT 'Installing schema version 5';

	DROP INDEX [IX_HangFire_JobQueue_QueueAndFetchedAt] ON [$(HangFireSchema)].[JobQueue];
	PRINT 'Dropped index [IX_HangFire_JobQueue_QueueAndFetchedAt] to modify the [$(HangFireSchema)].[JobQueue].[Queue] column';

	ALTER TABLE [$(HangFireSchema)].[JobQueue] ALTER COLUMN [Queue] NVARCHAR (50) NOT NULL;
	PRINT 'Modified [$(HangFireSchema)].[JobQueue].[Queue] length to 50';

	CREATE NONCLUSTERED INDEX [IX_HangFire_JobQueue_QueueAndFetchedAt] ON [$(HangFireSchema)].[JobQueue] (
        [Queue] ASC,
        [FetchedAt] ASC
    );
    PRINT 'Re-created index [IX_HangFire_JobQueue_QueueAndFetchedAt]';
		
	ALTER TABLE [$(HangFireSchema)].[Server] ALTER COLUMN [Id] NVARCHAR (100) NOT NULL;
	PRINT 'Modified [$(HangFireSchema)].[Server].[Id] length to 100';
		
	SET @CURRENT_SCHEMA_VERSION = 5;
END
	
/*IF @CURRENT_SCHEMA_VERSION = 5
BEGIN
	PRINT 'Installing schema version 6';

	-- Insert migration here

	SET @CURRENT_SCHEMA_VERSION = 6;
END*/

UPDATE [$(HangFireSchema)].[Schema] SET [Version] = @CURRENT_SCHEMA_VERSION
IF @@ROWCOUNT = 0 
	INSERT INTO [$(HangFireSchema)].[Schema] ([Version]) VALUES (@CURRENT_SCHEMA_VERSION)        

PRINT 'Hangfire database schema installed';

COMMIT TRANSACTION;
PRINT 'Hangfire SQL objects installed';
