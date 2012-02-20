#What is HaywireMQ
HaywireMQ is an attempt to create a high performance [Message Queue](http://en.wikipedia.org/wiki/Message_queue).

#Ideas
* Use [ZeroMQ](http://www.zeromq.org) for high performance communications. 
* Use [Cassandra](http://cassandra.apache.org) for the message log. Cassandra has optimized write performance. Reliable message queues are often disk I/O bound.