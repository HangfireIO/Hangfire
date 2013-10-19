@redis
Feature: Succeeded state

Background:
    Given a job
    Given the Succeeded state

Scenario: State name is 'Succeeded'
	 Then the state name is equal to 'Succeeded'

Scenario: After applying it should expire the job data
     When I apply it
	 Then it should expire the job

Scenario: After applying it should change the stats
     When I apply it
	 Then it should increase the succeeded counter

Scenario: After applying it should add the job to the succeeded list
     When I apply it
	 Then the job should be added to the succeeded list

Scenario: After unapplying it should persist the job data
     When I apply it
	  And after I unapply it
	 Then it should persist the job

Scenario: After unapplying it should change the stats
     When I apply it
	  And after I unapply it
	 Then it should decrease the succeeded counter

Scenario: After unapplying it should remove the job from the succeeded list
     When I apply it
	  And after I unapply it
	 Then the job should be removed from the succeeded list