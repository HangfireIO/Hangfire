
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
SET XACT_ABORT ON
DECLARE @TARGET_SCHEMA_VERSION INT;
DECLARE @DISABLE_HEAVY_MIGRATIONS BIT;
SET @TARGET_SCHEMA_VERSION = 7;
--SET @DISABLE_HEAVY_MIGRATIONS = 1;

PRINT 'Installing Hangfire SQL objects...';

BEGIN TRANSACTION;

-- Acquire exclusive lock to prevent deadlocks caused by schema creation / version update
DECLARE @SchemaLockResult INT;
EXEC @SchemaLockResult = sp_getapplock @Resource = 'HangFire:SchemaLock', @LockMode = 'Exclusive'

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

-- Create the [HangFire].Schema table if not exists
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

PRINT 'Current Hangfire schema version: ' + CASE WHEN @CURRENT_SCHEMA_VERSION IS NULL THEN 'none' ELSE CONVERT(nvarchar, @CURRENT_SCHEMA_VERSION) END;

IF @CURRENT_SCHEMA_VERSION IS NOT NULL AND @CURRENT_SCHEMA_VERSION > @TARGET_SCHEMA_VERSION
BEGIN
    ROLLBACK TRANSACTION;
    PRINT 'Hangfire current database schema version ' + CAST(@CURRENT_SCHEMA_VERSION AS NVARCHAR) +
          ' is newer than the configured SqlServerStorage schema version ' + CAST(@TARGET_SCHEMA_VERSION AS NVARCHAR) +
          '. Will not apply any migrations.';
    RETURN;
END

-- Install [HangFire] schema objects
IF @CURRENT_SCHEMA_VERSION IS NULL
BEGIN
    IF @DISABLE_HEAVY_MIGRATIONS = 1
    BEGIN
        SET @DISABLE_HEAVY_MIGRATIONS = 0;
        PRINT 'Enabling HEAVY_MIGRATIONS, because we are installing objects from scratch';
    END

    PRINT 'Installing schema version 1';
        
    -- Create job tables
    CREATE TABLE [HangFire].[Job] (
        [Id] [int] IDENTITY(1,1) NOT NULL,
		[StateId] [int] NULL,
		[StateName] [nvarchar](20) NULL, -- To speed-up queries.
        [InvocationData] [nvarchar](max) NOT NULL,
        [Arguments] [nvarchar](max) NOT NULL,
        [CreatedAt] [datetime] NOT NULL,
        [ExpireAt] [datetime] NULL,

        CONSTRAINT [PK_HangFire_Job] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
    PRINT 'Created table [HangFire].[Job]';

	CREATE NONCLUSTERED INDEX [IX_HangFire_Job_StateName] ON [HangFire].[Job] ([StateName] ASC);
	PRINT 'Created index [IX_HangFire_Job_StateName]';
        
    -- Job history table
        
    CREATE TABLE [HangFire].[State] (
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [JobId] [int] NOT NULL,
		[Name] [nvarchar](20) NOT NULL,
		[Reason] [nvarchar](100) NULL,
        [CreatedAt] [datetime] NOT NULL,
        [Data] [nvarchar](max) NULL,
            
        CONSTRAINT [PK_HangFire_State] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
    PRINT 'Created table [HangFire].[State]';

    ALTER TABLE [HangFire].[State] ADD CONSTRAINT [FK_HangFire_State_Job] FOREIGN KEY([JobId])
        REFERENCES [HangFire].[Job] ([Id])
        ON UPDATE CASCADE
        ON DELETE CASCADE;
    PRINT 'Created constraint [FK_HangFire_State_Job]';
        
    CREATE NONCLUSTERED INDEX [IX_HangFire_State_JobId] ON [HangFire].[State] ([JobId] ASC);
    PRINT 'Created index [IX_HangFire_State_JobId]';
        
    -- Job parameters table
        
    CREATE TABLE [HangFire].[JobParameter](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [JobId] [int] NOT NULL,
        [Name] [nvarchar](40) NOT NULL,
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
        [Queue] [nvarchar](20) NOT NULL,
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
        [Id] [nvarchar](200) NOT NULL,
        [Data] [nvarchar](max) NULL,
        [LastHeartbeat] [datetime] NULL,
            
        CONSTRAINT [PK_HangFire_Server] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
    PRINT 'Created table [HangFire].[Server]';
        
    -- Extension tables
        
    CREATE TABLE [HangFire].[Hash](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [Key] [nvarchar](100) NOT NULL,
        [Name] [nvarchar](40) NOT NULL,
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

	SET @CURRENT_SCHEMA_VERSION = 1;
END

IF @CURRENT_SCHEMA_VERSION = 1
BEGIN
	PRINT 'Installing schema version 2';

	-- https://github.com/odinserj/HangFire/issues/83

	DROP INDEX [IX_HangFire_Counter_Key] ON [HangFire].[Counter];

	ALTER TABLE [HangFire].[Counter] ALTER COLUMN [Value] SMALLINT NOT NULL;

	CREATE NONCLUSTERED INDEX [IX_HangFire_Counter_Key] ON [HangFire].[Counter] ([Key] ASC)
	INCLUDE ([Value]);
	PRINT 'Index [IX_HangFire_Counter_Key] re-created';

	DROP TABLE [HangFire].[Value];
	DROP TABLE [HangFire].[Hash];
	PRINT 'Dropped tables [HangFire].[Value] and [HangFire].[Hash]'

	DELETE FROM [HangFire].[Server] WHERE [LastHeartbeat] IS NULL;
	ALTER TABLE [HangFire].[Server] ALTER COLUMN [LastHeartbeat] DATETIME NOT NULL;

	SET @CURRENT_SCHEMA_VERSION = 2;
END

IF @CURRENT_SCHEMA_VERSION = 2
BEGIN
	PRINT 'Installing schema version 3';

	DROP INDEX [IX_HangFire_JobQueue_JobIdAndQueue] ON [HangFire].[JobQueue];
	PRINT 'Dropped index [IX_HangFire_JobQueue_JobIdAndQueue]';

	CREATE TABLE [HangFire].[Hash](
		[Id] [int] IDENTITY(1,1) NOT NULL,
		[Key] [nvarchar](100) NOT NULL,
		[Field] [nvarchar](100) NOT NULL,
		[Value] [nvarchar](max) NULL,
		[ExpireAt] [datetime2](7) NULL,
		
		CONSTRAINT [PK_HangFire_Hash] PRIMARY KEY CLUSTERED ([Id] ASC)
	);
	PRINT 'Created table [HangFire].[Hash]';

	CREATE UNIQUE NONCLUSTERED INDEX [UX_HangFire_Hash_Key_Field] ON [HangFire].[Hash] (
		[Key] ASC,
		[Field] ASC
	);
	PRINT 'Created index [UX_HangFire_Hash_Key_Field]';

	SET @CURRENT_SCHEMA_VERSION = 3;
END

IF @CURRENT_SCHEMA_VERSION = 3
BEGIN
	PRINT 'Installing schema version 4';

	CREATE TABLE [HangFire].[AggregatedCounter] (
		[Id] [int] IDENTITY(1,1) NOT NULL,
		[Key] [nvarchar](100) NOT NULL,
		[Value] [bigint] NOT NULL,
		[ExpireAt] [datetime] NULL,

		CONSTRAINT [PK_HangFire_CounterAggregated] PRIMARY KEY CLUSTERED ([Id] ASC)
	);
	PRINT 'Created table [HangFire].[AggregatedCounter]';

	CREATE UNIQUE NONCLUSTERED INDEX [UX_HangFire_CounterAggregated_Key] ON [HangFire].[AggregatedCounter] (
		[Key] ASC
	) INCLUDE ([Value]);
	PRINT 'Created index [UX_HangFire_CounterAggregated_Key]';

	CREATE NONCLUSTERED INDEX [IX_HangFire_Hash_ExpireAt] ON [HangFire].[Hash] ([ExpireAt])
	INCLUDE ([Id]);

	CREATE NONCLUSTERED INDEX [IX_HangFire_Job_ExpireAt] ON [HangFire].[Job] ([ExpireAt])
	INCLUDE ([Id]);

	CREATE NONCLUSTERED INDEX [IX_HangFire_List_ExpireAt] ON [HangFire].[List] ([ExpireAt])
	INCLUDE ([Id]);

	CREATE NONCLUSTERED INDEX [IX_HangFire_Set_ExpireAt] ON [HangFire].[Set] ([ExpireAt])
	INCLUDE ([Id]);

	PRINT 'Created indexes for [ExpireAt] columns';

	CREATE NONCLUSTERED INDEX [IX_HangFire_Hash_Key] ON [HangFire].[Hash] ([Key] ASC)
	INCLUDE ([ExpireAt]);
	PRINT 'Created index [IX_HangFire_Hash_Key]';

	CREATE NONCLUSTERED INDEX [IX_HangFire_List_Key] ON [HangFire].[List] ([Key] ASC)
	INCLUDE ([ExpireAt], [Value]);
	PRINT 'Created index [IX_HangFire_List_Key]';

	CREATE NONCLUSTERED INDEX [IX_HangFire_Set_Key] ON [HangFire].[Set] ([Key] ASC)
	INCLUDE ([ExpireAt], [Value]);
	PRINT 'Created index [IX_HangFire_Set_Key]';

	SET @CURRENT_SCHEMA_VERSION = 4;
END

IF @CURRENT_SCHEMA_VERSION = 4
BEGIN
	PRINT 'Installing schema version 5';

	DROP INDEX [IX_HangFire_JobQueue_QueueAndFetchedAt] ON [HangFire].[JobQueue];
	PRINT 'Dropped index [IX_HangFire_JobQueue_QueueAndFetchedAt] to modify the [HangFire].[JobQueue].[Queue] column';

	ALTER TABLE [HangFire].[JobQueue] ALTER COLUMN [Queue] NVARCHAR (50) NOT NULL;
	PRINT 'Modified [HangFire].[JobQueue].[Queue] length to 50';

	CREATE NONCLUSTERED INDEX [IX_HangFire_JobQueue_QueueAndFetchedAt] ON [HangFire].[JobQueue] (
        [Queue] ASC,
        [FetchedAt] ASC
    );
    PRINT 'Re-created index [IX_HangFire_JobQueue_QueueAndFetchedAt]';

	ALTER TABLE [HangFire].[Server] DROP CONSTRAINT [PK_HangFire_Server]
    PRINT 'Dropped constraint [PK_HangFire_Server] to modify the [HangFire].[Server].[Id] column';

	ALTER TABLE [HangFire].[Server] ALTER COLUMN [Id] NVARCHAR (200) NOT NULL;
	PRINT 'Modified [HangFire].[Server].[Id] length to 200';

	ALTER TABLE [HangFire].[Server] ADD  CONSTRAINT [PK_HangFire_Server] PRIMARY KEY CLUSTERED
	(
		[Id] ASC
	);
	PRINT 'Re-created constraint [PK_HangFire_Server]';

	SET @CURRENT_SCHEMA_VERSION = 5;
END

IF @CURRENT_SCHEMA_VERSION = 5 AND @DISABLE_HEAVY_MIGRATIONS = 1
BEGIN
    PRINT 'Migration process STOPPED at schema version ' + CAST(@CURRENT_SCHEMA_VERSION AS NVARCHAR) +
          '. WILL NOT upgrade to schema version ' + CAST(@TARGET_SCHEMA_VERSION AS NVARCHAR) +
          ', because @DISABLE_HEAVY_MIGRATIONS option is set.';
END
ELSE IF @CURRENT_SCHEMA_VERSION = 5
BEGIN
	PRINT 'Installing schema version 6';

	-- First, we will drop all the secondary indexes on the HangFire.Set table, because we will
	-- modify that table, and unknown indexes may be added there (see https://github.com/HangfireIO/Hangfire/issues/844).
	-- So, we'll drop all of them, and then re-create the required index with a well-known name.

	DECLARE @dropIndexSql NVARCHAR(MAX) = N'';
	SELECT @dropIndexSql += N'DROP INDEX ' + QUOTENAME(SCHEMA_NAME(o.[schema_id])) + '.' + QUOTENAME(o.name) + '.' + QUOTENAME(i.name) + ';'
	FROM sys.indexes AS i
	INNER JOIN sys.tables AS o
	ON i.[object_id] = o.[object_id]
	WHERE i.is_primary_key = 0
	AND i.index_id <> 0
	AND o.is_ms_shipped = 0
	AND SCHEMA_NAME(o.[schema_id]) = 'HangFire'
	AND o.name = 'Set';

	EXEC sp_executesql @dropIndexSql;
	PRINT 'Dropped all secondary indexes on the [Set] table';

	-- Next, we'll remove the unnecessary indexes. They were unnecessary in the previous schema,
	-- and are unnecessary in the new schema as well. We'll not re-create them.

	DROP INDEX [IX_HangFire_Hash_Key] ON [HangFire].[Hash];
	PRINT 'Dropped unnecessary index [IX_HangFire_Hash_Key]';

	-- Next, all the indexes that cover expiration will be filtered, to include only non-null values. This
	-- will prevent unnecessary index modifications – we are seeking these indexes only for non-null
	-- expiration time. Also, they include the Id column by a mistake. So we'll re-create them later in the
	-- migration.

	DROP INDEX [IX_HangFire_Hash_ExpireAt] ON [HangFire].[Hash];
	PRINT 'Dropped index [IX_HangFire_Hash_ExpireAt]';

	DROP INDEX [IX_HangFire_Job_ExpireAt] ON [HangFire].[Job];
	PRINT 'Dropped index [IX_HangFire_Job_ExpireAt]';

	DROP INDEX [IX_HangFire_List_ExpireAt] ON [HangFire].[List];
	PRINT 'Dropped index [IX_HangFire_List_ExpireAt]';

	-- IX_HangFire_Job_StateName index can also be optimized, since we are querying it only with a
	-- non-null state name. This will decrease the number of operations, when creating a background job.
	-- It will be recreated later in the migration.

	DROP INDEX [IX_HangFire_Job_StateName] ON [HangFire].Job;
	PRINT 'Dropped index [IX_HangFire_Job_StateName]';

	-- Dropping foreign key constraints based on the JobId column, because we need to modify the underlying
	-- column type of the clustered index to BIGINT. We'll recreate them later in the migration.

	ALTER TABLE [HangFire].[JobParameter] DROP CONSTRAINT [FK_HangFire_JobParameter_Job];
	PRINT 'Dropped constraint [FK_HangFire_JobParameter_Job]';

	ALTER TABLE [HangFire].[State] DROP CONSTRAINT [FK_HangFire_State_Job];
	PRINT 'Dropped constraint [FK_HangFire_State_Job]';

	-- We are going to create composite clustered indexes that are more natural for the following tables,
	-- so the following indexes will be unnecessary. Natural sorting will keep related data close to each
	-- other, and simplify the index modifications by the cost of fragmentation and additional page splits.

	DROP INDEX [UX_HangFire_CounterAggregated_Key] ON [HangFire].[AggregatedCounter];
	PRINT 'Dropped index [UX_HangFire_CounterAggregated_Key]';

	DROP INDEX [IX_HangFire_Counter_Key] ON [HangFire].[Counter];
	PRINT 'Dropped index [IX_HangFire_Counter_Key]';

	DROP INDEX [IX_HangFire_JobParameter_JobIdAndName] ON [HangFire].[JobParameter];
	PRINT 'Dropped index [IX_HangFire_JobParameter_JobIdAndName]';

	DROP INDEX [IX_HangFire_JobQueue_QueueAndFetchedAt] ON [HangFire].[JobQueue];
	PRINT 'Dropped index [IX_HangFire_JobQueue_QueueAndFetchedAt]';

	DROP INDEX [UX_HangFire_Hash_Key_Field] ON [HangFire].[Hash];
	PRINT 'Dropped index [UX_HangFire_Hash_Key_Field]';

	DROP INDEX [IX_HangFire_List_Key] ON [HangFire].[List];
	PRINT 'Dropped index [IX_HangFire_List_Key]';

	DROP INDEX [IX_HangFire_State_JobId] ON [HangFire].[State];
	PRINT 'Dropped index [IX_HangFire_State_JobId]';

	-- Then, we need to drop the primary key constraints, to modify id columns to the BIGINT type. Some of them
	-- will be re-created later in the migration. But some of them would be removed forever, because their
	-- uniqueness property sometimes unnecessary.

	ALTER TABLE [HangFire].[AggregatedCounter] DROP CONSTRAINT [PK_HangFire_CounterAggregated];
	PRINT 'Dropped constraint [PK_HangFire_CounterAggregated]';

	ALTER TABLE [HangFire].[Counter] DROP CONSTRAINT [PK_HangFire_Counter];
	PRINT 'Dropped constraint [PK_HangFire_Counter]';

	ALTER TABLE [HangFire].[Hash] DROP CONSTRAINT [PK_HangFire_Hash];
	PRINT 'Dropped constraint [PK_HangFire_Hash]';

	ALTER TABLE [HangFire].[Job] DROP CONSTRAINT [PK_HangFire_Job];
	PRINT 'Dropped constraint [PK_HangFire_Job]';

	ALTER TABLE [HangFire].[JobParameter] DROP CONSTRAINT [PK_HangFire_JobParameter];
	PRINT 'Dropped constraint [PK_HangFire_JobParameter]';

	ALTER TABLE [HangFire].[JobQueue] DROP CONSTRAINT [PK_HangFire_JobQueue];
	PRINT 'Dropped constraint [PK_HangFire_JobQueue]';

	ALTER TABLE [HangFire].[List] DROP CONSTRAINT [PK_HangFire_List];
	PRINT 'Dropped constraint [PK_HangFire_List]';

	ALTER TABLE [HangFire].[Set] DROP CONSTRAINT [PK_HangFire_Set];
	PRINT 'Dropped constraint [PK_HangFire_Set]';

	ALTER TABLE [HangFire].[State] DROP CONSTRAINT [PK_HangFire_State];
	PRINT 'Dropped constraint [PK_HangFire_State]';

	-- We are removing identity columns of the following tables completely, their clustered
	-- index will be based on natural values. So, instead of modifying them to BIGINT, we
	-- are dropping them.

	ALTER TABLE [HangFire].[AggregatedCounter] DROP COLUMN [Id];
	PRINT 'Dropped [AggregatedCounter].[Id] column, we will cluster on [Key] column with uniqufier';

	ALTER TABLE [HangFire].[Counter] DROP COLUMN [Id];
	PRINT 'Dropped [Counter].[Id] column, we will cluster on [Key] column';

	ALTER TABLE [HangFire].[Hash] DROP COLUMN [Id];
	PRINT 'Dropped [Hash].[Id] column, we will cluster on [Key]/[Field] columns';

	ALTER TABLE [HangFire].[Set] DROP COLUMN [Id];
	PRINT 'Dropped [Set].[Id] column, we will cluster on [Key]/[Value] columns';

	ALTER TABLE [HangFire].[JobParameter] DROP COLUMN [Id];
	PRINT 'Dropped [JobParameter].[Id] column, we will cluster on [JobId]/[Name] columns';

	-- Then we need to modify all the remaining Id columns to be of type BIGINT.

	ALTER TABLE [HangFire].[List] ALTER COLUMN [Id] BIGINT NOT NULL;
	PRINT 'Modified [List].[Id] type to BIGINT';

	ALTER TABLE [HangFire].[Job] ALTER COLUMN [Id] BIGINT NOT NULL;
	PRINT 'Modified [Job].[Id] type to BIGINT';

	ALTER TABLE [HangFire].[Job] ALTER COLUMN [StateId] BIGINT NULL;
	PRINT 'Modified [Job].[StateId] type to BIGINT';

	ALTER TABLE [HangFire].[JobParameter] ALTER COLUMN [JobId] BIGINT NOT NULL;
	PRINT 'Modified [JobParameter].[JobId] type to BIGINT';

	ALTER TABLE [HangFire].[JobQueue] ALTER COLUMN [JobId] BIGINT NOT NULL;
	PRINT 'Modified [JobQueue].[JobId] type to BIGINT';

	ALTER TABLE [HangFire].[JobQueue] ALTER COLUMN [Id] BIGINT NOT NULL;
	PRINT 'Modified [JobQueue].[Id] type to BIGINT';

	ALTER TABLE [HangFire].[State] ALTER COLUMN [Id] BIGINT NOT NULL;
	PRINT 'Modified [State].[Id] type to BIGINT';

	ALTER TABLE [HangFire].[State] ALTER COLUMN [JobId] BIGINT NOT NULL;
	PRINT 'Modified [State].[JobId] type to BIGINT';

	ALTER TABLE [HangFire].[Counter] ALTER COLUMN [Value] INT NOT NULL;
	PRINT 'Modified [Counter].[Value] type to INT';

	-- Adding back all the Primary Key constraints or clustered indexes where PKs aren't appropriate.

	ALTER TABLE [HangFire].[AggregatedCounter] ADD CONSTRAINT [PK_HangFire_CounterAggregated] PRIMARY KEY CLUSTERED (
		[Key] ASC
	);
	PRINT 'Re-created constraint [PK_HangFire_CounterAggregated]';

	CREATE CLUSTERED INDEX [CX_HangFire_Counter] ON [HangFire].[Counter] ([Key]);
	PRINT 'Created clustered index [CX_HangFire_Counter]';

	ALTER TABLE [HangFire].[Hash] ADD CONSTRAINT [PK_HangFire_Hash] PRIMARY KEY CLUSTERED (
		[Key] ASC,
		[Field] ASC
	);
	PRINT 'Re-created constraint [PK_HangFire_Hash]';

	ALTER TABLE [HangFire].[Job] ADD CONSTRAINT [PK_HangFire_Job] PRIMARY KEY CLUSTERED ([Id] ASC);
	PRINT 'Re-created constraint [PK_HangFire_Job]';
	
	ALTER TABLE [HangFire].[JobParameter] ADD CONSTRAINT [PK_HangFire_JobParameter] PRIMARY KEY CLUSTERED (
		[JobId] ASC,
		[Name] ASC
	);
	PRINT 'Re-created constraint [PK_HangFire_JobParameter]';

	ALTER TABLE [HangFire].[JobQueue] ADD CONSTRAINT [PK_HangFire_JobQueue] PRIMARY KEY CLUSTERED (
		[Queue] ASC,
		[Id] ASC
	);
	PRINT 'Re-created constraint [PK_HangFire_JobQueue]';

	ALTER TABLE [HangFire].[List] ADD CONSTRAINT [PK_HangFire_List] PRIMARY KEY CLUSTERED (
		[Key] ASC,
		[Id] ASC
	);
	PRINT 'Re-created constraint [PK_HangFire_List]';

	ALTER TABLE [HangFire].[Set] ADD CONSTRAINT [PK_HangFire_Set] PRIMARY KEY CLUSTERED (
		[Key] ASC,
		[Value] ASC
	);
	PRINT 'Re-created constraint [PK_HangFire_Set]';

	ALTER TABLE [HangFire].[State] ADD CONSTRAINT [PK_HangFire_State] PRIMARY KEY CLUSTERED (
		[JobId] ASC,
		[Id]
	);
	PRINT 'Re-created constraint [PK_HangFire_State]';

	-- Creating secondary, nonclustered indexes

	CREATE NONCLUSTERED INDEX [IX_HangFire_Job_StateName] ON [HangFire].[Job] ([StateName])
	WHERE [StateName] IS NOT NULL;
	PRINT 'Re-created index [IX_HangFire_Job_StateName]';

	CREATE NONCLUSTERED INDEX [IX_HangFire_Set_Score] ON [HangFire].[Set] ([Score])
	WHERE [Score] IS NOT NULL;
	PRINT 'Created index [IX_HangFire_Set_Score]';

	CREATE NONCLUSTERED INDEX [IX_HangFire_Server_LastHeartbeat] ON [HangFire].[Server] ([LastHeartbeat]);
	PRINT 'Created index [IX_HangFire_Server_LastHeartbeat]';

	-- Creating filtered indexes for ExpireAt columns

	CREATE NONCLUSTERED INDEX [IX_HangFire_AggregatedCounter_ExpireAt] ON [HangFire].[AggregatedCounter] ([ExpireAt])
	WHERE [ExpireAt] IS NOT NULL;
	PRINT 'Created index [IX_HangFire_AggregatedCounter_ExpireAt]';

	CREATE NONCLUSTERED INDEX [IX_HangFire_Hash_ExpireAt] ON [HangFire].[Hash] ([ExpireAt])
	WHERE [ExpireAt] IS NOT NULL;
	PRINT 'Re-created index [IX_HangFire_Hash_ExpireAt]';

	CREATE NONCLUSTERED INDEX [IX_HangFire_Job_ExpireAt] ON [HangFire].[Job] ([ExpireAt])
	INCLUDE ([StateName])
	WHERE [ExpireAt] IS NOT NULL;
	PRINT 'Re-created index [IX_HangFire_Job_ExpireAt]';

	CREATE NONCLUSTERED INDEX [IX_HangFire_List_ExpireAt] ON [HangFire].[List] ([ExpireAt])
	WHERE [ExpireAt] IS NOT NULL;
	PRINT 'Re-created index [IX_HangFire_List_ExpireAt]';

	CREATE NONCLUSTERED INDEX [IX_HangFire_Set_ExpireAt] ON [HangFire].[Set] ([ExpireAt])
	WHERE [ExpireAt] IS NOT NULL;
	PRINT 'Re-created index [IX_HangFire_Set_ExpireAt]';

	-- Restoring foreign keys

	ALTER TABLE [HangFire].[State] ADD CONSTRAINT [FK_HangFire_State_Job] FOREIGN KEY([JobId])
		REFERENCES [HangFire].[Job] ([Id])
		ON UPDATE CASCADE
		ON DELETE CASCADE;
	PRINT 'Re-created constraint [FK_HangFire_State_Job]';

	ALTER TABLE [HangFire].[JobParameter] ADD CONSTRAINT [FK_HangFire_JobParameter_Job] FOREIGN KEY([JobId])
		REFERENCES [HangFire].[Job] ([Id])
		ON UPDATE CASCADE
		ON DELETE CASCADE;
	PRINT 'Re-created constraint [FK_HangFire_JobParameter_Job]';

	SET @CURRENT_SCHEMA_VERSION = 6;
END

IF @CURRENT_SCHEMA_VERSION = 6
BEGIN
	PRINT 'Installing schema version 7';

	DROP INDEX [IX_HangFire_Set_Score] ON [HangFire].[Set];
	PRINT 'Dropped index [IX_HangFire_Set_Score]';

	CREATE NONCLUSTERED INDEX [IX_HangFire_Set_Score] ON [HangFire].[Set] ([Key], [Score]);
	PRINT 'Created index [IX_HangFire_Set_Score] with the proper composite key';

	SET @CURRENT_SCHEMA_VERSION = 7;
END

/*IF @CURRENT_SCHEMA_VERSION = 7
BEGIN
	PRINT 'Installing schema version 8';

	 Insert migration here
	-- TODO: Increase Server.ServerId column length to 200, add IGNORE_DUP_KEY for [Set] and [Hash] tables
	-- TODO: Add clustered index for [AggregatedCounter] table

	SET @CURRENT_SCHEMA_VERSION = 8;
END*/

UPDATE [HangFire].[Schema] SET [Version] = @CURRENT_SCHEMA_VERSION
IF @@ROWCOUNT = 0 
	INSERT INTO [HangFire].[Schema] ([Version]) VALUES (@CURRENT_SCHEMA_VERSION)        

PRINT 'Hangfire database schema installed';

COMMIT TRANSACTION;
PRINT 'Hangfire SQL objects installed';

