
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
        self.memory_percentage = round(memory_percent, 2)

    def with_net_i_o(self, input_bytes, output_bytes):
        self.net_input_bytes = input_bytes
        self.net_output_bytes = output_bytes

    def with_disk_i_o(self, input_bytes, output_bytes):
        self.disk_input_bytes = input_bytes
        self.disk_output_bytes = output_bytes
