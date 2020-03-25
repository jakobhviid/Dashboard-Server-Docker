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
    kafka_url = os.environ.get("KAFKA_URL")
    return KafkaProducer(
        bootstrap_servers=kafka_url, value_serializer=lambda v: json.dumps(v).encode('utf-8'))
