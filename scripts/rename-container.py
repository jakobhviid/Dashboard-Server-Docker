#!/usr/bin/env python3

import docker
import sys

docker_client = docker.from_env()

try:
    container_id = sys.argv[1]
    new_name = sys.argv[2]
    print(container_id)
    print(new_name)
    container_to_restart = docker_client.containers.get(container_id=container_id)
    container_to_restart.rename(new_name)
except docker.errors.NotFound:
    print("Container not found")
    sys.exit(1)
except docker.errors.APIError as err:
    print(err.explanation)
    sys.exit(1)

# def run_container():
#     try:
#         docker_client.containers.run(
#             **requst_validation_result[1], detach=True)
#         return Response(json.dumps({'message': 'Container Started'}), status=200, mimetype='application/json')
#     except docker.errors.ImageNotFound:
#         return Response(json.dumps({'message': 'Image not found'}), status=400, mimetype='application/json')
#     except docker.errors.APIError as err:
#         return Response(json.dumps({'message': err.explanation}), status=err.response.status_code,
#                         mimetype='application/json')

# def start_container():
#     try:
#         container_to_start = docker_client.containers.get(
#             **requst_validation_result[1])
#         container_to_start.start()
#         return Response(json.dumps({'message': 'Container started'}), status=200, mimetype='application/json')
#     except docker.errors.NotFound:
#         return Response(json.dumps({'message': 'Container not found'}), status=404, mimetype='application/json')


# def stop_container():
#     try:
#         container_to_stop = docker_client.containers.get(
#             **requst_validation_result[1])
#         container_to_stop.stop(timeout=6)
#         return Response(json.dumps({'message': 'Container stopped'}), status=200, mimetype='application/json')
#     except docker.errors.NotFound:
#         return Response(json.dumps({'message': 'Container not found'}), status=404, mimetype='application/json')


# def remove_container():
#     try:
#         container_to_stop = docker_client.containers.get(
#             **requst_validation_result[1])
#         container_to_stop.remove(force=True)
#         return Response(json.dumps({'message': 'Container removed'}), status=200, mimetype='application/json')
#     except docker.errors.NotFound:
#         return Response(json.dumps({'message': 'Container not found'}), status=404, mimetype='application/json')


# def restart_container():
#     try:
#         container_to_restart = docker_client.containers.get(
#             **requst_validation_result[1])
#         container_to_restart.restart(timeout=6)
#         return Response(json.dumps({'message': 'Container restarted'}), status=200, mimetype='application/json')
#     except docker.errors.NotFound:
#         return Response(json.dumps({'message': 'Container not found'}), status=404, mimetype='application/json')
#     except docker.errors.APIError as err:
#         return Response(json.dumps({'message': err.explanation}), status=err.response.status_code,
#                         mimetype='application/json')


# def update_configuration():
#     try:
#         container_to_restart = docker_client.containers.get(
#             requst_validation_result[1]['container_id'])
#         del requst_validation_result[1]['container_id']
#         container_to_restart.update(**requst_validation_result[1])
#         return Response(
#             json.dumps({'message': 'Container successfully reconfigured'}), status=200, mimetype='application/json')
#     except docker.errors.NotFound:
#         return Response(json.dumps({'message': 'Container not found'}), status=404, mimetype='application/json')
#     except docker.errors.APIError as err:
#         return Response(json.dumps({'message': err.explanation}), status=err.response.status_code,
#                         mimetype='application/json')
#     except docker.errors.DockerException as err:
#         return Response(json.dumps({'message': 'Something Went Wrong'}), status=500,
#                         mimetype='application/json')
