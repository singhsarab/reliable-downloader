# Questions

## How did you approach solving the problem?
As we needed to have reliable downloads and we are fine waiting for when the connection is valid, I tried to implement a retry logic on top of the existing web calls so that if any of the calls fail we can retry.
I try 2 calls after 2 seconds each and then change the wait time to 2 mins as described in the spec.

Then I implemented the individual download functionality for both the cases
1. When we download partially i.e. when the AcceptRanges contains "Bytes"
2. When we download the full

I also added check for the file of it exists already.

Since we did not get a chance to clarify the spec. I made some assumptions and added the code for that.
1. I skip if it's already downloaded
2. I resume the download if it was previous cancelled in case of partial download only.

Since I wanted to show progress for download full scenario as well I added the call for logging progress for that as well.
I added some progress indication, but it updates too frequently for now. I'd rather not writelines but replace the console line.

## How did you verify your solution works correctly?
I reduced the batchsize that I was using, it made the download slower as it would require more web requests. And then I switched my network off and on to verify that the download resumes.

## How long did you spend on the exercise?
I'd say time spent would be as follows
  1. 2 hours for validating my code works end to end. 
  2. 30 mins for progress related code, I had to test that code multiple times because I wanted to include resume and wanted the remaining time estimations to be accurate.
  3. And then 20 mins to beautify the code.
  4. And 10 mins for MD5 check.

## What would you add if you had more time and how?
1. I haven't written a lot of unit tests for the exercise yet. It might take up some more time as I'd I have to refactor some of mode and mock some of the file interactions and actual API calls.
2. I'd make the progress indicator look more appealing 
3. I'd also add some code for taking in Url and localFilePath as arguments for the app. 
    a. Probably allow multiple url and a download directory as arguments so we can run downloads in parallel.


