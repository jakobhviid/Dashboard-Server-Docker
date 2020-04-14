class ContainerOverviewInfo:
    def __init__(self, container_id, name):
        self.id = container_id
        self.name = name

    def with_image_tags(self, image_tags):
        self.image = image_tags

    def with_state(self, state):
        status = state['Status']

        startTime = state['StartedAt']
        finishTime = state['FinishedAt']

        self.state = {
            'status': status, 'startTime': startTime, 'finishTime': finishTime
        }
        if 'Health' in state:
            del state['Health']['Log']
            self.state['health'] = state['Health']
            # lowercase dictionary keys fix
            self.state['health']['status'] = self.state['health'].pop('Status')
            self.state['health']['failingStreak'] = self.state['health'].pop(
                'FailingStreak')

    def with_creation_time(self, created):
        self.creation_time = created
