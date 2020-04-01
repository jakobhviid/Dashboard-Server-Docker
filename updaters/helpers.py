#!/usr/bin/env python3

from kafka import KafkaProducer
import os
import json


def replace_or_add_container_in_list(containers, container_to_add):
    container_found = False
    for i, container in enumerate(containers):
        if container.id == container_to_add.id:
            # replacing container if it already exists with updated values
            containers[i] = container_to_add
            container_found = True
            break
    if not container_found:
        containers.append(container_to_add)
    return containers


def create_producer():
    # kafka_urls = os.environ.get("KAFKA_URLS")
    kafka_urls = "kafka2.cfei.dk:9092,kafka3.cfei.dk:9092"
    kafka_urls = kafka_urls.split(",")
    print(kafka_urls)
    return KafkaProducer(
        bootstrap_servers=kafka_urls, value_serializer=lambda v: json.dumps(v).encode('utf-8'))
