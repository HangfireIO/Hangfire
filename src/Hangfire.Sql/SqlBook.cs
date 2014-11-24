namespace Hangfire.Sql {
    public class SqlBook {
        public string SqlConnection_CreateExpiredJob_Job = @"
insert into HangFire.Job (InvocationData, Arguments, CreatedAt, ExpireAt)
values (@invocationData, @arguments, @createdAt, @expireAt);
SELECT CAST(SCOPE_IDENTITY() as int)";

        public string SqlConnection_CreateExpiredJob_Parameter = @"
insert into HangFire.JobParameter (JobId, Name, Value)
values (@jobId, @name, @value)";

        public string SqlConnection_GetJobData = @"select InvocationData, StateName, Arguments, CreatedAt from HangFire.Job where Id = @id";

        public string SqlConnection_GetStateData = @"
select s.Name, s.Reason, s.Data
from HangFire.State s
inner join HangFire.Job j on j.StateId = s.Id
where j.Id = @jobId";

        public string SqlConnection_SetJobParameter = @"
merge HangFire.JobParameter as Target
using (VALUES (@jobId, @name, @value)) as Source (JobId, Name, Value)
on Target.JobId = Source.JobId AND Target.Name = Source.Name
when matched then update set Value = Source.Value
when not matched then insert (JobId, Name, Value) values (Source.JobId, Source.Name, Source.Value)";

        public string SqlConnection_GetJobParameter =
            @"select Value from HangFire.JobParameter where JobId = @id and Name = @name";

        public string SqlConnection_GetAllItemsFromSet =
            @"select Value from HangFire.[Set] where [Key] = @key";

        public string SqlConnection_GetFirstByLowestScoreFromSet =
            @"select top 1 Value from HangFire.[Set] where [Key] = @key and Score between @from and @to order by Score";

        public string SqlConnection_SetRangeInHash = @"
merge HangFire.Hash as Target
using (VALUES (@key, @field, @value)) as Source ([Key], Field, Value)
on Target.[Key] = Source.[Key] and Target.Field = Source.Field
when matched then update set Value = Source.Value
when not matched then insert ([Key], Field, Value) values (Source.[Key], Source.Field, Source.Value);";

        public string SqlConnection_GetAllEntriesFromHash = "select Field, Value from HangFire.Hash where [Key] = @key";
        
        public string SqlConnection_AnnounceServer = @"
merge HangFire.Server as Target
using (VALUES (@id, @data, @heartbeat)) as Source (Id, Data, Heartbeat)
on Target.Id = Source.Id 
when matched then update set Data = Source.Data, LastHeartbeat = Source.Heartbeat 
when not matched then insert (Id, Data, LastHeartbeat) values (Source.Id, Source.Data, Source.Heartbeat)";

        public string SqlConnection_RemoveServer = @"delete from HangFire.Server where Id = @id";

        public string SqlConnection_Heartbeat = @"update HangFire.Server set LastHeartbeat = @now where Id = @id";

        public string SqlConnection_RemoveTimedOutServers = @"delete from HangFire.Server where LastHeartbeat < @timeOutAt";

        public string SqlMonitoringApi_Servers = @"select * from HangFire.Server";
        public string SqlMonitoringApi_JobDetails = @"
select * from HangFire.Job where Id = @id
select * from HangFire.JobParameter where JobId = @id
select * from HangFire.State where JobId = @id order by Id desc";
        public string SqlMonitoringApi_GetStatistics = @"
select StateName as [State], count(Id) as [Count] From HangFire.Job 
group by StateName
having StateName is not null;
select count(Id) from HangFire.Server;
select sum([Value]) from HangFire.Counter where [Key] = N'stats:succeeded';
select sum([Value]) from HangFire.Counter where [Key] = N'stats:deleted';
select count(*) from HangFire.[Set] where [Key] = N'recurring-jobs';
";
        public string SqlMonitoringApi_GetHourlyTimelineStats = @"
select [Key], count([Value]) as Count from [HangFire].[Counter]
group by [Key]
having [Key] in @keys";

        public string SqlMonitoringApi_GetTimelineStats = @"
select [Key], count([Value]) as Count from [HangFire].[Counter]
group by [Key]
having [Key] in @keys";

        public string SqlMonitoringApi_EnqueuedJobs = @"
select j.*, s.Reason as StateReason, s.Data as StateData 
from HangFire.Job j
left join HangFire.State s on s.Id = j.StateId
left join HangFire.JobQueue jq on jq.JobId = j.Id
where j.Id in @jobIds and jq.FetchedAt is null";

        public string SqlMonitoringApi_GetNumberOfJobsByStateName = @"
select count(Id) from HangFire.Job where StateName = @state";

        public string SqlMonitoringApi_GetJobs = @"
select * from (
  select j.*, s.Reason as StateReason, s.Data as StateData, row_number() over (order by j.Id desc) as row_num
  from HangFire.Job j
  left join HangFire.State s on j.StateId = s.Id
  where j.StateName = @stateName
) as j where j.row_num between @vstart and @vend
";
        public string SqlMonitoringApi_FetchedJobs = @"
select j.*, jq.FetchedAt, s.Reason as StateReason, s.Data as StateData 
from HangFire.Job j
left join HangFire.State s on s.Id = j.StateId
left join HangFire.JobQueue jq on jq.JobId = j.Id
where j.Id in @jobIds and jq.FetchedAt is not null";

        public string SqlWriteOnlyTransaction_ExpireJob = @"update HangFire.Job set ExpireAt = @expireAt where Id = @id";
        public string SqlWriteOnlyTransaction_SetJobState = @"
insert into HangFire.State (JobId, Name, Reason, CreatedAt, Data)
values (@jobId, @name, @reason, @createdAt, @data);
update HangFire.Job set StateId = SCOPE_IDENTITY(), StateName = @name where Id = @id;";
        public string SqlWriteOnlyTransaction_AddJobState = @"
insert into HangFire.State (JobId, Name, Reason, CreatedAt, Data)
values (@jobId, @name, @reason, @createdAt, @data)";

        public string SqlWriteOnlyTransaction_IncrementCounter =
            @"insert into HangFire.Counter ([Key], [Value]) values (@key, @value)";

        public string SqlWriteOnlyTransaction_IncrementCounter_expirein =
            @"insert into HangFire.Counter ([Key], [Value], [ExpireAt]) values (@key, @value, @expireAt)";

        public string SqlWriteOnlyTransaction_DecrementCounter =
            @"insert into HangFire.Counter ([Key], [Value]) values (@key, @value)";
        
        public string SqlWriteOnlyTransaction_DecrementCounter_expirein =
            @"insert into HangFire.Counter ([Key], [Value], [ExpireAt]) values (@key, @value, @expireAt)";

        public string SqlWriteOnlyTransaction_AddToSet = @"
merge HangFire.[Set] as Target
using (VALUES (@key, @value, @score)) as Source ([Key], Value, Score)
on Target.[Key] = Source.[Key] and Target.Value = Source.Value
when matched then update set Score = Source.Score
when not matched then insert ([Key], Value, Score) values (Source.[Key], Source.Value, Source.Score);";

        public string SqlWriteOnlyTransaction_RemoveFromSet =
            @"delete from HangFire.[Set] where [Key] = @key and Value = @value";

        public string SqlWriteOnlyTransaction_InsertToList =
            @"insert into HangFire.List ([Key], Value) values (@key, @value)";

        public string SqlWriteOnlyTransaction_RemoveFromList =
            @"delete from HangFire.List where [Key] = @key and Value = @value";

        public string SqlWriteOnlyTransaction_TrimList = @"
with cte as (
select row_number() over (order by Id desc) as row_num, [Key] from HangFire.List)
delete from cte where row_num not between @start and @end and [Key] = @key";

        public string SqlWriteOnlyTransaction_SetRangeInHash = @"
merge HangFire.Hash as Target
using (VALUES (@key, @field, @value)) as Source ([Key], Field, Value)
on Target.[Key] = Source.[Key] and Target.Field = Source.Field
when matched then update set Value = Source.Value
when not matched then insert ([Key], Field, Value) values (Source.[Key], Source.Field, Source.Value);";

        public string SqlWriteOnlyTransaction_RemoveHash = "delete from HangFire.Hash where [Key] = @key";

        //public string SqlFetchedJob
        //todo: continue from here
    }
}