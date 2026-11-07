"""Minimal KSP ConfigNode parser (reads files only). API: parse_file(path)->Node; Node.walk(), get, getall, has, children; iter_cfgs(root)."""
import re, os

class Node:
    __slots__ = ('name', 'values', 'nodes', 'file', 'line')
    def __init__(self, name, file=None, line=0):
        self.name = name; self.values = []; self.nodes = []; self.file = file; self.line = line
    def get(self, key, default=None):
        for k, v, _ in self.values:
            if k == key: return v
        return default
    def getall(self, key):
        return [v for k, v, _ in self.values if k == key]
    def has(self, key):
        return any(k == key for k, _, _ in self.values)
    def children(self, name=None):
        return [n for n in self.nodes if name is None or n.name == name]
    def walk(self):
        yield self
        for n in self.nodes:
            yield from n.walk()

def parse_text(text, file=None):
    root = Node('ROOT', file); stack = [root]; pending = None
    for ln, raw in enumerate(text.splitlines(), 1):
        line = raw.split('//', 1)[0]
        for p in re.split(r'([{}])', line):
            if p == '{':
                n = Node(pending[0].strip() if pending else '', file, pending[1] if pending else ln)
                stack[-1].nodes.append(n); stack.append(n); pending = None
            elif p == '}':
                if pending: _flush(stack[-1], pending); pending = None
                if len(stack) > 1: stack.pop()
            else:
                s = p.strip()
                if not s: continue
                if pending: _flush(stack[-1], pending)
                pending = (s, ln)
        if pending and '=' in pending[0]:
            _flush(stack[-1], pending); pending = None
    if pending: _flush(stack[-1], pending)
    return root

def _flush(node, pending):
    s, ln = pending
    if '=' in s:
        k, v = s.split('=', 1); node.values.append((k.strip(), v.strip(), ln))

def parse_file(path):
    with open(path, encoding='utf-8-sig', errors='replace') as f:
        return parse_text(f.read(), path)

def iter_cfgs(root):
    for dp, dn, fn in os.walk(root):
        for f in fn:
            if f.lower().endswith('.cfg'):
                yield os.path.join(dp, f)
