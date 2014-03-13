@redis
Feature: Server

Scenario: When a server starts, it should add itself to the servers set
     When the 'Test' server starts
     Then the servers set should contain the 'Test' server

Scenario: When a server starts, it should publicate its properties
     When the 'Test' server starts with 5 workers
     Then the 'Test' server's properties should contain the following items:
          | Name        | Value              |
          | WorkerCount | 5                  |
          | StartedAt   | <UtcNow timestamp> |

Scenario: When a server starts, it should publicate its queues
     When the 'Test' server starts with the queues critical, high
     Then the 'Test' server's queues list should contain queues critical, high

Scenario: When a server shuts down, it should remove itself from the servers set
     When the 'Test' server shuts down
     Then the servers set should not contain the 'Test' server

Scenario: When a server shuts down, it should clear its properties list
     When the 'Test' server shuts down
     Then the storage should not contain an entry for the 'Test' server properties

Scenario: When a server shuts down, it should clear its queues
     When the 'Test' server shuts down
     Then the storage should not contain an entry for the 'Test' server queues