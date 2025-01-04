A command line utility which allows administrating a raft node.
First start the util by running from the command line:
`Raft.Admin.exe -a localhost:5002`
Then you can run any of the admin commands and that will be executed on the localhost:5002 raft node:
- `ping` - Add file contents to the index.
- `info` - Get informatin about the current node.
- `command` - Update the raft state machine state. Takes -v variable, -o operand, -l literal e.g `command -v A -o = -l 1`
- `log-info` - Get information about the log of the current node.
- `get-state` - Get the current state of the node's state machine
- `disconnect` - Disconnect from the cluster.
- `reconnect` - Reconnect to the cluster.

Quit by entring `q` or `quit` or `exit`.
