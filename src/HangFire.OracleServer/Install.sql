declare
  v_exists pls_integer;
  v_schema_version pls_integer;
  v_target_schema_version pls_integer;

begin  

  v_target_schema_version := 4;

  DBMS_OUTPUT.put_line('Installing Hangfire Oracle objects...');

  select count(1)
    into v_exists
    from dba_users
   where lower(username) = 'hangfire';

  if v_exists = 0 then
    begin
      EXECUTE IMMEDIATE 'CREATE USER hangfire IDENTIFIED BY hangfire DEFAULT TABLESPACE GC_DAT_USR TEMPORARY TABLESPACE TEMP';
      DBMS_OUTPUT.put_line('Created database user/schema [hangfire]');
    end;
  else
    DBMS_OUTPUT.put_line('Database schema [hangfire] already exists;');
  end if;
  
  BEGIN
    execute immediate 'CREATE TABLE "schema" ("version" INT NOT NULL, CONSTRAINT schema_primary PRIMARY KEY ("version"))';
    DBMS_OUTPUT.put_line('Created TABLE user/schema [hangfire]');
    
  EXCEPTION
    WHEN OTHERS THEN
      IF SQLCODE = -955 THEN
        DBMS_OUTPUT.put_line('Table [hangfire].[schema] already exists');
      ELSE
         RAISE;
      END IF;
  END; 
  
  SELECT NVL(max("version"), 0) INTO v_schema_version FROM "schema" ;
  
  DBMS_OUTPUT.put_line('Current HangFire schema version: ' || v_schema_version);
  
  IF v_schema_version >= v_target_schema_version THEN
    begin
      raise_application_error(-20999, 'HangFire current database schema version ' || v_schema_version ||  ' is equal or newer than the configured SqlServerStorage schema version ' || v_target_schema_version || '. Please update to the latest HangFire.SqlServer NuGet package.');
    end;
  ELSE
    BEGIN
      IF v_schema_version = 0 THEN
        BEGIN
          
        /*
            DROP INDEX IX_HF_Job_StateName;
            DROP SEQUENCE SEQHangFireJob;
            DROP TABLE hangfire.Job CASCADE CONSTRAINTS;
            
            DROP INDEX IX_HF_State_JobId;
            DROP SEQUENCE SEQHangFireState;
            DROP TABLE hangfire.State CASCADE CONSTRAINTS;
            
            DROP INDEX IX_HF_JobParameter_JobIdName;
            DROP SEQUENCE SEQHangFireJobParameter;
            DROP TABLE hangfire.JobParameter CASCADE CONSTRAINTS;
            
            DROP INDEX IX_HF_JobQueue_QueueFetchedAt;
            DROP INDEX IX_HF_JobQueue_JobIdAndQueue;
            DROP SEQUENCE SEQHangFireJobQueue;
            DROP TABLE hangfire.JobQueue CASCADE CONSTRAINTS;
            
            DROP TABLE hangfire.Server CASCADE CONSTRAINTS;            
            
            DROP INDEX IX_HF_Counter_Key;
            DROP SEQUENCE SEQHangFireCounter;
            DROP TABLE hangfire.Counter CASCADE CONSTRAINTS;
            
            DROP INDEX UX_HF_Value_Key;
            DROP SEQUENCE SEQHangFireValue;
            DROP TABLE hangfire.Value CASCADE CONSTRAINTS;
            
            DROP INDEX UX_HF_Sett_KeyAndValue;
            DROP SEQUENCE SEQHangFireSett;
            DROP TABLE hangfire.Sett CASCADE CONSTRAINTS;
            
            DROP SEQUENCE SEQHangFireList;
            DROP TABLE hangfire.List CASCADE CONSTRAINTS;
            
            DROP INDEX UX_HF_Hash_KeyAndName;            
            DROP SEQUENCE SEQHangFireHash;            
            DROP TABLE hangfire.Hash CASCADE CONSTRAINTS;      
            
        */
        
          DBMS_OUTPUT.put_line('Installing schema version 1');
          
           -- Create job tables
            EXECUTE IMMEDIATE 'CREATE TABLE hangfire.Job (
              Id NUMBER NOT NULL,
              StateId NUMBER NULL,
              StateName VARCHAR2(20) NULL, -- To speed-up queries.
              InvocationData VARCHAR2(4000) NOT NULL,
              Arguments VARCHAR2(4000) NOT NULL,
              CreatedAt date NOT NULL,
              ExpireAt date NULL,

              CONSTRAINT PK_HF_Job PRIMARY KEY (Id)
            )';
              
          DBMS_OUTPUT.put_line('Created TABLE hangfire.Job');
            
          EXECUTE IMMEDIATE 'CREATE SEQUENCE SEQHangFireJob 
          START WITH     1
          INCREMENT BY   1
          MAXVALUE       9999999999999999999999999999
          NOCACHE
          NOCYCLE';
            
          DBMS_OUTPUT.put_line('Created SEQUENCE SEQHangFireJob');

          EXECUTE IMMEDIATE 'CREATE INDEX IX_HF_Job_StateName on hangfire.Job (StateName)';

          DBMS_OUTPUT.put_line('Created INDEX IX_HF_Job_StateName');

          -- Job history table       

          EXECUTE IMMEDIATE 'CREATE TABLE hangfire.State (
            Id NUMBER NOT NULL,
            JobId int NOT NULL,
            "Name" VARCHAR2(20) NOT NULL,
            "Reason" VARCHAR2(100) NULL,
            "CreatedAt" DATE NOT NULL,
            "Data" VARCHAR2(4000) NULL,
                  
            CONSTRAINT PK_HF_State PRIMARY KEY (Id),
            CONSTRAINT FK_HF_State_Job FOREIGN  KEY (Id) REFERENCES hangfire.Job (Id)
            ON DELETE CASCADE
          )';  --ON UPDATE CASCADE

              
          DBMS_OUTPUT.put_line('Created TABLE hangfire.State');
              
          EXECUTE IMMEDIATE 'CREATE SEQUENCE SEQHangFireState 
            START WITH     1
            INCREMENT BY   1
            MAXVALUE       9999999999999999999999999999
            NOCACHE
            NOCYCLE';
                
          DBMS_OUTPUT.put_line('Created SEQUENCE SEQHangFireJob');
              
          EXECUTE IMMEDIATE 'CREATE INDEX IX_HF_State_JobId ON hangfire.State (JobId)';
            
          DBMS_OUTPUT.put_line('Created INDEX IX_HF_State_JobId');
             
          -- Job parameters table
              
          EXECUTE IMMEDIATE 'CREATE TABLE hangfire.JobParameter(
              Id NUMBER NOT NULL,
              JobId NUMBER NOT NULL,
              Name VARCHAR2(40) NOT NULL,
              Value VARCHAR2(4000) NULL,                  
              CONSTRAINT PK_HF_JobParameter PRIMARY KEY (Id),
              CONSTRAINT FK_HF_JobParameter_Job FOREIGN KEY(JobId)
                REFERENCES hangfire.Job (Id)
                ON DELETE CASCADE
          )'; --  ON UPDATE CASCADE
              
          DBMS_OUTPUT.put_line('Created TABLE hangfire.JobParameter');
              
          EXECUTE IMMEDIATE 'CREATE SEQUENCE SEQHangFireJobParameter 
            START WITH     1
            INCREMENT BY   1
            MAXVALUE       9999999999999999999999999999
            NOCACHE
            NOCYCLE';
                
          DBMS_OUTPUT.put_line('Created SEQUENCE SEQHangFireJobParameter');
              
          EXECUTE IMMEDIATE 'CREATE INDEX IX_HF_JobParameter_JobIdName ON hangfire.JobParameter (
              JobId,
              Name
          )';
              
          DBMS_OUTPUT.put_line('Created INDEX IX_HF_JobParameter_JobIdName');
            
          EXECUTE IMMEDIATE 'CREATE TABLE hangfire.JobQueue(
            Id NUMBER NOT NULL,
            JobId NUMBER NOT NULL,
            Queue VARCHAR2(20) NOT NULL,
            FetchedAt date NULL,
                  
            CONSTRAINT PK_HF_JobQueue PRIMARY KEY  (Id),
            CONSTRAINT FK_HF_JobQueue_Job FOREIGN KEY(JobId)
            REFERENCES hangfire.Job (Id)
            ON DELETE CASCADE
          )'; -- FK_HF_JobQueue_Job did not existe in the main code but i believe it is needed
              
          DBMS_OUTPUT.put_line('Created TABLE hangfire.JobQueue');
              
          EXECUTE IMMEDIATE 'CREATE SEQUENCE SEQHangFireJobQueue 
            START WITH     1
            INCREMENT BY   1
            MAXVALUE       9999999999999999999999999999
            NOCACHE
            NOCYCLE';
                
          DBMS_OUTPUT.put_line('Created SEQUENCE SEQHangFireJobQueue');
              
          EXECUTE IMMEDIATE 'CREATE INDEX IX_HF_JobQueue_JobIdAndQueue ON hangfire.JobQueue (
              JobId,
              Queue
          )';
              
          DBMS_OUTPUT.put_line('Created INDEX IX_HF_JobQueue_JobIdAndQueue');
              
          EXECUTE IMMEDIATE 'CREATE INDEX IX_HF_JobQueue_QueueFetchedAt ON hangfire.JobQueue (
              Queue,
              FetchedAt
          )';
              
          DBMS_OUTPUT.put_line('Created INDEX IX_HF_JobQueue_QueueFetchedAt');
              
          -- Servers table
              
          EXECUTE IMMEDIATE 'CREATE TABLE hangfire.Server(
              Id VARCHAR2(50) NOT NULL,
              Data VARCHAR2(4000) NULL,
              LastHeartbeat date NULL,
                  
              CONSTRAINT PK_HF_Server PRIMARY KEY (Id)
          )';
              
          DBMS_OUTPUT.put_line('Created TABLE hangfire.Server');              

          -- Extension tables
              
          EXECUTE IMMEDIATE 'CREATE TABLE hangfire.Hash(
              Id NUMBER NOT NULL,
              Key VARCHAR2(100) NOT NULL,
              Name VARCHAR2(40) NOT NULL,
              StringValue VARCHAR2(4000) NULL,
              IntValue int NULL,
              ExpireAt date NULL,
                  
              CONSTRAINT PK_HF_Hash PRIMARY KEY (Id)
          )';
          
          DBMS_OUTPUT.put_line('Created TABLE hangfire.Hash');
              
          EXECUTE IMMEDIATE 'CREATE SEQUENCE SEQHangFireHash 
            START WITH     1
            INCREMENT BY   1
            MAXVALUE       9999999999999999999999999999
            NOCACHE
            NOCYCLE';
                
          DBMS_OUTPUT.put_line('Created SEQUENCE SEQHangFireHash');
              
          EXECUTE IMMEDIATE 'CREATE UNIQUE INDEX UX_HF_Hash_KeyAndName ON hangfire.Hash (
              Key,
              Name
          )';
          
          DBMS_OUTPUT.put_line('Created INDEX UX_HF_Hash_KeyAndName');
              
          EXECUTE IMMEDIATE 'CREATE TABLE hangfire.List(
              Id NUMBER NOT NULL,
              Key VARCHAR2(100) NOT NULL,
              Value VARCHAR2(4000) NULL,
              ExpireAt DATE NULL,
                  
              CONSTRAINT PK_HF_List PRIMARY KEY (Id)
          )';
          
          DBMS_OUTPUT.put_line('Created TABLE hangfire.List');
              
          EXECUTE IMMEDIATE 'CREATE SEQUENCE SEQHangFireList 
            START WITH     1
            INCREMENT BY   1
            MAXVALUE       9999999999999999999999999999
            NOCACHE
            NOCYCLE';
                
          DBMS_OUTPUT.put_line('Created SEQUENCE SEQHangFireList');
              
          EXECUTE IMMEDIATE 'CREATE TABLE hangfire.Sett(
              Id NUMBER NOT NULL,
              Key VARCHAR2(100) NOT NULL,
              Score NUMBER(13,2) NOT NULL,
              Value VARCHAR2(256) NOT NULL,
              ExpireAt DATE NULL,
                  
              CONSTRAINT PK_HF_Set PRIMARY KEY (Id)
          )';
          
          -- The name Set is invalid for a table on Oracle
          
          DBMS_OUTPUT.put_line('Created TABLE hangfire.Sett');
              
          EXECUTE IMMEDIATE 'CREATE SEQUENCE SEQHangFireSett 
            START WITH     1
            INCREMENT BY   1
            MAXVALUE       9999999999999999999999999999
            NOCACHE
            NOCYCLE';
                
          DBMS_OUTPUT.put_line('Created SEQUENCE SEQHangFireSett');
              
          EXECUTE IMMEDIATE 'CREATE UNIQUE INDEX UX_HF_Sett_KeyAndValue ON hangfire.Sett (
              Key,
              Value
          )';
          
          DBMS_OUTPUT.put_line('Created INDEX UX_HF_Sett_KeyAndValue');
              
          EXECUTE IMMEDIATE 'CREATE TABLE hangfire.Value(
              Id NUMBER NOT NULL,
              Key VARCHAR2(100) NOT NULL,
              StringValue VARCHAR2(4000) NULL,
              IntValue int NULL,
              ExpireAt DATE NULL,
                  
              CONSTRAINT PK_HF_Value PRIMARY KEY (Id)
          )';
              
          DBMS_OUTPUT.put_line('Created TABLE hangfire.Value');
              
          EXECUTE IMMEDIATE 'CREATE SEQUENCE SEQHangFireValue 
            START WITH     1
            INCREMENT BY   1
            MAXVALUE       9999999999999999999999999999
            NOCACHE
            NOCYCLE';
                
          DBMS_OUTPUT.put_line('Created SEQUENCE SEQHangFireValue');
              
          EXECUTE IMMEDIATE 'CREATE UNIQUE INDEX UX_HF_Value_Key ON hangfire.Value (Key)';
          
          DBMS_OUTPUT.put_line('Created INDEX UX_HF_Value_Key');

          EXECUTE IMMEDIATE 'CREATE TABLE hangfire.Counter(
            Id NUMBER NOT NULL,
            Key VARCHAR2(100) NOT NULL,
            Value SMALLINT NOT NULL,
            ExpireAt DATE NULL,

            CONSTRAINT PK_HF_Counter PRIMARY KEY (Id)
          )';
          
          DBMS_OUTPUT.put_line('Created TABLE hangfire.Counter');
              
          EXECUTE IMMEDIATE 'CREATE SEQUENCE SEQHangFireCounter 
            START WITH     1
            INCREMENT BY   1
            MAXVALUE       9999999999999999999999999999
            NOCACHE
            NOCYCLE';
                
          DBMS_OUTPUT.put_line('Created SEQUENCE SEQHangFireCounter');

          EXECUTE IMMEDIATE 'CREATE INDEX IX_HF_Counter_Key ON hangfire.Counter (Key, Value)';
          -- ON SqL plugin the Value is added and INCLUDE
          
          DBMS_OUTPUT.put_line('Created INDEX IX_HF_Counter_Key');

          v_schema_version := 1;
        END;
      END IF;
            
            
      IF v_schema_version = 1 THEN
        BEGIN
          DBMS_OUTPUT.put_line('Installing schema version 2');

          -- https://github.com/odinserj/HangFire/issues/83

          EXECUTE IMMEDIATE 'DROP INDEX IX_HF_Counter_Key';

          --EXECUTE IMMEDIATE 'ALTER TABLE hangfire.Counter MODIFY (Value SMALLINT NOT NULL)';
          -- comment due to the fact the field already is exacly on the creation of the table

          EXECUTE IMMEDIATE 'CREATE INDEX IX_HF_Counter_Key ON hangfire.Counter (Key, Value)';
          -- SqL version - INCLUDE (Value);
          
          DBMS_OUTPUT.put_line('Index IX_HF_Counter_Key re-created');

          EXECUTE IMMEDIATE 'DROP TABLE hangfire.Value';
          EXECUTE IMMEDIATE 'DROP TABLE hangfire.Hash';
          
          DBMS_OUTPUT.put_line('Dropped tables hangfire.Value and hangfire.Hash');
          
          EXECUTE IMMEDIATE 'DROP SEQUENCE SEQHangFireHash';
          
          DBMS_OUTPUT.put_line('Dropped sequence SEQHangFireHash');

          EXECUTE IMMEDIATE 'DELETE FROM hangfire.Server WHERE LastHeartbeat IS NULL';
          
          EXECUTE IMMEDIATE 'ALTER TABLE hangfire.Server MODIFY ( LastHeartbeat DATE NOT NULL)';

          v_schema_version := 2;
        END;
      END IF;      

      IF v_schema_version = 2 THEN
        BEGIN
          
        /*
          DROP INDEX UX_HF_Hash_Key_Field;
          DROP SEQUENCE SEQHangFireHash;
          DROP TABLE hangfire.Hash CASCADE CONSTRAINTS;           
        */
                    
          DBMS_OUTPUT.put_line('Installing schema version 3');

          EXECUTE IMMEDIATE 'DROP INDEX IX_HF_JobQueue_JobIdAndQueue';
            
          DBMS_OUTPUT.put_line('Dropped index IX_HF_JobQueue_JobIdAndQueue');

          EXECUTE IMMEDIATE 'CREATE TABLE hangfire.Hash(
            Id NUMBER  NOT NULL,
            Key VARCHAR2(100) NOT NULL,
            Field VARCHAR2(100) NOT NULL,
            Value VARCHAR2(4000) NULL,
            ExpireAt DATE NULL,
            
            CONSTRAINT PK_HF_Hash PRIMARY KEY (Id)
          )';
            
          DBMS_OUTPUT.put_line('Created table hangfire.Hash');
            
          EXECUTE IMMEDIATE 'CREATE SEQUENCE SEQHangFireHash 
          START WITH     1
          INCREMENT BY   1
          MAXVALUE       9999999999999999999999999999
          NOCACHE
          NOCYCLE';
            
          DBMS_OUTPUT.put_line('Created SEQUENCE SEQHangFireHash');

          EXECUTE IMMEDIATE 'CREATE UNIQUE INDEX UX_HF_Hash_Key_Field ON hangfire.Hash (
            Key,
            Field
          )';
            
          DBMS_OUTPUT.put_line('Created index UX_HF_Hash_Key_Field');

          v_schema_version := 3;
        END;
       END IF;        

      IF v_schema_version = 3 THEN
        BEGIN
          
        /*
            DROP INDEX IX_HF_Sett_Key;
            DROP INDEX IX_HF_List_Key;
            DROP INDEX IX_HF_Hash_Key;
            DROP INDEX IX_HF_Sett_ExpireAt;
            DROP INDEX IX_HF_List_ExpireAt;
            DROP INDEX IX_HF_Job_ExpireAt;
            DROP INDEX IX_HF_Hash_ExpireAt;
            DROP INDEX UX_HF_CounterAggregated_Key;
            DROP SEQUENCE SEQHangFireAggregatedCounter;
            DROP TABLE hangfire.AggregatedCounter CASCADE CONSTRAINTS;
        */  
          
          DBMS_OUTPUT.put_line('Installing schema version 4');

          EXECUTE IMMEDIATE 'CREATE TABLE hangfire.AggregatedCounter (
            Id NUMBER NOT NULL,
            Key VARCHAR2(100) NOT NULL,
            Value NUMBER NOT NULL,
            ExpireAt DATE NULL,

            CONSTRAINT PK_HF_CounterAggregated PRIMARY KEY (Id)
          )';
            
          DBMS_OUTPUT.put_line('Created table hangfire.AggregatedCounter');
              
          EXECUTE IMMEDIATE 'CREATE SEQUENCE SEQHangFireAggregatedCounter 
          START WITH     1
          INCREMENT BY   1
          MAXVALUE       9999999999999999999999999999
          NOCACHE
          NOCYCLE';
              
          DBMS_OUTPUT.put_line('Created SEQUENCE SEQHangFireAggregatedCounter');

          EXECUTE IMMEDIATE 'CREATE UNIQUE INDEX UX_HF_CounterAggregated_Key ON hangfire.AggregatedCounter (
            Key,
            Value
          )';
          
          -- INCLUDE (Value);
          DBMS_OUTPUT.put_line('Created index UX_HF_CounterAggregated_Key');

          EXECUTE IMMEDIATE 'CREATE INDEX IX_HF_Hash_ExpireAt ON hangfire.Hash (ExpireAt, Id)';
          -- INCLUDE (Id);

          EXECUTE IMMEDIATE 'CREATE INDEX IX_HF_Job_ExpireAt ON hangfire.Job (ExpireAt, Id)';
          -- INCLUDE (Id);

          EXECUTE IMMEDIATE 'CREATE INDEX IX_HF_List_ExpireAt ON hangfire.List (ExpireAt, Id)';
          -- INCLUDE (Id);

          EXECUTE IMMEDIATE 'CREATE INDEX IX_HF_Sett_ExpireAt ON hangfire.Sett (ExpireAt, Id)';
          -- INCLUDE (Id);

          DBMS_OUTPUT.put_line('Created indexes for ExpireAt columns');

          EXECUTE IMMEDIATE 'CREATE INDEX IX_HF_Hash_Key ON hangfire.Hash (Key, ExpireAt)';
          -- INCLUDE (ExpireAt);
              
          DBMS_OUTPUT.put_line('Created index IX_HF_Hash_Key');

          EXECUTE IMMEDIATE 'CREATE INDEX IX_HF_List_Key ON hangfire.List (Key, ExpireAt, Value)';
          -- INCLUDE (ExpireAt, Value);
              
          DBMS_OUTPUT.put_line('Created index IX_HF_List_Key');

          EXECUTE IMMEDIATE 'CREATE INDEX IX_HF_Sett_Key ON hangfire.Sett (Key, ExpireAt, Value)';
          -- INCLUDE (ExpireAt, Value);
              
          DBMS_OUTPUT.put_line('Created index IX_HF_Sett_Key');

          v_schema_version := 4;
        END;
      END IF;
      
      EXECUTE IMMEDIATE 'UPDATE "schema" SET "version" = ' || v_schema_version;
          
      IF SQL%ROWCOUNT = 0 THEN
        EXECUTE IMMEDIATE 'INSERT INTO "schema" ("version") VALUES (' || v_schema_version || ')';
      END IF;        

        DBMS_OUTPUT.put_line('HangFire database schema installed');
        DBMS_OUTPUT.put_line('HangFire ORACLE objects installed');
        
        COMMIT;
    END;
  END IF;

end;