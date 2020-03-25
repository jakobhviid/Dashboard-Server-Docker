
def calculate_cpu_percent(container_id, container_cpu_usage, num_of_cpu, system_cpu_usage, previous_container):
    if previous_container is None:  # The first time the containers stats is being collected it's not possible to calculate how much cpu usage it has
        return 0

    previous_cpu_usage = previous_container.cpu_usage
    previous_system_cpu_usage = previous_container.system_cpu_usage

    container_cpu_delta = container_cpu_usage - previous_cpu_usage
    system_cpu_usage_delta = system_cpu_usage - previous_system_cpu_usage

    cpu_percent = ((container_cpu_delta / system_cpu_usage_delta)
                   * num_of_cpu) * 100

    return round(cpu_percent, 2)


kilobyte_size = 1000
megabyte_size = kilobyte_size*kilobyte_size
gigabyte_size = megabyte_size * kilobyte_size


def calculate_appropiate_byte_type(bytes):
    num_digits = len(str(bytes))
    # kilobytes
    if num_digits <= len(str(kilobyte_size)) + 1:
        return str(round((bytes / kilobyte_size), 1)) + " KB"
    elif num_digits <= len(str(megabyte_size)) + 1:
        return str(round((bytes / megabyte_size), 1)) + " MB"
    elif num_digits <= len(str(gigabyte_size)) + 1:
        return str(round((bytes / gigabyte_size), 1)) + "GB"


class ContainerStatsInfo:
    def __init__(self, container_id, name):
        self.id = container_id
        self.name = name

    def with_cpu(self, total_cpu_usage, num_of_cpu, system_cpu_usage, previous_container):
        self.cpu_usage = total_cpu_usage
        self.num_of_cpu = num_of_cpu
        self.system_cpu_usage = system_cpu_usage
        self.cpu_percentage = calculate_cpu_percent(self.id,
                                                    total_cpu_usage, num_of_cpu, system_cpu_usage, previous_container)

    def with_memory(self, memory_limit, memory_usage):
        memory_percent = (memory_usage / memory_limit) * 100
        self.memory_percent = round(memory_percent, 2)

    def with_net_i_o(self, input_bytes, output_bytes):
        input = calculate_appropiate_byte_type(input_bytes)
        output = calculate_appropiate_byte_type(output_bytes)
        self.net = input + " / " + output

    def with_disk_i_o(self, input_bytes, output_bytes):
        input = calculate_appropiate_byte_type(input_bytes)
        output = calculate_appropiate_byte_type(output_bytes)
        self.disk = input + " / " + output
