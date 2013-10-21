@redis
Feature: Client

Background:
    Given a client

Scenario: The client creates the job in the storage
     When I create a job
     Then the storage contains the job

Scenario: The job contains the 'Type' parameter equal to the assembly qualified type name
     When I create a job
     Then it has the following parameters:
          | Name | Value                                                      |
          | Type | <Assembly qualified name of 'HangFire.Tests.TestJob' type> |

Scenario: If the arguments were not provided, the 'Args' parameter contains the empty JSON value
     When I create an argumentless job
     Then it has the following parameters:
          | Name | Value |
          | Args | {}    |

Scenario: Arguments are converted to a JSON string and contained in the 'Args' parameter 
     When I create a job with the following arguments:
          | Name      | Value   |
          | ArticleId | 5       |
          | State     | Deleted |
     Then the job contains all of the above arguments in the JSON format

Scenario: The given state was applied to the job
     When I create a job
     Then the given state was applied to it