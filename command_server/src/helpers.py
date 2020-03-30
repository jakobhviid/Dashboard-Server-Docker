from enum import Enum


class Necessity(Enum):
    REQUIRED = 1
    OPTIONAL = 2


label = 'label'
necessity = 'necessity'

parameters_to_check = {
    'run-container': [{label: 'image', necessity: Necessity.REQUIRED},
                      {label: 'name', necessity: Necessity.REQUIRED},
                      {label: 'command', necessity: Necessity.OPTIONAL},
                      {label: 'ports', necessity: Necessity.OPTIONAL},
                      {label: 'environment', necessity: Necessity.OPTIONAL},
                      {label: 'restart_policy', necessity: Necessity.OPTIONAL}],
    'stop-container': [{label: 'container_id', necessity: Necessity.REQUIRED}],

    'start-container': [{label: 'container_id', necessity: Necessity.REQUIRED}],

    'restart-container': [{label: 'container_id', necessity: Necessity.REQUIRED}],

    'rename-container': [{label: 'container_id', necessity: Necessity.REQUIRED},
                         {label: 'name', necessity: Necessity.REQUIRED}],

    'update-container-configuration': [{label: 'container_id', necessity: Necessity.REQUIRED},
                                       {label: 'blkio_weight', necessity: Necessity.OPTIONAL},
                                       {label: 'mem_limit', necessity: Necessity.OPTIONAL},
                                       {label: 'mem_reservation', necessity: Necessity.OPTIONAL},
                                       {label: 'memswap_limit', necessity: Necessity.OPTIONAL},
                                       {label: 'kernel_memory', necessity: Necessity.OPTIONAL},
                                       {label: 'restart_policy', necessity: Necessity.OPTIONAL},
                                       {label: 'cpu_shares', necessity: Necessity.OPTIONAL},
                                       {label: 'cpu_period', necessity: Necessity.OPTIONAL},
                                       {label: 'cpu_quota', necessity: Necessity.OPTIONAL},
                                       {label: 'cpuset_cpus', necessity: Necessity.OPTIONAL},
                                       {label: 'cpuset_mems', necessity: Necessity.OPTIONAL}, ]
}


# Return +1 if every parameter is present, returns 0 if method can't be checked, return -1 if a required parameter is
# not present, or if a parameter is of wrong type

def check_request_body(request_body, method):
    if method not in parameters_to_check:
        return 0
    parameters_present = {}
    for parameter in parameters_to_check[method]:
        if parameter[necessity] is Necessity.REQUIRED and \
                (parameter[label] not in request_body or request_body[parameter[label]] == ''
                 or request_body[parameter[label]] is None):
            return -1, 'Invalid arguments. Parameter \'' + parameter[label] + '\' required!'
        elif parameter[label] in request_body:
            parameters_present[parameter[label]] = request_body[parameter[label]]
    return 1, parameters_present
