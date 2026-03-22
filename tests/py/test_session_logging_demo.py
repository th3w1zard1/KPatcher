"""Smoke tests for session logging + subprocess instrumentation (run with ``--log-level=TRACE``)."""

from __future__ import annotations

import logging
import subprocess
import sys
import threading

from session_logging import get_session_logger


def test_logger_levels_use_stdlib_logger() -> None:
    """Emit one message per level using a real ``logging.Logger``."""
    log = logging.getLogger("tests.demo.levels")
    log.trace("TRACE: fine-grained diagnostics")  # pyright: ignore[reportAttributeAccessIssue]
    log.verbose("VERBOSE: chatter between DEBUG and INFO")  # pyright: ignore[reportAttributeAccessIssue]
    log.debug("DEBUG: developer detail")
    log.info("INFO: normal progress")
    log.warning("WARN: recoverable issue")
    log.error("ERROR: failure that may continue")
    log.critical("CRITICAL: abort or severe failure")
    assert True


def test_subprocess_spawn_is_traced_at_trace_level() -> None:
    """At TRACE, ``session_logging`` wraps ``Popen`` (see stderr during pytest)."""
    subprocess.run(
        [sys.executable, "-c", "import sys; sys.stdout.write('ok')"],
        check=True,
        capture_output=True,
        text=True,
    )


def test_thread_start_traced_at_trace_level() -> None:
    done = threading.Event()

    def work() -> None:
        done.set()

    t = threading.Thread(target=work, name="demo-worker")
    t.start()
    t.join(timeout=5.0)
    assert done.is_set()


def test_session_logger_singleton_name() -> None:
    assert get_session_logger().name == "kpatcher.pytest.session"
