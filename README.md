# SimpleSqlMigrate
Simple sql server db migrator

This is a simple database migrator tool which allows you to migrate  your schema/data changes. I use this tool for all my db migrations for [teambins](http://www.teambins.com/dashboard/teambinsdev)

You can run the console program and provides the directory where your migration scripts are located and the connection string to connect to your database.

The tool will create a table called `__DbMigrations` in your database and stores the history of all your migrations.

Scripts will be executed in the order of their names. So name it appropriately (Ex : 0001-CreateUserTable.sql, 0002-AddCreatedDateToUserTable.sql)

All the scripts will be executed in a single transaction. So if one script has bad content, nothing will be commited.
