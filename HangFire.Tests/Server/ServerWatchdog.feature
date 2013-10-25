@redis
Feature: Server watchdog

    As an Administrator, I would like if the crashed servers 
    are automatically removed.

Background:
    Given a server watchdog

Scenario: It should not remove the active server
    Given a server that was started a day ago
      And its last heartbeat was a second ago
     When the watchdog gets the job done
     Then the server should not be removed

Scenario: It should remove the server when its last heartbeat timed out
    Given a server that was started a day ago
      And its last heartbeat was a day ago
     When the watchdog gets the job done
     Then the server should be removed

Scenario: It should not remove the recently started server with no heartbeats
    Given a server that was started a second ago
      And there are no any heartbeats
     When the watchdog gets the job done
     Then the server should not be removed

Scenario: It should remove the long-running server with no heartbeats
    Given a server that was started a day ago
      And there are no any heartbeats
     When the watchdog gets the job done
     Then the server should be removed

Scenario: It should not remove the recently started server with timed out heartbeat
    Given a server that was started a second ago
      And its last heartbeat was a day ago
     When the watchdog gets the job done
     Then the server should not be removed

Scenario: It should remove only those servers that were timed out
    Given a server 'Active' that was started a second ago
      And a server 'TimedOut' that was started a day ago
     When the watchdog gets the job done
     Then the server 'TimedOut' should be removed
      But the server 'Active' should not be removed