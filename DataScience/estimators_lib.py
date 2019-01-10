import sys

class Estimator:
    def __init__(self, type):
        self.type = type
    
        if self.type in {'ips', 'snips'}:
            self.data = {'n':0.,'N':0,'d':0.,'Ne':0,'c':0.,'SoS':0}
        else:
            print('Estimator type {} is not correct'.format(self.type))
            return

    def add_example(self, p_log, r, p_pred):
        self.data['N'] += 1
        if p_pred > 0:
            p_over_p = p_pred/p_log
            self.data['d'] += p_over_p
            self.data['Ne'] += 1
            if r != 0:
                self.data['n'] += r*p_over_p
                self.data['c'] = max(self.data['c'], r*p_over_p)
                self.data['SoS'] += (r*p_over_p)**2
                
    def get_estimate(self):
        if self.type == 'ips':
            return self.data['n']/self.data['N']
        elif self.type == 'snips':
            return self.data['n']/self.data['d']