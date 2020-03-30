import docker
import os
import sys
import time
from kafka import errors
from classes import container_overview_info
from classes import container_stats_info
import helpers
import json

docker_client = docker.from_env()
servername = os.environ.get('SERVER_NAME')

containers_overview = {
    "servername": servername,
    "containers": []
}

containers_stats = {
    "servername": servername,
    "containers": []
}


def serialize(obj):
    """JSON serializer for objects not serializable by default json code"""
    return obj.__dict__


def send_overview_data_to_kafka(producer, topic):
    # TODO Figure out your own ID so you don't send information about yourself
    all_containers = docker_client.containers.list(all)

    for c in all_containers:
        # Retrieve general overview data from the container
        container_overview_data = container_overview_info.ContainerOverviewInfo(
            c.short_id, c.name)

        container_overview_data.with_image_tags(c.image.tags)

        container_overview_data.with_state(c.attrs['State'])

        container_overview_data.with_creation_time(c.attrs['Created'])

        helpers.replace_or_add_container_in_list(
            containers_overview['containers'], container_overview_data)

    producer.send(topic, json.dumps(containers_overview, default=serialize))


def send_stats_data_to_kafka(producer, topic):
    running_containers = docker_client.containers.list()
    for i, previous_container in enumerate(containers_stats['containers']):
        container_still_running = False
        for c in running_containers:
            if previous_container.id == c.short_id:
                container_still_running = True
                break
        if not container_still_running:
            containers_stats['containers'].pop(i)

    for c in running_containers:
        # Retrieve stats data from the container
        container_stats_data = container_stats_info.ContainerStatsInfo(
            c.short_id, c.name)
        container_stats = c.stats(stream=False)

        previous_container = None
        for container in containers_stats['containers']:
            if container.id == c.short_id:
                previous_container = container

        container_stats_data.with_cpu(
            container_stats['cpu_stats']['cpu_usage']['total_usage'],
            len(container_stats['cpu_stats']['cpu_usage']['percpu_usage']),
            container_stats['cpu_stats']['system_cpu_usage'], previous_container)

        container_stats_data.with_memory(
            container_stats['memory_stats']['limit'], container_stats['memory_stats']['usage'])

        # RX Bytes = total number of bytes recieved over a network interface
        # TX Bytes = total number of bytes transmitted over a network interface
        total_rx_bytes = 0
        total_tx_bytes = 0
        for network in container_stats['networks']:
            total_rx_bytes = total_rx_bytes + \
                container_stats['networks'][network]['rx_bytes']
            total_tx_bytes = total_tx_bytes + \
                container_stats['networks'][network]['tx_bytes']

        container_stats_data.with_net_i_o(total_rx_bytes, total_tx_bytes)

        total_disk_read_bytes = 0
        total_disk_write_bytes = 0
        for io_operation in container_stats['blkio_stats']['io_service_bytes_recursive']:
            operation_type = io_operation['op']
            operation_value_bytes = io_operation['value']
            if operation_type == 'Read':
                total_disk_read_bytes = total_disk_read_bytes + operation_value_bytes
            elif operation_type == 'Write':
                total_disk_write_bytes = total_disk_write_bytes + operation_value_bytes

        container_stats_data.with_disk_i_o(
            total_disk_read_bytes, total_disk_write_bytes)

        helpers.replace_or_add_container_in_list(
            containers_stats['containers'], container_stats_data)

    producer.send(topic, json.dumps(containers_stats, default=serialize))


try:
    producer = helpers.create_producer()

    # The script requires one argument when run - how often data should be send
    interval_delay = int(sys.argv[1])
    overview_topic = 'general_info'
    stats_topic = "stats_info"
    while True:
        send_overview_data_to_kafka(producer, overview_topic)
        send_stats_data_to_kafka(producer, stats_topic)
        time.sleep(interval_delay)

except errors.NoBrokersAvailable:
    print('No brokers available')
    sys.exit(100)
except ValueError:
    print('Invalid interval delay argument!')
    sys.exit(1)
except errors.KafkaTimeoutError:
    print('Could not connect to Kafka')
    sys.exit(1)
