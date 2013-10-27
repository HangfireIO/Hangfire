@redis
Feature: Job manager

    As a user, I expect that my enqueued jobs will be processed by the Server.

Scenario: Manager should perform a job
    Given an enqueued job
     When the manager processes the next job
     Then the job should be performed

Scenario: Successfully performed job should have the Succeeded state
    Given an enqueued job
     When the manager processes the next job
     Then its state should be Succeeded
      And the job should be removed from the dequeued list

Scenario: After performing the broken job, it should have the Failed state
    Given an enqueued broken job
     When the manager processes the next job
     Then its state should be Failed
      And the job should be removed from the dequeued list

Scenario: An unexisting job should not be processed, but it should be removed from the dequeued list
    Given the 'unexisting' job, that was enqueued
     When the manager processes the next job
     Then there should be no performing actions
      But the 'unexisting' job should be removed from the dequeued list
      
Scenario: Worker should processes only jobs in the Enqueued state, but it should remove the job from the dequeued list anyway
    Given an enqueued job
      And its state is Processing
     When the manager processes the next job
     Then the job should not be performed
      But it should be removed from the dequeued list 

Scenario: Disposable job should be disposed after processing
    Given an enqueued job
     When the manager processes the next job
     Then the job should be disposed

Scenario: Job arguments should be deserialized correctly
    Given the following job type:
          """
          public void CustomJob : BackgroundJob
          {
              public int ArticleId { get; set; }
              public string Author { get; set; }

              public override void Perform()
              {
              }
          } 
          """
      And an enqueued CustomJob with the following arguments:
          | Name      | Value  |
          | ArticleId | 2      |
          | Author    | nobody |
     When the manager processes the next job
     Then the last ArticleId should be equal to 2
      And the last Author should be equal to 'nobody'
