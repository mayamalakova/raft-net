Example
1) Run Program as cmd
`raft add leader lead1 --port 5001`
Will produce prompt "Listening for commands"
2) Run Program as cmd
`raft add follower foll1 --port 5002 --leader localhost:5001`
Will produce prompt "Ready to send message"
3) Run Program as cmd
`raft add follower foll2 --port 5003 --leader localhost:5001`
Will produce prompt "Ready to send message"
4) Run Program as cmd
`raft add follower foll3 --port 5004 --leader localhost:5001`
Will produce prompt "Ready to send message"
5) On foll1 run `raft "Hello"`
6) foll1 will send the message to lead1
7) lead1 will print `Received message "Hello"` and send the `ack "Hello"` message to all followers
8) all followers will print `"Hello" confirmed by leader`