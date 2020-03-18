#!/usr/bin/python3

import docker
import os
from kafka import KafkaProducer

client = docker.from_env()
containers = client.containers.list(all)

if os.environ.get("DATA_UPLOAD") == "kafka":
    kafka_url = os.environ.get("KAFKA_URL")
    kafka_topic = os.environ.get("KAFKA_TOPIC")
    producer = KafkaProducer(bootst)

