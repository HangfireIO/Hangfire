@redis
Feature: JobClient

    A a user, I expect that my jobs are created and the initial state
    is applied to them.

Background:
    Given a client

Scenario: The client creates should create a job in the storage
     When I create a job
     Then the storage should contain the job

Scenario: The job should contain the 'Type' parameter that is equal to the assembly qualified type name
     When I create a job
     Then it should have the following parameters:
          | Name | Value                                                      |
          | Type | <Assembly qualified name of 'HangFire.Tests.TestJob' type> |

Scenario: If the arguments were not provided, the 'Args' parameter should contain the empty JSON value
     When I create an argumentless job
     Then it should have the following parameters:
          | Name | Value |
          | Args | {}    |

Scenario: Arguments should be converted to a JSON string and contained in the 'Args' parameter 
     When I create a job with the following arguments:
          | Name      | Value   |
          | ArticleId | 5       |
          | State     | Deleted |
     Then the job should contain all of the above arguments in the JSON format

Scenario: The specified state should be applied to the job
     When I create a job
     Then the given state should be applied to it

Scenario: Creating a job with an empty id should cause an exception
     When I create a job with an empty id
     Then a 'System.ArgumentNullException' should be thrown by the client

Scenario: Creating a job with an empty type should cause an exception
     When I create a job with null type
     Then a 'System.ArgumentNullException' should be thrown by the client

Scenario: Creating a job with the type, that is not derived from the 'BackgroundJob' should cause an exception
     When I create a job with the incorrect type
     Then a 'System.ArgumentException' should be thrown by the client

Scenario: Creating a job with an empty state should cause an exception
     When I create a job with an empty state
     Then a 'System.ArgumentNullException' should be thrown by the client

Scenario: Creating a job with a null dictionary as arguments should cause an exception
     When I create a job with a null dictionary as arguments
     Then a 'System.ArgumentNullException' should be thrown by the client