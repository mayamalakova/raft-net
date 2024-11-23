Example
1) To initialize the cluster add a leader by running 
`raft.node add --role leader --name lead1 --port 5001`
2) To add a follower run 
`raft.node add --role follower --name fol1 --port 5002 -c localhost:5001`
where the -c option is the address of any node already in the cluster
3) To run commands on any node of the cluster:
- run `raft.cli -a localhost:5002` to initialize the raft client 
- when prompted run raft client commands:
- `ping` get a response from the node
- `info` get info about the node - role, name, address, leader address