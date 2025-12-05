1. Check for methods whilst ensuring that their unique identifier is retained (in MusicBrainz and Discogs) and part of the model used in repo (I guess? I am not sure how you're returning the info):
    - a. Fetching an artist
    - b. Fetching an album (including all tracks on it)
        - c. Detailed metadata if the distinction exists for the album and all tracks
    - d. Fetching a track (get detailed/advanced metadata always)
    - e. Tabulation of search results using Spectre with 4 columns:
        - Artist Name
        - Album Name
        - Track Title (If applicable -- not in album search or artist search obviously)
        - Duration
        - Release Year (always focus on recording year and not remaster/reissue year)
    - f. Being able to also get other info like the comments or notes in certain Discogs releases.
    - g. Being able to optionally specify label for releases
    - h. Ability to filter out results based on genre
    - i. Ability to sort results based on release date, popularity, duration, etc.
2. Centralize resiliency by employing sempahore (mostly because these are free APIs so they won't allow concurrency) + Polly (for transient errors) + any other featurs/libraries/methods... Use that centralized resiliency operator for all API/network calls in all methods in all class and all services (check all services/musicmetadataservices)
2b. Ensure values are centralized too - always retries 10, gaps of 3 seconds each time, exponential increase
3. Assess potential of creating a new directory called Utilities to hold Spectre and Resiliency (and assess if any others fit the bill)
3b. Assess Exceute being split into 3 different wrapper methods as opposed to 1
4. Bit confused by Models/records.cs... Is it a remnant from older design philosophy or intended to be a central repo for all records? If so then why are records present elsewhere? If that too is intentional, explain rationale for having some records inside Models and some not

Furthermore, if you have one Models folder how do you prevent things beceoming cluttered with no clear separation as more services are added... YT, Google Sheets, etc.
5. Task 7 was misunderstood - I meant what is the optimal way of designing so that you can supply either artist name, album name, track name or combination of two (the way one can search inside Spotify) without getting into a mess of nested ifs/overloads.
6. What is the best way to handle exceptions when calling APIs? Should we just throw them up the stack or should we catch them at each level and return nulls instead?
7. Assess resiliency of codebase against network failures, timeout, etc.
8. Add unit tests for all methods and also add integration tests where possible
9. Suggest best way to cache data locally in general... Not merely this... I meant you typically get a repsonse in JSON? So how would you go about caching that response locally when you are querying the API in bulk and end up with thousands of records (you convert them to models before pushing them -- no cache by default)
10. Easiest way of handling state management and protecting oneself against race conditions, power failures, terminal crashing and similar issues so that a file is not corrupted
11. More tests for you after Spectre is implemented for printing results properly onto screen:
a. Artist: Karajan, Album: Dvorak's Symphony No. 9, Label: EMI
b. Artist: Rozhdestvensky, Album: Schnittke Concerto Grosso No. 1
c. Artist: Barenboim, Album: Symphonies 1-9, Year: 2014
d. Track Name: "'87 and Cry'"

(And create more tests to cover all possible permutations...)
12. Refactor to get rid of models/records if it is duplicate code
13. I hope tests are self-contained for now to prevent externalities causing tests to fail
14. I have no idea of setup/teardown... Do we need to worry about that? Have you already implemented it? Is it really necessary to get into all that?
15. IS current exception handling in need of improvement?
16. Assess project that call remote APIs for resiliency improvements - for implementing Polly/Semaphore/Delay/Retry/etc.
17. Explain to me the difference between these 4 (if they are libraries)
18. Using discretion apply all needed services for maximal resiliency across project
19. I just meant making http requests... Not sure if that is JSON. How would one store the reply returned?
20. No SQL - find alternative OR tell me scenarios in which not using a database becomes untenable (in personal settings -- not enterprise)