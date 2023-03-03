# ledger-es
An exaple ledger management system built to demonstrate event sourcing and event-driven architecture.

# Getting started
Bring the docker project up from the command line by changing into the root directory of the project and running the following command:

    docker compose up -d

Once the SQL Server instance is up create the database schema by running [schema.sql](src/Js.LedgerEs/ReadModelPersistence/schema.sql) in SSMS.

Navigate to the project web interface at http://localhost:8080

# References
 1. The EventStore website has a [good overview](https://www.eventstore.com/event-sourcing) of event sourcing and how it relates to patterns such as CQRS and DD.
 2. The EventStore github [samples repository](https://github.com/EventStore/samples) is a good resource on how to integrate EventStoreDB with other databases for storing read models

# Debugging
## EventStore
The EventStore management interface runs at http://localhost:8081

## RabbitMQ
The RabbitMQ management interface runs at http://localhost:8082 with usename `user` and password `password`.

## SQL Server
The SQL Server instance can be connected to with the connection string `Data Source=localhost,1434;User Id=sa;Password=X13ppP2prRi6fPmW;`
