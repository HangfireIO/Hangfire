@redis
Feature: Processing state

Background:
    Given a job
      And the Processing state

Scenario: After applying, it should add the job to the processing set
     When I apply it
     Then the processing set should contain the job
      And processing timestamp should be set to UtcNow

Scenario: After unapplying, it should remove the job from the processing set
     When I apply it
      And after I unapply it
     Then the processing set should not contain the job