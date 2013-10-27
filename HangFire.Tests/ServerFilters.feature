@redis
Feature: Server filters

Scenario: Server filters should be executed when the worker performs the job
    Given an enqueued job
      And a server filter 'test'
     When the manager processes the next job
     Then the server filter methods should be executed in the following order:
          | Method             |
          | test::OnPerforming |
          | test::OnPerformed  |
      And the job should be performed

Scenario: Multiple server filters should be executed depending on their order
    Given an enqueued job
      And a server filter 'first'
      And a server filter 'second'
     When the manager processes the next job
     Then the server filter methods should be executed in the following order:
          | Method               |
          | first::OnPerforming  |
          | second::OnPerforming |
          | second::OnPerformed  |
          | first::OnPerformed   |

Scenario: I should be able to set a job parameter in the server filter
    Given an enqueued job
      And the server filter 'first' that sets the following parameters:
          | Name      | Value |
          | Culture   | en-US |
          | UICulture | ru-RU |
     When the manager processes the next job
     Then the job should have all of the above parameters encoded as JSON string
      And the job should be performed

Scenario: When I specify an empty or null parameter name, an exception should be thrown
    Given an enqueued job
      And the server filter 'first' that sets the following parameters:
          | Name | Value |
          |      | en-US |
     When the manager processes the next job
     Then the job should be performed

Scenario: Server filter should be able to read the job parameters
    Given an enqueued job
      And the server filter 'first' that sets the following parameters:
          | Name    | Value |
          | Culture | en-GB |
      And the server filter 'second' that reads all of the above parameters
     When the manager processes the next job
     Then the 'second' server filter got the actual values of the parameters
      And the job should be performed

Scenario: When I specify an empty or null parameter name in the GetParameter method call, an exception should be thrown
    Given an enqueued job
      And the server filter 'first' that gets the following parameters:
          | Name | Value |
          |      | en-GB |
     When the manager processes the next job
     Then the job should not be performed

Scenario: Server filter should be able to cancel the performing of a job
    Given an enqueued job
      And a server filter 'first'
      And a server filter 'second' that cancels the performing
      And a server filter 'third'
     When the manager processes the next job
     Then the job should not be performed
      And only the following server filter methods should be executed:
          | Method                                          |
          | first::OnPerforming                             |
          | second::OnPerforming                            |
          | first::OnPerformed (with the canceled flag set) |

Scenario: Server filter's OnPerformed could be skipped if there was an exception
    Given an enqueued job
      And a server filter 'first'
      And a server filter 'second' that throws an exception
     When the manager processes the next job
     Then only the following server filter methods should be executed:
          | Method               |
          | first::OnPerforming  |
          | second::OnPerforming |
          | first::OnPerformed   |
      And the state of the job should be Failed

Scenario: Server filter can handle the exception
    Given an enqueued job
      And a server filter 'first'
      And a server filter 'second' that handles an exception
      And a server filter 'third' that throws an exception
     When the manager processes the next job
     Then the server filter methods should be executed in the following order:
          | Method               |
          | first::OnPerforming  |
          | second::OnPerforming |
          | third::OnPerforming  |
          | second::OnPerformed  |
          | first::OnPerformed   |
      And the state of the job should be Succeeded

Scenario: Server exception filters are executed when there was an exception while performing a job
    Given an enqueued broken job
      And a server exception filter 'test'
     When the manager processes the next job
     Then the server exception filter should be executed
      And the state of the job should be Failed

Scenario: Multiple server exception filters are executed depending on their order
    Given an enqueued broken job
      And a server exception filter 'first'
      And a server exception filter 'second'
     When the manager processes the next job
     Then the server exception filters should be executed in the following order:
          | Filter |
          | first  |
          | second |
      And the state of the job should be Failed

Scenario: Server exception filter can handle the exception
    Given an enqueued broken job
      And a server exception filter 'first'
      And a server exception filter 'second' that handles an exception
      And a server exception filter 'third'
     When the manager processes the next job
     Then the following server exception filters should be executed:
          | Filter |
          | first  |
          | second |
          | third  |
      And the state of the job should be Succeeded
