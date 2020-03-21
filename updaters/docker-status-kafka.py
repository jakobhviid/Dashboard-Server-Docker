import docker
import os
import sys
import time
from kafka import KafkaProducer
from kafka import errors
import json

docker_client = docker.from_env()


class ContainerInfo:
    def __init__(self, container_id, name):
        self.container_id = container_id
        self.name = name

    def with_image_tags(self, image_tags):
        self.image_tags = image_tags
        return self

    def with_state(self, state):
        status = state['Status']

        startTime = state['StartedAt']
        finishTime = state['FinishedAt']

        self.state = {
            'status': status, 'startTime': startTime, 'finishTime': finishTime
        }

        return self

    def with_creation_time(self, created):
        self.creation_time = created
        return self


def send_data_to_kafka(producer, topic):
    containers = docker_client.containers.list(all, json)

    # TODO Figure out your own ID so you don't send information about yourself
    for container in containers:
        relevant_data = ContainerInfo(
            container.short_id, container.name)

        if container.image.tags:
            relevant_data.with_image_tags(container.image.tags)

        relevant_data.with_state(container.attrs['State'])

        producer.send(topic, json.dumps(relevant_data.__dict__))


def create_producer():
    # kafka_url = os.environ.get("KAFKA_URL")
    kafka_url = 'kafka2.cfei.dk:9093'
    return KafkaProducer(
        bootstrap_servers=kafka_url, value_serializer=lambda v: json.dumps(v).encode('utf-8'))


try:
    producer = create_producer()
    # The script requires one argument when run - how often data should be send
    # interval_delay = int(sys.argv[1])
    # kafka_topic = os.environ.get("KAFKA_TOPIC")
    kafka_topic = 'test'
    while True:
        send_data_to_kafka(producer, kafka_topic)
        time.sleep(5)

except errors.NoBrokersAvailable:
    print('No brokers available')
    sys.exit(100)
except ValueError:
    print('Invalid interval delay argument!')
    sys.exit(1)
except errors.KafkaTimeoutError:
    print('Could not connect to Kafka')
    sys.exit(1)
