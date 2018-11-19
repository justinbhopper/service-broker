use master;

if exists (select * from sys.databases where name = 'SbTest')
	drop database SbTest;
go

create database SbTest;
go

alter database SbTest set enable_broker with rollback immediate;
go

alter authorization on database::SbTest to sa;
go

use SbTest;

create table Client (
	ClientId int not null identity,
	ClientKey uniqueidentifier not null,
	FirstName varchar(50) not null,
	LastName varchar(50) not null,
	constraint PK_Client primary key (ClientId)
)
go

create message type SyncRequest validation = none;

create contract SyncContract authorization dbo
(
	SyncRequest sent by initiator
);

create queue SyncQueue_init;
create queue SyncQueue_receive;

create service SyncService_init on queue SyncQueue_init (SyncContract);
create service SyncService_receive on queue SyncQueue_receive (SyncContract);
go

create or alter procedure spx_SendSyncRequest
	@request nvarchar(max) 
as
begin
	set nocount on;

	declare @handle uniqueidentifier

	begin tran

	begin dialog conversation @handle 
		from service SyncService_init
		to service 'SyncService_receive'
		on contract SyncContract
		with encryption = off;

	send on conversation @handle message type SyncRequest (@request)

	commit tran
end
go

create or alter procedure spx_ReceiveSyncRequest
	@timeoutMs int = 100
as
begin
	set nocount on;

	declare 
		@handle uniqueidentifier,
		@body varbinary(max)

	begin tran

	waitfor (
		receive top (1)
			@handle = conversation_handle,
			@body = message_body
		from
			SyncQueue_receive
	), timeout @timeoutMs;

	if (@handle is not null)
	begin
		end conversation @handle;
		
		select 
			@handle as ConversationHandle, 
			@body as Body
	end

	commit tran
end
go

create or alter procedure spx_TruncateClients
as
begin
	set nocount on;

	truncate table Client;
end
go

create or alter procedure spx_FlushSyncRequests
as
begin
	set nocount on;

	declare 
		@id uniqueidentifier,
		@handle uniqueidentifier

	while(1=1)
	begin
		waitfor (
			receive top (1)
				@id = conversation_group_id
			from SyncQueue_receive
		), timeout 1000;
		
		if (@@rowcount = 0)
		begin
			break;
		end
	end

	declare conv cursor for select conversation_handle from sys.conversation_endpoints

	open conv
	fetch NEXT FROM conv into @handle

	while @@FETCH_STATUS = 0
	begin
		end conversation @handle with cleanup
		fetch next from conv into @handle
	end

	close conv
	deallocate conv
end
go

create or alter procedure spx_InsertClient
	@clientKey uniqueidentifier,
	@firstName varchar(50),
	@lastName varchar(50)
as
begin
	insert into Client (ClientKey, FirstName, LastName) values (@clientKey, @firstName, @lastName)
end
go

create or alter procedure spx_UpdateClient
	@clientId int,
	@firstName varchar(50)
as
begin
	update Client set FirstName = @firstName where ClientId = @clientId
end
go

create or alter procedure spx_DeleteClient
	@clientId int
as
begin
	delete from Client where ClientId = @clientId
end
go

create or alter trigger trg_Client_ForInsertUpdate on Client for update
as
	declare @request nvarchar(max) = (
		select
		'Client' as 'TableName',
		COLUMNS_UPDATED() as 'UpdatedColumns',
		(
			select ClientId as 'Id'
			from inserted 
			for json path
		) as Updated
		for json path
	)

	exec spx_SendSyncRequest @request
go

create or alter trigger trg_Client_ForInsertDelete on Client for insert, delete
as
	declare @request nvarchar(max) = (
		select
		'Client' as 'TableName',
		(
			select ClientId as 'Id'
			from inserted 
			for json path
		) as Inserted,
		(
			select ClientId as 'Id'
			from deleted 
			for json path
		) as Deleted
		for json path
	)

	exec spx_SendSyncRequest @request
go

if exists (select loginname from master.sys.syslogins where name = 'SbUser')
	drop login SbUser
go

create login SbUser with password = 'SbUser', check_policy = off, check_expiration = off
go

create user SbUser for login SbUser
go

exec sp_addrolemember 'db_owner', 'SbUser'
go

use master;