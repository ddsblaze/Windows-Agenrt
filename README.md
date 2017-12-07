# Windows-Agent
Rudimentary and basic code for an agent to query various host features.
Currently able to: 
- Hash the DLLs loaded into each process (on disk, not in memory).
- Determine the default program to open each file extention.
- Collect statistics on processes, specifically memory usage and processor/privileged processor time.
- Collect Run and RunOnce registry values.
- Take the hash of any file copied to a usb drive (untested: properly add/remove drives as they are inserted and removed).
- Map process names to active network ports.
- Makes basic use of parallelizaiton.
- Makes basic use of locking to avoid issues with processes starting/terminating.
- Currently outputs to command line.