@redis
Feature: Failed state

Background: 
    Given a job
      And the Failed state

Scenario: After applying it should add the job to the failed set
     When I apply it
     Then the job should be added to the failed set

Scenario: After unapplying it should remove the job from the failed set
     When I apply it
      And after I unapply it
     Then the job should be removed from the failed set