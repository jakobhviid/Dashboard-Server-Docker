#!/usr/bin/python3

from app import app


@app.route('/stop-container', methods=['GET'])
def stop_container():
    return "Container Stopped! TODO"


if __name__ == "__main__":
    # Listen on any connections for containerization
    app.run(host='0.0.0.0')
