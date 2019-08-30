IF NOT EXISTS (SELECT * FROM sys.sysusers WHERE [name] = 'SANNETAdmin')
BEGIN
	PRINT 'Creating "SANNETAdmin" user"..'

	CREATE USER [SANNETAdmin] FOR LOGIN [SANNETAdmin] WITH DEFAULT_SCHEMA=[dbo]
	GRANT SELECT, INSERT, UPDATE, DELETE, EXECUTE TO [SANNETAdmin]
END
ELSE
	PRINT 'The user "SANNETAdmin" already exists..'
