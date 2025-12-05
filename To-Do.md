PS C:\Users\Lance\Desktop\Playground> dotnet run metadata
Info: Starting Playground CLI (CliFx)
───────────────────────────────────────────────── Metadata Search ──────────────────────────────────────────────────Title: (Random Access Memories): 

Refa

1. Change to "Track Title" without an example (and get rid of all examples)
2. Implement accept input via CliFx natively.
3. Rewrite metadata as "music tags"
4. don't mention scrape for now
5. Supplying metadata should not default to searching but rather show options possible... or rather not supplying a subcommand should auto-invoke help
6. Add proper subcommands for search
7. Why still no resiliency: ───────────────────────────── Metadata Search ──────────────────────────────────────────────────Title: (Random Access Memories): bowie never let me down
Artist: (Daft Punk): bowie
Info: Fetching up to 10 results per source
Warning: [MusicBrainz] Retry 0/10 in 3.0s: The SSL connection could not be established, see inner exception.
Warning: No results found
PS C:\Users\Lance\Desktop\Playground> 

8. Default to 50 per call not 10.
9. You haven't asked me which service to use for getting music tags. 