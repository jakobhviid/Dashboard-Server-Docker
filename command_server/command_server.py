#!/usr/bin/env python3

from app import app
from flask import request
from flask import Response
from flask import jsonify
import helpers
import docker
import json

docker_client = docker.from_env()


def check_request(method_name):
    json_dict = request.json
    request_body_result = helpers.check_request_body(
        json_dict, method_name)
    request_body_valid = request_body_result[0]
    if request_body_valid == -1:
        return -1, Response(json.dumps({'message': request_body_result[1]}), status=400, mimetype='application/json')
    else:
        return 1, request_body_result[1]


@app.route('/run-container', methods=['POST'])
def run_container():
    requst_validation_result = check_request('run-container')
    if requst_validation_result[0] == -1:
        return requst_validation_result[1]
    try:
        docker_client.containers.run(
            **requst_validation_result[1], detach=True)
        return Response(json.dumps({'message': 'Container Started'}), status=200, mimetype='application/json')
    except docker.errors.ImageNotFound:
        return Response(json.dumps({'message': 'Image not found'}), status=400, mimetype='application/json')
    except docker.errors.APIError as err:
        return Response(json.dumps({'message': err.explanation}), status=err.response.status_code,
                        mimetype='application/json')


@app.route('/stop-container', methods=['POST'])
def stop_container():
    requst_validation_result = check_request('stop-container')
    if requst_validation_result[0] == -1:
        return requst_validation_result[1]
    try:
        container_to_stop = docker_client.containers.get(
            **requst_validation_result[1])
        container_to_stop.stop(timeout=6)
        return Response(json.dumps({'message': 'Container stopped'}), status=200, mimetype='application/json')
    except docker.errors.NotFound:
        return Response(json.dumps({'message': 'Container not found'}), status=404, mimetype='application/json')


@app.route('/remove-container', methods=['POST'])
def remove_container():
    requst_validation_result = check_request('remove-container')
    if requst_validation_result[0] == -1:
        return requst_validation_result[1]
    try:
        container_to_stop = docker_client.containers.get(
            **requst_validation_result[1])
        container_to_stop.remove(force=True)
        return Response(json.dumps({'message': 'Container removed'}), status=200, mimetype='application/json')
    except docker.errors.NotFound:
        return Response(json.dumps({'message': 'Container not found'}), status=404, mimetype='application/json')


@app.route('/start-container', methods=['POST'])
def start_container():
    requst_validation_result = check_request('start-container')
    if requst_validation_result[0] == -1:
        return requst_validation_result[1]

    try:
        container_to_start = docker_client.containers.get(
            **requst_validation_result[1])
        container_to_start.start()
        return Response(json.dumps({'message': 'Container started'}), status=200, mimetype='application/json')
    except docker.errors.NotFound:
        return Response(json.dumps({'message': 'Container not found'}), status=404, mimetype='application/json')


@app.route('/restart-container', methods=['POST'])
def restart_container():
    requst_validation_result = check_request('restart-container')
    if requst_validation_result[0] == -1:
        return requst_validation_result[1]

    try:
        container_to_restart = docker_client.containers.get(
            **requst_validation_result[1])
        container_to_restart.restart(timeout=6)
        return Response(json.dumps({'message': 'Container restarted'}), status=200, mimetype='application/json')
    except docker.errors.NotFound:
        return Response(json.dumps({'message': 'Container not found'}), status=404, mimetype='application/json')
    except docker.errors.APIError as err:
        return Response(json.dumps({'message': err.explanation}), status=err.response.status_code,
                        mimetype='application/json')


@app.route('/rename-container', methods=['POST'])
def rename_container():
    requst_validation_result = check_request('rename-container')
    if requst_validation_result[0] == -1:
        return requst_validation_result[1]

    try:
        container_to_restart = docker_client.containers.get(
            requst_validation_result[1]['container_id'])
        container_to_restart.rename(requst_validation_result[1]['name'])
        return Response(json.dumps({'message': 'Container successfully renamed to ' + requst_validation_result[1]['name']}),
                        status=200, mimetype='application/json')
    except docker.errors.NotFound:
        return Response(json.dumps({'message': 'Container not found'}), status=404, mimetype='application/json')
    except docker.errors.APIError as err:
        return Response(json.dumps({'message': err.explanation}), status=err.response.status_code,
                        mimetype='application/json')


@app.route('/update-container-configuration', methods=['POST'])
def update_configuration():
    requst_validation_result = check_request('update-container-configuration')
    if requst_validation_result[0] == -1:
        return requst_validation_result[1]

    try:
        container_to_restart = docker_client.containers.get(
            requst_validation_result[1]['container_id'])
        del requst_validation_result[1]['container_id']
        container_to_restart.update(**requst_validation_result[1])
        return Response(
            json.dumps({'message': 'Container successfully reconfigured'}), status=200, mimetype='application/json')
    except docker.errors.NotFound:
        return Response(json.dumps({'message': 'Container not found'}), status=404, mimetype='application/json')
    except docker.errors.APIError as err:
        return Response(json.dumps({'message': err.explanation}), status=err.response.status_code,
                        mimetype='application/json')


if __name__ == "__main__":
    # Listen on any connections for containerization
    app.run(host='0.0.0.0', debug=True)
