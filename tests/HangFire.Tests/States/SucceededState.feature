@redis
Feature: Succeeded state

Background:
    Given a job
    Given the Succeeded state

Scenario: After applying it should add the job to the succeeded list
     When I apply it
     Then the job should be added to the succeeded list

Scenario: After unapplying it should remove the job from the succeeded list
     When I apply it
      And after I unapply it
     Then the job should be removed from the succeeded list