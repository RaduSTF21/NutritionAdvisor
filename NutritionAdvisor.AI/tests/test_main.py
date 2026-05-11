import sys
import os
import types
import pytest

import importlib.util

# Load the AI service module by file path (package may not be installed in this environment)
spec = importlib.util.spec_from_file_location("ai_main", os.path.abspath(os.path.join(os.path.dirname(__file__), '..', 'main.py')))
ai_main = importlib.util.module_from_spec(spec)
spec.loader.exec_module(ai_main)


def test_is_placeholder_url_true():
    assert ai_main._is_placeholder_url("http://example.com/foo") is True
    assert ai_main._is_placeholder_url("https://www.example.org") is True


def test_is_placeholder_url_false():
    assert ai_main._is_placeholder_url("https://real-site.com/recipe") is False
    assert ai_main._is_placeholder_url(None) is False


class DummyResponse:
    def __init__(self, status_code=200, url=None, history=()):
        self.status_code = status_code
        self.url = url
        self.history = history


class DummyRequests:
    def __init__(self, head_response=None, get_response=None):
        self._head_response = head_response or DummyResponse(200)
        self._get_response = get_response or DummyResponse(200)

    def head(self, url, timeout=None, allow_redirects=True):
        return self._head_response

    def get(self, url, timeout=None, allow_redirects=True, stream=False):
        return self._get_response


def test_is_url_accessible_head_ok(monkeypatch):
    dr = DummyResponse(status_code=200, url="https://real-site.com/recipe")
    dummy = DummyRequests(head_response=dr)
    monkeypatch.setitem(sys.modules, 'requests', dummy)
    ai_main._URL_VALIDATION_CACHE.clear()
    assert ai_main._is_url_accessible("https://real-site.com/recipe") is True


def test_is_url_accessible_head_405_then_get(monkeypatch):
    head = DummyResponse(status_code=405, url="https://real-site.com/recipe", history=())
    get = DummyResponse(status_code=200, url="https://real-site.com/recipe")
    dummy = DummyRequests(head_response=head, get_response=get)
    monkeypatch.setitem(sys.modules, 'requests', dummy)

    ai_main._URL_VALIDATION_CACHE.clear()
    assert ai_main._is_url_accessible("https://real-site.com/recipe") is True


def test_is_url_accessible_redirects_to_homepage(monkeypatch):
    # Simulate redirect to homepage with history
    head = DummyResponse(status_code=200, url="https://real-site.com/index", history=(1,))
    # original had path /recipe, final is /index -> considered redirected_to_homepage
    dummy = DummyRequests(head_response=head)
    monkeypatch.setitem(sys.modules, 'requests', dummy)

    ai_main._URL_VALIDATION_CACHE.clear()
    assert ai_main._is_url_accessible("https://real-site.com/recipe") is False
