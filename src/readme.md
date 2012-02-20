#What is HaywireMQ
HaywireMQ is an attempt to create a high performance [Message Queue](http://en.wikipedia.org/wiki/Message_queue].

#Ideas
* Use ZeroMQ for high performance communications. 
* Use Cassandra for the message log. Cassandra has optimized write performance. Reliable message queues are often disk I/O bound.