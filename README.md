# ledger-es
An exaple ledger management system built to demonstrate event sourcing and event-driven architecture.

# Getting started
Bring the docker project up from the command line by changing into the root directory of the project and running the following command:

    docker compose up -d

Navigate to the project web interface at http://localhost:8080

# Debugging
## EventStore
The EventStore management interface runs at http://localhost:8081

## RabbitMQ
The RabbitMQ management interface runs at http://localhost:8082 with usename `user` and password `password`.
