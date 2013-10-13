@redis
Feature: Requeue of timed out jobs

Background: 
	Given a server processing the default queue

Scenario: Non-checked jobs at fail point #1 should be marked as checked
	Given a fetched job
	 When the watcher runs
	 Then it marks the job as checked

Scenario: Checked and non-timed out jobs at fail point #1 should be leaved as is
	Given a job at the fail point #1 that was checked a millisecond ago
	 When the watcher runs
	 Then the fetched jobs queue still contains the job
	  But the 'default' queue does not contain the job
	  And the job has the checked flag set
	  
	  
Scenario: Checked and timed out jobs at fail point #1 should be re-queued
	Given a job at the fail point #1 that was checked a day ago 
	 When the watcher runs
	 Then the 'default' queue contains the job
	  But the fetched jobs queue does not contain the job 
	  And the job does not have the checked flag set
	  